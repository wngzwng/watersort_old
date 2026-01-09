namespace WaterSort.Core.CommonSolver;

public readonly struct StateKey : IEquatable<StateKey>
{
    private readonly int _hash;

    public StateKey(int hash)
    {
        _hash = hash;
    }

    public bool Equals(StateKey other) => _hash == other._hash;
    public override int GetHashCode() => _hash;
}
