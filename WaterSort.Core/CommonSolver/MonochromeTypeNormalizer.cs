namespace WaterSort.Core.CommonSolver;

public sealed class MonochromeTypeNormalizer : IStateNormalizer
{
    public IEnumerable<MoveGroup> Normalize(State state)
    {
        // 1. 识别同类型、同颜色、同高度的单色瓶
        // 2. 生成一个“规范代表状态”
        // 3. 不发生任何颜色迁移
        // 4. 不改变 tube 语义类型

        // ⚠️ 可改变 State 表示，但必须语义等价
        
        // 颜色聚合条件， 腾出空瓶
        /*
         * 两轮聚合：
         * 1. 聚合目的。 能够腾出空瓶
         * 情况 1：容量少的单色瓶 -> 多的单色瓶 (同类型的瓶子)
         * 情况 2：单色瓶 -> 非单色顶部空间（无限制）
         * 情况 3: 单色瓶 -> 聚合 + 非顶部空间 (暂不考虑)
         */
        
        /*
         * MVP 版本。   UNP 关卡 等长无道具关卡
         * 1. 所有单色瓶类型一致
         * 2. 作用单元： 颜色（颜色要么单色瓶聚合，要么去非单色瓶顶部空间
         *
         * 特点： 空瓶不参与其中
         * 1. 根据 TubeColorAvailability 得出 color 集合
         * 2. 根据是否聚合 排除不可聚合的颜色
         * 3. 优先单色瓶聚合
         * 4. 在非单色瓶顶部空间处理
         */
        
        // 复制 tubes（Normalize-2 允许改变 State 表示）
        // var tubes = state.Tubes
        //     .Select(Tube.DeepCopy) // 必须是深拷贝
        //     .ToList();

        // 1. 可参与 Normalize 的 availability（排除空瓶 / 满瓶）
    var availabilities = AvailabilityScanner.Scan(state)
        .Where(a => a.ExportCount > 0 && a.AcceptCount > 0)
        .ToList();

    // 2. 按颜色分组
    var byColor = availabilities.GroupBy(a => a.Color);

    var moveGroups = new List<MoveGroup>();

    foreach (var colorGroup in byColor)
    {
        // --- 单色瓶 ---
        var monos = colorGroup
            .Where(a => state.Tubes[a.TubeIndex].IsMonochrome)
            .OrderByDescending(a => a.ExportCount)
            .ToList();

        if (monos.Count == 0)
            continue;

        // ─────────────────────────────
        // Case 1: 多个单色瓶 → 单色瓶聚合
        // ─────────────────────────────
        if (monos.Count > 1)
        {
            var target = monos[0];
            int remain = target.AcceptCount;

            var moves = new List<Move>();

            foreach (var from in monos.Skip(1))
            {
                if (remain <= 0)
                    break;

                int moveCount = Math.Min(from.ExportCount, remain);
                if (moveCount <= 0)
                    continue;

                moves.Add(new Move(
                    from: from.TubeIndex,
                    to: target.TubeIndex,
                    color: target.Color,
                    count: moveCount
                ));

                remain -= moveCount;
            }

            if (moves.Count > 0)
                moveGroups.Add(
                    new MoveGroup(MoveGroupKind.NormalizeMonochrome, moves)
                );

            continue;
        }

        // ─────────────────────────────
        // Case 2: 单色瓶 → 非单色瓶顶部空间
        // ─────────────────────────────
        var nonMonos = colorGroup
            .Where(a => !state.Tubes[a.TubeIndex].IsMonochrome)
            .ToList();

        if (nonMonos.Count == 0)
            continue;

        int acceptTotal = nonMonos.Sum(a => a.AcceptCount);
        var fromMono = monos[0];

        if (fromMono.ExportCount > acceptTotal)
            continue;

        int remain1 = fromMono.ExportCount;
        var moves1 = new List<Move>();

        foreach (var to in nonMonos)
        {
            if (remain1 <= 0)
                break;

            int moveCount = Math.Min(remain1, to.AcceptCount);
            if (moveCount <= 0)
                continue;

            moves1.Add(new Move(
                from: fromMono.TubeIndex,
                to: to.TubeIndex,
                color: to.Color,
                count: moveCount
            ));

            remain1 -= moveCount;
        }

        if (moves1.Count > 0)
            moveGroups.Add(
                new MoveGroup(MoveGroupKind.NormalizeMonochrome, moves1)
            );
    }
    
    if (moveGroups.Count == 0)
        return Enumerable.Empty<MoveGroup>();
   
    return moveGroups;

    // // 3. 统一 Apply（Normalize-2）
    // var nextState = state.DeepClone();
    //
    // foreach (var group in moveGroups)
    // {
    //     nextState.Apply(group);
    // }
    //
    // return nextState;
    }
}
