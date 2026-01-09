namespace WaterSort.Core.CommonSolver;

public interface ITubeRule
{
    bool CanExport(Tube tube);
    bool CanAccept(Tube tube, int color);
}

// ITubeRule rule = TubeRuleRegistry.Get(tube.Type);