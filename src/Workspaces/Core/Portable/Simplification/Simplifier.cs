// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification;

/// <summary>
/// Expands and Reduces subtrees.
/// 
/// Expansion:
///      1) Makes inferred names explicit (on anonymous types and tuples).
///      2) Replaces names with fully qualified dotted names.
///      3) Adds parentheses around expressions
///      4) Adds explicit casts/conversions where implicit conversions exist
///      5) Adds escaping to identifiers
///      6) Rewrites extension method invocations with explicit calls on the class containing the extension method.
///      
/// Reduction:
///     1) Shortens dotted names to their minimally qualified form
///     2) Removes unnecessary parentheses
///     3) Removes unnecessary casts/conversions
///     4) Removes unnecessary escaping
///     5) Rewrites explicit calls to extension methods to use dot notation
///     6) Removes unnecessary tuple element names and anonymous type member names
/// </summary>
public static partial class Simplifier
{
    /// <summary>
    /// The annotation the reducer uses to identify sub trees to be reduced.
    /// The Expand operations add this annotation to nodes so that the Reduce operations later find them.
    /// </summary>
    public static SyntaxAnnotation Annotation { get; } = new SyntaxAnnotation();

    /// <summary>
    /// This is the annotation used by the simplifier and expander to identify Predefined type and preserving
    /// them from over simplification
    /// </summary>
    public static SyntaxAnnotation SpecialTypeAnnotation { get; } = new SyntaxAnnotation();

    /// <summary>
    /// The annotation <see cref="CodeAction.CleanupDocumentAsync"/> used to identify sub trees to look for symbol annotations on.
    /// It will then add import directives for these symbol annotations.
    /// </summary>
    public static SyntaxAnnotation AddImportsAnnotation { get; } = new SyntaxAnnotation();

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    public static async Task<TNode> ExpandAsync<TNode>(TNode node, Document document, Func<SyntaxNode, bool>? expandInsideNode = null, bool expandParameter = false, CancellationToken cancellationToken = default) where TNode : SyntaxNode
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return Expand(node, semanticModel, document.Project.Solution.Services, expandInsideNode, expandParameter, cancellationToken);
    }

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    public static TNode Expand<TNode>(TNode node, SemanticModel semanticModel, Workspace workspace, Func<SyntaxNode, bool>? expandInsideNode = null, bool expandParameter = false, CancellationToken cancellationToken = default) where TNode : SyntaxNode
    {
        if (workspace == null)
            throw new ArgumentNullException(nameof(workspace));

        return Expand(node, semanticModel, workspace.Services.SolutionServices, expandInsideNode, expandParameter, cancellationToken);
    }

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    internal static TNode Expand<TNode>(TNode node, SemanticModel semanticModel, SolutionServices services, Func<SyntaxNode, bool>? expandInsideNode = null, bool expandParameter = false, CancellationToken cancellationToken = default) where TNode : SyntaxNode
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (semanticModel == null)
            throw new ArgumentNullException(nameof(semanticModel));

        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var result = services.GetRequiredLanguageService<ISimplificationService>(node.Language)
            .Expand(node, semanticModel, annotationForReplacedAliasIdentifier: null, expandInsideNode: expandInsideNode, expandParameter: expandParameter, cancellationToken: cancellationToken);

        return (TNode)result;
    }

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    public static async Task<SyntaxToken> ExpandAsync(SyntaxToken token, Document document, Func<SyntaxNode, bool>? expandInsideNode = null, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return Expand(token, semanticModel, document.Project.Solution.Services, expandInsideNode, cancellationToken);
    }

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    public static SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Workspace workspace, Func<SyntaxNode, bool>? expandInsideNode = null, CancellationToken cancellationToken = default)
    {
        if (workspace == null)
            throw new ArgumentNullException(nameof(workspace));

        return Expand(token, semanticModel, workspace.Services.SolutionServices, expandInsideNode, cancellationToken);
    }

    /// <summary>
    /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
    /// </summary>
    internal static SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, SolutionServices services, Func<SyntaxNode, bool>? expandInsideNode = null, CancellationToken cancellationToken = default)
    {
        if (semanticModel == null)
            throw new ArgumentNullException(nameof(semanticModel));

        if (services == null)
            throw new ArgumentNullException(nameof(services));

        return services.GetRequiredLanguageService<ISimplificationService>(token.Language)
            .Expand(token, semanticModel, expandInsideNode, cancellationToken);
    }

    /// <summary>
    /// Reduce all sub-trees annotated with <see cref="Annotation" /> found within the document. The annotated node and all child nodes will be reduced.
    /// </summary>
    public static async Task<Document> ReduceAsync(Document document, OptionSet? optionSet = null, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable RS0030 // Do not used banned APIs
        return await ReduceAsync(document, root.FullSpan, optionSet, cancellationToken).ConfigureAwait(false);
#pragma warning restore
    }

    internal static async Task<Document> ReduceAsync(Document document, SimplifierOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return await ReduceAsync(document, root.FullSpan, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the subtrees identified with the specified <paramref name="annotation"/>.
    /// The annotated node and all child nodes will be reduced.
    /// </summary>
    public static async Task<Document> ReduceAsync(Document document, SyntaxAnnotation annotation, OptionSet? optionSet = null, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (annotation == null)
        {
            throw new ArgumentNullException(nameof(annotation));
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable RS0030 // Do not used banned APIs
        return await ReduceAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), optionSet, cancellationToken).ConfigureAwait(false);
#pragma warning restore
    }

    internal static async Task<Document> ReduceAsync(Document document, SyntaxAnnotation annotation, SimplifierOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return await ReduceAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the specified span.
    /// The annotated node and all child nodes will be reduced.
    /// </summary>
    public static Task<Document> ReduceAsync(Document document, TextSpan span, OptionSet? optionSet = null, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

#pragma warning disable RS0030 // Do not used banned APIs
        return ReduceAsync(document, SpecializedCollections.SingletonEnumerable(span), optionSet, cancellationToken);
    }
#pragma warning restore

    internal static Task<Document> ReduceAsync(Document document, TextSpan span, SimplifierOptions options, CancellationToken cancellationToken)
        => ReduceAsync(document, SpecializedCollections.SingletonEnumerable(span), options, cancellationToken);

    /// <summary>
    /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the specified spans.
    /// The annotated node and all child nodes will be reduced.
    /// </summary>
    public static async Task<Document> ReduceAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? optionSet = null, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (spans == null)
        {
            throw new ArgumentNullException(nameof(spans));
        }

        var options = await GetOptionsAsync(document, optionSet, cancellationToken).ConfigureAwait(false);

        return await document.GetRequiredLanguageService<ISimplificationService>().ReduceAsync(
            document, spans.ToImmutableArrayOrEmpty(), options, reducers: default, cancellationToken).ConfigureAwait(false);
    }

    internal static Task<Document> ReduceAsync(Document document, IEnumerable<TextSpan> spans, SimplifierOptions options, CancellationToken cancellationToken)
        => document.GetRequiredLanguageService<ISimplificationService>().ReduceAsync(
            document, spans.ToImmutableArrayOrEmpty(), options, reducers: default, cancellationToken);

    internal static async Task<Document> ReduceAsync(
        Document document, ImmutableArray<AbstractReducer> reducers, SimplifierOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return await document.GetRequiredLanguageService<ISimplificationService>()
            .ReduceAsync(document, [root.FullSpan], options,
                         reducers, cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable RS0030 // Do not used banned APIs (backwards compatibility)
    internal static async Task<SimplifierOptions> GetOptionsAsync(Document document, OptionSet? optionSet, CancellationToken cancellationToken)
    {
        optionSet ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        var simplificationService = document.Project.Solution.Services.GetRequiredLanguageService<ISimplificationService>(document.Project.Language);
        return simplificationService.GetSimplifierOptions(optionSet, fallbackOptions: null);
    }
#pragma warning restore
}
