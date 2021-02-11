using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Caravela.Compiler.UnitTests
{
    public class TreeTrackerTests
    {
        [Fact]
        public void CanFindOmittedArraySizeExpression()
        {
            var type = SyntaxFactory.ParseTypeName("int[]");

            var omittedExpression = type.DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Single();

            Assert.Same(omittedExpression, TreeTracker.FindNode<OmittedArraySizeExpressionSyntax>(type, omittedExpression.FullSpan, findInsideTrivia: false));
        }

        [Fact]
        public void ModifiedBlockSyntax()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ int i; }");

            var block = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            block = block.AddStatements(SyntaxFactory.ParseStatement("i++;"));

            Assert.Same(originalBlock, TreeTracker.GetPreTransformationSyntax(block));
            Assert.Same(originalBlock.Statements[0], TreeTracker.GetPreTransformationSyntax(block.Statements[0]));
            Assert.Null(TreeTracker.GetPreTransformationSyntax(block.Statements[1]));
        }

        [Fact]
        public void ReplacedBlockSyntax()
        {
            var originalBlock = (BlockSyntax)SyntaxFactory.ParseStatement("{ int i; }");

            var block = TreeTracker.AnnotateNodeAndChildren(originalBlock);

            block = SyntaxFactory.Block(block.Statements.Append(SyntaxFactory.ParseStatement("i++;")));

            Assert.Same(block, TreeTracker.GetPreTransformationSyntax(block));
            Assert.Same(originalBlock.Statements[0], TreeTracker.GetPreTransformationSyntax(block.Statements[0]));
            Assert.Same(block.Statements[1], TreeTracker.GetPreTransformationSyntax(block.Statements[1]));
        }
    }
}
