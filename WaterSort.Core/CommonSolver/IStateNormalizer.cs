namespace WaterSort.Core.CommonSolver;

public interface IStateNormalizer
{
    IEnumerable<MoveGroup> Normalize(State state);
}
