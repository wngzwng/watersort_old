using System.Globalization;
using CsvHelper;

namespace WaterSort.Core.Util;

public static class CsvUtils
{
    public static void WriteDictCsv(
        string file,
        IEnumerable<OrderedDictionary<string, object?>> rows,
        IEnumerable<string>? columns = null,
        Dictionary<string, Func<object?, string>>? converters = null,
        Dictionary<string, string>? rename = null)
    {
        // 自动收集所有字段
        // var allKeys = (columns ?? rows
        //         .SelectMany(r => r.Keys)
        //         .Distinct()
        //         .OrderBy(k => k))
        //     .ToList();
        
        List<string> allKeys;

        if (columns != null)
        {
            // 使用 columns 的顺序
            allKeys = columns.ToList();
        }
        else
        {
            // 从 rows 中按出现顺序收集所有字段
            var seen = new HashSet<string>();
            allKeys = new List<string>();

            foreach (var row in rows)
            {
                foreach (var key in row.Keys)
                {
                    if (seen.Add(key))
                        allKeys.Add(key);
                }
            }
        }

        using var writer = new StreamWriter(file);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // 写表头（支持 rename）
        foreach (var key in allKeys)
        {
            var headerName = rename != null && rename.TryGetValue(key, out var newName)
                ? newName
                : key;

            csv.WriteField(headerName);
        }
        csv.NextRecord();

        // 写数据
        foreach (var row in rows)
        {
            foreach (var key in allKeys)
            {
                row.TryGetValue(key, out var value);

                if (converters != null &&
                    converters.TryGetValue(key, out var conv))
                {
                    csv.WriteField(conv(value));
                }
                else
                {
                    csv.WriteField(value);
                }
            }

            csv.NextRecord();
        }

        Console.WriteLine($"\n Saved To: {file}");
    }
    
    public static IEnumerable<OrderedDictionary<string, object?>> ReadDictCsv(
        string file,
        Dictionary<string, Func<string, object?>>? converters = null,
        Dictionary<string, string>? rename = null)
    {
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord!;

        while (csv.Read())
        {
            var row = new OrderedDictionary<string, object?>();

            foreach (var header in headers)
            {
                // 1. 读取原始文本
                var raw = csv.GetField(header);

                // 2. 重命名：CSV 列名 → 内部 key
                var key = header;
                if (rename != null && rename.TryGetValue(header, out var mapped))
                    key = mapped;

                // 3. 指定转换器
                if (converters != null && converters.TryGetValue(key, out var conv))
                {
                    row[key] = conv(raw);
                }
                else
                {
                    row[key] = raw; // 默认原样给 string
                }
            }

            yield return row;
        }
    }
}
