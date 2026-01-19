namespace WaterSort.Core.Solvers;

public readonly struct TubeSignature : IComparable<TubeSignature>
{
    public readonly int Color;
    public readonly int Capacity;
    // public readonly int Height;

    // private TubeSignature(int type, int color, int height, int capacity)
    // {
    //     Type = type;
    //     Color = color;
    //     Height = height;
    //     Capacity = capacity;
    // }
    
    private TubeSignature(int color, int capacity)
    {
        Color = color;
        Capacity = capacity;
    }

    public static TubeSignature From(Tube tube)
        => new TubeSignature(tube.TopColor, tube.Capacity);

    public int CompareTo(TubeSignature other)
    {
        var c = Color.CompareTo(other.Color);
        if (c != 0) return c;
        return Capacity.CompareTo(other.Capacity);
    }
}
