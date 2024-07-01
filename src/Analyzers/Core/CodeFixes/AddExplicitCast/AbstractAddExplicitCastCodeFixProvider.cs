// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast;

internal abstract partial class AbstractAddExplicitCastCodeFixProvider<TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
    where TExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but
    /// logs telemetry.
    /// </summary>
    private const int MaximumConversionOptions = 3;

    protected abstract TExpressionSyntax Cast(TExpressionSyntax expression, ITypeSymbol type);
    protected abstract void GetPartsOfCastOrConversionExpression(TExpressionSyntax expression, out SyntaxNode type, out TExpressionSyntax castedExpression);

    /// <summary>
    /// Output the current type information of the target node and the conversion type(s) that the target node is going
    /// to be cast by. Implicit downcast can appear on Variable Declaration, Return Statement, Function Invocation,
    /// Attribute
    /// <para/>
    /// For example:
    /// Base b; Derived d = [||]b;
    /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by
    /// is {Derived}
    /// </summary>
    /// <param name="diagnosticId">The Id of diagnostic</param>
    /// <param name="spanNode">the innermost node that contains the span</param>
    /// <returns>
    /// Output (target expression, potential conversion type) pairs.
    /// </returns>
    protected ImmutableArray<(TExpressionSyntax node, ITypeSymbol type)> GetPotentialTargetTypes(
        Document document, SemanticModel semanticModel, SyntaxNode root,
        string diagnosticId, TExpressionSyntax spanNode, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<(TExpressionSyntax node, ITypeSymbol type)>.GetInstance(out var candidates);

        this.AddPotentialTargetTypes(document, semanticModel, root, diagnosticId, spanNode, candidates, cancellationToken);
        candidates.RemoveDuplicates();

        return FilterValidPotentialConversionTypes(document, semanticModel, candidates);
    }

    protected abstract void AddPotentialTargetTypes(
        Document document, SemanticModel semanticModel, SyntaxNode root,
        string diagnosticId, TExpressionSyntax spanNode,
        ArrayBuilder<(TExpressionSyntax node, ITypeSymbol type)> candidates,
        CancellationToken cancellationToken);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

        var potentialConversionTypes = GetPotentialTargetTypes(
            document, semanticModel, root, diagnostic.Id, spanNode, cancellationToken);

        if (potentialConversionTypes.Length == 1)
        {
            RegisterCodeFix(context, CodeFixesResources.Add_explicit_cast, nameof(CodeFixesResources.Add_explicit_cast));
        }
        else if (potentialConversionTypes.Length > 1)
        {
            using var actions = TemporaryArray<CodeAction>.Empty;

            // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
            foreach (var (targetNode, conversionType) in potentialConversionTypes)
            {
                var title = GetSubItemName(semanticModel, targetNode.SpanStart, conversionType);

                actions.Add(CodeAction.Create(
                    title,
                    cancellationToken =>
                    {
                        var (finalTarget, replacement) = ApplyFix(document, semanticModel, targetNode, conversionType, cancellationToken);

                        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(finalTarget, replacement)));
                    },
                    title));

                if (actions.Count == MaximumConversionOptions)
                    break;
            }

            context.RegisterCodeFix(
                CodeAction.Create(CodeFixesResources.Add_explicit_cast, actions.ToImmutableAndClear(), isInlinable: false),
                context.Diagnostics);
        }
    }

    private (SyntaxNode finalTarget, SyntaxNode finalReplacement) ApplyFix(
        Document document,
        SemanticModel semanticModel,
        TExpressionSyntax targetNode,
        ITypeSymbol conversionType,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

        var (currentTarget, currentReplacement) = ApplyFixWorker();

        // If the original target was surrounded by parentheses, we can consider trying to remove that as well.
        if (syntaxFacts.IsParenthesizedExpression(currentTarget.Parent))
        {
            return (currentTarget.Parent, currentTarget.Parent.ReplaceNode(currentTarget, currentReplacement).WithAdditionalAnnotations(Simplifier.Annotation));
        }

        return (currentTarget, currentReplacement);

        (SyntaxNode finalTarget, SyntaxNode finalReplacement) ApplyFixWorker()
        {
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
                        return (targetNode, this.Cast(castedExpression, conversionType).WithTriviaFrom(targetNode));
                    }
                }
            }

            return Cast(semanticModel, targetNode, conversionType);
        }
    }

    protected virtual (SyntaxNode finalTarget, SyntaxNode finalReplacement) Cast(SemanticModel semanticModel, TExpressionSyntax targetNode, ITypeSymbol conversionType)
        => (targetNode, this.Cast(targetNode, conversionType));

    private static string GetSubItemName(SemanticModel semanticModel, int position, ITypeSymbol conversionType)
    {
        return string.Format(
            CodeFixesResources.Convert_type_to_0,
            conversionType.ToMinimalDisplayString(semanticModel, position));
    }

    private static ImmutableArray<(TExpressionSyntax, ITypeSymbol)> FilterValidPotentialConversionTypes(
        Document document,
        SemanticModel semanticModel,
        ArrayBuilder<(TExpressionSyntax node, ITypeSymbol type)> candidates)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

        using var _ = ArrayBuilder<(TExpressionSyntax, ITypeSymbol)>.GetInstance(candidates.Count, out var validPotentialConversionTypes);
        foreach (var (targetNode, targetNodeConversionType) in candidates)
        {
            // For cases like object creation expression. for example:
            // Derived d = [||]new Base();
            // It is always invalid except the target node has explicit conversion operator or is numeric.
            if (syntaxFacts.IsObjectCreationExpression(targetNode) &&
                !semanticFacts.ClassifyConversion(semanticModel, targetNode, targetNodeConversionType).IsUserDefined)
            {
                continue;
            }

            validPotentialConversionTypes.Add((targetNode, targetNodeConversionType));
        }

        return validPotentialConversionTypes.ToImmutableAndClear();
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

    protected sealed override async Task FixAllAsync(
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
                var potentialConversionTypes = GetPotentialTargetTypes(
                    document, semanticModel, root, diagnostics[0].Id, spanNode, cancellationToken);
                if (potentialConversionTypes.Length == 1)
                {
                    var (newTarget, newReplacement) = ApplyFix(document, semanticModel, potentialConversionTypes[0].node, potentialConversionTypes[0].type, cancellationToken);
                    return root.ReplaceNode(newTarget, newReplacement);
                }

                return root;
            },
            cancellationToken).ConfigureAwait(false);
    }
}
