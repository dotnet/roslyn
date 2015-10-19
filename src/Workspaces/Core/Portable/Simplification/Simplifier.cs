// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// Expands and Reduces subtrees.
    /// 
    /// Expansion:
    ///      1) Replaces names with fully qualified dotted names.
    ///      2) Adds parentheses around expressions
    ///      3) Adds explicit casts/conversions where implicit conversions exist
    ///      4) Adds escaping to identifiers
    ///      5) Rewrites extension method invocations with explicit calls on the class containing the extension method.
    ///      
    /// Reduction:
    ///     1) Shortens dotted names to their minimally qualified form
    ///     2) Removes unnecessary parentheses
    ///     3) Removes unnecessary casts/conversions
    ///     4) Removes unnecessary escaping
    ///     5) Rewrites explicit calls to extension methods to use dot notation
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
        /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
        /// </summary>
        public static async Task<TNode> ExpandAsync<TNode>(TNode node, Document document, Func<SyntaxNode, bool> expandInsideNode = null, bool expandParameter = false, CancellationToken cancellationToken = default(CancellationToken)) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return Expand(node, semanticModel, document.Project.Solution.Workspace, expandInsideNode, expandParameter, cancellationToken);
        }

        /// <summary>
        /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
        /// </summary>
        public static TNode Expand<TNode>(TNode node, SemanticModel semanticModel, Workspace workspace, Func<SyntaxNode, bool> expandInsideNode = null, bool expandParameter = false, CancellationToken cancellationToken = default(CancellationToken)) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (semanticModel == null)
            {
                throw new ArgumentNullException(nameof(semanticModel));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            var result = workspace.Services.GetLanguageServices(node.Language).GetService<ISimplificationService>()
                .Expand(node, semanticModel, annotationForReplacedAliasIdentifier: null, expandInsideNode: expandInsideNode, expandParameter: expandParameter, cancellationToken: cancellationToken);

            return (TNode)result;
        }

        /// <summary>
        /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
        /// </summary>
        public static async Task<SyntaxToken> ExpandAsync(SyntaxToken token, Document document, Func<SyntaxNode, bool> expandInsideNode = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return Expand(token, semanticModel, document.Project.Solution.Workspace, expandInsideNode, cancellationToken);
        }

        /// <summary>
        /// Expand qualifying parts of the specified subtree, annotating the parts using the <see cref="Annotation" /> annotation.
        /// </summary>
        public static SyntaxToken Expand(SyntaxToken token, SemanticModel semanticModel, Workspace workspace, Func<SyntaxNode, bool> expandInsideNode = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (semanticModel == null)
            {
                throw new ArgumentNullException(nameof(semanticModel));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return workspace.Services.GetLanguageServices(token.Language).GetService<ISimplificationService>()
                .Expand(token, semanticModel, expandInsideNode, cancellationToken);
        }

        /// <summary>
        /// Reduce all sub-trees annotated with <see cref="Annotation" /> found within the document. The annotated node and all child nodes will be reduced.
        /// </summary>
        public static async Task<Document> ReduceAsync(Document document, OptionSet optionSet = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await ReduceAsync(document, root.FullSpan, optionSet, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the subtrees identified with the specified <paramref name="annotation"/>.
        /// The annotated node and all child nodes will be reduced.
        /// </summary>
        public static async Task<Document> ReduceAsync(Document document, SyntaxAnnotation annotation, OptionSet optionSet = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await ReduceAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), optionSet, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the specified span.
        /// The annotated node and all child nodes will be reduced.
        /// </summary>
        public static Task<Document> ReduceAsync(Document document, TextSpan span, OptionSet optionSet = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return ReduceAsync(document, SpecializedCollections.SingletonEnumerable(span), optionSet, cancellationToken);
        }

        /// <summary>
        /// Reduce the sub-trees annotated with <see cref="Annotation" /> found within the specified spans.
        /// The annotated node and all child nodes will be reduced.
        /// </summary>
        public static Task<Document> ReduceAsync(Document document, IEnumerable<TextSpan> spans, OptionSet optionSet = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            return document.Project.LanguageServices.GetService<ISimplificationService>().ReduceAsync(document, spans, optionSet, cancellationToken: cancellationToken);
        }

        internal static async Task<Document> ReduceAsync(Document document, IEnumerable<AbstractReducer> reducers, OptionSet optionSet = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await document.Project.LanguageServices.GetService<ISimplificationService>()
                .ReduceAsync(document, SpecializedCollections.SingletonEnumerable(root.FullSpan), optionSet, reducers, cancellationToken).ConfigureAwait(false);
        }
    }
}
