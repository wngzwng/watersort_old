using System;
using System.Collections.Generic;
using System.Linq;
using WaterSort.Core.WaterSortSolver.Domain;

static class TestHelpers
{
    public static void ExpectHasSolution(
        IEnumerable<List<Move>> solutions,
        string caseName)
    {
        if (solutions == null || !solutions.Any())
            Fail(caseName, "期望有解，但未找到解");

        Pass(caseName);
    }

    public static void ExpectNoSolution(
        IEnumerable<List<Move>> solutions,
        string caseName)
    {
        if (solutions != null && solutions.Any())
            Fail(caseName, "期望无解，但找到了方案");

        Pass(caseName);
    }

    public static void ExpectSolvedInitially(
        IEnumerable<List<Move>> solutions,
        string caseName)
    {
        if (solutions == null)
            Fail(caseName, "返回 null");

        if (solutions.Any() && solutions.First().Count != 0)
            Fail(caseName, "已解决状态不应产生 move");

        Pass(caseName);
    }

    public static void PrintFirstSolution(
        IEnumerable<List<Move>> solutions)
    {
        var sol = solutions.FirstOrDefault();
        if (sol == null) return;

        Console.WriteLine("Solution:");
        Console.WriteLine(string.Join(" -> ", sol.Select(m => m.FromBottle)));
    }

    private static void Pass(string name)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[PASS] {name}");
        Console.ResetColor();
    }

    private static void Fail(string name, string reason)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAIL] {name}: {reason}");
        Console.ResetColor();
        throw new Exception($"Test failed: {name}");
    }
}
