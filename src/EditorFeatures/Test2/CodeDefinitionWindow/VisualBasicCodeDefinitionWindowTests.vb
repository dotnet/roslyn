' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
    Public Class VisualBasicCodeDefinitionWindowTests
        Inherits AbstractCodeDefinitionWindowTests

        <Fact>
        Public Async Function ClassFromDefinition() As Task
            Const code As String = "
Class $$[|C|]
End Class"

            Await VerifyContextLocationAsync(code, "Class C")
        End Function

        <Fact>
        Public Async Function ClassFromReference() As Task
            Const code As String = "
Class [|C|]
    Shared Sub M()
        $$C.M()
    End Sub
End Class"

            Await VerifyContextLocationAsync(code, "Class C")
        End Function

        <Fact>
        Public Async Function MethodFromDefinition() As Task
            Const code As String = "
Class C
    Sub $$[|M|]()
    End Sub
End Class"

            Await VerifyContextLocationAsync(code, "Sub C.M()")
        End Function

        <Fact>
        Public Async Function MethodFromReference() As Task
            Const code As String = "
Class C
    Sub [|M|]()
        Me.$$M()
    End Sub
End Class"

            Await VerifyContextLocationAsync(code, "Sub C.M()")
        End Function

        <Fact>
        Public Async Function ReducedGenericExtensionMethod() As Task
            Const code As String = "
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module M
    <Extension>
    Sub [|M|](Of T)(list As List(Of T))
    End Sub
End Module

Module Program
    Sub Main()
        Dim list As New List(Of Integer)
        list.$$M()
    End Sub
End Module"

            Await VerifyContextLocationAsync(code, "Sub M.M(Of T)(List(Of T))")
        End Function

        Protected Overrides Function CreateWorkspace(code As String, testComposition As TestComposition) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(code, composition:=testComposition)
        End Function
    End Class
End Namespace
