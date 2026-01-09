namespace WaterSort.Core.WaterSortSolver.Domain;

public class WaterSortState
{
    public List<int> TopBoundaryIndex { get; }
    public Dictionary<int, int> ReleaseUnits { get; }
    public Dictionary<int, int> TopSpaces { get; }

    public WaterSortState(
        List<int> topBoundaryIndex, 
        Dictionary<int, int> releaseUnits, 
        Dictionary<int, int> topSpaces
        )
    {
        TopBoundaryIndex = topBoundaryIndex;
        ReleaseUnits = releaseUnits;
        TopSpaces = topSpaces;
    }

    public bool IsSolved() => TopBoundaryIndex.All(i => i == 0);

    public string Hash(WaterSortContext ctx)
    {
        return string.Join(",",
            TopBoundaryIndex.Select((idx, i) => ctx.Boundaries[i][idx])
            );
    }

    public WaterSortState Apply(Move move, WaterSortContext ctx)
    {
        var next = Copy();
        ApplyInternal(next, ctx, move.FromBottle);
        return next;
    }

    private WaterSortState Copy()
    {
        return new WaterSortState(
            new List<int>(TopBoundaryIndex),
            new Dictionary<int, int>(ReleaseUnits),
            new Dictionary<int, int>(TopSpaces)
            );
    }

    private static void ApplyInternal(
        WaterSortState state,
        WaterSortContext ctx, 
        int bottleIdx
    )
    {
        int tbIdx = state.TopBoundaryIndex[bottleIdx];
        int oldBoundary = ctx.Boundaries[bottleIdx][tbIdx];
        int newTbIdx = tbIdx - 1;
        int newBoundary = ctx.Boundaries[bottleIdx][newTbIdx];

        state.TopBoundaryIndex[bottleIdx] = newTbIdx;

        int oldColor = ctx.WaterData[bottleIdx][oldBoundary];
        int newColor = ctx.WaterData[bottleIdx][newBoundary];

        // 更新释放的新颜色的数据统计
        int released = oldBoundary - newBoundary;
        state.ReleaseUnits[newColor] =
            state.ReleaseUnits.GetValueOrDefault(newColor)+ released;

        int newTopSpace = ctx.BottleCapacity - newBoundary;
        if (newTbIdx > 0) // 新颜色的顶部空间更新尽限 非单色瓶
            state.TopSpaces[newColor] =
                state.TopSpaces.GetValueOrDefault(newColor) + newTopSpace;

        // 旧颜色的此处的顶部空间减少
        int oldSpace = ctx.BottleCapacity - oldBoundary;
        state.TopSpaces[oldColor] -= oldSpace;
    }
}