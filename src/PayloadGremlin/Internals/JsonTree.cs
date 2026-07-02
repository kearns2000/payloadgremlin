using System.Text.Json;
using System.Text.Json.Nodes;

namespace PayloadGremlin.Internals;

/// <summary>Represents a parsed JSON document with path navigation helpers.</summary>
internal sealed class JsonTree
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private JsonNode _root;

    public JsonNode Root => _root;

    private JsonTree(JsonNode root)
    {
        _root = root;
    }

    public void ReplaceRoot(JsonNode newRoot) => _root = newRoot;

    public static JsonTree Parse(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                throw new global::PayloadGremlin.JsonGremlinException("Input JSON parsed to null. Provide a valid JSON value.");
            }

            return new JsonTree(node);
        }
        catch (JsonException ex)
        {
            throw new global::PayloadGremlin.JsonGremlinException($"Input is not valid JSON: {ex.Message}", ex);
        }
    }

    public JsonTree DeepClone() => new(Root.DeepClone());

    public string ToJson() => Root.ToJsonString(SerializerOptions);

    public IEnumerable<(string Path, JsonNode Node, JsonNode Parent, string? PropertyName)> Walk()
    {
        yield return ("$", Root, Root, null);
        foreach (var item in WalkChildren(Root, []))
        {
            yield return item;
        }
    }

    private static IEnumerable<(string Path, JsonNode Node, JsonNode Parent, string? PropertyName)> WalkChildren(
        JsonNode node,
        List<string> segments)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    var childSegments = new List<string>(segments) { property.Key };
                    var path = JsonPath.Format(childSegments);
                    yield return (path, property.Value!, obj, property.Key);
                    foreach (var child in WalkChildren(property.Value!, childSegments))
                    {
                        yield return child;
                    }
                }

                break;

            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    var childSegments = new List<string>(segments) { i.ToString() };
                    var path = JsonPath.Format(childSegments);
                    var element = array[i]!;
                    yield return (path, element, array, i.ToString());
                    foreach (var child in WalkChildren(element, childSegments))
                    {
                        yield return child;
                    }
                }

                break;
        }
    }

    public IReadOnlyList<string> GetAllValuePaths()
    {
        return Walk()
            .Where(w => w.Node is JsonValue or JsonArray or JsonObject)
            .Select(w => w.Path)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    public bool TryGetNode(string path, out JsonNode? node, out JsonNode? parent, out string? propertyName)
    {
        node = null;
        parent = null;
        propertyName = null;

        if (path == "$")
        {
            node = Root;
            parent = Root;
            return true;
        }

        var segments = JsonPath.Parse(path);
        JsonNode current = Root;
        JsonNode? currentParent = Root;
        string? currentProperty = null;

        foreach (var segment in segments)
        {
            currentParent = current;
            currentProperty = segment;

            switch (current)
            {
                case JsonObject obj when obj.TryGetPropertyValue(segment, out var child) && child is not null:
                    current = child;
                    break;
                case JsonArray arr when int.TryParse(segment, out var index) && index >= 0 && index < arr.Count:
                    current = arr[index]!;
                    break;
                default:
                    return false;
            }
        }

        node = current;
        parent = currentParent;
        propertyName = currentProperty;
        return true;
    }
}
