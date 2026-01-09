namespace WaterSort.Core.CommonSolver;

public static class AvailabilityScanner
{
    public static IReadOnlyList<TubeColorAvailability> Scan(State state)
    {
        var result = new List<TubeColorAvailability>();

        for (int i = 0; i < state.Tubes.Count; i++)
        {
            var tube = state.Tubes[i];
            int color = tube.TopColor;
            int export = tube.TopRunLength();
            int accept = tube.FreeSpace;
            result.Add(new TubeColorAvailability(
                TubeIndex: i,
                Color: color,
                ExportCount: export,
                AcceptCount: accept
            ));
        }
        return result;
    }
}
