' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeAccessibilityModifier
    Public Class ChangeAccessibilityModifierTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Public Sub New(logger As ITestOutputHelper)
            MyBase.New(logger)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicChangeAccessibilityModifierCodeFixProvider)
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Theory>
        <InlineData("Public", 0)>
        <InlineData("Protected", 1)>
        <InlineData("Friend", 2)>
        <InlineData("Friend Protected", 3)>
        <InlineData("Private Protected", 4)>
        Public Async Function TestProperty(accessibility As String, index As Integer) As Task
            Dim initial = "
MustInherit Class C
    Private [|MustOverride|] ReadOnly Property Prop As String
End Class
"
            Dim expected0 = $"
MustInherit Class C
    {accessibility} MustOverride ReadOnly Property Prop As String
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected0, index:=index)
        End Function

        <Theory>
        <InlineData("Public", 0)>
        <InlineData("Protected", 1)>
        <InlineData("Friend", 2)>
        <InlineData("Friend Protected", 3)>
        <InlineData("Private Protected", 4)>
        Public Async Function TestMethod(accessibility As String, index As Integer) As Task
            Dim initial = "
MustInherit Class C
    Private [|MustOverride|] Sub M() As String
End Class
"
            Dim expected0 = $"
MustInherit Class C
    {accessibility} MustOverride Sub M() As String
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected0, index:=index)
        End Function

        <Fact>
        Public Async Function TestPublicOverride() As Task
            Dim initial = "
MustInherit Class C
    Private Overrides Function [|ToString|]() As String
    End Function
End Class
"
            Dim expected = "
MustInherit Class C
    Public Overrides Function ToString() As String
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestProtectedOverride() As Task
            Dim initial = "
MustInherit Class C
    Protected MustOverride ReadOnly Property Prop As String
End Class
Class D
    Inherits C

    Overrides ReadOnly Property [|Prop|] As String
End Class
"
            Dim expected = "
MustInherit Class C
    Protected MustOverride ReadOnly Property Prop As String
End Class
Class D
    Inherits C

    Protected Overrides ReadOnly Property Prop As String
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Theory>
        <InlineData("Public", 0)>
        <InlineData("Protected", 1)>
        <InlineData("Friend", 2)>
        <InlineData("Friend Protected", 3)>
        <InlineData("Private Protected", 4)>
        Public Async Function TestFixAll(accessibility As String, index As Integer) As Task
            Dim initial = "
MustInherit Class C
    Private {|FixAllInDocument:MustOverride|} ReadOnly Property Prop As String

    Private MustOverride Sub M()

    Private MustOverride Property Indexer(index As Integer) As String

    Private Overrides Function ToString() As String
        Return Nothing
    End Function
End Class
"
            Dim expected = $"
MustInherit Class C
    {accessibility} MustOverride ReadOnly Property Prop As String

    {accessibility} MustOverride Sub M()

    {accessibility} MustOverride Property Indexer(index As Integer) As String

    Private Overrides Function ToString() As String
        Return Nothing
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected, index:=index)
        End Function

        <Fact>
        Public Async Function TestFixAllOverride() As Task
            Dim initial = "
MustInherit Class B
    Public MustOverride Sub Foo()

    Protected MustOverride Sub Boo()

    Private Overridable Sub Bar()
    End Sub
End Class
Class D
    Inherits B

    Private Overrides Sub {|FixAllInDocument:Foo|}()
    End Sub

    Overrides Sub Boo()
    End Sub

    Overrides Sub Bar()
    End Sub
End Class
"
            Dim expected = "
MustInherit Class B
    Public MustOverride Sub Foo()

    Protected MustOverride Sub Boo()

    Private Overridable Sub Bar()
    End Sub
End Class
Class D
    Inherits B

    Public Overrides Sub Foo()
    End Sub

    Protected Overrides Sub Boo()
    End Sub

    Overrides Sub Bar()
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function
    End Class
End Namespace
