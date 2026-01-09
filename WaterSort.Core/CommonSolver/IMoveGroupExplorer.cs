namespace WaterSort.Core.CommonSolver;

public interface IMoveGroupExplorer
{
    IEnumerable<MoveGroup> Explore(State state);
}
