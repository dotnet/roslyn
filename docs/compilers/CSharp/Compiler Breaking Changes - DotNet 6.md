## This document lists known breaking changes in Roslyn in C# 10.0 which will be introduced with .NET 6.

1. Beginning with C# 10.0, null suppression operator is no longer allowed in patterns.
    ```csharp
    void M(object o)
    {
        if (o is null!) {} // error
    }
    ```