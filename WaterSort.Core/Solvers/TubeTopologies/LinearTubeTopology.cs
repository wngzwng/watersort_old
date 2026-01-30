namespace WaterSort.Core.Solvers.TubeTopologies;

/// <summary>
/// 默认 tube topology：线性排列
/// 0 - 1 - 2 - ... - (N-1)
/// </summary>
public sealed class LinearTubeTopology : ITubeTopology
{
    public int TubeCount { get; }

    public LinearTubeTopology(int tubeCount)
    {
        if (tubeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(tubeCount));

        TubeCount = tubeCount;
    }

    public IReadOnlyList<int> GetNeighbors(int tubeIndex)
    {
        if (tubeIndex < 0 || tubeIndex >= TubeCount)
            return Array.Empty<int>();

        if (TubeCount == 1)
            return Array.Empty<int>();

        if (tubeIndex == 0)
            return new[] { 1 };

        if (tubeIndex == TubeCount - 1)
            return new[] { TubeCount - 2 };

        return new[] { tubeIndex - 1, tubeIndex + 1 };
    }

    public bool AreAdjacent(int a, int b)
    {
        return Math.Abs(a - b) == 1;
    }
}