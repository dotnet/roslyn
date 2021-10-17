## This document lists known breaking changes in Roslyn after .NET 6 all the way to .NET 7.

1. Beginning with C# 11.0, `Length` and `Count` properties on countable and indexable types
are assumed to be non-negative for purpose of subsumption and exhaustiveness analysis of patterns and switches.
Those types can be used with implicit Index indexer and list patterns.

    ```csharp
    void M(int[] i)
    {
        if (i is { Length: -1 }) {} // error: impossible under assumption of non-negative length
    }
    ```