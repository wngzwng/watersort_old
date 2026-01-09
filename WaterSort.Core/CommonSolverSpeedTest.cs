using System.Diagnostics;
using System.Text.Json;
using WaterSort.Core.CommonSolver;
using WaterSort.Core.Util;

namespace WaterSort.Core;

public record SolverSpeedResult(
    string LevelId,
    string SolverName,
    bool Solved,
    int StepCount,
    double ElapsedMs
);

public record LevelData(
    string Id,
    List<List<int>> Bottles,
    List<int> EmptyCapacities,
    int TubeCapacity
);

public sealed class CommonSolverSpeedTest
{

    public void Run(string input, string output, Solver solver)
    {
        var levelDataRaw = CsvUtils.ReadDictCsv(input).ToList();

        var levelDatas = levelDataRaw
            .Select(raw => FromContent(raw["id"].ToString(), raw["content"].ToString()));


        double meanElapseMs = 0;
        int count = 1;
        foreach (var (level, record) in Enumerable.Zip(levelDatas, levelDataRaw))
        {
            var result = RunSingle(level, solver);
            record["stepCount"] = result.StepCount;
            record["elapsedMs"] = result.ElapsedMs;
            record["solved"] = result.Solved;
            
            meanElapseMs += (result.ElapsedMs - meanElapseMs) / count;
            count++;
        }
        
        CsvUtils.WriteDictCsv(output, levelDataRaw);
        Console.WriteLine($"mean elapsed ms: {Math.Round(meanElapseMs, 3)}");
    }
    public IEnumerable<SolverSpeedResult> Run(
        IEnumerable<LevelData> levels,
        Solver solver
    )
    {
        foreach (var level in levels)
        {
            yield return RunSingle(level, solver);
        }
    }

    private SolverSpeedResult RunSingle(LevelData level, Solver solver)
    {
        var tubes = level.Bottles
            .Select(b => Tube.CreateNormalTube(level.TubeCapacity, b))
            .ToList();

        var emptyTubes = level.EmptyCapacities
            .Select(cap => Tube.CreateEmptyTube(cap));

        tubes.AddRange(emptyTubes);

        var state = new State(tubes);

        // var sw = Stopwatch.StartNew();
        //
        IReadOnlyList<Move>? solution = null;
        //
        // foreach (var moves in solver.SolveDfsStack(state.DeepClone()))
        // {
        //     solution = moves.ToList();
        //     break;
        // }
        //
        // sw.Stop();
        
        // warm-up
        foreach (var _ in solver.SolveDfsStack(state))
            break;

        var times = new List<double>();

        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();

            foreach (var moves in solver.SolveDfsStack(state))
            {
                solution = moves;
                break;
            }

            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        double median = times.OrderBy(x => x).ElementAt(times.Count / 2);

        return new SolverSpeedResult(
            LevelId: level.Id,
            SolverName: solver.GetType().Name,
            Solved: solution != null,
            StepCount: solution?.Count ?? 0,
            ElapsedMs: median
        );
    }


    public LevelData FromContent(string id, string content)
    {
        List<List<int>> bottles =
            JsonSerializer.Deserialize<List<List<int>>>(content)!;

        bottles = bottles.Where(bottle => bottle.Count > 0).ToList();

        return new LevelData(
            Id: id,
            Bottles: bottles,
            EmptyCapacities: [4, 4],
            TubeCapacity: 4
        );
    }
}
