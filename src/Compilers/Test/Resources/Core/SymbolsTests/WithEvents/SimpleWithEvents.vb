' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Public Interface I1
    Event I1(ByRef x As Integer)

    Sub Raise(ByRef x As Integer)
End Interface

Public Class C1
    Implements I1

    Public Event I1(ByRef x As Integer) Implements I1.I1

    Public Sub Raise(ByRef x As Integer) Implements I1.Raise
        RaiseEvent I1(x)
    End Sub
End Class

Public Class Class1
    Public WithEvents WE1 As I1

    Public WithEvents WE2 As C1

    Public Sub goo() Handles WE1.I1
        Console.WriteLine("Class1.Goo")
    End Sub
End Class

Public Class Derived
    Inherits Class1

    Public Sub goo1() Handles WE1.I1
        Console.WriteLine("Derived.Goo1")
    End Sub

End Class
