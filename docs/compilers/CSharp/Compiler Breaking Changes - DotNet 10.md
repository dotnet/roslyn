# This document lists known breaking changes in Roslyn after .NET 9 all the way to .NET 10.

## Diagnostics now reported for improper use of pattern-based disposal method in `foreach`

***Introduced in Visual Studio 2022 version 17.13***

For instance, an obsolete `DisposeAsync` method is now reported in `await foreach`.
```csharp
await foreach (var i in new C()) { } // 'C.AsyncEnumerator.DisposeAsync()' is obsolete

class C
{
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }

    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current { get => throw null; }
        public Task<bool> MoveNextAsync() => throw null;

        [System.Obsolete]
        public ValueTask DisposeAsync() => throw null;
    }
}
```

Similarly, an `[UnmanagedCallersOnly]` `Dispose` method is now reported in `foreach` with a `ref struct` enumerator.
```csharp
public struct S
{
    public static void M2(S s)
    {
        foreach (var i in s) { } // 'SEnumerator.Dispose()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly.
    }
    public static SEnumerator GetEnumerator() => throw null;
}
public ref struct SEnumerator
{
    public bool MoveNext() => throw null;
    public int Current => throw null;
    [UnmanagedCallersOnly]
    public void Dispose() => throw null;
}
```
