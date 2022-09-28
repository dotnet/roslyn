// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Metalama.Compiler
{
    internal static class TreeTracker
    {
        private static readonly ConditionalWeakTable<SyntaxAnnotation, AnnotationData> annotations = new();

        // "include descendants" means that the annotation also applies to all descendant node
        // this is commonly used for nodes that are exactly the same as in the pre-transformation tree
        private const string IncludeDescendantsData = "IncludeDescendants";

        // "exclude descendants" means that the annotation only applies to the specified node
        // this is used for nodes that changed from the pre-transformation tree
        private const string ExcludeDescendantsData = "ExcludeDescendants";

        private static SyntaxAnnotation CreateAnnotationForNode(SyntaxNode? node, bool includeChildren)
        {
            var annotation = new SyntaxAnnotation(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind,
                includeChildren ? IncludeDescendantsData : ExcludeDescendantsData);

            annotations.Add(annotation, new AnnotationData(node));

            return annotation;
        }

        private static SyntaxAnnotation CreateAnnotationForToken(SyntaxToken token)
        {
            var annotation = new SyntaxAnnotation(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind, null);

            annotations.Add(annotation, new AnnotationData(token));

            return annotation;
        }

        public static SyntaxAnnotation? GetAnnotationForNodeToBeModified(SyntaxNode node)
        {
            var trackedNode = TrackIfNeeded(node);
            if (!trackedNode.TryGetAnnotationFast(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind,
                    out var oldAnnotation))
            {
                // The node is not in original source code.
                return null;
            }

            if (oldAnnotation.Data == ExcludeDescendantsData)
            {
                return oldAnnotation;
            }

            if (!annotations.TryGetValue(oldAnnotation, out var oldAnnotationData))
            {
                Debug.Fail("Cannot get the annotation data.");
            }

            var newAnnotation = new SyntaxAnnotation(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind, ExcludeDescendantsData);
            annotations.Add(newAnnotation, new AnnotationData(oldAnnotationData.NodeOrToken));

            return newAnnotation;
        }

        public static bool IsAnnotated(SyntaxNode node) => node.HasAnnotations(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind);

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node, SyntaxNode? preTransformationNode)
            where TNode : SyntaxNode
        {
            Debug.Assert(!node.HasAnnotations(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind));

            // copied from SyntaxNode.WithAdditionalAnnotationsInternal to avoid infinite recursion
            return (TNode)node.Green.WithAdditionalAnnotationsGreen(new[]
            {
                CreateAnnotationForNode(preTransformationNode, preTransformationNode != null)
            }).CreateRed();
        }

        public static TNode AnnotateNodeAndChildren<TNode>(TNode node)
            where TNode : SyntaxNode =>
            AnnotateNodeAndChildren(node, node);

        private static SyntaxToken AnnotateToken(SyntaxToken token, SyntaxToken preTransformationToken)
        {
            // copied from SyntaxToken.WithAdditionalAnnotations to avoid infinite recursion
            if (token.Node != null)
            {
                return new SyntaxToken(
                    null,
                    token.Node.WithAdditionalAnnotationsGreen(new[]
                    {
                        CreateAnnotationForToken(preTransformationToken)
                    }),
                    0,
                    0);
            }

            return default;
        }

        public static void SetAnnotationExcludeChildren(ref SyntaxAnnotation[] annotations, SyntaxNode node)
        {
            if (NeedsTrackingAnnotation(node, out var preTransformationNode))
            {
                // no annotation found, but is needed, create a new one
                Array.Resize(ref annotations, annotations.Length + 1);
                annotations[^1] = CreateAnnotationForNode(preTransformationNode, false);
            }
            else
            {
                // look for existing annotation
                var index = Array.FindIndex(annotations, a => a.Kind == MetalamaCompilerAnnotations.OriginalLocationAnnotationKind);
                if (index != -1)
                {
                    var oldAnnotation = annotations[index];

                    if (oldAnnotation.Data == ExcludeDescendantsData)
                    {
                        // found and correct, do nothing
                        return;
                    }

                    // found and incorrect, replace the annotation
                    TreeTracker.annotations.TryGetValue(oldAnnotation, out var oldNode);
                    Debug.Assert(oldNode != null);
                    annotations = annotations.ToArray();
                    annotations[index] = CreateAnnotationForNode(oldNode.NodeOrToken.AsNode(),
                        false);
                }
            }
        }

        private static (SyntaxNode? ancestor, SyntaxAnnotation? annotation) FindAncestorWithAnnotation(SyntaxNode node)
        {
            var ancestor = node;

            // find an ancestor that contains the annotation
            while (ancestor != null)
            {
                if (ancestor.TryGetAnnotationFast(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind, out var annotation))
                {
                    return (ancestor, annotation);
                }

                ancestor = ancestor.ParentOrStructuredTriviaParent;
            }

            return (null, null);
        }

        private static (SyntaxNodeOrToken? ancestor, SyntaxAnnotation? annotation) FindAncestorWithAnnotation(
            SyntaxToken token)
        {
            if (token.TryGetAnnotationFast(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind, out var annotation))
            {
                return (token, annotation);
            }

            var parent = token.Parent;
            if (parent == null)
            {
                return (null, null);
            }

            return FindAncestorWithAnnotation(parent);
        }

        [return: NotNull]
        internal static SyntaxNode FindNodeByPosition(this SyntaxNode ancestor, int kind, TextSpan span, bool findInsideTrivia)
        {
            SyntaxNode? foundNode;

            // span is the very end of the ancestor, which is technically not inside it, so FindNode would throw
            if (span.IsEmpty && span.Start == ancestor.EndPosition)
            {
                foundNode = ancestor.DescendantNodes(descendIntoTrivia: findInsideTrivia).Last();
            }
            else
            {
                foundNode = ancestor.FindNode(span, findInsideTrivia, true);
            }

            // we were looking for node with zero width (like OmittedArraySizeExpression or a missing node), but found something larger
            // FindNode uses FindToken, which does not return zero-width tokens, so we use it ourselves, but then look just before it
            // see also https://github.com/dotnet/roslyn/issues/47706
            if (span.IsEmpty && !foundNode.FullSpan.IsEmpty)
            {
                SyntaxToken possibleZeroWidthToken;

                // but that doesn't work if sought node is at the end, so we need a special case for that
                if (span.Start == foundNode.FullSpan.End)
                {
                    possibleZeroWidthToken = foundNode.GetLastToken(true);
                }
                else
                {
                    possibleZeroWidthToken = foundNode.FindToken(span.Start).GetPreviousToken(true);
                }

                while (possibleZeroWidthToken.FullSpan.IsEmpty)
                {
                    var possibleZeroWidthNode = possibleZeroWidthToken.Parent;
                    if (possibleZeroWidthNode?.FullSpan == span && possibleZeroWidthNode.RawKind == kind)
                    {
                        return possibleZeroWidthNode;
                    }

                    possibleZeroWidthToken = possibleZeroWidthToken.GetPreviousToken(true);
                }
            }

            // if the first found node is not the correct type, it should be one of its ancestors (with the same FullSpan as the found node)
            while (foundNode.RawKind != kind)
            {
                foundNode = foundNode.Parent;
                Debug.Assert(foundNode?.FullSpan == span);
            }

            return foundNode;
        }

        [return: NotNull]
        private static bool TryFindSourceNode([DisallowNull] SyntaxNode node, SyntaxNode ancestor, SyntaxAnnotation annotation, [NotNullWhen(true)] out SyntaxNode? sourceNode)
        {
            annotations.TryGetValue(annotation, out var annotationData);
            Debug.Assert(annotationData != null);

            var sourceAncestor = annotationData.NodeOrToken.AsNode()!;

            if (node == ancestor)
            {
                // If the annotation was found on the node itself, then the source node is directly known.
                sourceNode = sourceAncestor;
                return true;
            }
            else if (annotation.Data == ExcludeDescendantsData)
            {
                sourceNode = null;
                return false;
            }
            else
            {
                var originalPosition = node.Position - ancestor.Position + sourceAncestor.Position;
                var nodeOriginalSpan = new TextSpan(originalPosition, node.FullWidth);

                sourceNode =
                    sourceAncestor!
                        .FindNodeByPosition(node.RawKind, nodeOriginalSpan, node.IsPartOfStructuredTrivia());

                return true;
            }
        }

        private static bool TryFindSourceToken(SyntaxToken token, SyntaxNode parent, SyntaxAnnotation annotation, out SyntaxToken foundToken)
        {
            annotations.TryGetValue(annotation, out var annotationData);
            Debug.Assert(annotationData != null);

            var originalPosition = token.Position - parent.Position + annotationData.NodeOrToken.Position;

            // position is the very end of the ancestor, which is technically not inside it, so FindToken would throw
            var preTransformationNode = annotationData.NodeOrToken.AsNode()!;
            if (originalPosition == preTransformationNode.EndPosition)
            {
                foundToken = preTransformationNode.GetLastToken(true);
            }
            else
            {
                foundToken = preTransformationNode.FindToken(originalPosition,
                    token.IsPartOfStructuredTrivia());
            }

            if (foundToken.FullSpan != new TextSpan(originalPosition, token.FullWidth))
            {
                if (token.FullWidth != 0)
                {
                    // This happens when the annotation is copied from one node to another, different node
                    // e.g. with MetalamaCompilerAnnotations.WithOriginalLocationAnnotationFrom(). 
                    return false;
                }

                do
                {
                    foundToken = foundToken.GetPreviousToken(true,
                        includeDocumentationComments: token.IsPartOfStructuredTrivia());
                } while (foundToken.FullWidth == 0 && foundToken.RawKind != token.RawKind && foundToken.RawKind != 0);

                if (foundToken.RawKind != token.RawKind)
                {
                    return false;
                }
            }

            return true;
        }

        [return: NotNullIfNotNull("node")]
        public static T TrackIfNeeded<T>(T node) where T : SyntaxNode?
        {
            if (NeedsTrackingAnnotation(node, out var preTransformationNode))
#pragma warning disable CS8631, CS8825
            {
                return AnnotateNodeAndChildren(node, preTransformationNode);
            }
#pragma warning restore CS8631, CS8825

            return node;
        }

        public static SyntaxToken TrackIfNeeded(SyntaxToken token)
        {
            if (NeedsTrackingAnnotation(token, out var preTransformationToken))
            {
                return AnnotateToken(token, preTransformationToken.Value);
            }

            return token;
        }

        public static SyntaxTrivia TrackIfNeeded(SyntaxTrivia trivia)
        {
            if (trivia.GetStructure() is { } structure)
            {
                var newStructure = TrackIfNeeded(structure);
                if (newStructure != structure)
                {
                    // copied from SyntaxFactory.Trivia(StructuredTriviaSyntax), which can't be called here, because it's C#-specific
                    return new SyntaxTrivia(default, newStructure.Green, 0, 0);
                }
            }

            return trivia;
        }

        private static bool NeedsTrackingAnnotation<T>([NotNullWhen(true)] T node, [NotNullWhen(true)] out SyntaxNode? sourceNode)
            where T : SyntaxNode?
        {
            sourceNode = null!;

            if (node == null)
            {
                return false;
            }

            // SyntaxList is a special kind of node that does not need tracking
            if (node is SyntaxList)
            {
                return false;
            }

            var (ancestor, annotation) = FindAncestorWithAnnotation(node);

            // no annotation means there's nothing to track
            if (annotation == null)
            {
                return false;
            }

            Debug.Assert(ancestor != null);

            // node is already tracked
            if (ancestor == node)
            {
                return false;
            }

            // unannotated children of ancestor annotated as "exclude children" shouldn't be tracked
            if (annotation.Data == ExcludeDescendantsData)
            {
                return false;
            }

            // compute original node of the current node from the original node of the annotated ancestor
            if (TryFindSourceNode(node, ancestor, annotation, out sourceNode))
            {
                return true;
            }
            else
            {
                // There is no source code for this node
                return false;
            }

        }

        public static bool NeedsTrackingAnnotation(SyntaxToken token, [NotNullWhen(true)] out SyntaxToken? preTransformationToken)
        {
            preTransformationToken = null;

            var (ancestor, annotation) = FindAncestorWithAnnotation(token);

            // no annotation means there's nothing to track
            if (annotation == null)
            {
                return false;
            }

            Debug.Assert(ancestor != null);

            // node is already tracked
            if (ancestor == token)
            {
                return false;
            }

            Debug.Assert(ancestor.Value.IsNode);

            // unannotated children of ancestor annotated as "exclude children" shouldn't be tracked
            if (annotation.Data == ExcludeDescendantsData)
            {
                return false;
            }

            // compute original node of the current node from the original node of the annotated ancestor
            if (!TryFindSourceToken(token, ancestor.Value.AsNode()!, annotation,
                    out var foundToken))
            {
                return false;
            }

            preTransformationToken = foundToken;
            return true;
        }

        private static SyntaxToken? GetSourceSyntaxToken(SyntaxToken token)
        {
            var (ancestor, annotation) = FindAncestorWithAnnotation(token);

            // no annotation means no change
            if (annotation == null)
            {
                return token;
            }

            Debug.Assert(ancestor != null);

            // current node is annotated, so return its stored original token directly
            if (ancestor == token)
            {
                annotations.TryGetValue(annotation, out var preTransformationToken);
                return preTransformationToken!.NodeOrToken.AsToken();
            }

            Debug.Assert(ancestor.Value.IsNode);

            // unannotated children of ancestor annotated as "exclude children" don't have original node
            if (annotation.Data == ExcludeDescendantsData)
            {
                return null;
            }

            // compute original node of the current node from the original node of the annotated ancestor
            if (!TryFindSourceToken(token, ancestor.Value.AsNode()!, annotation, out var foundToken))
            {
                return null;
            }
            else
            {
                return foundToken;
            }
        }

        public static SyntaxNode? GetSourceSyntaxNode(SyntaxNode? node)
        {
            if (node == null)
            {
                return null;
            }

            var (ancestor, annotation) = FindAncestorWithAnnotation(node);

            // no annotation means no change
            if (annotation == null)
            {
                return node;
            }

            Debug.Assert(ancestor != null);

            return GetSourceSyntaxNode(node, ancestor, annotation);


        }

        private static SyntaxNode? GetSourceSyntaxNode(SyntaxNode node, SyntaxNode ancestorWithAnnotation, SyntaxAnnotation annotation)
        {

            // current node is annotated, so return its stored original node directly
            if (ancestorWithAnnotation == node)
            {
                annotations.TryGetValue(annotation, out var preTransformationNode);
                return preTransformationNode?.NodeOrToken.AsNode();
            }

            // unannotated children of ancestor annotated as "exclude children" don't have original node
            if (annotation.Data == ExcludeDescendantsData)
            {
                return null;
            }

            // compute original node of the current node from the original node of the annotated ancestor
            if (TryFindSourceNode(node, ancestorWithAnnotation, annotation, out var sourceNode))
            {
                return sourceNode;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<Diagnostic> MapDiagnostics(IEnumerable<Diagnostic> diagnostics)
         => diagnostics.Select(MapDiagnostic);

        public static Diagnostic MapDiagnostic(Diagnostic diagnostic)
        {
            var locationInfo = GetPreTransformationLocationInfo(diagnostic.Location);
            var location = locationInfo.Location;

            if (location != null)
            {
                return diagnostic.WithLocation(location);
            }
            else
            {
                return diagnostic;
            }
        }

        public static bool IsTransformedLocation(this Location location)
            => GetPreTransformationLocationInfo(location).Location == null;

        public static Location GetSourceLocation(this Location location) =>
            GetPreTransformationLocationInfo(location).Location ?? Location.Create(location.SourceTree!, default);

        public static Location GetSourceLocation(this SyntaxNode node) =>
            node.Location.GetSourceLocation();

        public static bool IsTransformedSyntaxNode(this SyntaxNode node)
            => GetSourceSyntaxNode(node) == null;

        public static TextSpan GetSourceSpan(this SyntaxNode node, bool throwOnTransforcedCode = true)
            => GetSourceSyntaxNode(node)?.Span ?? (throwOnTransforcedCode ? throw new InvalidOperationException() : default);

        public static TextSpan GetSourceSpan(this SyntaxToken token, bool throwOnTransforcedCode = true)
         => GetSourceSyntaxToken(token)?.Span ?? (throwOnTransforcedCode ? throw new InvalidOperationException() : default);

        public static TextSpan GetSourceSpan(this SyntaxTokenList list, bool throwOnTransforcedCode = true)
        {
            if (list.Count == 0)
            {
                return default;
            }

            var firstSpan = list[0].GetSourceSpan(throwOnTransforcedCode);
            if (list.Count == 1)
            {
                return firstSpan;
            }
            else
            {
                var lastSpan = list[list.Count - 1].GetSourceSpan(throwOnTransforcedCode);
                return TextSpan.FromBounds(firstSpan.Start, lastSpan.End);
            }

        }


        public static SyntaxTree GetSourceSyntaxTree(this SyntaxNode node, bool throwOnTransforcedCode = true)
        {
            var sourceRoot = GetSourceSyntaxNode(node.SyntaxTree.GetRoot());

            if (sourceRoot == null)
            {
                if (throwOnTransforcedCode)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    return node.SyntaxTree;
                }
            }
            else
            {
                return sourceRoot.SyntaxTree;
            }

        }

        private static (Location? Location, SyntaxNode? SyntaxNode) GetPreTransformationLocationInfo(Location location)
        {
            var tree = location.SourceTree;
            if (tree == null)
            {
                return (location, null);
            }

            // if there are no annotations in the whole tree, then there is nothing to do
            if (!tree.GetRoot().TryGetAnnotationFast(MetalamaCompilerAnnotations.OriginalLocationAnnotationKind, out _))
            {
                return (location, null);
            }

            // From the given location, try to find the corresponding node, token or token pair and then proceed as usual
            var foundNode = tree.GetRoot()
                .FindNode(location.SourceSpan, true, true);
            if (foundNode.Span == location.SourceSpan)
            {
                // The location corresponds to a node in the transformed tree. Find the corresponding tree in the source tree.

                var (ancestor, annotation) = FindAncestorWithAnnotation(foundNode);

                // We must find some annotation since we know there is one on the syntax root.
                Debug.Assert(annotation != null);
                Debug.Assert(ancestor != null);

                var sourceNode = GetSourceSyntaxNode(foundNode, ancestor, annotation);

                if (sourceNode != null)
                {
                    return (sourceNode.Location, sourceNode);
                }
                else
                {
                    // The location does not correspond to a node in the transformed tree.
                    // This can happen with XML documentation (cref) nodes: in the source tree, it is represented as a trivia,
                    // but it is parsed into a new syntax tree, and the diagnostic is reported on the parsed node.
                    // The transformed node then maps to a span of a trivia in the source tree.

                    var sourceAncestor = GetSourceSyntaxNode(ancestor, ancestor, annotation);

                    if (sourceAncestor != null)
                    {

                        var sourcePosition = sourceAncestor.FullSpan.Start + (foundNode.SpanStart - ancestor.FullSpan.Start);
                        var sourceTrivia = sourceAncestor.FindTrivia(sourcePosition);

                        var newTextSpan = new TextSpan(sourcePosition, location.SourceSpan.Length);
                        
                        if (sourceTrivia.FullSpan.Contains(newTextSpan) && 
                            tree.GetText().GetSubText(location.SourceSpan).ToString() == sourceAncestor.SyntaxTree.GetText().GetSubText(newTextSpan).ToString() )
                        {
                            // The source node indeeds maps to inside the trivia.
                            
                            return (Location.Create(sourceAncestor.SyntaxTree!, newTextSpan), null);
                        }

                    }

                    return (location, null);
                }
            }

            var startToken = foundNode.FindToken(location.SourceSpan.Start, true);
            var preTransformationStartToken = GetSourceSyntaxToken(startToken);

            if (preTransformationStartToken == null)
            {
                return (null, null);
            }

            var preTransformationSyntaxNode = preTransformationStartToken.Value.Parent;

            if (startToken.Span == location.SourceSpan)
            {
                return (preTransformationStartToken.Value.GetLocation(), preTransformationSyntaxNode);
            }

            if (location.SourceSpan.IsEmpty)
            {
                return (Location.Create(
                        preTransformationStartToken.Value.SyntaxTree!,
                        new TextSpan(preTransformationStartToken.Value.Position, 0)),
                    preTransformationSyntaxNode);
            }

            // if the location is contained within a single token and the width of the token didn't change during transformation,
            // assume we can still map the location within this token
            if (startToken.FullSpan.Contains(location.SourceSpan))
            {
                if (startToken.Width == preTransformationStartToken.Value.Width)
                {
                    return (Location.Create(
                            preTransformationStartToken.Value.SyntaxTree!,
                            new TextSpan(
                                location.SourceSpan.Start - startToken.SpanStart +
                                preTransformationStartToken.Value.SpanStart, location.SourceSpan.Length)),
                        preTransformationSyntaxNode);
                }
                else
                {
                    return (null, null);
                }
            }

            var endToken = foundNode.FindToken(location.SourceSpan.End - 1);
            if (TextSpan.FromBounds(startToken.Span.Start, endToken.Span.End) == location.SourceSpan)
            {
                var preTransformationEndToken = GetSourceSyntaxToken(endToken);

                if (preTransformationEndToken != null)
                {
                    return (Location.Create(
                            preTransformationStartToken.Value.SyntaxTree!,
                            TextSpan.FromBounds(preTransformationStartToken.Value.SpanStart,
                                preTransformationEndToken.Value.Span.End)),
                        preTransformationSyntaxNode);
                }
            }

            return (null, null);
        }

#if DEBUG
        private static readonly ConditionalWeakTable<SyntaxTree, object?> undebuggableTrees = new();

        public static bool IsUndebuggable(SyntaxTree? tree) =>
            tree is not null && undebuggableTrees.TryGetValue(tree, out _);

        public static void MarkAsUndebuggable(SyntaxTree tree) => undebuggableTrees.Add(tree, null);
#endif

        private class AnnotationData
        {
            public SyntaxNodeOrToken NodeOrToken { get; }

            public AnnotationData(SyntaxNodeOrToken nodeOrToken)
            {
                NodeOrToken = nodeOrToken;
            }
        }

    }
}
