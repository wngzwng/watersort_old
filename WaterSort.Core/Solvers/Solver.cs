namespace WaterSort.Core.Solvers;

public sealed class Solver
{
    private readonly MoveGroupExplorer _explorer;
    private readonly MoveActuator _actuator;
    private readonly IStateHasher _hasher;        // BuildKey(state)
    private readonly bool _useVisited = true;

    private long _nodeCount;

    public Solver(
        MoveGroupExplorer explorer,
        MoveActuator actuator,
        IStateHasher hasher)
    {
        _explorer = explorer;
        _actuator = actuator;
        _hasher = hasher;
    }

    private sealed class DfsFrame
    {
        public State State { get; }
        public IEnumerator<MoveGroup> Enumerator { get; }
        public int Depth { get; }
        
        // 进入该 frame 时，path 的长度
        public int PathCountAtEntry { get; }

        public DfsFrame(State state, IEnumerator<MoveGroup> enumerator, int depth, int pathCountAtEntry)
        {
            State = state;
            Enumerator = enumerator;
            Depth = depth;
            PathCountAtEntry = pathCountAtEntry;
        }
    }

    public IEnumerable<IReadOnlyList<Move>> SolveDfsStack(State start, bool stepByStep = false)
    {
        // ─────────────────────────
        // Local Helpers
        // ─────────────────────────
        static void RollbackTo(List<MoveGroup> path, int pathCountAtEntry)
        {
            if (pathCountAtEntry < 0 || pathCountAtEntry > path.Count)
                throw new ArgumentException(
                    $"pathCountAtEntry({pathCountAtEntry}) 不合法，当前 path.Count={path.Count}");

            var removeCount = path.Count - pathCountAtEntry;
            if (removeCount <= 0)
                return;

            path.RemoveRange(pathCountAtEntry, removeCount);
        }

        // ─────────────────────────
        // 0) Init
        // ─────────────────────────
        _nodeCount = 0;

        var visited = new HashSet<StateKey>();
        var path = new List<MoveGroup>();

        // ─────────────────────────
        // 1) Normalize Start
        // ─────────────────────────
        ApplyNormalizeInPlace(start, path, stepByStep, titleBefore: "初始关卡", titleAfter: "初始关卡规整后");

        if (_useVisited)
            visited.Add(_hasher.BuildKey(start));

        // ─────────────────────────
        // 2) Push Root Frame
        // ─────────────────────────
        var stack = new Stack<DfsFrame>();
        stack.Push(new DfsFrame(
            start,
            _explorer.Explore(start).GetEnumerator(),
            depth: 0,
            pathCountAtEntry: path.Count
        ));
        _nodeCount++;

        var curDepth = 0;

        // ─────────────────────────
        // 3) DFS Main Loop
        // ─────────────────────────
        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            // 3.1) Goal => yield + backtrack
            if (IsGoal(frame.State))
            {
                yield return Flatten(path);

                stack.Pop();
                curDepth--;
                RollbackTo(path, frame.PathCountAtEntry);
                
                continue;
            }

            // 3.2) No more MoveGroup => backtrack
            if (!frame.Enumerator.MoveNext())
            {
                stack.Pop();
                curDepth--;
                RollbackTo(path, frame.PathCountAtEntry);

                if (stepByStep)
                    StepPause($"没有移动了，回溯, curDepth={curDepth}, frameDepth={frame.Depth}");

                continue;
            }

            // ─────────────────────────
            // 4) Expand One MoveGroup
            // ─────────────────────────
            var group = frame.Enumerator.Current;

            if (stepByStep)
                LogState(frame.State, $"移动前: curDepth={curDepth}, frameDepth={frame.Depth}");

            var next = frame.State.DeepClone();

            // 4.1) Apply group
            ApplyGroup(next, group);

            if (stepByStep)
            {
                LogMoveGroup(group);
                LogState(next, $"移动后: curDepth={curDepth}, frameDepth={frame.Depth}");
            }

            // ─────────────────────────
            // 5) Normalize Next (optional)
            // ─────────────────────────
            var beforeAppend = path.Count;
            
            path.Add(group);

            if (TryNormalize(next, out var normalizedGroup))
            {
                ApplyGroup(next, normalizedGroup);
                path.Add(normalizedGroup);
                
                if (stepByStep)
                {
                    LogMoveGroup(normalizedGroup);
                    LogState(next, $"规整后: curDepth={curDepth}, frameDepth={frame.Depth}");
                }
            }
            else
            {
                if (stepByStep)
                    Console.WriteLine("======= 无需规整 ========");
            }

            // ─────────────────────────
            // 6) Next is Goal => yield (no push)
            // ─────────────────────────
            if (IsGoal(next))
            {
                yield return Flatten(path);
                
                // 回退到 append 前（撤销 group + normalizedGroup)
                RollbackTo(path, beforeAppend);
                continue;
            }

            // ─────────────────────────
            // 7) Visited Check (only for non-goal)
            // ─────────────────────────
            if (_useVisited)
            {
                var key = _hasher.BuildKey(next);

                if (stepByStep)
                    Console.WriteLine($"hash: {key.GetHashCode()}");

                if (!visited.Add(key))
                {
                    if (stepByStep)
                        StepPause("已访问节点, 跳过");

                    // 这里还没把 groupList 加入 path，所以不需要回滚
                    continue;
                }
            }

            // ─────────────────────────
            // 8) Push Next Frame
            // ─────────────────────────
            stack.Push(new DfsFrame(
                next,
                _explorer.Explore(next).GetEnumerator(),
                depth: ++curDepth, 
                pathCountAtEntry: beforeAppend
            ));
            _nodeCount++;

            if (stepByStep)
                StepPause("向下 DFS");
        }
    }



    // ------------------------------------------------------------
    // Apply：通过 MoveActuator 应用 MoveGroup
    // ------------------------------------------------------------
    private void ApplyGroup(State state, MoveGroup group)
    {
        // 如果你 MoveGroup 中的 Moves 已经保证顺序，就是直接 apply
        foreach (var move in group.Moves)
            _actuator.Apply(state, move);
    }

    // ------------------------------------------------------------
    // Normalize：原地规整 + 返回“规整 MoveGroup”
    // ------------------------------------------------------------
    private void ApplyNormalizeInPlace(State start, List<MoveGroup> path, bool stepByStep, string titleBefore, string titleAfter)
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
        // 你原逻辑是：TryNormalize(next, group, out normalizedGroup)
        // 这里保持接口
        var groups = _explorer.Normal(state).ToList();
        if (groups.Count == 0)
        {
            normalizedGroup = null;
            return false;
        }

        normalizedGroup = new MoveGroup();
        normalizedGroup.Description = "规整化";
        normalizedGroup.Moves = groups.SelectMany(group => group.Moves).ToList();
        if (normalizedGroup.Moves.Count <= 0)
        {
            return false;
        }
        return true;
    }

    // ------------------------------------------------------------
    // Goal / 展平
    // ------------------------------------------------------------
    private bool IsGoal(State state)
    {
        // TODO: 你自己的终盘判定
        // 例如：所有 tube 要么空要么单色满
        // return state.IsSolved();
        return state.Tubes.All(t => t.IsEmpty || (t.IsMonochrome && t.IsFull));
    }

    private static IReadOnlyList<Move> Flatten(List<MoveGroup> path)
        => path.SelectMany(g => g.Moves).ToList();

    // ------------------------------------------------------------
    // Debug helpers（你已有就替换）
    // ------------------------------------------------------------
    private static void StepPause(string msg)
    {
        Console.WriteLine(msg);
        Console.WriteLine("按任意键继续...");
        Console.ReadKey(intercept: true);
    }

    private static void LogState(State state, string title)
    {
        Console.WriteLine($"==== {title} ====");
        Console.WriteLine(state); // 你可以换成 TextRender
    }

    private static void LogMoveGroup(MoveGroup group)
    {
        Console.WriteLine($"MoveGroup: {group.Description}");
        foreach (var m in group.Moves)
            Console.WriteLine($"  {m.From} -> {m.To}, color={m.Color}, count={m.Count}");
    }
}
