namespace WaterSort.Core.CommonSolver;

public enum MoveGroupKind
{
    AdvanceBoundary,      // 推进可见边界（主推进）
    MergeMonochrome,      // 不同类型单色瓶的真实聚合
    TransformSemantic,    // TubeType / 规则语义改变
    NormalizeMonochrome   // （如果你保留它作为边，否则不用）
}
