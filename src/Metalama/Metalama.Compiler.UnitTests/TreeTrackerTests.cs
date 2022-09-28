using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Metalama.Compiler.UnitTests
{
    public class TreeTrackerTests
    {
        [Fact]
        public void CanFindOmittedArraySizeExpression()
        {
            var type = SyntaxFactory.ParseTypeName("int[]");

            var omittedExpression = type.DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Single();

            Assert.Same(omittedExpression, type.FindNodeByPosition(omittedExpression.RawKind, omittedExpression.FullSpan, findInsideTrivia: false));
        }

        [Fact]
        public void ModifiedBlockSyntax()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ int i; }");

            var block = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            block = block.AddStatements(SyntaxFactory.ParseStatement("i++;"));

            Assert.Same(originalBlock, TreeTracker.GetSourceSyntaxNode(block));
            Assert.Same(originalBlock.Statements[0], TreeTracker.GetSourceSyntaxNode(block.Statements[0]));
            Assert.Null(TreeTracker.GetSourceSyntaxNode(block.Statements[1]));
        }

        [Fact]
        public void ReplacedBlockSyntax()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ int i; }");

            var block = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            block = SyntaxFactory.Block(block.Statements.Append(SyntaxFactory.ParseStatement("i++;")));

            Assert.Same(block, TreeTracker.GetSourceSyntaxNode(block));
            Assert.Same(originalBlock.Statements[0], TreeTracker.GetSourceSyntaxNode(block.Statements[0]));
            Assert.Same(block.Statements[1], TreeTracker.GetSourceSyntaxNode(block.Statements[1]));
        }

        [Fact]
        public void Cref()
        {
            var originalCompilationUnit = SyntaxFactory.ParseCompilationUnit(@"
/// <seealso cref=""some_cref""/>
class C {} 
");
            var originalCref = originalCompilationUnit.DescendantNodes(descendIntoTrivia: true).OfType<CrefSyntax>()
                .Single();

            var trackedCompilationUnit = TreeTracker.AnnotateNodeAndChildren(originalCompilationUnit);
            

            var trackedClass = trackedCompilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            var modifiedClass = trackedClass.WithIdentifier(SyntaxFactory.Identifier("D"))
                .WithLeadingTrivia(trackedClass.GetLeadingTrivia().Insert(0, SyntaxFactory.Comment("// My two cents.")));
            var modifiedCompilationUnit = trackedCompilationUnit.ReplaceNode(trackedClass, modifiedClass);
            var modifiedCref = modifiedCompilationUnit.DescendantNodes(descendIntoTrivia:true).OfType<CrefSyntax>().Single();
            var mappedLocation = modifiedCref.Location.GetSourceLocation();
            Assert.Same(originalCompilationUnit.SyntaxTree, mappedLocation.SourceTree);
            Assert.Equal(originalCref.Location.SourceSpan, mappedLocation.SourceSpan);
        }

        [Fact]
        public void CopyOriginalLocation()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ return 5; }");
            var originalStatement = originalBlock.DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            var originalExpression = originalStatement.Expression;

            var annotatedBlock = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            var modifiedBlock = new CopyOriginalLocationRewriter().Visit(annotatedBlock);

            var transformedStatement = modifiedBlock.DescendantNodes().OfType<ExpressionStatementSyntax>().Single();
            var assignment = (AssignmentExpressionSyntax)transformedStatement.Expression;


            Assert.Same(originalStatement, TreeTracker.GetSourceSyntaxNode(transformedStatement));

            var transformedExpression = assignment.Right;

            Assert.Same(originalExpression, TreeTracker.GetSourceSyntaxNode(transformedExpression));

            Assert.Null( TreeTracker.GetSourceSyntaxNode(assignment.Left));

            Assert.Null(TreeTracker.GetAnnotationForNodeToBeModified(assignment.Left));


            // Check that rewriting of trivia still works. This caused an error in a dependent repo.
            _ = modifiedBlock.NormalizeWhitespace();
        }

        class CopyOriginalLocationRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
            {
                var assignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("x"), node.Expression!));

                assignment = assignment.WithOriginalLocationAnnotationFrom(node);

                return SyntaxFactory.Block(assignment,
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("x")));
            }
        }

        [Fact]
        public void CopyOriginalLocationTwice()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ return 5; }");

            var annotatedBlock = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            var modifiedBlock1 = new CopyOriginalLocationTwiceRewriter1().Visit(annotatedBlock);
            var modifiedBlock2 = new CopyOriginalLocationTwiceRewriter2().Visit(modifiedBlock1);

            // Check that rewriting of trivias still works.
            _ = modifiedBlock2.NormalizeWhitespace();
        }

        class CopyOriginalLocationTwiceRewriter1 : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
            {
                var assignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("x"), node.Expression!));

                assignment = assignment.WithOriginalLocationAnnotationFrom(node);

                return SyntaxFactory.Block(assignment,
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("x")));
            }
        }

        class CopyOriginalLocationTwiceRewriter2 : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return node.WithTrailingTrivia(node.GetTrailingTrivia().Add(SyntaxFactory.ElasticLineFeed));
            }
        }
    }
}
