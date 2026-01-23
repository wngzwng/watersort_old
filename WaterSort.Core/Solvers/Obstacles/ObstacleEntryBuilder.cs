namespace WaterSort.Core.Solvers.Obstacles;

public sealed class ObstacleEntryBuilder
{
    private int _nextId;

    // 可选：builder 自带收集功能
    private readonly List<ObstacleEntry> _built = new();

    public ObstacleEntryBuilder(int idStart = 1)
    {
        _nextId = idStart;
    }

    // ─────────────────────────
    // Output
    // ─────────────────────────

    public IReadOnlyList<ObstacleEntry> BuildList() => _built.ToList();

    public ObstacleEntryBuilder Clear()
    {
        _built.Clear();
        return this;
    }

    // ─────────────────────────
    // Id policy
    // ─────────────────────────

    public int PeekNextId() => _nextId;

    public int TakeNextId() => _nextId++;

    // ─────────────────────────
    // Core
    // ─────────────────────────

    private static int[] Norm(params int[] xs)
        => xs?.Distinct().ToArray() ?? Array.Empty<int>();

    private static int[] Norm(IEnumerable<int> xs)
        => xs?.Distinct().ToArray() ?? Array.Empty<int>();

    private static IReadOnlyList<int>? NormNullable(IEnumerable<int>? xs)
        => xs == null ? null : xs.Distinct().ToArray();

    private ObstacleEntry Make(
        ObstacleKind kind,
        IReadOnlyList<int> tubes,
        IReadOnlyList<int>? cells = null,
        int? color = null,
        bool enabled = true,
        IObstacleExtra? extra = null,
        int? id = null)
    {
        if (tubes == null || tubes.Count <= 0)
            throw new ArgumentException($"{kind}: TubeTargets 不能为空");

        return new ObstacleEntry
        {
            Id = id ?? TakeNextId(),
            Kind = kind,
            TubeTargets = tubes,
            CellTargets = cells,
            Color = color,
            Enabled = enabled,
            Extra = extra,
        };
    }

    private ObstacleEntry AddBuilt(ObstacleEntry e)
    {
        _built.Add(e);
        return e;
    }

    // ─────────────────────────
    // 语义 API：Mystery（问号）
    // ─────────────────────────
  
    /// <summary>问号：Cell 级</summary>
    public ObstacleEntry MysteryCells(int tube, IEnumerable<int> cells, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Mystery, Norm(tube), cells: Norm(cells), enabled: enabled, extra: extra, id: id);

    // ─────────────────────────
    // 语义 API：Clamp（机械臂/固定瓶）
    // ─────────────────────────

    /// <summary>固定 tube（Clamp）：通常 Tube 级</summary>
    public ObstacleEntry Clamp(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Clamp, Norm(tube), enabled: enabled, extra: extra, id: id);

    public ObstacleEntry ClampGroup(IEnumerable<int> tubes, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Clamp, Norm(tubes), enabled: enabled, extra: extra, id: id);

    // ─────────────────────────
    // 语义 API：Curtain（窗帘）
    // ─────────────────────────

    /// <summary>窗帘：封死 tube 的进出</summary>
    public ObstacleEntry Curtain(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Curtain, Norm(tube), color:color, enabled: enabled, extra: extra, id: id);
    
    // ─────────────────────────
    // 语义 API：CupboardKey（钥匙）
    // ─────────────────────────
    // 你这里 Color 的语义很明确：钥匙颜色（对应柜子颜色）
    // 所以签名强制要求 color，避免配置层漏填。

    public ObstacleEntry CupboardKey(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.CupboardKey, Norm(tube), color: color, enabled: enabled, extra: extra, id: id);
    
    // ─────────────────────────
    // 语义 API：Cupboard（柜子）
    // ─────────────────────────
    // 柜子大概率是 group 机制（覆盖一组 tubes）
    // 同时必须带 color（锁的颜色）

    public ObstacleEntry Cupboard(int color, params int[] tubes)
        => Make(ObstacleKind.Cupboard, Norm(tubes), color: color);

    public ObstacleEntry Cupboard(int color, IEnumerable<int> tubes, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Cupboard, Norm(tubes), color: color, enabled: enabled, extra: extra, id: id);

    // ─────────────────────────
    // 语义 API：Plaster（石膏）
    // ─────────────────────────

    public ObstacleEntry Plaster(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Plaster, Norm(tube), enabled: enabled, extra: extra, id: id);
    
    // ─────────────────────────
    // 语义 API：ColorLock（固定颜色瓶/专属颜色瓶）
    // ─────────────────────────
    // 必须带 color，否则毫无意义，所以强制 color。

    public ObstacleEntry ColorLock(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.ColorLock, Norm(tube), color: color, enabled: enabled, extra: extra, id: id);
    // ─────────────────────────
    // 语义 API：IceBox（冰盒）
    // ─────────────────────────

    public ObstacleEntry IceBox(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.IceBox, Norm(tube), enabled: enabled, extra: extra, id: id);

    public ObstacleEntry IceBoxGroup(IEnumerable<int> tubes, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.IceBox, Norm(tubes) , enabled: enabled, extra: extra, id: id);

    // ─────────────────────────
    // 语义 API：Scroll（卷轴）
    // ─────────────────────────
    // 卷轴必然是 group 机制

    public ObstacleEntry Scroll(IEnumerable<int> tubes, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Make(ObstacleKind.Scroll, Norm(tubes), enabled: enabled, extra: extra, id: id);

    public ObstacleEntry Scroll(params int[] tubes)
        => Make(ObstacleKind.Scroll, Norm(tubes));

    // ─────────────────────────
    // Add 系列（链式批量）
    // ─────────────────────────

    public ObstacleEntryBuilder Add(ObstacleEntry e)
    {
        _built.Add(e);
        return this;
    }
    
    public ObstacleEntryBuilder AddMysteryCells(int tube, IEnumerable<int> cells, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(MysteryCells(tube, cells, enabled, extra, id));

    public ObstacleEntryBuilder PatchAddMysteryCells(int tubeCapacity, IEnumerable<int> cells, bool enabled = true)
    {
        Dictionary<int, HashSet<int>> cellGroup = new();
        foreach (var glocalCellIndex in Norm(cells))
        {
            int tubeIndex = glocalCellIndex / tubeCapacity;
            int cellIndex = glocalCellIndex % tubeCapacity;

            if (!cellGroup.TryGetValue(tubeIndex, out var list))
            {
                list = new HashSet<int>();
                cellGroup.Add(tubeIndex, list);
            }

            list.Add(cellIndex);
        }

        foreach (var kv in cellGroup)
        {
            _built.Add(MysteryCells(kv.Key, kv.Value, enabled));
        }

        return this;
    }

    public ObstacleEntryBuilder AddClamp(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(Clamp(tube, enabled, extra, id));

    public ObstacleEntryBuilder AddCurtain(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(Curtain(tube, color, enabled, extra, id));

    public ObstacleEntryBuilder AddCupboardKey(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(CupboardKey(tube, color, enabled, extra, id));

    public ObstacleEntryBuilder AddCupboard(int color, params int[] tubes)
        => Add(Cupboard(color, tubes));

    public ObstacleEntryBuilder AddPlaster(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(Plaster(tube, enabled, extra, id));

    public ObstacleEntryBuilder AddColorLock(int tube, int color, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(ColorLock(tube, color, enabled, extra, id));

    public ObstacleEntryBuilder AddIceBox(int tube, bool enabled = true, IObstacleExtra? extra = null, int? id = null)
        => Add(IceBox(tube, enabled, extra, id));

    public ObstacleEntryBuilder AddScroll(bool enabled = true, IObstacleExtra? extra = null, int? id = null, params int[] tubes)
        => Add(Scroll(tubes, enabled: enabled, extra: extra, id: id));
}
