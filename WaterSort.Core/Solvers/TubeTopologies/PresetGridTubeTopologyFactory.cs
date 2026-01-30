namespace WaterSort.Core.Solvers.TubeTopologies;

public sealed class PresetGridTubeTopologyFactory : ITubeTopologyFactory
{
    public ITubeTopology Create(int tubeCount)
    {
        var mask = BuildMask(tubeCount);
        return new GridTubeTopology(mask);
    }

    // ─────────────────────────────
    // Preset definitions
    // ─────────────────────────────

    private static readonly Dictionary<int, int[]> _presets = new()
    {
        { 5,  new[] { 3, 2 } },
        { 6,  new[] { 3, 3 } },
        { 7,  new[] { 4, 3 } },
        { 8,  new[] { 4, 4 } },
        { 9,  new[] { 5, 4 } },

        { 10, new[] { 3, 4, 3 } },
        { 11, new[] { 4, 4, 3 } },
        { 12, new[] { 4, 4, 4 } },
        { 13, new[] { 4, 5, 4 } },
        { 14, new[] { 5, 5, 4 } },
        { 15, new[] { 5, 5, 5 } },
    };

    private static List<List<int>> BuildMask(int tubeCount)
    {
        if (!_presets.TryGetValue(tubeCount, out var rows))
            throw new ArgumentException(
                $"No preset tube layout for tubeCount={tubeCount}");

        return BuildMaskFromRowCounts(rows);
    }

    private static List<List<int>> BuildMaskFromRowCounts(int[] rows)
    {
        var mask = new List<List<int>>(rows.Length);

        foreach (var count in rows)
        {
            var row = new List<int>(count);
            for (int i = 0; i < count; i++)
                row.Add(1);
            mask.Add(row);
        }

        return mask;
    }
}