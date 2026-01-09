using System.Text;

namespace WaterSort.Core.CommonSolver;

public sealed class CanonicalStateHasher : IStateHasher
{
    
    public bool OpenVisual { get; set; }

    public CanonicalStateHasher()
    {
        OpenVisual = false;
    }
    public StateKey BuildKey(State state)
    {
        // Normalize-1：位置同构
        // 核心思想：构造 TubeSignature，然后排序 signature

        // ─────────────────────────
        // 1. 分离三类 tube
        // ─────────────────────────

        var nonMonoTopBoundaries = new List<int>();
        var monoAndEmpty = new List<TubeSignature>();

        for (int i = 0; i < state.Tubes.Count; i++)
        {
            var tube = state.Tubes[i];

            if (tube.IsEmpty || tube.IsMonochrome)
            {
                // 单色瓶 / 空瓶：位置无关
                monoAndEmpty.Add(TubeSignature.From(tube));
            }
            else
            {
                // 非单色瓶：位置相关，只取顶部边界
                nonMonoTopBoundaries.Add(BuildTopBoundary(tube));
            }
        }
        // 打印
        if (OpenVisual)
            Console.WriteLine(Render(nonMonoTopBoundaries, monoAndEmpty));

        // ─────────────────────────
        // 2. 排序
        // ─────────────────────────

        // 2.1 非单色瓶：按 tube_index（天然已有顺序，这里不再 sort）
        // nonMonos 已按 index 插入

        // 2.2 单色瓶 & 空瓶：按 TubeSignature 排序
        monoAndEmpty.Sort();

        // ─────────────────────────
        // 3. 组合 hash
        // ─────────────────────────

        var hash = new HashCode();

        // 非单色瓶：顺序敏感
        foreach (var sig in nonMonoTopBoundaries)
            hash.Add(sig);

        // 单色瓶 & 空瓶：顺序无关（已排序）
        foreach (var sig in monoAndEmpty)
            hash.Add(sig);

        return new StateKey(hash.ToHashCode());
    }

    private int BuildTopBoundary(Tube tube)
    {
        int topBoundary = 0;
        // for (int i = tube.Count - 1; i > 0; i--)
        // {
        //     if (tube.Cells[i] != tube.Cells[i - 1])
        //         topBoundary = i;
        // }
        // 顶部边界，我要最后那一个
        for (int i = 0; i < tube.Count - 1; i++)
        {
            if (tube.Cells[i] != tube.Cells[i + 1])
                topBoundary = i + 1;
        }
        return topBoundary;
    }

    private string Render(List<int> nonMonoTopBoundaries, List<TubeSignature> monoAndEmpty)
    {
        var sb = new StringBuilder();
        TextRender.Title(sb, "hash详情");
        sb.AppendLine($"\nnonMonoTopBoundaries: {string.Join(",", nonMonoTopBoundaries)}");
        foreach (var tubeSignature in monoAndEmpty)
        {
            // sb.AppendLine(
                // $"type: {tubeSignature.Type} count: {tubeSignature.Height} color: {tubeSignature.Color} capacity: {tubeSignature.Capacity}");
            sb.AppendLine(
                $"type: {tubeSignature.Type} color: {tubeSignature.Color} capacity: {tubeSignature.Capacity}");
        }
        TextRender.Divider(sb);
        return sb.ToString();
    }
}
