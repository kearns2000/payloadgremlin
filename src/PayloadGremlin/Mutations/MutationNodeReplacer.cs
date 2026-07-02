using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal static class MutationNodeReplacer
{
    public static void Replace(JsonTree tree, MutationContext context, JsonNode newNode)
    {
        if (context.JsonPath == "$")
        {
            tree.ReplaceRoot(newNode);
            return;
        }

        switch (context.Parent)
        {
            case JsonObject obj when context.PropertyName is not null:
                obj[context.PropertyName] = newNode;
                break;
            case JsonArray array when int.TryParse(context.PropertyName, out var index):
                array[index] = newNode;
                break;
            default:
                throw new InvalidOperationException($"Cannot replace node at {context.JsonPath}");
        }
    }
}
