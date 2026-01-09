namespace WaterSort.Core.CommonSolver;

public sealed class State
{
    public IReadOnlyList<Tube> Tubes { get; }

    public State(IReadOnlyList<Tube> tubes)
    {
        Tubes = tubes;
    }

    public State Apply(MoveGroup group)
    {
        // ⚠️ 只允许真实状态变化
        // 这里不做 Normalize、不做 Hash
        return group.Apply(this);
    }

    public State DeepClone()
    {
        return new State(Tubes.Select(Tube.DeepCopy).ToList());
    }


    // ─────────────────────────────────────────
    // Render / Visualize
    // ─────────────────────────────────────────
    public string Render(bool hexMode = false, bool showIndex = true)
    {
        if (Tubes.Count == 0)
            return "[空盘面]";

        int maxCapacity = Tubes.Max(t => t.Capacity);
        var lines = new List<string>();

        // 从顶部到底部渲染
        for (int layer = maxCapacity - 1; layer >= 0; layer--)
        {
            var cells = new List<string>();

            foreach (var tube in Tubes)
            {
                bool isCapacityLine = layer == tube.Capacity - 1;
                bool hasColor = layer < tube.Cells.Length;

                if (hasColor)
                {
                    int color = tube.Cells[layer];
                    string colorStr = hexMode
                        ? color.ToString("X2")
                        : color.ToString().PadLeft(2);

                    // cells.Add(colorStr);
                    cells.Add(AnsiColor.Colorize(color, colorStr))
                        
                        ;
                }
                else
                {
                    if (isCapacityLine)
                        cells.Add(" -");
                    else
                        cells.Add("  ");
                }
            }

            lines.Add(string.Join(" ", cells));
        }

        if (showIndex)
        {
            int width = 3 * Tubes.Count - 1;
            lines.Add(new string('-', width));
            lines.Add(string.Join(" ",
                Enumerable.Range(0, Tubes.Count)
                    .Select(i => i.ToString().PadLeft(2))));
        }

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString()
    {
        return Render(true);
    }
}
