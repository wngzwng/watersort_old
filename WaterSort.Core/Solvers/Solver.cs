namespace WaterSort.Core.Solvers;

public sealed class Solver
{
    private readonly MoveGroupExplorer _explorer;
    private readonly MoveActuator _actuator;
    private readonly IStateHasher _hasher;
    private readonly bool _useVisited;

    private long _nodeCount;
    public long NodeCount => _nodeCount;

    public Solver(
        MoveGroupExplorer explorer,
        MoveActuator actuator,
        IStateHasher hasher,
        bool useVisited = true)
    {
        _explorer = explorer;
        _actuator = actuator;
        _hasher = hasher;
        _useVisited = useVisited;
    }

    // ============================================================
    // DFS Frame
    // ============================================================
    private sealed class DfsFrame
    {
        public State State { get; }
        public IEnumerator<MoveGroup> Enumerator { get; }
        public int Depth { get; }

        // 进入该 frame 时 path 的长度；回溯时必须回退到这个长度
        public int PathCountAtEntry { get; }

        public DfsFrame(State state, IEnumerator<MoveGroup> enumerator, int depth, int pathCountAtEntry)
        {
            State = state;
            Enumerator = enumerator;
            Depth = depth;
            PathCountAtEntry = pathCountAtEntry;
        }
    }

    // ============================================================
    // Expand Result
    // ============================================================
    private readonly struct ExpandResult
    {
        public State Next { get; }
        public int RollbackToCount { get; } // 本次 Expand 追加 path 前的长度
        public bool IsGoal { get; }
        public bool IsPruned { get; }

        public ExpandResult(State next, int rollbackToCount, bool isGoal, bool isPruned)
        {
            Next = next;
            RollbackToCount = rollbackToCount;
            IsGoal = isGoal;
            IsPruned = isPruned;
        }
    }

    // ============================================================
    // Public API
    // ============================================================
    public IEnumerable<IReadOnlyList<Move>> SolveDfsStack(State start, bool stepByStep = false)
    {
        _nodeCount = 0;

        var visited = new HashSet<StateKey>();
        var path = new List<MoveGroup>();

        // 1) 初始规整化：作为“固定前缀”写入 path，并且直接修改 start
        ApplyNormalizeInPlace(start, path, stepByStep, "初始关卡", "初始关卡规整后");

        if (_useVisited)
            visited.Add(_hasher.BuildKey(start));

        // 2) Push root
        var stack = new Stack<DfsFrame>();
        stack.Push(new DfsFrame(
            state: start,
            enumerator: _explorer.Explore(start).GetEnumerator(),
            depth: 0,
            pathCountAtEntry: path.Count
        ));
        _nodeCount++;

        // 3) DFS
        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            // 3.1) 当前帧就是终局 => yield + 回溯
            if (IsGoal(frame.State))
            {
                yield return Flatten(path);

                stack.Pop();
                RollbackToCount(path, frame.PathCountAtEntry);
                continue;
            }

            // 3.2) 没有可扩展的 MoveGroup => 回溯
            if (!TryGetNextGroup(frame, out var group))
            {
                stack.Pop();
                RollbackToCount(path, frame.PathCountAtEntry);

                if (stepByStep)
                    StepPause($"没有移动了，回溯, frameDepth={frame.Depth}");

                continue;
            }

            // 3.3) Expand one branch
            var expand = ExpandOne(frame.State, group, path, visited, stepByStep);

            // 剪枝：ExpandOne 已经负责把 path 回滚干净了
            if (expand.IsPruned)
                continue;

            // next 直接终局：yield + 回滚本次追加
            if (expand.IsGoal)
            {
                yield return Flatten(path);
                RollbackToCount(path, expand.RollbackToCount);
                continue;
            }

            // 3.4) 正常入栈
            stack.Push(new DfsFrame(
                state: expand.Next,
                enumerator: _explorer.Explore(expand.Next).GetEnumerator(),
                depth: frame.Depth + 1,
                pathCountAtEntry: expand.RollbackToCount
            ));
            _nodeCount++;

            if (stepByStep)
                StepPause("向下 DFS");
        }
    }

    // ============================================================
    // Expand Logic (clone/apply/normalize/visited/path)
    // ============================================================
    private ExpandResult ExpandOne(
        State cur,
        MoveGroup group,
        List<MoveGroup> path,
        HashSet<StateKey> visited,
        bool stepByStep)
    {
        if (stepByStep)
            LogState(cur, "移动前");

        // 1) clone + apply move group
        var next = cur.DeepClone();
        ApplyGroup(next, group);

        if (stepByStep)
        {
            LogMoveGroup(group);
            LogState(next, "移动后");
        }

        // 2) append path (用于回滚)
        var rollbackTo = path.Count;
        path.Add(group);

        // 3) normalize (optional)
        if (TryNormalize(next, out var normalized))
        {
            ApplyGroup(next, normalized);
            path.Add(normalized);

            if (stepByStep)
            {
                LogMoveGroup(normalized);
                LogState(next, "规整后");
            }
        }

        // 4) goal
        if (IsGoal(next))
            return new ExpandResult(next, rollbackTo, isGoal: true, isPruned: false);

        // 5) visited prune
        if (_useVisited)
        {
            var key = _hasher.BuildKey(next);
            if (!visited.Add(key))
            {
                RollbackToCount(path, rollbackTo);

                if (stepByStep)
                    StepPause("已访问节点, 跳过");

                return new ExpandResult(next, rollbackTo, isGoal: false, isPruned: true);
            }
        }

        return new ExpandResult(next, rollbackTo, isGoal: false, isPruned: false);
    }

    private static bool TryGetNextGroup(DfsFrame frame, out MoveGroup group)
    {
        if (!frame.Enumerator.MoveNext())
        {
            group = null!;
            return false;
        }

        group = frame.Enumerator.Current;
        return true;
    }

    private static void RollbackToCount(List<MoveGroup> path, int count)
    {
        if ((uint)count > (uint)path.Count)
            throw new ArgumentOutOfRangeException(nameof(count), $"count={count}, path.Count={path.Count}");

        if (count == path.Count)
            return;

        path.RemoveRange(count, path.Count - count);
    }

    // ============================================================
    // Apply / Normalize
    // ============================================================
    private void ApplyGroup(State state, MoveGroup group)
    {
        foreach (var move in group.Moves)
            _actuator.Apply(state, move);
    }

    private void ApplyNormalizeInPlace(
        State start,
        List<MoveGroup> path,
        bool stepByStep,
        string titleBefore,
        string titleAfter)
    {
        var groups = _explorer.Normal(start).ToList();

        if (stepByStep)
            LogState(start, titleBefore);

        if (groups.Count == 0)
            return;

        foreach (var g in groups)
        {
            if (stepByStep)
                LogMoveGroup(g);

            ApplyGroup(start, g);
            path.Add(g);
        }

        if (stepByStep)
            LogState(start, titleAfter);
    }

    private bool TryNormalize(State state, out MoveGroup normalizedGroup)
    {
        var groups = _explorer.Normal(state).ToList();
        if (groups.Count == 0)
        {
            normalizedGroup = null!;
            return false;
        }

        normalizedGroup = new MoveGroup
        {
            Description = "规整化",
            Moves = groups.SelectMany(g => g.Moves).ToList()
        };

        return normalizedGroup.Moves.Count > 0;
    }

    // ============================================================
    // Goal / Flatten
    // ============================================================
    private static bool IsGoal(State state)
        => state.Tubes.All(t => t.IsEmpty || (t.IsMonochrome && t.IsFull));

    private static IReadOnlyList<Move> Flatten(List<MoveGroup> path)
        => path.SelectMany(g => g.Moves).ToList();

    // ============================================================
    // Debug Helpers
    // ============================================================
    private static void StepPause(string msg)
    {
        Console.WriteLine(msg);
        Console.WriteLine("按任意键继续...");
        Console.ReadKey(intercept: true);
    }

    private static void LogState(State state, string title)
    {
        Console.WriteLine($"==== {title} ====");
        Console.WriteLine(state);
    }

    private static void LogMoveGroup(MoveGroup group)
    {
        Console.WriteLine($"MoveGroup: {group.Description}");
        foreach (var m in group.Moves)
            Console.WriteLine($"  {m.From} -> {m.To}, color={m.Color}, count={m.Count}");
    }
}
