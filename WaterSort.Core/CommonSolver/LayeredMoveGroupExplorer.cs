namespace WaterSort.Core.CommonSolver;

public sealed record FromTube(
    int TubeIndex,
    int Color,
    int ExportCount
);

public sealed record ToTube(
    int TubeIndex,
    int AcceptCount
);


public sealed class LayeredMoveGroupExplorer : IMoveGroupExplorer
{
    private static readonly MoveGroupKind[] Order =
    {
        MoveGroupKind.AdvanceBoundary,
        MoveGroupKind.MergeMonochrome,
        MoveGroupKind.TransformSemantic,
        MoveGroupKind.NormalizeMonochrome // 如果你允许它作为边
    };

    public IEnumerable<MoveGroup> Explore(State state)
    {
        var availability = AvailabilityScanner.Scan(state);
        var (froms, tos) = AvailabilityPartition.Partition(availability);

        foreach (var kind in Order)
        {
            foreach (var group in ExploreLayer(kind, state, froms, tos))
            {
                yield return group;
            }
        }
    }

    private IEnumerable<MoveGroup> ExploreLayer(
        MoveGroupKind kind,
        State state,
        IReadOnlyList<FromTube> froms,
        IReadOnlyList<ToTube> tos)
    {
        foreach (var from in froms)
        {
            // 1. 基于 kind，筛选 To 候选
            var toCandidates = SelectToCandidates(kind, state, from, tos);
            if (toCandidates.Count == 0)
                continue;

            // 2. 在当前 kind 语义下，尝试构造 MoveGroup
            if (TryBuildMoveGroup(kind, state, from, toCandidates, out var group))
            {
                yield return group;
            }
        }
    }
    
    
    
    private static IReadOnlyList<ToTube> SelectToCandidates(
        MoveGroupKind kind,
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos)
    {
        return kind switch
        {
            MoveGroupKind.AdvanceBoundary =>
                SelectForAdvanceBoundary(state, from, tos),

            MoveGroupKind.MergeMonochrome =>
                SelectForMergeMonochrome(state, from, tos),

            // MoveGroupKind.TransformSemantic =>
                // SelectForTransformSemantic(state, from, tos),

            _ => Array.Empty<ToTube>()
        };
    }
    
    
    private static bool TryBuildMoveGroup(
        MoveGroupKind kind,
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos,
        out MoveGroup group)
    {
        group = null!;

        return kind switch
        {
            MoveGroupKind.AdvanceBoundary =>
                TryBuildAdvanceBoundary(state, from, tos, out group),

            // MoveGroupKind.MergeMonochrome =>
            //     TryBuildMergeMonochrome(state, from, tos, out group),
            //
            // MoveGroupKind.TransformSemantic =>
            //     TryBuildTransformSemantic(state, from, tos, out group),

            _ => false
        };
    }

    private static int BuildTopBoundary(Tube tube)
    {
        int topBoundary = 0;
        for (int i = 0; i < tube.Count - 1; i++)
        {
            if (tube.Cells[i] != tube.Cells[i + 1])
                topBoundary = i + 1;
        }
        return topBoundary;
    }
    
    private static IReadOnlyList<ToTube> SelectForAdvanceBoundary(
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos)
    {
        var fromTopBoundary = BuildTopBoundary(state.Tubes[from.TubeIndex]);
        if (fromTopBoundary == 0)
            return Enumerable.Empty<ToTube>().ToList();
        
        var allowColor = from.Color;
        return tos
            .Where(t =>
            {
                // 基础合法性
                if (from.TubeIndex == t.TubeIndex)
                    return false;
                
                var toTube = state.Tubes[t.TubeIndex];
                if (!toTube.IsEmpty && toTube.TopColor != allowColor)
                    return false;

                return true;
            })
            .ToList();
    }

    private static IReadOnlyList<ToTube> SelectForMergeMonochrome(
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos)
    {
        var fromTube = state.Tubes[from.TubeIndex];

        // 必须是单色
        if (!fromTube.IsMonochrome)
            return Array.Empty<ToTube>();

        return tos
            .Where(t =>
            {
                var toTube = state.Tubes[t.TubeIndex];

                // 单色
                if (!toTube.IsMonochrome)
                    return false;

                // 同颜色
                if (toTube.TopColor != from.Color)
                    return false;

                // 不同 TubeType（这是关键）
                if (toTube.Type == fromTube.Type)
                    return false;

                return true;
            })
            .ToList();
    }

    
    private static bool TryBuildAdvanceBoundary(
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos,
        out MoveGroup group)
    {
        group = null!;
        if (!WillAdvanceBoundary(state, from, tos))
            return false;

        List<Move> moves = new List<Move>();
        int remainCount = from.ExportCount;
        foreach (var to in tos)
        {
            if (remainCount <= 0)
                break;
            
            int moveCount = Math.Min(remainCount, to.AcceptCount);
            if (moveCount <= 0)
                continue;

            var move = new Move(
                from: from.TubeIndex,
                to: to.TubeIndex,
                color: from.Color,
                count: moveCount
            );
            moves.Add(move);
            remainCount -= move.Count;
        }
        
        group = new MoveGroup(
            MoveGroupKind.AdvanceBoundary,
            moves
        );

        return true;
    }
    
    private static bool WillAdvanceBoundary(
        State state,
        FromTube from,
        IReadOnlyList<ToTube> tos)
    {
        // ─────────────────────
        // 1. from 是否有可倒出
        // ─────────────────────
        if (from.ExportCount <= 0)
            return false;

        // ─────────────────────
        // 2. 计算接收空间总量
        // ─────────────────────
        int totalAccept = 0;
        foreach (var to in tos)
        {
            totalAccept += to.AcceptCount;
        }

        if (totalAccept <= 0)
            return false;

        return from.ExportCount <= totalAccept;
    }

}
