namespace WaterSort.Core.Solvers.Obstacles;

public enum ObstacleKind
{
    None,
    /// <summary>
    /// 问号
    /// </summary>
    Mystery,

    /// <summary>
    /// 机械臂 / 固定瓶
    /// </summary>
    Clamp,

    /// <summary>
    /// 窗帘 / 幕布
    /// </summary>
    Curtain,

    /// <summary>
    /// 钥匙（柜子钥匙）
    /// </summary>
    CupboardKey,

    /// <summary>
    /// 柜子
    /// </summary>
    Cupboard,

    /// <summary>
    /// 石膏（旁消）
    /// </summary>
    Plaster,

    /// <summary>
    /// 固定颜色瓶 / 专属颜色瓶
    /// </summary>
    ColorLock,

    /// <summary>
    /// 冰盒
    /// </summary>
    IceBox,

    /// <summary>
    /// 卷轴
    /// </summary>
    Scroll
}


