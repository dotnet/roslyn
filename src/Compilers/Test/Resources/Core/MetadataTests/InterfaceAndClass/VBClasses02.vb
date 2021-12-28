' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Metadata
    Public Class A
        Public Sub SubAB(p As Byte)
        End Sub
    End Class

    Public Class B
        Inherits A
        Public Overloads Sub SubAB(p As SByte)
        End Sub
    End Class

    Public Class VBClass01

        Public Overridable Sub Sub01(p1 As A, p2 As B)
            Console.Write("AB_OV ")
        End Sub

        Public Overridable Sub Sub01(p1 As B, p2 As A)
            Console.Write("BA_OV ")
        End Sub

        Protected Overridable Sub Sub01(p1 As A, p2 As A)
            Console.Write("PT_AA_OV ")
        End Sub

        Protected Overridable Sub Sub01(p1 As B, ByRef p2 As B)
            Console.Write("PT_BRefB_OV ")
        End Sub

        Friend Overridable Sub Sub01(ParamArray p1 As B())
            Console.Write("FriendBAry_OV ")
        End Sub

    End Class

    Public Class VBClass02
        Inherits VBClass01

        Public NotOverridable Overrides Sub Sub01(p1 As B, p2 As A)
            Console.Write("(02)BA_Seal ")
        End Sub

    End Class

    Public Class VBBase
        ' members same as IMeth03.INested
        Public Overridable Sub NestedSub(p As UShort)
            Console.Write("VBaseSub (Virtual) ")
        End Sub
        Public Function NestedFunc(ByRef p As Object) As String
            Return "VBaseFunc (Non-Virtual) "
        End Function
    End Class

End Namespace
