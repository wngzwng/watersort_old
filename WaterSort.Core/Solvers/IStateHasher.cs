namespace WaterSort.Core.Solvers;

public interface IStateHasher
{
    StateKey BuildKey(State state);
}