namespace WaterSort.Core.CommonSolver;

public sealed class Tube
{
    // ─────────────
    // 基本属性（稳定）
    // ─────────────

    public int Capacity { get; }
    public TubeType Type { get; }

    // bottom -> top
    private readonly int[] _cells;
    private int _count;

    // ─────────────
    // 构造
    // ─────────────

    public Tube(int capacity, TubeType type, IEnumerable<int> colors)
    {
        Capacity = capacity;
        Type = type;

        _cells = new int[capacity];
        _count = 0;

        foreach (var c in colors)
        {
            _cells[_count++] = c;
        }
    }

    public static Tube CreateNormalTube(int capacity, IEnumerable<int> colors)
    {
        return new Tube(capacity, TubeType.Normal, colors);
    }

    public static Tube CreateEmptyTube(int capacity)
    {
        return new Tube(capacity, TubeType.Normal, []);
    }

    // ─────────────
    // 只读视图（给 Explore / Hash / Normalize）
    // ─────────────

    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public bool IsFull => _count == Capacity;

    public int TopColor => _count == 0 ? -1 : _cells[_count - 1];

    public bool IsMonochrome
    {
        get
        {
            if (_count <= 1) return true;
            var c = _cells[0];
            for (int i = 1; i < _count; i++)
                if (_cells[i] != c)
                    return false;
            return true;
        }
    }

    public ReadOnlySpan<int> Cells
        => new ReadOnlySpan<int>(_cells, 0, _count);

    // 顶部连续同色层数（Explore 必备）
    public int TopRunLength()
    {
        if (_count == 0) return 0;

        var c = _cells[_count - 1];
        int i = _count - 1;
        while (i >= 0 && _cells[i] == c)
            i--;

        return _count - 1 - i;
    }

    public int FreeSpace => Capacity - _count;

    // ─────────────
    // 原子修改（仅供 Move.Apply）
    // ─────────────

    public void Pop(int count)
    {
        _count -= count;
    }

    public void Push(int color, int count)
    {
        for (int i = 0; i < count; i++)
            _cells[_count++] = color;
    }


    public static Tube DeepCopy(Tube source)
    {
        return new Tube(source.Capacity, source.Type, source._cells[0..source.Count]);
    }
}
