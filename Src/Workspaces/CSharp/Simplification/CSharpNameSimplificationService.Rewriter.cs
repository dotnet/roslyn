using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Extensions;
using Roslyn.Services.Shared.Collections;

namespace Roslyn.Services.CSharp.Simplification
{
    internal partial class CSharpNameSimplificationService
    {
        private class Rewriter : SyntaxRewriter
        {
            private readonly SemanticModel semanticModel;
            private readonly SimpleIntervalTree<TextSpan> spans;
            private readonly CancellationToken cancellationToken;

            private bool shouldRecordSimplifiedNode = true;
            private HashSet<SyntaxNode> simplifiedNodes = new HashSet<SyntaxNode>();
            public HashSet<SyntaxNode> SimplifiedNodes
            {
                get { return simplifiedNodes; }
            }

            public Rewriter(
                SemanticModel semanticModel,
                SimpleIntervalTree<TextSpan> spans,
                CancellationToken cancellationToken)
            {
                this.semanticModel = semanticModel;
                this.spans = spans;
                this.cancellationToken = cancellationToken;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return base.Visit(node);
            }

            private bool TrySimplify(SyntaxNode node, out SyntaxNode result)
            {
                if (spans.GetOverlappingIntervals(node.Span.Start, node.Span.Length).Any())
                {
                    var simplified = Simplify(semanticModel, node, cancellationToken);
                    if (simplified != node)
                    {
                        result = simplified;
                        
                        if (shouldRecordSimplifiedNode)
                        {
                            this.simplifiedNodes.Add(node);
                        }

                        return true;
                    }
                }

                result = null;
                return false;
            }

            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                SyntaxNode result;
                if (TrySimplify(node, out result))
                {
                    return result;
                }

                return base.VisitAliasQualifiedName(node);
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                SyntaxNode result;
                if (TrySimplify(node, out result))
                {
                    return result;
                }

                return base.VisitQualifiedName(node);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                SyntaxNode result;
                if (TrySimplify(node, out result))
                {
                    return result;
                }

                return base.VisitMemberAccessExpression(node);
            }

            public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                bool isOrAsNode = node.Kind == SyntaxKind.AsExpression || node.Kind == SyntaxKind.IsExpression;

                // If we have a is or as node, we are going to record that node as being simplified.
                // So don't record any of it's children as being simplified.
                var oldValue = shouldRecordSimplifiedNode;
                if (isOrAsNode)
                {
                    shouldRecordSimplifiedNode = false;
                }

                var result = (ExpressionSyntax)base.VisitBinaryExpression(node);
                
                if (isOrAsNode)
                {
                    shouldRecordSimplifiedNode = oldValue;
                }

                if (result != node && isOrAsNode)
                {
                    // In order to handle cases in which simplifying a name would result in code
                    // that parses different, we pre-emptively add parentheses that will be
                    // simplfied away later.
                    //
                    // For example, this code...
                    //
                    //     var x = y as Nullable<int> + 1;
                    //
                    // ...should simplify as...
                    //
                    //     var x = (y as int?) + 1;

                    this.simplifiedNodes.Add(node);
                    return result.Parenthesize().WithAdditionalAnnotations(CodeAnnotations.Formatting);
                }

                return result;
            }
        }
    }
}