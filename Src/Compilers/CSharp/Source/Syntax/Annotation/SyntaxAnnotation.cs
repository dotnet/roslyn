#if false
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp.InternalSyntax;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Annotates syntax with additional information. As syntax elements are immutable they can not actually be
    /// annotated with information. However, new syntax elements can be created from an existing syntax element and an
    /// annotation to return an entirely new syntax element with that associated annotation.
    /// 
    /// Annotations do not survive across changes made to a <see cref="SyntaxTree"/> with <see 
    /// cref="SyntaxTree.WithChange"/>. However, they will survive manual rewrites of a tree using
    /// <see cref="SyntaxRewriter{T}"/> or if a tree is explicitly created using annotated syntax
    /// elements.
    /// </summary>
    public partial class SyntaxAnnotation : ISyntaxAnnotation
    {
        /// <summary>
        /// Adds this annotation to the given syntax node, creating a new syntax node of the same type with the
        /// annotation on it.
        /// </summary>
        public T AddAnnotationTo<T>(T node) where T : SyntaxNode
        {
            if (node != null)
            {
                return (T)node.Green.WithAdditionalAnnotations(this).ToRed();
            }

            return null;
        }

        /// <summary>
        /// Adds this annotation to a given syntax token, creating a new syntax token of the same type with the
        /// annotation on it.
        /// </summary>
        public SyntaxToken AddAnnotationTo(SyntaxToken token)
        {
            if (token.Node != null)
            {
                return new SyntaxToken(parent: null, node: token.Node.WithAdditionalAnnotations(this), position: 0, index: 0);
            }

            return default(SyntaxToken);
        }

        /// <summary>
        /// Adds this annotation to the given syntax trivia, creating a new syntax trivia of the same type with the
        /// annotation on it.
        /// </summary>
        public SyntaxTrivia AddAnnotationTo(SyntaxTrivia trivia)
        {
            if (trivia.UnderlyingNode != null)
            {
                return new SyntaxTrivia(token: default(SyntaxToken), node: trivia.UnderlyingNode.WithAdditionalAnnotations(this), position: 0, index: 0);
            }

            return default(SyntaxTrivia);
        }

        /// <summary>
        /// Finds all nodes with this annotation attached, that are on or under node.
        /// </summary>
        public IEnumerable<SyntaxNodeOrToken> FindAnnotatedNodesOrTokens(SyntaxNode root)
        {
            if (root != null)
            {
                return NodeOrTokenResolver.Resolve(root, this);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNodeOrToken>();
        }

        /// <summary>
        /// Finds all trivia with this annotation attached, that are on or under node.
        /// </summary>
        public IEnumerable<SyntaxTrivia> FindAnnotatedTrivia(SyntaxNode root)
        {
            if (root != null)
            {
                return TriviaResolver.Resolve(root, this);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
        }

        /// <summary>
        /// Finds any annotations that are on node "from", and then attaches them to node "to",
        /// returning a new node with the annotations attached. 
        /// 
        /// If no annotations are copied, just returns "to".
        /// 
        /// It can also be used manually to preserve annotations in a more complex tree
        /// modification, even if the type of a node changes.
        /// </summary>
        public static T CopyAnnotations<T>(SyntaxNode from, T to) where T : SyntaxNode
        {
            if (from == null || to == null)
            {
                return default(T);
            }

            var annotations = from.Green.GetAnnotations();
            if (annotations == null || annotations.Length == 0)
            {
                return to;
            }

            return (T)to.Green.WithAdditionalAnnotations(annotations).ToRed();
        }

        /// <summary>
        /// Finds any annotations that are on token "from", and then attaches them to token "to",
        /// returning a new token with the annotations attached. 
        /// 
        /// If no annotations are copied, just returns "to".
        /// </summary>
        public static SyntaxToken CopyAnnotations(SyntaxToken from, SyntaxToken to)
        {
            if (from.Node == null || to.Node == null)
            {
                return default(SyntaxToken);
            }

            var annotations = from.Node.GetAnnotations();
            if (annotations == null || annotations.Length == 0)
            {
                return to;
            }

            return new SyntaxToken(parent: null, node: to.Node.WithAdditionalAnnotations(annotations), position: 0, index: 0);
        }

        /// <summary>
        /// Finds any annotations that are on trivia "from", and then attaches them to trivia "to",
        /// returning a new trivia with the annotations attached. 
        /// 
        /// If no annotations are copied, just returns "to".
        /// </summary>
        public static SyntaxTrivia CopyAnnotations(SyntaxTrivia from, SyntaxTrivia to)
        {
            if (from.UnderlyingNode == null || to.UnderlyingNode == null)
            {
                return default(SyntaxTrivia);
            }

            var annotations = from.UnderlyingNode.GetAnnotations();
            if (annotations == null || annotations.Length == 0)
            {
                return to;
            }

            return new SyntaxTrivia(token: default(SyntaxToken), node: to.UnderlyingNode.WithAdditionalAnnotations(annotations), position: 0, index: 0);
        }

        #region ISyntaxAnnotation

        T ISyntaxAnnotation.AddAnnotationTo<T>(T node)
        {
            var syntaxNode = node as SyntaxNode;
            return this.AddAnnotationTo(syntaxNode) as T;
        }

        CommonSyntaxToken ISyntaxAnnotation.AddAnnotationTo(CommonSyntaxToken token)
        {
            return this.AddAnnotationTo((SyntaxToken)token);
        }

        CommonSyntaxTrivia ISyntaxAnnotation.AddAnnotationTo(CommonSyntaxTrivia trivia)
        {
            return this.AddAnnotationTo((SyntaxTrivia)trivia);
        }

        IEnumerable<CommonSyntaxNodeOrToken> ISyntaxAnnotation.FindAnnotatedNodesOrTokens(CommonSyntaxNode root)
        {
            return this.FindAnnotatedNodesOrTokens((SyntaxNode)root).Select(i => (CommonSyntaxNodeOrToken)i);
        }

        IEnumerable<CommonSyntaxTrivia> ISyntaxAnnotation.FindAnnotatedTrivia(CommonSyntaxNode root)
        {
            return this.FindAnnotatedTrivia((SyntaxNode)root).Select(t => (CommonSyntaxTrivia)t);
        }

        #endregion
    }
}
#endif