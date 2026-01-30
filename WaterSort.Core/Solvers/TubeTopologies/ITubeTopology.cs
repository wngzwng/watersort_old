namespace WaterSort.Core.Solvers.TubeTopologies;

public interface ITubeTopology
{
    /// <summary>
    /// 所有 tube 数量
    /// </summary>
    int TubeCount { get; }

    /// <summary>
    /// 获取某个 tube 的邻接 tubes
    /// </summary>
    IReadOnlyList<int> GetNeighbors(int tubeIndex);

    /// <summary>
    /// 判断两个 tube 是否相邻
    /// </summary>
    bool AreAdjacent(int a, int b);
}
