using WaterSort.Core.WaterSortSolver.Domain;
using WaterSort.Core.WaterSortSolver.Search;

namespace WaterSort.Core.WaterSortSolver.Solver;

    public class WaterSortSolver
    {
        public IEnumerable<List<Move>> Solve(
            List<List<int>> waterData,
            int extraEmptyBottleCount,
            int bottleCapacity = 4,
            int emptyBottleCapacity = 4,
            bool findAll = false,
            int? maxDepth = null
        )
        {
            var (state, ctx) = BuildInitial(waterData, extraEmptyBottleCount, bottleCapacity, emptyBottleCapacity);
            return new DepthFirstSearch().Search(state, ctx, findAll, maxDepth);
        }

        private static (WaterSortState, WaterSortContext) BuildInitial(
            List<List<int>> waterData,
            int extraEmptyBottleCount,
            int bottleCapacity,
            int emptyBottleCapacity
        )
        {
            var data = waterData.Select((b => b.ToList())).ToList();
            for (int i = 0; i < extraEmptyBottleCount; i++)
                data.Add(new List<int>());

            var boundaries = BuildBoundaries(data);
            
            var released = new Dictionary<int, int>();
            var spaces = new Dictionary<int, int>();
            var topIndex = new List<int>();

            for (int i = 0; i < data.Count; i++)
            {
                var bottle = data[i];
                var b = boundaries[i];
                int idx = b.Count - 1;
                topIndex.Add(idx);

                int boundary = b[idx];
                for (int j = boundary; j < bottle.Count; j++)
                    released[bottle[j]] = released.GetValueOrDefault(bottle[j]) + 1;

                if (idx > 0) // 非单色瓶
                {
                    // 获取顶部颜色
                    int color = bottle[boundary];
                    // 更新非单色瓶中的 顶部空间
                    spaces[color] = spaces.GetValueOrDefault(color) + (bottleCapacity - boundary);
                }
            }

            return (
                new WaterSortState(topIndex, released, spaces),
                new WaterSortContext(data, boundaries, bottleCapacity, emptyBottleCapacity)
            );
        }

        private static List<List<int>> BuildBoundaries(List<List<int>> waterData)
        {
            var result = new List<List<int>>();
            foreach (var bottle in waterData)
            {
                var list = new List<int> { 0 };
                for (int i = 0; i < bottle.Count - 1; i++)
                {
                    if (bottle[i] != bottle[i + 1])
                    {
                        list.Add(i + 1);
                    }
                }
                result.Add(list);
            }

            return result;
        }
    }