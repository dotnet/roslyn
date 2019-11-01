' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Experiments
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <UseExportProvider>
    Public Class SymbolCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Private Const s_unicodeEllipsis = ChrW(&H2026)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New SymbolCompletionProvider()
        End Function

        Protected Overrides Function GetExportProvider() As ExportProvider
            Return ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(TestExperimentationService))).CreateExportProvider()
        End Function

#Region "StandaloneNamespaceAndTypeSourceTests"

        Private Async Function VerifyNSATIsAbsentAsync(markup As String) As Task
            ' Verify namespace 'System' is absent
            Await VerifyItemIsAbsentAsync(markup, "System")

            ' Verify type 'String' is absent
            Await VerifyItemIsAbsentAsync(markup, "String")
        End Function

        Private Async Function VerifyNSATExistsAsync(markup As String) As Task
            ' Verify namespace 'System' is absent
            Await VerifyItemExistsAsync(markup, "System")

            ' Verify type 'String' is absent
            Await VerifyItemExistsAsync(markup, "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEmptyFile() As Task
            Await VerifyNSATIsAbsentAsync("$$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEmptyFileWithImports() As Task
            Await VerifyNSATIsAbsentAsync(AddImportsStatement("Imports System", "$$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", "Class A(Of T As $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", "Class A(Of T As { II, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint3() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", "Class A(Of T As $$)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint4() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", "Class A(Of T As { II, $$})"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements1() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements $$.Method")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements3() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements I.Method, $$.Method")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs1() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As $$ Implements II.Method")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs3() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method(ByVal args As $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAsNew() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d As New $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGetType1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = GetType($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeOfIs() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TypeOf d Is $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreation() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = New $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayCreation() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d() = New $$() {")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CType(obj, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TryCast(obj, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast3() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = DirectCast(obj, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayType() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d() as $$(")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNullableType() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d as $$?")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList1() As Task
            Await VerifyNSATIsAbsentAsync(AddImportsStatement("Imports System", CreateContent("Class A(Of $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList2() As Task
            Await VerifyNSATIsAbsentAsync(AddImportsStatement("Imports System", CreateContent("Class A(Of T, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList3() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d as D(Of $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList4() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d as D(Of A, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInferredFieldInitializer() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim anonymousCust2 = New With {Key $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedFieldInitializer() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim anonymousCust = New With {.Name = $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInitializer() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturnStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Return $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIfStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("If $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIfStatement2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("If Var1 Then",
                                      "Else If $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCatchFilterClause() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Try",
                                      "Catch ex As Exception when $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestErrorStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Error $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Select $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Select Case $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleCaseClause1() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleCaseClause2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case 1, $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeCaseClause1() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case $$ To"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeCaseClause2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case 1 To $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRelationalCaseClause1() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case Is > $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRelationalCaseClause2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case >= $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSyncLockStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("SyncLock $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileOrUntilClause1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Do While $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileOrUntilClause2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Do Until $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("While $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("For i = $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("For i = 1 To $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStepClause() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("For i = 1 To 10 Step $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForEachStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("For Each I in $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUsingStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Using $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestThrowStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Throw $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAssignmentStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$ = a")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAssignmentStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("a = $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCallStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Call $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCallStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$(1)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("AddHandler $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("AddHandler T.Event, AddressOf $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement3() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("RemoveHandler $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement4() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("RemoveHandler T.Event, AddressOf $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithStatement() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("With $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedExpression() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = ($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeOfIs2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = TypeOf $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMemberAccessExpression1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$.Name")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMemberAccessExpression2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$!Name")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvocationExpression() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$(1)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentExpression() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("$$(Of Integer)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast4() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CType($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast5() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TryCast($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast6() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = DirectCast($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBuiltInCase() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CInt($$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryExpression1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = $$ + d")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryExpression2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = d + $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUnaryExpression() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = +$$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryConditionExpression1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If($$,")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryConditionExpression2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If($$, a, b")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, $$, c")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression3() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, b, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSingleArgument() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("D($$)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedArgument() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("D(Name := $$)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeArgument1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a($$ To 10)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeArgument2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a(0 To $$)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionRangeVariable() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From var in $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExpressionRangeVariable() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From var In collection Let b = $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFunctionAggregation() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In col Aggregate o In c.o Into an = Any($$)")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhereQueryOperator() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In col Where $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionWhileQueryOperator1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim customerList = From c In cust Order By c.C Skip While $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionWhileQueryOperator2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim customerList = From c In cust Order By c.C Take While $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionQueryOperator1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In cust Skip $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionQueryOperator2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In cust Take $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestJoinCondition1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim p1 = From p In P Join d In Desc On $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestJoinCondition2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim p1 = From p In P Join d In Desc On p.P Equals $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOrdering() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From b In books Order By $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestXmlEmbeddedExpression() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim book As XElement = <book isbn=<%= $$ %>></book>")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNextStatement1() As Task
            Await VerifyNSATIsAbsentAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("For i = 1 To 10",
                                      "Next $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNextStatement2() As Task
            Await VerifyNSATIsAbsentAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("For i = 1 To 10",
                                      "Next i, $$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEraseStatement1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Erase $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEraseStatement2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Erase i, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionInitializer1() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = new List(Of Integer) from { $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionInitializer2() As Task
            Await VerifyNSATExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = { $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestStringLiteral() As Task
            Await VerifyNSATIsAbsentAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = ""$$""")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestComment1() As Task
            Await VerifyNSATIsAbsentAsync(AddImportsStatement("Imports System", AddInsideMethod("' $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestComment2() As Task
            Await VerifyNSATExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("'", "$$"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInactiveRegion1() As Task
            Await VerifyNSATIsAbsentAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("#IF False Then", " $$"))))
        End Function

#Region "Tests that verify namespaces and types separately"

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAliasImportsClause1() As Task
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", "Imports T = $$"), "System")
            Await VerifyItemIsAbsentAsync(AddImportsStatement("Imports System", "Imports T = $$"), "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAliasImportsClause2() As Task
            Await VerifyItemExistsAsync("Imports $$ = S", "System")
            Await VerifyItemIsAbsentAsync("Imports $$ = S", "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersImportsClause1() As Task
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", "Imports $$"), "System")
            Await VerifyItemIsAbsentAsync(AddImportsStatement("Imports System", "Imports $$"), "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersImportsClause2() As Task
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", "Imports System, $$"), "System")
            Await VerifyItemIsAbsentAsync(AddImportsStatement("Imports System", "Imports System, $$"), "String")
        End Function

        <WorkItem(529191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529191")>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes1() As Task
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", CreateContent("<$$>")), "System")
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", CreateContent("<$$>")), "String")
        End Function

        <WorkItem(529191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529191")>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes2() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("<$$>",
                                  "Class Cl")), "System")
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("<$$>",
                                  "Class Cl")), "String")
        End Function

        <WorkItem(529191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529191")>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes3() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class Cl",
                                  "    <$$>",
                                  "    Function Method()")), "System")
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class Cl",
                                  "    <$$>",
                                  "    Function Method()")), "String")
        End Function

#End Region

#End Region

#Region "SymbolCompletionProviderTests"

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function IsCommitCharacterTest() As Task
            Const code = "
Imports System
Class C
    Sub M()
        $$
    End Sub
End Class"

            Await VerifyCommonCommitCharactersAsync(code, textTypedSoFar:="")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SendEnterThroughToEditorTest() As Task
            Const code = "
Imports System
Class C
    Sub M()
        $$
    End Sub
End Class"

            Await VerifySendEnterThroughToEditorAsync(code, "Int32", expected:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterDateLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call #1/1/2010#.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterStringLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call """".$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterTrueLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call True.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterFalseLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call False.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterNumericLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call 2.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterCharacterLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call ""c""c.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoMembersAfterNothingLiteral() As Task
            Await VerifyItemIsAbsentAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call Nothing.$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedDateLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (#1/1/2010#).$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedStringLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call ("""").$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedTrueLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (True).$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedFalseLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (False).$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedNumericLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (2).$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersAfterParenthesizedCharacterLiteral() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (""c""c).$$")), "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoMembersAfterParenthesizedNothingLiteral() As Task
            Await VerifyItemIsAbsentAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (Nothing).$$")), "Equals")
        End Function

        <WorkItem(539243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539243")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedClassesInImports() As Task
            Await VerifyItemExistsAsync("Imports System.$$", "Console")
        End Function

        <WorkItem(539332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539332")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceTypesAvailableInImportsAlias() As Task
            Await VerifyItemExistsAsync("Imports S = System.$$", "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceTypesAvailableInImports() As Task
            Await VerifyItemExistsAsync("Imports System.$$", "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVarInMethod() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Dim banana As Integer = 4" + vbCrLf + "$$")), "banana")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommandCompletionsInScript() As Task
            Await VerifyItemExistsAsync(<text>#$$</text>.Value, "#R", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReferenceCompletionsInScript() As Task
            Await VerifyItemExistsAsync(<text>#r "$$"</text>.Value, "System.dll", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <WorkItem(539300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539300")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMembersAfterMe1() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        Me.$$
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "s")
        End Function

        <WorkItem(539300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539300")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMembersAfterMe2() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        Me.$$
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "Method")
        End Function

        <WorkItem(539300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539300")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersAfterMe1() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        Me.$$
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "field")
        End Function

        <WorkItem(539300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539300")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersAfterMe2() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        Me.$$
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "M")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoEventSymbolAfterMe() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class EventClass
    Public Event X()

    Sub Test()
        Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoEventSymbolAfterMyClass() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class EventClass
    Public Shared Event X()

    Sub Test()
        MyClass.$$
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoEventSymbolAfterMyBase() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class C1
    Public Event E(x As Integer)
End Class

Class C2
    Inherits C1
    Sub M1()
        MyBase.$$
    End Sub
End Class
</Text>.Value, "E")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoEventSymbolAfterInstanceMember() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class EventClass
    Public Shared Event X()

    Sub Test()
        Dim a As New EventClass()
        a.$$
    End Sub
End Class
</Text>.Value, "E")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventSymbolAfterMeInAddHandlerContext() As Task
            Await VerifyItemExistsAsync(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        AddHandler Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventSymbolAfterInstanceMemberInAddHandlerContext() As Task
            Await VerifyItemExistsAsync(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        AddHandler a.$$
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventSymbolAfterInstanceMemberInParenthesizedAddHandlerContext() As Task
            Await VerifyItemExistsAsync(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        AddHandler (a.$$), a.XEvent
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventSymbolAfterMeInRemoveHandlerContext() As Task
            Await VerifyItemExistsAsync(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        RemoveHandler Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoImplicitlyDeclaredMembersFromEventDeclarationAfterMe() As Task
            Dim source = <Text>
Class EventClass
    Public Event X()
    Sub Test()
        Me.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(source, "XEventHandler")
            Await VerifyItemIsAbsentAsync(source, "XEvent")
            Await VerifyItemIsAbsentAsync(source, "add_X")
            Await VerifyItemIsAbsentAsync(source, "remove_X")
        End Function

        <WorkItem(530617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530617")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoImplicitlyDeclaredMembersFromEventDeclarationAfterInstance() As Task
            Dim source = <Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        a.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(source, "XEventHandler")
            Await VerifyItemIsAbsentAsync(source, "XEvent")
            Await VerifyItemIsAbsentAsync(source, "add_X")
            Await VerifyItemIsAbsentAsync(source, "remove_X")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplicitlyDeclaredEventHandler() As Task
            Dim source = <Text>
Class EventClass
    Public Event X()
    Dim a As $$
End Class
</Text>.Value

            Await VerifyItemExistsAsync(source, "XEventHandler")
        End Function

        <WorkItem(529570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529570")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplicitlyDeclaredFieldFromWithEvents() As Task
            Dim source = <Text>
Public Class C1
    Protected WithEvents w As C1 = Me
    Sub Goo()
        Me.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(source, "_w")
        End Function

        <WorkItem(529147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529147")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplicitlyDeclaredFieldFromAutoProperty() As Task
            Dim source = <Text>
Class C1
    Property X As C1
    Sub test()
        Me.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(source, "_X")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingBeforeDot() As Task
            Dim code = <Text>
Module Module1
    Sub Main()
        .$$
    End Sub
End Module
                       </Text>.Value

            Await VerifyItemIsAbsentAsync(code, "Main")
        End Function

        <WorkItem(539276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539276")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMembersAfterWithMe1() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        With Me
            .$$
        End With
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "s")
        End Function

        <WorkItem(539276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539276")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMembersAfterWithMe2() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        With Me
            .$$
        End With
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "Method")
        End Function

        <WorkItem(539276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539276")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersAfterWithMe1() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        With Me
            .$$
        End With
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "field")
        End Function

        <WorkItem(539276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539276")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersAfterWithMe2() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Dim field As Integer
    Shared s As Integer
    Sub M()
        With Me
            .$$
        End With
    End Sub
    Shared Sub Method()
    End Sub
End Class
</Text>.Value, "M")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNestedWithBlocks() As Task
            Await VerifyItemExistsAsync(
<Text>
Class C
    Sub M()
        Dim s As String = ""
        With s
            With .Length
                .$$
            End With
        End With
    End Sub
End Class
</Text>.Value, "ToString")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGlobalScriptMembers() As Task
            Await VerifyItemExistsAsync(
<Text>
$$
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGlobalScriptMembersAfterStatement() As Task
            Await VerifyItemExistsAsync(
<Text>
Dim x = 1: $$
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)

            Await VerifyItemExistsAsync(
<Text>
Dim x = 1
$$
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)

            Await VerifyItemIsAbsentAsync(
<Text>
Dim x = 1 $$
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGlobalStatementMembersBeforeDirectives() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
$$

#If DEBUG
#End If
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGlobalScriptMembersInsideDirectives() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
#If $$
</Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGlobalScriptMembersAfterAnnotation() As Task
            Await VerifyItemIsAbsentAsync(
<Text><![CDATA[
<Annotation>
$$
]]></Text>.Value, "Console", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoSharedMembers() As Task
            Dim test = <Text>
Class C
    Sub M()
        Dim s = 1
        s.$$
    End Sub
End Class
</Text>.Value

            ' This is an intentional change from Dev12 behavior where constant
            ' field members were shown
            Await VerifyItemIsAbsentAsync(test, "MaxValue")
            Await VerifyItemIsAbsentAsync(test, "ReferenceEquals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLabelAfterGoto1() As Task
            Dim test = <Text>
Class C
    Sub M()
        Goo: Dim i As Integer
        Goto $$"
</Text>.Value

            Await VerifyItemExistsAsync(test, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLabelAfterGoto2() As Task
            Dim test = <Text>
Class C
    Sub M()
        Goo: Dim i As Integer
        Goto Goo $$"
</Text>.Value

            Await VerifyItemIsAbsentAsync(test, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LabelAfterGoto3() As Task
            Dim test = <Text>
Class C
    Sub M()
        10: Dim i As Integer
        Goto $$"
</Text>.Value.NormalizeLineEndings()

            Dim text As String = Nothing
            Dim position As Integer
            MarkupTestFile.GetPosition(test, text, position)

            ' We don't trigger intellisense within numeric literals, so we 
            ' explicitly test only the "nothing typed" case.
            ' This is also the Dev12 behavior for suggesting labels.
            Await VerifyAtPositionAsync(
                text, position, usePreviousCharAsTrigger:=True,
                expectedItemOrNull:="10", expectedDescriptionOrNull:=Nothing,
                sourceCodeKind:=SourceCodeKind.Regular, checkForAbsence:=False,
                glyph:=Nothing, matchPriority:=Nothing, hasSuggestionItem:=Nothing,
                displayTextSuffix:=Nothing, matchingFilters:=Nothing)
        End Function

        <WorkItem(541235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541235")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAlias1() As Task
            Dim test = <Text>
Imports N = NS1.NS2

Namespace NS1.NS2
  Public Class A
      Public Shared Sub M
        N.$$
      End Sub
  End Class
End Namespace
</Text>.Value

            Await VerifyItemExistsAsync(test, "A")
        End Function

        <WorkItem(541235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541235")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAlias2() As Task
            Dim test = <Text>
Imports N = NS1.NS2

Namespace NS1.NS2
  Public Class A
      Public Shared Sub M
        N.A.$$
      End Sub
  End Class
End Namespace
</Text>.Value

            Await VerifyItemExistsAsync(test, "M")
        End Function

        <WorkItem(541235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541235")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAlias3() As Task
            Dim test = <Text>
Imports System
Imports System.Collections.Generic
Imports System.Linq 
Imports N = NS1.NS2

Module Program
    Sub Main(args As String())
        N.$$
    End Sub
End Module

Namespace NS1.NS2
  Public Class A
      Public Shared Sub M
      End Sub
  End Class
End Namespace
</Text>.Value

            Await VerifyItemExistsAsync(test, "A")
        End Function

        <WorkItem(541235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541235")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAlias4() As Task
            Dim test = <Text>
Imports System
Imports System.Collections.Generic
Imports System.Linq 
Imports N = NS1.NS2

Module Program
    Sub Main(args As String())
        N.A.$$
    End Sub
End Module

Namespace NS1.NS2
  Public Class A
      Public Shared Sub M
      End Sub
  End Class
End Namespace
</Text>.Value

            Await VerifyItemExistsAsync(test, "M")
        End Function

        <WorkItem(541399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541399")>
        <WorkItem(529190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529190")>
        <WpfFact(Skip:="529190"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterSingleLineIf() As Task
            Dim test = <Text>
Module Program
    Sub Main(args As String())
        Dim x1 As Integer
        If True Then $$
    End Sub
End Module
</Text>.Value

            Await VerifyItemExistsAsync(test, "x1")
        End Function

        <WorkItem(540442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540442")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOnlyInterfacesInImplementsStatements() As Task
            Dim test = <Text>
Interface IOuter
    Delegate Sub Del()
    
    Interface INested
        Sub DoNested()
    End Interface
End Interface

Class nested
    Implements IOuter.$$

    
                       </Text>

            Await VerifyItemExistsAsync(test.Value, "INested")
            Await VerifyItemIsAbsentAsync(test.Value, "Del")
        End Function

        <WorkItem(540442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540442")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNestedInterfaceInImplementsClause() As Task
            Dim test = <Text>
Interface IOuter
    Sub DoOuter()
    
    Interface INested
        Sub DoNested()
    End Interface
End Interface

Class nested
    Implements IOuter.INested

    Sub DoStuff() implements IOuter.$$    
                       </Text>

            Await VerifyItemExistsAsync(test.Value, "INested")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingAfterBadQualifiedImplementsClause() As Task
            Dim test = <Text>
Class SomeClass
    Implements Gibberish.$$
End Class
                       </Text>

            Await VerifyNoItemsExistAsync(test.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingAfterBadImplementsClause() As Task
            Dim test = <Text>
Module Module1
    Sub Goo()
    End Sub
End Module

Class SomeClass
    Sub DoStuff() Implements Module1.$$
                       </Text>

            Await VerifyItemIsAbsentAsync(test.Value, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescriptionGenericTypeParameter() As Task
            Dim test = <Text><![CDATA[
Class SomeClass(Of T)
    Sub M()
        $$
    End Sub
End Class
                       ]]></Text>

            Await VerifyItemExistsAsync(test.Value, "T", $"T {FeaturesResources.in_} SomeClass(Of T)")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeName() As Task
            Dim test = <Text><![CDATA[
Imports System
<$$
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameAfterSpecifier() As Task
            Dim test = <Text><![CDATA[
Imports System
<Assembly:$$
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameInAttributeList() As Task
            Dim test = <Text><![CDATA[
Imports System
<CLSCompliant,$$
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameInAttributeListAfterSpecifier() As Task
            Dim test = <Text><![CDATA[
Imports System
<Assembly:CLSCompliant,Assembly:$$
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameBeforeClass() As Task
            Dim test = <Text><![CDATA[
Imports System
<$$
Public Class C
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameAfterSpecifierBeforeClass() As Task
            Dim test = <Text><![CDATA[
Imports System
<Assembly:$$
Public Class C
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliant")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliantAttribute")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameInAttributeArgumentList() As Task
            Dim test = <Text><![CDATA[
Imports System
<CLSCompliant($$
Public Class C
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliantAttribute")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliant")
        End Function

        <WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeNameInsideClass() As Task
            Dim test = <Text><![CDATA[
Imports System
Public Class C
    Dim c As $$
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "CLSCompliantAttribute")
            Await VerifyItemIsAbsentAsync(test.Value, "CLSCompliant")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNewAfterMeWhenFirstStatementInCtor() As Task
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.New(accountKey, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String)
        Me.New(accountKey, accountName, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String)
        Me.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "New")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNewAfterMeWhenNotFirstStatementInCtor() As Task
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.New(accountKey, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String)
        Me.New(accountKey, accountName, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String)
        Dim x As Integer
        Me.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemIsAbsentAsync(test.Value, "New")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <WorkItem(759729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/759729")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNewAfterMeWhenFirstStatementInSingleCtor() As Task
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemIsAbsentAsync(test.Value, "New")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <WorkItem(759729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/759729")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNewAfterMyClassWhenFirstStatementInCtor() As Task
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.New(accountKey, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String)
        Me.New(accountKey, accountName, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String)
        MyClass.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemExistsAsync(test.Value, "New")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNewAfterMyClassWhenNotFirstStatementInCtor() As Task
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.New(accountKey, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String)
        Me.New(accountKey, accountName, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String)
        Dim x As Integer
        MyClass.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemIsAbsentAsync(test.Value, "New")
        End Function

        <WorkItem(542441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542441")>
        <WorkItem(759729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/759729")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNewAfterMyClassWhenFirstStatementInSingleCtor() As Task
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        MyClass.$$
    End Sub
End Class
]]></Text>

            Await VerifyItemIsAbsentAsync(test.Value, "New")
        End Function

        <WorkItem(542242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542242")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOnlyShowAttributesInAttributeNameContext1() As Task
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim markup = <Text><![CDATA[
Imports System

<$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Goo
        Class SomethingAttribute
            Inherits Attribute
 
        End Class

        Class C2
        End Class
    End Class

    Class C1
    End Class
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Bar")
            Await VerifyItemIsAbsentAsync(markup, "D")
        End Function

        <WorkItem(542242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542242")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOnlyShowAttributesInAttributeNameContext2() As Task
            Dim markup = <Text><![CDATA[
Imports System

<Bar.$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Goo
        Class SomethingAttribute
            Inherits Attribute
 
        End Class

        Class C2
        End Class
    End Class

    Class C1
    End Class
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo")
            Await VerifyItemIsAbsentAsync(markup, "C1")
        End Function

        <WorkItem(542242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542242")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOnlyShowAttributesInAttributeNameContext3() As Task
            Dim markup = <Text><![CDATA[
Imports System

<Bar.Goo.$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Goo
        Class SomethingAttribute
            Inherits Attribute
 
        End Class

        Class C2
        End Class
    End Class

    Class C1
    End Class
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Something")
            Await VerifyItemIsAbsentAsync(markup, "C2")
        End Function

        <WorkItem(25589, "https://github.com/dotnet/roslyn/issues/25589")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithNestedAttribute1() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class CustomAttribute
            Inherits System.Attribute
        End Class
    End Namespace
End Namespace

<$$>
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Namespace1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithNestedAttribute2() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class CustomAttribute
            Inherits System.Attribute
        End Class
    End Namespace
End Namespace

<Namespace1.$$>
]]></Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "Namespace2")
            Await VerifyItemExistsAsync(markup, "Namespace3")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithNestedAttribute3() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class CustomAttribute
            Inherits System.Attribute
        End Class
    End Namespace
End Namespace

<Namespace1.Namespace3.$$>
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Namespace4")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithNestedAttribute4() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class CustomAttribute
            Inherits System.Attribute
        End Class
    End Namespace
End Namespace

<Namespace1.Namespace3.Namespace4.$$>
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Custom")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithNestedAttribute_NamespaceAlias() As Task
            Dim markup = <Text><![CDATA[
Imports Namespace1Alias = Namespace1
Imports Namespace2Alias = Namespace1.Namespace2
Imports Namespace3Alias = Namespace1.Namespace3
Imports Namespace4Alias = Namespace1.Namespace3.Namespace4

Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class CustomAttribute
            Inherits System.Attribute
        End Class
    End Namespace
End Namespace

<$$>
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Namespace1Alias")
            Await VerifyItemIsAbsentAsync(markup, "Namespace2Alias")
            Await VerifyItemExistsAsync(markup, "Namespace3Alias")
            Await VerifyItemExistsAsync(markup, "Namespace4Alias")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeSearch_NamespaceWithoutNestedAttribute() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Namespace Namespace2
        Class NonAttribute
        End Class
    End Namespace
    Namespace Namespace3.Namespace4
        Class NonAttribute
            Inherits System.NonAttribute
        End Class
    End Namespace
End Namespace

<$$>
]]></Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "Namespace1")
        End Function

        <WorkItem(542737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542737")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestQueryVariableAfterSelectClause() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim q1 = From num In Enumerable.Range(3, 4) Select $$
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "num")
        End Function

        <WorkItem(542683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542683")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplementsClassesWithNestedInterfaces() As Task
            Dim markup = <Text><![CDATA[
Interface MyInterface1
    Class MyClass2
        Interface MyInterface3
        End Interface
    End Class

    Class MyClass3

    End Class
End Interface
 
Class D
    Implements MyInterface1.$$
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "MyClass2")
            Await VerifyItemIsAbsentAsync(markup, "MyClass3")
        End Function

        <WorkItem(542683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542683")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplementsClassesWithNestedInterfacesClassOutermost() As Task
            Dim markup = <Text><![CDATA[
Class MyClass1
    Class MyClass2
        Interface MyInterface
        End Interface
    End Class
End Class

Class G
    Implements $$
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "MyClass1")
        End Function

        <WorkItem(542876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542876")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQuerySelect1() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = From i In New Integer() {1},
                        j In New String() {""}
                    Select $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i")
            Await VerifyItemExistsAsync(markup, "j")
        End Function

        <WorkItem(542876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542876")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQuerySelect2() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = From i In New Integer() {1},
                        j In New String() {""}
                    Select i,$$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i")
            Await VerifyItemExistsAsync(markup, "j")
        End Function

        <WorkItem(542876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542876")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQuerySelect3() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = From i In New Integer() {1},
                        j In New String() {""}
                    Select i, $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i")
            Await VerifyItemExistsAsync(markup, "j")
        End Function

        <WorkItem(542927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542927")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryGroupByInto1() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim arr = New Integer() {1}
        Dim query = From i In arr
                    Group By i Into $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Count")
        End Function

        <WorkItem(542927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542927")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryGroupByInto2() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim col = New String() { }

        Dim temp = From x in col
                   Group By x.Length Into $$
    End Sub

    <Extension()>
    Function LongestString(list As IEnumerable(Of String)) As String
        Return list.First()
    End Function
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "LongestString")
        End Function

        <WorkItem(542927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542927")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryGroupByInto3() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim col = New String() { }

        Dim temp = From x in col
                   Group By x.Length Into Group, $$
    End Sub

    <Extension()>
    Function LongestString(list As IEnumerable(Of String)) As String
        Return list.First()
    End Function
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "LongestString")
        End Function

        <WorkItem(542927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542927")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryGroupByInto4() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim col = New String() { }

        Dim temp = From x in col
                   Group By x.Length Into g = $$
    End Sub

    <Extension()>
    Function LongestString(list As IEnumerable(Of String)) As String
        Return list.First()
    End Function
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "LongestString")
        End Function

        <WorkItem(542929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542929")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryAggregateInto1() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Distinct")
        End Function

        <WorkItem(542929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542929")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryAggregateInto2() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Distinct")
        End Function

        <WorkItem(542929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542929")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInQueryAggregateInto3() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = Distinct(), $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Sum")
        End Function

        <WorkItem(543137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543137")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAndKeywordInComplexJoin() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Module1
    Sub Main(args As String())
        Dim arr = New Byte() {4, 5}
        Dim q2 = From num In arr Join n1 In arr On num.ToString() Equals n1.ToString() And $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "num")
        End Function

        <WorkItem(543181, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543181")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterGroupKeywordInGroupByClause() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim q1 = From i1 In New Integer() {4, 5} Group $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i1")
        End Function

        <WorkItem(543182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543182")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterByInGroupByClause() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim q1 = From i1 In New Integer() {3, 2} Group i1 By $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i1")
        End Function

        <WorkItem(543210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543210")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterByInsideExprVarDeclGroupByClause() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q1 = From i1 In arr Group i1 By i2 = $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i1")
            Await VerifyItemExistsAsync(markup, "arr")
            Await VerifyItemExistsAsync(markup, "args")
        End Function

        <WorkItem(543213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterGroupInsideExprVarDeclGroupByClause() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q1 = From i1 In arr Group i1 = $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "i1")
            Await VerifyItemExistsAsync(markup, "arr")
            Await VerifyItemExistsAsync(markup, "args")
        End Function

        <WorkItem(543246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543246")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAggregateKeyword() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
 
Module Program
    Sub Main(args As String())
            Dim query = Aggregate $$
]]></Text>.Value

            Await VerifyNoItemsExistAsync(markup)
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterDelegateCreationExpression1() As Task
            Dim markup =
<Text>
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2($$
    End Sub

    Delegate Sub Goo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "Goo2")
            Await VerifyItemIsAbsentAsync(markup, "Bar2")
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterDelegateCreationExpression2() As Task
            Dim markup =
<Text>
Module Program
    Sub Main(args As String())
        Dim f1 = New Goo2($$
    End Sub

    Delegate Sub Goo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "Goo2")
            Await VerifyItemIsAbsentAsync(markup, "Bar2")
        End Function

        <WorkItem(619388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619388")>
        <WpfFact(Skip:="619388"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverloadsHiding() As Task
            Dim markup = <Text><![CDATA[
Public Class Base
    Sub Configure()
    End Sub
End Class

Public Class Derived
    Inherits Base
    Overloads Sub Configure()
        Config$$
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Configure", "Sub Derived.Configure()")
            Await VerifyItemIsAbsentAsync(markup, "Configure", "Sub Base.Configure()")
        End Function

        <WorkItem(543580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543580")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterMyBaseDot1() As Task
            Dim markup = <Text><![CDATA[
Public Class Base
    Protected Sub Configure()
        Console.WriteLine("test")
    End Sub
End Class
 
Public Class Inherited
    Inherits Base
    Public Shadows Sub Configure()
        MyBase.$$
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Configure")
        End Function

        <WorkItem(7648, "http://github.com/dotnet/roslyn/issues/7648")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingMyBaseDotInScriptContext() As Task
            Await VerifyItemIsAbsentAsync("MyBase.$$", "ToString", sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <WorkItem(543580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543580")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterMyBaseDot2() As Task
            Dim markup = <Text>
Public Class Base
    Protected Sub Goo()
        Console.WriteLine("test")
    End Sub
End Class
 
Public Class Inherited
    Inherits Base

    Public Sub Bar()
        MyBase.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo")
            Await VerifyItemIsAbsentAsync(markup, "Bar")
        End Function

        <WorkItem(543547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543547")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterRaiseEvent() As Task
            Dim markup = <Text>
Module Program
    Public Event NewRegistrations(ByVal pStudents As String)

    Sub Main(args As String())
        RaiseEvent $$
    End Sub
End Module
</Text>.Value

            Await VerifyItemExistsAsync(markup, "NewRegistrations")
        End Function

        <WorkItem(543730, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543730")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInheritedEventsAfterRaiseEvent() As Task
            Dim markup = <Text>
Class C1
    Event baseEvent
End Class
Class C2
    Inherits C1
    Event derivedEvent(x As Integer)
    Sub M()
        RaiseEvent $$
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "derivedEvent")
            Await VerifyItemIsAbsentAsync(markup, "baseEvent")
        End Function

        <WorkItem(529116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529116")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSingleLineLambda1() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1) $$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "x1")
            Await VerifyItemExistsAsync(markup, "x5")
        End Function

        <WorkItem(529116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529116")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSingleLineLambda2() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1)$$
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "x1")
            Await VerifyItemExistsAsync(markup, "x5")
        End Function

        <WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")>
        <WorkItem(530595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530595")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInstanceFieldsInSharedMethod() As Task
            Dim markup = <Text>
Class C
    Private x As Integer
    Shared Sub M()
        $$
    End Sub
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "x")
        End Function

        <WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInstanceFieldsInSharedFieldInitializer() As Task
            Dim markup = <Text>
Class C
    Private x As Integer
    Private Shared y As Integer = $$
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "x")
        End Function

        <WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedFieldsInSharedMethod() As Task
            Dim markup = <Text>
Class C
    Private Shared x As Integer
    Shared Sub M()
        $$
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "x")
        End Function

        <WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedFieldsInSharedFieldInitializer() As Task
            Dim markup = <Text>
Class C
    Private Shared x As Integer
    Private Shared y As Integer = $$
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "x")
        End Function

        <WorkItem(543680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInstanceFieldsFromOuterClassInInstanceMethod() As Task
            Dim markup = <Text>
Class outer
    Dim i As Integer
    Class inner
        Sub M()
            $$
        End Sub
    End Class
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(markup, "i")
        End Function

        <WorkItem(543680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedFieldsFromOuterClassInInstanceMethod() As Task
            Dim markup = <Text>
Class outer
    Shared i As Integer
    Class inner
        Sub M()
            $$
        End Sub
    End Class
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "i")
        End Function

        <WorkItem(543104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOnlyEnumMembersInEnumTypeMemberAccess() As Task
            Dim markup = <Text>
Class C
    Enum x
        a
        b
        c
    End Enum

    Sub M()
        x.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "a")
            Await VerifyItemExistsAsync(markup, "b")
            Await VerifyItemExistsAsync(markup, "c")
            Await VerifyItemIsAbsentAsync(markup, "Equals")
        End Function

        <WorkItem(539450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539450")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordEscaping1() As Task
            Dim markup = <Text>
Module [Structure]
    Sub M()
        dim [dim] = 0
        console.writeline($$
    End Sub
End Module
</Text>.Value

            Await VerifyItemExistsAsync(markup, "dim")
            Await VerifyItemIsAbsentAsync(markup, "[dim]")
            Await VerifyItemExistsAsync(markup, "Structure")
            Await VerifyItemIsAbsentAsync(markup, "[Structure]")
        End Function

        <WorkItem(539450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539450")> <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordEscaping2() As Task
            Dim markup = <Text>
Module [Structure]
    Sub [dim]()
    End Sub
    Sub [New]()
    End Sub
    Sub [rem]()
        [Structure].$$
    End Sub
End Module
</Text>.Value

            Await VerifyItemExistsAsync(markup, "dim")
            Await VerifyItemIsAbsentAsync(markup, "[dim]")
            Await VerifyItemExistsAsync(markup, "New")
            Await VerifyItemIsAbsentAsync(markup, "[New]")
            Await VerifyItemExistsAsync(markup, "rem")
            Await VerifyItemIsAbsentAsync(markup, "[rem]")
        End Function

        <WorkItem(539450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539450")> <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordEscaping3() As Task
            Dim markup = <Text>
Namespace Goo
    Module [Structure]
        Sub M()
            Dim x as Goo.$$
        End Sub
    End Module
End Namespace
</Text>.Value

            Await VerifyItemExistsAsync(markup, "Structure")
            Await VerifyItemIsAbsentAsync(markup, "[Structure]")
        End Function

        <WorkItem(539450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539450")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeKeywordEscaping() As Task
            Dim markup = <Text>
Imports System
Class classattribute : Inherits Attribute
End Class
&lt;$$
Class C
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "class")
            Await VerifyItemIsAbsentAsync(markup, "[class]")
        End Function

        <WorkItem(645898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645898")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EscapedKeywordAttributeCommit() As Task
            Dim markup = <Text>
Imports System
Class classattribute : Inherits Attribute
End Class
&lt;$$
Class C
End Class
</Text>.Value

            Dim expected = <Text>
Imports System
Class classattribute : Inherits Attribute
End Class
&lt;[class](
Class C
End Class
</Text>.Value

            Await VerifyProviderCommitAsync(markup, "class", expected, "("c, "")
        End Function

        <WorkItem(543104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllMembersInEnumLocalAccess() As Task
            Dim markup = <Text>
Class C
    Enum x
        a
        b
        c
    End Enum

    Sub M()
        Dim y = x.a
        y.$$
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "a")
            Await VerifyItemExistsAsync(markup, "b")
            Await VerifyItemExistsAsync(markup, "c")
            Await VerifyItemExistsAsync(markup, "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReadOnlyPropertiesPresentOnRightSideInObjectInitializer() As Task
            Dim text = <a>Class C
    Public Property Goo As Integer
    Public ReadOnly Property Bar As Integer
        Get
            Return 0
        End Get
    End Property

    Sub M()
        Dim c As New C With { .Goo = .$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "Goo")
            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableNotBeforeExplicitDeclaration_ExplicitOff() As Task
            Dim text = <Text>
Option Explicit Off
Class C
    Sub M()
        $$
        Dim goo = 3
    End Sub
End Class</Text>.Value

            Await VerifyItemIsAbsentAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableNotBeforeExplicitDeclaration_ExplicitOn() As Task
            Dim text = <Text>
Option Explicit On
Class C
    Sub M()
        $$
        Dim goo = 3
    End Sub
End Class</Text>.Value

            Await VerifyItemIsAbsentAsync(text, "goo")
        End Function

        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <WorkItem(530595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530595")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableBeforeImplicitDeclaration() As Task
            Dim text = <Text>
Option Explicit Off
Class C
    Function M() as Integer
        $$
        Return goo
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableInItsDeclaration() As Task
            ' "Dim goo As Integer = goo" is legal code while "Dim goo = goo" is not, but
            ' offer the local name on the right in either case because in the second
            ' case there's an error stating that goo needs to be explicitly typed and
            ' the user can then add the As clause. This mimics the behavior of 
            ' "var x = x = 0" in C#.
            Dim text = <Text>
Class C
    Sub M()
        Dim goo = $$
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableInItsDeclarator() As Task
            Dim text = <Text>
Class C
    Sub M()
        Dim goo = 4, bar = $$, baz = 5
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableNotBeforeItsDeclarator() As Task
            Dim text = <Text>
Class C
    Sub M()
        Dim goo = $$, bar = 5
    End Sub
End Class</Text>.Value

            Await VerifyItemIsAbsentAsync(text, "bar")
        End Function

        <Fact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLocalVariableAfterDeclarator() As Task
            Dim text = <Text>
Class C
    Sub M()
        Dim goo = 5, bar = $$
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(545439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545439")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayAfterReDim() As Task
            Dim text = <Text>
Class C
    Sub M()
        Dim goo(10, 20) As Integer
        ReDim $$
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(545439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545439")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayAfterReDimPreserve() As Task
            Dim text = <Text>
Class C
    Sub M()
        Dim goo(10, 20) As Integer
        ReDim Preserve $$
    End Sub
End Class</Text>.Value

            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact>
        <WorkItem(546353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546353")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNamespaceDeclarationIntellisense() As Task
            Dim text = <Text>
Namespace Goo.$$
Class C
End Class</Text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(531258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531258")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLabelsAfterOnErrorGoTo() As Task
            Dim code =
<Code>
Class C
    Sub M()
        On Error GoTo $$

        label1:
            Dim x = 1
    End Sub
End Class</Code>.Value

            Await VerifyItemExistsAsync(code, "label1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAwaitableItem() As Task
            Dim code =
<Code>
Imports System.Threading.Tasks
Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Goo() As Task
        Me.$$
        End Function
End Class</Code>.Value

            Dim description =
$"<{VBFeaturesResources.Awaitable}> Function C.Goo() As Task
Doc Comment!
{WorkspacesResources.Usage_colon}
  {SyntaxFacts.GetText(SyntaxKind.AwaitKeyword)} Goo()"

            Await VerifyItemWithMscorlib45Async(code, "Goo", description, LanguageNames.VisualBasic)
        End Function

        <WorkItem(550760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550760")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterAwait() As Task
            Dim code =
<Code>
Imports System.Threading.Tasks
Class SomeClass
    Public Async Sub goo()
        Await $$
    End Sub
 
    Async Function Bar() As Task(Of Integer)
        Return Await Task.Run(Function() 42)
    End Function
End Class</Code>.Value


            Await VerifyItemExistsAsync(code, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObsoleteItem() As Task
            Dim code =
<Code>
Imports System
Class SomeClass
    &lt;Obsolete&gt;
    Public Sub Goo()
        $$
    End Sub
End Class</Code>.Value

            Await VerifyItemExistsAsync(code, "Goo", $"({VBFeaturesResources.Deprecated}) Sub SomeClass.Goo()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExpressionAfterYield() As Task
            Dim code =
<Code>
Class SomeClass
    Iterator Function Goo() As Integer
        Dim x As Integer
        Yield $$
    End Function
End Class
</Code>.Value

            Await VerifyItemExistsAsync(code, "x")
        End Function

        <WorkItem(568986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoMembersOnDottingIntoUnboundType() As Task
            Dim code =
<Code>
Module Program
    Dim goo As RegistryKey

    Sub Main(args() As String)
        goo.$$
    End Sub
End Module
</Code>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(611154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611154")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoOperators() As Task
            Await VerifyItemIsAbsentAsync(
                    AddInsideMethod("String.$$"), "op_Equality")
        End Function

        <WorkItem(736891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736891")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInBinaryConditionalExpression() As Task
            Dim code =
<Code>
Module Program
    Sub Main(args() As String)
        args = If($$
    End Sub
End Module
</Code>.Value

            Await VerifyItemExistsAsync(code, "args")
        End Function

        <WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInTopLevelFieldInitializer() As Task
            Dim code =
<Code>
Dim aaa = 1
Dim bbb = $$
</Code>.Value

            Await VerifyItemExistsAsync(code, "aaa")
        End Function

#End Region

#Region "SharedMemberSourceTests"

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation1() As Task
            Await VerifyItemIsAbsentAsync("System.Console.$$", "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation2() As Task
            Await VerifyItemIsAbsentAsync(AddImportsStatement("Imports System", "System.Console.$$"), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation3() As Task
            Await VerifyItemIsAbsentAsync("Imports System.Console.$$", "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation4() As Task
            Await VerifyItemIsAbsentAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "' Console.$$")), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation5() As Task
            Await VerifyItemIsAbsentAsync(AddImportsStatement("Imports System", AddInsideMethod("Dim d = ""Console.$$")), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvalidLocation6() As Task
            Await VerifyItemIsAbsentAsync("<System.Console.$$>", "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInsideMethodBody() As Task
            Await VerifyItemExistsAsync(AddImportsStatement("Imports System", AddInsideMethod("Console.$$")), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInsideAccessorBody() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "     Property Prop As String",
                                  "         Get",
                                  "             Console.$$")), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldInitializer() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "     Dim d = Console.$$")), "Beep")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMethods() As Task
            Await VerifyItemExistsAsync(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "Private Shared Function Method() As Boolean",
                                  "End Function",
                                  "     Dim d = $$",
                                  "")), "Method")
        End Function

#End Region

#Region "EditorBrowsableTests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function


        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
   Sub M()
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_Overloads_BothBrowsableAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar(x as Integer)     
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Method_Overloads_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverriddenSymbolsFilteredFromCompletionList() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim d as D
        d.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    Public Overridable Sub Goo(original As Integer) 
    End Sub
End Class

Public Class D
    Inherits B
    Public Overrides Sub Goo(derived As Integer) 
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c = New C()
        c.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class C
    Public Sub Goo() 
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim d = new D()
        d.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class B
    Public Sub Goo() 
    End Sub
End Class

Public Class D
    Inherits B
    Public Overloads Sub Goo(x As Integer)
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_HidingWithDifferentArgumentList() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim d = new D()
        d.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class B
    Public Sub Goo() 
    End Sub
End Class

Public Class D
    Inherits B
    Public Sub Goo(x As Integer)
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_BrowsableStateNeverMethodsInBaseClass() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Inherits B
    Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo() 
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = new C(Of Integer)()
        ci.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Goo(t As T)  
    End Sub

    Public Sub Goo(i as Integer)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = new C(Of Integer)()
        ci.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t as T)  
    End Sub

    Public Sub Goo(i as Integer)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = new C(Of Integer)()
        ci.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Goo(t As T)  
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = new C(Of Integer)()
        ci.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)  
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii = new C(Of Integer, Of Integer)()
        cii.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    Public Sub Goo(t As T)
    End Sub
  
    Public Sub Goo(u As U)
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii = new C(Of Integer, Of Integer)()
        cii.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)
    End Sub
  
    Public Sub Goo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii = new C(Of Integer, Of Integer)()
        cii.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)
    End Sub
  
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Field_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public bar As Integer
End Class
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Field_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public bar As Integer
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Field_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public bar As Integer
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Property_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Property Bar As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    Public Property Bar As Integer
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Get
            Return 5
        End Get
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Set(value As Integer)
        End Set
    End Property
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Property_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Property Bar As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Property_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim goo As Goo
        goo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Property Bar As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Constructor_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Constructor_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Constructor_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Constructor_MixedOverloads1() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub

    Public Sub New(x As Integer)    
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Constructor_MixedOverloads2() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Event_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C
        AddHandler c.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    Delegate Sub DelegateType()
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Event Handler As DelegateType
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Event_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C
        AddHandler c.$$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    Delegate Sub DelegateType()
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Event Handler As DelegateType
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Event_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C
        AddHandler c.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    Delegate Sub DelegateType()
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Event Handler As DelegateType
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Delegate_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Delegate Sub DelegateType()
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Delegate_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Delegate Sub DelegateType()
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Delegate_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Delegate Sub DelegateType()
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateNever_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateNever_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateNever_FullyQualified() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As NS.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Namespace NS
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class C
    End Class
End Namespace
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAlways_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAlways_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAlways_FullyQualified() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As NS.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Namespace NS
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Class C
    End Class
End Namespace
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Class Goo
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_BrowsableStateAdvanced_FullyQualified() As Task
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As NS.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Namespace NS
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Class C
    End Class
End Namespace
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Class_IgnoreBaseClassBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    Inherits Bar
End Class

<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Bar
End Class
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Struct_BrowsableStateNever_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Structure Goo
End Structure
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Structure Goo
End Structure
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Structure Goo
End Structure
]]></Text>.Value

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Enum_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Enum Goo
    A
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Enum_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Enum Goo
    A
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Enum_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Enum Goo
    A
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateNever_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateNever_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Interface Goo
End Interface
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_CrossLanguage_VBtoCS_Always() As Task
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Goo
{
}
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.CSharp,
                hideAdvancedMembers:=False)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_CrossLanguage_VBtoCS_Never() As Task
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Goo
{
}
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Goo",
                expectedSymbolsSameSolution:=0,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.CSharp,
                hideAdvancedMembers:=False)
        End Function
#End Region

#Region "Inherits/Implements Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AfterInherits() As Task
            Const markup = "
Public Class Base
End Class

Class Derived
    Inherits $$
End Class
"

            Await VerifyItemExistsAsync(markup, "Base")
            Await VerifyItemIsAbsentAsync(markup, "Derived")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AfterInheritsDotIntoClass() As Task
            Const markup = "
Public Class Base
    Public Class Nest
    End Class
End Class

Class Derived
    Inherits Base.$$
End Class
"

            Await VerifyItemExistsAsync(markup, "Nest")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_AfterImplements() As Task
            Const markup = "
Public Interface IGoo
End Interface

Class C
    Implements $$
End Class
"

            Await VerifyItemExistsAsync(markup, "IGoo")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_AliasedInterfaceAfterImplements() As Task
            Const markup = "
Imports IAlias = IGoo
Public Interface IGoo
End Interface

Class C
    Implements $$
End Class
"

            Await VerifyItemExistsAsync(markup, "IAlias")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_AliasedNamespaceAfterImplements() As Task
            Const markup = "
Imports AliasedNS = NS1
Namespace NS1
    Public Interface IGoo
    End Interface

    Class C
        Implements $$
    End Class
End Namespace
"

            Await VerifyItemExistsAsync(markup, "AliasedNS")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AliasedClassAfterInherits() As Task
            Const markup = "
Imports AliasedClass = Base
Public Class Base
End Interface

Class C
    Inherits $$
End Class
"

            Await VerifyItemExistsAsync(markup, "AliasedClass")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AliasedNamespaceAfterInherits() As Task
            Const markup = "
Imports AliasedNS = NS1
Namespace NS1
Public Class Base
End Interface

Class C
    Inherits $$
    End Class
End Namespace
"

            Await VerifyItemExistsAsync(markup, "AliasedNS")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AliasedClassAfterInherits2() As Task
            Const markup = "
Imports AliasedClass = NS1.Base
Namespace NS1
Public Class Base
End Interface

Class C
    Inherits $$
    End Class
End Namespace
"

            Await VerifyItemExistsAsync(markup, "AliasedClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_AfterImplementsComma() As Task
            Const markup = "
Public Interface IGoo
End Interface

Public Interface IBar
End interface

Class C
    Implements IGoo, $$
End Class
"

            Await VerifyItemExistsAsync(markup, "IBar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_ClassContainingInterface() As Task
            Const markup = "
Public Class Base
    Public Interface Nest
    End Class
End Class

Class Derived
    Implements $$
End Class
"

            Await VerifyItemExistsAsync(markup, "Base")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_NoClassNotContainingInterface() As Task
            Const markup = "
Public Class Base
End Class

Class Derived
    Implements $$
End Class
"

            Await VerifyItemIsAbsentAsync(markup, "Base")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_GenericClass() As Task
            Const markup = "
Public Class base(Of T)

End Class

Public Class derived
    Inherits $$
End Class
"

            Await VerifyItemExistsAsync(markup, "base", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_GenericInterface() As Task
            Const markup = "
Public Interface IGoo(Of T)

End Interface

Public Class bar
    Implements $$
End Class
"

            Await VerifyItemExistsAsync(markup, "IGoo", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(546610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546610")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_IncompleteClassDeclaration() As Task
            Const markup = "
Public Interface IGoo
End Interface
Public Interface IBar
End interface
Class C
    Implements IGoo,$$
"

            Await VerifyItemExistsAsync(markup, "IBar")
        End Function

        <WorkItem(546611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546611")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_NotNotInheritable() As Task
            Const markup = "
Public NotInheritable Class D
End Class
Class C
    Inherits $$
"

            Await VerifyItemIsAbsentAsync(markup, "D")
        End Function

        <WorkItem(546802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546802")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_KeywordIdentifiersShownUnescaped() As Task
            Const markup = "
Public Class [Inherits]
End Class
Class C
    Inherits $$
"

            Await VerifyItemExistsAsync(markup, "Inherits")
        End Function

        <WorkItem(546802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546802")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Inherits_KeywordIdentifiersCommitEscaped() As Task
            Const markup = "
Public Class [Inherits]
End Class
Class C
    Inherits $$
"

            Const expected = "
Public Class [Inherits]
End Class
Class C
    Inherits [Inherits].
"

            Await VerifyProviderCommitAsync(markup, "Inherits", expected, "."c, "")
        End Function

        <WorkItem(546801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546801")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_Modules() As Task
            Const markup = "
Module Module1
Sub Main()
End Sub
End Module
Module Module2
  Class Bx
  End Class

End Module 

Class Max
  Class Bx
  End Class
End Class

Class A
Inherits $$

End Class
"

            Await VerifyItemExistsAsync(markup, "Module2")
            Await VerifyItemIsAbsentAsync(markup, "Module1")
        End Function

        <WorkItem(530726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530726")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_DoNotShowNamespaceWithNoApplicableClasses() As Task
            Const markup = "
Namespace N
    Module M
    End Module
End Namespace
Class C
    Inherits $$
End Class
"

            Await VerifyItemIsAbsentAsync(markup, "N")
        End Function

        <WorkItem(530725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530725")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_CheckStructContents() As Task
            Const markup = "
Namespace N
    Public Structure S1
        Public Class B
        End Class
    End Structure
    Public Structure S2
    End Structure
End Namespace
Class C
    Inherits N.$$
End Class
"
            Await VerifyItemIsAbsentAsync(markup, "S2")
            Await VerifyItemExistsAsync(markup, "S1")
        End Function

        <WorkItem(530724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530724")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_NamespaceContainingInterface() As Task
            Const markup = "
Namespace N
    Interface IGoo
    End Interface
End Namespace
Class C
    Implements $$
End Class
"

            Await VerifyItemExistsAsync(markup, "N")
        End Function

        <WorkItem(531256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531256")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_OnlyInterfacesForInterfaceInherits1() As Task
            Const markup = "
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IGoo
    Inherits $$
"

            Await VerifyItemExistsAsync(markup, "ITestInterface")
            Await VerifyItemIsAbsentAsync(markup, "TestClass")
        End Function

        <WorkItem(1036374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036374")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_InterfaceCircularInheritance() As Task
            Const markup = "
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface A(Of T)
    Inherits A(Of A(Of T))
    Interface B
        Inherits $$
    End Interface
End Interface
"

            Await VerifyItemExistsAsync(markup, "ITestInterface")
            Await VerifyItemIsAbsentAsync(markup, "TestClass")
        End Function

        <WorkItem(531256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531256")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_OnlyInterfacesForInterfaceInherits2() As Task
            Const markup = "
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IGoo
    Implements $$
"

            Await VerifyItemIsAbsentAsync(markup, "ITestInterface")
            Await VerifyItemIsAbsentAsync(markup, "TestClass")
        End Function

        <WorkItem(547291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547291")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Inherits_CommitGenericOnParen() As Task
            Const markup = "
Class G(Of T)
End Class

Class DG
    Inherits $$
End Class
"

            Dim expected = "
Class G(Of T)
End Class

Class DG
    Inherits G(
End Class
"

            Await VerifyProviderCommitAsync(markup, "G(Of " & s_unicodeEllipsis & ")", expected, "("c, "")
        End Function

        <WorkItem(579186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579186")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements_AfterImplementsWithCircularInheritance() As Task
            Const markup = "
Interface I(Of T)
End Interface
 
Class C(Of T)
    Class D
        Inherits C(Of D)
        Implements $$
    End Class
End Class
"

            Await VerifyItemExistsAsync(markup, "I", displayTextSuffix:="(Of …)")
        End Function

        <WorkItem(622563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622563")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Inherits_CommitNonGenericOnParen() As Task
            Const markup = "
Class G
End Class

Class DG
    Inherits $$
End Class
"

            Dim expected = "
Class G
End Class

Class DG
    Inherits G(
End Class
"
            Await VerifyProviderCommitAsync(markup, "G", expected, "("c, "")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_AfterInheritsWithCircularInheritance() As Task
            Const markup = "
Class B
End Class
 
Class C(Of T)
    Class D
        Inherits C(Of D)
        Inherits $$
    End Class
End Class
"

            Await VerifyItemExistsAsync(markup, "B")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_ClassesInsideSealedClasses() As Task
            Const markup = "
Public NotInheritable Class G
    Public Class H

    End Class
End Class

Class SomeClass
    Inherits $$

End Class
"

            Await VerifyItemExistsAsync(markup, "G")
        End Function

        <WorkItem(638762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638762")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInherits_ClassWithinNestedStructs() As Task
            Const markup = "
Structure somestruct
    Structure Inner
        Class FinallyAClass
        End Class
    End Structure
 
End Structure
Class SomeClass
    Inherits $$

End
"

            Await VerifyItemExistsAsync(markup, "somestruct")
        End Function

#End Region

        <WorkItem(715146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715146")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExtensionMethodsOffered() As Task
            Dim markup = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Class Program
    Sub Main(args As String())
        Me.$$
    End Sub
End Class
Module Extensions
    <Extension>
    Sub Goo(program As Program)
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo")
        End Function

        <WorkItem(715146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715146")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExtensionMethodsOffered2() As Task
            Dim markup = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Class Program
    Sub Main(args As String())
        Dim a = new Program()
        a.$$
    End Sub
End Class
Module Extensions
    <Extension>
    Sub Goo(program As Program)
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo")
        End Function

        <WorkItem(715146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715146")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLinqExtensionMethodsOffered() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Sub Main(args As String())
        Dim a as IEnumerable(Of Integer) = Nothing
        a.$$
    End Sub
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Average")
        End Function

        <WorkItem(884060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884060")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionOffTypeParameter() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Function bar(Of T As Object)() As T
        T.$$
    End Function
End Moduleb
End Class
]]></Text>.Value

            Await VerifyNoItemsExistAsync(markup)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAvailableInBothLinkedFiles() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
    Dim x as integer
    sub goo()
        $$
    end sub
end class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences=" True" AssemblyName="Proj2">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Await VerifyItemInLinkedFilesAsync(markup, "x", $"({FeaturesResources.field}) C.x As Integer")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAvailableInOneLinkedFile() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1" PreprocessorSymbols="GOO=True">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
#If GOO Then
    Dim x as integer
#End If
            Sub goo()
        $$
    End Sub
        End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj2">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Dim expectedDescription = $"({FeaturesResources.field}) C.x As Integer" + vbCrLf + vbCrLf + String.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available) + vbCrLf + String.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available) + vbCrLf + vbCrLf + FeaturesResources.You_can_use_the_navigation_bar_to_switch_context
            Await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription)
        End Function

        <WorkItem(13161, "https://github.com/dotnet/roslyn/issues/13161")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitGenericOnTab() As Task
            Dim text =
<code>
Class G(Of T)
End Class

Class DG
    Function Bar() as $$
End Class</code>.Value

            Dim expected =
<code>
Class G(Of T)
End Class

Class DG
    Function Bar() as G(Of
End Class</code>.Value

            Await VerifyProviderCommitAsync(text, "G(Of …)", expected, Nothing, "")
        End Function

        <WorkItem(909121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909121")>
        <WorkItem(2048, "https://github.com/dotnet/roslyn/issues/2048")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitGenericOnParen() As Task
            Dim text =
<code>
Class G(Of T)
End Class

Class DG
    Function Bar() as $$
End Class</code>.Value

            Dim expected =
<code>
Class G(Of T)
End Class

Class DG
    Function Bar() as G(
End Class</code>.Value

            Await VerifyProviderCommitAsync(text, "G(Of …)", expected, "("c, "")
        End Function

        <WorkItem(668159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/668159")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributesShownWithBraceCompletionActive() As Task
            Dim text =
<code><![CDATA[
Imports System
<$$>
Class C
End Class

Class GooAttribute
    Inherits Attribute
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <WorkItem(991466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescriptionInAliasedType() As Task
            Dim text =
<code><![CDATA[
Imports IAlias = IGoo
Class C
    Dim x as IA$$
End Class

''' <summary>
''' summary for interface IGoo
''' </summary>
Interface IGoo
    Sub Bar()
End Interface
]]></code>.Value

            Await VerifyItemExistsAsync(text, "IAlias", expectedDescriptionOrNull:="Interface IGoo" + vbCrLf + "summary for interface IGoo")
        End Function

        <WorkItem(842049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/842049")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMergedNamespace1() As Task
            Dim text =
<code><![CDATA[
Imports A
Imports B
 
Namespace A.X
    Class C
    End Class
End Namespace
 
Namespace B.X
    Class D
    End Class
End Namespace
 
Module M
    Dim c As X.C
    Dim d As X.D
    Dim e As X.$$
End Module

]]></code>.Value

            Await VerifyItemExistsAsync(text, "C")
            Await VerifyItemExistsAsync(text, "D")
        End Function

        <WorkItem(842049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/842049")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMergedNamespace2() As Task
            Dim text =
<code><![CDATA[
Imports A
Imports B
 
Namespace A.X
    Class C
    End Class
End Namespace
 
Namespace B.X
    Class D
    End Class
End Namespace
 
Module M
    Dim c As X.C
    Dim d As X.D
    
    Sub Goo()
        X.$$
    End Sub
End Module

]]></code>.Value

            Await VerifyItemExistsAsync(text, "C")
            Await VerifyItemExistsAsync(text, "D")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_EmptyNameSpan_TopLevel() As Task
            Dim source = <code><![CDATA[
Namespace $$
End Namespace
]]></code>.Value
            Await VerifyItemExistsAsync(source, "System")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_EmptyNameSpan_Nested() As Task
            Dim source = <code><![CDATA[
Namespace System
    Namespace $$
    End Namespace
End Namespace
]]></code>.Value
            Await VerifyItemExistsAsync(source, "Runtime")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_TopLevelNoPeers() As Task
            Dim source = <code><![CDATA[
Imports System;

Namespace $$

]]></code>.Value
            Await VerifyItemExistsAsync(source, "System")
            Await VerifyItemIsAbsentAsync(source, "String")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_TopLevelWithPeer() As Task
            Dim source = <code><![CDATA[
Namespace A
End Namespace

Namespace $$

]]></code>.Value


            Await VerifyItemExistsAsync(source, "A")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_NestedWithNoPeers() As Task
            Dim source = <code><![CDATA[
Namespace A

    Namespace $$

End Namespace

]]></code>.Value

            Await VerifyNoItemsExistAsync(source)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_NestedWithPeer() As Task
            Dim source = <code><![CDATA[
Namespace A

    Namespace B
    End Namespace

    Namespace $$

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemExistsAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_ExcludesCurrentDeclaration() As Task
            Dim source = <code><![CDATA[Namespace N$$S]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "NS")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_WithNested() As Task
            Dim source = <code><![CDATA[
Namespace A

    Namespace $$
    
        Namespace B
        End Namespace
    
    End Namespace

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemIsAbsentAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_WithNestedAndMatchingPeer() As Task
            Dim source = <code><![CDATA[
Namespace A.B
End Namespace

Namespace A

    Namespace $$

        Namespace B
        End Namespace

    End Namespace

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemExistsAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_InnerCompletionPosition() As Task
            Dim source = <code><![CDATA[
Namespace Sys$$tem
End Namespace

]]></code>.Value

            Await VerifyItemExistsAsync(source, "System")
            Await VerifyItemIsAbsentAsync(source, "Runtime")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Unqualified_IncompleteDeclaration() As Task
            Dim source = <code><![CDATA[
Namespace A

    Namespace B

        Namespace $$

        Namespace C1
        End Namespace

    End Namespace

    Namespace B.C2
    End Namespace

End Namespace

Namespace A.B.C3
End Namespace

]]></code>.Value

            ' Ideally, all the C* namespaces would be recommended but, because of how the parser
            ' recovers from the missing end statement, they end up with the following qualified names...
            '
            '     C1 => A.B.?.C1
            '     C2 => A.B.B.C2
            '     C3 => A.A.B.C3
            '
            ' ...none of which are found by the current algorithm.
            Await VerifyItemIsAbsentAsync(source, "C1")
            Await VerifyItemIsAbsentAsync(source, "C2")
            Await VerifyItemIsAbsentAsync(source, "C3")

            Await VerifyItemIsAbsentAsync(source, "A")

            ' Because of the above, B does end up in the completion list
            ' since A.B.B appears to be a peer of the New declaration
            Await VerifyItemExistsAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_NoPeers() As Task

            Dim source = <code><![CDATA[Namespace A.$$]]></code>.Value
            Await VerifyNoItemsExistAsync(source)

        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_TopLevelWithPeer() As Task

            Dim source = <code><![CDATA[
Namespace A.B
End Namespace

Namespace A.$$
]]></code>.Value

            Await VerifyItemExistsAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_NestedWithPeer() As Task

            Dim source = <code><![CDATA[
Namespace A

    Namespace B.C
    End Namespace

    Namespace B.$$

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemIsAbsentAsync(source, "B")
            Await VerifyItemExistsAsync(source, "C")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_WithNested() As Task

            Dim source = <code><![CDATA[
Namespace A.$$

    Namespace B
    End Namespace

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemIsAbsentAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_WithNestedAndMatchingPeer() As Task

            Dim source = <code><![CDATA[
Namespace A.B
End Namespace

Namespace A.$$

    Namespace B
    End Namespace

End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemExistsAsync(source, "B")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_InnerCompletionPosition() As Task
            Dim source = <code><![CDATA[
Namespace Sys$$tem.Runtime
End Namespace

]]></code>.Value

            Await VerifyItemExistsAsync(source, "System")
            Await VerifyItemIsAbsentAsync(source, "Runtime")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_Qualified_IncompleteDeclaration() As Task

            Dim source = <code><![CDATA[
Namespace A

    Namespace B

        Namespace C.$$

        Namespace C.D1
        End Namespace

    End Namespace

    Namespace B.C.D2
    End Namespace

End Namespace

Namespace A.B.C.D3
End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "A")
            Await VerifyItemIsAbsentAsync(source, "B")
            Await VerifyItemIsAbsentAsync(source, "C")

            ' Ideally, all the D* namespaces would be recommended but, because of how the parser
            ' recovers from the end statement, they end up with the following qualified names...
            '
            '     D1 => A.B.C.C.?.D1
            '     D2 => A.B.B.C.D2
            '     D3 => A.A.B.C.D3
            '
            ' ...none of which are found by the current algorithm.
            Await VerifyItemIsAbsentAsync(source, "D1")
            Await VerifyItemIsAbsentAsync(source, "D2")
            Await VerifyItemIsAbsentAsync(source, "D3")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_OnKeyword() As Task
            Dim source = <code><![CDATA[
Name$$space System
End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "System")
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceName_OnNestedKeyword() As Task
            Dim source = <code><![CDATA[
Namespace System
    Name$$space Runtime
    End Namespace
End Namespace

]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "System")
            Await VerifyItemIsAbsentAsync(source, "Runtime")
        End Function

        <WorkItem(925469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/925469")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitWithCloseBracketLeaveOpeningBracket1() As Task
            Dim text =
<code><![CDATA[
Class Await
    Sub Goo()
        Dim x = new [Awa$$]
    End Sub
End Class]]></code>.Value

            Dim expected =
<code><![CDATA[
Class Await
    Sub Goo()
        Dim x = new [Await]
    End Sub
End Class]]></code>.Value

            Await VerifyProviderCommitAsync(text, "Await", expected, "]"c, Nothing)
        End Function

        <WorkItem(925469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/925469")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitWithCloseBracketLeavesOpeningBracket2() As Task
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Goo()
        Dim x = new [Cla$$]
    End Sub
End Class]]></code>.Value

            Dim expected =
<code><![CDATA[
Class [Class]
    Sub Goo()
        Dim x = new [Class]
    End Sub
End Class]]></code>.Value

            Await VerifyProviderCommitAsync(text, "Class", expected, "]"c, Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConditionalOperatorCompletion() As Task
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Goo()
        Dim x = new Object()
        x?.$$
    End Sub
End Class]]></code>.Value

            Await VerifyItemExistsAsync(text, "ToString")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConditionalOperatorCompletion2() As Task
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Goo()
        Dim x = new Object()
        x?.ToString()?.$$
    End Sub
End Class]]></code>.Value

            Await VerifyItemExistsAsync(text, "ToString")
        End Function

        <WorkItem(1041269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041269")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestHidePropertyBackingFieldAndEventsAtExpressionLevel() As Task
            Dim text =
<code><![CDATA[
Imports System
Class C
Property p As Integer = 15
Event e as EventHandler
Sub f()
Dim x = $$
End Sub
End Class
]]></code>.Value

            Await VerifyItemIsAbsentAsync(text, "_p")
            Await VerifyItemIsAbsentAsync(text, "e")
        End Function

        <WorkItem(1041269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041269")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNullableForConditionalAccess() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub Goo()
        Dim x as Integer? = Nothing
        x?.$$
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "GetTypeCode")
            Await VerifyItemIsAbsentAsync(text, "HasValue")
        End Function

        <WorkItem(1079694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079694")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDontThrowForNullPropagatingOperatorInErase() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main()
        Dim x?(1)
        Erase x?.$$
    End Sub
End Module
]]></code>.Value

            Await VerifyItemExistsAsync(text, "ToString")
        End Function

        <WorkItem(1109319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUnwrapNullableForConditionalFromStructure() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As A
        x?.$$b?.c
    End Sub
End Module

Structure A
    Public b As B
End Structure

Public Class B
    Public c As C
End Class

Public Class C
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "b")
            Await VerifyItemIsAbsentAsync(text, "c")
        End Function

        <WorkItem(1109319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithinChainOfConditionalAccess() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As A
        x?.$$b?.c
    End Sub
End Module

Class A
    Public b As B
End Class

Public Class B
    Public c As C
End Class

Public Class C
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "b")
            Await VerifyItemIsAbsentAsync(text, "c")
        End Function


        <WorkItem(1079694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079694")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDontThrowForNullPropagatingOperatorOnTypeParameter() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Goo(Of T)(x As T)
        x?.$$
    End Sub
End Module
]]></code>.Value

            Await VerifyItemExistsAsync(text, "ToString")
        End Function

        <WorkItem(1079723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079723")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionAfterNullPropagatingOperatingInWithBlock() As Task
            Dim text =
<code><![CDATA[
Option Strict On
Module Program
    Sub Main()
        Dim s = ""
        With s
            ?.$$
        End With
    End Sub
End Module 
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Length")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext1() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext2() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext3() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext4() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext5() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext6() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext7() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext8() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext9() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(Me.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext10() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(Me.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext11() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyClass.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext12() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyClass.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext13() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemIsAbsentAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext14() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub

    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            Await VerifyItemIsAbsentAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext15() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class

Class D
    Inherits C

    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInNameOfArgumentContext16() As Task
            Dim text =
<code><![CDATA[
Class C
    Shared Sub Goo()
    End Sub

    Sub Bar()
    End Sub
End Class

Class D
    Inherits C

    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInInterpolationExpressionContext1() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{$$}"
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "x")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowCompletionInInterpolationExpressionContext2() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{$$
]]></code>.Value

            Await VerifyItemExistsAsync(text, "x")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionInInterpolationAlignmentContext() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{x,$$}"
    End Sub
End Class
]]></code>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionInInterpolationFormatContext() As Task
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{x:$$}"
    End Sub
End Class
]]></code>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(1293, "https://github.com/dotnet/roslyn/issues/1293")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTriggeredAfterDotInWithAfterNumericLiteral() As Task
            Dim text =
<code><![CDATA[
Class Program
    Public Property P As Long

    Sub M()
        With Me
            .P = 122
            .$$
        End With
    End Sub
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "M", usePreviousCharAsTrigger:=True)
        End Function

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionForConditionalAccessOnTypes1() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        System?.$$
    End Sub
End Module
]]></code>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionForConditionalAccessOnTypes2() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Console?.$$
    End Sub
End Module
]]></code>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCompletionForConditionalAccessOnTypes3() As Task
            Dim text =
<code><![CDATA[
Imports a = System
Module Program
    Sub Main(args As String())
        a?.$$
    End Sub
End Module
]]></code>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(3086, "https://github.com/dotnet/roslyn/issues/3086")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedMembersOffInstanceInColorColor() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x = C.$$
    End Sub

    Dim C As New C()
End Module

Class C
    Public X As Integer = 1
    Public Shared Y As Integer = 2
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "X")
            Await VerifyItemExistsAsync(text, "Y")
        End Function

        <WorkItem(3086, "https://github.com/dotnet/roslyn/issues/3086")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotSharedMembersOffAliasInColorColor() As Task
            Dim text =
<code><![CDATA[
Imports B = C
Module Program
    Sub Main(args As String())
        Dim x = B.$$
    End Sub

    Dim B As New B()
End Module

Class C
    Public X As Integer = 1
    Public Shared Y As Integer = 2
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(text, "X")
            Await VerifyItemIsAbsentAsync(text, "Y")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType() As Task
            Dim text =
<code><![CDATA[
MustInherit Class Test
    Private _field As Integer
    NotInheritable Class InnerTest
        Inherits Test
        Sub SomeTest()
            Dim x = $$
        End Sub
    End Class
End Class
]]></code>.Value
            Await VerifyItemExistsAsync(text, "_field")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType2() As Task
            Dim text =
<code><![CDATA[
Class C(Of T)
    Sub M()
    End Sub
    Class N
        Inherits C(Of Integer)
        Sub Test()
            $$ ' M recommended and accessible
        End Sub
        Class NN
            Sub Test2()
                ' M inaccessible and not recommended
            End Function
        End Class
    End Class
End Class
]]></code>.Value
            Await VerifyItemExistsAsync(text, "M")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType3() As Task
            Dim text =
<code><![CDATA[
Class C(Of T)
    Sub M()
    End Sub
    Class N
        Inherits C(Of Integer)
        Sub Test()
            ' M recommended and accessible
        End Sub
        Class NN
            Sub Test2()
                $$ ' M inaccessible and not recommended
            End Function
        End Class
    End Class
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "M")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType4() As Task
            Dim text =
<code><![CDATA[
Class C(Of T)
    Sub M()
    End Sub
    Class N
        Inherits C(Of Integer)
        Sub Test()
            M() ' M recommended and accessible
        End Sub
        Class NN
            Inherits N
            Sub Test2()
                $$ ' M inaccessible and not recommended
            End Function
        End Class
    End Class
End Class
]]></code>.Value
            Await VerifyItemExistsAsync(text, "M")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType5() As Task
            Dim text =
<code><![CDATA[
Class D
    Public Sub Q()
    End Sub
End Class
Class C(Of T)
    Inherits D
    Class N
        Sub Test()
            $$
        End Sub
    End Class
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Q")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInstanceMembersFromBaseOuterType6() As Task
            Dim text =
<code><![CDATA[
Class Base(Of T)
    Public X As Integer
End Class
Class Derived
    Inherits C(Of Integer)
    Class Nested
        Sub Test()
            $$
        End Sub
    End Class
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "X")
        End Function

        <WorkItem(4900, "https://github.com/dotnet/roslyn/issues/4090")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInstanceMembersWhenDottingIntoType() As Task
            Dim text =
<code><![CDATA[
Class Instance
    Public Shared x as Integer
    Public y as Integer
End Class

Class Program
    Sub Goo()
        Instance.$$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "y")
            Await VerifyItemExistsAsync(text, "x")
        End Function

        <WorkItem(4900, "https://github.com/dotnet/roslyn/issues/4090")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoSharedMemberWhenDottingIntoInstance() As Task
            Dim text =
<code><![CDATA[
Class Instance
    Public Shared x as Integer
    Public y as Integer
End Class

Class Program
    Sub Goo()
        Dim x = new Instance()
        x.$$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "x")
            Await VerifyItemExistsAsync(text, "y")
        End Function

        <WorkItem(4136, "https://github.com/dotnet/roslyn/issues/4136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoValue__WhenDottingIntoEnum() As Task
            Dim text =
<code><![CDATA[
Enum E
    A
End Enum

Class Program
    Sub Goo()
        E.$$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemExistsAsync(text, "A")
            Await VerifyItemIsAbsentAsync(text, "value__")
        End Function

        <WorkItem(4136, "https://github.com/dotnet/roslyn/issues/4136")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoValue__WhenDottingIntoLocalOfEnumType() As Task
            Dim text =
<code><![CDATA[
Enum E
    A
End Enum

Class Program
    Sub Goo()
        Dim x = E.A
        x.$$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "value__")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SharedProjectFieldAndPropertiesTreatedAsIdentical() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1" PreprocessorSymbols="ONE=True">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
#if ONE Then
    Public  x As Integer
#endif
#if TWO Then
    Public Property x as Integer
#endif
    Sub goo()
        x$$
    End Sub
End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj2" PreprocessorSymbols="TWO=True">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Dim expectedDescription = $"({ FeaturesResources.field }) C.x As Integer"
            Await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSharedProjectFieldAndPropertiesTreatedAsIdentical2() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1" PreprocessorSymbols="ONE=True">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
#if TWO Then
    Public  x As Integer
#endif
#if ONE Then
    Public Property x as Integer
#endif
    Sub goo()
        x$$
    End Sub
End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj2" PreprocessorSymbols="TWO=True">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Dim expectedDescription = $"Property C.x As Integer"
            Await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription)
        End Function

        <WorkItem(4405, "https://github.com/dotnet/roslyn/issues/4405")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function VerifyDelegateEscapedWhenCommitted() As Task
            Dim text =
<code><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x As {0}
    End Sub
End Module

]]></code>.Value
            Await VerifyProviderCommitAsync(markupBeforeCommit:=String.Format(text, "$$"),
                                 itemToCommit:="Delegate",
                                 expectedCodeAfterCommit:=String.Format(text, "[Delegate]"),
                                 commitChar:=Nothing,
                                 textTypedSoFar:="")
        End Function


        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncExcludedInExpressionContext1() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        args.Select($$)
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncExcludedInExpressionContext2() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncExcludedInStatementContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncIncludedInGetType() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        GetType($$)
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncIncludedInTypeOf() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim s = TypeOf args Is $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncIncludedInReturnTypeContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Function x() as $$
    End Function
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemFuncIncludedInFieldTypeContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Dim x as $$
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Func", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemDelegateInStatementContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Delegate")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionExcludedInExpressionContext1() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        args.Select($$)
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionExcludedInExpressionContext2() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionExcludedInStatementContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionIncludedInGetType() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        GetType($$)
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionIncludedInTypeOf() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim s = TypeOf args Is $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionIncludedInReturnTypeContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Function x() as $$
    End Function
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemActionIncludedInFieldTypeContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Dim x as $$
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Action", displayTextSuffix:="(Of " & s_unicodeEllipsis & ")")
        End Function

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSystemDelegateInExpressionContext() As Task
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim x = $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Delegate")
        End Function

        <WorkItem(4750, "https://github.com/dotnet/roslyn/issues/4750")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConditionalAccessInWith1() As Task
            Dim text =
<code><![CDATA[
Module Module1
    Sub Main()
        Dim s As String

        With s
1:         Console.WriteLine(If(?.$$, -1))
            Console.WriteLine()
        End With
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Length")
        End Function

        <WorkItem(4750, "https://github.com/dotnet/roslyn/issues/4750")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConditionalAccessInWith2() As Task
            Dim text =
<code><![CDATA[
Module Module1
    Sub Main()
        Dim s As String

        With s
1:         Console.WriteLine(If(?.Length, -1))
           ?.$$
            Console.WriteLine()
        End With
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Length")
        End Function

        <WorkItem(3290, "https://github.com/dotnet/roslyn/issues/3290")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDotAfterArray() As Task
            Dim text =
<code><![CDATA[
Module Module1
    Sub Main()
        Dim x = {1}.$$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "Length")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LocalInForLoop() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Integer
        For $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "x")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExcludeConstLocalInForLoop() As Task
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim const x As Integer
        For $$
    End Sub
End Module
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "x")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExcludeConstFieldInForLoop() As Task
            Dim text =
<code><![CDATA[
Class Program
    Const x As Integer = 0
    Sub Main(args As String())
        For $$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "x")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExcludeReadOnlyFieldInForLoop() As Task
            Dim text =
<code><![CDATA[
Class Program
    ReadOnly x As Integer
    Sub Main(args As String())
        For $$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemIsAbsentAsync(text, "x")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function FieldInForLoop() As Task
            Dim text =
<code><![CDATA[
Class Program
    Dim x As Integer
    Sub Main(args As String())
        For $$
    End Sub
End Class
]]></code>.Value
            Await VerifyItemExistsAsync(text, "x")
        End Function

        <WorkItem(153633, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/153633")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumMembers() As Task
            Dim text =
<code><![CDATA[
Module Module1
    Sub Main()
        Do Until (System.Console.ReadKey.Key = System.ConsoleKey.$$
        Loop
    End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "A")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TupleElements() As Task
            Dim text =
<code><![CDATA[
Module Module1
    Sub Main()
        Dim t = (Alice:=1, Item2:=2, ITEM3:=3, 4, 5, 6, 7, 8, Bob:=9)
        t.$$
    End Sub
End Module
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure

    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)
        Public Dim Rest As TRest

        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, rest As TRest)
        End Sub

        Public Overrides Function ToString() As String
            Return ""
        End Function

        Public Overrides Function GetHashCode As Integer
            Return 0
        End Function

        Public Overrides Function CompareTo(value As Object) As Integer
            Return 0
        End Function

        Public Overrides Function GetType As Type
            Return Nothing
        End Function
    End Structure
End Namespace
]]></code>.Value

            Await VerifyItemExistsAsync(text, "Alice")
            Await VerifyItemExistsAsync(text, "Bob")
            Await VerifyItemExistsAsync(text, "CompareTo")
            Await VerifyItemExistsAsync(text, "Equals")
            Await VerifyItemExistsAsync(text, "GetHashCode")
            Await VerifyItemExistsAsync(text, "GetType")
            For index = 2 To 8
                Await VerifyItemExistsAsync(text, "Item" + index.ToString())
            Next
            Await VerifyItemExistsAsync(text, "ToString")

            Await VerifyItemIsAbsentAsync(text, "Item1")
            Await VerifyItemIsAbsentAsync(text, "Item9")
            Await VerifyItemIsAbsentAsync(text, "Rest")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function WinformsInstanceMembers() As Task
            ' Setting the the preprocessor symbol _MyType=WindowsForms will cause the
            ' compiler to automatically generate the My Template. See
            ' GroupClassTests.vb for the compiler layer equivalent of these tests.
            Dim input =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="_MyType=WindowsForms">

                        <Document name="Form.vb">
                            <![CDATA[
Namespace Global.System.Windows.Forms
    Public Class Form
        Implements IDisposable

        Public Sub InstanceMethod()
        End Sub

        Public Shared Sub SharedMethod()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
    ]]></Document>
                        <Document name="types.vb"><![CDATA[
Imports System

Namespace Global.WindowsApplication1
    Public Class Form2
        Inherits System.Windows.Forms.Form
    End Class
End Namespace

Namespace Global.WindowsApplication1
    Public Class Form1
        Inherits System.Windows.Forms.Form

 Private Sub Goo()
        Form2.$$
    End Sub
    End Class
End Namespace
    ]]></Document>

                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(input)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.DocumentWithCursor.Id)
                Dim position = workspace.DocumentWithCursor.CursorPosition.Value
                Await CheckResultsAsync(document, position, "InstanceMethod", expectedDescriptionOrNull:=Nothing, usePreviousCharAsTrigger:=False, checkForAbsence:=False,
                                        glyph:=Nothing, matchPriority:=Nothing, hasSuggestionModeItem:=Nothing, displayTextSuffix:=Nothing, inlineDescription:=Nothing,
                                        matchingFilters:=Nothing)
                Await CheckResultsAsync(document, position, "SharedMethod", expectedDescriptionOrNull:=Nothing, usePreviousCharAsTrigger:=False, checkForAbsence:=False,
                                        glyph:=Nothing, matchPriority:=Nothing, hasSuggestionModeItem:=Nothing, displayTextSuffix:=Nothing, inlineDescription:=Nothing,
                                        matchingFilters:=Nothing)
            End Using

        End Function

        <WorkItem(22002, "https://github.com/dotnet/roslyn/issues/22002")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotCrashInTupleAlias() As Task
            Dim text =
<code><![CDATA[
Imports Boom = System.Collections.Generic.List(Of ($$) '<---put caret between brackets.

Public Module Module1
  Public Sub Main()
  End Sub
End Module
]]></code>.Value
            Await VerifyItemExistsAsync(text, "System")
        End Function

        Private Shared Function CreateThenIncludeTestCode(lambdaExpressionString As String, methodDeclarationString As String) As String
            Dim template = "
Imports System                       
Imports System.Collections.Generic
Imports System.Linq
Imports System.Linq.Expressions

Namespace ThenIncludeIntellisenseBug

    Class Program
        Shared Sub Main(args As String())
            Dim registrations = New List(Of Registration)().AsQueryable()
            Dim reg = registrations.Include(Function(r) r.Activities).ThenInclude([1])
        End Sub
    End Class

    Friend Class Registration
        Public Property Activities As ICollection(Of Activity)
    End Class

    Public Class Activity
        Public Property Task As Task
    End Class

    Public Class Task
        Public Property Name As String
    End Class

    Public Interface IIncludableQueryable(Of Out TEntity, Out TProperty)
        Inherits IQueryable(Of TEntity)
    End Interface

    Public Module EntityFrameworkQuerybleExtensions
        <System.Runtime.CompilerServices.Extension>
        Public Function Include(Of TEntity, TProperty)(
                source As IQueryable(Of TEntity), 
                navigationPropertyPath As Expression(Of Func(Of TEntity, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

[2]

    End Module
End Namespace"

            Return template.Replace("[1]", lambdaExpressionString).Replace("[2]", methodDeclarationString)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenInclude() As Task
            Dim source = CreateThenIncludeTestCode("Function(b) b.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)), 
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function
")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeNoExpression() As Task
            Dim source = CreateThenIncludeTestCode("Function(b) b.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)),
                navigationPropertyPath As Func(Of TPreviousProperty, TProperty)) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                navigationPropertyPath As Func(Of TPreviousProperty, TProperty)) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeSecondArgument() As Task
            Dim source = CreateThenIncludeTestCode("0, Function(b) b.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function
")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeSecondArgumentAndMultiArgumentLambda() As Task
            Dim source = CreateThenIncludeTestCode("0, Function(a, b, c) c.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)), 
                a as Integer,                                                                             
                navigationPropertyPath As Expression(Of Func(Of string, string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of string, string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeSecondArgumentNoOverlap() As Task
            Dim source = CreateThenIncludeTestCode("Function(b) b.Task, Function(b) b.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)),
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty)),
                anotherNavigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemIsAbsentAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeSecondArgumentAndMultiArgumentLambdaWithNoLambdaOverlap() As Task
            Dim source = CreateThenIncludeTestCode("0, Function(a, b, c) c.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of string, string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function
")

            Await VerifyItemIsAbsentAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/35100"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeGenericAndNoGenericOverloads() As Task
            Dim source = CreateThenIncludeTestCode("Function(c) c.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(
                source As IIncludableQueryable(Of Registration, ICollection(Of Activity)),
                navigationPropertyPath As Func(Of Activity, Task)) As IIncludableQueryable(Of Registration, Task)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                navigationPropertyPath As Expression(Of Func(Of TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function
")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ThenIncludeNoGenericOverloads() As Task
            Dim source = CreateThenIncludeTestCode("Function(c) c.$$",
"       <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(
                source As IIncludableQueryable(Of Registration, ICollection(Of Activity)),
                navigationPropertyPath As Func(Of Activity, Task)) As IIncludableQueryable(Of Registration, Task)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(
                source As IIncludableQueryable(Of Registration, ICollection(Of Activity)),
                navigationPropertyPath As Expression(Of Func(Of ICollection(Of Activity), Activity))) As IIncludableQueryable(Of Registration, Activity)
            Return Nothing
        End Function
")

            Await VerifyItemExistsAsync(source, "Task")
            Await VerifyItemExistsAsync(source, "FirstOrDefault")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaWithOverloads() As Task
            Dim source =
<code><![CDATA[
Imports System.Linq.Expressions
Imports System.Collections
Imports System
Imports System.Collections.Generic

Namespace VBTest
    Public Class SomeItem
        Public A As String
        Public B As Integer
    End Class

    Class SomeCollection(Of T)
        Inherits List(Of T)
        Public Overridable Function Include(path As String) As SomeCollection(Of T)
            Return Nothing
        End Function
    End Class

    Module Extensions
        <System.Runtime.CompilerServices.Extension>
        Public Function Include(Of T, TProperty)(ByVal source As IList(Of T), path As Expression(Of Func(Of T, TProperty))) As IList(Of T)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
       Public Function Include(ByVal source As IList, path As String) As IList
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function Include(Of T)(ByVal source As IList(Of T), path As String) As IList(Of T)
            Return Nothing
        End Function
    End Module

    Class Program
        Sub M(c As SomeCollection(Of SomeItem))
            Dim a = From m In c.Include(Function(t) t.$$)
        End Sub
    End Class
End Namespace
]]></code>.Value

            Await VerifyItemExistsAsync(source, "Substring")
            Await VerifyItemExistsAsync(source, "A")
            Await VerifyItemExistsAsync(source, "B")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInsideMethodsWithNonFunctionsAsArguments() As Task
            Dim source =
<code><![CDATA[
Imports System
Class C
    Sub M()
        Goo(Sub(b) b.$$
    End Sub

    Sub Goo(configure As Action(Of Builder))
        Dim builder = New Builder()
        configure(builder)
    End Sub
End Class

Class Builder
    Public Property Something As Integer
End Class
]]></code>.Value

            Await VerifyItemExistsAsync(source, "Something")
            Await VerifyItemIsAbsentAsync(source, "BeginInvoke")
            Await VerifyItemIsAbsentAsync(source, "Clone")
            Await VerifyItemIsAbsentAsync(source, "Method")
            Await VerifyItemIsAbsentAsync(source, "Target")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInsideMethodsWithDelegatesAsArguments() As Task
            Dim source =
<code><![CDATA[
Imports System

Module Module2
    Class Program
        Public Delegate Sub Delegate1(u As Uri)
        Public Delegate Sub Delegate2(g As Guid)

        Public Sub M(d As Delegate1)
        End Sub

        Public Sub M(d As Delegate2)
        End Sub

        Public Sub Test()
            M(Sub(d) d.$$)
        End Sub
    End Class
End Module
]]></code>.Value

            ' Guid
            Await VerifyItemExistsAsync(source, "ToByteArray")

            ' Uri
            Await VerifyItemExistsAsync(source, "AbsoluteUri")
            Await VerifyItemExistsAsync(source, "Fragment")
            Await VerifyItemExistsAsync(source, "Query")

            ' Should Not appear for Delegate
            Await VerifyItemIsAbsentAsync(source, "BeginInvoke")
            Await VerifyItemIsAbsentAsync(source, "Clone")
            Await VerifyItemIsAbsentAsync(source, "Method")
            Await VerifyItemIsAbsentAsync(source, "Target")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInsideMethodsWithDelegatesAndReversingArguments() As Task
            Dim source =
<code><![CDATA[
Imports System

Module Module2
    Class Program
        Public Delegate Sub Delegate1(Of T1, T2)(t2 As T2, t1 As T1)
        Public Delegate Sub Delegate2(Of T1, T2)(t2 As T2, a As Integer, t1 As T1)

        Public Sub M(d As Delegate1(Of Uri, Guid))
        End Sub

        Public Sub M(d As Delegate2(Of Uri, Guid))
        End Sub

        Public Sub Test()
            M(Sub(d) d.$$)
        End Sub
    End Class
End Module
]]></code>.Value

            ' Guid
            Await VerifyItemExistsAsync(source, "ToByteArray")

            ' Should Not appear for Delegate
            Await VerifyItemIsAbsentAsync(source, "AbsoluteUri")
            Await VerifyItemIsAbsentAsync(source, "Fragment")
            Await VerifyItemIsAbsentAsync(source, "Query")

            ' Should Not appear for Delegate
            Await VerifyItemIsAbsentAsync(source, "BeginInvoke")
            Await VerifyItemIsAbsentAsync(source, "Clone")
            Await VerifyItemIsAbsentAsync(source, "Method")
            Await VerifyItemIsAbsentAsync(source, "Target")
        End Function

        <WorkItem(36029, "https://github.com/dotnet/roslyn/issues/36029")>
        <Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)>
        Public Async Function CompletionInsideMethodWithParamsBeforeParams() As Task
            Dim source =
<code><![CDATA[
Imports System
Class C
    Sub M()
        Goo(Sub(b) b.$$)
    End Sub

    Sub Goo(action As Action(Of Builder), ParamArray otherActions() As Action(Of AnotherBuilder))
    End Sub
End Class

Class Builder
    Public Something As Integer
End Class

Class AnotherBuilder
    Public AnotherSomething As Integer
End Class
]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "AnotherSomething")
            Await VerifyItemIsAbsentAsync(source, "FirstOrDefault")
            Await VerifyItemExistsAsync(source, "Something")
        End Function

        <WorkItem(36029, "https://github.com/dotnet/roslyn/issues/36029")>
        <Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)>
        Public Async Function CompletionInsideMethodWithParamsInParams() As Task
            Dim source =
<code><![CDATA[
Imports System
Class C
    Sub M()
        Goo(Nothing, Nothing, Sub(b) b.$$)
    End Sub

    Sub Goo(action As Action(Of Builder), ParamArray otherActions() As Action(Of AnotherBuilder))
    End Sub
End Class

Class Builder
    Public Something As Integer
End Class

Class AnotherBuilder
    Public AnotherSomething As Integer
End Class
]]></code>.Value

            Await VerifyItemIsAbsentAsync(source, "Something")
            Await VerifyItemIsAbsentAsync(source, "FirstOrDefault")
            Await VerifyItemExistsAsync(source, "AnotherSomething")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)>
        Public Async Function TestTargetTypeFilterWithExperimentEnabled() As Task
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, True)
            Dim markup =
"Class C
    Dim intField As Integer
    Sub M(x as Integer)
        M($$)
    End Sub
End Class"
            Await VerifyItemExistsAsync(
                markup, "intField",
                matchingFilters:=New List(Of CompletionFilter) From {FilterSet.FieldFilter, FilterSet.TargetTypedFilter})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)>
        Public Async Function TestNoTargetTypeFilterWithExperimentDisabled() As Task
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, False)
            Dim markup =
"Class C
    Dim intField As Integer
    Sub M(x as Integer)
        M($$)
    End Sub
End Class"
            Await VerifyItemExistsAsync(
                markup, "intField",
                matchingFilters:=New List(Of CompletionFilter) From {FilterSet.FieldFilter})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)>
        Public Async Function TestTargetTypeFilter_NotOnObjectMembers() As Task
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, True)
            Dim markup =
"Class C
    Dim intField As Integer
    Sub M(x as Integer)
        M($$)
    End Sub
End Class"
            Await VerifyItemExistsAsync(
                markup, "GetHashCode",
                matchingFilters:=New List(Of CompletionFilter) From {FilterSet.MethodFilter})
        End Function
    End Class
End Namespace
