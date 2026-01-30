namespace WaterSort.Core.Solvers.Obstacles;



public readonly record struct ObstacleSignature(string Key)
{
    public static ObstacleSignature Build(State state, int tubeIndex)
    {
        var chain = state.Obstacles.GetEffectiveChain(tubeIndex);
        if (chain.Count == 0)
            return new ObstacleSignature("none");

        // Kind + (Color?) + (CellTargets.Count?)
        // 这样能区分：Curtain#3 vs Curtain#5，Question(2) vs Question(1)
        var parts = new List<string>(chain.Count);

        foreach (var e in chain)
        {
            var s = e.Kind.ToString();

            if (e.Color is not null)
                s += $"#{e.Color.Value}";

            if (e.CellTargets is not null && e.CellTargets.Count > 0)
                s += $"({e.CellTargets.Count})";

            parts.Add(s);
        }

        return new ObstacleSignature(string.Join("|", parts));
    }
}

public readonly record struct MonoTypeKey(
    int Capacity,
    ObstacleSignature ObstacleSig)
{
    public static MonoTypeKey Build(State state, int tubeIndex)
    {
        var tube = state.Tubes[tubeIndex];

        // 只用“可见有效链”（MaskLower 截断）
        var sig = ObstacleSignature.Build(state, tubeIndex);

        return new MonoTypeKey(tube.Capacity, sig);
    }
}

public interface IObstacleHandler
{
    ObstacleKind Kind { get; }

    /// <summary>
    /// 对 tube ability 进行适配（类似 middleware）。
    /// 只处理“本 tube 的 entry”。
    /// </summary>
    void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability);
}

public abstract class ObstacleHandler : IObstacleHandler
{
    public virtual ObstacleKind Kind { get; }

    /// <summary>
    /// 对 tube ability 进行适配（类似 middleware）。
    /// 只处理“本 tube 的 entry”。
    /// </summary>
    public virtual void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 验证类型和瓶子下标是否匹配
    /// </summary>
    /// <param name="state"></param>
    /// <param name="entry"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public bool IsMatch(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        if (!entry.Enabled || entry.Kind != Kind)
            return false;

        if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
            return false;

        if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
            return false;

        return true;
    }
}

/// <summary>
/// 问号道具 移动能力
/// </summary>
public sealed class MysteryHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.Mystery;

    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        // 0) 快速过滤
        // if (!entry.Enabled || entry.Kind != Kind)
        //     return;
        //
        // if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
        //     return;
        //
        // if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
        //     return;
        if (!IsMatch(state, entry, ref ability))
            return;

        if (ability.ExportCount <= 0)
            return;

        if (entry.CellTargets is null || entry.CellTargets.Count == 0)
            return;

        var tube = state.Tubes[ability.TubeIndex];
        if (tube.Count <= 0)
            return;

        // 1) 问号顶层（最高覆盖层）
        var topLayer = entry.CellTargets[^1];

        // 防御：topLayer 不应超出 tube 容量范围（或当前层范围）
        // 这里用 tube.Capacity 更合理（如果没有就删掉）
        if (topLayer < 0 || topLayer >= tube.Capacity)
            return;

        // 2) 如果 TopBoundary 已经越过 topLayer，说明问号已揭开，不限制导出
        var topBoundary = tube.TopBoundary;
        if (topBoundary > topLayer)
            return;

        // 3) 计算“允许导出的顶部连续段长度”
        //    顶部可导出段 = [topLayer+1 .. tube.Count-1]
        var exportLimit = tube.Count - (topLayer + 1);

        // exportLimit <=0 => 说明问号顶层就在顶部或高于顶部 => 完全不能倒出
        if (exportLimit <= 0)
        {
            ability.ExportCount = 0;
            return;
        }

        // 4) 应用限制：ExportCount 取 min
        if (exportLimit < ability.ExportCount)
            ability.ExportCount = exportLimit;
    }
}

/// <summary>
/// 窗帘，纸盒 移动能力
/// </summary>
public sealed class CurtainHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.Curtain;

    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        // 0) 快速过滤
        // if (!entry.Enabled)
        //     return;
        //
        // if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
        //     return;
        //
        // if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
        //     return;
        if (!IsMatch(state, entry, ref ability))
            return;

        // Curtain = 禁止该 tube 的任何交互
        ability.ExportCount = 0;
        ability.AcceptCount = 0;
    }
}

/// <summary>
/// 固定瓶，机械臂 移动能力
/// </summary>
public sealed class ClampHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.Clamp;

    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        // 0) 快速过滤
        // if (!entry.Enabled)
        //     return;
        //
        // if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
        //     return;
        //
        // if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
        //     return;
        if (!IsMatch(state, entry, ref ability))
            return;

        // Clamp = 禁止该 tube 倒出
        ability.ExportCount = 0;
    }
}


/// <summary>
/// 石膏，旁消 移动能力
/// </summary>
public sealed class PlasterHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.Plaster;

    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        // if (!entry.Enabled)
        //     return;
        //
        // if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
        //     return;
        //
        // if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
        //     return;
        if (!IsMatch(state, entry, ref ability))    
            return;
        
        // Plaster = 禁止该 tube 的任何交互
        ability.ExportCount = 0;
        ability.AcceptCount = 0;
    }
}



/// <summary>
/// 固定颜色 移动能力
/// </summary>
public sealed class ColorLockHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.ColorLock;
    
    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {
        // if (!entry.Enabled)
        //     return;
        //
        // if (ability.TubeIndex < 0 || ability.TubeIndex >= state.Tubes.Count)
        //     return;
        //
        // if (entry.TubeTargets.Count > 0 && !entry.TubeTargets.Contains(ability.TubeIndex))
        //     return;
        if (!IsMatch(state, entry, ref ability))
            return;

        if (!entry.Color.HasValue)
        {
            throw new ArgumentException(
                $"Obstacle {entry.Kind} requires Color, but entry.Color is null. EntryId={entry.Id}"
            );
        }
        
        ability.AcceptColor = entry.Color.Value;
        if (state.Tubes[ability.TubeIndex].TopColor != entry.Color.Value)
        {
            ability.AcceptCount = 0;
        }
    }
}



/// <summary>
/// 柜子 移动能力
/// </summary>
public sealed class CupboardHandler : ObstacleHandler
{
    public override ObstacleKind Kind => ObstacleKind.Cupboard;
    
    public override void Adapt(State state, ObstacleEntry entry, ref TubeMoveAbility ability)
    {   
        if (!IsMatch(state, entry, ref ability))
            return;
        
        ability.AcceptCount = 0;
        ability.ExportCount = 0;
    }
}

public sealed class ObstacleRegistry
{
    private readonly Dictionary<ObstacleKind, IObstacleHandler> _map = new();

    public void Register(IObstacleHandler handler)
        => _map[handler.Kind] = handler;

    public IObstacleHandler GetHandler(ObstacleKind kind)
    {
        if (_map.TryGetValue(kind, out var h))
            return h;

        throw new KeyNotFoundException($"Obstacle handler not registered: {kind}");
    }

    public static ObstacleRegistry CreateDefault()
    {
        var registry = new ObstacleRegistry();
        registry.Register(new MysteryHandler());
        registry.Register(new CurtainHandler());
        registry.Register(new ClampHandler());
        // registry.Register(new PlasterHandler());
        registry.Register(new ColorLockHandler());
        // registry.Register(new CupboardHandler());
        return registry;
    }
}

public sealed class ObstaclePipeline
{
    private readonly ObstacleRegistry _registry;

    public ObstaclePipeline(ObstacleRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// 1) base abilities -> 2) obstacle adapt -> 3) filtered abilities
    /// </summary>
    public List<TubeMoveAbility> BuildAdaptedTubeAbilities(State state)
    {
        var result = new List<TubeMoveAbility>(capacity: state.Tubes.Count);

        foreach (var baseAbility in state.BuildTubeAbilities())
        {
            var ability = baseAbility; // copy struct or copy-by-value

            var tubeIndex = ability.TubeIndex;
            var tube = state.Tubes[tubeIndex];

            // ⚠️ 强烈建议：用 EffectiveChain（遮蔽截断）
            var chain = state.Obstacles.GetEffectiveChain(tubeIndex);

            foreach (var entry in chain)
            {
                if (!entry.Enabled) continue; // 防御式（虽然 effective chain 一般已经过滤）
                var handler = _registry.GetHandler(entry.Kind);
                handler.Adapt(state, entry, ref ability);
            }

            // 最终过滤：既不能倒也不能接
            if (ability.ExportCount <= 0 && ability.AcceptCount <= 0)
                continue;

            result.Add(ability);
        }

        return result;
    }

    /// <summary>
    /// 把 tube abilities 按 solver 关注的维度分组。
    /// 你可以按需要继续扩展 Key。
    /// </summary>
    public static Dictionary<MonoTypeKey, List<TubeMoveAbility>> GroupMonoByType(
        IReadOnlyList<TubeMoveAbility> abilities,
        State state)
    {
        var map = new Dictionary<MonoTypeKey, List<TubeMoveAbility>>();

        foreach (var a in abilities)
        {
            var tube = state.Tubes[a.TubeIndex];
            
            if (!tube.IsMonochrome) continue;

            var key = MonoTypeKey.Build(state, a.TubeIndex);

            if (!map.TryGetValue(key, out var list))
            {
                list = new List<TubeMoveAbility>();
                map.Add(key, list);
            }

            list.Add(a);
        }

        return map;
    }
}
