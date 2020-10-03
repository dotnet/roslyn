' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers.VisualBasicRemoveSharedFromModuleMembersCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveSharedFromModuleMembers
    Public Class RemoveSharedFromModuleMembersTests
        <Fact>
        Public Async Function TestSharedFieldInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30593:Shared|} x As Integer
End Module
"
            Dim fixedSource = "
Public Module M
    Public x As Integer
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestSharedAutoPropertyInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30503:Shared|} Property X As Integer
End Module
"
            Dim fixedSource = "
Public Module M
    Public Property X As Integer
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestSharedReadOnlyAutoPropertyInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30503:Shared|} ReadOnly Property X As Integer
End Module
"
            Dim fixedSource = "
Public Module M
    Public ReadOnly Property X As Integer
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestSharedFullPropertyInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30503:Shared|} Property X As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Module
"
            Dim fixedSource = "
Public Module M
    Public Property X As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestFunctionWithSharedInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30433:Shared|} Function DoSomething()
    End Function
End Module
"
            Dim fixedSource = "
Public Module M
    Public Function DoSomething()
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestSubWithSharedInModule() As Task
            Dim source = "
Public Module M
    Public {|BC30433:Shared|} Sub DoSomething()
    End Sub
End Module
"
            Dim fixedSource = "
Public Module M
    Public Sub DoSomething()
    End Sub
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function

        <Fact>
        Public Async Function TestFixAll() As Task
            Dim source = "
Public Module M
    Public {|BC30593:Shared|} x As Integer

    Public {|BC30433:Shared|} Sub DoSomething()
    End Sub
End Module
"
            Dim fixedSource = "
Public Module M
    Public x As Integer

    Public Sub DoSomething()
    End Sub
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, fixedSource)
        End Function
    End Class
End Namespace
