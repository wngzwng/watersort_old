namespace WaterSort.Core.CommonSolver;

public static class SolverFactory
{
    public static Solver CreateDefault(bool openVisual = false)
    {
        var explorer = new LayeredMoveGroupExplorer();
        var normalizer = new MonochromeTypeNormalizer();
        var hasher = new CanonicalStateHasher();
        hasher.OpenVisual = openVisual;

        return new Solver(
            explorer,
            normalizer,
            hasher
        );
    }
}