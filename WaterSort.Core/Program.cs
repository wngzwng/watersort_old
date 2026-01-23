using System.Diagnostics;
using WaterSort.Core;
using WaterSort.Core.CommonSolver;
using WaterSort.Core.Solvers;
using WaterSort.Core.Solvers.Obstacles;
using WaterSort.Core.WaterSortSolver.Solver;
using Move = WaterSort.Core.CommonSolver.Move;
using Solver = WaterSort.Core.CommonSolver.Solver;
using State = WaterSort.Core.CommonSolver.State;
using Tube = WaterSort.Core.CommonSolver.Tube;

class Program
{
    static void Main()
    {
        // Run("Case1_SimpleSolvable", TestCase1);
        // Run("Case2_SimpleUnsolvable", TestCase2);
        // Run("Case3_EdgeCases", TestCase3);
        // Run("Case4_ComplexSolvable", TestCase4);
        // Run("Case5_LargeCapacity", TestCase5);
        //
        // Console.WriteLine("\nAll tests completed.");
        
        // TestSolver();
        // TestCommonSolver();

        TestDemoSolver();
    }

    static void TestCommonSolver()
    {
        string input = "/Users/admin/Desktop/watorsort_level/test_level.csv";
        string output = "/Users/admin/Desktop/watorsort_level/test_level_elapse.csv";
        var test = new CommonSolverSpeedTest();
        var solver = SolverFactory.CreateDefault();
        test.Run(input, output, solver);
    }

    static void TestDemoSolver()
    {
        // // 长试管
        // List<List<int>> bottles =
        // [
        //     [1, 5, 1, 5, 7, 4, 4, 2],
        //     [4, 6, 2, 3, 5, 2, 6, 1],
        //     [5, 7, 2, 6, 3, 4, 1, 2],
        //     [5, 2, 3, 7, 3, 6, 3, 1],
        //     [1, 1, 6, 6, 4, 2, 5, 7],
        //     [3, 3, 5, 7, 2, 4, 4, 5],
        //     [1, 7, 6, 6, 3, 7, 4, 7],
        // ];
        // var bottleCapacity = 8;
        // List<int> extraEmptyConfig = [4, 4];
        //
        // List<List<int>> bottles =
        // [
        //     [1, 1, 2, 2],
        //     [3, 3, 4, 4],
        //     [2, 2, 3, 3],
        //     [4, 4, 1, 1],
        // ];
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [4, 4];

        // List<List<int>> bottles =
        // [
        //     [1, 1, 1, 1],
        //     [2],
        //     [2, 2, 2],
        // ];
        // var bottleCapacities = [4, 4, 3];
        
        // List<List<int>> bottles =  
        // [
        //     [6, 2, 5, 1],
        //     [4, 5, 4, 1],
        //     [5, 6, 4, 2],
        //     [3, 6, 2, 3],
        //     [1, 2, 4, 1],
        //     [3, 5, 3, 6],
        // ];
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [2, 2];
        
        
        // List<List<int>> bottles =
        // [
        //     [8, 2, 11, 5],
        //     [2, 10, 4, 2],
        //     [7, 3, 1, 5],
        //     [4, 11, 7, 13],
        //     [6, 13, 1, 3],
        //     [6, 4, 10, 7],
        //     [8, 11, 1, 9],
        //     [12, 7, 4, 2],
        //     [12, 6, 9, 5],
        //     [12, 3, 11, 1],
        //     [8, 9, 8, 6],
        //     [13, 9, 12, 5],
        //     [10, 3, 13, 10],
        //     // [],
        //     // []
        // ];
        //
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [3, 2];
        //
        // List<List<int>> bottles =
        // [
        //     [4, 3, 2, 1],
        //     [1, 6, 2, 5],
        //     [9, 6, 8, 7],
        //     [4, 8, 3, 10],
        //     [9, 11, 8, 5],
        //     [10, 6, 3, 7],
        //     [3, 7, 12, 4],
        //     [6, 9, 1, 10],  
        //     [11, 12, 5, 11],
        //     [9, 12, 12, 10],
        //     [5, 2, 8, 1],
        //     [7, 2, 4, 11],
        // ];
        //
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [4, 4];

        // List<ObstacleEntry> entries =
        // [
        //     new ObstacleEntry()
        //     {
        //         Id = 1,
        //         Kind = ObstacleKind.Mystery,
        //         TubeTargets = [0],
        //         CellTargets = [0, 1, 2]
        //     },
        //     new ObstacleEntry()
        //     {
        //         Id = 2,
        //         Kind = ObstacleKind.Mystery,
        //         TubeTargets = [1],
        //         CellTargets = [0, 1, 2]
        //     },
        //     new ObstacleEntry()
        //     {
        //         Id = 3,
        //         Kind = ObstacleKind.Mystery,
        //         TubeTargets = [2],
        //         CellTargets = [0, 1, 2]
        //     }
        // ];

        // |c,0,9|c,1,4|c,4,5|c,5,9|q,25,29,33,37,41,45
        // List<List<int>> bottles =
        // [
        //     [8, 10, 7], 
        //     [6, 1, 8], 
        //     [3, 7, 9], 
        //     [8, 7, 3], 
        //     [6, 10, 7], 
        //     [6, 10, 4], 
        //     [5, 8, 1], 
        //     [4, 5, 1], 
        //     [4, 1, 5], 
        //     [9, 9, 3], 
        //     [4, 5, 6], 
        //     [3, 10, 9]
        // ]; // |c,0,9|c,1,4|c,4,5|c,5,9|q,25,29,33,37,41,45
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [];
        // var entries = new ObstacleEntryBuilder()
        //     .AddMysteryCells(6, [1])
        //     .AddMysteryCells(7, [1])
        //     .AddMysteryCells(8, [1])
        //     .AddMysteryCells(9, [1])
        //     .AddMysteryCells(10, [1])
        //     .AddMysteryCells(11, [1])
        //     .AddCurtain(0, 9)
        //     .AddCurtain(1, 4)
        //     .AddCurtain(4, 5)
        //     .AddCurtain(5, 9)
        //     // .AddClamp(4)
        //     .BuildList();
        //
        
        List<List<int>> bottles =
        [
            [1, 12, 9, 6], 
            [5, 7, 12, 3], 
            [12, 8, 10, 10], 
            [2, 2], 
            [5, 8, 6, 1], 
            [10, 6, 1],
            [3, 11, 11],
            [3, 8, 7, 5], 
            [5, 7, 4, 11],
            [4, 11, 4, 6], 
            [9, 9], 
            [3, 1, 4, 8], 
            [2, 2, 9], 
            [12, 7, 10]
        ]; //|c,0,2|c,1,8|c,7,11|c,8,6|q,0,1,2,4,5,6,8,9,10,16,17,18,21,25,28,29,30,32,33,34,36,37,38,44,45,46,49,53
        var bottleCapacity = 4;
        List<int> extraEmptyConfig = [];
        var entries = new ObstacleEntryBuilder()
            .PatchAddMysteryCells(bottleCapacity,
            [
                0, 1, 2, 4, 5, 6, 8, 9, 10, 16, 17, 18, 21, 25, 28, 29, 30, 32, 33, 34, 36, 37, 38, 44, 45, 46, 49, 53
            ])
            .AddCurtain(0, 2)
            .AddCurtain(1, 8)
            .AddCurtain(7, 11)
            .AddCurtain(8, 6)
            .BuildList();
        var sw = Stopwatch.StartNew();
        var stepBySteop = false;
        if (Demo.Test(bottles, bottleCapacity, out var solutionMoves, out var nodeCount, extraEmptyConfig, entries, stepBySteop))
        // if (Demo.Test(bottles, bottleCapacities, out var solutionMoves))
        {
            sw.Stop();
            
            Console.WriteLine(string.Join(",\n", solutionMoves));
            Console.WriteLine($"有解, {solutionMoves.Count}步, 耗时：{sw.Elapsed.TotalMilliseconds:F3}ms, 搜索节点数：{nodeCount}");
            return;
        }
        sw.Stop();
        Console.WriteLine($"无解, 耗时：{sw.Elapsed.TotalMilliseconds:F3}ms, 搜索节点数：{nodeCount}");
        
    }
    static void TestSolver()
    { 
        // List<List<int>> bottles =
        // [
        //     [1, 1, 2, 2],
        //     [3, 3, 4, 4],
        //     [2, 2, 3, 3],
        //     [4, 4, 1, 1],
        //     [],
        //     []
        // ];
        
        // List<List<int>> bottles =  
        // [
        //     [6, 2, 5, 1],
        //     [4, 5, 4, 1],
        //     [5, 6, 4, 2],
        //     [3, 6, 2, 3],
        //     [1, 2, 4, 1],
        //     [3, 5, 3, 6],
        //     [],
        //     []
        // ];
        
        
        
        // List<List<int>> bottles =
        // [
        //     [1, 5, 1, 5, 7, 4, 4, 2],
        //     [4, 6, 2, 3, 5, 2, 6, 1],
        //     [5, 7, 2, 6, 3, 4, 1, 2],
        //     [5, 2, 3, 7, 3, 6, 3, 1],
        //     [1, 1, 6, 6, 4, 2, 5, 7],
        //     [3, 3, 5, 7, 2, 4, 4, 5],
        //     [1, 7, 6, 6, 3, 7, 4, 7],
        //     // [],
        //     // []
        // ];

        // List<List<int>> bottles =
        // [
        //     [8, 2, 11, 5],
        //     [2, 10, 4, 2],
        //     [7, 3, 1, 5],
        //     [4, 11, 7, 13],
        //     [6, 13, 1, 3],
        //     [6, 4, 10, 7],
        //     [8, 11, 1, 9],
        //     [12, 7, 4, 2],
        //     [12, 6, 9, 5],
        //     [12, 3, 11, 1],
        //     [8, 9, 8, 6],
        //     [13, 9, 12, 5],
        //     [10, 3, 13, 10],
        //     // [],
        //     // []
        // ];
        //
        // List<List<int>> bottles =
        // [
        //     [1, 1, 2, 2],
        //     [1, 1],
        //     [3, 3, 2],
        //     [3, 3, 2]
        // ];
        
        // List<List<int>> bottles =  
        // [
        //     [6, 2, 5, 1],
        //     [4, 5, 4, 1],
        //     [5, 6, 4, 2],
        //     [3, 6, 2, 3],
        //     [1, 2, 4, 1],
        //     [3, 5, 3, 6],
        // ];
        // var bottleCapacity = 4;
        // List<int> extraEmptyConfig = [4, 3];
        
        List<List<int>> bottles =
        [
            [8, 2, 11, 5],
            [2, 10, 4, 2],
            [7, 3, 1, 5],
            [4, 11, 7, 13],
            [6, 13, 1, 3],
            [6, 4, 10, 7],
            [8, 11, 1, 9],
            [12, 7, 4, 2],
            [12, 6, 9, 5],
            [12, 3, 11, 1],
            [8, 9, 8, 6],
            [13, 9, 12, 5],
            [10, 3, 13, 10],
            // [],
            // []
        ];
        
        var bottleCapacity = 4;
        List<int> extraEmptyConfig = [3, 3];

        
        var tubes = bottles
            .Select(bottle => Tube.CreateNormalTube(bottleCapacity, bottle))
            .ToList();

        // List<int> emptyInfo = [6, 6];
        // List<int> emptyInfo = [0];
        List<Tube> emptyTubes = extraEmptyConfig.Select(capatity => Tube.CreateEmptyTube(capatity)).ToList();
        
        tubes.AddRange(emptyTubes);

        // --------------------
        // 0. 构造 state / solver
        // --------------------
        var solver = SolverFactory.CreateDefault();
        var state = new State(tubes);

        // --------------------
        // 1. warm-up + 正确性
        // --------------------
        bool solved = TrySolveOnce(solver, state, out var solution);

        Console.WriteLine($"Solved: {solved}");
        if  (solver.NodeCount > 0)
            Console.WriteLine($"CurVisitNodeCount: {solver.NodeCount}");
        Console.WriteLine($"StepCount: {solution?.Count ?? 0}");

        // 可选：只在这里打印解
        if (solution != null)
        {
            Console.WriteLine("---- solution ----");
            foreach (var m in solution)
                Console.WriteLine($"({m.From},{m.To}):  {m.Count}");
        }

        // --------------------
        // 2. 性能测试（纯计时）
        // --------------------
        double medianMs = MeasureSolveTimeMs(
            solver,
            state,
            repeat: 7
        );

        Console.WriteLine($"Solvers median time: {medianMs:F3} ms");
        
        
        
        static bool TrySolveOnce(
            Solver solver,
            State state,
            out IReadOnlyList<Move>? solution,
            bool stepByStep = false
        )
        {
            solution = null;

            foreach (var moves in solver.SolveDfsStack(state, stepByStep))
            {
                // 这里才 materialize，一次就好
                solution = moves.ToList();
                return true;
            }

            return false;
        }
        
        static double MeasureSolveTimeMs(
            Solver solver,
            State state,
            int repeat
        )
        {
            var times = new List<double>(repeat);

            for (int i = 0; i < repeat; i++)
            {
                var sw = Stopwatch.StartNew();

                foreach (var _ in solver.SolveDfsStack(state))
                {
                    break; // 只要第一条解
                }

                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            times.Sort();
            return times[times.Count / 2]; // 中位数
        }
    }

    static void Run(string name, Action test)
    {
        Console.WriteLine($"\n=== {name} ===");
        test();
    }

    // =========================
    // Test Cases
    // =========================

    static void TestCase1()
    {
        List<List<int>> bottles =
        [
            [1, 1, 2, 2],
            [3, 3, 4, 4],
            [2, 2, 3, 3],
            [4, 4, 1, 1],
            [],
            []
        ];

        var solver = new WaterSortSolver();
        var solutions = solver.Solve(bottles, extraEmptyBottleCount: 0).ToList();

        TestHelpers.ExpectHasSolution(solutions, "Simple solvable");
        TestHelpers.PrintFirstSolution(solutions);
    }

    static void TestCase2()
    {
        List<List<int>> bottles =  
        [
            [1, 2, 3, 4],
            [1, 2, 3, 4],
            [5, 6, 5, 6],
            [5, 6, 5, 6],
            [],
            []
        ];

        var solver = new WaterSortSolver();
        var solutions = solver.Solve(bottles, extraEmptyBottleCount: 0);

        TestHelpers.ExpectNoSolution(solutions, "Simple unsolvable");
    }

    static void TestCase3()
    {
        var solver = new WaterSortSolver();

        // 3.1 空瓶
        List<List<int>> empty = 
        [
            [],
            [],
            []
        ];

        var sol1 = solver.Solve(empty, extraEmptyBottleCount: 0);
        TestHelpers.ExpectSolvedInitially(sol1, "All empty bottles");

        // 3.2 已解决
        List<List<int>> solved = 
        [
            [1, 1, 1, 1],
            [2, 2, 2, 2],
            [3, 3, 3, 3]
        ];

        var sol2 = solver.Solve(solved, extraEmptyBottleCount: 0);
        TestHelpers.ExpectSolvedInitially(sol2, "Already solved");
    }

    static void TestCase4()
    {
        List<List<int>> bottles =  
        [
            [6, 2, 5, 1],
            [4, 5, 4, 1],
            [5, 6, 4, 2],
            [3, 6, 2, 3],
            [1, 2, 4, 1],
            [3, 5, 3, 6],
            [],
            []
        ];

        var solver = new WaterSortSolver();
        var solutions = solver.Solve(bottles, extraEmptyBottleCount: 0).ToList();

        TestHelpers.ExpectHasSolution(solutions, "Complex solvable");
        TestHelpers.PrintFirstSolution(solutions);
    }

    static void TestCase5()
    {
        List<List<int>> bottles =
        [
            [1, 5, 1, 5, 7, 4, 4, 2],
            [4, 6, 2, 3, 5, 2, 6, 1],
            [5, 7, 2, 6, 3, 4, 1, 2],
            [5, 2, 3, 7, 3, 6, 3, 1],
            [1, 1, 6, 6, 4, 2, 5, 7],
            [3, 3, 5, 7, 2, 4, 4, 5],
            [1, 7, 6, 6, 3, 7, 4, 7],
            [],
            []
        ];



        var solver = new WaterSortSolver();
        var solutions = solver.Solve(
            bottles,
            extraEmptyBottleCount: 0,
            bottleCapacity: 8,
            emptyBottleCapacity: 8
        ).ToList();

        TestHelpers.ExpectHasSolution(solutions, "Large capacity bottles");
        TestHelpers.PrintFirstSolution(solutions);
    }
}

/*
(0, 7), 1
(0, 8), 2
(2, 7), 1
(1, 2), 1
(4, 0), 1
(5, 4), 1
(5, 8), 2
(7, 5), 2
(0, 7), 2
(4, 0), 2
(4, 5), 1
(6, 7), 1
(8, 4), 3
(8, 6), 1
(0, 8), 3
(2, 0), 2
(3, 0), 1
(6, 2), 2
(6, 7), 1
(3, 6), 1
(3, 1), 1
(3, 6), 1
(7, 3), 4
(0, 7), 4
(8, 0), 3
(0, 8), 4
(0, 7), 1
(2, 0), 3
(2, 6), 1
(1, 2), 2
(4, 0), 4
(4, 2), 2
(4, 7), 2
(2, 4), 5
(1, 2), 1
(1, 8), 1
(5, 2), 4
(6, 1), 4
(6, 4), 2
(3, 5), 4
(3, 6), 1
(1, 3), 5
(5, 6), 5
(8, 5), 5
(1, 8), 1
(1, 4), 1
(1, 0), 1
(2, 1), 6
(8, 1), 1
(2, 8), 1
(5, 2), 6
(3, 5), 6
(3, 1), 1
(3, 2), 1
(6, 3), 7
(8, 3), 1
(6, 7), 1


Move { From = 0, To = 6, Color = 1, Count = 1 },
   Move { From = 0, To = 7, Color = 5, Count = 1 },
   Move { From = 2, To = 0, Color = 2, Count = 1 },
   Move { From = 1, To = 6, Color = 1, Count = 1 },
   Move { From = 1, To = 2, Color = 4, Count = 1 },
   Move { From = 1, To = 7, Color = 5, Count = 1 },
   Move { From = 2, To = 1, Color = 4, Count = 2 },
   Move { From = 5, To = 2, Color = 6, Count = 1 },
   Move { From = 3, To = 5, Color = 3, Count = 1 },
   Move { From = 3, To = 0, Color = 2, Count = 1 },
   Move { From = 2, To = 3, Color = 6, Count = 2 },
   Move { From = 2, To = 7, Color = 5, Count = 1 },
   Move { From = 0, To = 2, Color = 2, Count = 3 },
   Move { From = 3, To = 0, Color = 6, Count = 3 },
   Move { From = 5, To = 3, Color = 3, Count = 2 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 2, Color = 2, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 5, To = 4, Color = 5, Count = 1 },
   Move { From = 5, To = 3, Color = 3, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 2, Color = 2, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 2, Color = 2, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 3, To = 2, Color = 6, Count = 3 },
   Move { From = 5, To = 3, Color = 3, Count = 2 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 2, Color = 2, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 5, To = 4, Color = 3, Count = 2 },
   Move { From = 5, To = 2, Color = 3, Count = 2 },
   Move { From = 5, To = 2, Color = 3, Count = 2 },
   Move { From = 5, To = 2, Color = 3, Count = 2 },
   Move { From = 3, To = 2, Color = 6, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 4, To = 1, Color = 4, Count = 1 },
   Move { From = 0, To = 3, Color = 2, Count = 1 },
   Move { From = 0, To = 4, Color = 2, Count = 1 },
   Move { From = 2, To = 0, Color = 6, Count = 2 },
   Move { From = 2, To = 7, Color = 5, Count = 1 },
   Move { From = 4, To = 2, Color = 2, Count = 2 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 5, To = 2, Color = 3, Count = 2 },
   Move { From = 0, To = 2, Color = 6, Count = 1 },
   Move { From = 3, To = 4, Color = 2, Count = 1 },
   Move { From = 3, To = 0, Color = 2, Count = 1 },
   Move { From = 4, To = 0, Color = 2, Count = 2 },
   Move { From = 4, To = 6, Color = 1, Count = 1 },
   Move { From = 5, To = 4, Color = 3, Count = 2 },
   Move { From = 5, To = 7, Color = 5, Count = 1 },
   Move { From = 5, To = 4, Color = 3, Count = 1 },
   Move { From = 3, To = 0, Color = 2, Count = 2 },
   Move { From = 2, To = 3, Color = 6, Count = 2 },
   Move { From = 2, To = 5, Color = 6, Count = 1 },
   Move { From = 2, To = 5, Color = 6, Count = 3 },
   Move { From = 7, To = 5, Color = 5, Count = 2 },
   Move { From = 3, To = 0, Color = 2, Count = 2 },
   Move { From = 2, To = 3, Color = 6, Count = 2 },
   Move { From = 2, To = 7, Color = 6, Count = 1 },
   Move { From = 5, To = 2, Color = 5, Count = 3 },
   Move { From = 5, To = 4, Color = 3, Count = 1 },
   Move { From = 3, To = 5, Color = 6, Count = 3 },
   Move { From = 3, To = 4, Color = 3, Count = 1 },
   Move { From = 7, To = 5, Color = 6, Count = 1 }
   
*/