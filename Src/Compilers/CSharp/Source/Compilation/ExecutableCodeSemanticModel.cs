using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    // TODO (tomat): Could we unify this for global code and methods - both should just use IEnumerable<StatementSyntax>?
    internal abstract partial class ExecutableCodeSemanticModel : MemberSemanticModel
    {
        private readonly Binder rootBinder;
        private readonly DiagnosticBag diagnostics = new DiagnosticBag();
        private readonly ReaderWriterLockSlim nodeMapLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly Dictionary<SyntaxNode, BoundNode> guardedNodeMap = new Dictionary<SyntaxNode, BoundNode>();

        protected ExecutableCodeSemanticModel(Compilation compilation, SyntaxNode rootNode, Symbol memberSymbol, Binder rootBinder)
            : base(compilation, rootNode, memberSymbol)
        {
            this.rootBinder = rootBinder;
        }

        internal override Binder RootBinder
        {
            get
            {
                return this.rootBinder;
            }
        }

        public override SyntaxTree SyntaxTree
        {
            get
            {
                return this.rootBinder.SyntaxTree;
            }
        }

        internal override SemanticInfo GetSemanticInfoWorker(SyntaxNode node, SemanticInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateSemanticInfoOptions(options);

            SyntaxNode bindableNode = this.GetBindableSyntaxNode(node);
            SyntaxNode bindableParent = this.GetBindableParentNode(bindableNode);

            // Special handling for the Color Color case.
            //
            // Suppose we have:
            // public class Color {
            //   public void M(int x) {}
            //   public static void M(params int[] x) {}
            // }
            // public class C {
            //   public void Test() {
            //     Color Color = new Color();
            //     System.Action<int> d = Color.M;
            //   }
            // }
            //
            // We actually don't know how to interpret the "Color" in "Color.M" until we
            // perform overload resolution on the method group.  Now, if we were getting
            // the semantic info for the method group, then bindableParent would be the
            // variable declarator "d = Color.M" and so we would be able to pull the result
            // of overload resolution out of the bound (method group) conversion.  However,
            // if we are getting the semantic info for just the "Color" part, then
            // bindableParent will be the member access, which doesn't have enough information
            // to determine which "Color" to use (since no overload resolution has been
            // performed).  We resolve this problem by detecting the case where we're looking
            // up the LHS of a member access and calling GetBindableParentNode one more time.
            // This gets us up to the level where the method group conversion occurs.
            if (bindableParent != null && bindableParent.Kind == SyntaxKind.MemberAccessExpression && ((MemberAccessExpressionSyntax)bindableParent).Expression == bindableNode)
            {
                bindableParent = this.GetBindableParentNode(bindableParent);
            }

            BoundNode boundParent = null;
            BoundNode boundNode = null;
            BoundNode highestBoundNode = null;

            if (bindableParent != null)
            {
                boundParent = this.GetBoundNode(bindableParent);
            }

            boundNode = this.GetBoundNode(bindableNode);

            if (boundParent != null)
            {
                highestBoundNode = this.GetBoundChild(boundParent, node);
            }
            else
            {
                highestBoundNode = boundNode;
            }

            return base.GetSemanticInfoForNode(bindableNode, options, boundNode, highestBoundNode, boundParent);
        }

        // In lambda binding scenarios we need to know two things: First,
        // what is the *innermost* lambda that contains the expression we're
        // interested in?  Second, what is the smallest expression that contains 
        // the *outermost* lambda that we can bind in order to get a sensible
        // lambda binding?
        //
        // For example, suppose we have the statement:
        //
        // A().B(x=>x.C(y=>y.D().E())).F().G();
        //
        // and the user wants binding information about method group "D".  We must know
        // the bindable expression that is outside of every lambda:
        //
        // A().B(x=>x.C(y=>y.D().E()))
        //
        // By binding that we can determine the type of lambda parameters "x" and "y" and
        // put that information in the bound tree. Once we know those facts then
        // we can obtain the binding object associated with the innermost lambda:
        //
        // y=>y.D().E()
        //
        // And use that binding to obtain the analysis of:
        //
        // y.D
        //
        private SyntaxNode GetInnermostLambdaOrQuery(SyntaxNode node, int position, bool allowStarting = false)
        {
            Debug.Assert(node != null);

            for (var current = node; current != this.Root; current = current.Parent)
            {
                // current can only become null if we somehow got past the root. The only way we
                // could have gotten past the root is to have started outside of it. That's
                // unexpected; the binding should only be asked to provide an opinion on syntax
                // nodes that it knows about.
                Debug.Assert(current != null, "Why are we being asked to find an enclosing lambda outside of our root?");

                if (!(current.IsAnonymousFunction() || current.IsQuery()))
                {
                    continue;
                }

                // If the position is not actually within the scope of the lambda, then keep
                // looking.
                if (!LookupPosition.IsInAnonymousFunctionOrQuery(position, current))
                {
                    continue;
                }

                // If we were asked for the innermost lambda enclosing a lambda then don't return
                // that; it's not enclosing anything. Only return the lambda if it's enclosing the
                // original node.

                if (!allowStarting && current == node)
                {
                    continue;
                }

                // If the lambda that is "enclosing" us is in fact enclosing an explicit lambda
                // parameter type then keep on going; that guy is logically bound outside of the
                // lambda. For example, if we have:
                //
                // D d = (Foo f)=>{int Foo; };
                //
                // Then the type "Foo" is bound in the context outside the lambda body, not inside
                // where it might get confused with local "Foo".
                if (NodeIsExplicitType(node, current))
                {
                    continue;
                }

                return current;
            }

            // If we made it to the root, then we are not "inside" a lambda even if the root is a
            // lambda. Remember, the point of this code is to get the binding that is associated
            // with the innermost lambda; if we are already in a binding associated with the
            // innermost lambda then we're done.
            return null;
        }

        private static bool NodeIsExplicitType(SyntaxNode node, SyntaxNode lambda)
        {
            Debug.Assert(node != null);
            Debug.Assert(lambda != null);
            Debug.Assert(lambda.IsAnonymousFunction() || lambda.IsQuery());

            // UNDONE;
            return false;
        }

        private SyntaxNode GetOutermostLambdaOrQuery(SyntaxNode node)
        {
            Debug.Assert(node != null);

            SyntaxNode lambda = null;
            for (var current = node; current != this.Root; current = current.Parent)
            {
                // (It is possible for the outermost lambda to be the node we were given.)
                if (current.IsAnonymousFunction() || current.IsQuery())
                {
                    lambda = current;
                }

                // current can only become null if we somehow got past the root. The only way we
                // could have gotten past the root is to have started outside of it. That's
                // unexpected; the binding should only be asked to provide an opinion on syntax
                // nodes that it knows about.
                Debug.Assert(current != null, "Why are we being asked to find an enclosing lambda outside of our root?");
            }

            // As above, if the root is a lambda then it does not count as the one "outside" of the
            // given node. We want the outermost lambda that we can find a binding for; if the root
            // of this binding is the lambda we're inside then "this" is the right binding.
            return lambda;
        }

        private BoundNode GetBoundNodeFromMap(SyntaxNode node)
        {
            using (nodeMapLock.DisposableRead())
            {
                BoundNode bound;
                return this.guardedNodeMap.TryGetValue(node, out bound)
                    ? bound
                    : null;
            }
        }

        // Simply add a syntax/bound pair to the map.
        private void AddBoundNodeToMap(SyntaxNode syntax, BoundNode bound)
        {
            using (nodeMapLock.DisposableWrite())
            {
                // Suppose we have bound a subexpression, say "x", and have cached the result in the
                // map. Later on, we bind the parent expression, "x + y" and end up re-binding x. In
                // this situation we do not want to clobber the existing "x" in the map because x
                // might be a complex expression that contains lambda symbols (or, equivalently,
                // lambda parameter symbols). We want to make sure we always get the same symbols
                // out of the cache every time we ask.
                if (!this.guardedNodeMap.ContainsKey(syntax))
                {
                    this.guardedNodeMap.Add(syntax, bound);
                }
            }
        }

        // Adds every syntax/bound pair in a tree rooted at the given bound node to the map, and the
        // performs a lookup of the given syntax node in the map. 
        private BoundNode AddBoundTreeAndGetBoundNodeFromMap(SyntaxNode syntax, BoundNode bound)
        {
            using (nodeMapLock.DisposableWrite())
            {
                NodeMapBuilder.AddToMap(bound, this.guardedNodeMap);
                BoundNode result;
                this.guardedNodeMap.TryGetValue(syntax, out result);
                return result;
            }
        }

        // We have a lambda; we want to find a syntax node or statement which can be bound such that
        // we can get the type of the lambda, if there is one. For example if given
        //
        // A().B(x=>M(x)).C();  then we want to find  A().B(x=>M())
        // object d = (D)(x=>M(x)); then we want to find (D)(x=>M(x))
        // D d = x=>M(x); then we want to find the whole thing.
        // 
        protected virtual SyntaxNode GetBindableSyntaxNodeOfLambdaOrQuery(SyntaxNode node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node != this.Root);
            Debug.Assert(node.IsAnonymousFunction() || node.IsQuery());

            SyntaxNode current = node.Parent;
            for (; current != this.Root; current = current.Parent)
            {
                Debug.Assert(current != null, "How did we get outside the root?");

                if (current is StatementSyntax)
                {
                    return current;
                }

                if (current.Kind == SyntaxKind.ParenthesizedExpression)
                {
                    continue;
                }

                if (current is ExpressionSyntax)
                {
                    return GetBindableSyntaxNode(current);
                }
            }

            // We made it up to the root without finding a viable expression or statement. Just bind
            // the lambda and hope for the best.
            return node;
        }

        // We might not have actually been given a bindable expression or statement; the caller can
        // give us variable declaration nodes, for example. If we're not at an expression or
        // statement, back up until we find one.
        private SyntaxNode GetExpressionOrStatement(SyntaxNode node)
        {
            Debug.Assert(node != null);

            for (SyntaxNode current = node; current != this.Root; current = current.Parent)
            {
                Debug.Assert(current != null, "How did we get outside the root?");

                if (current is StatementSyntax)
                {
                    return current;
                }

                if (current is ExpressionSyntax)
                {
                    return GetBindableSyntaxNode(current);
                }
            }

            // We made it up to the root without finding a viable expression or statement. Just bind
            // what we've got and hope for the best.
            return node;
        }

        // We want the binder in which this syntax node is going to be bound, NOT the binder which
        // this syntax node *produces*. That is, suppose we have
        //
        // void M() { int x; { int y; { int z; } } } 
        //
        // We want the enclosing binder of the syntax node for { int z; }.  We do not want the binder
        // that has local z, but rather the binder that has local y. The inner block is going to be
        // bound in the context of its enclosing binder; it's contents are going to be bound in the
        // context of its binder.
        internal override Binder GetEnclosingBinder(int position)
        {
            AssertPositionAdjusted(position);

            // If we have a root binder with no tokens in it, position can be outside the span event
            // after position is adjusted. If this happens, there can't be any 
            if (!this.Root.FullSpan.Contains(position))
                return RootBinder;

            SyntaxToken token = this.Root.FindToken(position);
            SyntaxNode node = token.Parent;

            SyntaxNode innerLambda = GetInnermostLambdaOrQuery(node, position, allowStarting: true);

            // There are three possible scenarios here.
            //
            // 1) the node is outside all lambdas in this context, or
            // 2) The node is an outermost lambda in this context, or
            // 3) the node is inside the outermost lambda in this context.
            //
            // In the first case, no lambdas are involved at all so let's just fall back on the
            // original enclosing binder code.
            //
            // In the second case, we have been asked to bind an entire lambda and we know it to be
            // the outermost lambda in this context. Therefore the enclosing binder is going to be
            // the enclosing binder of this expression. However, we do not simply want to say
            // "here's the enclosing binder":
            // 
            // void M() { Func<int, int> f = x=>x+1; }
            //
            // We should step out to the enclosing statement or expression, if there is one, and
            // bind that.

            if (innerLambda == null)
            {
                return base.GetEnclosingBinder(node, position);
            }

            // In the third case, we're in a child lambda. Have we already cached a binder for it?
            // If not, bind the outermost expression containing the lambda and then fill in the map.
            BoundNode boundInnerLambda = this.GetBoundNodeFromMap(innerLambda);
            SyntaxNode outerExpression = null;
            if (boundInnerLambda == null)
            {
                SyntaxNode outerLambda = GetOutermostLambdaOrQuery(innerLambda);
                Debug.Assert(outerLambda != null);
                Debug.Assert(outerLambda != this.Root);
                outerExpression = GetBindableSyntaxNodeOfLambdaOrQuery(outerLambda);
                BoundNode boundOuterExpression = this.Bind(base.GetEnclosingBinder(outerExpression, position), outerExpression, this.diagnostics);
                boundInnerLambda = AddBoundTreeAndGetBoundNodeFromMap(innerLambda, boundOuterExpression);
            }

            // If there is a bug in the binder such that we "lose" a subexpression containing a
            // lambda, and never put bound state for it into the bound tree, then the bound lambda
            // that comes back from the map lookup will be null. This can occur in error recovery
            // situations.  If it is null, we fall back to the outer binder.
            if (boundInnerLambda == null || (boundInnerLambda.Kind != BoundKind.Lambda && boundInnerLambda.Kind != BoundKind.QueryClause))
            {
                return base.GetEnclosingBinder(node, position);
            }

            switch (boundInnerLambda.Kind)
            {
                case BoundKind.UnboundLambda:
                    boundInnerLambda = ((UnboundLambda)boundInnerLambda).BindForErrorRecovery();
                    goto case BoundKind.Lambda;
                case BoundKind.Lambda:
                    return GetLambdaEnclosingBinder(position, node, innerLambda, ((BoundLambda)boundInnerLambda).Binder);
                case BoundKind.QueryClause:
                    return GetQueryEnclosingBinder(position, node, ((BoundQueryClause)boundInnerLambda));
                default:
                    return base.GetEnclosingBinder(node, position);
            }
        }

        private Binder GetQueryEnclosingBinder(int position, SyntaxNode node, BoundQueryClause queryClause)
        {
            for (BoundNode n = queryClause.Value; n != null; )
            {
                switch (n.Kind)
                {
                    case BoundKind.QueryClause:
                        queryClause = (BoundQueryClause)n;
                        n = queryClause.Value;
                        continue;
                    case BoundKind.Call:
                        var call = (BoundCall)n;
                        if (call == null || call.Arguments.Count == 0) return queryClause.Binder;
                        // TODO: should we look for the "nearest" argument as a fallback?
                        n = call.Arguments[call.Arguments.Count - 1];
                        foreach (var arg in call.Arguments)
                        {
                            if (arg.Syntax.FullSpan.Contains(position)) n = arg;
                        }
                        continue;
                    case BoundKind.Conversion:
                        n = ((BoundConversion)n).Operand;
                        continue;
                    case BoundKind.UnboundLambda:
                        return ((UnboundLambda)n).BindForErrorRecovery().Binder;
                    case BoundKind.Lambda:
                        return ((BoundLambda)n).Binder;
                    default:
                        return queryClause.Binder;
                }
            }

            return queryClause.Binder;
        }

        /// <summary>
        /// Performs the same function as GetEnclosingBinder, but is known to take place within a
        /// specified lambda.  Walks up the syntax hierarchy until a node with an associated binder
        /// is found.
        /// </summary>
        /// <remarks>
        /// CONSIDER: can this share code with MemberSemanticModel.GetEnclosingBinder?
        /// </remarks>
        private Binder GetLambdaEnclosingBinder(int position, SyntaxNode startingNode, SyntaxNode containingLambda, ExecutableCodeBinder lambdaBinder)
        {
            AssertPositionAdjusted(position);
            Debug.Assert(containingLambda.IsAnonymousFunction());
            Debug.Assert(LookupPosition.IsInAnonymousFunctionOrQuery(position, containingLambda));

            var current = startingNode;
            while (current != containingLambda)
            {
                Debug.Assert(current != null);

                StatementSyntax stmt = current as StatementSyntax;
                if (stmt != null)
                {
                    if (LookupPosition.IsInStatementScope(position, stmt))
                    {
                        Binder binder = lambdaBinder.GetBinder(current);
                        if (binder != null)
                        {
                            return binder;
                        }
                    }
                }
                else if (current.Kind == SyntaxKind.CatchClause)
                {
                    if (LookupPosition.IsInCatchClauseScope(position, (CatchClauseSyntax)current))
                    {
                        Binder binder = lambdaBinder.GetBinder(current);
                        if (binder != null)
                        {
                            return binder;
                        }
                    }
                }
                else if (current.IsAnonymousFunction())
                {
                    if (LookupPosition.IsInAnonymousFunctionOrQuery(position, current))
                    {
                        Binder binder = lambdaBinder.GetBinder(current);
                        if (binder != null)
                        {
                            return binder;
                        }
                    }
                }
                else
                {
                    // If this ever breaks, make sure that all callers of
                    // CanHaveAssociatedLocalBinder are in sync.
                    Debug.Assert(!current.CanHaveAssociatedLocalBinder());
                }

                current = current.Parent;
            }

            return lambdaBinder;
        }

        internal override BoundNode GetBoundNode(SyntaxNode node)
        {
            // If this method is called with a null parameter, that implies that the Root should be
            // bound, but make sure that the Root is bindable.
            if (node == null)
            {
                node = GetBindableSyntaxNode(Root);
            }

            Debug.Assert(node == GetBindableSyntaxNode(node));

            // We have one SemanticModel for each method.
            //
            // The SemanticModel contains a lazily-built immutable map from scope-introducing 
            // syntactic statements (such as blocks) to binders, but not from lambdas to binders.
            //
            // The SemanticModel also contains a mutable map from syntax to bound nodes; that is 
            // declared here. Since the map is not thread-safe we ensure that it is guarded with a
            // reader-writer lock.
            //
            // Have we already got the desired bound node in the mutable map? If so, return it.
            BoundNode result = GetBoundNodeFromMap(node);
            if (result != null)
            {
                return result;
            }

            // We didn't have it in the map. Obtain a binder suitable for obtaining the answer.
            Binder binder = GetEnclosingBinder(GetAdjustedNodePosition(node));
            Debug.Assert(binder != null);

            // If the binder we just obtained is for a child lambda then by obtaining the binder we
            // might have as a side effect caused the map to be populated with the answer we seek.
            // Check again.
            result = GetBoundNodeFromMap(node);
            if (result != null)
            {
                return result;
            }

            // We might not actually have been given an expression or statement even though we were
            // allegedly given something that is "bindable".
            SyntaxNode nodeToBind = GetExpressionOrStatement(node);

            // We are in one of the scenarios where binding the lambda does not populate the map
            // with the desired node. Bind the node in the binder, and then add to the map every
            // syntax/bound node pair in the resulting bound node and its children.
            //
            // It is possible that we're being asked to bind an entire outermost lambda. For example:
            //
            // void M() { Func<int, int> f = x=>x+1; }
            //                     bind this ^----^
            //
            // In that case the enclosing binder is the body binder for M, but we do not simply want
            // to bind the expression "x=>x+1". Rather, we want to bind the whole statement
            // enclosing it, so that we have the right type for x.

            if (nodeToBind.IsAnonymousFunction() && GetInnermostLambdaOrQuery(nodeToBind, node.Span.Start) == null)
            {
                nodeToBind = GetBindableSyntaxNodeOfLambdaOrQuery(nodeToBind);
            }

            BoundNode boundRoot = this.Bind(binder, nodeToBind, this.diagnostics);
            Debug.Assert(boundRoot != null);
            result = AddBoundTreeAndGetBoundNodeFromMap(node, boundRoot);

            // Special case:
            //
            // We might be in a "Color Color" scenario. That is, suppose we have been asked to bind
            // "Color" in "x = Color.Red;" or in "x = Color.ToString();"  The former could be the
            // type "Color" and the latter could be the property "this.Color" or a local variable
            // named Color of type Color.
            //
            // We've just bound "Color"; perhaps it resolved to be the property. Now bind
            // "Color.Red" and re-do the mapping. That might replace the node in the map with the
            // right value, or it might leave it the same, or it might leave it untouched, if the
            // original syntax node does not appear in the new bound tree. Either way, we'll have
            // more accurate information in the tree for this case.
            if (node != this.Root &&
                node.Parent != null &&
                node.Parent.Kind == SyntaxKind.MemberAccessExpression &&
                node.Kind == SyntaxKind.IdentifierName)
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Parent;
                if (memberAccess.Name.Kind == SyntaxKind.IdentifierName)
                {
                    BoundNode boundMemberAccess = this.Bind(binder, memberAccess, this.diagnostics);
                    result = AddBoundTreeAndGetBoundNodeFromMap(node, boundMemberAccess);
                }
            }

            // It is *still* possible that we haven't gotten the result. For example, consider
            // something like "int x = (a + b) * c;"  If we ask for binding information on the
            // parenthetical expression then we'll get back a bound node for the syntax "a + b", not
            // the syntax "(a + b)"; the binder generates no bound state for the parenthetical. The
            // best we can do in this bad situation is to use the expression we just bound.  Add it
            // to the map in case we're asked again.
            if (result == null)
            {
                AddBoundNodeToMap(node, boundRoot);
                result = boundRoot;
            }

            Debug.Assert(result != null);
            return result;
        }
    }
}