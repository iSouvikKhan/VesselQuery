using System.IO.Compression;
using System.Text.Json;

namespace VesselQuery.Core.Data;

public static class VesselDataLoader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Vessel data file not found: '{path}'", path);
        }

        using var file = File.OpenRead(path);
        using var stream = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(file, CompressionMode.Decompress)
            : (Stream)file;

        return LoadFromStream(stream);
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> LoadFromStream(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);

        var start = bytes.Span.IndexOf((byte)'[');
        var end = bytes.Span.LastIndexOf((byte)']');
        if (start < 0 || end < start)
        {
            throw new InvalidDataException("Vessel data does not contain a JSON array.");
        }

        using var document = JsonDocument.Parse(bytes[start..(end + 1)]);

        var rows = new List<IReadOnlyDictionary<string, object?>>(document.RootElement.GetArrayLength());
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"Expected every vessel to be a JSON object but found {element.ValueKind}.");
            }

            rows.Add(ToRow(element));
        }

        return rows;
    }

    private static Dictionary<string, object?> ToRow(JsonElement element)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            row[property.Name] = Normalise(property.Value);
        }

        return row;
    }

    private static object? Normalise(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.GetRawText()
    };
}
