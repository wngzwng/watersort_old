using WaterSort.Core.Solvers.Obstacles;
namespace WaterSort.Core.Solvers;

public class Demo
{
   private Solver _solver;
   public bool stepByStep = false;
   
   public bool Run(State state, out List<Move> solutionMoves)
   {
      SetUp();
      
      var moveIter = _solver.SolveDfsStack(state, stepByStep);
      var moves = moveIter.FirstOrDefault();
      if (moves != null)
      {
         solutionMoves = moves.ToList();
         return true;
      }

      solutionMoves = null;
      return false;
   }
   
   public void SetUp()
   {
      // var explorer = new MoveGroupExplorer();
      var explorer = new MoveGroupExplorerWithObstacle();
      
      var moveActuator = new MoveActuator();
      var hasher = new CanonicalStateHasher();

      var obstacleUpdater = new ObstacleUpdater();

      moveActuator.OnApplyBefore += (state, move) =>
      {
         if (stepByStep)
            Console.WriteLine($"移动前 {move}");
         // if (move is { From: 10, To: 0 })
         // {
         //    Console.WriteLine($"移动前: \n{state}");
         // }
         //
         // if (move is { From: 0, To: 10 })
         // {
         //    Console.WriteLine($"移动前: \n{state}");
         // }
      };

      moveActuator.OnApplyAfter += (state, move) =>
      {
         if (stepByStep)
            Console.WriteLine($"移动后 {move}");
         // if (move is { From: 2, To: 5 })
         // {
         //    Console.WriteLine($"{move}\t移动后: \n{state}");
         // }
         // if (move is { From: 0, To: 10 })
         // {
         //    Console.WriteLine($"移动前: \n{state}");
         // }
         obstacleUpdater.UpdateInPlace(state, move);
      };
      
      _solver = new Solver(explorer, moveActuator, hasher);
   }


   public static bool Test(
      List<List<int>> bottles, 
      int bottleCapacity, 
      out List<Move> solutionMoves,
      out long nodeCount,
      List<int>? extraEmptyConfig = null,
      IEnumerable<ObstacleEntry>? obstacleEntries = null,
      bool stepByStep = false
      )
   {
      
      var state = CreateState(bottles, bottleCapacity, extraEmptyConfig, obstacleEntries);

      var solver = new Demo();
      solutionMoves = null;
      solver.stepByStep = stepByStep;
      if (solver.Run(state, out solutionMoves))
      {
         nodeCount = solver._solver.NodeCount;
         return true;
      }
      nodeCount = solver._solver.NodeCount;
      return false;
   }

   public static bool Test(
      List<List<int>> bottles,
      List<int> capacities,
      out List<Move> solutionMoves,
      bool stepByStep = false
   )
   {
      var tubes = bottles
         .Select((bottle, index) => Tube.CreateTube(bottle, capacities[index]))
         .ToList();
      var state = new State(tubes);
      
      var solver = new Demo();
      solutionMoves = null;
      solver.stepByStep = stepByStep;
      if (solver.Run(state, out solutionMoves))
      {
         return true;
      }
      return false;
   }

   public static State CreateState(List<List<int>> bottles, int bottleCapacity, List<int>? extraEmptyConfig = null, IEnumerable<ObstacleEntry>? entries = null)
   {
      var tubes = bottles
         .Select(bottle => Tube.CreateTube(bottle, bottleCapacity))
         .ToList();

      if (extraEmptyConfig != null && extraEmptyConfig.Count > 0 && extraEmptyConfig.All(capacity => capacity > 0))
      {
         List<Tube> emptyTubes = extraEmptyConfig.Select(Tube.CreateEmptyTube).ToList();
         tubes.AddRange(emptyTubes);
      }

      var state = new State(tubes, entries);
      return state;
   }
   
}