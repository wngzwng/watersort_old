namespace WaterSort.Core.CommonSolver;

public interface IStateHasher
{
    StateKey BuildKey(State state);
}