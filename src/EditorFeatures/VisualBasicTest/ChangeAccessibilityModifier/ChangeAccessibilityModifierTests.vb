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
        Public Async Function TestNotOnOverride() As Task
            Dim initial = "
MustInherit Class C
    Private Overrides Function [|ToString|]() As String
    End Function
End Class
"
            Await TestMissingAsync(initial)
        End Function
    End Class
End Namespace
