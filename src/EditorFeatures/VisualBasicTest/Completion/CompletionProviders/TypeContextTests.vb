' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    ' TODO: consider merging these tests with the keyword recommending tests in some way
    Public Class TypeContextTests
        Inherits AbstractContextTests
        Protected Overrides Sub CheckResult(validLocation As Boolean, position As Integer, syntaxTree As SyntaxTree)
            Dim token = syntaxTree.GetTargetToken(position, CancellationToken.None)
            Assert.Equal(validLocation, syntaxTree.IsTypeContext(position, token, CancellationToken.None))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EmptyFile()
            VerifyFalse("$$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint1()
            VerifyTrue("Class A(Of T As $$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint2()
            VerifyTrue("Class A(Of T As { II, $$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint3()
            VerifyTrue("Class A(Of T As $$)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint4()
            VerifyTrue("Class A(Of T As { II, $$})")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements1()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method() As A Implements $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements2()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method() As A Implements $$.Method"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements3()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method() As A Implements I.Method, $$.Method"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As1()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method() As $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As2()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method() As $$ Implements II.Method"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As3()
            VerifyTrue(CreateContent("Class A",
                                     "  Function Method(ByVal args As $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AsNew()
            VerifyTrue(AddInsideMethod("Dim d As New $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub GetType1()
            VerifyTrue(AddInsideMethod("Dim d = GetType($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeOfIs()
            VerifyTrue(AddInsideMethod("Dim d = TypeOf d Is $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ObjectCreation()
            VerifyTrue(AddInsideMethod("Dim d = New $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayCreation()
            VerifyTrue(AddInsideMethod("Dim d() = New $$() {"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast1()
            VerifyTrue(AddInsideMethod("Dim d = CType(obj, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast2()
            VerifyTrue(AddInsideMethod("Dim d = TryCast(obj, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast3()
            VerifyTrue(AddInsideMethod("Dim d = DirectCast(obj, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayType()
            VerifyTrue(AddInsideMethod("Dim d() as $$("))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NullableType()
            VerifyTrue(AddInsideMethod("Dim d as $$?"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BaseDeclarations1()
            VerifyTrue(CreateContent("Class A",
                                     "    Inherits $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BaseDeclarations2()
            VerifyTrue(CreateContent("Class A",
                                     "    Implements $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList1()
            VerifyFalse(CreateContent("Class A(Of $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList2()
            VerifyFalse(CreateContent("Class A(Of T, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList3()
            VerifyTrue(AddInsideMethod("Dim d as D(Of $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList4()
            VerifyTrue(AddInsideMethod("Dim d as D(Of A, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InferredFieldInitializer()
            VerifyTrue(AddInsideMethod("Dim anonymousCust2 = New With {Key $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NamedFieldInitializer()
            VerifyTrue(AddInsideMethod("Dim anonymousCust = New With {.Name = $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Initializer()
            VerifyTrue(AddInsideMethod("Dim a = $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReturnStatement()
            VerifyTrue(AddInsideMethod("Return $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IfStatement1()
            VerifyTrue(AddInsideMethod("If $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IfStatement2()
            VerifyTrue(AddInsideMethod(CreateContent("If Var1 Then",
                                                     "Else If $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CatchFilterClause()
            VerifyTrue(AddInsideMethod(CreateContent("Try",
                                                     "Catch ex As Exception when $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ErrorStatement()
            VerifyTrue(AddInsideMethod("Error $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectStatement1()
            VerifyTrue(AddInsideMethod("Select $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectStatement2()
            VerifyTrue(AddInsideMethod("Select Case $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SimpleCaseClause1()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SimpleCaseClause2()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case 1, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeCaseClause1()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case $$ To")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeCaseClause2()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case 1 To $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RelationalCaseClause1()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case Is > $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RelationalCaseClause2()
            VerifyTrue(AddInsideMethod(CreateContent("Select T",
                                                     "Case >= $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SyncLockStatement()
            VerifyTrue(AddInsideMethod("SyncLock $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileOrUntilClause1()
            VerifyTrue(AddInsideMethod("Do While $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileOrUntilClause2()
            VerifyTrue(AddInsideMethod("Do Until $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileStatement()
            VerifyTrue(AddInsideMethod("While $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement1()
            VerifyTrue(AddInsideMethod("For i = $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement2()
            VerifyTrue(AddInsideMethod("For i = 1 To $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStepClause()
            VerifyTrue(AddInsideMethod("For i = 1 To 10 Step $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForEachStatement()
            VerifyTrue(AddInsideMethod("For Each I in $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UsingStatement()
            VerifyTrue(AddInsideMethod("Using $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ThrowStatement()
            VerifyTrue(AddInsideMethod("Throw $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AssignmentStatement1()
            VerifyTrue(AddInsideMethod("$$ = a"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AssignmentStatement2()
            VerifyTrue(AddInsideMethod("a = $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CallStatement1()
            VerifyTrue(AddInsideMethod("Call $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CallStatement2()
            VerifyTrue(AddInsideMethod("$$(1)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement1()
            VerifyTrue(AddInsideMethod("AddHandler $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement2()
            VerifyTrue(AddInsideMethod("AddHandler T.Event, AddressOf $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement3()
            VerifyTrue(AddInsideMethod("RemoveHandler $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement4()
            VerifyTrue(AddInsideMethod("RemoveHandler T.Event, AddressOf $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WithStatement()
            VerifyTrue(AddInsideMethod("With $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParenthesizedExpression()
            VerifyTrue(AddInsideMethod("Dim a = ($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeOfIs2()
            VerifyTrue(AddInsideMethod("Dim a = TypeOf $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MemberAccessExpression1()
            VerifyTrue(AddInsideMethod("$$.Name"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MemberAccessExpression2()
            VerifyTrue(AddInsideMethod("$$!Name"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvocationExpression()
            VerifyTrue(AddInsideMethod("$$(1)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentExpression()
            VerifyTrue(AddInsideMethod("$$(Of Integer)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast4()
            VerifyTrue(AddInsideMethod("Dim d = CType($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast5()
            VerifyTrue(AddInsideMethod("Dim d = TryCast($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast6()
            VerifyTrue(AddInsideMethod("Dim d = DirectCast($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BuiltInCase()
            VerifyTrue(AddInsideMethod("Dim d = CInt($$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryExpression1()
            VerifyTrue(AddInsideMethod("Dim d = $$ + d"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryExpression2()
            VerifyTrue(AddInsideMethod("Dim d = d + $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UnaryExpression()
            VerifyTrue(AddInsideMethod("Dim d = +$$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryConditionExpression1()
            VerifyTrue(AddInsideMethod("Dim d = If($$,"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryConditionExpression2()
            VerifyTrue(AddInsideMethod("Dim d = If(a, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression1()
            VerifyTrue(AddInsideMethod("Dim d = If($$, a, b"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression2()
            VerifyTrue(AddInsideMethod("Dim d = If(a, $$, c"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression3()
            VerifyTrue(AddInsideMethod("Dim d = If(a, b, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SingleArgument()
            VerifyTrue(AddInsideMethod("D($$)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NamedArgument()
            VerifyTrue(AddInsideMethod("D(Name := $$)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeArgument1()
            VerifyTrue(AddInsideMethod("Dim a($$ To 10)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeArgument2()
            VerifyTrue(AddInsideMethod("Dim a(0 To $$)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionRangeVariable()
            VerifyTrue(AddInsideMethod("Dim a = From var in $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExpressionRangeVariable()
            VerifyTrue(AddInsideMethod("Dim a = From var In collection Let b = $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FunctionAggregation()
            VerifyTrue(AddInsideMethod("Dim a = From c In col Aggregate o In c.o Into an = Any($$)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhereQueryOperator()
            VerifyTrue(AddInsideMethod("Dim a = From c In col Where $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionWhileQueryOperator1()
            VerifyTrue(AddInsideMethod("Dim customerList = From c In cust Order By c.C Skip While $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionWhileQueryOperator2()
            VerifyTrue(AddInsideMethod("Dim customerList = From c In cust Order By c.C Take While  $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionQueryOperator1()
            VerifyTrue(AddInsideMethod("Dim a = From c In cust Skip $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionQueryOperator2()
            VerifyTrue(AddInsideMethod("Dim a = From c In cust Take $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub JoinCondition1()
            VerifyTrue(AddInsideMethod("Dim p1 = From p In P Join d In Desc On $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub JoinCondition2()
            VerifyTrue(AddInsideMethod("Dim p1 = From p In P Join d In Desc On p.P Equals $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Ordering()
            VerifyTrue(AddInsideMethod("Dim a = From b In books Order By $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub XmlEmbeddedExpression()
            VerifyTrue(AddInsideMethod("Dim book As XElement = <book isbn=<%= $$ %>></book>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NextStatement1()
            VerifyFalse(AddInsideMethod(CreateContent("For i = 1 To 10",
                                                      "Next $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NextStatement2()
            VerifyFalse(AddInsideMethod(CreateContent("For i = 1 To 10",
                                                      "Next i, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EraseStatement1()
            VerifyTrue(AddInsideMethod("Erase $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EraseStatement2()
            VerifyTrue(AddInsideMethod("Erase i, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionInitializer1()
            VerifyTrue(AddInsideMethod("Dim d = new List(Of Integer) from { $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionInitializer2()
            VerifyTrue(AddInsideMethod("Dim d = { $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasImportsClause1()
            VerifyTrue("Imports T = $$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasImportsClause2()
            VerifyTrue("Imports $$ = S")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersImportsClause1()
            VerifyTrue("Imports $$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersImportsClause2()
            VerifyTrue("Imports System, $$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes1()
            VerifyTrue(CreateContent("<$$>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes2()
            VerifyTrue(CreateContent("<$$>",
                                     "Class Cl"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes3()
            VerifyTrue(CreateContent("Class Cl",
                                     "    <$$>",
                                     "    Function Method()"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub StringLiteral()
            VerifyFalse(AddInsideMethod("Dim d = ""$$"""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Comment1()
            VerifyFalse(AddInsideMethod("' $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Comment2()
            VerifyTrue(AddInsideMethod(CreateContent("'",
                                                      "$$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InactiveRegion()
            VerifyFalse(AddInsideMethod(CreateContent("#IF False Then",
                                                      "$$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumAs()
            VerifyFalse(CreateContent("Enum Foo As $$"))
        End Sub

    End Class
End Namespace
