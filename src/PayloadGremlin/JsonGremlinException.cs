namespace PayloadGremlin;

/// <summary>Thrown when input JSON is invalid or cannot be processed.</summary>
public sealed class JsonGremlinException : Exception
{
    public JsonGremlinException(string message) : base(message) { }
    public JsonGremlinException(string message, Exception inner) : base(message, inner) { }
}
