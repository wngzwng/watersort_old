namespace WaterSort.Core.Solvers;

public interface IMoveGroupExplorer
{
    IEnumerable<MoveGroup> Explore(State state);

    IEnumerable<MoveGroup> Normal(State state);
}
