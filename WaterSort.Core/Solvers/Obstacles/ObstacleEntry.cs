namespace WaterSort.Core.Solvers.Obstacles;

public interface IObstacleExtra {}


/// <summary>
/// 障碍物实例（Entry）。
/// 仅描述：类型 + 挂载目标 + 参数 + 是否仍生效（Enabled）。
/// “tube 当前生效的障碍物是谁”由优先级/排外策略在 View 中计算。
/// </summary>
public sealed class ObstacleEntry
{
    /// <summary>实例唯一 Id，用于更新/移除定位。</summary>
    public int Id { get; init; }

    /// <summary>障碍物类型。</summary>
    public ObstacleKind Kind { get; init; }

    /// <summary>作用的 tube 列表：单 tube 用 [i]，组机制用 [a,b,c]。</summary>
    public IReadOnlyList<int> TubeTargets { get; set; } = Array.Empty<int>();

    /// <summary>Cell 级挂载（可空）。null 表示 Tube 级。</summary>
    public IReadOnlyList<int>? CellTargets { get; set; }

    /// <summary>通用颜色参数（可空）。含义由 Kind 决定（如窗帘/柜子/钥匙/专属瓶）。</summary>
    public int? Color { get; init; }

    /// <summary>是否仍生效（true=未解除；false=已解除）。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>障碍物额外参数（类型由 Kind 决定）。</summary>
    public IObstacleExtra? Extra { get; init; }

    public static ObstacleEntry DeepCopy(ObstacleEntry source)
    {
        return new ObstacleEntry()
        {
            Id = source.Id,
            TubeTargets = source.TubeTargets.ToList(),
            CellTargets = source.CellTargets?.ToList(),
            Color = source.Color,
            Enabled = source.Enabled,
            Extra = source.Extra
        };
    }
}

// ⚠️ 注意：Curtain（同色多 tube）不建议用组 entry
// 因为它可能“部分揭开”（受柜子门控），更适合展开成多个 entry（每个 tube 一个）。


// tube -> ObstacleList  [Priority], 顶层以  makelower 截止  跳过IsActive = false，的，收集截止到 makeLower位置，
// 要求， makelower 的优先级别要比较高
// tube-> List[ObstacleEntry] 关系图表，只需构建一次
//