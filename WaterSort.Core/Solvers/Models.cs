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
    public string Description = string.Empty;
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
        
        var list = colors.ToList();

        if (capacity.HasValue && capacity.Value < list.Count)
        {
            throw new ArgumentException(
                $"capacity({capacity.Value}) must be >= colors({list.Count})",
                nameof(capacity)
            );
        }

        Capacity = capacity ?? list.Count;

        _cells = new int[Capacity];
        _count = list.Count;

        // 填充已有颜色（底->顶）
        for (int i = 0; i < list.Count; i++)
        {
            _cells[i] = list[i];
        }
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

    public static Tube CreateTube(IEnumerable<int> colors, int? capacity = null)
    {
        return new Tube(colors, capacity);
    }

    public static Tube CreateEmptyTube(int capacity)
    {
        return new Tube(Enumerable.Empty<int>(), capacity);
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
    
    public string Render(bool hexMode = false, bool showIndex = true)
    {
        if (Tubes.Count == 0)
            return "[空盘面]";

        int maxCapacity = Tubes.Max(t => t.Capacity);
        var lines = new List<string>();

        // 从顶部到底部渲染
        for (int layer = maxCapacity - 1; layer >= 0; layer--)
        {
            var cells = new List<string>();

            foreach (var tube in Tubes)
            {
                bool isCapacityLine = layer == tube.Capacity - 1;
                bool hasColor = layer < tube.Cells.Length;

                if (hasColor)
                {
                    int color = tube.Cells[layer];
                    string colorStr = hexMode
                        ? color.ToString("X2")
                        : color.ToString().PadLeft(2);

                    // cells.Add(colorStr);
                    cells.Add(AnsiColor.Colorize(color, colorStr))
                        
                        ;
                }
                else
                {
                    if (isCapacityLine)
                        cells.Add(" -");
                    else
                        cells.Add("  ");
                }
            }

            lines.Add(string.Join(" ", cells));
        }

        if (showIndex)
        {
            int width = 3 * Tubes.Count - 1;
            lines.Add(new string('-', width));
            lines.Add(string.Join(" ",
                Enumerable.Range(0, Tubes.Count)
                    .Select(i => i.ToString().PadLeft(2))));
        }

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString()
    {
        return Render(true);
    }
}