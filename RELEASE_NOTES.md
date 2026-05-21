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
