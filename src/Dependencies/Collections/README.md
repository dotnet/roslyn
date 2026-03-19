# Microsoft.CodeAnalysis.Collections

This package contains shared code implementing specialized collection types.

## Collections

* `SegmentedArray<T>`: This type behaves similarly to `T[]`, but segments the underlying storage to avoid allocating arrays in the Large Object Heap. This type is most applicable in scenarios where all of the following hold:

    * The code currently uses `T[]`
    * The code relies on the performance characteristics `T[]` (O(1) reads and writes, storage density, and/or data locality, such that tree-based data structures like `ImmutableList<T>` are not acceptable)
    * The implementation does not need to use `AsMemory()`/`AsSpan()` on this instance to treat it as a `Memory<T>`/`Span<T>` (this operation requires the entire array be continuous in memory)
    * Application profiling suggests that allocations of `T[]` in the Large Object Heap are causing performance problems that need to be addressed

## Usage

The source package produced by this project may be consumed by other projects outside dotnet/roslyn.