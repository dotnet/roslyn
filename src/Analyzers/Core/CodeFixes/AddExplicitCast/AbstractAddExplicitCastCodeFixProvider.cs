// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
{
    internal abstract partial class AbstractAddExplicitCastCodeFixProvider<TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
    {
        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix,
        /// but logs telemetry
        /// </summary>
        private const int MaximumConversionOptions = 3;

        protected abstract TExpressionSyntax Cast(TExpressionSyntax expression, ITypeSymbol type);
        protected abstract void GetPartsOfCastOrConversionExpression(TExpressionSyntax expression, out SyntaxNode type, out SyntaxNode castedExpression);

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is
        /// going to be cast by. Implicit downcast can appear on Variable Declaration, Return Statement, Function
        /// Invocation, Attribute
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by
        /// is {Derived}
        /// </summary>
        /// <param name="diagnosticId">The Id of diagnostic</param>
        /// <param name="spanNode">the innermost node that contains the span</param>
        /// <param name="potentialConversionTypes"> Output (target expression, potential conversion type) pairs</param>
        /// <returns>
        /// True, if there is at least one potential conversion pair, and they are assigned to
        /// "potentialConversionTypes" False, if there is no potential conversion pair.
        /// </returns>
        protected abstract bool TryGetTargetTypeInfo(
            Document document, SemanticModel semanticModel, SyntaxNode root,
            string diagnosticId, TExpressionSyntax spanNode, CancellationToken cancellationToken,
            out ImmutableArray<(TExpressionSyntax node, ITypeSymbol type)> potentialConversionTypes);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var spanNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .GetAncestorsOrThis<TExpressionSyntax>().FirstOrDefault();
            if (spanNode == null)
                return;

            var hasSolution = TryGetTargetTypeInfo(document,
                semanticModel, root, diagnostic.Id, spanNode, cancellationToken,
                out var potentialConversionTypes);
            if (!hasSolution)
                return;

            if (potentialConversionTypes.Length == 1)
            {
                RegisterCodeFix(context, CodeFixesResources.Add_explicit_cast, nameof(CodeFixesResources.Add_explicit_cast));
                return;
            }

            using var actions = TemporaryArray<CodeAction>.Empty;

            // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
            for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConversionTypes.Length); i++)
            {
                var targetNode = potentialConversionTypes[i].node;
                var conversionType = potentialConversionTypes[i].type;
                var title = GetSubItemName(semanticModel, targetNode.SpanStart, conversionType);

                actions.Add(CodeAction.Create(
                    title,
                    cancellationToken => Task.FromResult(document.WithSyntaxRoot(
                        ApplyFix(document, semanticModel, root, targetNode, conversionType, cancellationToken))),
                    title));
            }

            context.RegisterCodeFix(
                CodeAction.Create(CodeFixesResources.Add_explicit_cast, actions.ToImmutableAndClear(), isInlinable: false),
                context.Diagnostics);
        }

        private SyntaxNode ApplyFix(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode currentRoot,
            TExpressionSyntax targetNode,
            ITypeSymbol conversionType,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            // if the node we're about to cast already has a cast, replace that cast if both are reference-identity downcasts.
            if (syntaxFacts.IsCastExpression(targetNode) || syntaxFacts.IsConversionExpression(targetNode))
            {
                GetPartsOfCastOrConversionExpression(targetNode, out var castTypeNode, out var castedExpression);

                var castType = semanticModel.GetTypeInfo(castTypeNode, cancellationToken).Type;
                if (castType != null)
                {
                    var firstConversion = semanticFacts.ClassifyConversion(semanticModel, castedExpression, castType);
                    var secondConversion = semanticModel.Compilation.ClassifyCommonConversion(castType, conversionType);

                    if (firstConversion is { IsImplicit: false, IsReference: true } or { IsIdentity: true } &&
                        secondConversion is { IsImplicit: false, IsReference: true })
                    {
                        return currentRoot.ReplaceNode(
                            targetNode,
                            this.Cast((TExpressionSyntax)castedExpression, conversionType)
                                .WithTriviaFrom(targetNode)
                                .WithAdditionalAnnotations(Simplifier.Annotation));
                    }
                }
            }

            return currentRoot.ReplaceNode(
                targetNode,
                this.Cast(targetNode, conversionType).WithAdditionalAnnotations(Simplifier.Annotation));
        }

        private static string GetSubItemName(SemanticModel semanticModel, int position, ITypeSymbol conversionType)
        {
            return string.Format(
                CodeFixesResources.Convert_type_to_0,
                conversionType.ToMinimalDisplayString(semanticModel, position));
        }

        protected static ImmutableArray<(TExpressionSyntax, ITypeSymbol)> FilterValidPotentialConversionTypes(
            Document document,
            SemanticModel semanticModel,
            ArrayBuilder<(TExpressionSyntax node, ITypeSymbol type)> mutablePotentialConversionTypes)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            using var _ = ArrayBuilder<(TExpressionSyntax, ITypeSymbol)>.GetInstance(out var validPotentialConversionTypes);
            foreach (var conversionTuple in mutablePotentialConversionTypes)
            {
                var targetNode = conversionTuple.node;
                var targetNodeConversionType = conversionTuple.type;

                // For cases like object creation expression. for example:
                // Derived d = [||]new Base();
                // It is always invalid except the target node has explicit conversion operator or is numeric.
                if (syntaxFacts.IsObjectCreationExpression(targetNode) &&
                    !semanticFacts.ClassifyConversion(semanticModel, targetNode, targetNodeConversionType).IsUserDefined)
                {
                    continue;
                }

                validPotentialConversionTypes.Add(conversionTuple);
            }

            return validPotentialConversionTypes.Distinct().ToImmutableArray();
        }

        protected static bool FindCorrespondingParameterByName(
            string argumentName, ImmutableArray<IParameterSymbol> parameters, ref int parameterIndex)
        {
            for (var j = 0; j < parameters.Length; j++)
            {
                if (argumentName.Equals(parameters[j].Name))
                {
                    parameterIndex = j;
                    return true;
                }
            }

            return false;
        }

        protected override async Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var spanNodes = diagnostics.SelectAsArray(
                d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true)
                         .GetAncestorsOrThis<TExpressionSyntax>().First());

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, spanNodes,
                (semanticModel, spanNode) => true,
                (semanticModel, root, spanNode) =>
                {
                    // All diagnostics have the same error code
                    if (TryGetTargetTypeInfo(document, semanticModel, root, diagnostics[0].Id, spanNode, cancellationToken, out var potentialConversionTypes) &&
                        potentialConversionTypes.Length == 1)
                    {
                        return ApplyFix(document, semanticModel, root, potentialConversionTypes[0].node, potentialConversionTypes[0].type, cancellationToken);
                    }

                    return root;
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
