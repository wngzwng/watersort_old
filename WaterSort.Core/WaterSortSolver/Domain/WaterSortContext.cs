namespace WaterSort.Core.WaterSortSolver.Domain;

public class WaterSortContext
{
    public IReadOnlyList<IReadOnlyList<int>> WaterData { get; }
    public IReadOnlyList<IReadOnlyList<int>> Boundaries { get; }
    public int BottleCapacity { get; }
    public int EmptyBottleCapacity { get; }

    public WaterSortContext(
        IReadOnlyList<IReadOnlyList<int>> waterData,
        IReadOnlyList<IReadOnlyList<int>> boundaries,
        int bottleCapacity,
        int emptyBottleCapacity
    )
    {
        WaterData = waterData;
        Boundaries = boundaries;
        BottleCapacity = bottleCapacity;
        EmptyBottleCapacity = emptyBottleCapacity;
    }
}