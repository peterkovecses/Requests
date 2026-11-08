namespace Kovecses.Requests;

/// <summary>Thrown when an event has no registered handlers.</summary>
public sealed class NoEventSubscribersException : Exception
{
    /// <summary>The event type for which no handlers were found.</summary>
    public Type EventType { get; }

    /// <summary>Creates an exception for the specified event type.</summary>
    /// <param name="eventType">The event type without subscribers.</param>
    public NoEventSubscribersException(Type eventType)
        : base($"No event subscribers for {eventType}.")
    {
        ArgumentNullException.ThrowIfNull(eventType);
        EventType = eventType;
    }
}
