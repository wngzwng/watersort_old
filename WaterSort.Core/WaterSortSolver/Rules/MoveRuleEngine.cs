using WaterSort.Core.WaterSortSolver.Domain;

namespace WaterSort.Core.WaterSortSolver.Rules;

public class MoveRuleEngine
{
    public IEnumerable<Move> Enumrate(WaterSortState state, WaterSortContext ctx)
    {
        int availableMono =
            state.TopBoundaryIndex.Count(i => i == 0);

        var requireMonoTable = BuildRequiredMonoTable(state, ctx);
        int totalRequired = requireMonoTable.Values.Sum();

        for (int i = 0; i < state.TopBoundaryIndex.Count; i++)
        {
            int tbIndex = state.TopBoundaryIndex[i];
            if (tbIndex <= 0) continue;

            int boundary = ctx.Boundaries[i][tbIndex];
            int color = ctx.WaterData[i][boundary];
            int space = ctx.BottleCapacity - boundary;

            int adjusted = RequiredMonoAfterPour(state, ctx, color, space);

            if (totalRequired - requireMonoTable[color] + adjusted <= availableMono)
                yield return new Move(i);
        }
    }

    private static Dictionary<int, int> BuildRequiredMonoTable(
        WaterSortState state,
        WaterSortContext ctx
    )
    {
        var table = new Dictionary<int, int>();
        foreach (var color  in state.ReleaseUnits.Keys)
        {
            int released = state.ReleaseUnits[color];
            int space = state.TopSpaces.GetValueOrDefault(color);
            table[color] = RequiredMono(released, space, ctx.EmptyBottleCapacity);
        }
        return table;
    }

    private static int RequiredMono(int released, int space, int cap)
    {
        return Math.Max(0, 
            (int)Math.Ceiling( (released - space) / (double)cap)
        );
    }

    private static int RequiredMonoAfterPour(
        WaterSortState state,
        WaterSortContext ctx,
        int color,
        int freedSpace
    )
    {
        int newSpace = state.TopSpaces.GetValueOrDefault(color) - freedSpace;
        int released = 0;
        try
        {
            released = state.ReleaseUnits[color];
        }
        catch (Exception e)
        {
            Console.WriteLine(state.ReleaseUnits);
        }
        return RequiredMono(released, newSpace, ctx.EmptyBottleCapacity);
    }
}