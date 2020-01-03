' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryParentheses
    ''' <summary>
    ''' Theses are the tests for the VisualBasicRemoveUnnecessaryParenthesesFixAllCodeFixProvider.
    ''' This provider is specifically around to handle fixing unnecessary parentheses 
    ''' whose current option is set to something other than 'Ignore'.
    ''' </summary>
    Partial Public Class RemoveUnnecessaryParenthesesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer(), New VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider())
        End Function

        Friend Function GetRemoveUnnecessaryParenthesesDiagnostic(text As String, line As Integer, column As Integer) As DiagnosticDescription
            Return TestHelpers.Diagnostic(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId, text, startLocation:=New LinePosition(line, column))
        End Function

        Friend Overrides Function ShouldSkipMessageDescriptionVerification(descriptor As DiagnosticDescriptor) As Boolean
            Return descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary) And descriptor.DefaultSeverity = DiagnosticSeverity.Hidden
        End Function

        Private Shadows Async Function TestAsync(initial As String, expected As String,
                                                 offeredWhenRequireAllParenthesesForClarityIsEnabled As Boolean,
                                                 Optional ByVal index As Integer = 0) As Task
            Await TestInRegularAndScriptAsync(initial, expected, options:=RemoveAllUnnecessaryParentheses, index:=index)

            If (offeredWhenRequireAllParenthesesForClarityIsEnabled) Then
                Await TestInRegularAndScriptAsync(initial, expected, options:=MyBase.RequireAllParenthesesForClarity, index:=index)
            Else
                Await TestMissingAsync(initial, parameters:=New TestParameters(options:=MyBase.RequireAllParenthesesForClarity))
            End If
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestVariableInitializer_Always() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1)
    end sub
end class", New TestParameters(options:=IgnoreAllParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        <WorkItem(29736, "https://github.com/dotnet/roslyn/issues/29736")>
        Public Async Function TestVariableInitializer_MissingParenthesis() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestArithmeticRequiredForClarity1() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 + $$(2 * 3)
    end sub
end class", New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestArithmeticRequiredForClarity2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = a orelse $$(b andalso c)
    end sub
end class",
"class C
    sub M()
        dim x = a orelse b andalso c
    end sub
end class", parameters:=New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLogicalRequiredForClarity1() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = a orelse $$(b andalso c)
    end sub
end class", New TestParameters(options:=RequireOtherBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLogicalRequiredForClarity2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = a + $$(b * c)
    end sub
end class",
"class C
    sub M()
        dim x = a + b * c
    end sub
end class", parameters:=New TestParameters(options:=RequireOtherBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame1() As Task
            Await TestAsync(
"class C
    sub M()
        dim x = 1 + $$(2 + 3)
    end sub
end class",
"class C
    sub M()
        dim x = 1 + 2 + 3
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame2() As Task
            Await TestAsync(
"class C
    sub M()
        dim x = $$(1 + 2) + 3
    end sub
end class",
"class C
    sub M()
        dim x = 1 + 2 + 3
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1() As Task
            Await TestAsync(
"class C
    sub M()
        dim x = a orelse $$(b orelse c)
    end sub
end class",
"class C
    sub M()
        dim x = a orelse b orelse c
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2() As Task
            Await TestAsync(
"class C
    sub M()
        dim x = $$(a orelse b) orelse c
    end sub
end class",
"class C
    sub M()
        dim x = a orelse b orelse c
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestVariableInitializer_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim x = $$(1)
    end sub
end class",
"class C
    sub M()
        dim x = 1
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestReturnStatement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    function M() as integer
        return $$(1 + 2)
    end function
end class",
"class C
    function M() as integer
        return 1 + 2
    end function
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLocalVariable_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C

    sub M()
        dim i = $$(1 + 2)
    end sub
end class",
"class C

    sub M()
        dim i = 1 + 2
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLocalVariable_TestAvailableWithRequiredForClarity() As Task
            Await TestMissingAsync(
"class C

    sub M()
        dim i = 1 $$= 2
    end sub
end class", New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C

    sub M()
        i = $$(1 + 2)
    end sub
end class",
"class C

    sub M()
        i = 1 + 2
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestPrimaryAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C

    sub M()
        i = $$(x.Length)
    end sub
end class",
"class C

    sub M()
        i = x.Length
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestCompoundAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C

    sub M()
        i *= $$(1 + 2)
    end sub
end class",
"class C

    sub M()
        i *= 1 + 2
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestCompoundPrimaryAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C

    sub M()
        i *= $$(x.Length)
    end sub
end class",
"class C

    sub M()
        i *= x.Length
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestNestedParenthesizedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim i = ( $$(1 + 2) )
    end sub
end class",
"class C
    sub M()
        dim i = ( 1 + 2 )
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestLambdaBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim i = function () $$(1)
    end sub
end class",
"class C
    sub M()
        dim i = function () 1
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestArrayElement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim i as integer() = { $$(1) }
    end sub
end class",
"class C
    sub M()
        dim i as integer() = { 1 }
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestWhereClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim q = from c in customer
                where $$(c.Age > 21)
                select c
    end sub
end class",
"class C
    sub M()
        dim q = from c in customer
                where c.Age > 21
                select c
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim i = directcast( $$(1), string)
    end sub
end class",
"class C
    sub M()
        dim i = directcast( 1, string)
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestAroundCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim i = $$(directcast(1, string))
    end sub
end class",
"class C
    sub M()
        dim i = directcast(1, string)
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestMissingForConditionalAccess() As Task
            Await TestMissingAsync(
"class C
    sub M(s as string)
        dim v = $$(s?.Length).ToString()
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestMissingForConditionalIndex() As Task
            Await TestMissingAsync(
"class C
    sub M(s as string)
        dim v = $$(s?(0)).ToString()
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestNonConditionalInInterpolation_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity() As Task
            Await TestAsync(
"class C
    sub M()
        dim s = $""{ $$(true) }""
    end sub
end class",
"class C
    sub M()
        dim s = $""{ true }""
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity1() As Task
            Await TestAsync(
"class C
    sub M()
        dim q = $$(a * b) + c
    end sub
end class",
"class C
    sub M()
        dim q = a * b + c
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity2() As Task
            Await TestAsync(
"class C
    sub M()
        dim q = c + $$(a * b)
    end sub
end class",
"class C
    sub M()
        dim q = c + a * b
    end sub
end class", offeredWhenRequireAllParenthesesForClarityIsEnabled:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestForOverloadedOperatorOnLeft() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M(c1 as C, c2 as C, c3 as C)
        dim x = $$(c1 + c2) + c3
    end sub

    public shared operator +(c1 as C, c2 as C) as C
        return nothing
    end operator
end class",
"class C
    sub M(c1 as C, c2 as C, c3 as C)
        dim x = c1 + c2 + c3
    end sub

    public shared operator +(c1 as C, c2 as C) as C
        return nothing
    end operator
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestMissingForOverloadedOperatorOnRight() As Task
            Await TestMissingAsync(
"class C
    sub M(c1 as C, c2 as C, c3 as C)
        dim x = c1 + $$(c2 + c3)
    end sub

    public shared operator +(c1 as C, c2 as C) as C
        return nothing
    end operator
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestShiftRequiredForClarity1() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1 + 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestShiftRequiredForClarity2() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1 + 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestDoNotRemoveShiftIfDifferentPrecedence1() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1 + 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestDoNotRemoveShiftIfDifferentPrecedence2() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 << $$(2 << 3)
    end sub
end class", parameters:=New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestDoNotRemoveShiftIfKindsDiffer() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = $$(1 >> 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestRemoveShiftWithSamePrecedence() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = $$(1 << 2) << 3
    end sub
end class",
"class C
    sub M()
        dim x = 1 << 2 << 3
    end sub
end class", parameters:=New TestParameters(options:=RemoveAllUnnecessaryParentheses))
        End Function

        <WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestUnnecessaryParenthesisDiagnosticSingleLineExpression() As Task
            Dim parentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2)", 2, 16)
            Await TestDiagnosticsAsync(
"class C
    sub M()
        dim x = [|(1 + 2)|]
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses), parentheticalExpressionDiagnostic)
        End Function

        <WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestUnnecessaryParenthesisDiagnosticInMultiLineExpression() As Task
            Dim firstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 +", 2, 16)
            Await TestDiagnosticsAsync(
"class C
    sub M()
        dim x = [|(1 +
            2)|]
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses), firstLineParentheticalExpressionDiagnostic)
        End Function

        <WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestUnnecessaryParenthesisDiagnosticInNestedExpression() As Task
            Dim outerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + (2 + 3) + 4)", 2, 16)
            Dim innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(2 + 3)", 2, 21)
            Dim expectedDiagnostics = New DiagnosticDescription() {outerParentheticalExpressionDiagnostic, innerParentheticalExpressionDiagnostic}
            Await TestDiagnosticsAsync(
"class C
    sub M()
        dim x = [|(1 + (2 + 3) + 4)|]
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses), expectedDiagnostics)
        End Function

        <WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestUnnecessaryParenthesisDiagnosticInNestedMultiLineExpression() As Task
            Dim outerFirstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2 +", 2, 16)
            Dim innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(3 + 4)", 3, 12)
            Dim expectedDiagnostics = New DiagnosticDescription() {outerFirstLineParentheticalExpressionDiagnostic, innerParentheticalExpressionDiagnostic}
            Await TestDiagnosticsAsync(
"class C
    sub M()
        dim x = [|(1 + 2 +
            (3 + 4) +
            5 + 6)|]
    end sub
end class", New TestParameters(options:=RemoveAllUnnecessaryParentheses), expectedDiagnostics)
        End Function

        <WorkItem(27925, "https://github.com/dotnet/roslyn/issues/39529")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)>
        Public Async Function TestUnnecessaryParenthesisIncludesFadeLocations() As Task
            Dim input =
"class C
    sub M()
        dim x = [|{|expression:{|fade:(|}1 + 2{|fade:)|}|}|]
    end sub
end class"

            Dim parameters = New TestParameters(options:=RemoveAllUnnecessaryParentheses)
            Using workspace As TestWorkspace = CreateWorkspaceFromOptions(input, parameters)
                Dim expectedSpans = workspace.Documents.First().AnnotatedSpans

                Dim diagnostics = Await GetDiagnosticsAsync(workspace, parameters).ConfigureAwait(False)
                Dim diagnostic = diagnostics.Single()

                Assert.Equal(3, diagnostic.AdditionalLocations.Count)
                Assert.Equal(expectedSpans.Item("expression").Item(0), diagnostic.AdditionalLocations.Item(0).SourceSpan)
                Assert.Equal(expectedSpans.Item("fade").Item(0), diagnostic.AdditionalLocations.Item(1).SourceSpan)
                Assert.Equal(expectedSpans.Item("fade").Item(1), diagnostic.AdditionalLocations.Item(2).SourceSpan)

                Assert.Equal("[1,2]", diagnostic.Properties.Item(WellKnownDiagnosticTags.Unnecessary))
            End Using

        End Function
    End Class
End Namespace
