namespace WaterSort.Core.Solvers.Obstacles;

using System.Collections.ObjectModel;

/// <summary>
/// 障碍物的视图目录，支持优先级别和遮蔽处理
/// </summary>
public sealed class ObstacleCatalog
{
    public IReadOnlyList<ObstacleEntry> All { get; }

    private readonly IReadOnlyDictionary<int, IReadOnlyList<ObstacleEntry>> _byTube;
    private readonly IReadOnlyDictionary<(int tube, int cell), IReadOnlyList<ObstacleEntry>> _byCell;

    public ObstacleCatalog(IReadOnlyList<ObstacleEntry> all)
    {
        All = all ?? Array.Empty<ObstacleEntry>();

        _byTube = BuildByTube(All);
        _byCell = BuildByCell(All);
    }

    /// <summary>
    /// 获取某个 tube 上挂载的所有障碍物（按 Priority 降序排列）。
    /// </summary>
    public IReadOnlyList<ObstacleEntry> GetByTube(int tubeIndex)
        => _byTube.TryGetValue(tubeIndex, out var list) ? list : Array.Empty<ObstacleEntry>();

    /// <summary>
    /// 获取某个 tube 的某个 cell 上挂载的障碍物（通常用于 Key / Question 等 cell 级机制）。
    /// </summary>
    public IReadOnlyList<ObstacleEntry> GetByCell(int tubeIndex, int cellIndex)
        => _byCell.TryGetValue((tubeIndex, cellIndex), out var list) ? list : Array.Empty<ObstacleEntry>();

    /// <summary>
    /// 获取某个 tube 当前“有效链”（EffectiveChain）：
    /// - 跳过 Enabled=false 的障碍
    /// - 按 Priority 降序
    /// - 遇到 MaskLower 则截断（遮蔽低优先级障碍）
    /// </summary>
    public IReadOnlyList<ObstacleEntry> GetEffectiveChain(int tubeIndex, ObstacleKind? targetKind = null)
    {
        var all = GetByTube(tubeIndex);
        if (all.Count == 0) return Array.Empty<ObstacleEntry>();

        var chain = new List<ObstacleEntry>(capacity: all.Count);

        foreach (var e in all)
        {
            if (!e.Enabled) continue;
            if (!targetKind.HasValue || targetKind.Value == e.Kind)
                chain.Add(e);

            var desc = Describe(e.Kind);
            if (desc.Exclusivity == ObstacleExclusivity.MaskLower)
                break;
        }

        return chain;
    }
    
    public IReadOnlyList<ObstacleEntry> GetRequireAfterApplyChain(int tubeIndex, ObstacleKind? targetKind = null)
    {
        var all = GetByTube(tubeIndex);
        if (all.Count == 0) return Array.Empty<ObstacleEntry>();

        var chain = new List<ObstacleEntry>(capacity: all.Count);

        foreach (var e in all)
        {
            if (!e.Enabled) continue;

            var desc = Describe(e.Kind);
            // 1) 需要更新的才加入结果
            if (desc.RequireAfterApply && ((!targetKind.HasValue || targetKind.Value == e.Kind)))
                chain.Add(e);

            // 2) 但遮蔽截断必须无条件生效
            if (desc.Exclusivity == ObstacleExclusivity.MaskLower)
                break;
        }

        return chain;
    }

    /// <summary>
    /// 获取障碍物类型的策略描述（Descriptor）。
    /// 若缺失则抛异常，避免 silent bug。
    /// </summary>
    public ObstacleDescriptor Describe(ObstacleKind kind)
    {
        if (!ObstacleDescriptors.Map.TryGetValue(kind, out var desc))
            throw new KeyNotFoundException($"ObstacleDescriptor not found for kind: {kind}");
        return desc;
    }

    // ─────────────────────────────
    // Build Index
    // ─────────────────────────────

    private static IReadOnlyDictionary<int, IReadOnlyList<ObstacleEntry>> BuildByTube(IReadOnlyList<ObstacleEntry> all)
    {
        var dict = new Dictionary<int, List<ObstacleEntry>>();

        foreach (var e in all)
        {
            // TubeTargets 允许是组机制：[a,b,c]
            foreach (var tube in e.TubeTargets)
            {
                if (!dict.TryGetValue(tube, out var list))
                    dict[tube] = list = new List<ObstacleEntry>();

                list.Add(e);
            }
        }

        // 排序：Priority 降序（同 Priority 保持稳定顺序即可）
        var sorted = new Dictionary<int, IReadOnlyList<ObstacleEntry>>(dict.Count);
        foreach (var (tube, list) in dict)
        {
            var ordered = list
                .OrderByDescending(x => ObstacleDescriptors.Map[x.Kind].Priority)
                .ToList();

            sorted[tube] = new ReadOnlyCollection<ObstacleEntry>(ordered);
        }

        return new ReadOnlyDictionary<int, IReadOnlyList<ObstacleEntry>>(sorted);
    }

    private static IReadOnlyDictionary<(int tube, int cell), IReadOnlyList<ObstacleEntry>> BuildByCell(IReadOnlyList<ObstacleEntry> all)
    {
        var dict = new Dictionary<(int tube, int cell), List<ObstacleEntry>>();

        foreach (var e in all)
        {
            if (e.CellTargets is null || e.CellTargets.Count == 0)
                continue;

            foreach (var tube in e.TubeTargets)
            foreach (var cell in e.CellTargets)
            {
                var key = (tube, cell);
                if (!dict.TryGetValue(key, out var list))
                    dict[key] = list = new List<ObstacleEntry>();

                list.Add(e);
            }
        }

        // cell 上通常数量很少，这里也按 Priority 降序排一下
        var sorted = new Dictionary<(int tube, int cell), IReadOnlyList<ObstacleEntry>>(dict.Count);
        foreach (var (key, list) in dict)
        {
            var ordered = list
                .OrderByDescending(x => ObstacleDescriptors.Map[x.Kind].Priority)
                .ToList();

            sorted[key] = new ReadOnlyCollection<ObstacleEntry>(ordered);
        }

        return new ReadOnlyDictionary<(int tube, int cell), IReadOnlyList<ObstacleEntry>>(sorted);
    }
}
