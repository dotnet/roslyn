' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class SpeculativeSemanticModelTests
        Inherits SpeculativeSemanticModelTestsBase

        <Fact>
        Public Sub SyntaxTreeCreateAcceptsAnySyntaxNode()
            Dim node As VisualBasicSyntaxNode = SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.SimpleImportsClause(SyntaxFactory.IdentifierName("Blah"))))
            Dim tree = VisualBasicSyntaxTree.Create(node)
            CheckTree(tree)
        End Sub

        <Fact>
        Public Sub SyntaxTreeCreateWithoutCloneAcceptsAnySyntaxNode()
            Dim node As VisualBasicSyntaxNode = SyntaxFactory.CatchStatement(SyntaxFactory.IdentifierName("Goo"), SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName(GetType(InvalidOperationException).Name)), Nothing)
            Dim tree = VisualBasicSyntaxTree.CreateWithoutClone(node)
            CheckTree(tree)
        End Sub

        <Fact>
        Public Sub SyntaxTreeHasCompilationUnitRootReturnsTrueForFullDocument()
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText("Module Module1 _ Sub Main() _ System.Console.WriteLine(""Wah"") _ End Sub _ End Module")
            Assert.Equal(True, tree.HasCompilationUnitRoot)
            Assert.Equal(GetType(CompilationUnitSyntax), tree.GetRoot().GetType())
        End Sub

        <Fact>
        Public Sub SyntaxTreeHasCompilationUnitRootReturnsFalseForArbitrarilyRootedTree()
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.Create(SyntaxFactory.FromClause(SyntaxFactory.CollectionRangeVariable(SyntaxFactory.ModifiedIdentifier("Nay"), SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal(823)))))
            Dim root As SyntaxNode = Nothing
            Assert.Equal(True, tree.TryGetRoot(root))
            Assert.Equal(False, tree.HasCompilationUnitRoot)
            Assert.NotEqual(GetType(CompilationUnitSyntax), root.GetType())
        End Sub

        <Fact>
        Public Sub CompilationDoesNotAcceptArbitrarilyRootedTree()
            Dim arbitraryTree = VisualBasicSyntaxTree.Create(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Wooh")))
            Dim parsedTree = VisualBasicSyntaxTree.ParseText("Class TheClass _ End Class")
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.Create("Grrr", syntaxTrees:={arbitraryTree}))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("Wah").AddSyntaxTrees(arbitraryTree))
            Assert.Throws(Of ArgumentException)(Sub() VisualBasicCompilation.Create("Bah", syntaxTrees:={parsedTree}).ReplaceSyntaxTree(parsedTree, arbitraryTree))
            'FIXME: Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.Create("Woo").GetSemanticModel(tree))
        End Sub

        <Fact>
        Public Sub SyntaxNodeSyntaxTreeIsEmptyWhenCreatingUnboundNode()
            Dim node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3))
            Dim syntaxTreeField = GetType(VisualBasicSyntaxNode).GetFields(BindingFlags.NonPublic Or BindingFlags.Instance).Single(Function(f) f.FieldType Is GetType(SyntaxTree))
            Assert.Equal(Nothing, syntaxTreeField.GetValue(node))
        End Sub

        <Fact>
        Public Sub SyntaxNodeSyntaxTreeIsIdenticalOnSubsequentGets()
            Dim node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3))
            Dim tree = node.SyntaxTree
            Assert.Equal(tree, node.SyntaxTree)
        End Sub

        <Fact>
        Public Sub SyntaxNodeSyntaxTreeReturnsParentsSyntaxTree()
            Dim node = SyntaxFactory.UnaryMinusExpression( _
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3)))
            Dim childTree = node.Operand.SyntaxTree
            Dim parentTree = node.SyntaxTree
            ' Don't inline these variables - order of evaluation and initialization would change
            Assert.Equal(parentTree, childTree)
        End Sub

        <Fact>
        Public Sub SyntaxNodeSyntaxTreeReturnsOriginalSyntaxTree()
            Dim tree = VisualBasicSyntaxTree.ParseText("class TheClass { }")
            Assert.Equal(tree, tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)().Single().SyntaxTree)
        End Sub

        Private Sub CheckTree(tree As SyntaxTree)
#If False Then ' https://github.com/dotnet/roslyn/issues/4453
            CheckAllMembers(
                tree,
                New Dictionary(Of Type, Func(Of Object)) From
                {
                    {GetType(SyntaxTree), Function() tree},
                    {GetType(VisualBasicSyntaxTree), Function() tree},
                    {GetType(TextSpan), Function() TextSpan.FromBounds(0, 0)},
                    {GetType(SourceText), Function() New StringText("Module Module1 _ End Module")},
                    {GetType(SyntaxNodeOrToken), Function() New SyntaxNodeOrToken(tree.GetRoot())},
                    {GetType(SyntaxNodeOrToken), Function() New SyntaxNodeOrToken(tree.GetRoot())}
                },
                New Dictionary(Of MemberInfo, Type) From
                {
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetCompilationUnitRoot"), GetType(InvalidCastException)},
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetDiagnostics", {GetType(VisualBasicSyntaxNode)}), GetType(ArgumentNullException)},
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetDiagnostics", {GetType(SyntaxToken)}), GetType(InvalidOperationException)},
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetDiagnostics", {GetType(SyntaxTrivia)}), GetType(InvalidOperationException)},
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetDiagnostics", {GetType(SyntaxNode)}), GetType(ArgumentNullException)},
                    {GetType(VisualBasicSyntaxTree).GetMethod("GetDiagnostics", {GetType(SyntaxToken)}), GetType(InvalidOperationException)}
                })
#End If
        End Sub
    End Class
End Namespace
