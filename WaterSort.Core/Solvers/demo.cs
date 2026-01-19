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
      var explorer = new MoveGroupExplorer();
      var moveActuator = new MoveActuator();
      var hasher = new CanonicalStateHasher();

      moveActuator.OnApplyBefore += (state, move) =>
      {
         if (stepByStep)
            Console.WriteLine($"移动前 {move}");
      };

      moveActuator.OnApplyAfter += (state, move) =>
      {
         if (stepByStep)
            Console.WriteLine($"移动后 {move}");
      };
      
      _solver = new Solver(explorer, moveActuator, hasher);
   }


   public static bool Test(
      List<List<int>> bottles, 
      int bottleCapacity, 
      out List<Move> solutionMoves,
      List<int>? extraEmptyConfig = null,
      bool stepByStep = false
      )
   {
      
      var state = CreateState(bottles, bottleCapacity, extraEmptyConfig);

      var solver = new Demo();
      solutionMoves = null;
      solver.stepByStep = stepByStep;
      if (solver.Run(state, out solutionMoves))
      {
         return true;
      }
      return false;
   }

   public static State CreateState(List<List<int>> bottles, int bottleCapacity, List<int>? extraEmptyConfig = null)
   {
      var tubes = bottles
         .Select(bottle => Tube.CreateTube(bottle, bottleCapacity))
         .ToList();

      if (extraEmptyConfig != null && extraEmptyConfig.Count > 0 && extraEmptyConfig.All(capacity => capacity > 0))
      {
         List<Tube> emptyTubes = extraEmptyConfig.Select(Tube.CreateEmptyTube).ToList();
         tubes.AddRange(emptyTubes);
      }

      var state = new State(tubes);
      return state;
   }
   
}