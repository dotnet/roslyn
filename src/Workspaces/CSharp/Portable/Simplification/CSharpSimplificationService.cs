// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    [ExportLanguageService(typeof(ISimplificationService), LanguageNames.CSharp), Shared]
    internal partial class CSharpSimplificationService : AbstractSimplificationService<ExpressionSyntax, StatementSyntax, CrefSyntax>
    {
        protected override IEnumerable<AbstractReducer> GetReducers()
        {
            yield return new CSharpCastReducer();
            yield return new CSharpNameReducer(); // the cast simplifier should run earlier to minimize the type expressions
            yield return new CSharpParenthesesReducer();
            yield return new CSharpExtensionMethodReducer();
            yield return new CSharpEscapingReducer();
            yield return new CSharpMiscellaneousReducer();
        }

        public override SyntaxNode Expand(SyntaxNode node, SemanticModel semanticModel, SyntaxAnnotation annotationForReplacedAliasIdentifier, Func<SyntaxNode, bool> expandInsideNode, bool expandParameter, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Simplifier_ExpandNode, cancellationToken))
            {
                if (node is AttributeSyntax ||
                    node is AttributeArgumentSyntax ||
                    node is ConstructorInitializerSyntax ||
                    node is ExpressionSyntax ||
                    node is FieldDeclarationSyntax ||
                    node is StatementSyntax ||
                    node is CrefSyntax ||
                    node is XmlNameAttributeSyntax ||
                    node is TypeConstraintSyntax ||
                    node is BaseTypeSyntax)
                {
                    var rewriter = new Expander(semanticModel, expandInsideNode, expandParameter, cancellationToken, annotationForReplacedAliasIdentifier);
                    return rewriter.Visit(node);
                }
                else
                {
                    throw new ArgumentException(
                        CSharpWorkspaceResources.OnlyAttributesConstructorI,
                        paramName: "node");
                }
            }
        }

        public override SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Func<SyntaxNode, bool> expandInsideNode, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Simplifier_ExpandToken, cancellationToken))
            {
                var rewriter = new Expander(semanticModel, expandInsideNode, false, cancellationToken);

                var rewrittenToken = TryEscapeIdentifierToken(rewriter.VisitToken(token), token.Parent, semanticModel).WithAdditionalAnnotations(Simplifier.Annotation);
                SyntaxToken rewrittenTokenWithElasticTrivia;
                if (TryAddLeadingElasticTriviaIfNecessary(rewrittenToken, token, out rewrittenTokenWithElasticTrivia))
                {
                    return rewrittenTokenWithElasticTrivia;
                }

                return rewrittenToken;
            }
        }

        public static SyntaxToken TryEscapeIdentifierToken(SyntaxToken syntaxToken, SyntaxNode parentOfToken, SemanticModel semanticModel)
        {
            // do not escape an already escaped identifier
            if (syntaxToken.IsVerbatimIdentifier())
            {
                return syntaxToken;
            }

            if (SyntaxFacts.GetKeywordKind(syntaxToken.ValueText) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(syntaxToken.ValueText) == SyntaxKind.None)
            {
                return syntaxToken;
            }

            var parent = parentOfToken.Parent;
            if (parentOfToken is SimpleNameSyntax && parent.Kind() == SyntaxKind.XmlNameAttribute)
            {
                // do not try to escape XML name attributes
                return syntaxToken;
            }

            // do not escape global in a namespace qualified name
            if (parent.Kind() == SyntaxKind.AliasQualifiedName &&
                syntaxToken.ValueText == "global")
            {
                return syntaxToken;
            }

            // safe to escape identifier
            return syntaxToken.CopyAnnotationsTo(
                SyntaxFactory.VerbatimIdentifier(
                    syntaxToken.LeadingTrivia,
                    syntaxToken.ToString(),
                    syntaxToken.ValueText,
                    syntaxToken.TrailingTrivia))
                        .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static T AppendElasticTriviaIfNecessary<T>(T rewrittenNode, T originalNode) where T : SyntaxNode
        {
            var firstRewrittenToken = rewrittenNode.GetFirstToken(true, false, true, true);
            var firstOriginalToken = originalNode.GetFirstToken(true, false, true, true);

            SyntaxToken rewrittenTokenWithLeadingElasticTrivia;
            if (TryAddLeadingElasticTriviaIfNecessary(firstRewrittenToken, firstOriginalToken, out rewrittenTokenWithLeadingElasticTrivia))
            {
                return rewrittenNode.ReplaceToken(firstRewrittenToken, rewrittenTokenWithLeadingElasticTrivia);
            }

            return rewrittenNode;
        }

        private static bool TryAddLeadingElasticTriviaIfNecessary(SyntaxToken token, SyntaxToken originalToken, out SyntaxToken tokenWithLeadingWhitespace)
        {
            tokenWithLeadingWhitespace = default(SyntaxToken);

            if (token.HasLeadingTrivia)
            {
                return false;
            }

            var previousToken = originalToken.GetPreviousToken();

            if (previousToken.HasTrailingTrivia)
            {
                return false;
            }

            tokenWithLeadingWhitespace = token.WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithAdditionalAnnotations(Formatter.Annotation);
            return true;
        }

        protected override SemanticModel GetSpeculativeSemanticModel(ref SyntaxNode nodeToSpeculate, SemanticModel originalSemanticModel, SyntaxNode originalNode)
        {
            var syntaxNodeToSpeculate = nodeToSpeculate;
            Contract.ThrowIfNull(syntaxNodeToSpeculate);
            Contract.ThrowIfFalse(SpeculationAnalyzer.CanSpeculateOnNode(nodeToSpeculate));
            return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(originalNode, syntaxNodeToSpeculate, originalSemanticModel);
        }

        protected override ImmutableArray<NodeOrTokenToReduce> GetNodesAndTokensToReduce(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
        {
            return NodesAndTokensToReduceComputer.Compute(root, isNodeOrTokenOutsideSimplifySpans);
        }

        protected override bool CanNodeBeSimplifiedWithoutSpeculation(SyntaxNode node)
        {
            return false;
        }

        private static readonly string s_CS8019_UnusedUsingDirective = "CS8019";

        protected override void GetUnusedNamespaceImports(SemanticModel model, HashSet<SyntaxNode> namespaceImports, CancellationToken cancellationToken)
        {
            var root = model.SyntaxTree.GetRoot();
            var diagnostics = model.GetDiagnostics(cancellationToken: cancellationToken);

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == s_CS8019_UnusedUsingDirective)
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan) as UsingDirectiveSyntax;

                    if (node != null)
                    {
                        namespaceImports.Add(node);
                    }
                }
            }
        }
    }
}
