' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualBasic.CompilerServices
Imports Roslyn.Test.Utilities
Imports Xunit

Public Class SyntaxFactsTests
    <Fact>
    Public Sub IsKeyword1()
        Assert.False(CType(Nothing, SyntaxToken).IsKeyword())
    End Sub

    <Fact>
    Public Sub IsKeyword2()
        Assert.True(SyntaxFactory.Token(SyntaxKind.MyClassKeyword).IsKeyword())
    End Sub

    <Fact>
    Public Sub IsKeyword3()
        Assert.False(SyntaxFactory.IntegerLiteralToken("1", LiteralBase.Decimal, TypeCharacter.None, 1).IsKeyword())
    End Sub

    <Fact>
    Public Sub IsXmlTextToken1()
        Assert.False(SyntaxFacts.IsXmlTextToken(Nothing))
    End Sub

    <Fact>
    Public Sub IsXmlTextToken2()
        Assert.True(SyntaxFacts.IsXmlTextToken(SyntaxKind.XmlTextLiteralToken))
    End Sub

    <Fact>
    Public Sub IsXmlTextToken3()
        Assert.False(SyntaxFacts.IsXmlTextToken(SyntaxKind.AtToken))
    End Sub

    <Fact>
    Public Sub Bug2644()
        Assert.Equal(SyntaxKind.ClassKeyword, SyntaxFacts.GetKeywordKind("Class"))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetKeywordKind("Where"))
    End Sub

    <Fact>
    Public Sub GetAccessorStatementKind()
        Assert.Equal(SyntaxKind.GetAccessorStatement, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.GetKeyword))
        Assert.Equal(SyntaxKind.SetAccessorStatement, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.SetKeyword))
        Assert.Equal(SyntaxKind.RemoveHandlerStatement, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.RemoveHandlerKeyword))
        Assert.Equal(SyntaxKind.AddHandlerStatement, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.AddHandlerKeyword))
        Assert.Equal(SyntaxKind.RaiseEventAccessorStatement, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.RaiseEventKeyword))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetAccessorStatementKind(SyntaxKind.AddressOfKeyword))
    End Sub

    <Fact>
    Public Sub GetBaseTypeStatementKind()
        Assert.Equal(SyntaxKind.EnumStatement, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.EnumKeyword))
        Assert.Equal(SyntaxKind.ClassStatement, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.ClassKeyword))
        Assert.Equal(SyntaxKind.StructureStatement, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.StructureKeyword))
        Assert.Equal(SyntaxKind.InterfaceStatement, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.InterfaceKeyword))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.ForKeyword))
    End Sub

    <Fact>
    Public Sub GetBinaryExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IsKeyword, SyntaxKind.IsNotKeyword, SyntaxKind.LikeKeyword, SyntaxKind.AndKeyword, SyntaxKind.AndAlsoKeyword, SyntaxKind.OrKeyword, SyntaxKind.OrElseKeyword, SyntaxKind.XorKeyword, SyntaxKind.AmpersandToken, SyntaxKind.AsteriskToken, SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.SlashToken, SyntaxKind.BackslashToken, SyntaxKind.ModKeyword, SyntaxKind.CaretToken, SyntaxKind.LessThanToken, SyntaxKind.LessThanEqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.EqualsToken, SyntaxKind.GreaterThanToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.LessThanLessThanToken, SyntaxKind.GreaterThanGreaterThanToken}
            Assert.NotEqual(SyntaxKind.None, SyntaxFacts.GetBinaryExpression(item))
        Next
        Assert.Equal(SyntaxKind.SubtractExpression, SyntaxFacts.GetBinaryExpression(SyntaxKind.MinusToken))
        Assert.Equal(SyntaxKind.AndAlsoExpression, SyntaxFacts.GetBinaryExpression(SyntaxKind.AndAlsoKeyword))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBinaryExpression(SyntaxKind.ForKeyword))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.ForKeyword))
    End Sub

    <Fact>
    Public Sub GetBlockName()
        Assert.Equal("Case", SyntaxFacts.GetBlockName(SyntaxKind.CaseBlock))
        Assert.Equal("Do Loop", SyntaxFacts.GetBlockName(SyntaxKind.DoLoopForeverBlock))
        Assert.Equal("Do Loop", SyntaxFacts.GetBlockName(SyntaxKind.DoLoopTopTestBlock))
        Assert.Equal("While", SyntaxFacts.GetBlockName(SyntaxKind.WhileBlock))
        Assert.Equal("With", SyntaxFacts.GetBlockName(SyntaxKind.WithBlock))
        Assert.Equal("SyncLock", SyntaxFacts.GetBlockName(SyntaxKind.SyncLockBlock))
        Assert.Equal("Using", SyntaxFacts.GetBlockName(SyntaxKind.UsingBlock))
        Assert.Equal("For", SyntaxFacts.GetBlockName(SyntaxKind.ForBlock))
        Assert.Equal("For Each", SyntaxFacts.GetBlockName(SyntaxKind.ForEachBlock))
        Assert.Equal("Select", SyntaxFacts.GetBlockName(SyntaxKind.SelectBlock))
        Assert.Equal("If", SyntaxFacts.GetBlockName(SyntaxKind.SingleLineIfStatement))
        Assert.Equal("If", SyntaxFacts.GetBlockName(SyntaxKind.MultiLineIfBlock))
        Assert.Equal("Else If", SyntaxFacts.GetBlockName(SyntaxKind.ElseIfPart))
        Assert.Equal("Else", SyntaxFacts.GetBlockName(SyntaxKind.SingleLineElsePart))
        Assert.Equal("Else", SyntaxFacts.GetBlockName(SyntaxKind.ElsePart))
        Assert.Equal("Try", SyntaxFacts.GetBlockName(SyntaxKind.TryBlock))
        Assert.Equal("Catch", SyntaxFacts.GetBlockName(SyntaxKind.CatchPart))
        Assert.Equal("Finally", SyntaxFacts.GetBlockName(SyntaxKind.FinallyPart))

        Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                          Dim i = SyntaxFacts.GetBlockName(SyntaxKind.TryPart)
                                                      End Sub)
    End Sub

    <Fact>
    Public Sub GetContextualKeywordKind()
        Assert.Equal(SyntaxKind.MidKeyword, SyntaxFacts.GetContextualKeywordKind("mid"))
        Assert.Equal(SyntaxKind.FromKeyword, SyntaxFacts.GetContextualKeywordKind("from"))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetContextualKeywordKind(String.Empty))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBaseTypeStatementKind(SyntaxKind.ForKeyword))

        Dim expected = New String() {"aggregate", "all", "ansi", "ascending", "assembly", "async", "auto", "await", "binary", "by", "compare", "custom", "descending", "distinct", "equals", "explicit", "externalsource", "externalchecksum", "from", "group", "infer", "into", "isfalse", "istrue", "iterator", "join", "key", "mid", "off", "order", "out", "preserve", "region", "skip", "strict", "take", "text", "unicode", "until", "where", "type", "xml", "yield"}
        For Each item In expected
            Assert.NotEqual(SyntaxKind.None, SyntaxFacts.GetContextualKeywordKind(item))
        Next

        Dim actualCount = Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetContextualKeywordKinds)
        Assert.Equal(Of Integer)(expected.Count, actualCount)
    End Sub

    <Fact>
    <WorkItem(15925, "DevDiv_Projects/Roslyn")>
    Public Sub GetContextualKeywordsKinds()
        Assert.NotEqual(Of Integer)(0, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetContextualKeywordKinds))
        Assert.Contains(Of SyntaxKind)(SyntaxKind.FromKeyword, SyntaxFacts.GetContextualKeywordKinds)
        Assert.DoesNotContain(Of SyntaxKind)(SyntaxKind.DimKeyword, SyntaxFacts.GetContextualKeywordKinds)
        Assert.DoesNotContain(Of SyntaxKind)(SyntaxKind.StaticKeyword, SyntaxFacts.GetContextualKeywordKinds)
    End Sub

    <Fact>
    Public Sub GetInstanceExpresssion()
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetInstanceExpression(SyntaxKind.DeclareKeyword))
        Assert.Equal(SyntaxKind.MeExpression, SyntaxFacts.GetInstanceExpression(SyntaxKind.MeKeyword))
        Assert.Equal(SyntaxKind.MyBaseExpression, SyntaxFacts.GetInstanceExpression(SyntaxKind.MyBaseKeyword))
        Assert.Equal(SyntaxKind.MyClassExpression, SyntaxFacts.GetInstanceExpression(SyntaxKind.MyClassKeyword))
    End Sub

    <Fact>
    Public Sub GetKeywordkinds()
        Assert.NotEqual(Of Integer)(0, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetKeywordKinds))
        Assert.Contains(Of SyntaxKind)(SyntaxKind.CIntKeyword, SyntaxFacts.GetKeywordKinds)
    End Sub

    <Fact>
    Public Sub GetPreprocessorKeywordKind()
        Dim item As String
        For Each item In New String() {"if", "elseif", "else", "endif", "region", "end", "const", "externalsource", "externalchecksum"}
            Assert.NotEqual(SyntaxKind.None, SyntaxFacts.GetPreprocessorKeywordKind(item))
        Next
        Assert.Equal(SyntaxKind.ExternalSourceKeyword, SyntaxFacts.GetPreprocessorKeywordKind("externalsource"))
        Assert.Equal(SyntaxKind.EndKeyword, SyntaxFacts.GetPreprocessorKeywordKind("end"))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetPreprocessorKeywordKind(String.Empty))
        Assert.Equal(SyntaxKind.None, SyntaxFacts.GetPreprocessorKeywordKind("d"))
        Assert.Equal(Of Integer)(9, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetPreprocessorKeywordKinds))
    End Sub

    <Fact>
    Public Sub GetPreprocessorKeywordKinds()
        Assert.NotEqual(Of Integer)(0, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetPreprocessorKeywordKinds))
        Assert.Contains(Of SyntaxKind)(SyntaxKind.RegionKeyword, SyntaxFacts.GetPreprocessorKeywordKinds)
        Assert.DoesNotContain(Of SyntaxKind)(SyntaxKind.PublicKeyword, SyntaxFacts.GetPreprocessorKeywordKinds)
    End Sub

    <Fact>
    Public Sub GetPunctuationKinds()
        Assert.NotEqual(Of Integer)(0, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetPunctuationKinds))
        Assert.Contains(Of SyntaxKind)(SyntaxKind.ExclamationToken, SyntaxFacts.GetPunctuationKinds)
        Assert.Contains(Of SyntaxKind)(SyntaxKind.EmptyToken, SyntaxFacts.GetPunctuationKinds)
        Assert.DoesNotContain(Of SyntaxKind)(SyntaxKind.NumericLabel, SyntaxFacts.GetPunctuationKinds)
    End Sub

    <Fact>
    Public Sub GetReservedKeywordsKinds()
        Assert.NotEqual(Of Integer)(0, Enumerable.Count(Of SyntaxKind)(SyntaxFacts.GetReservedKeywordKinds))
        Assert.Contains(Of SyntaxKind)(SyntaxKind.AddressOfKeyword, SyntaxFacts.GetReservedKeywordKinds)
        Assert.DoesNotContain(Of SyntaxKind)(SyntaxKind.QualifiedName, SyntaxFacts.GetReservedKeywordKinds)
    End Sub

    <Fact>
    Public Sub IsAccessorStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement, SyntaxKind.AddHandlerAccessorStatement, SyntaxKind.RemoveHandlerAccessorStatement, SyntaxKind.RaiseEventAccessorStatement}
            Assert.True(SyntaxFacts.IsAccessorStatement(item))
        Next
        Assert.False(SyntaxFacts.IsAccessorStatement(SyntaxKind.SubKeyword))
        Assert.False(SyntaxFacts.IsAccessorStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsAccessorStatementKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.GetKeyword, SyntaxKind.SetKeyword, SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.RaiseEventKeyword}
            Assert.True(SyntaxFacts.IsAccessorStatementKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsAccessorStatementKeyword(SyntaxKind.SubKeyword))
        Assert.False(SyntaxFacts.IsAccessorStatementKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsAddRemoveHandlerStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AddHandlerStatement, SyntaxKind.RemoveHandlerStatement}
            Assert.True(SyntaxFacts.IsAddRemoveHandlerStatement(item))
        Next
        Assert.False(SyntaxFacts.IsAddRemoveHandlerStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsAddRemoveHandlerStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsAddRemoveHandlerStatementAddHandlerOrRemoveHandlerKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword}
            Assert.True(SyntaxFacts.IsAddRemoveHandlerStatementAddHandlerOrRemoveHandlerKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsAddRemoveHandlerStatementAddHandlerOrRemoveHandlerKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsAddRemoveHandlerStatementAddHandlerOrRemoveHandlerKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsaddressofOperand()
        Dim source =
          <compilation name="TestAddressof">
              <file name="a.vb">
                  <![CDATA[
#If Debug Then
#End If

Namespace NS1
    Module Module1
        Delegate Sub DelFoo(xx As Integer)
        Sub Foo(xx As Integer)
        End Sub

        Sub Main()
            Dim a1 = GetType(Integer)
            Dim d As DelFoo = AddressOf Foo
            d.Invoke(xx:=1)
            Dim Obj Gen As New genClass(Of Integer)
        End Sub
        <Obsolete>
        Sub OldMethod()
        End Sub
    End Module

    Class genClass(Of t)
    End Class

End Namespace
]]></file>
          </compilation>



        Dim tree = CompilationUtils.CreateCompilationWithMscorlib(source).SyntaxTrees.Item(0)
        Dim symNode = VerificationHelpers.FindNodeOrTokenByKind(tree, SyntaxKind.AddressOfExpression, 1).AsNode
        Assert.False(SyntaxFacts.IsAddressOfOperand(DirectCast(symNode, ExpressionSyntax)))
        Assert.False(SyntaxFacts.IsInvocationOrAddressOfOperand(DirectCast(symNode, ExpressionSyntax)))
        Assert.True(SyntaxFacts.IsAddressOfOperand(CType(symNode.ChildNodes(0), ExpressionSyntax)))
        Assert.True(SyntaxFacts.IsInvocationOrAddressOfOperand(CType(symNode.ChildNodes(0), ExpressionSyntax)))
        Assert.False(SyntaxFacts.IsInvoked(DirectCast(VerificationHelpers.FindNodeOrTokenByKind(tree, SyntaxKind.InvocationExpression, 1).AsNode, ExpressionSyntax)))

        symNode = VerificationHelpers.FindNodeOrTokenByKind(tree, SyntaxKind.InvocationExpression, 1).AsNode
        Assert.False(SyntaxFacts.IsInvoked(CType(symNode, ExpressionSyntax)))
        Assert.True(SyntaxFacts.IsInvoked(CType(symNode.ChildNodes(0), ExpressionSyntax)))
        symNode = VerificationHelpers.FindNodeOrTokenByKind(tree, SyntaxKind.Attribute, 1).AsNode
        Assert.False(SyntaxFacts.IsAttributeName(symNode))
        Assert.True(SyntaxFacts.IsAttributeName(symNode.ChildNodes(0)))
        symNode = VerificationHelpers.FindNodeOrTokenByKind(tree, SyntaxKind.Attribute, 1).AsNode
    End Sub

    <Fact>
    Public Sub IsAssignmentStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SimpleAssignmentStatement, SyntaxKind.MidAssignmentStatement, SyntaxKind.AddAssignmentStatement, SyntaxKind.SubtractAssignmentStatement, SyntaxKind.MultiplyAssignmentStatement, SyntaxKind.DivideAssignmentStatement, SyntaxKind.IntegerDivideAssignmentStatement, SyntaxKind.ExponentiateAssignmentStatement, SyntaxKind.LeftShiftAssignmentStatement, SyntaxKind.RightShiftAssignmentStatement, SyntaxKind.ConcatenateAssignmentStatement}
            Assert.True(SyntaxFacts.IsAssignmentStatement(item))
        Next
        Assert.False(SyntaxFacts.IsAssignmentStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsAssignmentStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsAssignmentStatementOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.EqualsToken, SyntaxKind.PlusEqualsToken, SyntaxKind.MinusEqualsToken, SyntaxKind.AsteriskEqualsToken, SyntaxKind.SlashEqualsToken, SyntaxKind.BackslashEqualsToken, SyntaxKind.CaretEqualsToken, SyntaxKind.LessThanLessThanEqualsToken, SyntaxKind.GreaterThanGreaterThanEqualsToken, SyntaxKind.AmpersandEqualsToken}
            Assert.True(SyntaxFacts.IsAssignmentStatementOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsAssignmentStatementOperatorToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsAssignmentStatementOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsAttributeTargetAttributeModifier()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AssemblyKeyword, SyntaxKind.ModuleKeyword}
            Assert.True(SyntaxFacts.IsAttributeTargetAttributeModifier(item))
        Next
        Assert.False(SyntaxFacts.IsAttributeTargetAttributeModifier(SyntaxKind.SubKeyword))
        Assert.False(SyntaxFacts.IsAttributeTargetAttributeModifier(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsBinaryExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, SyntaxKind.MultiplyExpression, SyntaxKind.DivideExpression, SyntaxKind.IntegerDivideExpression, SyntaxKind.ExponentiateExpression, SyntaxKind.LeftShiftExpression, SyntaxKind.RightShiftExpression, SyntaxKind.ConcatenateExpression, SyntaxKind.ModuloExpression, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.LessThanExpression, SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanExpression, SyntaxKind.IsExpression, SyntaxKind.IsNotExpression, SyntaxKind.LikeExpression, SyntaxKind.OrExpression, SyntaxKind.ExclusiveOrExpression, SyntaxKind.AndExpression, SyntaxKind.OrElseExpression, SyntaxKind.AndAlsoExpression}
            Assert.True(SyntaxFacts.IsBinaryExpression(item))
        Next
        Assert.False(SyntaxFacts.IsBinaryExpression(SyntaxKind.MinusToken))
        Assert.False(SyntaxFacts.IsBinaryExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsBinaryExpressionOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.AsteriskToken, SyntaxKind.SlashToken, SyntaxKind.BackslashToken, SyntaxKind.CaretToken, SyntaxKind.LessThanLessThanToken, SyntaxKind.GreaterThanGreaterThanToken, SyntaxKind.AmpersandToken, SyntaxKind.ModKeyword, SyntaxKind.EqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.LessThanToken, SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.GreaterThanToken, SyntaxKind.IsKeyword, SyntaxKind.IsNotKeyword, SyntaxKind.LikeKeyword, SyntaxKind.OrKeyword, SyntaxKind.XorKeyword, SyntaxKind.AndKeyword, SyntaxKind.OrElseKeyword, SyntaxKind.AndAlsoKeyword}
            Assert.True(SyntaxFacts.IsBinaryExpressionOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsBinaryExpressionOperatorToken(SyntaxKind.MinusEqualsToken))
        Assert.False(SyntaxFacts.IsBinaryExpressionOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsCaseBlock()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock}
            Assert.True(SyntaxFacts.IsCaseBlock(item))
        Next
        Assert.False(SyntaxFacts.IsCaseBlock(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsCaseBlock(SyntaxKind.None))
    End Sub


    <Fact>
    Public Sub IsCaseRelationalClause()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CaseEqualsClause, SyntaxKind.CaseNotEqualsClause, SyntaxKind.CaseLessThanClause, SyntaxKind.CaseLessThanOrEqualClause, SyntaxKind.CaseGreaterThanOrEqualClause, SyntaxKind.CaseGreaterThanClause}
            Assert.True(SyntaxFacts.IsCaseRelationalClause(item))
        Next
        Assert.False(SyntaxFacts.IsCaseRelationalClause(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsCaseRelationalClause(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsCaseRelationalClauseOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.EqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.LessThanToken, SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.GreaterThanToken}
            Assert.True(SyntaxFacts.IsCaseRelationalClauseOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsCaseRelationalClauseOperatorToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsCaseRelationalClauseOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsCaseStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CaseStatement, SyntaxKind.CaseElseStatement}
            Assert.True(SyntaxFacts.IsCaseStatement(item))
        Next
        Assert.False(SyntaxFacts.IsCaseStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsCaseStatement(SyntaxKind.None))
    End Sub


    <Fact>
    Public Sub IsContextualKeyword1()
        Assert.False(SyntaxFacts.IsContextualKeyword(SyntaxKind.GosubKeyword))
        Assert.True(SyntaxFacts.IsContextualKeyword(SyntaxKind.AggregateKeyword))
    End Sub

    <Fact>
    Public Sub IsReservedKeyword()
        Assert.False(SyntaxFacts.IsReservedKeyword(SyntaxKind.OrderByClause))
        Assert.True(SyntaxFacts.IsReservedKeyword(SyntaxKind.AddHandlerKeyword))
    End Sub

    <Fact>
    Public Sub IsContinueStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ContinueWhileStatement, SyntaxKind.ContinueDoStatement, SyntaxKind.ContinueForStatement}
            Assert.True(SyntaxFacts.IsContinueStatement(item))
        Next
        Assert.False(SyntaxFacts.IsContinueStatement(SyntaxKind.WithKeyword))
        Assert.False(SyntaxFacts.IsContinueStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsContinueStatementBlockKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.WhileKeyword, SyntaxKind.DoKeyword, SyntaxKind.ForKeyword}
            Assert.True(SyntaxFacts.IsContinueStatementBlockKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsContinueStatementBlockKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsContinueStatementBlockKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsDeclareStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement}
            Assert.True(SyntaxFacts.IsDeclareStatement(item))
        Next
        Assert.False(SyntaxFacts.IsDeclareStatement(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsDeclareStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsDeclareStatementCharsetKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AnsiKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.AutoKeyword}
            Assert.True(SyntaxFacts.IsDeclareStatementCharsetKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsDeclareStatementCharsetKeyword(SyntaxKind.FunctionKeyword))
        Assert.False(SyntaxFacts.IsDeclareStatementCharsetKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsDeclareStatementKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword}
            Assert.True(SyntaxFacts.IsDeclareStatementKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsDeclareStatementKeyword(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsDeclareStatementKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsDelegateStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement}
            Assert.True(SyntaxFacts.IsDelegateStatement(item))
        Next
        Assert.False(SyntaxFacts.IsDelegateStatement(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsDelegateStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsDelegateStatementKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword}
            Assert.True(SyntaxFacts.IsDelegateStatementKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsDelegateStatementKeyword(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsDelegateStatementKeyword(SyntaxKind.None))
    End Sub


    <Fact>
    Public Sub IsDoLoopBlock()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DoLoopTopTestBlock, SyntaxKind.DoLoopBottomTestBlock, SyntaxKind.DoLoopForeverBlock}
            Assert.True(SyntaxFacts.IsDoLoopBlock(item))
        Next
        Assert.False(SyntaxFacts.IsDoLoopBlock(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsDoLoopBlock(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsEndBlockStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.EndIfStatement, SyntaxKind.EndUsingStatement, SyntaxKind.EndWithStatement, SyntaxKind.EndSelectStatement, SyntaxKind.EndStructureStatement, SyntaxKind.EndEnumStatement, SyntaxKind.EndInterfaceStatement, SyntaxKind.EndClassStatement, SyntaxKind.EndModuleStatement, SyntaxKind.EndNamespaceStatement, SyntaxKind.EndSubStatement, SyntaxKind.EndFunctionStatement, SyntaxKind.EndGetStatement, SyntaxKind.EndSetStatement, SyntaxKind.EndPropertyStatement, SyntaxKind.EndOperatorStatement, SyntaxKind.EndEventStatement, SyntaxKind.EndAddHandlerStatement, SyntaxKind.EndRemoveHandlerStatement, SyntaxKind.EndRaiseEventStatement, SyntaxKind.EndWhileStatement, SyntaxKind.EndTryStatement, SyntaxKind.EndSyncLockStatement}
            Assert.True(SyntaxFacts.IsEndBlockStatement(item))
        Next
        Assert.False(SyntaxFacts.IsEndBlockStatement(SyntaxKind.AddHandlerStatement))
    End Sub

    <Fact>
    Public Sub IsEndBlockStatementBlockKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfKeyword, SyntaxKind.UsingKeyword, SyntaxKind.WithKeyword, SyntaxKind.SelectKeyword, SyntaxKind.StructureKeyword, SyntaxKind.EnumKeyword, SyntaxKind.InterfaceKeyword, SyntaxKind.ClassKeyword, SyntaxKind.ModuleKeyword, SyntaxKind.NamespaceKeyword, SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.GetKeyword, SyntaxKind.SetKeyword, SyntaxKind.PropertyKeyword, SyntaxKind.OperatorKeyword, SyntaxKind.EventKeyword, SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.RaiseEventKeyword, SyntaxKind.WhileKeyword, SyntaxKind.TryKeyword, SyntaxKind.SyncLockKeyword}
            Assert.True(SyntaxFacts.IsEndBlockStatementBlockKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsEndBlockStatementBlockKeyword(SyntaxKind.AddHandlerStatement))
        Assert.False(SyntaxFacts.IsEndBlockStatementBlockKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsExitStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ExitDoStatement, SyntaxKind.ExitForStatement, SyntaxKind.ExitSubStatement, SyntaxKind.ExitFunctionStatement, SyntaxKind.ExitOperatorStatement, SyntaxKind.ExitPropertyStatement, SyntaxKind.ExitTryStatement, SyntaxKind.ExitSelectStatement, SyntaxKind.ExitWhileStatement}
            Assert.True(SyntaxFacts.IsExitStatement(item))
        Next
        Assert.False(SyntaxFacts.IsExitStatement(SyntaxKind.WithKeyword))
        Assert.False(SyntaxFacts.IsExitStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsExitStatementBlockKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DoKeyword, SyntaxKind.ForKeyword, SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.OperatorKeyword, SyntaxKind.PropertyKeyword, SyntaxKind.TryKeyword, SyntaxKind.SelectKeyword, SyntaxKind.WhileKeyword}
            Assert.True(SyntaxFacts.IsExitStatementBlockKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsExitStatementBlockKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsExitStatementBlockKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsIfDirective()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia}
            Assert.True(SyntaxFacts.IsIfDirectiveTrivia(item))
        Next
        Assert.False(SyntaxFacts.IsIfDirectiveTrivia(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsIfDirectiveTrivia(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsIfDirectiveIfOrElseIfKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfKeyword, SyntaxKind.ElseIfKeyword}
            Assert.True(SyntaxFacts.IsIfDirectiveTriviaIfOrElseIfKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsIfDirectiveTriviaIfOrElseIfKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsIfDirectiveTriviaIfOrElseIfKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsIfPart()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfPart, SyntaxKind.ElseIfPart}
            Assert.True(SyntaxFacts.IsIfPart(item))
        Next
        Assert.False(SyntaxFacts.IsIfPart(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsIfPart(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsIfStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfStatement, SyntaxKind.ElseIfStatement}
            Assert.True(SyntaxFacts.IsIfStatement(item))
        Next
        Assert.False(SyntaxFacts.IsIfStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsIfStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsIfStatementIfOrElseIfKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IfKeyword, SyntaxKind.ElseIfKeyword}
            Assert.True(SyntaxFacts.IsIfStatementIfOrElseIfKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsIfStatementIfOrElseIfKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsIfStatementIfOrElseIfKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsInstanceExpression()
        Assert.True(SyntaxFacts.IsInstanceExpression(SyntaxKind.MeKeyword))
        Assert.True(SyntaxFacts.IsInstanceExpression(SyntaxKind.MyBaseKeyword))
        Assert.False(SyntaxFacts.IsInstanceExpression(SyntaxKind.REMKeyword))
    End Sub

    <Fact>
    Public Sub IsKeywordEventContainerKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.MyBaseKeyword, SyntaxKind.MeKeyword, SyntaxKind.MyClassKeyword}
            Assert.True(SyntaxFacts.IsKeywordEventContainerKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsKeywordEventContainerKeyword(SyntaxKind.SubKeyword))
        Assert.False(SyntaxFacts.IsKeywordEventContainerKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsKeywordKind()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AddHandlerKeyword, SyntaxKind.AddressOfKeyword, SyntaxKind.AliasKeyword, SyntaxKind.AndKeyword, SyntaxKind.AndAlsoKeyword, SyntaxKind.AsKeyword, SyntaxKind.BooleanKeyword, SyntaxKind.ByRefKeyword, SyntaxKind.ByteKeyword, SyntaxKind.ByValKeyword, SyntaxKind.CallKeyword, SyntaxKind.CaseKeyword, SyntaxKind.CatchKeyword, SyntaxKind.CBoolKeyword, SyntaxKind.CByteKeyword, SyntaxKind.CCharKeyword, SyntaxKind.CDateKeyword, SyntaxKind.CDecKeyword, SyntaxKind.CDblKeyword, SyntaxKind.CharKeyword, SyntaxKind.CIntKeyword, SyntaxKind.ClassKeyword, SyntaxKind.CLngKeyword, SyntaxKind.CObjKeyword, SyntaxKind.ConstKeyword, SyntaxKind.ReferenceKeyword, SyntaxKind.ContinueKeyword, SyntaxKind.CSByteKeyword, SyntaxKind.CShortKeyword, SyntaxKind.CSngKeyword, SyntaxKind.CStrKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.CUIntKeyword, SyntaxKind.CULngKeyword, SyntaxKind.CUShortKeyword, SyntaxKind.DateKeyword, SyntaxKind.DecimalKeyword, SyntaxKind.DeclareKeyword, SyntaxKind.DefaultKeyword, SyntaxKind.DelegateKeyword, SyntaxKind.DimKeyword, SyntaxKind.DirectCastKeyword, SyntaxKind.DoKeyword, SyntaxKind.DoubleKeyword, SyntaxKind.EachKeyword, SyntaxKind.ElseKeyword, SyntaxKind.ElseIfKeyword, SyntaxKind.EndKeyword, SyntaxKind.EnumKeyword, SyntaxKind.EraseKeyword, SyntaxKind.ErrorKeyword, SyntaxKind.EventKeyword, SyntaxKind.ExitKeyword, SyntaxKind.FalseKeyword, SyntaxKind.FinallyKeyword, SyntaxKind.ForKeyword, SyntaxKind.FriendKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.GetKeyword, SyntaxKind.GetTypeKeyword, SyntaxKind.GetXmlNamespaceKeyword, SyntaxKind.GlobalKeyword, SyntaxKind.GoToKeyword, SyntaxKind.HandlesKeyword, SyntaxKind.IfKeyword, SyntaxKind.ImplementsKeyword, SyntaxKind.ImportsKeyword, SyntaxKind.InKeyword, SyntaxKind.InheritsKeyword, SyntaxKind.IntegerKeyword, SyntaxKind.InterfaceKeyword, SyntaxKind.IsKeyword, SyntaxKind.IsNotKeyword, SyntaxKind.LetKeyword, SyntaxKind.LibKeyword, SyntaxKind.LikeKeyword, SyntaxKind.LongKeyword, SyntaxKind.LoopKeyword, SyntaxKind.MeKeyword, SyntaxKind.ModKeyword, SyntaxKind.ModuleKeyword, SyntaxKind.MustInheritKeyword, SyntaxKind.MustOverrideKeyword, SyntaxKind.MyBaseKeyword, SyntaxKind.MyClassKeyword, SyntaxKind.NamespaceKeyword, SyntaxKind.NarrowingKeyword, SyntaxKind.NextKeyword, SyntaxKind.NewKeyword, SyntaxKind.NotKeyword, SyntaxKind.NothingKeyword, SyntaxKind.NotInheritableKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.ObjectKeyword, SyntaxKind.OfKeyword, SyntaxKind.OnKeyword, SyntaxKind.OperatorKeyword, SyntaxKind.OptionKeyword, SyntaxKind.OptionalKeyword, SyntaxKind.OrKeyword, SyntaxKind.OrElseKeyword, SyntaxKind.OverloadsKeyword, SyntaxKind.OverridableKeyword, SyntaxKind.OverridesKeyword, SyntaxKind.ParamArrayKeyword, SyntaxKind.PartialKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.PropertyKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PublicKeyword, SyntaxKind.RaiseEventKeyword, SyntaxKind.ReadOnlyKeyword, SyntaxKind.ReDimKeyword, SyntaxKind.REMKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.ResumeKeyword, SyntaxKind.ReturnKeyword, SyntaxKind.SByteKeyword, SyntaxKind.SelectKeyword, SyntaxKind.SetKeyword, SyntaxKind.ShadowsKeyword, SyntaxKind.SharedKeyword, SyntaxKind.ShortKeyword, SyntaxKind.SingleKeyword, SyntaxKind.StaticKeyword, SyntaxKind.StepKeyword, SyntaxKind.StopKeyword, SyntaxKind.StringKeyword, SyntaxKind.StructureKeyword, SyntaxKind.SubKeyword, SyntaxKind.SyncLockKeyword, SyntaxKind.ThenKeyword, SyntaxKind.ThrowKeyword, SyntaxKind.ToKeyword, SyntaxKind.TrueKeyword, SyntaxKind.TryKeyword, SyntaxKind.TryCastKeyword, SyntaxKind.TypeOfKeyword, SyntaxKind.UIntegerKeyword, SyntaxKind.ULongKeyword, SyntaxKind.UShortKeyword, SyntaxKind.UsingKeyword, SyntaxKind.WhenKeyword, SyntaxKind.WhileKeyword, SyntaxKind.WideningKeyword, SyntaxKind.WithKeyword, SyntaxKind.WithEventsKeyword, SyntaxKind.WriteOnlyKeyword, SyntaxKind.XorKeyword, SyntaxKind.EndIfKeyword, SyntaxKind.GosubKeyword, SyntaxKind.VariantKeyword, SyntaxKind.WendKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.AllKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.AscendingKeyword, SyntaxKind.AssemblyKeyword, SyntaxKind.AutoKeyword, SyntaxKind.BinaryKeyword, SyntaxKind.ByKeyword, SyntaxKind.CompareKeyword, SyntaxKind.CustomKeyword, SyntaxKind.DescendingKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.EqualsKeyword, SyntaxKind.ExplicitKeyword, SyntaxKind.ExternalSourceKeyword, SyntaxKind.ExternalChecksumKeyword, SyntaxKind.FromKeyword, SyntaxKind.GroupKeyword, SyntaxKind.InferKeyword, SyntaxKind.IntoKeyword, SyntaxKind.IsFalseKeyword, SyntaxKind.IsTrueKeyword, SyntaxKind.JoinKeyword, SyntaxKind.KeyKeyword, SyntaxKind.MidKeyword, SyntaxKind.OffKeyword, SyntaxKind.OrderKeyword, SyntaxKind.OutKeyword, SyntaxKind.PreserveKeyword, SyntaxKind.RegionKeyword, SyntaxKind.SkipKeyword, SyntaxKind.StrictKeyword, SyntaxKind.TakeKeyword, SyntaxKind.TextKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.UntilKeyword, SyntaxKind.WhereKeyword, SyntaxKind.TypeKeyword, SyntaxKind.XmlKeyword}
            Assert.True(SyntaxFacts.IsKeywordKind(item))
        Next
        Assert.False(SyntaxFacts.IsKeywordKind(SyntaxKind.MinusEqualsToken))
        Assert.False(SyntaxFacts.IsKeywordKind(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsLabelStatementLabelToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IdentifierToken, SyntaxKind.IntegerLiteralToken}
            Assert.True(SyntaxFacts.IsLabelStatementLabelToken(item))
        Next
        Assert.False(SyntaxFacts.IsLabelStatementLabelToken(SyntaxKind.WithKeyword))
        Assert.False(SyntaxFacts.IsLabelStatementLabelToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsLambdaHeader()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader}
            Assert.True(SyntaxFacts.IsLambdaHeader(item))
        Next
        Assert.False(SyntaxFacts.IsLambdaHeader(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsLambdaHeader(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsLambdaHeaderKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword}
            Assert.True(SyntaxFacts.IsLambdaHeaderKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsLambdaHeaderKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsLambdaHeaderKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsLanguagePunctuation()
        Assert.True(SyntaxFacts.IsLanguagePunctuation(SyntaxKind.ExclamationToken))
        Assert.False(SyntaxFacts.IsLanguagePunctuation(SyntaxKind.ConstKeyword))
        Assert.False(SyntaxFacts.IsLanguagePunctuation(SyntaxKind.FromKeyword))
    End Sub

    <Fact>
    Public Sub IsLiteralExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CharacterLiteralExpression, SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression, SyntaxKind.NumericLiteralExpression, SyntaxKind.DateLiteralExpression, SyntaxKind.StringLiteralExpression, SyntaxKind.NothingLiteralExpression}
            Assert.True(SyntaxFacts.IsLiteralExpression(item))
        Next
        Assert.False(SyntaxFacts.IsLiteralExpression(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsLiteralExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsMemberAccessExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.DictionaryAccessExpression}
            Assert.True(SyntaxFacts.IsMemberAccessExpression(item))
        Next
        Assert.False(SyntaxFacts.IsMemberAccessExpression(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsMemberAccessExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsMemberAccessExpressionOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DotToken, SyntaxKind.ExclamationToken}
            Assert.True(SyntaxFacts.IsMemberAccessExpressionOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsMemberAccessExpressionOperatorToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsMemberAccessExpressionOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsmethodBlock()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubBlock, SyntaxKind.FunctionBlock}
            Assert.True(SyntaxFacts.IsMethodBlock(item))
        Next

        For Each item In New SyntaxKind() {SyntaxKind.ConstructorBlock, SyntaxKind.OperatorBlock, SyntaxKind.PropertyGetBlock, SyntaxKind.PropertySetBlock, SyntaxKind.AddHandlerBlock, SyntaxKind.RemoveHandlerBlock, SyntaxKind.RaiseEventBlock}
            Assert.False(SyntaxFacts.IsMethodBlock(item))
        Next

        For Each item In New SyntaxKind() {SyntaxKind.PropertyGetBlock, SyntaxKind.PropertySetBlock, SyntaxKind.AddHandlerBlock, SyntaxKind.RemoveHandlerBlock, SyntaxKind.RaiseEventBlock}
            Assert.True(SyntaxFacts.IsAccessorBlock(item))
        Next

        For Each item In New SyntaxKind() {SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock, SyntaxKind.OperatorBlock}
            Assert.False(SyntaxFacts.IsAccessorBlock(item))
        Next

        Assert.False(SyntaxFacts.IsMethodBlock(SyntaxKind.MultiLineIfBlock))
        Assert.False(SyntaxFacts.IsMethodBlock(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsMethodStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubStatement, SyntaxKind.FunctionStatement}
            Assert.True(SyntaxFacts.IsMethodStatement(item))
        Next
        Assert.False(SyntaxFacts.IsMethodStatement(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsMethodStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsMethodStatementKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword}
            Assert.True(SyntaxFacts.IsMethodStatementKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsMethodStatementKeyword(SyntaxKind.NamespaceBlock))
        Assert.False(SyntaxFacts.IsMethodStatementKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsMultiLineLambdaExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression}
            Assert.False(SyntaxFacts.IsMultiLineLambdaExpression(item))
        Next
        For Each item In New SyntaxKind() {SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression}
            Assert.True(SyntaxFacts.IsMultiLineLambdaExpression(item))
        Next

        Assert.False(SyntaxFacts.IsMultiLineLambdaExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsName()
        Assert.True(SyntaxFacts.IsName(SyntaxKind.IdentifierName))
        Assert.True(SyntaxFacts.IsName(SyntaxKind.GenericName))
        Assert.True(SyntaxFacts.IsName(SyntaxKind.QualifiedName))
        Assert.True(SyntaxFacts.IsName(SyntaxKind.GlobalName))
        Assert.False(SyntaxFacts.IsName(SyntaxKind.GlobalKeyword))
        Assert.False(SyntaxFacts.IsName(SyntaxKind.CommaToken))
        Assert.False(SyntaxFacts.IsName(SyntaxKind.FunctionKeyword))
    End Sub

    <Fact>
    Public Sub IsNamespaceDeclaration()
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.ClassStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.InterfaceStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.StructureStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.EnumStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.ModuleStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.NamespaceStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.DelegateFunctionStatement))
        Assert.True(SyntaxFacts.IsNamespaceMemberDeclaration(SyntaxKind.DelegateSubStatement))
        Assert.False(SyntaxFacts.IsName(SyntaxKind.FunctionStatement))
        Assert.False(SyntaxFacts.IsName(SyntaxKind.SubStatement))
    End Sub

    <Fact>
    Public Sub IsOnErrorGoToStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.OnErrorGoToZeroStatement, SyntaxKind.OnErrorGoToMinusOneStatement, SyntaxKind.OnErrorGoToLabelStatement}
            Assert.True(SyntaxFacts.IsOnErrorGoToStatement(item))
        Next
        Assert.False(SyntaxFacts.IsOnErrorGoToStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsOnErrorGoToStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsOperator()
        Assert.True(SyntaxFacts.IsOperator(SyntaxKind.AndKeyword))
        Assert.False(SyntaxFacts.IsOperator(SyntaxKind.ForKeyword))
    End Sub

    <Fact>
    Public Sub IsOperatorStatementOperator()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CTypeKeyword, SyntaxKind.IsTrueKeyword, SyntaxKind.IsFalseKeyword, SyntaxKind.NotKeyword, SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.AsteriskToken, SyntaxKind.SlashToken, SyntaxKind.CaretToken, SyntaxKind.BackslashToken, SyntaxKind.AmpersandToken, SyntaxKind.LessThanLessThanToken, SyntaxKind.GreaterThanGreaterThanToken, SyntaxKind.ModKeyword, SyntaxKind.OrKeyword, SyntaxKind.XorKeyword, SyntaxKind.AndKeyword, SyntaxKind.LikeKeyword, SyntaxKind.EqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.LessThanToken, SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.GreaterThanToken}
            Assert.True(SyntaxFacts.IsOperatorStatementOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsOperatorStatementOperatorToken(SyntaxKind.SubKeyword))
        Assert.False(SyntaxFacts.IsOperatorStatementOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsOptionStatementNameKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ExplicitKeyword, SyntaxKind.StrictKeyword, SyntaxKind.CompareKeyword, SyntaxKind.InferKeyword}
            Assert.True(SyntaxFacts.IsOptionStatementNameKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsOptionStatementNameKeyword(SyntaxKind.AddHandlerStatement))
        Assert.False(SyntaxFacts.IsOptionStatementNameKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsOrdering()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AscendingOrdering, SyntaxKind.DescendingOrdering}
            Assert.True(SyntaxFacts.IsOrdering(item))
        Next
        Assert.False(SyntaxFacts.IsOrdering(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsOrdering(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsOrderingAscendingOrDescendingKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.AscendingKeyword, SyntaxKind.DescendingKeyword}
            Assert.True(SyntaxFacts.IsOrderingAscendingOrDescendingKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsOrderingAscendingOrDescendingKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsOrderingAscendingOrDescendingKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPartitionClause()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SkipClause, SyntaxKind.TakeClause}
            Assert.True(SyntaxFacts.IsPartitionClause(item))
        Next
        Assert.False(SyntaxFacts.IsPartitionClause(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsPartitionClause(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPartitionClauseSkipOrTakeKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword}
            Assert.True(SyntaxFacts.IsPartitionClauseSkipOrTakeKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsPartitionClauseSkipOrTakeKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsPartitionClauseSkipOrTakeKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPartitionWhileClause()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SkipWhileClause, SyntaxKind.TakeWhileClause}
            Assert.True(SyntaxFacts.IsPartitionWhileClause(item))
        Next
        Assert.False(SyntaxFacts.IsPartitionWhileClause(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsPartitionWhileClause(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPartitionWhileClauseSkipOrTakeKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword}
            Assert.True(SyntaxFacts.IsPartitionWhileClauseSkipOrTakeKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsPartitionWhileClauseSkipOrTakeKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsPartitionWhileClauseSkipOrTakeKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPredefinedCastExpressionKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.CObjKeyword, SyntaxKind.CBoolKeyword, SyntaxKind.CDateKeyword, SyntaxKind.CCharKeyword, SyntaxKind.CStrKeyword, SyntaxKind.CDecKeyword, SyntaxKind.CByteKeyword, SyntaxKind.CSByteKeyword, SyntaxKind.CUShortKeyword, SyntaxKind.CShortKeyword, SyntaxKind.CUIntKeyword, SyntaxKind.CIntKeyword, SyntaxKind.CULngKeyword, SyntaxKind.CLngKeyword, SyntaxKind.CSngKeyword, SyntaxKind.CDblKeyword}
            Assert.True(SyntaxFacts.IsPredefinedCastExpressionKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsPredefinedCastExpressionKeyword(SyntaxKind.MinusToken))
        Assert.False(SyntaxFacts.IsPredefinedCastExpressionKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPredefinedType()
        Assert.True(SyntaxFacts.IsPredefinedType(SyntaxKind.IntegerKeyword))
        Assert.True(SyntaxFacts.IsPredefinedType(SyntaxKind.ObjectKeyword))
        Assert.False(SyntaxFacts.IsPredefinedType(SyntaxKind.NothingKeyword))
    End Sub

    <Fact>
    Public Sub IsPreprocessorDirective()
        Assert.True(SyntaxFacts.IsPreprocessorDirective(SyntaxKind.IfDirectiveTrivia))
        Assert.False(SyntaxFacts.IsPreprocessorDirective(SyntaxKind.IfKeyword))
    End Sub

    <Fact>
    Public Sub IsPreProcessorKeyword()
        Assert.True(SyntaxFacts.IsPreprocessorKeyword(SyntaxKind.ExternalSourceKeyword))
        Assert.True(SyntaxFacts.IsPreprocessorKeyword(SyntaxKind.IfKeyword))
        Assert.False(SyntaxFacts.IsPreprocessorKeyword(SyntaxKind.FromKeyword))
    End Sub

    <Fact>
    Public Sub IsPreProcessorPunctuation()
        Assert.True(SyntaxFacts.IsPreprocessorPunctuation(SyntaxKind.HashToken))
        Assert.False(SyntaxFacts.IsPreprocessorPunctuation(SyntaxKind.DotToken))
        Assert.False(SyntaxFacts.IsPreprocessorPunctuation(SyntaxKind.AmpersandToken))
    End Sub

    <Fact>
    Public Sub IsPunctuation()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ExclamationToken, SyntaxKind.AtToken, SyntaxKind.CommaToken, SyntaxKind.HashToken, SyntaxKind.AmpersandToken, SyntaxKind.SingleQuoteToken, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken, SyntaxKind.SemicolonToken, SyntaxKind.AsteriskToken, SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.DotToken, SyntaxKind.SlashToken, SyntaxKind.ColonToken, SyntaxKind.LessThanToken, SyntaxKind.LessThanEqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.EqualsToken, SyntaxKind.GreaterThanToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.BackslashToken, SyntaxKind.CaretToken, SyntaxKind.ColonEqualsToken, SyntaxKind.AmpersandEqualsToken, SyntaxKind.AsteriskEqualsToken, SyntaxKind.PlusEqualsToken, SyntaxKind.MinusEqualsToken, SyntaxKind.SlashEqualsToken, SyntaxKind.BackslashEqualsToken, SyntaxKind.CaretEqualsToken, SyntaxKind.LessThanLessThanToken, SyntaxKind.GreaterThanGreaterThanToken, SyntaxKind.LessThanLessThanEqualsToken, SyntaxKind.GreaterThanGreaterThanEqualsToken, SyntaxKind.QuestionToken, SyntaxKind.DoubleQuoteToken, SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken, SyntaxKind.EmptyToken, SyntaxKind.SlashGreaterThanToken, SyntaxKind.LessThanSlashToken, SyntaxKind.LessThanExclamationMinusMinusToken, SyntaxKind.MinusMinusGreaterThanToken, SyntaxKind.LessThanQuestionToken, SyntaxKind.QuestionGreaterThanToken, SyntaxKind.LessThanPercentEqualsToken, SyntaxKind.PercentGreaterThanToken, SyntaxKind.BeginCDataToken, SyntaxKind.EndCDataToken, SyntaxKind.EndOfXmlToken}
            Assert.True(SyntaxFacts.IsPunctuation(item))
        Next
        Assert.False(SyntaxFacts.IsPunctuation(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsPunctuation(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsPunctuationOrKeyword()
        Assert.True(SyntaxFacts.IsPunctuationOrKeyword(SyntaxKind.AddHandlerKeyword))
        Assert.True(SyntaxFacts.IsPunctuationOrKeyword(SyntaxKind.EndOfXmlToken))
        Assert.False(SyntaxFacts.IsPunctuationOrKeyword(SyntaxKind.XmlNameToken))
        Assert.False(SyntaxFacts.IsPunctuationOrKeyword(SyntaxKind.AliasImportsClause))
        Assert.False(SyntaxFacts.IsPunctuationOrKeyword(SyntaxKind.ForStatement))
    End Sub

    <Fact>
    Public Sub IsReDimStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ReDimStatement, SyntaxKind.ReDimPreserveStatement}
            Assert.True(SyntaxFacts.IsReDimStatement(item))
        Next
        Assert.False(SyntaxFacts.IsReDimStatement(SyntaxKind.SimpleAssignmentStatement))
        Assert.False(SyntaxFacts.IsReDimStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsRelationalOperator()
        Assert.True(SyntaxFacts.IsRelationalOperator(SyntaxKind.LessThanToken))
        Assert.False(SyntaxFacts.IsRelationalOperator(SyntaxKind.DotToken))
    End Sub

    <Fact>
    Public Sub IsReservedKeyword1()
        Dim VB1 As New SyntaxToken
        Assert.False(VB1.IsReservedKeyword())
        Assert.True(SyntaxFacts.IsReservedKeyword(SyntaxKind.AddHandlerKeyword))
    End Sub

    <Fact>
    Public Sub IsResumeStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.ResumeStatement, SyntaxKind.ResumeLabelStatement, SyntaxKind.ResumeNextStatement}
            Assert.True(SyntaxFacts.IsResumeStatement(item))
        Next
        Assert.False(SyntaxFacts.IsResumeStatement(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsResumeStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsSingleLineLambdaExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression}
            Assert.True(SyntaxFacts.IsSingleLineLambdaExpression(item))
        Next
        Assert.False(SyntaxFacts.IsSingleLineLambdaExpression(SyntaxKind.MinusToken))
        Assert.False(SyntaxFacts.IsSingleLineLambdaExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsSpecialConstraint()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.NewConstraint, SyntaxKind.ClassConstraint, SyntaxKind.StructureConstraint}
            Assert.True(SyntaxFacts.IsSpecialConstraint(item))
        Next
        Assert.False(SyntaxFacts.IsSpecialConstraint(SyntaxKind.ConstDirectiveTrivia))
        Assert.False(SyntaxFacts.IsSpecialConstraint(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsSpecialConstraintKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.NewKeyword, SyntaxKind.ClassKeyword, SyntaxKind.StructureKeyword}
            Assert.True(SyntaxFacts.IsSpecialConstraintConstraintKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsSpecialConstraintConstraintKeyword(SyntaxKind.ModuleKeyword))
        Assert.False(SyntaxFacts.IsSpecialConstraintConstraintKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsStopOrEndStatement()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.StopStatement, SyntaxKind.EndStatement}
            Assert.True(SyntaxFacts.IsStopOrEndStatement(item))
        Next
        Assert.False(SyntaxFacts.IsStopOrEndStatement(SyntaxKind.WithKeyword))
        Assert.False(SyntaxFacts.IsStopOrEndStatement(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsStopOrEndStatementStopOrEndKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.StopKeyword, SyntaxKind.EndKeyword}
            Assert.True(SyntaxFacts.IsStopOrEndStatementStopOrEndKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsStopOrEndStatementStopOrEndKeyword(SyntaxKind.WithKeyword))
        Assert.False(SyntaxFacts.IsStopOrEndStatementStopOrEndKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsToken()
        Assert.True(SyntaxFacts.IsAnyToken(SyntaxKind.AddHandlerKeyword))
        Assert.True(SyntaxFacts.IsAnyToken(SyntaxKind.CharacterLiteralToken))
        Assert.False(SyntaxFacts.IsAnyToken(SyntaxKind.GlobalName))
        Assert.False(SyntaxFacts.IsAnyToken(SyntaxKind.DocumentationCommentTrivia))
    End Sub

    <Fact>
    Public Sub IsTrivia()
        Assert.True(SyntaxFacts.IsTrivia(SyntaxKind.WhitespaceTrivia))
        Assert.False(SyntaxFacts.IsTrivia(SyntaxKind.REMKeyword))
    End Sub

    <Fact>
    Public Sub IsTypeOfExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.TypeOfIsExpression, SyntaxKind.TypeOfIsNotExpression}
            Assert.True(SyntaxFacts.IsTypeOfExpression(item))
        Next
        Assert.False(SyntaxFacts.IsTypeOfExpression(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsTypeOfExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsTypeOfExpressionOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.IsKeyword, SyntaxKind.IsNotKeyword}
            Assert.True(SyntaxFacts.IsTypeOfExpressionOperatorToken(item))
        Next
        Assert.False(SyntaxFacts.IsTypeOfExpressionOperatorToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsTypeOfExpressionOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsTypeParameterVarianceKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.InKeyword, SyntaxKind.OutKeyword}
            Assert.True(SyntaxFacts.IsTypeParameterVarianceKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsTypeParameterVarianceKeyword(SyntaxKind.GetKeyword))
        Assert.False(SyntaxFacts.IsTypeParameterVarianceKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsUnaryExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression, SyntaxKind.NotExpression, SyntaxKind.AddressOfExpression}
            Assert.True(SyntaxFacts.IsUnaryExpression(item))
        Next
        Assert.False(SyntaxFacts.IsUnaryExpression(SyntaxKind.MinusToken))
        Assert.False(SyntaxFacts.IsUnaryExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsUnaryExpressionOperatorToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.NotKeyword, SyntaxKind.AddressOfKeyword}
            Assert.True(SyntaxFacts.IsUnaryExpressionOperatorToken(item))
        Next

        Assert.False(SyntaxFacts.IsUnaryExpressionOperatorToken(SyntaxKind.MinusEqualsToken))
        Assert.False(SyntaxFacts.IsUnaryExpressionOperatorToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsWhileUntilClause()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.WhileClause, SyntaxKind.UntilClause}
            Assert.True(SyntaxFacts.IsWhileUntilClause(item))
        Next
        Assert.False(SyntaxFacts.IsWhileUntilClause(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsWhileUntilClause(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsWhileUntilClauseWhileOrUntilKeyword()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.WhileKeyword, SyntaxKind.UntilKeyword}
            Assert.True(SyntaxFacts.IsWhileUntilClauseWhileOrUntilKeyword(item))
        Next
        Assert.False(SyntaxFacts.IsWhileUntilClauseWhileOrUntilKeyword(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsWhileUntilClauseWhileOrUntilKeyword(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsXmlMemberAccessExpression()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.XmlElementAccessExpression, SyntaxKind.XmlDescendantAccessExpression, SyntaxKind.XmlAttributeAccessExpression}
            Assert.True(SyntaxFacts.IsXmlMemberAccessExpression(item))
        Next
        Assert.False(SyntaxFacts.IsXmlMemberAccessExpression(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsXmlMemberAccessExpression(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsXmlMemberAccessExpressionToken2()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DotToken, SyntaxKind.AtToken}
            Assert.True(SyntaxFacts.IsXmlMemberAccessExpressionToken2(item))
        Next
        Assert.False(SyntaxFacts.IsXmlMemberAccessExpressionToken2(SyntaxKind.MinusToken))
        Assert.False(SyntaxFacts.IsXmlMemberAccessExpressionToken2(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsXmlStringEndQuoteToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DoubleQuoteToken, SyntaxKind.SingleQuoteToken}
            Assert.True(SyntaxFacts.IsXmlStringEndQuoteToken(item))
        Next
        Assert.False(SyntaxFacts.IsXmlStringEndQuoteToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsXmlStringEndQuoteToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsXmlStringStartQuoteToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.DoubleQuoteToken, SyntaxKind.SingleQuoteToken}
            Assert.True(SyntaxFacts.IsXmlStringStartQuoteToken(item))
        Next
        Assert.False(SyntaxFacts.IsXmlStringStartQuoteToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsXmlStringStartQuoteToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub IsXmlTextToken()
        Dim item As SyntaxKind
        For Each item In New SyntaxKind() {SyntaxKind.XmlTextLiteralToken, SyntaxKind.XmlEntityLiteralToken, SyntaxKind.DocumentationCommentLineBreakToken}
            Assert.True(SyntaxFacts.IsXmlTextToken(item))
        Next
        Assert.False(SyntaxFacts.IsXmlTextToken(SyntaxKind.ExitKeyword))
        Assert.False(SyntaxFacts.IsXmlTextToken(SyntaxKind.None))
    End Sub

    <Fact>
    Public Sub VarianceKindFromToken()
        Dim keywordToken As SyntaxToken = SyntaxFactory.Token(SyntaxKind.InKeyword, text:=Nothing)
        Assert.Equal(VarianceKind.In, SyntaxFacts.VarianceKindFromToken(keywordToken))
        keywordToken = SyntaxFactory.Token(SyntaxKind.OutKeyword, text:=Nothing)
        Assert.Equal(VarianceKind.Out, SyntaxFacts.VarianceKindFromToken(keywordToken))
        keywordToken = SyntaxFactory.Token(SyntaxKind.FriendKeyword, text:=Nothing)
        Assert.Equal(VarianceKind.None, SyntaxFacts.VarianceKindFromToken(keywordToken))
    End Sub

    <Fact>
    Sub AllowsLeadingOrTrailingImplicitLineContinuation()

        Dim cu = SyntaxFactory.ParseCompilationUnit(My.Resources.Resource.VBAllInOne)

        Assert.False(cu.ContainsDiagnostics, "Baseline has diagnostics.")

        Dim tokens = cu.DescendantTokens(descendIntoTrivia:=False)

        Dim builder As New System.Text.StringBuilder(cu.FullSpan.Length * 2)

        Const explicitLineContinuation = " _" & vbCrLf

        ' For every token in the file 
        ' If there is explicit continuation that can be removed, remove it
        ' If there is implicit continuation, assert that it's allowed
        ' Otherwise if there is no continuation, add an explicit line continuation 
        Using enumerator = tokens.GetEnumerator()
            If Not enumerator.MoveNext() Then Return

            Dim currentToken = enumerator.Current
            Dim nextToken = enumerator.Current

            Do While enumerator.MoveNext()
                currentToken = nextToken
                nextToken = enumerator.Current

                If currentToken = currentToken.Parent.AncestorsAndSelf.OfType(Of StatementSyntax).First.GetLastToken() OrElse
                   nextToken.VisualBasicKind = SyntaxKind.EndOfFileToken Then
                    builder.Append(currentToken.ToFullString())
                    Continue Do
                End If

                If currentToken.HasLeadingTrivia Then
                    For Each trivia In currentToken.LeadingTrivia
                        builder.Append(trivia.ToFullString())
                    Next
                End If

                builder.Append(currentToken.ToString())

                If currentToken.HasTrailingTrivia Then
                    Dim hasContinuation = False

                    For Each trivia In currentToken.TrailingTrivia

                        If trivia.VisualBasicKind = SyntaxKind.LineContinuationTrivia Then

                            If SyntaxFacts.AllowsTrailingImplicitLineContinuation(currentToken) OrElse
                               SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken) Then

                                builder.Append(vbCrLf)

                            Else
                                builder.Append(trivia.ToFullString())

                            End If

                            hasContinuation = True
                        ElseIf trivia.VisualBasicKind = SyntaxKind.EndOfLineTrivia Then
                            If Not hasContinuation Then
                                hasContinuation = True
                                builder.Append(trivia.ToFullString())
                            End If

                        Else
                            builder.Append(trivia.ToFullString())
                        End If
                    Next

                    If Not hasContinuation AndAlso
                       currentToken <> currentToken.Parent.AncestorsAndSelf.OfType(Of StatementSyntax).First.GetLastToken() AndAlso
                       nextToken.VisualBasicKind <> SyntaxKind.EndOfFileToken Then

                        If SyntaxFacts.AllowsTrailingImplicitLineContinuation(currentToken) OrElse
                           SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken) Then

                            builder.Append(vbCrLf)

                            ' These tokens appear in XML literals, explicit line continuation is illegal in these contexts.
                        ElseIf currentToken.VisualBasicKind <> SyntaxKind.XmlKeyword AndAlso
                               currentToken.VisualBasicKind <> SyntaxKind.XmlNameToken AndAlso
                               currentToken.VisualBasicKind <> SyntaxKind.DoubleQuoteToken AndAlso
                               currentToken.VisualBasicKind <> SyntaxKind.XmlTextLiteralToken Then

                            builder.Append(explicitLineContinuation)
                        End If
                    End If
                End If
            Loop
        End Using

        cu = SyntaxFactory.ParseCompilationUnit(builder.ToString())

        Assert.False(cu.ContainsDiagnostics, "Transformed tree has diagnostics.")

    End Sub

    <Fact>
    Sub AllowsLeadingOrTrailingImplicitLineContinuationNegativeTests()

        Dim cu = SyntaxFactory.ParseCompilationUnit(My.Resources.Resource.VBAllInOne)

        Assert.False(cu.ContainsDiagnostics, "Baseline has diagnostics.")

        Dim tokens = cu.DescendantTokens(descendIntoTrivia:=False)

        Dim checked As New HashSet(Of Tuple(Of SyntaxKind, SyntaxKind))

        ' For every token in the file
        ' If implicit line continuation is not allowed after it or before the next token add one and
        ' verify a parse error is reported.
        Using enumerator = tokens.GetEnumerator()
            If Not enumerator.MoveNext() Then Return

            Dim currentToken = enumerator.Current
            Dim nextToken = enumerator.Current

            Do While enumerator.MoveNext()
                currentToken = nextToken
                nextToken = enumerator.Current

                ' Tokens for which adding trailing newline does nothing or
                ' creates a new text which could parse differently but valid code.
                If currentToken.TrailingTrivia.Any(Function(t)
                                                       Return t.VisualBasicKind = SyntaxKind.ColonTrivia OrElse t.VisualBasicKind = SyntaxKind.EndOfLineTrivia
                                                   End Function) OrElse
                   currentToken.VisualBasicKind = SyntaxKind.ColonToken OrElse
                   currentToken.VisualBasicKind = SyntaxKind.NextKeyword OrElse
                   nextToken.VisualBasicKind = SyntaxKind.DotToken OrElse
                   nextToken.VisualBasicKind = SyntaxKind.ColonToken OrElse
                   nextToken.VisualBasicKind = SyntaxKind.EndOfFileToken Then
                    Continue Do
                End If

                Dim kindAndParentKind = Tuple.Create(currentToken.VisualBasicKind(), currentToken.Parent.VisualBasicKind())

                If checked.Contains(kindAndParentKind) Then Continue Do

                If Not (SyntaxFacts.AllowsTrailingImplicitLineContinuation(currentToken) OrElse
                        SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken)) Then

                    Dim newTrailing = Aggregate trivia In currentToken.TrailingTrivia
                                      Where trivia.VisualBasicKind <> SyntaxKind.EndOfLineTrivia
                                      Into ToList()

                    newTrailing.Add(SyntaxFactory.EndOfLineTrivia(vbCrLf))

                    Assert.True(SyntaxFactory.ParseCompilationUnit(cu.ReplaceToken(currentToken, currentToken.WithTrailingTrivia(newTrailing)).ToFullString()).ContainsDiagnostics,
                                "Expected diagnostic when adding line continuation to " & currentToken.VisualBasicKind.ToString() & " in " & currentToken.Parent.ToString() & ".")

                    checked.Add(kindAndParentKind)
                End If
            Loop
        End Using

    End Sub

    <WorkItem(531480, "DevDiv")>
    <Fact>
    Public Sub ImplicitLineContinuationAfterQuery()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If Nothing Is From c In "" Distinct Then
        End If
        If Nothing Is From c In "" Order By c Ascending Then
        End If
        If Nothing Is From c In "" Order By c Descending Then
        End If
    End Sub
End Module
]]>)
        Dim tokens = tree.GetRoot().DescendantTokens().ToArray()
        Dim index = 0
        For Each token In tokens
            If token.VisualBasicKind = SyntaxKind.ThenKeyword Then
                Dim prevToken = tokens(index - 1)
                Dim nextToken = tokens(index)
                Assert.False(SyntaxFacts.AllowsTrailingImplicitLineContinuation(prevToken))
                Assert.False(SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken))
            End If
            index += 1
        Next
    End Sub

    <WorkItem(530665, "DevDiv")>
    <Fact>
    Public Sub ImplicitLineContinuationAfterDictionaryAccessOperator()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System.Collections
 
Module Program
    Sub Main()
        Dim x As New Hashtable
        Dim y = x ! _
        Foo
    End Sub
End Module
]]>)

        Dim memberAccess = tree.GetRoot().DescendantNodes().OfType(Of MemberAccessExpressionSyntax).Single()

        Assert.False(SyntaxFacts.AllowsLeadingImplicitLineContinuation(memberAccess.Name.Identifier))
        Assert.False(SyntaxFacts.AllowsTrailingImplicitLineContinuation(memberAccess.OperatorToken))

    End Sub

End Class