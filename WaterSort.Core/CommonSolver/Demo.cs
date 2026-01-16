namespace WaterSort.Core.CommonSolver.Demo;


// 定义移动
public sealed class Move
{
    public int From { get; set; }
    public int To { get; set; }
    public int Color { get; set; }
    public int Count { get; set; }
}


public  enum MoveGroupKind
{
    // 顶部边界下降
    AdvanceBoundary,      // 推进可见边界（主推进）
    MergeMonochrome,      // 不同类型单色瓶的真实聚合
    TransformSemantic,    // TubeType / 规则语义改变
    NormalizeMonochrome   // （如果你保留它作为边，否则不用)
}

public sealed class MoveGroup
{
    public MoveGroupKind Kind { get; set; }
    public List<Move> Moves { get; set; }
}





public interface IMoveGroupExplorer
{
    IEnumerable<MoveGroup> Explore(State state);
}

public sealed class TubeContext
{
    
}

public interface IStateNormalizer
{
    IEnumerable<MoveGroup> Normalize(State state);
}


public interface IStateHasher
{
    StateKey BuildKey(State state);
}


public interface IMoveActuator
{
    State Apply(State state, Move move);
    State Apply(State state, MoveGroup move);
    
    void OnBeforeMove(State state, Move move);
    void OnAfterMove(State state, Move move);
}


public enum TubeKind
{
    Normal,
}

public record TubeStructuralKey(
    int Capacity,
    bool IsMonochrome,
    TubeKind Kind // Normal / Mono / SpecialMono / Fixed / ...
);

public record TubeMoveAvailability(
    int TubeIndex,
    int ExportColor,
    int ExportCount, // 该颜色可从此瓶倒出的数量
    int AcceptCount, // 该颜色可被此瓶接收的数量
    int AcceptColor,
    TubeStructuralKey StructuralKey,
    Object? Extra
);



public class Demo
{
    
}