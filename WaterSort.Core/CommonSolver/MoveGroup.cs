using System.Text;

namespace WaterSort.Core.CommonSolver;

public sealed class MoveGroup
{
    public MoveGroupKind Kind { get; }
    public IReadOnlyList<Move> Moves { get; }

    public MoveGroup(MoveGroupKind kind, IReadOnlyList<Move> moves)
    {
        Kind = kind;
        Moves = moves;
    }

    public State Apply(State state)
    {
        var tubes = state.Tubes.ToArray();

        foreach (var move in Moves)
        {
            move.Apply(tubes);
        }

        return new State(tubes);
    }


    public string Render()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var move in Moves)
        {
            sb.Append($"(From: {move.From}, to: {move.To}): (color: {move.Color}, amount: {move.Count}) \n");
        }
        return sb.ToString();
    }
    public override string ToString()
    {
        return Render();
    }
}
