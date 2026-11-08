namespace Kovecses.Requests;

internal sealed class EventPublishConfiguration(
    EventPublishStrategy globalStrategy,
    IReadOnlyDictionary<Type, EventPublishStrategy> overrides)
{
    public static EventPublishConfiguration Default { get; } =
        new(EventPublishStrategy.Sequential, new Dictionary<Type, EventPublishStrategy>());

    public EventPublishStrategy GetStrategy(Type eventType) =>
        overrides.TryGetValue(eventType, out var strategy) ? strategy : globalStrategy;

    public EventPublishConfiguration WithGlobalStrategy(EventPublishStrategy strategy) =>
        new(strategy, overrides);

    public EventPublishConfiguration WithEventStrategy(Type eventType, EventPublishStrategy strategy)
    {
        var updated = new Dictionary<Type, EventPublishStrategy>(overrides) { [eventType] = strategy };

        return new EventPublishConfiguration(globalStrategy, updated);
    }
}
