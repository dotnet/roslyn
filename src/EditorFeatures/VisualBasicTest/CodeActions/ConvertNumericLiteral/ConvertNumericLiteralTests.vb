﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ConvertNumericLiteral
    Public Class ConvertNumericLiteralTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertNumericLiteralCodeRefactoringProvider()
        End Function

        Private Enum Refactoring
            ChangeBase1
            ChangeBase2
            AddOrRemoveDigitSeparators
        End Enum

        Private Async Function TestMissingOneAsync(initial As String) As Task
            Await TestMissingInRegularAndScriptAsync(CreateTreeText("[||]" + initial))
        End Function

        Private Async Function TestFixOneAsync(initial As String, expected As String, refactoring As Refactoring) As Task
            Await TestInRegularAndScriptAsync(CreateTreeText("[||]" + initial), CreateTreeText(expected), index:=DirectCast(refactoring, Integer))
        End Function

        Private Shared Function CreateTreeText(initial As String) As String
            Return "
Class X
    Sub M()
        Dim x = " + initial + "
    End Sub
End Class"
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestRemoveDigitSeparators() As Task
            Await TestFixOneAsync("&B1_0_01UL", "&B1001UL", Refactoring.AddOrRemoveDigitSeparators)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestConvertToBinary() As Task
            Await TestFixOneAsync("5", "&B101", Refactoring.ChangeBase1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestConvertToDecimal() As Task
            Await TestFixOneAsync("&B101", "5", Refactoring.ChangeBase1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestConvertToHex() As Task
            Await TestFixOneAsync("10", "&HA", Refactoring.ChangeBase2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestSeparateThousands() As Task
            Await TestFixOneAsync("100000000", "100_000_000", Refactoring.AddOrRemoveDigitSeparators)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestSeparateWords() As Task
            Await TestFixOneAsync("&H1111abcd1111", "&H1111_abcd_1111", Refactoring.AddOrRemoveDigitSeparators)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestSeparateNibbles() As Task
            Await TestFixOneAsync("&B10101010", "&B1010_1010", Refactoring.AddOrRemoveDigitSeparators)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestMissingOnFloatingPoint() As Task
            Await TestMissingOneAsync("1.1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestMissingOnScientificNotation() As Task
            Await TestMissingOneAsync("1e5")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestConvertToDecimal_02() As Task
            Await TestFixOneAsync("&H1e5", "485", Refactoring.ChangeBase1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestTypeCharacter() As Task
            Await TestFixOneAsync("&H1e5UL", "&B111100101UL", Refactoring.ChangeBase2)
        End Function

        <WorkItem(19225, "https://github.com/dotnet/roslyn/issues/19225")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestPreserveTrivia() As Task
            Await TestInRegularAndScriptAsync(
"Class X
    Sub M()
        Dim x As Integer() =
        {
            [||]&H1, &H2
        }
    End Sub
End Class",
"Class X
    Sub M()
        Dim x As Integer() =
        {
            &B1, &H2
        }
    End Sub
End Class", index:=Refactoring.ChangeBase2, ignoreTrivia:=False)
        End Function

        <WorkItem(19369, "https://github.com/dotnet/roslyn/issues/19369")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestCaretPositionAtTheEnd() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim a As Integer = 42[||]
End Class",
"Class C
    Dim a As Integer = &B101010
End Class", index:=Refactoring.ChangeBase1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestSelectionMatchesToken() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim a As Integer = [|42|]
End Class",
"Class C
    Dim a As Integer = &B101010
End Class", index:=Refactoring.ChangeBase1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)>
        Public Async Function TestSelectionDoesntMatchToken() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Dim a As Integer = [|42 * 2|]
End Class")
        End Function
    End Class
End Namespace
