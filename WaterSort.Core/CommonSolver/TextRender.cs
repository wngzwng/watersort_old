using System.Text;

namespace WaterSort.Core.CommonSolver;

public static class TextRender
{
    public static void Title(StringBuilder sb, string title)
        => AppendTitle(sb, title, 40);

    public static void Divider(StringBuilder sb, int width = 40)
        => sb.Append('=', width);

    private static void AppendTitle(
        StringBuilder sb,
        string title,
        int totalWidth,
        char fill = '=')
    {
        var content = $" {title} ";
        int pad = totalWidth - content.Length;
        int left = Math.Max(0, pad / 2);
        int right = Math.Max(0, pad - left);

        sb.Append(fill, left);
        sb.Append(content);
        sb.Append(fill, right);
    }
}
