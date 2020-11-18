using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Caravela.Compiler
{
    internal static class TreeTracker
    {
        private static readonly ConditionalWeakTable<SyntaxAnnotation, SyntaxNode?> preTransformationNodeMap = new();
        private static readonly ConditionalWeakTable<SyntaxAnnotation, StrongBox<SyntaxToken>> preTransformationTokenMap = new();

        private const string TrackingAnnotationKind = "Caravela.Compiler.Tracking";
        // "include descendants" means that the annotation also applies to all descendant node
        // this is commonly used for nodes that are exactly the same as in the pre-transformation tree
        private const string IncludeDescendantsData = nameof(IncludeDescendantsData);
        // "exclude descentants" means that the annotation only applies to the specified node
        // this is used for nodes that changed from the pre-transformation tree
        private const string ExcludeDescendantsData = nameof(ExcludeDescendantsData);

        private static SyntaxAnnotation CreateAnnotation(SyntaxNode? node, bool includeChildren)
        {
            var annotation = new SyntaxAnnotation(TrackingAnnotationKind, includeChildren ? IncludeDescendantsData : ExcludeDescendantsData);

            preTransformationNodeMap.Add(annotation, node);

            return annotation;
        }

        private static SyntaxAnnotation CreateAnnotation(SyntaxToken token)
        {
            var annotation = new SyntaxAnnotation(TrackingAnnotationKind, null);

            preTransformationTokenMap.Add(annotation, new(token));

            return annotation;
        }

        public static bool IsAnnotated(SyntaxNode node) => node.HasAnnotations(TrackingAnnotationKind);

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node, SyntaxNode? preTransformationNode) where TNode : SyntaxNode
        {
            Debug.Assert(!node.GetAnnotations(TrackingAnnotationKind).Any());

            // copied from SyntaxNode.WithAdditionalAnnotationsInternal to avoid infinite recursion
            return (TNode)node.Green.WithAdditionalAnnotationsGreen(new[] { CreateAnnotation(preTransformationNode, includeChildren: preTransformationNode != null) }).CreateRed();
        }

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node) where TNode : SyntaxNode =>
            AnnotateNodeAndChildren(node, node);

        public static SyntaxToken AnnotateToken(SyntaxToken token, SyntaxToken preTransformationToken)
        {
            // copied from SyntaxToken.WithAdditionalAnnotations to avoid infinite recursion
            if (token.Node != null)
            {
                return new SyntaxToken(
                    parent: null,
                    token: token.Node.WithAdditionalAnnotationsGreen(new[] { CreateAnnotation(preTransformationToken) }),
                    position: 0,
                    index: 0);
            }

            return default;
        }

        public static void SetAnnotationExcludeChildren(ref SyntaxAnnotation[] annotations, SyntaxNode node)
        {
            if (NeedsTracking(node, out var preTransformationNode))
            {
                // no annotation found, but is needed, create a new one
                Array.Resize(ref annotations, annotations.Length + 1);
                annotations[^1] = CreateAnnotation(preTransformationNode, includeChildren: false);
            }
            else
            {
                // look for existing annotation
                int index = Array.FindIndex(annotations, a => a.Kind == TrackingAnnotationKind);
                if (index != -1)
                {
                    var oldAnnotation = annotations[index];

                    if (oldAnnotation.Data == ExcludeDescendantsData)
                    {
                        // found and correct, do nothing
                        return;
                    }

                    // found and incorrect, replace the annotation
                    preTransformationNodeMap.TryGetValue(oldAnnotation, out var oldNode);
                    Debug.Assert(oldNode != null);
                    annotations = annotations.ToArray();
                    annotations[index] = CreateAnnotation(oldNode, includeChildren: false);
                }
            }
        }

        private static (SyntaxNode? ancestor, SyntaxAnnotation? annotation) FindAncestorWithAnnotation(SyntaxNode node)
        {
            SyntaxNode? ancestor = node;
            SyntaxAnnotation? annotation = null;

            // find an ancestor that contains the annotation
            while (ancestor != null)
            {
                annotation = ancestor.GetAnnotations(TrackingAnnotationKind).SingleOrDefault();

                if (annotation is not null)
                    break;

                ancestor = ancestor.Parent;
            }

            return (ancestor, annotation);
        }

        private static (SyntaxNodeOrToken? ancestor, SyntaxAnnotation? annotation) FindAncestorWithAnnotation(SyntaxToken token)
        {
            var annotation = token.GetAnnotations(TrackingAnnotationKind).SingleOrDefault();
            if (annotation is not null)
                return (token, annotation);

            var parent = token.Parent;
            if (parent == null)
                return (null, null);

            return FindAncestorWithAnnotation(parent);
        }

        [return: NotNull]
        internal static T FindNode<T>(this SyntaxNode ancestor, TextSpan span) where T : SyntaxNode?
        {
            var foundNode = ancestor.FindNode(span, getInnermostNodeForTie: true);

            // we were looking for node with zero width (like OmittedArraySizeExpression), but found something larger
            // FindNode uses FindToken, which does not return zero-width tokens, so we use it ourselves, but then look just before it
            // see also https://github.com/dotnet/roslyn/issues/47706
            if (span.IsEmpty && !foundNode.FullSpan.IsEmpty)
            {
                SyntaxNode? possibleZeroWidthNode;

                // but that doesn't work if sought node is at the end, so we need a special case for that
                if (span.Start == foundNode.FullSpan.End)
                    possibleZeroWidthNode = foundNode.DescendantNodes().Last();
                else
                    possibleZeroWidthNode = foundNode.FindToken(span.Start).GetPreviousToken(includeZeroWidth: true).Parent;
                if (possibleZeroWidthNode?.FullSpan == span && possibleZeroWidthNode is T t)
                    return t;
            }

            // if the first found node is not the correct type, it should be one of its ancestors (with the same FullSpan as the found node)
            while (foundNode is not T)
            {
                foundNode = foundNode.Parent;
                Debug.Assert(foundNode?.FullSpan == span);
            }
            return (T)foundNode;
        }

        [return: NotNull]
        private static T LocatePreTransformationSyntax<T>([DisallowNull] T node, SyntaxNode ancestor, SyntaxAnnotation annotation) where T : SyntaxNode?
        {
            preTransformationNodeMap.TryGetValue(annotation, out var preTransformationAncestor);
            Debug.Assert(preTransformationAncestor != null);

            var originalPosition = node.Position - ancestor.Position + preTransformationAncestor.Position;
            var nodeOriginalSpan = new TextSpan(originalPosition, node.FullWidth);
            return preTransformationAncestor.FindNode<T>(nodeOriginalSpan);
        }

        private static SyntaxToken LocatePreTransformationSyntax(SyntaxToken token, SyntaxNode parent, SyntaxAnnotation annotation)
        {
            preTransformationNodeMap.TryGetValue(annotation, out var preTransformationAncestor);
            Debug.Assert(preTransformationAncestor != null);

            var originalPosition = token.Position - parent.Position + preTransformationAncestor.Position;
            var foundToken = preTransformationAncestor.FindToken(originalPosition);

            if (foundToken.FullSpan != new TextSpan(originalPosition, token.FullWidth))
            {
                Debug.Assert(token.FullWidth == 0);

                do
                {
                    foundToken = foundToken.GetPreviousToken(includeZeroWidth: true);
                } while (foundToken.FullWidth == 0 && foundToken.RawKind != token.RawKind && foundToken.RawKind != 0);

                Debug.Assert(foundToken.RawKind == token.RawKind);
            }

            return foundToken;
        }

        [return: NotNullIfNotNull("node")]
        public static T TrackIfNeeded<T>(T node) where T : SyntaxNode?
        {
            if (NeedsTracking(node, out var preTransformationNode))
#pragma warning disable CS8631, CS8825
                return AnnotateNodeAndChildren(node, preTransformationNode);
#pragma warning restore CS8631, CS8825

            return node;
        }

        public static SyntaxToken TrackIfNeeded(SyntaxToken token)
        {
            if (NeedsTracking(token, out var preTransformationToken))
                return AnnotateToken(token, preTransformationToken.Value);

            return token;
        }

        public static bool NeedsTracking<T>([NotNullWhen(true)] T node, [NotNullWhen(true)] out T preTransformationNode) where T : SyntaxNode?
        {
            preTransformationNode = null!;

            if (node == null)
                return false;

            // SyntaxList is a special kind of node that does not need tracking
            if (node is SyntaxList)
                return false;

            var (ancestor, annotation) = FindAncestorWithAnnotation(node);

            // no annotation means there's nothing to track
            if (annotation == null)
                return false;

            Debug.Assert(ancestor != null);

            // node is already tracked
            if (ancestor == node)
                return false;

            // unannotated children of ancestor annotated as "exclude children" shouldn't be tracked
            if (annotation.Data == ExcludeDescendantsData)
                return false;

            // compute original node of the current node from the original node of the annotated ancestor
            preTransformationNode = LocatePreTransformationSyntax(node, ancestor, annotation);
            return true;
        }

        public static bool NeedsTracking(SyntaxToken token, [NotNullWhen(true)] out SyntaxToken? preTransformationToken)
        {
            preTransformationToken = null;

            var (ancestor, annotation) = FindAncestorWithAnnotation(token);

            // no annotation means there's nothing to track
            if (annotation == null)
                return false;

            Debug.Assert(ancestor != null);

            // node is already tracked
            if (ancestor == token)
                return false;

            Debug.Assert(ancestor.Value.IsNode);

            // unannotated children of ancestor annotated as "exclude children" shouldn't be tracked
            if (annotation.Data == ExcludeDescendantsData)
                return false;

            // compute original node of the current node from the original node of the annotated ancestor
            preTransformationToken = LocatePreTransformationSyntax(token, ancestor.Value.AsNode()!, annotation);
            return true;
        }

        private static SyntaxToken? GetPreTransformationSyntax(SyntaxToken token)
        {
            var (ancestor, annotation) = FindAncestorWithAnnotation(token);

            // no annotation means no change
            if (annotation == null)
                return token;

            Debug.Assert(ancestor != null);

            // current node is annotated, so return its stored original token directly
            if (ancestor == token)
            {
                preTransformationTokenMap.TryGetValue(annotation, out var preTransformationToken);
                return preTransformationToken!.Value;
            }

            Debug.Assert(ancestor.Value.IsNode);

            // unannotated children of ancestor annotated as "exclude children" don't have original node
            if (annotation.Data == ExcludeDescendantsData)
                return null;

            // compute original node of the current node from the original node of the annotated ancestor
            return LocatePreTransformationSyntax(token, ancestor.Value.AsNode()!, annotation);
        }

        public static T? GetPreTransformationSyntax<T>(T node) where T : SyntaxNode?
        {
            if (node == null)
                return null;

            var (ancestor, annotation) = FindAncestorWithAnnotation(node);

            // no annotation means no change
            if (annotation == null)
                return node;

            Debug.Assert(ancestor != null);

            // current node is annotated, so return its stored original node directly
            if (ancestor == node)
            {
                preTransformationNodeMap.TryGetValue(annotation, out var preTransformationNode);
                return (T?)preTransformationNode;
            }

            // unannotated children of ancestor annotated as "exclude children" don't have original node
            if (annotation.Data == ExcludeDescendantsData)
                return null;

            // compute original node of the current node from the original node of the annotated ancestor
            return LocatePreTransformationSyntax(node, ancestor, annotation);
        }

        public static Location GetPreTransformationLocation(Location location)
        {
            var tree = location.SourceTree;
            if (tree == null)
                return location;

            // if there are no annotations in the whole tree, then there is nothing to do
            if (!tree.GetRoot().GetAnnotations(TrackingAnnotationKind).Any())
                return location;

            // from the given location, try to find the corresponding node, token or token pair and then proceed as usual
            var foundNode = tree.GetRoot().FindNode(location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            if (foundNode.Span == location.SourceSpan)
            {
                var preTransformationNode = GetPreTransformationSyntax(foundNode);

                return preTransformationNode?.GetLocation() ?? Location.Create(tree, default);
            }

            var startToken = foundNode.FindToken(location.SourceSpan.Start, findInsideTrivia: true);
            var preTransformationStartToken = GetPreTransformationSyntax(startToken);

            if (preTransformationStartToken == null)
                return Location.Create(tree, default);

            if (startToken.Span == location.SourceSpan)
            {
                return preTransformationStartToken.Value.GetLocation();
            }
            if (location.SourceSpan.IsEmpty)
            {
                return Location.Create(
                    preTransformationStartToken.Value.SyntaxTree!,
                    new TextSpan(preTransformationStartToken.Value.Position, 0));
            }
            // if the location is contained within a single token and the width of the token didn't change during transformation,
            // assume we can still map the location within this token
            if (startToken.FullSpan.Contains(location.SourceSpan))
            {
                if (startToken.Width == preTransformationStartToken.Value.Width)
                {
                    return Location.Create(
                        preTransformationStartToken.Value.SyntaxTree!,
                        new TextSpan(location.SourceSpan.Start - startToken.SpanStart + preTransformationStartToken.Value.SpanStart, location.SourceSpan.Length));
                }
                else
                {
                    return Location.Create(tree, default);
                }
            }

            var endToken = foundNode.FindToken(location.SourceSpan.End - 1);
            if (TextSpan.FromBounds(startToken.Span.Start, endToken.Span.End) == location.SourceSpan)
            {
                var preTransformationEndToken = GetPreTransformationSyntax(endToken);

                if (preTransformationEndToken != null)
                    return Location.Create(
                        preTransformationStartToken.Value.SyntaxTree!,
                        TextSpan.FromBounds(preTransformationStartToken.Value.SpanStart, preTransformationEndToken.Value.Span.End));
            }

            return Location.Create(tree, default);
        }

#if DEBUG
        private static readonly ConditionalWeakTable<SyntaxTree, object?> undebuggableTrees = new();

        public static bool IsUndebuggable(SyntaxTree? tree) => tree is not null && undebuggableTrees.TryGetValue(tree, out _);

        public static void MarkAsUndebuggable(SyntaxTree tree) => undebuggableTrees.Add(tree, null);
#endif
    }
}
