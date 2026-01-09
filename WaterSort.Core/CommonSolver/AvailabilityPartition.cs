namespace WaterSort.Core.CommonSolver;

public static class AvailabilityPartition
{
    public static (
        IReadOnlyList<FromTube> Froms,
        IReadOnlyList<ToTube> Tos
        ) Partition(IEnumerable<TubeColorAvailability> availabilities)
    {
        var froms = new List<FromTube>();
        var tos = new List<ToTube>();

        foreach (var a in availabilities)
        {
            if (a.ExportCount > 0)
            {
                froms.Add(new FromTube(
                    a.TubeIndex,
                    a.Color,
                    a.ExportCount
                ));
            }

            if (a.AcceptCount > 0)
            {
                tos.Add(new ToTube(
                    a.TubeIndex,
                    a.AcceptCount
                ));
            }
        }

        return (froms, tos);
    }
}
