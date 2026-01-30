using System.IO;
using System.Runtime.InteropServices.JavaScript;
using WaterSort.Core.Solvers.Obstacles;
using WaterSort.Core.Solvers.TubeTopologies;

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
    /// 顶部连续同色层数
    /// </summary>
    public int TopBoundary
    {
        get
        {
            if (_count == 0) return 0;
            
            var c = _cells[_count - 1];
            int i = _count - 1;
            for (; i > 0; i--)
            {
                if (c != _cells[i - 1])
                    return i;
            }

            return 0;
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

    /// <summary>
    /// 障碍物实例列表（Entry 数据层）。
    /// 注意：更新障碍物时会生成新的 Entries（不可变思路）。
    /// </summary>
    public IReadOnlyList<ObstacleEntry> ObstacleEntries { get; }

    /// <summary>
    /// 障碍物查询视图（tube/cell -> obstacles，按优先级排序/有效链截断）。
    /// 这是由 ObstacleEntries 构建出来的索引，属于“View 层”。
    /// </summary>
    public ObstacleCatalog Obstacles { get; private set; }

    public ITubeTopology TubeTopology { get; private set; }

    public State(IReadOnlyList<Tube> tubes,  IEnumerable<ObstacleEntry>? obstacleEntries = null, ITubeTopology? tubeTopology = null)
    {
        Tubes = tubes ?? throw new ArgumentNullException(nameof(tubes));
        ObstacleEntries = obstacleEntries != null ? obstacleEntries.ToList() :  Array.Empty<ObstacleEntry>();

        // View 只构建一次（State 不变则 View 不变）
        Obstacles = new ObstacleCatalog(ObstacleEntries);

        TubeTopology = tubeTopology ?? new LinearTubeTopology(tubes.Count);
    }
    
    /// <summary>
    /// 当 ObstacleEntries 内部状态发生变化（Enabled/CellTargets/Extra 等）后，
    /// 重建障碍物索引视图（tube/cell -> entries）。
    /// </summary>
    public void RebuildObstacleCatalog()
    {
        Obstacles = new ObstacleCatalog(ObstacleEntries);
    }

    public State DeepClone()
    {
        // Tube 深拷贝 + 障碍物 entries 可共享（如果 entry 也不可变）
        // 如果你后续要改 Enabled，就用“生成新 State”的方式更新 entries
        return new State(
            Tubes.Select(Tube.DeepCopy).ToList(), 
            ObstacleEntries.Select(ObstacleEntry.DeepCopy).ToList());
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
    
    public override string ToString()
    {
        return Render(true);
    }
    
    // public string Render(bool hexMode = false, bool showIndex = true)
    // {
    //     if (Tubes.Count == 0)
    //         return "[空盘面]";
    //
    //     int maxCapacity = Tubes.Max(t => t.Capacity);
    //     var lines = new List<string>();
    //
    //     // 从顶部到底部渲染
    //     for (int layer = maxCapacity - 1; layer >= 0; layer--)
    //     {
    //         var cells = new List<string>();
    //
    //         for (int i = 0; i < Tubes.Count; i++)
    //         {
    //             var tube = Tubes[i];
    //             bool isCapacityLine = layer == tube.Capacity - 1;
    //             bool hasColor = layer < tube.Cells.Length;
    //
    //             if (hasColor)
    //             {
    //                 int color = tube.Cells[layer];
    //                 string colorStr = hexMode
    //                     ? color.ToString("X2")
    //                     : color.ToString().PadLeft(2);
    //
    //                 var hasSpecial = false;
    //                 var chain = Obstacles.GetByCell(i, layer);
    //                 if (chain.Any(obs => obs.Enabled))
    //                 {
    //                     string flag = "?";
    //                     cells.Add(flag.PadLeft(2));
    //                     hasSpecial = true;
    //                 }
    //                 else
    //                 {
    //                     var curtain = Obstacles.GetByTube(i, ObstacleKind.Curtain);
    //                     if (curtain.Count > 0 && curtain[0].Enabled)
    //                     {
    //                         cells.Add(AnsiColor.Colorize(curtain[0].Color.Value,"#".PadLeft(2)));
    //                         hasSpecial = true;
    //                     }
    //                 }
    //                 
    //                 if (!hasSpecial)
    //                 {
    //                     cells.Add(AnsiColor.Colorize(color, colorStr));
    //                 }
    //                 // cells.Add(colorStr);
    //             }
    //             else
    //             {
    //                 if (isCapacityLine)
    //                     cells.Add(" -");
    //                 else
    //                     cells.Add("  ");
    //             }
    //         }
    //
    //         lines.Add(string.Join(" ", cells));
    //     }
    //
    //     
    //     if (showIndex)
    //     {
    //         int width = 3 * Tubes.Count - 1;
    //         lines.Add(new string('-', width));
    //         lines.Add(string.Join(" ",
    //             Enumerable.Range(0, Tubes.Count)
    //                 .Select(i =>
    //                 {
    //                     if (HasClamp(this, i))
    //                     {
    //                         return AnsiColor.Colorize(10, i.ToString()).PadLeft(2);
    //                     }
    //                     return i.ToString().PadLeft(2);
    //                 })));
    //     }
    //
    //     return string.Join(Environment.NewLine, lines);
    //
    //     static bool HasClamp(State state, int tubeIndex)
    //     {
    //         var clamps = state.Obstacles.GetByTube(tubeIndex, ObstacleKind.Clamp);
    //         return clamps.Count > 0 && clamps[0].Enabled;
    //     }
    // }

    public string Render(bool hexMode = false, bool showIndex = true)
    {
        if (Tubes.Count == 0)
            return "[空盘面]";

        int maxCapacity = Tubes.Max(t => t.Capacity);
        var lines = new List<string>();

        // ─────────────────────────────
        // Body
        // ─────────────────────────────
        for (int layer = maxCapacity - 1; layer >= 0; layer--)
        {
            var cells = new List<string>();

            for (int i = 0; i < Tubes.Count; i++)
            {
                cells.Add(RenderCell(i, layer, hexMode));
            }

            lines.Add(string.Join(" ", cells));
        }

        // ─────────────────────────────
        // Footer
        // ─────────────────────────────
        if (showIndex)
            RenderIndex(lines);

        return string.Join(Environment.NewLine, lines);
        
        string RenderCell(int tubeIndex, int layer, bool hexMode)
        {
            var tube = Tubes[tubeIndex];

            bool isCapacityLine = layer == tube.Capacity - 1;
            bool hasColor = layer < tube.Cells.Length;

            if (hasColor)
            {
                int color = tube.Cells[layer];
                string colorStr = hexMode
                    ? color.ToString("X2")
                    : color.ToString().PadLeft(2);

                var chain = Obstacles.GetByCell(tubeIndex, layer);
                if (chain.Any(obs => obs.Enabled))
                {
                    return "?".PadLeft(2);
                }

                var curtain = Obstacles.GetByTube(tubeIndex, ObstacleKind.Curtain);
                if (curtain.Count > 0 && curtain[0].Enabled)
                {
                    return AnsiColor.Colorize(
                        curtain[0].Color.Value,
                        "#".PadLeft(2)
                    );
                }

                return AnsiColor.Colorize(color, colorStr);
            }
            else
            {
                return isCapacityLine ? " -" : "  ";
            }
        }
        
        void RenderIndex(List<string> lines)
        {
            int width = 3 * Tubes.Count - 1;
            lines.Add(new string('-', width));

            lines.Add(string.Join(" ",
                Enumerable.Range(0, Tubes.Count)
                    .Select(i =>
                    {
                        if (HasClamp(i))
                            return AnsiColor.Colorize(10, i.ToString()).PadLeft(2);

                        return i.ToString().PadLeft(2);
                    })));
        }

        bool HasClamp(int tubeIndex)
        {
            var clamps = Obstacles.GetByTube(tubeIndex, ObstacleKind.Clamp);
            return clamps.Count > 0 && clamps[0].Enabled;
        }

    }

}