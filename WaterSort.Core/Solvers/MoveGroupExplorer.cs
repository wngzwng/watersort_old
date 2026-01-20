using System.ComponentModel;
using System.Numerics;

namespace WaterSort.Core.Solvers;

public sealed class ColorBucket
{
    public int Color { get; }

    private readonly HashSet<int> _tubeIndexSet = new();
    public List<TubeMoveAbility> Abilities { get; } = new();

    public List<TubeMoveAbility> FromNonMonos { get; } = new();
    public List<TubeMoveAbility> FromMonos { get; } = new();
    public List<TubeMoveAbility> ToNonMonos { get; } = new();
    public List<TubeMoveAbility> ToMonos { get; } = new();

    public ColorBucket(int color) => Color = color;

    public void Add(TubeMoveAbility a)
    {
        var related =
            (a.ExportCount > 0 && a.ExportColor == Color)
            || (a.AcceptCount > 0 && a.AcceptColor == Color)
            || (a.AcceptCount > 0 && a.AcceptColor == Tube.COLOR_EMPTY);

        if (!related) return;

        if (!_tubeIndexSet.Add(a.TubeIndex)) return;

        Abilities.Add(a);
    }

    public void GroupBy(State state)
    {
        FromNonMonos.Clear();
        FromMonos.Clear();
        ToNonMonos.Clear();
        ToMonos.Clear();

        foreach (var a in Abilities)
        {
            var isMono = state.Tubes[a.TubeIndex].IsMonochrome;

            if (a.ExportCount > 0 && a.ExportColor == Color)
            {
                if (isMono) FromMonos.Add(a);
                else FromNonMonos.Add(a);
            }

            if (a.AcceptCount > 0 && (a.AcceptColor == Color || a.AcceptColor == Tube.COLOR_EMPTY))
            {
                if (isMono) ToMonos.Add(a);
                else ToNonMonos.Add(a);
            }
        }
    }
    
    public static Dictionary<int, ColorBucket> BuildBuckets(IEnumerable<TubeMoveAbility> abilities)
    {
        var list = abilities.ToList();
        var buckets = new Dictionary<int, ColorBucket>();

        // Step1: 根据 ExportColor + ExportCount > 0 建桶
        foreach (var a in list)
        {
            if (a.ExportCount <= 0) continue;
            if (a.ExportColor == Tube.COLOR_EMPTY) continue;

            if (!buckets.TryGetValue(a.ExportColor, out var bucket))
            {
                bucket = new ColorBucket(a.ExportColor);
                buckets.Add(a.ExportColor, bucket);
            }

            bucket.Add(a);
        }

        // Step2: 分配 tube（同色 + 空瓶可接任意色）
        foreach (var a in list)
        {
            if (a.ExportCount <= 0 && a.AcceptCount <= 0)
                continue;

            // from: ExportColor 桶
            if (a.ExportCount > 0 && buckets.TryGetValue(a.ExportColor, out var fromBucket))
                fromBucket.Add(a);

            // to: AcceptColor 桶（非 empty）
            if (a.AcceptCount > 0 && a.AcceptColor != Tube.COLOR_EMPTY
                                  && buckets.TryGetValue(a.AcceptColor, out var toBucket))
                toBucket.Add(a);

            // to: empty => 加入所有桶
            if (a.AcceptCount > 0 && a.AcceptColor == Tube.COLOR_EMPTY)
            {
                foreach (var bucket in buckets.Values)
                    bucket.Add(a);
            }
        }

        return buckets;
    }
    
}


public class MoveGroupExplorer
{
    public IEnumerable<MoveGroup> Explore(State state)
    {
        var abilities = state.BuildTubeAbilities();
        // Step1 + Step2：按 ExportColor 建桶 + 分配 tube 到桶
        var buckets = ColorBucket.BuildBuckets(abilities);

        foreach (var bucket in buckets.Values)
        {
            bucket.GroupBy(state);

            foreach (var g in ExploreBucket(state, bucket))
                yield return g;
        }
    }


    public IEnumerable<MoveGroup> Normal(State state)
    {
        var abilities = state.BuildTubeAbilities();
        // Step1 + Step2：按 ExportColor 建桶 + 分配 tube 到桶
        var buckets = ColorBucket.BuildBuckets(abilities);

        foreach (var bucket in buckets.Values)
        {
            bucket.GroupBy(state);

            foreach (var g in NormalCore(state, bucket))
                yield return g;
            
        }
    }

    private IEnumerable<MoveGroup> NormalCore(State state, ColorBucket bucket)
    {
        bool CanFreeable(TubeMoveAbility from, int allAcceptCount)
        {
            return from.ExportCount <= (allAcceptCount - from.AcceptCount);
        }

        var monos = bucket.Abilities
            .Where(a =>
                a.ExportCount > 0 &&
                a.AcceptCount > 0 &&
                state.Tubes[a.TubeIndex].IsMonochrome
            )
            .ToList();

        if (monos.Count <= 1)
            yield break;

        int allAcceptCount = monos.Sum(x => x.AcceptCount);

        // 1) From-first：优先找要腾空的瓶（export 少的优先）
        var freeSource = monos
            .Where(from => CanFreeable(from, allAcceptCount))
            .OrderBy(from => from.ExportCount)
            .ThenBy(from => from.TubeIndex)
            .FirstOrDefault();

        if (freeSource == null)
            yield break;

        // 2) targets：排除 freeSource，自身按 AcceptCount 大的优先（更容易接满）
        var targets = monos
            .Where(a => a.TubeIndex != freeSource.TubeIndex)
            .OrderByDescending(a => a.AcceptCount)
            .ThenBy(a => a.TubeIndex)
            .ToList();

        // 必须有足够的总接收容量才能腾空
        if (targets.Sum(x => x.AcceptCount) < freeSource.ExportCount)
            yield break;

        // 3) 构造 moveGroup：必须 full-drain freeSource
        var moveGroup = BuildMoveGroupGreedy(freeSource, targets, freeSource.ExportCount);

        if (moveGroup == null || moveGroup.Moves == null || moveGroup.Moves.Count == 0)
            yield break;

        // 强校验：必须把 freeSource 全倒完，否则“不能腾空”= 没意义
        int moved = moveGroup.Moves.Sum(m => m.Count);
        if (moved < freeSource.ExportCount)
            yield break;

        moveGroup.Description = $"颜色({bucket.Color})的规整化（腾空 {freeSource.TubeIndex}）";
        yield return moveGroup;
    }
    
    // private IEnumerable<MoveGroup> NormalCore(State state, ColorBucket bucket)
    // {
    //     var fromMonos = bucket.FromMonos;
    //     var toMonos = bucket.ToMonos;
    //     
    //     var monoGroupsMap = GroupMonoByType(toMonos, state);
    //     // 相同了类型的转换
    //     foreach (var from in fromMonos)
    //     {
    //         if (state.Tubes[from.TubeIndex].IsEmpty)
    //             continue;
    //         
    //         // from 所在的 mono 组（同类型组）
    //         var includeMonos = monoGroupsMap
    //             .FirstOrDefault(g => g.Any(x => x.TubeIndex == from.TubeIndex));
    //
    //         if (includeMonos == null || includeMonos.Count <= 1)
    //         {
    //             continue;
    //         }
    //         
    //         // 剔除空瓶
    //         var includeMonosWithOutEmpty = includeMonos.Where(ability 
    //                     => !state.Tubes[ability.TubeIndex].IsEmpty).ToList();
    //         
    //         if (includeMonosWithOutEmpty.Count <= 1)
    //         {
    //             continue;
    //         }
    //         
    //         // 先得到所有可接收 mono to（桶内同色，Select 只负责排除自己/AcceptCount>0）
    //         var monoTos = SelectTos(from, includeMonosWithOutEmpty);
    //         if (monoTos.Count <= 0 || monoTos.Sum(x => x.AcceptCount) < from.ExportCount)
    //             continue;
    //         
    //         var moveGroup =  BuildMoveGroupGreedy(from, monoTos, from.ExportCount);
    //         moveGroup.Description = "规整化";
    //         yield return moveGroup;
    //     }
    // }
    
    
    // ------------------------------------------------------------
    // Step3: 桶内策略层（只做 1 / 1+2 / 2）
    // ------------------------------------------------------------
    private IEnumerable<MoveGroup> ExploreBucket(State state, ColorBucket bucket)
    {
        var fromNonMonos = bucket.FromNonMonos;
        var fromMonos = bucket.FromMonos;
        var toNonMonos = bucket.ToNonMonos;
        var toMonos = bucket.ToMonos;
        

        // ========== 1) nonMono -> nonMono ==========
        foreach (var from in fromNonMonos)
        {
            var nonTos = SelectTos(from, toNonMonos);
            if (nonTos.Count <= 0) continue;

            if (nonTos.Sum(x => x.AcceptCount) < from.ExportCount)
                continue;
            var moveGroup = BuildMoveGroupGreedy(from, nonTos, from.ExportCount);
            moveGroup.Description = " 1) nonMono -> nonMono";
            yield return moveGroup;
        }

        // ========== 1 + 2) nonMono -> nonMono + mono ==========
        foreach (var from in fromNonMonos)
        {
            var nonTos = SelectTos(from, toNonMonos);
            if (nonTos.Count <= 0) continue;

            var nonCap = nonTos.Sum(x => x.AcceptCount);

            // non-mono 已经能装下全部 => 属于 1，不属于 1+2
            if (nonCap >= from.ExportCount)
                continue;

            var monoTos = SelectTos(from, toMonos);
            if (monoTos.Count <= 0) continue;

            var monoCap = monoTos.Sum(x => x.AcceptCount);

            // non + mono 也不够 => 该色无法移动
            if (nonCap + monoCap < from.ExportCount)
                continue;

            // 先填 non-mono
            var prefix = new MoveGroup();
            var remain = from.ExportCount;
            AppendMovesGreedy(prefix, from, nonTos, ref remain);

            if (remain <= 0)
                continue;

            // mono 分叉：按类型分组（目前按 capacity 分组，你后面可升级为 capacity+kind）
            var monoGroups = GroupMonoByType(monoTos, state);

            foreach (var monos in monoGroups)
            {
                if (monos.Sum(x => x.AcceptCount) < remain)
                    continue;

                var g = prefix.DeepCopy();
                var r = remain;
                AppendMovesGreedy(g, from, monos, ref r);

                if (r == 0)
                {
                    g.Description = "1 + 2) nonMono -> nonMono + mono";
                    yield return g;
                    
                }
            }
        }

        // ========== 2) nonMono -> mono ==========
        foreach (var from in fromNonMonos)
        {
            // 如果存在 non-mono 可接，则不是纯 2
            var nonTos = SelectTos(from, toNonMonos);
            if (nonTos.Count > 0)
                continue;

            var monoTos = SelectTos(from, toMonos);
            if (monoTos.Count <= 0)
                continue;

            if (monoTos.Sum(x => x.AcceptCount) < from.ExportCount)
                continue;

            var monoGroups = GroupMonoByType(monoTos, state);

            foreach (var monos in monoGroups)
            {
                if (monos.Sum(x => x.AcceptCount) < from.ExportCount)
                    continue;

                var g = BuildMoveGroupGreedy(from, monos, from.ExportCount);

                // 可选保险：确保填满
                if (g.Moves.Sum(m => m.Count) == from.ExportCount)
                {
                    g.Description = "2) nonMono -> mono";
                    yield return g; 
                }
                   
            }
        }
        
        // ========== 3) mono -> nonMono ==========
        foreach (var from in fromMonos)
        {
            var nonTos = SelectTos(from, toNonMonos);
            if (nonTos.Count <= 0) 
                continue;
            
            if (nonTos.Sum(x => x.AcceptCount) < from.ExportCount)
                continue;

            var moveGroup = BuildMoveGroupGreedy(from, nonTos, from.ExportCount);
            moveGroup.Description = "3) mono -> nonMono ";
            yield return moveGroup; 
        }
        
        // ========== 4) mono -> mono ==========  不同类型的转换 这里不能是tomonos，而是这个颜色桶的所有瓶子分类
        // var monoGroupsMap = GroupMonoByType(toMonos, state); 
        var monoGroupsMap = GroupMonoByType(bucket.Abilities, state);
        if (monoGroupsMap.Count < 2) yield break;
        
        foreach (var from in fromMonos)
        {
            // 先得到所有可接收 mono to（桶内同色，Select 只负责排除自己/AcceptCount>0）
            var monoTos = SelectTos(from, toMonos);
            if (monoTos.Count <= 0)
                continue;

            // from 所在的 mono 组（同类型组）
            var excludeMonos = monoGroupsMap
                .FirstOrDefault(g => g.Any(x => x.TubeIndex == from.TubeIndex));

            // 逐个“不同类型组”尝试（每个组一个分叉）
            foreach (var group in monoGroupsMap)
            {
                // 排除同类型组
                if (excludeMonos != null && ReferenceEquals(group, excludeMonos))
                    continue;

                // 这个 group 内部也要满足：能接这个 from（排除自己）
                var groupTos = SelectTos(from, group);
                if (groupTos.Count <= 0)
                    continue;

                // 检测容纳能力（是否 ok）
                if (groupTos.Sum(x => x.AcceptCount) < from.ExportCount)
                    continue;

                // ok：产出一个 MoveGroup（只用这个 group 的 tos 来接）
                var moveGroup = BuildMoveGroupGreedy(from, groupTos, from.ExportCount);
                moveGroup.Description = "   4.1 ) mono -> mono 不同类型";
                yield return moveGroup; 
                // yield return BuildMoveGroupGreedy(from, groupTos, from.ExportCount);
            }
        }
    }
    
    // ------------------------------------------------------------
    // 通用过程：选择/构造 MoveGroup
    // ------------------------------------------------------------

    /// <summary>
    /// 桶内同色，所以只需要排除自己 + AcceptCount > 0
    /// </summary>
    private static List<TubeMoveAbility> SelectTos(TubeMoveAbility from, IEnumerable<TubeMoveAbility> tos)
    {
        var list = new List<TubeMoveAbility>();

        foreach (var to in tos)
        {
            if (to.AcceptCount <= 0) continue;
            if (to.TubeIndex == from.TubeIndex) continue;
            list.Add(to);
        }

        return list;
    }

    private static MoveGroup BuildMoveGroupGreedy(
        TubeMoveAbility from,
        IEnumerable<TubeMoveAbility> tos,
        int needCount)
    {
        var g = new MoveGroup();
        var remain = needCount;
        AppendMovesGreedy(g, from, tos, ref remain);
        return g;
    }

    private static void AppendMovesGreedy(
        MoveGroup group,
        TubeMoveAbility from,
        IEnumerable<TubeMoveAbility> tos,
        ref int remainCount)
    {
        foreach (var to in tos)
        {
            if (remainCount <= 0)
                break;

            if (to.AcceptCount <= 0)
                continue;

            if (to.TubeIndex == from.TubeIndex)
                continue;

            var c = Math.Min(remainCount, to.AcceptCount);
            if (c <= 0)
                continue;

            group.Moves.Add(new Move
            {
                From = from.TubeIndex,
                To = to.TubeIndex,
                Count = c,
                Color = from.ExportColor
            });

            remainCount -= c;
        }
    }
    
    
    /// <summary>
    /// mono 的“类型分组”：当前按 capacity 分组
    /// 后续你可以升级成 (capacity, kind) 作为 key
    /// </summary>
    private static List<List<TubeMoveAbility>> GroupMonoByType(List<TubeMoveAbility> abilities, State state)
    {
        return abilities
            .GroupBy(a => state.Tubes[a.TubeIndex].Capacity)
            .Select(g => g.ToList())
            .ToList();
    }
}

