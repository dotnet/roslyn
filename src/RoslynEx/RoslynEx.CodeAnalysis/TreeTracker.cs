using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace RoslynEx
{
    internal static class TreeTracker
    {
        private static readonly ConditionalWeakTable<SyntaxAnnotation, SyntaxNode?> preTransformationNodeMap = new();

        private const string TrackingAnnotationKind = "RoslynEx.Tracking";
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

        public static bool IsAnnotated(SyntaxNode node) => node.HasAnnotations(TrackingAnnotationKind);

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node, SyntaxNode? preTransformationNode) where TNode : SyntaxNode =>
            node.WithAdditionalAnnotations(CreateAnnotation(preTransformationNode, includeChildren: preTransformationNode != null));

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node) where TNode : SyntaxNode =>
            AnnotateNodeAndChildren(node, node);

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

                    // found and incorrect, replace the annotation in the dictionary and array
                    preTransformationNodeMap.TryGetValue(oldAnnotation, out var oldNode);
                    Debug.Assert(oldNode != null);
                    preTransformationNodeMap.Remove(oldAnnotation);
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

        [return: NotNull]
        internal static T FindNode<T>(this SyntaxNode ancestor, TextSpan span) where T : SyntaxNode?
        {
            var foundNode = ancestor.FindNode(span, getInnermostNodeForTie: true);

            // we were looking for node with zero width (like OmittedArraySizeExpression), but found something larger
            // FindNode uses FindToken, which does not return zero-width tokens, so we use it ourselves, but then look just before it
            // see also https://github.com/dotnet/roslyn/issues/47706
            if (span.IsEmpty && !foundNode.FullSpan.IsEmpty)
            {
                var previousNode = foundNode.FindToken(span.Start).GetPreviousToken(includeZeroWidth: true).Parent;
                if (previousNode?.FullSpan == span && previousNode is T t)
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
        private static T LocatePreTransformationNode<T>([DisallowNull] T node, SyntaxNode ancestor, SyntaxAnnotation annotation) where T : SyntaxNode?
        {
            preTransformationNodeMap.TryGetValue(annotation, out var preTransformationAncestor);
            Debug.Assert(preTransformationAncestor != null);

            var originalPosition = node.Position - ancestor.Position + preTransformationAncestor.Position;
            var nodeOriginalSpan = new TextSpan(originalPosition, node.FullWidth);
            return preTransformationAncestor.FindNode<T>(nodeOriginalSpan);
        }

        [return: NotNullIfNotNull("node")]
        public static T TrackIfNeeded<T>(T node) where T : SyntaxNode?
        {
            if (NeedsTracking(node, out var preTransformationNode))
#pragma warning disable CS8631 
                return AnnotateNodeAndChildren(node, preTransformationNode);
#pragma warning restore CS8631

            return node;
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
            preTransformationNode = LocatePreTransformationNode(node, ancestor, annotation);
            return true;
        }

        public static T? GetPreTransformationNode<T>(T node) where T : SyntaxNode?
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
            return LocatePreTransformationNode(node, ancestor, annotation);
        }

#if DEBUG
        private static readonly ConditionalWeakTable<SyntaxTree, object?> undebuggableTrees = new();

        public static bool IsUndebuggable(SyntaxTree? tree) => tree is not null && undebuggableTrees.TryGetValue(tree, out _);

        public static void MarkAsUndebuggable(SyntaxTree tree) => undebuggableTrees.Add(tree, null);
#endif
    }
}
