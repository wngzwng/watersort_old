namespace WaterSort.Core.CommonSolver;

public sealed record TubeColorAvailability(
    int TubeIndex,
    int Color,
    int ExportCount,
    int AcceptCount
);
