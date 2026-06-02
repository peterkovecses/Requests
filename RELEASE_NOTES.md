# Release Notes - Kovecses.Requests 2.3.0

## Performance Improvements
- **Dynamic Factory Elimination:** Replaced `dynamic` dispatch with a pre-compiled, typed `Func<object, object, object>` delegate in the internal handler factory. This eliminates DLR (Dynamic Language Runtime) overhead and improves performance.
- **Redundancy Cleanup:** Optimized service registration by removing redundant reflection calls during the build phase of the pipeline handler.
- **NativeAOT Compatibility:** Improved compatibility with NativeAOT by reducing reliance on dynamic runtime features.

### Benchmark Results
- **Simple Request:** ~3.2% faster (56.3 ns vs 58.2 ns)
- **Pipeline (2 Behaviors):** Minor latency reduction and improved consistency.

---

# Release Notes - Kovecses.Requests 2.2.0

## New Features
- **OpenTelemetry Support:** Added optional, built-in support for distributed tracing.
  - New `AddOpenTelemetry()` extension method for `IRequestsBuilder`.
  - Automatic `Activity` creation for every request using `Kovecses.Requests` source.
  - Exception capturing and error status reporting within the tracing behavior.
  - Zero performance impact for users who don't enable it (completely optional behavior).

---

# Release Notes - Kovecses.Requests 2.1.0

## Performance Improvements
- **DI Factory Optimization:** Optimized the internal compiled Expression tree factory by removing an unused `IServiceProvider` parameter. This reduces delegate signature overhead during handler resolution.
- **Micro-benchmarks:** Achieved an additional 1.5-2.2% reduction in request handling overhead.

---

# Release Notes - Kovecses.Requests 2.0.0

## Breaking Changes
- **Simplified Behavior Registration:** Removed the ability to specify `ServiceLifetime` for pipeline behaviors. All behaviors are now strictly registered as `Transient`. 
  - **Reasoning:** This ensures better thread safety, prevents accidental state sharing between requests, and simplifies the internal registration logic. From a performance perspective, transient registration is well-optimized in the native .NET DI container.

---

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
