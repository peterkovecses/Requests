namespace Kovecses.Requests;

/// <summary>Determines how event handlers are invoked.</summary>
public enum EventPublishStrategy
{
    /// <summary>Invoke handlers sequentially, stopping at the first failure.</summary>
    Sequential,

    /// <summary>Invoke all handlers and wait for every invocation to finish.</summary>
    Parallel
}
