' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class SymbolCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Private Const s_unicodeEllipsis = ChrW(&H2026)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New SymbolCompletionProvider()
        End Function

#Region "StandaloneNamespaceAndTypeSourceTests"

        Private Sub VerifyNSATIsAbsent(markup As String)
            ' Verify namespace 'System' is absent
            VerifyItemIsAbsent(markup, "System")

            ' Verify type 'String' is absent
            VerifyItemIsAbsent(markup, "String")
        End Sub

        Private Sub VerifyNSATExists(markup As String)
            ' Verify namespace 'System' is absent
            VerifyItemExists(markup, "System")

            ' Verify type 'String' is absent
            VerifyItemExists(markup, "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EmptyFile()
            VerifyNSATIsAbsent("$$")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EmptyFileWithImports()
            VerifyNSATIsAbsent(AddImportsStatement("Imports System", "$$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint1()
            VerifyNSATExists(AddImportsStatement("Imports System", "Class A(Of T As $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint2()
            VerifyNSATExists(AddImportsStatement("Imports System", "Class A(Of T As { II, $$"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint3()
            VerifyNSATExists(AddImportsStatement("Imports System", "Class A(Of T As $$)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeConstraint4()
            VerifyNSATExists(AddImportsStatement("Imports System", "Class A(Of T As { II, $$})"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements1()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements $$.Method")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements3()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As A Implements I.Method, $$.Method")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As1()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method() As $$ Implements II.Method")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub As3()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class A",
                                  "  Function Method(ByVal args As $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AsNew()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d As New $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub GetType1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = GetType($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeOfIs()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TypeOf d Is $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ObjectCreation()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = New $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayCreation()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d() = New $$() {")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CType(obj, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TryCast(obj, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast3()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = DirectCast(obj, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayType()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d() as $$(")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NullableType()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d as $$?")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList1()
            VerifyNSATIsAbsent(AddImportsStatement("Imports System", CreateContent("Class A(Of $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList2()
            VerifyNSATIsAbsent(AddImportsStatement("Imports System", CreateContent("Class A(Of T, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList3()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d as D(Of $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentList4()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d as D(Of A, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InferredFieldInitializer()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim anonymousCust2 = New With {Key $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NamedFieldInitializer()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim anonymousCust = New With {.Name = $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Initializer()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReturnStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Return $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IfStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("If $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IfStatement2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("If Var1 Then",
                                      "Else If $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CatchFilterClause()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Try",
                                      "Catch ex As Exception when $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ErrorStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Error $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Select $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Select Case $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SimpleCaseClause1()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SimpleCaseClause2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case 1, $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeCaseClause1()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case $$ To"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeCaseClause2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case 1 To $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RelationalCaseClause1()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case Is > $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RelationalCaseClause2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("Select T",
                                      "Case >= $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SyncLockStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("SyncLock $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileOrUntilClause1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Do While $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileOrUntilClause2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Do Until $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhileStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("While $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("For i = $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("For i = 1 To $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStepClause()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("For i = 1 To 10 Step $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForEachStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("For Each I in $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UsingStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Using $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ThrowStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Throw $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AssignmentStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$ = a")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AssignmentStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("a = $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CallStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Call $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CallStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$(1)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("AddHandler $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("AddHandler T.Event, AddressOf $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement3()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("RemoveHandler $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AddRemoveHandlerStatement4()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("RemoveHandler T.Event, AddressOf $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WithStatement()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("With $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParenthesizedExpression()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = ($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeOfIs2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = TypeOf $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MemberAccessExpression1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$.Name")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MemberAccessExpression2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$!Name")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvocationExpression()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$(1)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypeArgumentExpression()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("$$(Of Integer)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast4()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CType($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast5()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = TryCast($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Cast6()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = DirectCast($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BuiltInCase()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = CInt($$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryExpression1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = $$ + d")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryExpression2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = d + $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UnaryExpression()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = +$$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryConditionExpression1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If($$,")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BinaryConditionExpression2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If($$, a, b")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, $$, c")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TernaryConditionExpression3()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = If(a, b, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SingleArgument()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("D($$)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NamedArgument()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("D(Name := $$)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeArgument1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a($$ To 10)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RangeArgument2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a(0 To $$)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionRangeVariable()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From var in $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExpressionRangeVariable()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From var In collection Let b = $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FunctionAggregation()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In col Aggregate o In c.o Into an = Any($$)")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WhereQueryOperator()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In col Where $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionWhileQueryOperator1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim customerList = From c In cust Order By c.C Skip While $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionWhileQueryOperator2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim customerList = From c In cust Order By c.C Take While $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionQueryOperator1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In cust Skip $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PartitionQueryOperator2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From c In cust Take $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub JoinCondition1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim p1 = From p In P Join d In Desc On $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub JoinCondition2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim p1 = From p In P Join d In Desc On p.P Equals $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Ordering()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim a = From b In books Order By $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub XmlEmbeddedExpression()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim book As XElement = <book isbn=<%= $$ %>></book>")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NextStatement1()
            VerifyNSATIsAbsent(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("For i = 1 To 10",
                                      "Next $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NextStatement2()
            VerifyNSATIsAbsent(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("For i = 1 To 10",
                                      "Next i, $$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EraseStatement1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Erase $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EraseStatement2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Erase i, $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionInitializer1()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = new List(Of Integer) from { $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CollectionInitializer2()
            VerifyNSATExists(AddImportsStatement("Imports System", AddInsideMethod("Dim d = { $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub StringLiteral()
            VerifyNSATIsAbsent(AddImportsStatement("Imports System", AddInsideMethod("Dim d = ""$$""")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Comment1()
            VerifyNSATIsAbsent(AddImportsStatement("Imports System", AddInsideMethod("' $$")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Comment2()
            VerifyNSATExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("'", "$$"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InactiveRegion1()
            VerifyNSATIsAbsent(
                AddImportsStatement("Imports System",
                    AddInsideMethod(
                        CreateContent("#IF False Then", " $$"))))
        End Sub

#Region "Tests that verify namespaces and types separately"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasImportsClause1()
            VerifyItemExists(AddImportsStatement("Imports System", "Imports T = $$"), "System")
            VerifyItemIsAbsent(AddImportsStatement("Imports System", "Imports T = $$"), "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasImportsClause2()
            VerifyItemExists("Imports $$ = S", "System")
            VerifyItemIsAbsent("Imports $$ = S", "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersImportsClause1()
            VerifyItemExists(AddImportsStatement("Imports System", "Imports $$"), "System")
            VerifyItemIsAbsent(AddImportsStatement("Imports System", "Imports $$"), "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersImportsClause2()
            VerifyItemExists(AddImportsStatement("Imports System", "Imports System, $$"), "System")
            VerifyItemIsAbsent(AddImportsStatement("Imports System", "Imports System, $$"), "String")
        End Sub

        <WorkItem(529191)>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes1()
            VerifyItemExists(AddImportsStatement("Imports System", CreateContent("<$$>")), "System")
            VerifyItemExists(AddImportsStatement("Imports System", CreateContent("<$$>")), "String")
        End Sub

        <WorkItem(529191)>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes2()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("<$$>",
                                  "Class Cl")), "System")
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("<$$>",
                                  "Class Cl")), "String")
        End Sub

        <WorkItem(529191)>
        <WpfFact(Skip:="529191"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Attributes3()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class Cl",
                                  "    <$$>",
                                  "    Function Method()")), "System")
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class Cl",
                                  "    <$$>",
                                  "    Function Method()")), "String")
        End Sub

#End Region

#End Region

#Region "SymbolCompletionProviderTests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IsCommitCharacterTest()
            Const code = "
Imports System
Class C
    Sub M()
        $$
    End Sub
End Class"

            VerifyCommonCommitCharacters(code, textTypedSoFar:="")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SendEnterThroughToEditorTest()
            Const code = "
Imports System
Class C
    Sub M()
        $$
    End Sub
End Class"

            VerifySendEnterThroughToEditor(code, "Int32", expected:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterDateLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call #1/1/2010#.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterStringLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call """".$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterTrueLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call True.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterFalseLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call False.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterNumericLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call 2.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterCharacterLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call ""c""c.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoMembersAfterNothingLiteral()
            VerifyItemIsAbsent(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call Nothing.$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedDateLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (#1/1/2010#).$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedStringLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call ("""").$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedTrueLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (True).$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedFalseLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (False).$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedNumericLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (2).$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersAfterParenthesizedCharacterLiteral()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (""c""c).$$")), "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoMembersAfterParenthesizedNothingLiteral()
            VerifyItemIsAbsent(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Call (Nothing).$$")), "Equals")
        End Sub

        <WorkItem(539243)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedClassesInImports()
            VerifyItemExists("Imports System.$$", "Console")
        End Sub

        <WorkItem(539332)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceTypesAvailableInImportsAlias()
            VerifyItemExists("Imports S = System.$$", "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceTypesAvailableInImports()
            VerifyItemExists("Imports System.$$", "String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVarInMethod()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    AddInsideMethod("Dim banana As Integer = 4" + vbCrLf + "$$")), "banana")
        End Sub

        <WorkItem(539300)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMembersAfterMe1()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539300)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMembersAfterMe2()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539300)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersAfterMe1()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539300)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersAfterMe2()
            VerifyItemExists(
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
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoEventSymbolAfterMe()
            VerifyItemIsAbsent(
<Text>
Class EventClass
    Public Event X()

    Sub Test()
        Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoEventSymbolAfterMyClass()
            VerifyItemIsAbsent(
<Text>
Class EventClass
    Public Shared Event X()

    Sub Test()
        MyClass.$$
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoEventSymbolAfterMyBase()
            VerifyItemIsAbsent(
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
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoEventSymbolAfterInstanceMember()
            VerifyItemIsAbsent(
<Text>
Class EventClass
    Public Shared Event X()

    Sub Test()
        Dim a As New EventClass()
        a.$$
    End Sub
End Class
</Text>.Value, "E")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventSymbolAfterMeInAddHandlerContext()
            VerifyItemExists(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        AddHandler Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventSymbolAfterInstanceMemberInAddHandlerContext()
            VerifyItemExists(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        AddHandler a.$$
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventSymbolAfterInstanceMemberInParenthesizedAddHandlerContext()
            VerifyItemExists(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        AddHandler (a.$$), a.XEvent
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventSymbolAfterMeInRemoveHandlerContext()
            VerifyItemExists(
<Text>
Class EventClass
    Public Event X()
    Sub Test()
        RemoveHandler Me.$$
    End Sub
End Class
</Text>.Value, "X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoImplicitlyDeclaredMembersFromEventDeclarationAfterMe()
            Dim source = <Text>
Class EventClass
    Public Event X()
    Sub Test()
        Me.$$
    End Sub
End Class
</Text>.Value

            VerifyItemIsAbsent(source, "XEventHandler")
            VerifyItemIsAbsent(source, "XEvent")
            VerifyItemIsAbsent(source, "add_X")
            VerifyItemIsAbsent(source, "remove_X")
        End Sub

        <WorkItem(530617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoImplicitlyDeclaredMembersFromEventDeclarationAfterInstance()
            Dim source = <Text>
Class EventClass
    Public Event X()
    Sub Test()
        Dim a As New EventClass()
        a.$$
    End Sub
End Class
</Text>.Value

            VerifyItemIsAbsent(source, "XEventHandler")
            VerifyItemIsAbsent(source, "XEvent")
            VerifyItemIsAbsent(source, "add_X")
            VerifyItemIsAbsent(source, "remove_X")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplicitlyDeclaredEventHandler()
            Dim source = <Text>
Class EventClass
    Public Event X()
    Dim a As $$
End Class
</Text>.Value

            VerifyItemExists(source, "XEventHandler")
        End Sub

        <WorkItem(529570)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplicitlyDeclaredFieldFromWithEvents()
            Dim source = <Text>
Public Class C1
    Protected WithEvents w As C1 = Me
    Sub Foo()
        Me.$$
    End Sub
End Class
</Text>.Value

            VerifyItemIsAbsent(source, "_w")
        End Sub

        <WorkItem(529147)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplicitlyDeclaredFieldFromAutoProperty()
            Dim source = <Text>
Class C1
    Property X As C1
    Sub test()
        Me.$$
    End Sub
End Class
</Text>.Value

            VerifyItemIsAbsent(source, "_X")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NothingBeforeDot()
            Dim code = <Text>
Module Module1
    Sub Main()
        .$$
    End Sub
End Module
                       </Text>.Value

            VerifyItemIsAbsent(code, "Main")
        End Sub

        <WorkItem(539276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMembersAfterWithMe1()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMembersAfterWithMe2()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersAfterWithMe1()
            VerifyItemExists(
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
        End Sub

        <WorkItem(539276)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersAfterWithMe2()
            VerifyItemExists(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NestedWithBlocks()
            VerifyItemExists(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoSharedMembers()
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
            VerifyItemIsAbsent(test, "MaxValue")
            VerifyItemIsAbsent(test, "ReferenceEquals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LabelAfterGoto1()
            Dim test = <Text>
Class C
    Sub M()
        Foo: Dim i As Integer
        Goto $$"
</Text>.Value

            VerifyItemExists(test, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LabelAfterGoto2()
            Dim test = <Text>
Class C
    Sub M()
        Foo: Dim i As Integer
        Goto Foo $$"
</Text>.Value

            VerifyItemIsAbsent(test, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LabelAfterGoto3()
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
            VerifyAtPosition(text, position, "10", Nothing, SourceCodeKind.Regular, usePreviousCharAsTrigger:=True, checkForAbsence:=False, glyph:=Nothing, experimental:=False)
        End Sub

        <WorkItem(541235)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAlias1()
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

            VerifyItemExists(test, "A")
        End Sub

        <WorkItem(541235)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAlias2()
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

            VerifyItemExists(test, "M")
        End Sub

        <WorkItem(541235)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAlias3()
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

            VerifyItemExists(test, "A")
        End Sub

        <WorkItem(541235)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAlias4()
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

            VerifyItemExists(test, "M")
        End Sub

        <WorkItem(541399)>
        <WorkItem(529190)>
        <WpfFact(Skip:="529190"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterSingleLineIf()
            Dim test = <Text>
Module Program
    Sub Main(args As String())
        Dim x1 As Integer
        If True Then $$
    End Sub
End Module
</Text>.Value

            VerifyItemExists(test, "x1")
        End Sub

        <WorkItem(540442)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyInterfacesInImplementsStatements()
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

            VerifyItemExists(test.Value, "INested")
            VerifyItemIsAbsent(test.Value, "Del")
        End Sub

        <WorkItem(540442)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NestedInterfaceInImplementsClause()
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

            VerifyItemExists(test.Value, "INested")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NothingAfterBadQualifiedImplementsClause()
            Dim test = <Text>
Class SomeClass
    Implements Gibberish.$$
End Class
                       </Text>

            VerifyNoItemsExist(test.Value)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NothingAfterBadImplementsClause()
            Dim test = <Text>
Module Module1
    Sub Foo()
    End Sub
End Module

Class SomeClass
    Sub DoStuff() Implements Module1.$$
                       </Text>

            VerifyItemIsAbsent(test.Value, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DescriptionGenericTypeParameter()
            Dim test = <Text><![CDATA[
Class SomeClass(Of T)
    Sub M()
        $$
    End Sub
End Class
                       ]]></Text>

            VerifyItemExists(test.Value, "T", $"T {FeaturesResources.In} SomeClass(Of T)")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeName()
            Dim test = <Text><![CDATA[
Imports System
<$$
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameAfterSpecifier()
            Dim test = <Text><![CDATA[
Imports System
<Assembly:$$
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameInAttributeList()
            Dim test = <Text><![CDATA[
Imports System
<CLSCompliant,$$
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameInAttributeListAfterSpecifier()
            Dim test = <Text><![CDATA[
Imports System
<Assembly:CLSCompliant,Assembly:$$
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameBeforeClass()
            Dim test = <Text><![CDATA[
Imports System
<$$
Public Class C
End Class
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameAfterSpecifierBeforeClass()
            Dim test = <Text><![CDATA[
Imports System
<Assembly:$$
Public Class C
End Class
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliant")
            VerifyItemIsAbsent(test.Value, "CLSCompliantAttribute")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameInAttributeArgumentList()
            Dim test = <Text><![CDATA[
Imports System
<CLSCompliant($$
Public Class C
End Class
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliantAttribute")
            VerifyItemIsAbsent(test.Value, "CLSCompliant")
        End Sub

        <WorkItem(542225)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNameInsideClass()
            Dim test = <Text><![CDATA[
Imports System
Public Class C
    Dim c As $$
End Class
]]></Text>

            VerifyItemExists(test.Value, "CLSCompliantAttribute")
            VerifyItemIsAbsent(test.Value, "CLSCompliant")
        End Sub

        <WorkItem(542441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NewAfterMeWhenFirstStatementInCtor()
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

            VerifyItemExists(test.Value, "New")
        End Sub

        <WorkItem(542441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoNewAfterMeWhenNotFirstStatementInCtor()
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

            VerifyItemIsAbsent(test.Value, "New")
        End Sub

        <WorkItem(542441)>
        <WorkItem(759729)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoNewAfterMeWhenFirstStatementInSingleCtor()
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.$$
    End Sub
End Class
]]></Text>

            VerifyItemIsAbsent(test.Value, "New")
        End Sub

        <WorkItem(542441)>
        <WorkItem(759729)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NewAfterMyClassWhenFirstStatementInCtor()
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

            VerifyItemExists(test.Value, "New")
        End Sub

        <WorkItem(542441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoNewAfterMyClassWhenNotFirstStatementInCtor()
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

            VerifyItemIsAbsent(test.Value, "New")
        End Sub

        <WorkItem(542441)>
        <WorkItem(759729)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoNewAfterMyClassWhenFirstStatementInSingleCtor()
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim test = <Text><![CDATA[
Class C1
    Public Sub New(ByVal accountKey As Integer)
        MyClass.$$
    End Sub
End Class
]]></Text>

            VerifyItemIsAbsent(test.Value, "New")
        End Sub

        <WorkItem(542242)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyShowAttributesInAttributeNameContext1()
            ' This is different from Dev10, where we lead users to call the same .ctor, which is illegal.
            Dim markup = <Text><![CDATA[
Imports System

<$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Foo
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

            VerifyItemExists(markup, "Bar")
            VerifyItemIsAbsent(markup, "D")
        End Sub

        <WorkItem(542242)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyShowAttributesInAttributeNameContext2()
            Dim markup = <Text><![CDATA[
Imports System

<Bar.$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Foo
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

            VerifyItemExists(markup, "Foo")
            VerifyItemIsAbsent(markup, "C1")
        End Sub

        <WorkItem(542242)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyShowAttributesInAttributeNameContext3()
            Dim markup = <Text><![CDATA[
Imports System

<Bar.Foo.$$
Class C
End Class

Class D
End Class

Class Bar
    MustInherit Class Foo
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

            VerifyItemExists(markup, "Something")
            VerifyItemIsAbsent(markup, "C2")
        End Sub

        <WorkItem(542737)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub QueryVariableAfterSelectClause()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim q1 = From num In Enumerable.Range(3, 4) Select $$
]]></Text>.Value

            VerifyItemExists(markup, "num")
        End Sub

        <WorkItem(542683)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplementsClassesWithNestedInterfaces()
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

            VerifyItemExists(markup, "MyClass2")
            VerifyItemIsAbsent(markup, "MyClass3")
        End Sub

        <WorkItem(542683)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplementsClassesWithNestedInterfacesClassOutermost()
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

            VerifyItemExists(markup, "MyClass1")
        End Sub

        <WorkItem(542876)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQuerySelect1()
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

            VerifyItemExists(markup, "i")
            VerifyItemExists(markup, "j")
        End Sub

        <WorkItem(542876)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQuerySelect2()
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

            VerifyItemExists(markup, "i")
            VerifyItemExists(markup, "j")
        End Sub

        <WorkItem(542876)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQuerySelect3()
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

            VerifyItemExists(markup, "i")
            VerifyItemExists(markup, "j")
        End Sub

        <WorkItem(542927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryGroupByInto1()
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

            VerifyItemExists(markup, "Count")
        End Sub

        <WorkItem(542927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryGroupByInto2()
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

            VerifyItemExists(markup, "LongestString")
        End Sub

        <WorkItem(542927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryGroupByInto3()
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

            VerifyItemExists(markup, "LongestString")
        End Sub

        <WorkItem(542927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryGroupByInto4()
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

            VerifyItemExists(markup, "LongestString")
        End Sub

        <WorkItem(542929)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryAggregateInto1()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = $$
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "Distinct")
        End Sub

        <WorkItem(542929)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryAggregateInto2()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = $$
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "Distinct")
        End Sub

        <WorkItem(542929)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InQueryAggregateInto3()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim query = Aggregate i In New Integer() {1} Into d = Distinct(), $$
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "Sum")
        End Sub

        <WorkItem(543137)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAndKeywordInComplexJoin()
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

            VerifyItemExists(markup, "num")
        End Sub

        <WorkItem(543181)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterGroupKeywordInGroupByClause()
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

            VerifyItemExists(markup, "i1")
        End Sub

        <WorkItem(543182)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterByInGroupByClause()
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

            VerifyItemExists(markup, "i1")
        End Sub

        <WorkItem(543210)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterByInsideExprVarDeclGroupByClause()
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

            VerifyItemExists(markup, "i1")
            VerifyItemExists(markup, "arr")
            VerifyItemExists(markup, "args")
        End Sub

        <WorkItem(543213)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterGroupInsideExprVarDeclGroupByClause()
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

            VerifyItemExists(markup, "i1")
            VerifyItemExists(markup, "arr")
            VerifyItemExists(markup, "args")
        End Sub

        <WorkItem(543246)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAggregateKeyword()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Linq
 
Module Program
    Sub Main(args As String())
            Dim query = Aggregate $$
]]></Text>.Value

            VerifyNoItemsExist(markup)
        End Sub

        <WorkItem(543270)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterDelegateCreationExpression1()
            Dim markup =
<Text>
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2($$
    End Sub

    Delegate Sub Foo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</Text>.Value

            VerifyItemIsAbsent(markup, "Foo2")
            VerifyItemIsAbsent(markup, "Bar2")
        End Sub

        <WorkItem(543270)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterDelegateCreationExpression2()
            Dim markup =
<Text>
Module Program
    Sub Main(args As String())
        Dim f1 = New Foo2($$
    End Sub

    Delegate Sub Foo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</Text>.Value

            VerifyItemIsAbsent(markup, "Foo2")
            VerifyItemIsAbsent(markup, "Bar2")
        End Sub

        <WorkItem(619388)>
        <WpfFact(Skip:="619388"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OverloadsHiding()
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

            VerifyItemExists(markup, "Configure", "Sub Derived.Configure()")
            VerifyItemIsAbsent(markup, "Configure", "Sub Base.Configure()")
        End Sub

        <WorkItem(543580)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterMyBaseDot1()
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

            VerifyItemExists(markup, "Configure")
        End Sub

        <WorkItem(543580)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterMyBaseDot2()
            Dim markup = <Text>
Public Class Base
    Protected Sub Foo()
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

            VerifyItemExists(markup, "Foo")
            VerifyItemIsAbsent(markup, "Bar")
        End Sub

        <WorkItem(543547)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterRaiseEvent()
            Dim markup = <Text>
Module Program
    Public Event NewRegistrations(ByVal pStudents As String)

    Sub Main(args As String())
        RaiseEvent $$
    End Sub
End Module
</Text>.Value

            VerifyItemExists(markup, "NewRegistrations")
        End Sub

        <WorkItem(543730)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInheritedEventsAfterRaiseEvent()
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

            VerifyItemExists(markup, "derivedEvent")
            VerifyItemIsAbsent(markup, "baseEvent")
        End Sub

        <WorkItem(529116)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InSingleLineLambda1()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1) $$
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "x1")
            VerifyItemExists(markup, "x5")
        End Sub

        <WorkItem(529116)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InSingleLineLambda2()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1)$$
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "x1")
            VerifyItemExists(markup, "x5")
        End Sub

        <WorkItem(543601)>
        <WorkItem(530595)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInstanceFieldsInSharedMethod()
            Dim markup = <Text>
Class C
    Private x As Integer
    Shared Sub M()
        $$
    End Sub
End Class
</Text>.Value

            VerifyItemIsAbsent(markup, "x")
        End Sub

        <WorkItem(543601)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInstanceFieldsInSharedFieldInitializer()
            Dim markup = <Text>
Class C
    Private x As Integer
    Private Shared y As Integer = $$
End Class
</Text>.Value

            VerifyItemIsAbsent(markup, "x")
        End Sub

        <WorkItem(543601)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedFieldsInSharedMethod()
            Dim markup = <Text>
Class C
    Private Shared x As Integer
    Shared Sub M()
        $$
    End Sub
End Class
</Text>.Value

            VerifyItemExists(markup, "x")
        End Sub

        <WorkItem(543601)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedFieldsInSharedFieldInitializer()
            Dim markup = <Text>
Class C
    Private Shared x As Integer
    Private Shared y As Integer = $$
End Class
</Text>.Value

            VerifyItemExists(markup, "x")
        End Sub

        <WorkItem(543680)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInstanceFieldsFromOuterClassInInstanceMethod()
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

            VerifyItemIsAbsent(markup, "i")
        End Sub

        <WorkItem(543680)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedFieldsFromOuterClassInInstanceMethod()
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

            VerifyItemExists(markup, "i")
        End Sub

        <WorkItem(543104)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyEnumMembersInEnumTypeMemberAccess()
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

            VerifyItemExists(markup, "a")
            VerifyItemExists(markup, "b")
            VerifyItemExists(markup, "c")
            VerifyItemIsAbsent(markup, "Equals")
        End Sub

        <WorkItem(539450)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordEscaping1()
            Dim markup = <Text>
Module [Structure]
    Sub M()
        dim [dim] = 0
        console.writeline($$
    End Sub
End Module
</Text>.Value

            VerifyItemExists(markup, "dim")
            VerifyItemIsAbsent(markup, "[dim]")
            VerifyItemExists(markup, "Structure")
            VerifyItemIsAbsent(markup, "[Structure]")
        End Sub

        <WorkItem(539450)> <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordEscaping2()
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

            VerifyItemExists(markup, "dim")
            VerifyItemIsAbsent(markup, "[dim]")
            VerifyItemExists(markup, "New")
            VerifyItemIsAbsent(markup, "[New]")
            VerifyItemExists(markup, "rem")
            VerifyItemIsAbsent(markup, "[rem]")
        End Sub

        <WorkItem(539450)> <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordEscaping3()
            Dim markup = <Text>
Namespace Foo
    Module [Structure]
        Sub M()
            Dim x as Foo.$$
        End Sub
    End Module
End Namespace
</Text>.Value

            VerifyItemExists(markup, "Structure")
            VerifyItemIsAbsent(markup, "[Structure]")
        End Sub

        <WorkItem(539450)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeKeywordEscaping()
            Dim markup = <Text>
Imports System
Class classattribute : Inherits Attribute
End Class
&lt;$$
Class C
End Class
</Text>.Value

            VerifyItemExists(markup, "class")
            VerifyItemIsAbsent(markup, "[class]")
        End Sub

        <WorkItem(645898)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EscapedKeywordAttributeCommit()
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

            VerifyProviderCommit(markup, "class", expected, "("c, "")
        End Sub

        <WorkItem(543104)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllMembersInEnumLocalAccess()
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

            VerifyItemExists(markup, "a")
            VerifyItemExists(markup, "b")
            VerifyItemExists(markup, "c")
            VerifyItemExists(markup, "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReadOnlyPropertiesPresentOnRightSideInObjectInitializer()
            Dim text = <a>Class C
    Public Property Foo As Integer
    Public ReadOnly Property Bar As Integer
        Get
            Return 0
        End Get
    End Property

    Sub M()
        Dim c As New C With { .Foo = .$$
    End Sub
End Class</a>.Value

            VerifyItemExists(text, "Foo")
            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableNotBeforeExplicitDeclaration_ExplicitOff()
            Dim text = <Text>
Option Explicit Off
Class C
    Sub M()
        $$
        Dim foo = 3
    End Sub
End Class</Text>.Value

            VerifyItemIsAbsent(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableNotBeforeExplicitDeclaration_ExplicitOn()
            Dim text = <Text>
Option Explicit On
Class C
    Sub M()
        $$
        Dim foo = 3
    End Sub
End Class</Text>.Value

            VerifyItemIsAbsent(text, "foo")
        End Sub

        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <WorkItem(530595)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableBeforeImplicitDeclaration()
            Dim text = <Text>
Option Explicit Off
Class C
    Function M() as Integer
        $$
        Return foo
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableInItsDeclaration()
            ' "Dim foo As Integer = foo" is legal code while "Dim foo = foo" is not, but
            ' offer the local name on the right in either case because in the second
            ' case there's an error stating that foo needs to be explicitly typed and
            ' the user can then add the As clause. This mimics the behavior of 
            ' "var x = x = 0" in C#.
            Dim text = <Text>
Class C
    Sub M()
        Dim foo = $$
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableInItsDeclarator()
            Dim text = <Text>
Class C
    Sub M()
        Dim foo = 4, bar = $$, baz = 5
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "bar")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableNotBeforeItsDeclarator()
            Dim text = <Text>
Class C
    Sub M()
        Dim foo = $$, bar = 5
    End Sub
End Class</Text>.Value

            VerifyItemIsAbsent(text, "bar")
        End Sub

        <WpfFact>
        <WorkItem(10572, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LocalVariableAfterDeclarator()
            Dim text = <Text>
Class C
    Sub M()
        Dim foo = 5, bar = $$
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(545439)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayAfterReDim()
            Dim text = <Text>
Class C
    Sub M()
        Dim foo(10, 20) As Integer
        ReDim $$
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(545439)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ArrayAfterReDimPreserve()
            Dim text = <Text>
Class C
    Sub M()
        Dim foo(10, 20) As Integer
        ReDim Preserve $$
    End Sub
End Class</Text>.Value

            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact>
        <WorkItem(546353)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoNamespaceDeclarationIntellisense()
            Dim text = <Text>
Namespace Foo.$$
Class C
End Class</Text>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(531258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LabelsAfterOnErrorGoTo()
            Dim code =
<Code>
Class C
    Sub M()
        On Error GoTo $$

        label1:
            Dim x = 1
    End Sub
End Class</Code>.Value

            VerifyItemExists(code, "label1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AwaitableItem()
            Dim code =
<Code>
Imports System.Threading.Tasks
Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Foo() As Task
        Me.$$
        End Function
End Class</Code>.Value

            Dim description =
$"<{VBFeaturesResources.Awaitable}> Function C.Foo() As Task
Doc Comment!
{WorkspacesResources.Usage}
  {VBFeaturesResources.Await} Foo()"

            VerifyItemWithMscorlib45(code, "Foo", description, LanguageNames.VisualBasic)
        End Sub

        <WorkItem(550760)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterAwait()
            Dim code =
<Code>
Imports System.Threading.Tasks
Class SomeClass
    Public Async Sub foo()
        Await $$
    End Sub
 
    Async Function Bar() As Task(Of Integer)
        Return Await Task.Run(Function() 42)
    End Function
End Class</Code>.Value


            VerifyItemExists(code, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ObsoleteItem()
            Dim code =
<Code>
Imports System
Class SomeClass
    &lt;Obsolete&gt;
    Public Sub Foo()
        $$
    End Sub
End Class</Code>.Value

            VerifyItemExists(code, "Foo", $"({VBFeaturesResources.Deprecated}) Sub SomeClass.Foo()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExpressionAfterYield()
            Dim code =
<Code>
Class SomeClass
    Iterator Function Foo() As Integer
        Dim x As Integer
        Yield $$
    End Function
End Class
</Code>.Value

            VerifyItemExists(code, "x")
        End Sub

        <WorkItem(568986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoMembersOnDottingIntoUnboundType()
            Dim code =
<Code>
Module Program
    Dim foo As RegistryKey

    Sub Main(args() As String)
        foo.$$
    End Sub
End Module
</Code>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(611154)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoOperators()
            VerifyItemIsAbsent(
                    AddInsideMethod("String.$$"), "op_Equality")
        End Sub

        <WorkItem(736891)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InBinaryConditionalExpression()
            Dim code =
<Code>
Module Program
    Sub Main(args() As String)
        args = If($$
    End Sub
End Module
</Code>.Value

            VerifyItemExists(code, "args")
        End Sub

        <WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InTopLevelFieldInitializer()
            Dim code =
<Code>
Dim aaa = 1
Dim bbb = $$
</Code>.Value

            VerifyItemExists(code, "aaa")
        End Sub

#End Region

#Region "SharedMemberSourceTests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation1()
            VerifyItemIsAbsent("System.Console.$$", "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation2()
            VerifyItemIsAbsent(AddImportsStatement("Imports System", "System.Console.$$"), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation3()
            VerifyItemIsAbsent("Imports System.Console.$$", "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation4()
            VerifyItemIsAbsent(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "' Console.$$")), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation5()
            VerifyItemIsAbsent(AddImportsStatement("Imports System", AddInsideMethod("Dim d = ""Console.$$")), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvalidLocation6()
            VerifyItemIsAbsent("<System.Console.$$>", "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InsideMethodBody()
            VerifyItemExists(AddImportsStatement("Imports System", AddInsideMethod("Console.$$")), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InsideAccessorBody()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "     Property Prop As String",
                                  "         Get",
                                  "             Console.$$")), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldInitializer()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "     Dim d = Console.$$")), "Beep")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMethods()
            VerifyItemExists(
                AddImportsStatement("Imports System",
                    CreateContent("Class C ",
                                  "Private Shared Function Method() As Boolean",
                                  "End Function",
                                  "     Dim d = $$",
                                  "")), "Method")
        End Sub

#End Region

#Region "EditorBrowsableTests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub


        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
   Sub M()
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_Overloads_BothBrowsableAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar(x as Integer)     
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_Overloads_BothBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OverriddenSymbolsFilteredFromCompletionList()

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
    Public Overridable Sub Foo(original As Integer) 
    End Sub
End Class

Public Class D
    Inherits B
    Public Overrides Sub Foo(derived As Integer) 
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()

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
    Public Sub Foo() 
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()

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
    Public Sub Foo() 
    End Sub
End Class

Public Class D
    Inherits B
    Public Overloads Sub Foo(x As Integer)
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_HidingWithDifferentArgumentList()

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
    Public Sub Foo() 
    End Sub
End Class

Public Class D
    Inherits B
    Public Sub Foo(x As Integer)
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()

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
    Public Sub Foo() 
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()

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
    Public Sub Foo(t As T)  
    End Sub

    Public Sub Foo(i as Integer)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()

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
    Public Sub Foo(t as T)  
    End Sub

    Public Sub Foo(i as Integer)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()

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
    Public Sub Foo(t As T)  
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()

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
    Public Sub Foo(t As T)  
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()

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
    Public Sub Foo(t As T)
    End Sub
  
    Public Sub Foo(u As U)
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=2,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()

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
    Public Sub Foo(t As T)
    End Sub
  
    Public Sub Foo(u As U)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()

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
    Public Sub Foo(t As T)
    End Sub
  
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(u As U)  
    End Sub
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=2,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Field_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public bar As Integer
End Class
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Field_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public bar As Integer
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Field_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public bar As Integer
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Property_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Property_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Property_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim foo As Foo
        foo.$$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Bar",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Constructor_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Constructor_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Constructor_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Constructor_MixedOverloads1()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub

    Public Sub New(x As Integer)    
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Constructor_MixedOverloads2()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x = New $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Event_BrowsableStateNever()

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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Event_BrowsableStateAlways()

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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Event_BrowsableStateAdvanced()

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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Handler",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Delegate_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Delegate Sub DelegateType()
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Delegate_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Delegate Sub DelegateType()
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Delegate_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Event e As $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Delegate Sub DelegateType()
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="DelegateType",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateNever_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateNever_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateNever_FullyQualified()

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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAlways_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAlways_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAlways_FullyQualified()

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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Inherits $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Class Foo
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_BrowsableStateAdvanced_FullyQualified()
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
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="C",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Class_IgnoreBaseClassBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    Inherits Bar
End Class

<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Bar
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Struct_BrowsableStateNever_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Structure Foo
End Structure
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Structure Foo
End Structure
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Structure Foo
End Structure
]]></Text>.Value

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Enum_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Enum Foo
    A
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Enum_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Enum Foo
    A
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Enum_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Enum Foo
    A
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateNever_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateNever_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal()

            Dim markup = <Text><![CDATA[
Class Program
    Public Sub M()
        $$    
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom()

            Dim markup = <Text><![CDATA[
Class Program
    Implements $$
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Interface Foo
End Interface
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_CrossLanguage_VBtoCS_Always()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Foo
{
}
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.CSharp,
                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_CrossLanguage_VBtoCS_Never()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Foo
{
}
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Foo",
                expectedSymbolsSameSolution:=0,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.CSharp,
                hideAdvancedMembers:=False)
        End Sub
#End Region

#Region "Inherits/Implements Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AfterInherits()
            Const markup = "
Public Class Base
End Class

Class Derived
    Inherits $$
End Class
"

            VerifyItemExists(markup, "Base")
            VerifyItemIsAbsent(markup, "Derived")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AfterInheritsDotIntoClass()
            Const markup = "
Public Class Base
    Public Class Nest
    End Class
End Class

Class Derived
    Inherits Base.$$
End Class
"

            VerifyItemExists(markup, "Nest")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_AfterImplements()
            Const markup = "
Public Interface IFoo
End Interface

Class C
    Implements $$
End Class
"

            VerifyItemExists(markup, "IFoo")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_AliasedInterfaceAfterImplements()
            Const markup = "
Imports IAlias = IFoo
Public Interface IFoo
End Interface

Class C
    Implements $$
End Class
"

            VerifyItemExists(markup, "IAlias")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_AliasedNamespaceAfterImplements()
            Const markup = "
Imports AliasedNS = NS1
Namespace NS1
    Public Interface IFoo
    End Interface

    Class C
        Implements $$
    End Class
End Namespace
"

            VerifyItemExists(markup, "AliasedNS")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AliasedClassAfterInherits()
            Const markup = "
Imports AliasedClass = Base
Public Class Base
End Interface

Class C
    Inherits $$
End Class
"

            VerifyItemExists(markup, "AliasedClass")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AliasedNamespaceAfterInherits()
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

            VerifyItemExists(markup, "AliasedNS")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AliasedClassAfterInherits2()
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

            VerifyItemExists(markup, "AliasedClass")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_AfterImplementsComma()
            Const markup = "
Public Interface IFoo
End Interface

Public Interface IBar
End interface

Class C
    Implements IFoo, $$
End Class
"

            VerifyItemExists(markup, "IBar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_ClassContainingInterface()
            Const markup = "
Public Class Base
    Public Interface Nest
    End Class
End Class

Class Derived
    Implements $$
End Class
"

            VerifyItemExists(markup, "Base")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_NoClassNotContainingInterface()
            Const markup = "
Public Class Base
End Class

Class Derived
    Implements $$
End Class
"

            VerifyItemIsAbsent(markup, "Base")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_GenericClass()
            Const markup = "
Public Class base(Of T)

End Class

Public Class derived
    Inherits $$
End Class
"

            VerifyItemExists(markup, "base(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_GenericInterface()
            Const markup = "
Public Interface IFoo(Of T)

End Interface

Public Class bar
    Implements $$
End Class
"

            VerifyItemExists(markup, "IFoo(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(546610)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_IncompleteClassDeclaration()
            Const markup = "
Public Interface IFoo
End Interface
Public Interface IBar
End interface
Class C
    Implements IFoo,$$
"

            VerifyItemExists(markup, "IBar")
        End Sub

        <WorkItem(546611)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_NotNotInheritable()
            Const markup = "
Public NotInheritable Class D
End Class
Class C
    Inherits $$
"

            VerifyItemIsAbsent(markup, "D")
        End Sub

        <WorkItem(546802)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_KeywordIdentifiersShownUnescaped()
            Const markup = "
Public Class [Inherits]
End Class
Class C
    Inherits $$
"

            VerifyItemExists(markup, "Inherits")
        End Sub

        <WorkItem(546802)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_KeywordIdentifiersCommitEscaped()
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

            VerifyProviderCommit(markup, "Inherits", expected, "."c, "")
        End Sub

        <WorkItem(546801)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_Modules()
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

            VerifyItemExists(markup, "Module2")
            VerifyItemIsAbsent(markup, "Module1")
        End Sub

        <WorkItem(530726)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_DoNotShowNamespaceWithNoApplicableClasses()
            Const markup = "
Namespace N
    Module M
    End Module
End Namespace
Class C
    Inherits $$
End Class
"

            VerifyItemIsAbsent(markup, "N")
        End Sub

        <WorkItem(530725)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_CheckStructContents()
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
            VerifyItemIsAbsent(markup, "S2")
            VerifyItemExists(markup, "S1")
        End Sub

        <WorkItem(530724)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_NamespaceContainingInterface()
            Const markup = "
Namespace N
    Interface IFoo
    End Interface
End Namespace
Class C
    Implements $$
End Class
"

            VerifyItemExists(markup, "N")
        End Sub

        <WorkItem(531256)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_OnlyInterfacesForInterfaceInherits1()
            Const markup = "
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IFoo
    Inherits $$
"

            VerifyItemExists(markup, "ITestInterface")
            VerifyItemIsAbsent(markup, "TestClass")
        End Sub

        <WorkItem(1036374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_InterfaceCircularInheritance()
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

            VerifyItemExists(markup, "ITestInterface")
            VerifyItemIsAbsent(markup, "TestClass")
        End Sub

        <WorkItem(531256)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_OnlyInterfacesForInterfaceInherits2()
            Const markup = "
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IFoo
    Implements $$
"

            VerifyItemIsAbsent(markup, "ITestInterface")
            VerifyItemIsAbsent(markup, "TestClass")
        End Sub

        <WorkItem(547291)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_CommitGenericOnParen()
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

            VerifyProviderCommit(markup, "G(Of " & s_unicodeEllipsis & ")", expected, "("c, "")
        End Sub

        <WorkItem(579186)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Implements_AfterImplementsWithCircularInheritance()
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

            VerifyItemExists(markup, "I(Of …)")
        End Sub

        <WorkItem(622563)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_CommitNonGenericOnParen()
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
            VerifyProviderCommit(markup, "G", expected, "("c, "")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_AfterInheritsWithCircularInheritance()
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

            VerifyItemExists(markup, "B")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_ClassesInsideSealedClasses()
            Const markup = "
Public NotInheritable Class G
    Public Class H

    End Class
End Class

Class SomeClass
    Inherits $$

End Class
"

            VerifyItemExists(markup, "G")
        End Sub

        <WorkItem(638762)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Inherits_ClassWithinNestedStructs()
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

            VerifyItemExists(markup, "somestruct")
        End Sub

#End Region

        <WorkItem(715146)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExtensionMethodsOffered()
            Dim markup = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Class Program
    Sub Main(args As String())
        Me.$$
    End Sub
End Class
Module Extensions
    <Extension>
    Sub Foo(program As Program)
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "Foo")
        End Sub

        <WorkItem(715146)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExtensionMethodsOffered2()
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
    Sub Foo(program As Program)
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "Foo")
        End Sub

        <WorkItem(715146)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LinqExtensionMethodsOffered()
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

            VerifyItemExists(markup, "Average")
        End Sub

        <WorkItem(884060)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionOffTypeParameter()
            Dim markup = <Text><![CDATA[
Module Program
    Function bar(Of T As Object)() As T
        T.$$
    End Function
End Moduleb
End Class
]]></Text>.Value

            VerifyNoItemsExist(markup)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AvailableInBothLinkedFiles()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
    Dim x as integer
    sub foo()
        $$
    end sub
end class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences=" True" AssemblyName="Proj2">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            VerifyItemInLinkedFiles(markup, "x", $"({FeaturesResources.Field}) C.x As Integer")
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AvailableInOneLinkedFile()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj1" PreprocessorSymbols="FOO=True">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
#If FOO Then
    Dim x as integer
#End If
            Sub foo()
        $$
    End Sub
        End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj2">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Dim expectedDescription = $"({FeaturesResources.Field}) C.x As Integer" + vbCrLf + vbCrLf + String.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available) + vbCrLf + String.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable) + vbCrLf + vbCrLf + FeaturesResources.UseTheNavigationBarToSwitchContext
            VerifyItemInLinkedFiles(markup, "x", expectedDescription)
        End Sub

        <WorkItem(909121)>
        <WorkItem(2048, "https://github.com/dotnet/roslyn/issues/2048")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitGenericOnParen()
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

            VerifyProviderCommit(text, "G(Of …)", expected, "("c, "")
        End Sub

        <WorkItem(668159)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributesShownWithBraceCompletionActive()
            Dim text =
<code><![CDATA[
Imports System
<$$>
Class C
End Class

Class FooAttribute
    Inherits Attribute
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WorkItem(991466)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DescriptionInAliasedType()
            Dim text =
<code><![CDATA[
Imports IAlias = IFoo
Class C
    Dim x as IA$$
End Class

''' <summary>
''' summary for interface IFoo
''' </summary>
Interface IFoo
    Sub Bar()
End Interface
]]></code>.Value

            VerifyItemExists(text, "IAlias", expectedDescriptionOrNull:="Interface IFoo" + vbCrLf + "summary for interface IFoo")
        End Sub

        <WorkItem(842049)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MergedNamespace1()
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

            VerifyItemExists(text, "C")
            VerifyItemExists(text, "D")
        End Sub

        <WorkItem(842049)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MergedNamespace2()
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
    
    Sub Foo()
        X.$$
    End Sub
End Module

]]></code>.Value

            VerifyItemExists(text, "C")
            VerifyItemExists(text, "D")
        End Sub

        <WorkItem(925469)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitWithCloseBracketLeaveOpeningBracket1()
            Dim text =
<code><![CDATA[
Class Await
    Sub Foo()
        Dim x = new [Awa$$]
    End Sub
End Class]]></code>.Value

            Dim expected =
<code><![CDATA[
Class Await
    Sub Foo()
        Dim x = new [Await]
    End Sub
End Class]]></code>.Value

            VerifyProviderCommit(text, "Await", expected, "]"c, Nothing)
        End Sub

        <WorkItem(925469)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitWithCloseBracketLeavesOpeningBracket2()
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Foo()
        Dim x = new [Cla$$]
    End Sub
End Class]]></code>.Value

            Dim expected =
<code><![CDATA[
Class [Class]
    Sub Foo()
        Dim x = new [Class]
    End Sub
End Class]]></code>.Value

            VerifyProviderCommit(text, "Class", expected, "]"c, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConditionalOperatorCompletion()
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Foo()
        Dim x = new Object()
        x?.$$
    End Sub
End Class]]></code>.Value

            VerifyItemExists(text, "ToString", experimental:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConditionalOperatorCompletion2()
            Dim text =
<code><![CDATA[
Class [Class]
    Sub Foo()
        Dim x = new Object()
        x?.ToString()?.$$
    End Sub
End Class]]></code>.Value

            VerifyItemExists(text, "ToString", experimental:=True)
        End Sub

        <WorkItem(1041269)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub HidePropertyBackingFieldAndEventsAtExpressionLevel()
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

            VerifyItemIsAbsent(text, "_p")
            VerifyItemIsAbsent(text, "e")
        End Sub

        <WorkItem(1041269)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NullableForConditionalAccess()
            Dim text =
<code><![CDATA[
Class C
    Sub Foo()
        Dim x as Integer? = Nothing
        x?.$$
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "GetTypeCode")
            VerifyItemIsAbsent(text, "HasValue")
        End Sub

        <WorkItem(1079694)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DontThrowForNullPropagatingOperatorInErase()
            Dim text =
<code><![CDATA[
Module Program
    Sub Main()
        Dim x?(1)
        Erase x?.$$
    End Sub
End Module
]]></code>.Value

            VerifyItemExists(text, "ToString")
        End Sub

        <WorkItem(1109319)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UnwrapNullableForConditionalFromStructure()
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

            VerifyItemExists(text, "b")
            VerifyItemIsAbsent(text, "c")
        End Sub

        <WorkItem(1109319)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WithinChainOfConditionalAccess()
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

            VerifyItemExists(text, "b")
            VerifyItemIsAbsent(text, "c")
        End Sub


        <WorkItem(1079694)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DontThrowForNullPropagatingOperatorOnTypeParameter()
            Dim text =
<code><![CDATA[
Module Program
    Sub Foo(Of T)(x As T)
        x?.$$
    End Sub
End Module
]]></code>.Value

            VerifyItemExists(text, "ToString")
        End Sub

        <WorkItem(1079723)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionAfterNullPropagatingOperatingInWithBlock()
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

            VerifyItemExists(text, "Length")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext1()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext2()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext3()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext4()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf($$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext5()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext6()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext7()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext8()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub M()
        Dim s = NameOf(C.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext9()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(Me.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext10()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(Me.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext11()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyClass.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext12()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyClass.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext13()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemIsAbsent(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext14()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim s = NameOf(MyBase.$$
    End Sub

    Shared Sub Foo()
    End Sub

    Sub Bar()
    End Sub
End Class
]]></code>.Value

            VerifyItemIsAbsent(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext15()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub Foo()
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

            VerifyItemExists(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInNameOfArgumentContext16()
            Dim text =
<code><![CDATA[
Class C
    Shared Sub Foo()
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

            VerifyItemExists(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInInterpolationExpressionContext1()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{$$}"
    End Sub
End Class
]]></code>.Value

            VerifyItemExists(text, "x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowCompletionInInterpolationExpressionContext2()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{$$
]]></code>.Value

            VerifyItemExists(text, "x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionInInterpolationAlignmentContext()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{x,$$}"
    End Sub
End Class
]]></code>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionInInterpolationFormatContext()
            Dim text =
<code><![CDATA[
Class C
    Sub M()
        Dim x = 1
        Dim s = $"{x:$$}"
    End Sub
End Class
]]></code>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(1293, "https://github.com/dotnet/roslyn/issues/1293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TriggeredAfterDotInWithAfterNumericLiteral()
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

            VerifyItemExists(text, "M", usePreviousCharAsTrigger:=True)
        End Sub

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionForConditionalAccessOnTypes1()
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        System?.$$
    End Sub
End Module
]]></code>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionForConditionalAccessOnTypes2()
            Dim text =
<code><![CDATA[
Module Program
    Sub Main(args As String())
        Console?.$$
    End Sub
End Module
]]></code>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionForConditionalAccessOnTypes3()
            Dim text =
<code><![CDATA[
Imports a = System
Module Program
    Sub Main(args As String())
        a?.$$
    End Sub
End Module
]]></code>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(3086, "https://github.com/dotnet/roslyn/issues/3086")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedMembersOffInstanceInColorColor()
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

            VerifyItemExists(text, "X")
            VerifyItemExists(text, "Y")
        End Sub

        <WorkItem(3086, "https://github.com/dotnet/roslyn/issues/3086")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotSharedMembersOffAliasInColorColor()
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

            VerifyItemExists(text, "X")
            VerifyItemIsAbsent(text, "Y")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType()
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
            VerifyItemExists(text, "_field")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType2()
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
            End Sub
        End Class
    End Class
End Class
]]></code>.Value
            VerifyItemExists(text, "M")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType3()
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
            End Sub
        End Class
    End Class
End Class
]]></code>.Value
            VerifyItemIsAbsent(text, "M")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType4()
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
            End Sub
        End Class
    End Class
End Class
]]></code>.Value
            VerifyItemExists(text, "M")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType5()
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
            VerifyItemIsAbsent(text, "Q")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InstanceMembersFromBaseOuterType6()
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
            VerifyItemIsAbsent(text, "X")
        End Sub

        <WorkItem(4900, "https://github.com/dotnet/roslyn/issues/4090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInstanceMembersWhenDottingIntoType()
            Dim text =
<code><![CDATA[
Class Instance
    Public Shared x as Integer
    Public y as Integer
End Class

Class Program
    Sub Foo()
        Instance.$$
    End Sub
End Class
]]></code>.Value
            VerifyItemIsAbsent(text, "y")
            VerifyItemExists(text, "x")
        End Sub

        <WorkItem(4900, "https://github.com/dotnet/roslyn/issues/4090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoSharedMemberWhenDottingIntoInstance()
            Dim text =
<code><![CDATA[
Class Instance
    Public Shared x as Integer
    Public y as Integer
End Class

Class Program
    Sub Foo()
        Dim x = new Instance()
        x.$$
    End Sub
End Class
]]></code>.Value
            VerifyItemIsAbsent(text, "x")
            VerifyItemExists(text, "y")
        End Sub

        <WorkItem(4136, "https://github.com/dotnet/roslyn/issues/4136")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoValue__WhenDottingIntoEnum()
            Dim text =
<code><![CDATA[
Enum E
    A
End Enum

Class Program
    Sub Foo()
        E.$$
    End Sub
End Class
]]></code>.Value
            VerifyItemExists(text, "A")
            VerifyItemIsAbsent(text, "value__")
        End Sub

        <WorkItem(4136, "https://github.com/dotnet/roslyn/issues/4136")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoValue__WhenDottingIntoLocalOfEnumType()
            Dim text =
<code><![CDATA[
Enum E
    A
End Enum

Class Program
    Sub Foo()
        Dim x = E.A
        x.$$
    End Sub
End Class
]]></code>.Value
            VerifyItemIsAbsent(text, "value__")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Sub SharedProjectFieldAndPropertiesTreatedAsIdentical()
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
    Sub foo()
        x$$
    End Sub
End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="True" AssemblyName="Proj2" PreprocessorSymbols="TWO=True">
                                 <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Dim expectedDescription = $"(field) C.x As Integer"
            VerifyItemInLinkedFiles(markup, "x", expectedDescription)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SharedProjectFieldAndPropertiesTreatedAsIdentical2()
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
    Sub foo()
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
            VerifyItemInLinkedFiles(markup, "x", expectedDescription)
        End Sub

        <WorkItem(4405, "https://github.com/dotnet/roslyn/issues/4405")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub VerifyDelegateEscapedWhenCommitted()
            Dim text =
<code><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim x As {0}
    End Sub
End Module

]]></code>.Value
            VerifyProviderCommit(markupBeforeCommit:=String.Format(text, "$$"),
                                 itemToCommit:="Delegate",
                                 expectedCodeAfterCommit:=String.Format(text, "[Delegate]"),
                                 commitChar:=Nothing,
                                 textTypedSoFar:="")
        End Sub


        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncExcludedInExpressionContext1()
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
            VerifyItemIsAbsent(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncExcludedInExpressionContext2()
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
            VerifyItemIsAbsent(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncExcludedInStatementContext()
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
            VerifyItemIsAbsent(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncIncludedInGetType()
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
            VerifyItemExists(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncIncludedInTypeOf()
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
            VerifyItemExists(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncIncludedInReturnTypeContext()
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
            VerifyItemExists(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemFuncIncludedInFieldTypeContext()
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Dim x as $$
End Module
]]></code>.Value
            VerifyItemExists(text, "Func(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemDelegateInStatementContext()
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
            VerifyItemExists(text, "Delegate")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionExcludedInExpressionContext1()
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
            VerifyItemIsAbsent(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionExcludedInExpressionContext2()
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
            VerifyItemIsAbsent(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionExcludedInStatementContext()
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
            VerifyItemIsAbsent(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionIncludedInGetType()
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
            VerifyItemExists(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionIncludedInTypeOf()
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
            VerifyItemExists(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionIncludedInReturnTypeContext()
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
            VerifyItemExists(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemActionIncludedInFieldTypeContext()
            Dim text =
<code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Dim x as $$
End Module
]]></code>.Value
            VerifyItemExists(text, "Action(Of " & s_unicodeEllipsis & ")")
        End Sub

        <WorkItem(4428, "https://github.com/dotnet/roslyn/issues/4428")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SystemDelegateInExpressionContext()
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
            VerifyItemExists(text, "Delegate")
        End Sub

        <WorkItem(4750, "https://github.com/dotnet/roslyn/issues/4750")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConditionalAccessInWith1()
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
            VerifyItemExists(text, "Length")
        End Sub

        <WorkItem(4750, "https://github.com/dotnet/roslyn/issues/4750")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConditionalAccessInWith2()
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
            VerifyItemExists(text, "Length")
        End Sub
    End Class
End Namespace