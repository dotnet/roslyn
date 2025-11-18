// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        internal abstract class IdentifierUsedAsValueFinder
        {
            private LookupResult? _lookupResult;

            protected void Free()
            {
                _lookupResult?.Free();
            }

            protected bool CheckIdentifiersInNode(CSharpSyntaxNode? node, Binder binder)
            {
                if (node == null)
                {
                    return true;
                }

                var nodesOfInterest = node.DescendantNodesAndSelf(descendIntoChildren: childrenNeedChecking, descendIntoTrivia: false);

                foreach (var n in nodesOfInterest)
                {
                    Binder enclosingBinder = getEnclosingBinderForNode(contextNode: node, contextBinder: binder, n);

                    switch (n)
                    {
                        case AnonymousFunctionExpressionSyntax lambdaSyntax:
                            if (!CheckLambda(lambdaSyntax, enclosingBinder))
                            {
                                return false;
                            }

                            break;

                        case IdentifierNameSyntax id:

                            switch (id.Parent)
                            {
                                case MemberAccessExpressionSyntax memberAccess:
                                    if (memberAccess.Expression != id)
                                    {
                                        continue;
                                    }
                                    break;

                                case QualifiedNameSyntax qualifiedName:
                                    if (qualifiedName.Left != id)
                                    {
                                        continue;
                                    }
                                    break;

                                case AssignmentExpressionSyntax assignment:
                                    if (assignment.Left == id &&
                                        assignment.Parent?.Kind() is SyntaxKind.ObjectInitializerExpression or SyntaxKind.WithInitializerExpression)
                                    {
                                        continue;
                                    }
                                    break;
                            }

                            if (SyntaxFacts.IsInTypeOnlyContext(id) &&
                                !(id.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isExpression &&
                                    isExpression.Right == id))
                            {
                                continue;
                            }

                            if (!IsIdentifierOfInterest(id))
                            {
                                continue;
                            }

                            if (!CheckIdentifier(enclosingBinder, id))
                            {
                                return false;
                            }

                            break;

                        case QueryExpressionSyntax query:
                            if (!CheckQuery(query, enclosingBinder))
                            {
                                return false;
                            }

                            break;
                    }
                }

                return true;

                static Binder getEnclosingBinderForNode(CSharpSyntaxNode contextNode, Binder contextBinder, SyntaxNode targetNode)
                {
                    while (true)
                    {
                        Binder? enclosingBinder = contextBinder.GetBinder(targetNode);

                        if (enclosingBinder is not null)
                        {
                            return enclosingBinder;
                        }

                        if (targetNode == contextNode)
                        {
                            return contextBinder;
                        }

                        Debug.Assert(targetNode.Parent is not null);
                        targetNode = targetNode.Parent;
                    }
                }

                static bool childrenNeedChecking(SyntaxNode n)
                {
                    switch (n)
                    {
                        case MemberBindingExpressionSyntax:
                        case BaseExpressionColonSyntax:
                        case NameEqualsSyntax:
                        case GotoStatementSyntax { RawKind: (int)SyntaxKind.GotoStatement }:
                        case TypeParameterConstraintClauseSyntax:
                        case AliasQualifiedNameSyntax:
                            // These nodes do not have anything interesting for us
                            return false;

                        case AttributeListSyntax:
                            // References in attributes, if any, are errors
                            // Skip them
                            return false;

                        case ParameterSyntax:
                            // Same as attributes
                            return false;

                        case AnonymousFunctionExpressionSyntax:
                        case QueryExpressionSyntax:
                            // Lambdas need special handling
                            return false;

                        case ExpressionSyntax expression:
                            if (SyntaxFacts.IsInTypeOnlyContext(expression) &&
                                !(expression.Parent is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isExpression &&
                                    isExpression.Right == expression))
                            {
                                return false;
                            }
                            break;
                    }

                    return true;
                }
            }

            protected abstract bool IsIdentifierOfInterest(IdentifierNameSyntax id);

            private bool CheckLambda(AnonymousFunctionExpressionSyntax lambdaSyntax, Binder enclosingBinder)
            {
                UnboundLambda unboundLambda = enclosingBinder.AnalyzeAnonymousFunction(lambdaSyntax, BindingDiagnosticBag.Discarded);
                var lambdaBodyBinder = CreateLambdaBodyBinder(enclosingBinder, unboundLambda);
                return CheckIdentifiersInNode(lambdaSyntax.Body, lambdaBodyBinder.GetBinder(lambdaSyntax.Body) ?? lambdaBodyBinder);
            }

            private static ExecutableCodeBinder CreateLambdaBodyBinder(Binder enclosingBinder, UnboundLambda unboundLambda)
            {
                unboundLambda.HasExplicitReturnType(out RefKind refKind, out ImmutableArray<CustomModifier> refCustomModifiers, out TypeWithAnnotations returnType);
                var lambdaSymbol = new LambdaSymbol(
                                        enclosingBinder,
                                        enclosingBinder.Compilation,
                                        enclosingBinder.ContainingMemberOrLambda!,
                                        unboundLambda,
                                        ImmutableArray<TypeWithAnnotations>.Empty,
                                        ImmutableArray<RefKind>.Empty,
                                        refKind,
                                        refCustomModifiers,
                                        returnType);

                return new ExecutableCodeBinder(unboundLambda.Syntax, lambdaSymbol, unboundLambda.GetWithParametersBinder(lambdaSymbol, enclosingBinder));
            }

            private bool CheckIdentifier(Binder enclosingBinder, IdentifierNameSyntax id)
            {
                Debug.Assert(_lookupResult?.IsClear != false);
                _lookupResult ??= LookupResult.GetInstance();

                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                enclosingBinder.LookupIdentifier(_lookupResult, id, SyntaxFacts.IsInvoked(id), ref useSiteInfo);

                return CheckAndClearLookupResult(enclosingBinder, id, _lookupResult);
            }

            protected abstract bool CheckAndClearLookupResult(Binder enclosingBinder, IdentifierNameSyntax id, LookupResult lookupResult);

            protected static bool IsTypeOrValueReceiver(
                Binder enclosingBinder,
                IdentifierNameSyntax id,
                TypeSymbol type,
                [NotNullWhen(true)] out SyntaxNode? memberAccessNode,
                [NotNullWhen(true)] out string? memberName,
                out int targetMemberArity,
                out bool invoked)
            {
                memberAccessNode = null;
                memberName = null;
                targetMemberArity = 0;
                invoked = false;

                switch (id.Parent)
                {
                    case MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess when memberAccess.Expression == id:
                        var simpleName = memberAccess.Name;
                        memberAccessNode = simpleName;
                        memberName = simpleName.Identifier.ValueText;
                        targetMemberArity = simpleName.Arity;
                        invoked = SyntaxFacts.IsInvoked(memberAccess);
                        break;
                    case QualifiedNameSyntax qualifiedName when qualifiedName.Left == id:
                        simpleName = qualifiedName.Right;
                        memberAccessNode = simpleName;
                        memberName = simpleName.Identifier.ValueText;
                        targetMemberArity = simpleName.Arity;
                        invoked = false;
                        break;
                    case FromClauseSyntax { Parent: QueryExpressionSyntax query } fromClause when query.FromClause == fromClause && fromClause.Expression == id:
                        memberName = GetFirstInvokedMethodName(query, out memberAccessNode);
                        targetMemberArity = 0;
                        invoked = true;
                        break;
                }

                return memberAccessNode is not null && enclosingBinder.IsPotentialColorColorReceiver(id, type);
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.BindInstanceMemberAccess"/> and <see cref="Binder.BindMemberOfType"/>
            /// </summary>
            protected static bool TreatAsInstanceMemberAccess(
                Binder enclosingBinder,
                TypeSymbol type,
                SyntaxNode memberAccessNode,
                string memberName,
                int targetMemberArity,
                bool invoked,
                LookupResult lookupResult)
            {
                Debug.Assert(!type.IsDynamic());
                Debug.Assert(lookupResult.IsClear);

                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                enclosingBinder.LookupInstanceMember(lookupResult, type, leftIsBaseReference: false, memberName, targetMemberArity, invoked, ref useSiteInfo);

                bool treatAsInstanceMemberAccess;
                if (lookupResult.IsMultiViable)
                {
                    // This branch follows the logic of BindMemberOfType
                    Debug.Assert(lookupResult.Symbols.Any());

                    var members = ArrayBuilder<Symbol>.GetInstance();
                    Symbol symbol = enclosingBinder.GetSymbolOrMethodOrPropertyGroup(lookupResult, memberAccessNode, memberName, targetMemberArity, members, BindingDiagnosticBag.Discarded, wasError: out _,
                                                                                     qualifierOpt: null);

                    if ((object)symbol == null)
                    {
                        Debug.Assert(members.Count > 0);

                        bool haveInstanceCandidates;
                        lookupResult.Clear();
                        enclosingBinder.CheckWhatCandidatesWeHave(members, type, memberName, targetMemberArity, invoked,
                                                                  ref lookupResult, ref useSiteInfo,
                                                                  out haveInstanceCandidates, out _);

                        treatAsInstanceMemberAccess = haveInstanceCandidates;
                    }
                    else
                    {
                        Debug.Assert(symbol.Kind != SymbolKind.Method);
                        treatAsInstanceMemberAccess = !(symbol.IsStatic || symbol.Kind == SymbolKind.NamedType);
                    }

                    members.Free();
                }
                else
                {
                    // At this point this could only be an extension member access or an error
                    bool haveInstanceCandidates;
                    lookupResult.Clear();
                    var members = ArrayBuilder<Symbol>.GetInstance();

                    enclosingBinder.CheckWhatCandidatesWeHave(members, type, memberName, targetMemberArity, invoked,
                                                              ref lookupResult, ref useSiteInfo,
                                                              out haveInstanceCandidates, out _);

                    members.Free();
                    treatAsInstanceMemberAccess = haveInstanceCandidates;
                }

                lookupResult.Clear();
                return treatAsInstanceMemberAccess;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.BindQuery"/>
            /// </summary>
            private bool CheckQuery(QueryExpressionSyntax query, Binder enclosingBinder)
            {
                if (CheckIdentifiersInNode(query.FromClause.Expression, enclosingBinder))
                {
                    (QueryTranslationState state, _) = enclosingBinder.MakeInitialQueryTranslationState(query, BindingDiagnosticBag.Discarded);

                    bool result = BindQueryInternal(enclosingBinder, state);

                    for (QueryContinuationSyntax? continuation = query.Body.Continuation; continuation != null && result; continuation = continuation.Body.Continuation)
                    {
                        // A query expression with a continuation
                        //     from ... into x ...
                        // is translated into
                        //     from x in ( from ... ) ...
                        enclosingBinder.PrepareQueryTranslationStateForContinuation(state, continuation, BindingDiagnosticBag.Discarded);
                        result = BindQueryInternal(enclosingBinder, state);
                    }

                    state.Free();
                    return result;
                }

                return false;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.BindQueryInternal2"/>
            /// </summary>
            private bool BindQueryInternal(Binder enclosingBinder, QueryTranslationState state)
            {
                // we continue reducing the query until it is reduced away.
                do
                {
                    if (state.clauses.IsEmpty())
                    {
                        return FinalTranslation(enclosingBinder, state);
                    }
                }
                while (ReduceQuery(enclosingBinder, state));

                return false;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.FinalTranslation"/>
            /// </summary>
            private bool FinalTranslation(Binder enclosingBinder, QueryTranslationState state)
            {
                Debug.Assert(state.clauses.IsEmpty());
                switch (state.selectOrGroup.Kind())
                {
                    case SyntaxKind.SelectClause:
                        {
                            // A query expression of the form
                            //     from x in e select v
                            // is translated into
                            //     ( e ) . Select ( x => v )
                            var selectClause = (SelectClauseSyntax)state.selectOrGroup;
                            var x = state.rangeVariable;
                            var v = selectClause.Expression;
                            return MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, v);
                        }
                    case SyntaxKind.GroupClause:
                        {
                            // A query expression of the form
                            //     from x in e group v by k
                            // is translated into
                            //     ( e ) . GroupBy ( x => k , x => v )
                            // except when v is the identifier x, the translation is
                            //     ( e ) . GroupBy ( x => k )
                            var groupClause = (GroupClauseSyntax)state.selectOrGroup;
                            var x = state.rangeVariable;
                            var v = groupClause.GroupExpression;
                            var k = groupClause.ByExpression;

                            return MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, k) &&
                                   MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, v);
                        }
                    default:
                        {
                            // there should have been a syntax error if we get here.
                            return true;
                        }
                }
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceQuery"/>
            /// </summary>
            private bool ReduceQuery(Binder enclosingBinder, QueryTranslationState state)
            {
                var topClause = state.clauses.Pop();
                switch (topClause.Kind())
                {
                    case SyntaxKind.WhereClause:
                        return ReduceWhere(enclosingBinder, (WhereClauseSyntax)topClause, state);
                    case SyntaxKind.JoinClause:
                        return ReduceJoin(enclosingBinder, (JoinClauseSyntax)topClause, state);
                    case SyntaxKind.OrderByClause:
                        return ReduceOrderBy(enclosingBinder, (OrderByClauseSyntax)topClause, state);
                    case SyntaxKind.FromClause:
                        return ReduceFrom(enclosingBinder, (FromClauseSyntax)topClause, state);
                    case SyntaxKind.LetClause:
                        return ReduceLet(enclosingBinder, (LetClauseSyntax)topClause, state);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(topClause.Kind());
                }
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceWhere"/>
            /// </summary>
            private bool ReduceWhere(Binder enclosingBinder, WhereClauseSyntax where, QueryTranslationState state)
            {
                // A query expression with a where clause
                //     from x in e
                //     where f
                //     ...
                // is translated into
                //     from x in ( e ) . Where ( x => f )
                return MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, where.Condition);
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceJoin"/>
            /// </summary>
            private bool ReduceJoin(Binder enclosingBinder, JoinClauseSyntax join, QueryTranslationState state)
            {
                if (CheckIdentifiersInNode(join.InExpression, enclosingBinder) &&
                    MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, join.LeftExpression))
                {
                    var x2 = state.AddRangeVariable(enclosingBinder, join.Identifier, BindingDiagnosticBag.Discarded);

                    if (MakeQueryUnboundLambda(enclosingBinder, QueryTranslationState.RangeVariableMap(x2), x2, join.RightExpression))
                    {
                        if (join.Into != null)
                        {
                            state.allRangeVariables[x2].Free();
                            state.allRangeVariables.Remove(x2);

                            state.AddRangeVariable(enclosingBinder, join.Into.Identifier, BindingDiagnosticBag.Discarded);
                        }

                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceOrderBy"/>
            /// </summary>
            private bool ReduceOrderBy(Binder enclosingBinder, OrderByClauseSyntax orderby, QueryTranslationState state)
            {
                // A query expression with an orderby clause
                //     from x in e
                //     orderby k1 , k2 , ... , kn
                //     ...
                // is translated into
                //     from x in ( e ) . 
                //     OrderBy ( x => k1 ) . 
                //     ThenBy ( x => k2 ) .
                //     ... .
                //     ThenBy ( x => kn )
                //     ...
                // If an ordering clause specifies a descending direction indicator,
                // an invocation of OrderByDescending or ThenByDescending is produced instead.
                foreach (var ordering in orderby.Orderings)
                {
                    if (!MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), state.rangeVariable, ordering.Expression))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceFrom"/>
            /// </summary>
            private bool ReduceFrom(Binder enclosingBinder, FromClauseSyntax from, QueryTranslationState state)
            {
                var x1 = state.rangeVariable;
                if (MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x1, from.Expression))
                {
                    state.AddRangeVariable(enclosingBinder, from.Identifier, BindingDiagnosticBag.Discarded);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Follows the logic of <see cref="Binder.ReduceLet"/>
            /// </summary>
            private bool ReduceLet(Binder enclosingBinder, LetClauseSyntax let, QueryTranslationState state)
            {
                // A query expression with a let clause
                //     from x in e
                //     let y = f
                //     ...
                // is translated into
                //     from * in ( e ) . Select ( x => new { x , y = f } )
                //     ...
                var x = state.rangeVariable;

                if (MakeQueryUnboundLambda(enclosingBinder, state.RangeVariableMap(), x, let.Expression))
                {
                    state.rangeVariable = state.TransparentRangeVariable(enclosingBinder);
                    state.AddTransparentIdentifier(x.Name);
                    var y = state.AddRangeVariable(enclosingBinder, let.Identifier, BindingDiagnosticBag.Discarded);
                    state.allRangeVariables[y].Add(let.Identifier.ValueText);
                    return true;
                }

                return false;
            }

            private bool MakeQueryUnboundLambda(Binder enclosingBinder, RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression)
            {
                UnboundLambda unboundLambda = Binder.MakeQueryUnboundLambda(
                    expression,
                    new QueryUnboundLambdaState(
                        enclosingBinder, qvm, ImmutableArray.Create(parameter),
                        (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag diagnostics) => throw ExceptionUtilities.Unreachable()),
                    withDependencies: false);

                var lambdaBodyBinder = CreateLambdaBodyBinder(enclosingBinder, unboundLambda);
                return CheckIdentifiersInNode(expression, lambdaBodyBinder.GetRequiredBinder(expression));
            }
        }
    }
}
