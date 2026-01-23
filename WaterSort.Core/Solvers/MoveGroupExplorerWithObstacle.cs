using WaterSort.Core.Solvers.Obstacles;

namespace WaterSort.Core.Solvers;


// ============================================================
// MoveGroupExplorer：生成 MoveGroup（Explore）+ 规整化（Normal）
// ============================================================
public sealed class MoveGroupExplorerWithObstacle : IMoveGroupExplorer
{
    private ObstaclePipeline _obstaclePipeline = new ObstaclePipeline(ObstacleRegistry.CreateDefault());
    
    public IEnumerable<MoveGroup> Explore(State state)
    {
        foreach (var bucket in BuildAndGroupBuckets(state))
        {
            foreach (var g in ExploreBucket(state, bucket))
                yield return g;
        }
    }

    public IEnumerable<MoveGroup> Normal(State state)
    {
        foreach (var bucket in BuildAndGroupBuckets(state))
        {
            foreach (var g in NormalCore(state, bucket))
                yield return g;
        }
    }

    // ------------------------------------------------------------
    // Buckets pipeline
    // ------------------------------------------------------------
    private IEnumerable<ColorBucket> BuildAndGroupBuckets(State state)
    { 
        var abilities = _obstaclePipeline.BuildAdaptedTubeAbilities(state);
        var buckets = ColorBucket.BuildBuckets(abilities);

        foreach (var bucket in buckets.Values)
        {
            bucket.GroupBy(state);
            yield return bucket;
        }
    }

    // ------------------------------------------------------------
    // Normal：单色瓶规整化（把某个 mono 腾空）
    // ------------------------------------------------------------
    private IEnumerable<MoveGroup> NormalCore(State state, ColorBucket bucket)
    {
        static bool CanFreeable(TubeMoveAbility from, int allAcceptCount)
            => from.ExportCount <= (allAcceptCount - from.AcceptCount);

        var monos = bucket.Abilities
            .Where(a =>
                a.ExportCount > 0 &&
                a.AcceptCount > 0 &&
                state.Tubes[a.TubeIndex].IsMonochrome)
            .ToList();

        if (monos.Count <= 1)
            yield break;

        int allAcceptCount = monos.Sum(x => x.AcceptCount);

        // 1) 找 freeSource：export 少优先（更容易腾空）
        var freeSource = monos
            .Where(from => CanFreeable(from, allAcceptCount))
            .OrderBy(from => from.ExportCount)
            .ThenBy(from => from.TubeIndex)
            .FirstOrDefault();

        if (freeSource == null)
            yield break;

        // 2) targets：排除 freeSource，优先 AcceptCount 大的（更容易接满）
        var targets = monos
            .Where(a => a.TubeIndex != freeSource.TubeIndex)
            .OrderByDescending(a => a.AcceptCount)
            .ThenBy(a => a.TubeIndex)
            .ToList();

        if (targets.Sum(x => x.AcceptCount) < freeSource.ExportCount)
            yield break;

        // 3) 必须 full-drain freeSource
        var moveGroup = BuildGreedyGroup(freeSource, targets, freeSource.ExportCount);
        if (moveGroup.Moves.Count == 0)
            yield break;

        int moved = moveGroup.Moves.Sum(m => m.Count);
        if (moved < freeSource.ExportCount)
            yield break;

        moveGroup.Description = $"颜色({bucket.Color})规整化（腾空 {freeSource.TubeIndex}）";
        yield return moveGroup;
    }

    // ------------------------------------------------------------
    // Explore：桶内策略层（1 / 1+2 / 2 / 3 / 4）
    // ------------------------------------------------------------
    private IEnumerable<MoveGroup> ExploreBucket(State state, ColorBucket bucket)
    {
        var fromNonMonos = bucket.FromNonMonos;
        var fromMonos = bucket.FromMonos;
        var toNonMonos = bucket.ToNonMonos;
        var toMonos = bucket.ToMonos;

        // ========== 1) nonMono -> nonMono ==========
        foreach (var from in fromNonMonos)
        {
            var tos = SelectTargets(from, toNonMonos);
            if (tos.Count == 0) continue;
            if (tos.Sum(x => x.AcceptCount) < from.ExportCount) continue;

            var g = BuildGreedyGroup(from, tos, from.ExportCount);
            g.Description = "1) nonMono -> nonMono";
            yield return g;
        }

        // ========== 1 + 2) nonMono -> nonMono + mono ==========
        foreach (var from in fromNonMonos)
        {
            var nonTos = SelectTargets(from, toNonMonos);
            if (nonTos.Count == 0) continue;

            int nonCap = nonTos.Sum(x => x.AcceptCount);
            if (nonCap >= from.ExportCount)
                continue; // 已经属于 case 1

            var monoTos = SelectTargets(from, toMonos);
            if (monoTos.Count == 0) continue;

            int monoCap = monoTos.Sum(x => x.AcceptCount);
            if (nonCap + monoCap < from.ExportCount)
                continue;

            // 先填 non-mono
            var prefix = new MoveGroup();
            int remain = from.ExportCount;
            AppendGreedy(prefix, from, nonTos, ref remain);

            if (remain <= 0)
                continue;

            // mono 分叉：按类型分组（目前按 capacity 分组）
            var monoGroups = GroupMonoByType(monoTos, state);

            foreach (var group in monoGroups.Values)
            {
                if (group.Sum(x => x.AcceptCount) < remain)
                    continue;

                var g = prefix.DeepCopy();
                int r = remain;
                AppendGreedy(g, from, group, ref r);

                if (r == 0)
                {
                    g.Description = "1+2) nonMono -> nonMono + mono";
                    yield return g;
                }
            }
        }

        // ========== 2) nonMono -> mono ==========
        foreach (var from in fromNonMonos)
        {
            // 如果存在 non-mono 可接，则不是纯 2
            if (SelectTargets(from, toNonMonos).Count > 0)
                continue;

            var monoTos = SelectTargets(from, toMonos);
            if (monoTos.Count == 0)
                continue;

            if (monoTos.Sum(x => x.AcceptCount) < from.ExportCount)
                continue;

            var monoGroups = GroupMonoByType(monoTos, state);

            foreach (var group in monoGroups.Values)
            {
                if (group.Sum(x => x.AcceptCount) < from.ExportCount)
                    continue;

                var g = BuildGreedyGroup(from, group, from.ExportCount);
                if (g.Moves.Sum(m => m.Count) == from.ExportCount)
                {
                    g.Description = "2) nonMono -> mono";
                    yield return g;
                }
            }
        }

        // ========== 3) mono -> nonMono ==========
        foreach (var from in fromMonos)
        {
            var tos = SelectTargets(from, toNonMonos);
            if (tos.Count == 0) continue;
            if (tos.Sum(x => x.AcceptCount) < from.ExportCount) continue;

            var g = BuildGreedyGroup(from, tos, from.ExportCount);
            g.Description = "3) mono -> nonMono";
            yield return g;
        }

        // ========== 4) mono -> mono（不同类型） ==========
        // 注意：这里不能只用 toMonos，而是用 bucket.Abilities 来分组，保证类型集合完整
        var monoGroupsMap = GroupMonoByType(bucket.Abilities, state);
        if (monoGroupsMap.Count < 2)
            yield break;
        
        Dictionary<int, MonoTypeKey> tubeToGroupKey = new();
        foreach (var kv in monoGroupsMap)
        {
            foreach (var a in kv.Value)
                tubeToGroupKey[a.TubeIndex] = kv.Key;
        }


        foreach (var from in fromMonos)
        {
            var monoTos = SelectTargets(from, toMonos);
            if (monoTos.Count == 0)
                continue;

            // from 所在类型组（排除同类型）
            var fromGroupKey = monoGroupsMap
                .FirstOrDefault(g => g.Value.Any(x => x.TubeIndex == from.TubeIndex))
                .Key;

            foreach (var group in monoGroupsMap)
            {
                if (fromGroupKey != null && group.Key == fromGroupKey)
                    continue;

                var groupTos = SelectTargets(from, group.Value);
                if (groupTos.Count == 0)
                    continue;

                if (groupTos.Sum(x => x.AcceptCount) < from.ExportCount)
                    continue;

                var g = BuildGreedyGroup(from, groupTos, from.ExportCount);
                g.Description = "4) mono -> mono (different type)";
                yield return g;
            }
        }
    }

    // ------------------------------------------------------------
    // Utils：选择 / 构造 MoveGroup
    // ------------------------------------------------------------
    private static List<TubeMoveAbility> SelectTargets(TubeMoveAbility from, IEnumerable<TubeMoveAbility> candidates)
    {
        var list = new List<TubeMoveAbility>();

        foreach (var to in candidates)
        {
            if (to.AcceptCount <= 0) continue;
            if (to.TubeIndex == from.TubeIndex) continue;
            list.Add(to);
        }

        return list;
    }

    private static MoveGroup BuildGreedyGroup(TubeMoveAbility from, IEnumerable<TubeMoveAbility> tos, int needCount)
    {
        var g = new MoveGroup();
        int remain = needCount;

        AppendGreedy(g, from, tos, ref remain);
        return g;
    }

    private static void AppendGreedy(
        MoveGroup group,
        TubeMoveAbility from,
        IEnumerable<TubeMoveAbility> tos,
        ref int remainCount)
    {
        foreach (var to in tos)
        {
            if (remainCount <= 0)
                break;

            if (to.AcceptCount <= 0)
                continue;

            if (to.TubeIndex == from.TubeIndex)
                continue;

            int moved = Math.Min(remainCount, to.AcceptCount);
            if (moved <= 0)
                continue;

            group.Moves.Add(new Move
            {
                From = from.TubeIndex,
                To = to.TubeIndex,
                Count = moved,
                Color = from.ExportColor
            });

            remainCount -= moved;
        }
    }

    /// <summary>
    /// mono 类型分组：目前按 Capacity 分组
    /// 你后续可以升级成 (Capacity, Kind) 或 TubeStructuralKey
    /// </summary>
    private Dictionary<MonoTypeKey, List<TubeMoveAbility>> GroupMonoByType(List<TubeMoveAbility> abilities, State state)
    {
        // return abilities
        //     .GroupBy(a => state.Tubes[a.TubeIndex].Capacity)
        //     .Select(g => g.ToList())
        //     .ToList();
        return ObstaclePipeline.GroupMonoByType(abilities, state);
    }
}
