# Scripting Unit Tests

## Native Memory Pressure

The scripting engine in Roslyn makes heavy use of creating `MetadataImageReference` over a `Stream`. That is the way in which runtime assembly references are added. This form of `MetadataReference` results in a lot of native allocations as opposed to using an `ImmutableArray<byte>` which has none. By default these native allocations are collected by the finalizer as `MetadadataImageReference` is not disposable.

For production code this dependency on the finalizer is largely fine. An application generally creates a fixed set of `MetadataReferences` and operates on them. For our unit tests the finalizer is not sufficient. Virtually every scripting test creates a new instance of the scripting engine, that in turns loads a set of references from `Stream` and allocates a significant amount of native memory. The number of tests mean that we end up allocating a significant amount of native memory and it can lead to tests OOMing in CI if the finalizer isn't running often enough. Particularly when run on x86 environments.

To address this we strive to have the individual unit tests in scripting aggressively free the native memory that they allocate. This is done by:

1. Deriving scripting tests from `ScriptTestBase`
2. Using `ScriptTestBase.ScriptOptions` as the basis for all options used in testing
3. Ensuring every scripting test passes `ScriptTestBase.ScriptOptions`

The logic in `ScriptTestBase` hooks the core scripting engine functions for loading off of disk and ensures the native resources are freed when the test case is disposed