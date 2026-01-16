using System.IO;
using System.Runtime.InteropServices.JavaScript;

namespace WaterSort.Core.Solvers;

public record struct Move
{
    public int From { get; set; }
    public int To { get; set; }
    public int Color { get; set; }
    public int Count { get; set; }
}

public class MoveGroup
{
    public List<Move> Moves = new();

    public MoveGroup DeepCopy()
    {
        return new MoveGroup()
        {
            Moves = this.Moves.ToList()
        };
    }
}

public class TubeMoveAbility
{
    public int TubeIndex { get; set; }
    
    public int ExportColor { get; set; }
    public int ExportCount { get; set; }
    
    public int AcceptColor { get; set; }
    public int AcceptCount { get; set;}
}


public class Tube
{
    public static int COLOR_EMPTY = -1;
    public int Capacity { get; }

    // bottom -> top
    private readonly int[] _cells;
    private int _count;

    public Tube(IEnumerable<int> colors, int? capacity = null)
    {
        if (colors == null) throw new ArgumentNullException(nameof(colors));

        _cells = colors.ToArray();

        if (capacity.HasValue && capacity.Value < _cells.Length)
        {
            throw new ArgumentException(
                $"capacity({capacity.Value}) must be >= colors({_cells.Length})",
                nameof(capacity)
            );
        }
        
        Capacity = capacity ?? _cells.Length;
    }



    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public bool IsFull => _count == Capacity;
    
    
    /// <summary>
    /// 顶部颜色。Tube.COLOR_EMPTY 表示空颜色，可以接受任意的颜色
    /// </summary>
    public int TopColor => _count == 0 ? Tube.COLOR_EMPTY : _cells[_count - 1];
    
    /// <summary>
    /// 是否为单色瓶，空瓶是单色瓶
    /// </summary>
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


    /// <summary>
    /// 顶部连续同色层数
    /// </summary>
    public int TopRunLength
    {
        get
        {
            if (_count == 0) return 0;
            
            var c = _cells[_count - 1];
            int i = _count - 1;
            while (i >= 0 && _cells[i] == c)
            {
                i--;
            }

            return _count - 1 - i;
        }
    }

    /// <summary>
    /// 顶部可用空间（不是TBS的顶部空间概念）
    /// </summary>
    public int FreeSpace => Capacity - _count;
    
    
    
    public void Pop(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count must be > 0");

        if (count > _count)
            throw new InvalidOperationException($"Pop({count}) exceeds current count({_count}).");

        // 可选：清空弹出的格子（便于 debug / 保持干净）
        for (int i = 0; i < count; i++)
            _cells[--_count] = 0; // 0 代表 Empty（你也可以用 -1）
    }
    
    
    public void Push(int color, int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count must be > 0");

        if (color <= 0)
            throw new ArgumentOutOfRangeException(nameof(color), "color must be > 0");

        if (_count + count > Capacity)
            throw new InvalidOperationException(
                $"Push({count}) exceeds capacity. count({_count}) + push({count}) > cap({Capacity})."
            );

        for (int i = 0; i < count; i++)
            _cells[_count++] = color;
    }
    
    public static Tube DeepCopy(Tube source)
    {
        return new Tube(source._cells[0..source.Count], source.Capacity);
    }
}

public class State
{
    public IReadOnlyList<Tube> Tubes { get; }

    public State(IReadOnlyList<Tube> tubes)
    {
        Tubes = tubes;
    }

    public State DeepClone()
    {
        return new State(Tubes.Select(Tube.DeepCopy).ToList());
    }


    public IEnumerable<TubeMoveAbility> BuildTubeAbilities()
    {
        for (int i = 0; i < Tubes.Count; i++)
        {
            yield return new TubeMoveAbility()
            {
                TubeIndex = i,
                ExportColor = Tubes[i].TopColor,
                ExportCount = Tubes[i].TopRunLength,
                AcceptColor = Tubes[i].TopColor,
                AcceptCount = Tubes[i].FreeSpace
            };
        }
    }
}