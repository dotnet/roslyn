# Microsoft.CodeAnalysis.Collections

This package contains shared code implementing specialized collection types.

* `SegmentedArray<T>`: This type behaves similarly to `T[]`, but segments the underlying storage to avoid allocating arrays in the Large Object Heap.
