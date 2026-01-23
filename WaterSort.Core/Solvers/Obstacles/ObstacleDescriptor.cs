namespace WaterSort.Core.Solvers.Obstacles;

public enum ObstacleExclusivity
{
    /// <summary>可叠加（不遮蔽）。例如 Question + FixColor。</summary>
    Stack,

    /// <summary>遮蔽低优先级障碍（通常是“完全禁用”类）。</summary>
    MaskLower
}

/// <summary>
/// 障碍物“类型级策略描述”（Descriptor）。
/// 用于统一定义：优先级、遮蔽规则、以及是否影响 Hash/Normalize。
/// </summary>
public sealed record ObstacleDescriptor(
    /// <summary> 障碍物类型。</summary>
    ObstacleKind Kind,

    /// <summary>
    /// 优先级（越大越先处理）。
    /// 同一 tube 上多个障碍物按此排序，用于计算当前生效链（ActiveChain）。
    /// </summary>
    int Priority,

    /// <summary>
    /// 排外/遮蔽策略：
    /// - Stack：可叠加，不截断低优先级障碍
    /// - MaskLower：生效时遮蔽低优先级障碍（用于“硬封禁”类）
    /// </summary>
    ObstacleExclusivity Exclusivity,

    /// <summary>
    /// 是否影响状态 Hash（visited 去重）。
    /// true 表示该障碍物会改变可达状态/规则，必须纳入 StateKey。
    /// </summary>
    bool AffectHash,

    /// <summary>
    /// 是否影响 Normalize（等价类/单色瓶聚合）。
    /// true 表示该障碍物会破坏 tube 交换等价性，不能被错误归并。
    /// </summary>
    bool AffectNormalize,
    
    
    /// <summary>
    /// 是否影响 Normalize（等价类/单色瓶聚合）。
    /// true 表示该障碍物会破坏 tube 交换等价性，不能被错误归并。
    /// </summary> 
    bool RequireAfterApply
);


public static class ObstacleDescriptors
{
    // Priority 建议分层，不要全 300，否则排序和遮蔽链会不稳定
    private const int P_BlockAll = 400;   // 不能倒也不能接（硬封禁）
    private const int P_Strong   = 300;   // 强约束但不遮蔽（固定瓶/专属瓶）
    private const int P_Mid      = 250;   // 中等约束（冰盒：通常只能倒入）
    private const int P_Low      = 200;   // 轻量修饰（问号）
    private const int P_Item     = 100;   // 收集物/不改规则（钥匙）

    public static readonly IReadOnlyDictionary<ObstacleKind, ObstacleDescriptor> Map =
        new Dictionary<ObstacleKind, ObstacleDescriptor>
        {
            // 问号：影响倒出量 / 顶部可见性 => 会影响搜索与等价类
            [ObstacleKind.Mystery] = new(
                ObstacleKind.Mystery,
                Priority: P_Low,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true
                ),

            // 固定瓶：不能倒出但能接 => 改规则，需参与 hash/normalize
            [ObstacleKind.Clamp] = new(
                ObstacleKind.Clamp,
                Priority: P_Strong,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: false
                ),

            // 窗帘：未揭开前不能倒也不能接 => 硬封禁，遮蔽下面所有
            [ObstacleKind.Curtain] = new(
                ObstacleKind.Curtain,
                Priority: P_BlockAll,
                Exclusivity: ObstacleExclusivity.MaskLower,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true),

            // 钥匙（杯柜钥匙）：本身不限制倒水，只是触发解锁 => 不必影响 hash/normalize
            [ObstacleKind.CupboardKey] = new(
                ObstacleKind.CupboardKey,
                Priority: P_Item,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: false,
                AffectNormalize: false,
                RequireAfterApply: true),

            // 柜子：未打开前不能倒也不能接 => 硬封禁，遮蔽下面所有
            [ObstacleKind.Cupboard] = new(
                ObstacleKind.Cupboard,
                Priority: P_BlockAll,
                Exclusivity: ObstacleExclusivity.MaskLower,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true),

            // 专属瓶：只能接某颜色 => 改规则，需参与 hash/normalize
            [ObstacleKind.ColorLock] = new(
                ObstacleKind.ColorLock,
                Priority: P_Strong,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: false),
            
            [ObstacleKind.Plaster] = new(
                ObstacleKind.Plaster,
                Priority: P_Strong,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true),

            // 冰盒：通常只能倒入不能倒出（组机制）=> 改规则，需参与 hash/normalize
            [ObstacleKind.IceBox] = new(
                ObstacleKind.IceBox,
                Priority: P_Mid,
                Exclusivity: ObstacleExclusivity.Stack,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true),

            // 卷轴（你这里叫 Scroll）：会挡住瓶子，每次完成一种颜色，收起一部分 => 硬封禁，遮蔽下面所有
            [ObstacleKind.Scroll] = new(
                ObstacleKind.Scroll,
                Priority: P_BlockAll,
                Exclusivity: ObstacleExclusivity.MaskLower,
                AffectHash: true,
                AffectNormalize: true,
                RequireAfterApply: true),
        };
}
