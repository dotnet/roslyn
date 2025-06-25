' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class SpeculativeSemanticModelTests
        <Fact>
        Public Sub SyntaxTreeCreateAcceptsAnySyntaxNode()
            Dim node As VisualBasicSyntaxNode = SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.SimpleImportsClause(SyntaxFactory.IdentifierName("Blah"))))
            Dim tree = VisualBasicSyntaxTree.Create(node)
            CheckTree(tree)
        End Sub

        <Fact>
        Public Sub SyntaxTreeCreateWithoutCloneAcceptsAnySyntaxNode()
            Dim node As VisualBasicSyntaxNode = SyntaxFactory.CatchStatement(SyntaxFactory.IdentifierName("Goo"), SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName(GetType(InvalidOperationException).Name)), Nothing)
            Dim tree = VisualBasicSyntaxTree.CreateWithoutClone(node, VisualBasicParseOptions.Default)
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
            Assert.Throws(Of InvalidCastException)(
                Sub()
                    Dim _ignored = DirectCast(DirectCast(tree.GetCompilationUnitRoot(), [Object]), VisualBasicSyntaxTree)
                End Sub)

            Assert.Throws(Of ArgumentNullException)(
                Sub()
                    tree.GetDiagnostics(DirectCast(Nothing, VisualBasicSyntaxNode))
                End Sub)

            Assert.Throws(Of InvalidOperationException)(
                Sub()
                    Dim token As SyntaxToken = Nothing
                    tree.GetDiagnostics(token)
                End Sub)

            Assert.Throws(Of ArgumentNullException)(
                Sub()
                    tree.GetDiagnostics(DirectCast(Nothing, SyntaxNode))
                End Sub)

            Assert.Throws(Of InvalidOperationException)(
                Sub()
                    Dim trivia As SyntaxTrivia = Nothing
                    tree.GetDiagnostics(trivia)
                End Sub)
        End Sub
    End Class
End Namespace
