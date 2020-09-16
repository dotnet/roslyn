using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace RoslynEx.UnitTests
{
    public class TreeTrackerTests
    {
        [Fact]
        public void CanFindOmittedArraySizeExpression()
        {
            var type = SyntaxFactory.ParseTypeName("int[]");

            var omittedExpression = type.DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Single();

            Assert.Same(omittedExpression, TreeTracker.FindNode<OmittedArraySizeExpressionSyntax>(type, omittedExpression.FullSpan));
        }
    }
}
