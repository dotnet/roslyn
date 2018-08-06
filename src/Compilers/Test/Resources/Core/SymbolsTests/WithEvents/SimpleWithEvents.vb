' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
