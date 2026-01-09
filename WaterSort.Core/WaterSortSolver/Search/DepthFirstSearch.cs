using WaterSort.Core.WaterSortSolver.Domain;
using WaterSort.Core.WaterSortSolver.Rules;

namespace WaterSort.Core.WaterSortSolver.Search;

public class DepthFirstSearch
{
    private readonly MoveRuleEngine _rules = new();

    public IEnumerable<List<Move>> Search(
        WaterSortState initial,
        WaterSortContext ctx,
        bool findAll,
        int? maxDepth
    )
    {
        var visited = new HashSet<string>();
        return Visit(initial, ctx, new List<Move>(), visited, findAll, maxDepth);
    }

    private IEnumerable<List<Move>> Visit(
        WaterSortState state,
        WaterSortContext ctx,
        List<Move> path,
        HashSet<string> visited,
        bool findAll,
        int? maxDepth
    )
    {
        if (state.IsSolved())
        {
            yield return new List<Move>(path);
            if (!findAll) yield break;
        }
        
        if (maxDepth.HasValue && path.Count >= maxDepth) yield break;
        
        string hash = state.Hash(ctx);
        if (!visited.Add(hash)) yield break;

        foreach (var move in _rules.Enumrate(state, ctx))
        {
            var next = state.Apply(move, ctx);
            path.Add(move);

            foreach (var sol in Visit(next, ctx, path, visited, findAll, maxDepth))
            {
                yield return sol;
            }
            
            path.RemoveAt(path.Count - 1);
        }
    }
}