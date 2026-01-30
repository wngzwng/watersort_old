namespace WaterSort.Core.Solvers.TubeTopologies;

public sealed class GridTubeTopology : ITubeTopology
{
    private readonly int[,] _grid;
    private readonly Dictionary<int, (int r, int c)> _posMap;

    public int Rows { get; }
    public int Cols { get; }
    public int TubeCount => _posMap.Count;

    /// <summary>
    /// 01 布局构造：
    /// 1 = 有 tube（自动分配 tubeIndex，base0）
    /// 0 = 空位
    /// </summary>
    public GridTubeTopology(List<List<int>> mask)
    {
        Rows = mask.Count;
        Cols = mask.Max(r => r.Count);

        _grid = new int[Rows, Cols];
        _posMap = new Dictionary<int, (int, int)>();

        // 初始化为空位
        for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols; c++)
            _grid[r, c] = -1;

        var tubeIndex = 0;

        for (int r = 0; r < Rows; r++)
        {
            var row = mask[r];
            for (int c = 0; c < row.Count; c++)
            {
                if (row[c] == 0)
                    continue;

                // 分配 tubeIndex
                _grid[r, c] = tubeIndex;
                _posMap[tubeIndex] = (r, c);
                tubeIndex++;
            }
        }
    }

    /// <summary>
    /// 获取邻居，这里只有左右邻居
    /// </summary>
    /// <param name="tubeIndex"></param>
    /// <returns></returns>
    public IReadOnlyList<int> GetNeighbors(int tubeIndex)
    {
        if (!_posMap.TryGetValue(tubeIndex, out var pos))
            return Array.Empty<int>();

        var (r, c) = pos;
        var list = new List<int>(2);

        // TryAdd(r - 1, c, list);
        // TryAdd(r + 1, c, list);
        TryAdd(r, c - 1, list);
        TryAdd(r, c + 1, list);

        return list;
    }

    private void TryAdd(int r, int c, List<int> list)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Cols)
            return;

        var idx = _grid[r, c];
        if (idx >= 0)
            list.Add(idx);
    }

    public bool AreAdjacent(int a, int b)
        => GetNeighbors(a).Contains(b);
    
}

