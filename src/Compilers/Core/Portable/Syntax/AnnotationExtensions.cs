// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    public static class AnnotationExtensions
    {
        /// <summary>
        /// Creates a new node identical to this node with the specified annotations attached.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be added to the new node.</param>
        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.WithAdditionalAnnotationsInternal(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations attached.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be added to the new node.</param>
        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.WithAdditionalAnnotationsInternal(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be removed from the new node.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.GetNodeWithoutAnnotations(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be removed from the new node.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.GetNodeWithoutAnnotations(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the annotations of the specified kind removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotationKind">The kind of annotation to remove.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, string annotationKind)
            where TNode : SyntaxNode
        {
            if (node.HasAnnotations(annotationKind))
            {
                return node.WithoutAnnotations<TNode>(node.GetAnnotations(annotationKind).ToArray());
            }
            else
            {
                return node;
            }
        }
    }
}
