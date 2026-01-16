namespace WaterSort.Core.Solvers;

public class MoveActuator
{
    private event Action<State, Move>? _onApplyBefore;
    private event Action<State, Move>? _onApplyAfter;

    public event Action<State, Move> OnApplyBefore
    {
        add => _onApplyBefore += value;
        remove => _onApplyBefore -= value;
    }

    public event Action<State, Move> OnApplyAfter
    {
        add => _onApplyAfter += value;
        remove => _onApplyAfter -= value;
    }

    private void ApplyCore(State state, Move move)
    {
        var fromTube = state.Tubes[move.From];
        var toTube = state.Tubes[move.To];

        fromTube.Pop(move.Count);
        toTube.Push(move.Color, move.Count);
    }

    public void Apply(State state, Move move)
    {
        _onApplyBefore?.Invoke(state, move);
        ApplyCore(state, move);
        _onApplyAfter?.Invoke(state, move);
    }

    public void Apply(State state, MoveGroup moveGroup)
    {
        foreach (var move in moveGroup.Moves)
        {
            Apply(state, move);
        }
    }
}
