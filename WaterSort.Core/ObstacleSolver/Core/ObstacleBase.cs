namespace WaterSort.Core.ObstacleSolver.Core;

public class ObstacleConfig
{
    // 障碍物类型
    // 障碍物ID
    // 障碍物作用类型
}

public enum ObstacleScope
{
    Tube,
    Cell
}

public record MoundPoint
{
    int TubeIndex { get; set; }
    int? CellIndex { get; set; }
}

public enum ObstacleType
{
    // 帷幕 
    // 石膏
    // 冰盒
} 

























