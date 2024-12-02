# Breaking changes in Roslyn after .NET 9.0.100 through .NET 10.0.100

This document lists known breaking changes in Roslyn after .NET 9 general release (.NET SDK version 9.0.100) through .NET 10 general release (.NET SDK version 10.0.100).

## Set state of enumerator object to "after" during disposal

***Introduced in Visual Studio 2022 version 17.13***

The state machine for enumerators incorrectly allowed resuming execution after the enumerator was disposed.  
Now, `MoveNext()` on a disposed enumerator properly returns `false` without executing any more user code.

```vb
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator()

        Console.Write(enumerator.MoveNext()) ' prints True
        Console.Write(enumerator.Current)    ' prints 1

        enumerator.Dispose()

        Console.Write(enumerator.MoveNext()) ' now prints False
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator() As IEnumerator(Of Integer)
        Yield 1
        Console.Write("not executed after disposal")
        Yield 2
    End Function
End Class
```

