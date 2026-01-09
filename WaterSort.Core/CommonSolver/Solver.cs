using System.ComponentModel.Design;
using System.Text;

namespace WaterSort.Core.CommonSolver;

public sealed class Solver
{
    private readonly IStateHasher _hasher;
    private readonly IMoveGroupExplorer _explorer;
    private readonly IStateNormalizer _normalizer2;

    public Solver(
        IMoveGroupExplorer explorer,
        IStateNormalizer normalizer2,
        IStateHasher hasher)
    {
        _explorer = explorer;
        _normalizer2 = normalizer2;
        _hasher = hasher;
    }

    // public IEnumerable<State> Solve(State start)
    // {
    //     var visited = new HashSet<StateKey>();
    //     var queue = new Queue<State>();
    //
    //     // 初始状态也要 Normalize-2
    //     start = _normalizer2.Normalize(start);
    //     var startKey = _hasher.BuildKey(start);
    //
    //     visited.Add(startKey);
    //     queue.Enqueue(start);
    //
    //     while (queue.Count > 0)
    //     {
    //         var state = queue.Dequeue();
    //
    //         // 1. Explore → MoveGroup
    //         foreach (var group in _explorer.Explore(state))
    //         {
    //             // 2. Apply → NewState
    //             var next = state.Apply(group);
    //
    //             // 3. Normalize-2（同类型单色瓶聚合）
    //             next = _normalizer2.Normalize(next);
    //
    //             // 4. BuildHashKey（含 Normalize-1）
    //             var key = _hasher.BuildKey(next);
    //
    //             if (visited.Add(key))
    //             {
    //                 queue.Enqueue(next);
    //                 yield return next;
    //             }
    //         }
    //     }
    // }
    //
    
    private sealed class DfsFrame
    {
        public State State { get; }
        public IEnumerator<MoveGroup> Enumerator { get; }
        
        public int Depth { get; }
        public MoveGroup? Incoming { get; }

        public DfsFrame(
            State state,
            IEnumerator<MoveGroup> enumerator,
            int depth,
            MoveGroup? incoming)
        {
            State = state;
            Enumerator = enumerator;
            Depth = depth;
            Incoming = incoming;
        }
    }

    public IEnumerable<IReadOnlyList<Move>> SolveDfsStack(State start, bool stepByStep = false)
    {
        var visited = new HashSet<StateKey>();
        var path = new List<MoveGroup>();

        // ─────────────────────────
        // 初始 Normalize-2
        // ─────────────────────────
        var moveGroups = _normalizer2.Normalize(start).ToList();

        if (moveGroups.Count > 0)
        { 
            if (stepByStep)
                LogState(start, "初始关卡");
            foreach (var group in moveGroups)
            {
                if (stepByStep)
                    LogMoveGroup(group);
                start.Apply(group);
            }
            if (stepByStep)
                LogState(start, "初始关卡规整后");
        }
        else
        {
            if (stepByStep)
                LogState(start, "初始关卡");
        }

        // 初始状态进入 visited（⚠️ 初始态一定是非终盘）
        var startKey = _hasher.BuildKey(start);
        visited.Add(startKey);

        var stack = new Stack<DfsFrame>();
        stack.Push(new DfsFrame(
            start,
            _explorer.Explore(start).GetEnumerator(),
            depth: 0,
            incoming: null
        ));

        var curDepth = 0;

        // ─────────────────────────
        // DFS 主循环
        // ─────────────────────────
        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            // ─────────────────────────
            // 1. 当前节点已是终盘 → 产出解并回溯
            // ─────────────────────────
            if (IsGoal(frame.State))
            {
                yield return path
                    .SelectMany(g => g.Moves)
                    .ToList();

                stack.Pop();
                curDepth--;
                if (frame.Incoming != null)
                    path.RemoveAt(path.Count - 1);

                continue;
            }

            // ─────────────────────────
            // 2. 没有更多 MoveGroup → 回溯
            // ─────────────────────────
            if (!frame.Enumerator.MoveNext())
            {
                stack.Pop();
                curDepth--;
                if (frame.Incoming != null)
                    path.RemoveAt(path.Count - 1);
                if (stepByStep)
                {
                    Console.WriteLine($"没有移动了，回溯,  curPath: {curDepth}, frameDepth: {frame.Depth}");
                    Console.WriteLine("按任意键继续...");
                    Console.ReadKey(intercept: true);
                }
                continue;
            }

            var group = frame.Enumerator.Current;

            // ─────────────────────────
            // 3. Apply
            // ─────────────────────────
            if (stepByStep)
                LogState(frame.State, $"移动前: curPath: {curDepth}, frameDepth: {frame.Depth}");

            var next = frame.State.DeepClone().Apply(group);
            if (stepByStep)
            {
                LogMoveGroup(group);
                LogState(next, $"移动后: curPath: {curDepth}, frameDepth: {frame.Depth}");
            }
           

            // ─────────────────────────
            // 4. Normalize-2
            // ─────────────────────────
            MoveGroup nextGroup = group;
            if (TryNormalize(next, group, out var normalizedGroup))
            {
                nextGroup = normalizedGroup;
                if (stepByStep)
                    LogState(next, $"规整后: curPath: {curDepth}, frameDepth: {frame.Depth}");
            }
            else
            {
                if (stepByStep)
                    Console.WriteLine("======= 无需规整 ========");
            }

            // ─────────────────────────
            // 5. 如果 next 是终盘 → 直接产出（⚠️ 不进 visited）
            // ─────────────────────────
            if (IsGoal(next))
            {
                path.Add(nextGroup);

                yield return path
                    .SelectMany(g => g.Moves)
                    .ToList();

                path.RemoveAt(path.Count - 1);
                continue;
            }

            // ─────────────────────────
            // 6. visited 只作用于【非终盘】
            // ─────────────────────────
            var key = _hasher.BuildKey(next);
            if (stepByStep)
                Console.WriteLine($"hash: {key.GetHashCode()}");
            
            if (!visited.Add(key))
            {
                if (stepByStep)
                {
                    var sb = new StringBuilder();
                    TextRender.Title(sb, "已访问节点, 跳过");

                    Console.WriteLine();
                    Console.WriteLine(sb.ToString());
                    
                    Console.WriteLine("按任意键继续...");
                    Console.ReadKey(intercept: true);
                }
                continue;
            }

            // ─────────────────────────
            // 7. 向下 DFS
            // ─────────────────────────
            path.Add(nextGroup);

            stack.Push(new DfsFrame(
                next,
                _explorer.Explore(next).GetEnumerator(),
                depth: ++curDepth,
                incoming: nextGroup
            ));

            if (stepByStep)
            {
                Console.WriteLine("按任意键继续...");
                Console.ReadKey(intercept: true);
            }
           
        }
    }



    public void LogState(State state, string title)
    {
        var sb = new StringBuilder();

        TextRender.Title(sb, title);
        sb.AppendLine();
        sb.AppendLine(state.Render(true));
        TextRender.Divider(sb);
        
        Console.WriteLine(sb.ToString());
    }

    public void LogMoveGroup(MoveGroup moveGroup, string title = "")
    {
        var sb = new StringBuilder();

        TextRender.Title(sb, string.IsNullOrEmpty(title) ? $"moves: {moveGroup.Moves.Count} kind: {moveGroup.Kind}" : title);
        sb.AppendLine();
        sb.AppendLine(moveGroup.Render());
        TextRender.Divider(sb);
        
        Console.WriteLine(sb.ToString());
    }

    private bool TryNormalize(State state, MoveGroup localMoveGroup, out MoveGroup nextMoveGroup)
    {
        nextMoveGroup = localMoveGroup;
        var moveGroups= _normalizer2.Normalize(state).ToList();
        if (moveGroups.Count == 0)
            return false;

        var moves = localMoveGroup.Moves.ToList();
        foreach (var group in moveGroups)
        {
            state.Apply(group);
            moves.AddRange(group.Moves);
        }

        nextMoveGroup = new MoveGroup(localMoveGroup.Kind, moves);
        return true;
      }
    
    private bool IsGoal(State state)
    {
        // 示例：所有非空瓶都是单色
        return state.Tubes.All(t => t.IsEmpty || t.IsMonochrome);
    }

}
