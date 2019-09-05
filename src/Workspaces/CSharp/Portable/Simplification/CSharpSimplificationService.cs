// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    [ExportLanguageService(typeof(ISimplificationService), LanguageNames.CSharp), Shared]
    internal partial class CSharpSimplificationService : AbstractSimplificationService<ExpressionSyntax, StatementSyntax, CrefSyntax>
    {
        // 1. the cast simplifier should run earlier then everything else to minimize the type expressions
        // 2. Extension method reducer may insert parentheses.  So run it before the parentheses remover.
        private static readonly ImmutableArray<AbstractReducer> s_reducers =
            ImmutableArray.Create<AbstractReducer>(
                new CSharpNameReducer(),
                new CSharpNullableAnnotationReducer(),
                new CSharpCastReducer(),
                new CSharpExtensionMethodReducer(),
                new CSharpParenthesesReducer(),
                new CSharpEscapingReducer(),
                new CSharpMiscellaneousReducer(),
                new CSharpInferredMemberNameReducer(),
                new CSharpDefaultExpressionReducer());

        [ImportingConstructor]
        public CSharpSimplificationService() : base(s_reducers)
        {
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
                    throw new ArgumentException(CSharpWorkspaceResources.Only_attributes_constructor_initializers_expressions_or_statements_can_be_made_explicit, nameof(node));
                }
            }
        }

        public override SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Func<SyntaxNode, bool> expandInsideNode, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Simplifier_ExpandToken, cancellationToken))
            {
                var rewriter = new Expander(semanticModel, expandInsideNode, false, cancellationToken);

                var rewrittenToken = TryEscapeIdentifierToken(rewriter.VisitToken(token), token.Parent, semanticModel).WithAdditionalAnnotations(Simplifier.Annotation);
                if (TryAddLeadingElasticTriviaIfNecessary(rewrittenToken, token, out var rewrittenTokenWithElasticTrivia))
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

            if (SyntaxFacts.GetContextualKeywordKind(syntaxToken.ValueText) == SyntaxKind.UnderscoreToken)
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
            if (TryAddLeadingElasticTriviaIfNecessary(firstRewrittenToken, firstOriginalToken, out var rewrittenTokenWithLeadingElasticTrivia))
            {
                return rewrittenNode.ReplaceToken(firstRewrittenToken, rewrittenTokenWithLeadingElasticTrivia);
            }

            return rewrittenNode;
        }

        private static bool TryAddLeadingElasticTriviaIfNecessary(SyntaxToken token, SyntaxToken originalToken, out SyntaxToken tokenWithLeadingWhitespace)
        {
            tokenWithLeadingWhitespace = default;

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

        private const string s_CS8019_UnusedUsingDirective = "CS8019";

        protected override void GetUnusedNamespaceImports(SemanticModel model, HashSet<SyntaxNode> namespaceImports, CancellationToken cancellationToken)
        {
            var root = model.SyntaxTree.GetRoot();
            var diagnostics = model.GetDiagnostics(cancellationToken: cancellationToken);

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == s_CS8019_UnusedUsingDirective)
                {

                    if (root.FindNode(diagnostic.Location.SourceSpan) is UsingDirectiveSyntax node)
                    {
                        namespaceImports.Add(node);
                    }
                }
            }
        }

        // Is the tuple on either side of a deconstruction (top-level or nested)?
        private static bool IsTupleInDeconstruction(SyntaxNode tuple)
        {
            Debug.Assert(tuple.IsKind(SyntaxKind.TupleExpression));
            var currentTuple = tuple;
            do
            {
                var parent = currentTuple.Parent;
                if (parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    return true;
                }

                if (!parent.IsKind(SyntaxKind.Argument))
                {
                    return false;
                }

                var grandParent = parent.Parent;
                if (!grandParent.IsKind(SyntaxKind.TupleExpression))
                {
                    return false;
                }

                currentTuple = grandParent;
            }
            while (true);
        }
    }
}
