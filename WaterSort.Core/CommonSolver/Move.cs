namespace WaterSort.Core.CommonSolver;

public sealed class Move
{
    public int From { get; }
    public int To { get; }
    public int Color { get; }
    public int Count { get; }

    public Move(int from, int to, int color, int count)
    {
        From = from;
        To = to;
        Color = color;
        Count = count;
    }

    public void Apply(Tube[] tubes)
    {
        // 真正倒水
        var from = tubes[From];
        var to = tubes[To];
        
        from.Pop(Count);
        to.Push(Color, Count);
    }
}
