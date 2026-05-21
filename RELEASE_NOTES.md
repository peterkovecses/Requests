# Release Notes - Kovecses.Requests 1.2.0

## Performance Improvements
- **IEnumerable Casting Optimization:** Replaced collection expression `[.. behaviors]` with smart casting using `as` operator to avoid unnecessary array allocations. The Microsoft DI container returns `T[]` directly in 99% of cases, resulting in **zero allocations** through direct reference reuse.
- **Expression Tree Factory Optimization:** Replaced `ActivatorUtilities.CreateFactory` with strongly-typed Expression tree compiled delegates, eliminating `object[]` parameter array allocations on every request.

### Benchmark Results
- **Simple Request:** 20.7% faster (60.5 ns vs 76.3 ns), 16% less allocation (168 B vs 200 B)
- **Pipeline (2 Behaviors):** 18.8% faster (148.7 ns vs 183.0 ns), 10% less allocation (624 B vs 696 B)
- **Now outperforms MediatR** by 18% in pipeline scenarios

---

# Release Notes - Kovecses.Requests 1.1.0

## Breaking Changes
- **HandleAsync Rename:** Renamed `Handle` to `HandleAsync` in `IRequestHandler` and `IPipelineBehavior` to better align with .NET asynchronous naming conventions.

## New Features
- **Behavior Lifetime Support:** Added optional `ServiceLifetime` parameter to `AddGlobalBehavior` and `AddBehavior` methods. Behaviors can now be registered as `Transient` (default), `Scoped`, or `Singleton`.

---

# Release Notes - Kovecses.Requests 1.0.0

## New Features
- **High-Performance Pipeline:** Optimized request handling with minimal allocations.
- **Fluent Builder API:** Easy registration of global, interface-based, and explicit behaviors.
- **Stateless Execution:** Full support for Retry patterns and recovery logic.
- **No-Magic Transparency:** Direct handler injection for clean debugging and F12 navigation.
- **Multi-Framework Support:** Native support for .NET 8, 9, and 10.

---
