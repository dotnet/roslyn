' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    ' TODO: consider merging these tests with the keyword recommending tests in some way
    Public Class TypeContextTests
        Inherits AbstractContextTests
        Protected Overrides Function CheckResultAsync(validLocation As Boolean, position As Integer, syntaxTree As SyntaxTree) As Task
            Dim token = syntaxTree.GetTargetToken(position, CancellationToken.None)
            Assert.Equal(validLocation, syntaxTree.IsTypeContext(position, token, CancellationToken.None))
            Return Task.CompletedTask
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEmptyFile() As Task
            Await VerifyFalseAsync("$$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint1() As Task
            Await VerifyTrueAsync("Class A(Of T As $$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint2() As Task
            Await VerifyTrueAsync("Class A(Of T As { II, $$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint3() As Task
            Await VerifyTrueAsync("Class A(Of T As $$)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeConstraint4() As Task
            Await VerifyTrueAsync("Class A(Of T As { II, $$})")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements1() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method() As A Implements $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements2() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method() As A Implements $$.Method"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplements3() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method() As A Implements I.Method, $$.Method"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs1() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method() As $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs2() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method() As $$ Implements II.Method"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAs3() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "  Function Method(ByVal args As $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAsNew() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d As New $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGetType1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = GetType($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeOfIs() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = TypeOf d Is $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreation() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = New $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayCreation() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d() = New $$() {"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = CType(obj, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = TryCast(obj, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast3() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = DirectCast(obj, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayType() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d() as $$("))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNullableType() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d as $$?"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBaseDeclarations1() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "    Inherits $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBaseDeclarations2() As Task
            Await VerifyTrueAsync(CreateContent("Class A",
                                     "    Implements $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList1() As Task
            Await VerifyFalseAsync(CreateContent("Class A(Of $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList2() As Task
            Await VerifyFalseAsync(CreateContent("Class A(Of T, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList3() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d as D(Of $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentList4() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d as D(Of A, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInferredFieldInitializer() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim anonymousCust2 = New With {Key $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedFieldInitializer() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim anonymousCust = New With {.Name = $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInitializer() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturnStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("Return $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIfStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("If $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIfStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("If Var1 Then",
                                                     "Else If $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCatchFilterClause() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Try",
                                                     "Catch ex As Exception when $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestErrorStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("Error $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Select $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Select Case $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleCaseClause1() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleCaseClause2() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case 1, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeCaseClause1() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case $$ To")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeCaseClause2() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case 1 To $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRelationalCaseClause1() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case Is > $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRelationalCaseClause2() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("Select T",
                                                     "Case >= $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSyncLockStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("SyncLock $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileOrUntilClause1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Do While $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileOrUntilClause2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Do Until $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhileStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("While $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("For i = $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("For i = 1 To $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStepClause() As Task
            Await VerifyTrueAsync(AddInsideMethod("For i = 1 To 10 Step $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForEachStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("For Each I in $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUsingStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("Using $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestThrowStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("Throw $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAssignmentStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$ = a"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAssignmentStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("a = $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCallStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Call $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCallStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$(1)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("AddHandler $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("AddHandler T.Event, AddressOf $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement3() As Task
            Await VerifyTrueAsync(AddInsideMethod("RemoveHandler $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddRemoveHandlerStatement4() As Task
            Await VerifyTrueAsync(AddInsideMethod("RemoveHandler T.Event, AddressOf $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithStatement() As Task
            Await VerifyTrueAsync(AddInsideMethod("With $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedExpression() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = ($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeOfIs2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = TypeOf $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMemberAccessExpression1() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$.Name"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMemberAccessExpression2() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$!Name"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvocationExpression() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$(1)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeArgumentExpression() As Task
            Await VerifyTrueAsync(AddInsideMethod("$$(Of Integer)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast4() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = CType($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast5() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = TryCast($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCast6() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = DirectCast($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBuiltInCase() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = CInt($$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryExpression1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = $$ + d"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryExpression2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = d + $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUnaryExpression() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = +$$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryConditionExpression1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = If($$,"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBinaryConditionExpression2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = If(a, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = If($$, a, b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = If(a, $$, c"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTernaryConditionExpression3() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = If(a, b, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSingleArgument() As Task
            Await VerifyTrueAsync(AddInsideMethod("D($$)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedArgument() As Task
            Await VerifyTrueAsync(AddInsideMethod("D(Name := $$)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeArgument1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a($$ To 10)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRangeArgument2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a(0 To $$)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionRangeVariable() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From var in $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExpressionRangeVariable() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From var In collection Let b = $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFunctionAggregation() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From c In col Aggregate o In c.o Into an = Any($$)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWhereQueryOperator() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From c In col Where $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionWhileQueryOperator1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim customerList = From c In cust Order By c.C Skip While $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionWhileQueryOperator2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim customerList = From c In cust Order By c.C Take While  $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionQueryOperator1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From c In cust Skip $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartitionQueryOperator2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From c In cust Take $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestJoinCondition1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim p1 = From p In P Join d In Desc On $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestJoinCondition2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim p1 = From p In P Join d In Desc On p.P Equals $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOrdering() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim a = From b In books Order By $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestXmlEmbeddedExpression() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim book As XElement = <book isbn=<%= $$ %>></book>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNextStatement1() As Task
            Await VerifyFalseAsync(AddInsideMethod(CreateContent("For i = 1 To 10",
                                                      "Next $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNextStatement2() As Task
            Await VerifyFalseAsync(AddInsideMethod(CreateContent("For i = 1 To 10",
                                                      "Next i, $$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEraseStatement1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Erase $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEraseStatement2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Erase i, $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionInitializer1() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = new List(Of Integer) from { $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCollectionInitializer2() As Task
            Await VerifyTrueAsync(AddInsideMethod("Dim d = { $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAliasImportsClause1() As Task
            Await VerifyTrueAsync("Imports T = $$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAliasImportsClause2() As Task
            Await VerifyTrueAsync("Imports $$ = S")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersImportsClause1() As Task
            Await VerifyTrueAsync("Imports $$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersImportsClause2() As Task
            Await VerifyTrueAsync("Imports System, $$")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes1() As Task
            Await VerifyTrueAsync(CreateContent("<$$>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes2() As Task
            Await VerifyTrueAsync(CreateContent("<$$>",
                                     "Class Cl"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributes3() As Task
            Await VerifyTrueAsync(CreateContent("Class Cl",
                                     "    <$$>",
                                     "    Function Method()"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestStringLiteral() As Task
            Await VerifyFalseAsync(AddInsideMethod("Dim d = ""$$"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestComment1() As Task
            Await VerifyFalseAsync(AddInsideMethod("' $$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestComment2() As Task
            Await VerifyTrueAsync(AddInsideMethod(CreateContent("'",
                                                      "$$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInactiveRegion() As Task
            Await VerifyFalseAsync(AddInsideMethod(CreateContent("#IF False Then",
                                                      "$$")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnumAs() As Task
            Await VerifyFalseAsync(CreateContent("Enum Goo As $$"))
        End Function

    End Class
End Namespace
