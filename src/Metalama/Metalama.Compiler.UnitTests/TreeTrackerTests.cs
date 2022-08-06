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

            Assert.Same(omittedExpression, type.FindNode<OmittedArraySizeExpressionSyntax>(omittedExpression.FullSpan, findInsideTrivia: false));
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
        public void CopyOriginalLocation()
        {
             var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ return 5; }");
            
            var annotatedBlock = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            var modifiedBlock = new ReturnRewriter().Visit(annotatedBlock);

            // Check that rewriting of trivias still works.
            _ = modifiedBlock.NormalizeWhitespace();

        }
        
        class ReturnRewriter : CSharpSyntaxRewriter
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
    }
}
