namespace WaterSort.Core.Solvers.Obstacles;

public sealed class AfterApplyFacts
{
    public required Move Move { get; init; }

    public int From => Move.From;
    public int To => Move.To;

    /// <summary>
    /// 本次移动后获得的钥匙颜色集合（用于解锁柜子）。
    /// </summary>
    public HashSet<int> ObtainedKeyColors { get; init; } = new();

    /// <summary>
    /// 本次移动后完成的 tube（满且单色等你定义的完成条件）。
    /// </summary>
    public HashSet<int> CompletedTubes { get; init; } = new();

    /// <summary>
    /// 本次移动后“完成的颜色”（用于揭开窗帘）。
    /// </summary>
    public HashSet<int> CompletedColors { get; init; } = new();
}

public sealed class AfterApplyFactsBuilder
{
    public AfterApplyFacts Build(State state, Move move)
    {
        var facts = new AfterApplyFacts { Move = move };

        // 1) tube 完成（只看 from/to，增量）
        TryCollectCompletedTube(state, move.From, facts);
        TryCollectCompletedTube(state, move.To, facts);

        // 2) 钥匙获得（只看 from/to tube 上的 KeyCupboard entries）
        CollectObtainedKeys(state, move, facts);

        // 3) 颜色完成（由“完成 tube 的单色”推导）
        foreach (var tubeIndex in facts.CompletedTubes)
        {
            var tube = state.Tubes[tubeIndex];
            if (tube.Count <= 0) continue;

            // 完成 tube 必是单色，这里取底色/首色更稳
            // （TopColor 在你某些实现里可能有特殊含义）
            facts.CompletedColors.Add(tube.TopColor);
        }

        return facts;
    }

    private static void TryCollectCompletedTube(State state, int tubeIndex, AfterApplyFacts facts)
    {
        if (tubeIndex < 0 || tubeIndex >= state.Tubes.Count) return;

        if (IsTubeCompleted(state, tubeIndex))
            facts.CompletedTubes.Add(tubeIndex);
    }

    private static void CollectObtainedKeys(State state, Move move, AfterApplyFacts facts)
    {
        // 只扫描 from/to 上的 key entries（增量）
        CollectObtainedKeysOnTube(state, move.From, facts);
        if (move.To != move.From)
            CollectObtainedKeysOnTube(state, move.To, facts);
    }

    private static void CollectObtainedKeysOnTube(State state, int tubeIndex, AfterApplyFacts facts)
    {
        var list = state.Obstacles.GetByTube(tubeIndex);
        if (list.Count == 0) return;

        // 注意：这里收集“将要获得的钥匙”，具体 Enabled=false 仍由 updater 做
        foreach (var e in list)
        {
            if (!e.Enabled) continue;
            if (e.Kind != ObstacleKind.CupboardKey) continue;
            if (e.Color is null) continue;

            if (e.CellTargets is null || e.CellTargets.Count == 0)
                continue;

            var keyCell = e.CellTargets[0];
            var tube = state.Tubes[tubeIndex];

            // 简化：钥匙层暴露到顶部（你这里 keyCell 只取第一个 cell）
            if (keyCell == tube.Count - 1)
            {
                facts.ObtainedKeyColors.Add(e.Color.Value);
            }
        }
    }

    private static bool IsTubeCompleted(State state, int tubeIndex)
    {
        var tube = state.Tubes[tubeIndex];

        if (tube.FreeSpace != 0)
            return false;

        if (!tube.IsMonochrome)
            return false;

        // 关键：有未揭开的问号 -> 不算完成
        // ⚠️ 这里不能用 EffectiveChain，因为它可能被 MaskLower 截断（例如 Cupboard 在顶层）
        // 我们要的语义是：问号只要存在且 Enabled，就阻止完成
        var list = state.Obstacles.GetByTube(tubeIndex);
        foreach (var e in list)
        {
            if (!e.Enabled) continue;

            if (e.Kind == ObstacleKind.Mystery)
                return false;

            // 如果你后续还有“帷幕遮蔽可见性”也可加在这里
            // if (e.Kind == ObstacleKind.Curtain) return false;
        }

        return true;
    }
}

public sealed class ObstacleUpdater
{
    private readonly AfterApplyFactsBuilder _factsBuilder = new();

    /// <summary>
    /// ApplyCore 后调用：更新障碍物运行时状态（Enabled/CellTargets/Extra），有变更则重建索引。
    /// </summary>
    public void UpdateInPlace(State state, Move move)
    {
        if (state.ObstacleEntries.Count == 0)
            return;

        var facts = _factsBuilder.Build(state, move);

        var changed = false;

        // A) 先处理局部更新：只关注 from/to、暴露、旁侧完成等
        changed |= ApplyLocalUpdates(state, facts);

        // B) 再处理全局更新：只关注 “获得钥匙 / 颜色完成 / tube 完成”
        changed |= ApplyGlobalUpdates(state, facts);

        if (changed)
            state.RebuildObstacleCatalog();
    }

    // ─────────────────────────────
    // A) Local Updates
    // ─────────────────────────────

    private bool ApplyLocalUpdates(State state, AfterApplyFacts facts)
    {
        var changed = false;

        // 只扫 from/to 的 RequireAfterApplyChain（并遵守 MaskLower 截断）
        Span<int> touched = stackalloc int[2] { facts.From, facts.To };

        for (int i = 0; i < touched.Length; i++)
        {
            var tubeIndex = touched[i];

            var chain = state.Obstacles.GetRequireAfterApplyChain(tubeIndex);
            if (chain.Count == 0) continue;

            foreach (var entry in chain)
            {
                if (!entry.Enabled) continue;

                // // 门控：被顶层 MaskLower 遮蔽的，不允许更新（例如柜子挡住窗帘/石膏）
                // if (IsMaskedForUpdate(state, tubeIndex, entry))
                //     continue;

                // Local 类型：Question / KeyCupboard / Scroll / IceBox
                if (UpdateLocalOne(state, facts, entry))
                    changed = true;
            }
        }

        return changed;
    }

    private bool UpdateLocalOne(State state, AfterApplyFacts facts, ObstacleEntry entry)
    {
        return entry.Kind switch
        {
            ObstacleKind.Mystery => UpdateMystery(state, facts, entry),
            ObstacleKind.CupboardKey => UpdateCupboardKey(state, facts, entry),
            // ObstacleKind.Scroll => UpdateScroll(state, facts, entry),
            // ObstacleKind.IceBox => UpdateIceBox(state, facts, entry),
            _ => false
        };
    }

    // ─────────────────────────────
    // B) Global Updates
    // ─────────────────────────────

    private bool ApplyGlobalUpdates(State state, AfterApplyFacts facts)
    {
        var changed = false;

        // 1) 钥匙获得 -> 解锁柜子
        if (facts.ObtainedKeyColors.Count > 0)
        {
            changed |= UnlockCupboardsByKeys(state, facts.ObtainedKeyColors);
        }

        // 2) 颜色完成 -> 揭开窗帘
        if (facts.CompletedColors.Count > 0)
        {
            changed |= RevealCurtainsByCompletedColors(state, facts.CompletedColors);
        }

        // 3) tube 完成 -> 石膏旁消
        if (facts.CompletedTubes.Count > 0)
        {
            changed |= BreakSidePlastersByCompletedTubes(state, facts.CompletedTubes);
        }

        return changed;
    }

    private static bool UnlockCupboardsByKeys(State state, HashSet<int> obtainedKeyColors)
    {
        var changed = false;

        foreach (var e in state.ObstacleEntries)
        {
            if (!e.Enabled) continue;
            if (e.Kind != ObstacleKind.Cupboard) continue;
            if (e.Color is null) continue;

            if (!obtainedKeyColors.Contains(e.Color.Value))
                continue;

            e.Enabled = false;
            changed = true;
        }

        return changed;
    }

    private static bool RevealCurtainsByCompletedColors(State state, HashSet<int> completedColors)
    {
        if (completedColors.Count == 0)
            return false;

        var changed = false;

        for (int tubeIndex = 0; tubeIndex < state.Tubes.Count; tubeIndex++)
        {
            var curtains = state.Obstacles.GetEffectiveChain(tubeIndex, ObstacleKind.Curtain);
            if (curtains.Count == 0) 
                continue;

            foreach (var curtain in curtains)
            {
                // 这里 curtain 必然 Enabled 且 Kind==Curtain
                if (curtain.Color is null)
                    continue;

                if (!completedColors.Contains(curtain.Color.Value))
                    continue;

                curtain.Enabled = false;
                changed = true; // 直接赋值更清晰
            }
        }

        return changed;
    }

    /// <summary>
    /// tube 完成 -> 触发邻侧 tube 的石膏(旁消)削减/移除
    /// </summary>
    private static bool BreakSidePlastersByCompletedTubes(State state, HashSet<int> completedTubes)
    {
        var changed = false;

        return false;
    }

    private static IEnumerable<int> GetSideNeighbors(State state, int tubeIndex)
    {
        // 最简单：线性左右邻居（你如果有坐标系统，在这里换成 grid 邻接）
        // state.TubeLayouts;
        if (tubeIndex - 1 >= 0) yield return tubeIndex - 1;
        if (tubeIndex + 1 < state.Tubes.Count) yield return tubeIndex + 1;
    }

    // ─────────────────────────────
    // 门控逻辑
    // ─────────────────────────────

    private static bool IsMaskedForUpdate(State state, int tubeIndex, ObstacleEntry self)
    {
        var selfDesc = state.Obstacles.Describe(self.Kind);

        // 自己是 MaskLower（顶层硬遮蔽）一般允许更新（比如柜子自己解锁）
        if (selfDesc.Exclusivity == ObstacleExclusivity.MaskLower)
            return false;

        var effective = state.Obstacles.GetEffectiveChain(tubeIndex);
        if (effective.Count == 0) return false;

        var top = effective[0];
        if (!top.Enabled) return false;

        if (ReferenceEquals(top, self) || top.Id == self.Id)
            return false;

        var topDesc = state.Obstacles.Describe(top.Kind);
        return topDesc.Exclusivity == ObstacleExclusivity.MaskLower;
    }

    private static bool IsTubeMaskedByTopBlocker(State state, int tubeIndex)
    {
        var effective = state.Obstacles.GetEffectiveChain(tubeIndex);
        if (effective.Count == 0) return false;

        var top = effective[0];
        if (!top.Enabled) return false;

        var topDesc = state.Obstacles.Describe(top.Kind);
        return topDesc.Exclusivity == ObstacleExclusivity.MaskLower;
    }

    // ─────────────────────────────
    // Local Update Implementations
    // ─────────────────────────────

    private static bool UpdateMystery(State state, AfterApplyFacts facts, ObstacleEntry e)
    {
        // 只关心 from tube
        if (!e.TubeTargets.Contains(facts.From))
            return false;

        if (e.CellTargets is null || e.CellTargets.Count == 0)
            return false;

        var tube = state.Tubes[facts.From];

        // 你这里用 ^1 当“顶层问号 cell”，保持你的定义
        var topLayer = e.CellTargets[^1];
        if (topLayer != tube.Count - 1)
            return false;

        // TopBoundary：当前可见边界（>= topBoundary 的 cell 视为已经揭开）
        var topBoundary = tube.TopBoundary;

        // 只保留仍然被遮蔽的 cell
        var newCells = e.CellTargets.Where(i => i < topBoundary).ToList();

        // 如果没有变化，不必标记 changed
        if (newCells.Count == e.CellTargets.Count)
            return false;

        if (newCells.Count == 0)
        {
            e.Enabled = false;
            e.CellTargets = null;
        }
        else
        {
            e.CellTargets = newCells;
        }
        return true;
    }

    private static bool UpdateCupboardKey(State state, AfterApplyFacts facts, ObstacleEntry e)
    {
        // key 只挂一个 tube
        if (e.TubeTargets.Count == 0) return false;
        var tubeIndex = e.TubeTargets[0];

        if (tubeIndex != facts.From && tubeIndex != facts.To)
            return false;

        if (e.Color is null)
            return false;

        // FactsBuilder 已经判断“钥匙暴露”，这里只做状态落地
        if (!facts.ObtainedKeyColors.Contains(e.Color.Value))
            return false;

        e.Enabled = false;
        return true;
    }

    private static bool UpdateScroll(State state, AfterApplyFacts facts, ObstacleEntry e)
    {
       // 卷轴，有颜色完成即刻缩一个
       var tubeIndexs = e.TubeTargets;
       if (tubeIndexs.Count <= 0) return false;

       // 说明你的 ExploreMove 阶段漏过滤了
       if (tubeIndexs.Contains(facts.From) || tubeIndexs.Contains(facts.To))
       {
           // 说明你的 ExploreMove 阶段漏过滤了
           System.Diagnostics.Debug.Fail(
               $"Move 进入卷轴范围：From={facts.From}, To={facts.To}, Scroll={string.Join(",", tubeIndexs)}");
           return false;
       }
       
       if (facts.CompletedTubes.Count <= 0)
       {
           return false;
       }
       var newTargets = e.TubeTargets.Take(e.TubeTargets.Count - 1).ToList();
       e.TubeTargets = newTargets;
       
       if (newTargets.Count == 0) e.Enabled = false;
       
       return true;
    }

    private static bool UpdateIceBox(State state, AfterApplyFacts facts, ObstacleEntry e)
    {
        // 组内任一 tube 完成 -> 整组解冻
        foreach (var t in e.TubeTargets)
        {
            if (facts.CompletedTubes.Contains(t))
            {
                e.Enabled = false;
                return true;
            }
        }

        return false;
    }
}

