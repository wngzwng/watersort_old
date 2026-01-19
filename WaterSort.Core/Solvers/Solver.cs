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
        public int Incoming { get; }

        public DfsFrame(State state, IEnumerator<MoveGroup> enumerator, int depth, int incoming = 0)
        {
            State = state;
            Enumerator = enumerator;
            Depth = depth;
            Incoming = 0;
        }
    }

    public IEnumerable<IReadOnlyList<Move>> SolveDfsStack(State start, bool stepByStep = false)
    {
        _nodeCount = 0;
        var visited = new HashSet<StateKey>();
        var path = new List<MoveGroup>();

        // ─────────────────────────
        // 0) 初始规整（Normalize-2）
        // ─────────────────────────
        ApplyNormalizeInPlace(start, stepByStep, titleBefore: "初始关卡", titleAfter: "初始关卡规整后");

        // 初始节点加入 visited（只用于非终盘）
        if (_useVisited)
        {
            var startKey = _hasher.BuildKey(start);
            visited.Add(startKey);
        }

        var stack = new Stack<DfsFrame>();
        stack.Push(new DfsFrame(
            start,
            _explorer.Explore(start).GetEnumerator(),
            depth: 0,
            incoming: 0
        ));
        _nodeCount++;

        var curDepth = 0;

        // ─────────────────────────
        // DFS 主循环
        // ─────────────────────────
        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            // 1) 已是终盘 => 产出解并回溯
            if (IsGoal(frame.State))
            {
                yield return Flatten(path);

                stack.Pop();
                curDepth--;
                if (frame.Incoming > 0)
                    path.RemoveAt(path.Count - frame.Incoming);

                continue;
            }

            // 2) 无更多 MoveGroup => 回溯
            if (!frame.Enumerator.MoveNext())
            {
                stack.Pop();
                curDepth--;
                if (frame.Incoming > 0)
                    path.RemoveAt(path.Count - frame.Incoming);

                if (stepByStep)
                    StepPause($"没有移动了，回溯, curDepth={curDepth}, frameDepth={frame.Depth}");

                continue;
            }

            var group = frame.Enumerator.Current;

            // 3) Apply：用 MoveActuator 应用 MoveGroup（在 clone 上）
            if (stepByStep)
                LogState(frame.State, $"移动前: curDepth={curDepth}, frameDepth={frame.Depth}");

            var next = frame.State.DeepClone();
            ApplyGroup(next, group);

            if (stepByStep)
            {
                LogMoveGroup(group);
                LogState(next, $"移动后: curDepth={curDepth}, frameDepth={frame.Depth}");
            }

            // 4) 规整：Normalize-2（可能改变 next，并给出 nextGroup）
            List<MoveGroup> groupList = [group];
            // var nextGroup = group;
            if (TryNormalize(next, out var normalizedGroup))
            {
                groupList.Add(normalizedGroup);
                ApplyGroup(next, normalizedGroup);
                if (stepByStep)
                    LogMoveGroup(normalizedGroup);
                    LogState(next, $"规整后: curDepth={curDepth}, frameDepth={frame.Depth}");
            }
            else
            {
                if (stepByStep)
                    Console.WriteLine("======= 无需规整 ========");
            }

            // 5) next 是终盘 => 直接产出（不进 visited）
            if (IsGoal(next))
            {
                foreach (var moveGroup in groupList)
                {
                    path.Add(moveGroup);
                }

                var groupListCount = groupList.Count;
                yield return Flatten(path);
                // path.RemoveAt(path.Count - 1);
                path.RemoveAt(path.Count - groupListCount);
                continue;
            }

            // 6) visited 仅作用于非终盘
            if (_useVisited)
            {
                var key = _hasher.BuildKey(next);

                if (stepByStep)
                    Console.WriteLine($"hash: {key.GetHashCode()}");

                if (!visited.Add(key))
                {
                    if (stepByStep)
                        StepPause("已访问节点, 跳过");
                    continue;
                }
            }

            // 7) 继续向下 DFS
            foreach (var moveGroup in groupList)
            {
                path.Add(moveGroup);
            }

            stack.Push(new DfsFrame(
                next,
                _explorer.Explore(next).GetEnumerator(),
                depth: ++curDepth,
                incoming: groupList.Count
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
    private void ApplyNormalizeInPlace(State start, bool stepByStep, string titleBefore, string titleAfter)
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
        normalizedGroup.Moves = groups.SelectMany(groups => groups.Moves).ToList();
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
