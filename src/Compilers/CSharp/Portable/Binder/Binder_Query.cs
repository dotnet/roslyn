// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts a QueryExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        private const string transparentIdentifierPrefix = "<>h__TransparentIdentifier";

        internal BoundExpression BindQuery(QueryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureQueryExpression.CheckFeatureAvailability(diagnostics, node.FromClause.FromKeyword);

            var fromClause = node.FromClause;
            var boundFromExpression = BindLeftOfPotentialColorColorMemberAccess(fromClause.Expression, diagnostics);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (boundFromExpression.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, fromClause.Expression.Location);
                boundFromExpression = BadExpression(fromClause.Expression, boundFromExpression);
            }
            else
            {
                boundFromExpression = BindToNaturalType(boundFromExpression, diagnostics);
            }

            (QueryTranslationState state, RangeVariableSymbol x) = MakeInitialQueryTranslationState(node, diagnostics);
            state.fromExpression = MakeMemberAccessValue(boundFromExpression, diagnostics);
#if DEBUG
            state.nextInvokedMethodName = GetFirstInvokedMethodName(node, out _);
#endif 

            // A from clause that explicitly specifies a range variable type
            //     from T x in e
            // is translated into
            //     from x in ( e ) . Cast < T > ( )
            BoundExpression? cast = null;
            if (fromClause.Type != null)
            {
                var typeRestriction = BindTypeArgument(fromClause.Type, diagnostics);
                cast = MakeQueryInvocation(fromClause, state.fromExpression, receiverIsCheckedForRValue: false, "Cast", fromClause.Type, typeRestriction, diagnostics
#if DEBUG
                    , state.nextInvokedMethodName
#endif
                    );
                state.fromExpression = cast;
#if DEBUG
                state.nextInvokedMethodName = null;
#endif
            }

            state.fromExpression = MakeQueryClause(fromClause, state.fromExpression, x, castInvocation: cast);
            BoundExpression result = BindQueryInternal1(state, diagnostics);
            for (QueryContinuationSyntax? continuation = node.Body.Continuation; continuation != null; continuation = continuation.Body.Continuation)
            {
                // A query expression with a continuation
                //     from ... into x ...
                // is translated into
                //     from x in ( from ... ) ...
                x = PrepareQueryTranslationStateForContinuation(state, continuation, diagnostics);
                state.fromExpression = result;

                result = BindQueryInternal1(state, diagnostics);
                result = MakeQueryClause(continuation.Body, result, x);
                result = MakeQueryClause(continuation, result, x);
            }

            state.Free();
            return MakeQueryClause(node, result);
        }

        private (QueryTranslationState, RangeVariableSymbol) MakeInitialQueryTranslationState(QueryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var fromClause = node.FromClause;
            var state = new QueryTranslationState();

            RangeVariableSymbol x = state.rangeVariable = state.AddRangeVariable(this, fromClause.Identifier, diagnostics);
            for (int i = node.Body.Clauses.Count - 1; i >= 0; i--)
            {
                state.clauses.Push(node.Body.Clauses[i]);
            }

            state.selectOrGroup = node.Body.SelectOrGroup;

            return (state, x);
        }

        private RangeVariableSymbol PrepareQueryTranslationStateForContinuation(QueryTranslationState state, QueryContinuationSyntax continuation, BindingDiagnosticBag diagnostics)
        {
            RangeVariableSymbol x;
            state.Clear();
            x = state.rangeVariable = state.AddRangeVariable(this, continuation.Identifier, diagnostics);
            Debug.Assert(state.clauses.IsEmpty());
            var clauses = continuation.Body.Clauses;
            for (int i = clauses.Count - 1; i >= 0; i--)
            {
                state.clauses.Push(clauses[i]);
            }

            state.selectOrGroup = continuation.Body.SelectOrGroup;

            return x;
        }

        private static string GetFirstInvokedMethodName(QueryExpressionSyntax query, out SyntaxNode correspondingAccessNode)
        {
            if (query.FromClause.Type != null)
            {
                correspondingAccessNode = query.FromClause;
                return "Cast";
            }
            else if (query.Body.Clauses.FirstOrDefault() is QueryClauseSyntax firstClause)
            {
                correspondingAccessNode = firstClause;

                switch (firstClause.Kind())
                {
                    case SyntaxKind.FromClause:
                        return "SelectMany";
                    case SyntaxKind.LetClause:
                        return "Select";
                    case SyntaxKind.WhereClause:
                        return "Where";
                    case SyntaxKind.JoinClause:
                        return ((JoinClauseSyntax)firstClause).Into == null ? "Join" : "GroupJoin";
                    case SyntaxKind.OrderByClause:
                        var firstOrdering = ((OrderByClauseSyntax)firstClause).Orderings.First();
                        return firstOrdering.IsKind(SyntaxKind.DescendingOrdering) ? "OrderByDescending" : "OrderBy";
                    default:
                        throw ExceptionUtilities.UnexpectedValue(firstClause.Kind());
                }
            }
            else
            {
                correspondingAccessNode = query.Body.SelectOrGroup;

                switch (query.Body.SelectOrGroup.Kind())
                {
                    case SyntaxKind.SelectClause:
                        return "Select";
                    case SyntaxKind.GroupClause:
                        return "GroupBy";
                    default:
                        throw ExceptionUtilities.UnexpectedValue(query.Body.SelectOrGroup.Kind());
                }
            }
        }

        private BoundExpression BindQueryInternal1(QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            // If the query is a degenerate one the form "from x in e select x", but in source,
            // then we go ahead and generate the select anyway.  We do this by skipping BindQueryInternal2,
            // whose job it is to (reduce away the whole query and) optimize away degenerate queries.
            return IsDegenerateQuery(state) ? FinalTranslation(state, diagnostics) : BindQueryInternal2(state, diagnostics);
        }

        private static bool IsDegenerateQuery(QueryTranslationState state)
        {
            if (!state.clauses.IsEmpty()) return false;

            // A degenerate query is of the form "from x in e select x".
            var select = state.selectOrGroup as SelectClauseSyntax;
            if (select == null) return false;
            var name = select.Expression as IdentifierNameSyntax;
            return name != null && state.rangeVariable.Name == name.Identifier.ValueText;
        }

        private BoundExpression BindQueryInternal2(QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            // we continue reducing the query until it is reduced away.
            while (true)
            {
                if (state.clauses.IsEmpty())
                {
                    if (state.selectOrGroup == null)
                    {
                        return state.fromExpression; // already reduced away
                    }
                    if (IsDegenerateQuery(state))
                    {
                        // A query expression of the form
                        //     from x in e select x
                        // is translated into
                        //     ( e )
                        var result = state.fromExpression;

                        // ignore missing or malformed Select method
                        BoundExpression? unoptimized = FinalTranslation(state, BindingDiagnosticBag.Discarded);

                        if (unoptimized.HasAnyErrors && !result.HasAnyErrors) unoptimized = null;
                        return MakeQueryClause(state.selectOrGroup, result, unoptimizedForm: unoptimized);
                    }

                    return FinalTranslation(state, diagnostics);
                }

                ReduceQuery(state, diagnostics);
            }
        }

        private BoundExpression FinalTranslation(QueryTranslationState state, BindingDiagnosticBag diagnostics)
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
                        var e = state.fromExpression;
                        var v = selectClause.Expression;
                        var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x, v, diagnostics.AccumulatesDependencies);
                        var result = MakeQueryInvocation(state.selectOrGroup, e, receiverIsCheckedForRValue: false, "Select", lambda, diagnostics
#if DEBUG
                                        , state.nextInvokedMethodName
#endif
                                        );
#if DEBUG
                        state.nextInvokedMethodName = null;
#endif
                        return MakeQueryClause(selectClause, result, queryInvocation: result);
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
                        var e = state.fromExpression;
                        var v = groupClause.GroupExpression;
                        var k = groupClause.ByExpression;
                        var vId = v as IdentifierNameSyntax;
                        BoundCall result;
                        var lambdaLeft = MakeQueryUnboundLambda(state.RangeVariableMap(), x, k, diagnostics.AccumulatesDependencies);

                        // this is the unoptimized form (when v is not the identifier x)
                        var d = BindingDiagnosticBag.GetInstance(diagnostics);
                        BoundExpression lambdaRight = MakeQueryUnboundLambda(state.RangeVariableMap(), x, v, diagnostics.AccumulatesDependencies);
                        result = MakeQueryInvocation(state.selectOrGroup, e, receiverIsCheckedForRValue: false, "GroupBy", ImmutableArray.Create(lambdaLeft, lambdaRight), d
#if DEBUG
                            , state.nextInvokedMethodName
#endif
                            );
#if DEBUG
                        state.nextInvokedMethodName = null;
#endif
                        // k and v appear reversed in the invocation, so we reorder their evaluation
                        result = ReverseLastTwoParameterOrder(result);

                        BoundExpression? unoptimizedForm = null;
                        if (vId != null && vId.Identifier.ValueText == x.Name)
                        {
                            // The optimized form.  We store the unoptimized form for analysis
                            unoptimizedForm = result;
                            result = MakeQueryInvocation(state.selectOrGroup, e, receiverIsCheckedForRValue: true, "GroupBy", lambdaLeft, diagnostics
#if DEBUG
                                , state.nextInvokedMethodName
#endif
                                );
#if DEBUG
                            state.nextInvokedMethodName = null;
#endif
                            if (unoptimizedForm.HasAnyErrors && !result.HasAnyErrors) unoptimizedForm = null;
                        }
                        else
                        {
                            diagnostics.AddRange(d);
                        }

                        d.Free();
                        return MakeQueryClause(groupClause, result, queryInvocation: result, unoptimizedForm: unoptimizedForm);
                    }
                default:
                    {
                        // there should have been a syntax error if we get here.
                        Debug.Assert(state.fromExpression.Type is { });
                        return new BoundBadExpression(
                            state.selectOrGroup, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol?>.Empty,
                            ImmutableArray.Create(state.fromExpression), state.fromExpression.Type);
                    }
            }
        }

        private static BoundCall ReverseLastTwoParameterOrder(BoundCall result)
        {
            // The input call has its arguments in the appropriate order for the invocation, but its last
            // two argument expressions appear in the reverse order from which they appeared in source.
            // Since we want region analysis to see them in source order, we rewrite the call so that these
            // two arguments are evaluated in source order.
            int n = result.Arguments.Length;
            var arguments = ArrayBuilder<BoundExpression>.GetInstance();
            arguments.AddRange(result.Arguments);
            var lastArgument = arguments[n - 1];
            arguments[n - 1] = arguments[n - 2];
            arguments[n - 2] = lastArgument;
            var argsToParams = ArrayBuilder<int>.GetInstance();
            argsToParams.AddRange(Enumerable.Range(0, n));
            argsToParams[n - 1] = n - 2;
            argsToParams[n - 2] = n - 1;
            var defaultArguments = result.DefaultArguments.Clone();
            (defaultArguments[n - 1], defaultArguments[n - 2]) = (defaultArguments[n - 2], defaultArguments[n - 1]);

            return result.Update(
                result.ReceiverOpt, result.InitialBindingReceiverIsSubjectToCloning, result.Method, arguments.ToImmutableAndFree(), argumentNamesOpt: default,
                argumentRefKindsOpt: default, result.IsDelegateCall, result.Expanded, result.InvokedAsExtensionMethod,
                argsToParams.ToImmutableAndFree(), defaultArguments, result.ResultKind, result.OriginalMethodsOpt, result.Type);
        }

        private void ReduceQuery(QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            var topClause = state.clauses.Pop();
            switch (topClause.Kind())
            {
                case SyntaxKind.WhereClause:
                    ReduceWhere((WhereClauseSyntax)topClause, state, diagnostics);
                    break;
                case SyntaxKind.JoinClause:
                    ReduceJoin((JoinClauseSyntax)topClause, state, diagnostics);
                    break;
                case SyntaxKind.OrderByClause:
                    ReduceOrderBy((OrderByClauseSyntax)topClause, state, diagnostics);
                    break;
                case SyntaxKind.FromClause:
                    ReduceFrom((FromClauseSyntax)topClause, state, diagnostics);
                    break;
                case SyntaxKind.LetClause:
                    ReduceLet((LetClauseSyntax)topClause, state, diagnostics);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(topClause.Kind());
            }
        }

        private void ReduceWhere(WhereClauseSyntax where, QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            // A query expression with a where clause
            //     from x in e
            //     where f
            //     ...
            // is translated into
            //     from x in ( e ) . Where ( x => f )
            var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, where.Condition, diagnostics.AccumulatesDependencies);
            var invocation = MakeQueryInvocation(where, state.fromExpression, receiverIsCheckedForRValue: false, "Where", lambda, diagnostics
#if DEBUG
                , state.nextInvokedMethodName
#endif
                );
#if DEBUG
            state.nextInvokedMethodName = null;
#endif
            state.fromExpression = MakeQueryClause(where, invocation, queryInvocation: invocation);
        }

        private void ReduceJoin(JoinClauseSyntax join, QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            var inExpression = BindRValueWithoutTargetType(join.InExpression, diagnostics);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (inExpression.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, join.InExpression.Location);
                inExpression = BadExpression(join.InExpression, inExpression);
            }

            BoundExpression? castInvocation = null;
            if (join.Type != null)
            {
                // A join clause that explicitly specifies a range variable type
                //     join T x in e on k1 equals k2
                // is translated into
                //     join x in ( e ) . Cast < T > ( ) on k1 equals k2
                var castType = BindTypeArgument(join.Type, diagnostics);
                castInvocation = MakeQueryInvocation(join, inExpression, receiverIsCheckedForRValue: false, "Cast", join.Type, castType, diagnostics
#if DEBUG
                    , expectedMethodName: null
#endif
                    );
                inExpression = castInvocation;
            }

            var outerKeySelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, join.LeftExpression, diagnostics.AccumulatesDependencies);

            var x1 = state.rangeVariable;
            var x2 = state.AddRangeVariable(this, join.Identifier, diagnostics);
            var innerKeySelectorLambda = MakeQueryUnboundLambda(QueryTranslationState.RangeVariableMap(x2), x2, join.RightExpression, diagnostics.AccumulatesDependencies);

            if (state.clauses.IsEmpty() && state.selectOrGroup.Kind() == SyntaxKind.SelectClause)
            {
                var select = (SelectClauseSyntax)state.selectOrGroup;
                BoundCall invocation;
                if (join.Into == null)
                {
                    // A query expression with a join clause without an into followed by a select clause
                    //     from x1 in e1
                    //     join x2 in e2 on k1 equals k2
                    //     select v
                    // is translated into
                    //     ( e1 ) . Join( e2 , x1 => k1 , x2 => k2 , ( x1 , x2 ) => v )
                    var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), select.Expression, diagnostics.AccumulatesDependencies);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        receiverIsCheckedForRValue: false,
                        "Join",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics
#if DEBUG
                        , state.nextInvokedMethodName
#endif
                        );
#if DEBUG
                    state.nextInvokedMethodName = null;
#endif
                }
                else
                {
                    // A query expression with a join clause with an into followed by a select clause
                    //     from x1 in e1
                    //     join x2 in e2 on k1 equals k2 into g
                    //     select v
                    // is translated into
                    //     ( e1 ) . GroupJoin( e2 , x1 => k1 , x2 => k2 , ( x1 , g ) => v )
                    state.allRangeVariables[x2].Free();
                    state.allRangeVariables.Remove(x2);
                    var g = state.AddRangeVariable(this, join.Into.Identifier, diagnostics);

                    var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, g), select.Expression, diagnostics.AccumulatesDependencies);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        receiverIsCheckedForRValue: false,
                        "GroupJoin",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics
#if DEBUG
                    , state.nextInvokedMethodName
#endif
                        );
#if DEBUG
                    state.nextInvokedMethodName = null;
#endif

                    // record the into clause in the bound tree
                    var arguments = invocation.Arguments;
                    arguments = arguments.SetItem(arguments.Length - 1, MakeQueryClause(join.Into, arguments[arguments.Length - 1], g));

                    invocation = invocation.Update(invocation.ReceiverOpt, invocation.InitialBindingReceiverIsSubjectToCloning, invocation.Method, arguments);
                }

                state.Clear(); // this completes the whole query
                state.fromExpression = MakeQueryClause(join, invocation, x2, invocation, castInvocation);
                state.fromExpression = MakeQueryClause(select, state.fromExpression);
            }
            else
            {
                BoundCall invocation;
                if (join.Into == null)
                {
                    // A query expression with a join clause without an into followed by something other than a select clause
                    //     from x1 in e1
                    //     join x2 in e2 on k1 equals k2 
                    //     ...
                    // is translated into
                    //     from * in ( e1 ) . Join(
                    //           e2 , x1 => k1 , x2 => k2 , ( x1 , x2 ) => new { x1 , x2 })
                    //     ...
                    var resultSelectorLambda = MakePairLambda(join, state, x1, x2, diagnostics.AccumulatesDependencies);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        receiverIsCheckedForRValue: false,
                        "Join",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics
#if DEBUG
                        , state.nextInvokedMethodName
#endif
                        );
#if DEBUG
                    state.nextInvokedMethodName = null;
#endif
                }
                else
                {
                    // A query expression with a join clause with an into followed by something other than a select clause
                    //     from x1 in e1
                    //     join x2 in e2 on k1 equals k2 into g
                    //     ...
                    // is translated into
                    //     from * in ( e1 ) . GroupJoin(
                    //                 e2 , x1 => k1 , x2 => k2 , ( x1 , g ) => new { x1 , g })
                    //     ...
                    state.allRangeVariables[x2].Free();
                    state.allRangeVariables.Remove(x2);

                    var g = state.AddRangeVariable(this, join.Into.Identifier, diagnostics);
                    var resultSelectorLambda = MakePairLambda(join, state, x1, g, diagnostics.AccumulatesDependencies);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        receiverIsCheckedForRValue: false,
                        "GroupJoin",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics
#if DEBUG
                        , state.nextInvokedMethodName
#endif
                        );
#if DEBUG
                    state.nextInvokedMethodName = null;
#endif

                    var arguments = invocation.Arguments;
                    arguments = arguments.SetItem(arguments.Length - 1, MakeQueryClause(join.Into, arguments[arguments.Length - 1], g));

                    invocation = invocation.Update(invocation.ReceiverOpt, invocation.InitialBindingReceiverIsSubjectToCloning, invocation.Method, arguments);
                }

                state.fromExpression = MakeQueryClause(join, invocation, x2, invocation, castInvocation);
            }
        }

        private void ReduceOrderBy(OrderByClauseSyntax orderby, QueryTranslationState state, BindingDiagnosticBag diagnostics)
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
            bool first = true;
            foreach (var ordering in orderby.Orderings)
            {
                string methodName = (first ? "OrderBy" : "ThenBy") + (ordering.IsKind(SyntaxKind.DescendingOrdering) ? "Descending" : "");
                var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, ordering.Expression, diagnostics.AccumulatesDependencies);
                var invocation = MakeQueryInvocation(ordering, state.fromExpression, receiverIsCheckedForRValue: false, methodName, lambda, diagnostics
#if DEBUG
                    , state.nextInvokedMethodName
#endif
                    );
#if DEBUG
                state.nextInvokedMethodName = null;
#endif
                state.fromExpression = MakeQueryClause(ordering, invocation, queryInvocation: invocation);
                first = false;
            }

            state.fromExpression = MakeQueryClause(orderby, state.fromExpression);
        }

        private void ReduceFrom(FromClauseSyntax from, QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            var x1 = state.rangeVariable;

            BoundExpression collectionSelectorLambda;
            if (from.Type == null)
            {
                collectionSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x1, from.Expression, diagnostics.AccumulatesDependencies);
            }
            else
            {
                collectionSelectorLambda = MakeQueryUnboundLambdaWithCast(state.RangeVariableMap(), x1, from.Expression, from.Type, BindTypeArgument(from.Type, diagnostics), diagnostics.AccumulatesDependencies);
            }

            var x2 = state.AddRangeVariable(this, from.Identifier, diagnostics);

            if (state.clauses.IsEmpty() && state.selectOrGroup.IsKind(SyntaxKind.SelectClause))
            {
                var select = (SelectClauseSyntax)state.selectOrGroup;

                // A query expression with a second from clause followed by a select clause
                //     from x1 in e1
                //     from x2 in e2
                //     select v
                // is translated into
                //     ( e1 ) . SelectMany( x1 => e2 , ( x1 , x2 ) => v )
                var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), select.Expression, diagnostics.AccumulatesDependencies);

                var invocation = MakeQueryInvocation(
                    from,
                    state.fromExpression,
                    receiverIsCheckedForRValue: false,
                    "SelectMany",
                    ImmutableArray.Create(collectionSelectorLambda, resultSelectorLambda),
                    diagnostics
#if DEBUG
                    , state.nextInvokedMethodName
#endif
                    );
#if DEBUG
                state.nextInvokedMethodName = null;
#endif

                // Adjust the second-to-last parameter to be a query clause (if it was an extension method, an extra parameter was added)
                BoundExpression? castInvocation = (from.Type != null) ? ExtractCastInvocation(invocation) : null;

                var arguments = invocation.Arguments;
                invocation = invocation.Update(
                    invocation.ReceiverOpt,
                    invocation.InitialBindingReceiverIsSubjectToCloning,
                    invocation.Method,
                    arguments.SetItem(arguments.Length - 2, MakeQueryClause(from, arguments[arguments.Length - 2], x2, invocation, castInvocation)));

                state.Clear();
                state.fromExpression = MakeQueryClause(from, invocation, definedSymbol: x2, queryInvocation: invocation);
                state.fromExpression = MakeQueryClause(select, state.fromExpression);
            }
            else
            {
                // A query expression with a second from clause followed by something other than a select clause:
                //     from x1 in e1
                //     from x2 in e2
                //     ...
                // is translated into
                //     from * in ( e1 ) . SelectMany( x1 => e2 , ( x1 , x2 ) => new { x1 , x2 } )
                //     ...

                // We use a slightly different translation strategy.  We produce
                //     from * in ( e ) . SelectMany ( x1 => e2, ( x1 , x2 ) => new Pair<X1,X2>(x1, x2) )
                // Where X1 is the type of x1, and X2 is the type of x2.
                // Subsequently, x1 (or members of x1, if it is a transparent identifier)
                // are accessed as TRID.Item1 (or members of that), and x2 is accessed
                // as TRID.Item2, where TRID is the compiler-generated identifier used
                // to represent the transparent identifier in the result.
                var resultSelectorLambda = MakePairLambda(from, state, x1, x2, diagnostics.AccumulatesDependencies);

                var invocation = MakeQueryInvocation(
                    from,
                    state.fromExpression,
                    receiverIsCheckedForRValue: false,
                    "SelectMany",
                    ImmutableArray.Create(collectionSelectorLambda, resultSelectorLambda),
                    diagnostics
#if DEBUG
                    , state.nextInvokedMethodName
#endif
                    );
#if DEBUG
                state.nextInvokedMethodName = null;
#endif

                BoundExpression? castInvocation = (from.Type != null) ? ExtractCastInvocation(invocation) : null;
                state.fromExpression = MakeQueryClause(from, invocation, x2, invocation, castInvocation);
            }
        }

        private static BoundExpression? ExtractCastInvocation(BoundCall invocation)
        {
            if (invocation.IsErroneousNode)
            {
                return null;
            }

            int index = invocation.InvokedAsExtensionMethod ? 1 : 0;
            var c1 = invocation.Arguments[index] as BoundConversion;
            var l1 = c1 != null ? c1.Operand as BoundLambda : null;
            var r1 = l1 != null ? l1.Body.Statements[0] as BoundReturnStatement : null;
            var i1 = r1 != null ? r1.ExpressionOpt as BoundCall : null;
            return i1;
        }

        private UnboundLambda MakePairLambda(CSharpSyntaxNode node, QueryTranslationState state, RangeVariableSymbol x1, RangeVariableSymbol x2, bool withDependencies)
        {
            Debug.Assert(LambdaUtilities.IsQueryPairLambda(node));

            LambdaBodyFactory bodyFactory = (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag d) =>
            {
                var x1Expression = new BoundParameter(node, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };
                var x2Expression = new BoundParameter(node, lambdaSymbol.Parameters[1]) { WasCompilerGenerated = true };
                var construction = MakePair(node, x1.Name, x1Expression, x2.Name, x2Expression, state, d);
                return lambdaBodyBinder.CreateBlockFromExpression(node, ImmutableArray<LocalSymbol>.Empty, RefKind.None, construction, null, d);
            };

            var result = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), node, bodyFactory, withDependencies);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x1.Name);
            var x2m = state.allRangeVariables[x2];
            x2m[x2m.Count - 1] = x2.Name;
            return result;
        }

        private void ReduceLet(LetClauseSyntax let, QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            // A query expression with a let clause
            //     from x in e
            //     let y = f
            //     ...
            // is translated into
            //     from * in ( e ) . Select ( x => new { x , y = f } )
            //     ...
            var x = state.rangeVariable;

            // We use a slightly different translation strategy.  We produce
            //     from * in ( e ) . Select ( x => new Pair<X,Y>(x, f) )
            // Where X is the type of x, and Y is the type of the expression f.
            // Subsequently, x (or members of x, if it is a transparent identifier)
            // are accessed as TRID.Item1 (or members of that), and y is accessed
            // as TRID.Item2, where TRID is the compiler-generated identifier used
            // to represent the transparent identifier in the result.
            LambdaBodyFactory bodyFactory = (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag d) =>
            {
                var xExpression = new BoundParameter(let, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };

                lambdaBodyBinder = lambdaBodyBinder.GetRequiredBinder(let.Expression);

                var yExpression = lambdaBodyBinder.BindRValueWithoutTargetType(let.Expression, d);
                SourceLocation errorLocation = new SourceLocation(let.SyntaxTree, new TextSpan(let.Identifier.SpanStart, let.Expression.Span.End - let.Identifier.SpanStart));
                if (!yExpression.HasAnyErrors && !yExpression.HasExpressionType())
                {
                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, yExpression.Display);
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create(yExpression), CreateErrorType());
                }
                else if (!yExpression.HasAnyErrors && yExpression.Type!.IsVoidType())
                {
                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, yExpression.Type!);
                    Debug.Assert(yExpression.Type is { });
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create(yExpression), yExpression.Type);
                }

                var construction = MakePair(let, x.Name, xExpression, let.Identifier.ValueText, yExpression, state, d);

                // The bound block represents a closure scope for transparent identifiers captured in the let clause.
                // Such closures shall be associated with the lambda body expression.
                return lambdaBodyBinder.CreateLambdaBlockForQueryClause(let.Expression, construction, d);
            };

            var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x), let.Expression, bodyFactory, diagnostics.AccumulatesDependencies);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x.Name);
            var y = state.AddRangeVariable(this, let.Identifier, diagnostics);
            state.allRangeVariables[y].Add(let.Identifier.ValueText);
            var invocation = MakeQueryInvocation(let, state.fromExpression, receiverIsCheckedForRValue: false, "Select", lambda, diagnostics
#if DEBUG
                                                 , state.nextInvokedMethodName
#endif
                                                 );
#if DEBUG
            state.nextInvokedMethodName = null;
#endif
            state.fromExpression = MakeQueryClause(let, invocation, y, invocation);
        }

        private BoundBlock CreateLambdaBlockForQueryClause(ExpressionSyntax expression, BoundExpression result, BindingDiagnosticBag diagnostics)
        {
            var locals = this.GetDeclaredLocalsForScope(expression);
            if (locals.Any())
            {
                CheckFeatureAvailability(expression, MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers, diagnostics, locals[0].GetFirstLocation());
            }

            return this.CreateBlockFromExpression(expression, locals, RefKind.None, result, expression, diagnostics);
        }

        private BoundQueryClause MakeQueryClause(
            CSharpSyntaxNode syntax,
            BoundExpression expression,
            RangeVariableSymbol? definedSymbol = null,
            BoundExpression? queryInvocation = null,
            BoundExpression? castInvocation = null,
            BoundExpression? unoptimizedForm = null)
        {
            if (unoptimizedForm != null && unoptimizedForm.HasAnyErrors && !expression.HasAnyErrors) unoptimizedForm = null;
            return new BoundQueryClause(
                syntax: syntax, value: expression,
                definedSymbol: definedSymbol,
                operation: queryInvocation,
                binder: this,
                cast: castInvocation, unoptimizedForm: unoptimizedForm,
                type: TypeOrError(expression));
        }

        private BoundExpression MakePair(CSharpSyntaxNode node, string field1Name, BoundExpression field1Value, string field2Name, BoundExpression field2Value, QueryTranslationState state, BindingDiagnosticBag diagnostics)
        {
            if (field1Name == field2Name)
            {
                // we will generate a diagnostic elsewhere
                field2Name = state.TransparentRangeVariableName();
                field2Value = new BoundBadExpression(field2Value.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create(field2Value), field2Value.Type, true);
            }

            AnonymousTypeDescriptor typeDescriptor = new AnonymousTypeDescriptor(
                                                            ImmutableArray.Create(
                                                                createField(field1Name, field1Value),
                                                                createField(field2Name, field2Value)),
                                                            node.Location
                                                     );

            AnonymousTypeManager manager = this.Compilation.AnonymousTypeManager;
            NamedTypeSymbol anonymousType = manager.ConstructAnonymousTypeSymbol(typeDescriptor, diagnostics);
            return MakeConstruction(node, anonymousType, ImmutableArray.Create(field1Value, field2Value), diagnostics);

            AnonymousTypeField createField(string fieldName, BoundExpression fieldValue) =>
                new AnonymousTypeField(fieldName, fieldValue.Syntax.Location, TypeWithAnnotations.Create(TypeOrError(fieldValue)), RefKind.None, ScopedKind.None);
        }

        private TypeSymbol TypeOrError(BoundExpression e)
        {
            return e.Type ?? CreateErrorType();
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression, bool withDependencies)
        {
            return MakeQueryUnboundLambda(qvm, ImmutableArray.Create(parameter), expression, withDependencies);
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax expression, bool withDependencies)
        {
            return MakeQueryUnboundLambda(expression, new QueryUnboundLambdaState(this, qvm, parameters, (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag diagnostics) =>
            {
                lambdaBodyBinder = lambdaBodyBinder.GetRequiredBinder(expression);
                Debug.Assert(lambdaSymbol != null);
                BoundExpression boundExpression = lambdaBodyBinder.BindValue(expression, diagnostics, BindValueKind.RValue);
                return lambdaBodyBinder.CreateLambdaBlockForQueryClause(expression, boundExpression, diagnostics);
            }), withDependencies);
        }

        private UnboundLambda MakeQueryUnboundLambdaWithCast(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression, TypeSyntax castTypeSyntax, TypeWithAnnotations castType, bool withDependencies)
        {
            return MakeQueryUnboundLambda(expression, new QueryUnboundLambdaState(this, qvm, ImmutableArray.Create(parameter), (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, BindingDiagnosticBag diagnostics) =>
            {
                lambdaBodyBinder = lambdaBodyBinder.GetRequiredBinder(expression);
                BoundExpression boundExpression = lambdaBodyBinder.BindValue(expression, diagnostics, BindValueKind.RValue);

                // We transform the expression from "expr" to "expr.Cast<castTypeOpt>()".
                boundExpression = lambdaBodyBinder.MakeQueryInvocation(expression, boundExpression, receiverIsCheckedForRValue: true, "Cast", castTypeSyntax, castType, diagnostics
#if DEBUG
                    , expectedMethodName: null
#endif
                    );

                return lambdaBodyBinder.CreateLambdaBlockForQueryClause(expression, boundExpression, diagnostics);
            }), withDependencies);
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, CSharpSyntaxNode node, LambdaBodyFactory bodyFactory, bool withDependencies)
        {
            return MakeQueryUnboundLambda(node, new QueryUnboundLambdaState(this, qvm, parameters, bodyFactory), withDependencies);
        }

        private static UnboundLambda MakeQueryUnboundLambda(CSharpSyntaxNode node, QueryUnboundLambdaState state, bool withDependencies)
        {
            Debug.Assert(node is ExpressionSyntax || LambdaUtilities.IsQueryPairLambda(node));
            // Function type is null because query expression syntax does not allow an explicit signature.
            var lambda = new UnboundLambda(node, state, functionType: null, withDependencies, hasErrors: false) { WasCompilerGenerated = true };
            state.SetUnboundLambda(lambda);
            return lambda;
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, bool receiverIsCheckedForRValue, string methodName, BoundExpression arg, BindingDiagnosticBag diagnostics
#if DEBUG
            , string? expectedMethodName
#endif
            )
        {
            return MakeQueryInvocation(node, receiver, receiverIsCheckedForRValue, methodName, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeWithAnnotations>), ImmutableArray.Create(arg), diagnostics
#if DEBUG
                , expectedMethodName
#endif
                );
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, bool receiverIsCheckedForRValue, string methodName, ImmutableArray<BoundExpression> args, BindingDiagnosticBag diagnostics
#if DEBUG
            , string? expectedMethodName
#endif
            )
        {
            return MakeQueryInvocation(node, receiver, receiverIsCheckedForRValue, methodName, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeWithAnnotations>), args, diagnostics
#if DEBUG
                , expectedMethodName
#endif
                );
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, bool receiverIsCheckedForRValue, string methodName, TypeSyntax typeArgSyntax, TypeWithAnnotations typeArg, BindingDiagnosticBag diagnostics
#if DEBUG
            , string? expectedMethodName
#endif
            )
        {
            return MakeQueryInvocation(node, receiver, receiverIsCheckedForRValue, methodName, new SeparatedSyntaxList<TypeSyntax>(new SyntaxNodeOrTokenList(typeArgSyntax, 0)), ImmutableArray.Create(typeArg), ImmutableArray<BoundExpression>.Empty, diagnostics
#if DEBUG
                , expectedMethodName
#endif
                );
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, bool receiverIsCheckedForRValue, string methodName, SeparatedSyntaxList<TypeSyntax> typeArgsSyntax, ImmutableArray<TypeWithAnnotations> typeArgs, ImmutableArray<BoundExpression> args, BindingDiagnosticBag diagnostics
#if DEBUG
            , string? expectedMethodName
#endif
            )
        {
#if DEBUG
            Debug.Assert(expectedMethodName is null || expectedMethodName == methodName);
#endif
            // clean up the receiver
            var ultimateReceiver = receiver;
            while (ultimateReceiver.Kind == BoundKind.QueryClause)
            {
                ultimateReceiver = ((BoundQueryClause)ultimateReceiver).Value;
            }
            Debug.Assert(receiver.Type is object || ultimateReceiver.Type is null);
            if ((object?)ultimateReceiver.Type == null)
            {
                Debug.Assert(ultimateReceiver.Kind != BoundKind.MethodGroup || ultimateReceiver.HasAnyErrors);

                if (ultimateReceiver.HasAnyErrors || node.HasErrors)
                {
                    // report no additional errors
                }
                else if (ultimateReceiver.IsLiteralNull())
                {
                    diagnostics.Add(ErrorCode.ERR_NullNotValid, node.Location);
                }
                else if (ultimateReceiver.IsLiteralDefault())
                {
                    diagnostics.Add(ErrorCode.ERR_DefaultLiteralNotValid, node.Location);
                }
                else if (ultimateReceiver.IsImplicitObjectCreation())
                {
                    diagnostics.Add(ErrorCode.ERR_ImplicitObjectCreationNotValid, node.Location);
                }
                else if (ultimateReceiver.Kind == BoundKind.NamespaceExpression)
                {
                    diagnostics.Add(ErrorCode.ERR_BadSKunknown, ultimateReceiver.Syntax.Location, ((BoundNamespaceExpression)ultimateReceiver).NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize());
                }
                else if (ultimateReceiver.Kind == BoundKind.Lambda || ultimateReceiver.Kind == BoundKind.UnboundLambda)
                {
                    // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.
                    diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, MessageID.IDS_AnonMethod.Localize(), methodName);
                }

                receiver = new BoundBadExpression(receiver.Syntax, LookupResultKind.NotAValue, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create(receiver), CreateErrorType());
            }
            else if (ultimateReceiver.Kind == BoundKind.TypeExpression)
            {
                if (ultimateReceiver.Type.TypeKind == TypeKind.TypeParameter)
                {
                    // We don't want to enable usage of static abstract members here
                    Error(diagnostics, ErrorCode.ERR_BadSKunknown, ultimateReceiver.Syntax, ultimateReceiver.Type, MessageID.IDS_SK_TYVAR.Localize());
                }
            }
            else if (ultimateReceiver.Kind == BoundKind.TypeOrValueExpression)
            {
                // CheckValue will be called by MakeInvocationExpression when it makes the member access, which will resolve
                // the type or value to the appropriate kind at that point.
            }
            else if (receiver.Type!.IsVoidType())
            {
                if (!receiver.HasAnyErrors && !node.HasErrors)
                {
                    diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, "void", methodName);
                }

                receiver = new BoundBadExpression(receiver.Syntax, LookupResultKind.NotAValue, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create(receiver), CreateErrorType());
            }
            else
            {
                if (!receiverIsCheckedForRValue)
                {
                    var checkedUltimateReceiver = CheckValue(ultimateReceiver, BindValueKind.RValue, diagnostics);
                    if (checkedUltimateReceiver != ultimateReceiver)
                    {
                        receiver = updateUltimateReceiver(receiver, ultimateReceiver, checkedUltimateReceiver);
                    }
                }
                else
                {
                    Debug.Assert(ultimateReceiver is not BoundQueryClause);
                }
            }

            return (BoundCall)MakeInvocationExpression(
                node,
                receiver,
                methodName,
                args,
                diagnostics,
                typeArgsSyntax,
                typeArgs,
                queryClause: node,
                // Queries are syntactical rewrites, so we allow fields and properties of delegate types to be invoked,
                // although no well-known non-generic query method is used atm.
                allowFieldsAndProperties: true);

            static BoundExpression updateUltimateReceiver(BoundExpression receiver, BoundExpression originalUltimateReceiver, BoundExpression replacementUltimateReceiver)
            {
                if (receiver is BoundQueryClause query)
                {
                    return query.Update(
                        updateUltimateReceiver(query.Value, originalUltimateReceiver, replacementUltimateReceiver),
                        query.DefinedSymbol,
                        query.Operation,
                        query.Cast,
                        query.Binder,
                        query.UnoptimizedForm,
                        query.Type);
                }

                Debug.Assert(receiver == originalUltimateReceiver);
                return replacementUltimateReceiver;
            }
        }

        protected BoundExpression MakeConstruction(CSharpSyntaxNode node, NamedTypeSymbol toCreate, ImmutableArray<BoundExpression> args, BindingDiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.AddRange(args);
            var result = BindClassCreationExpression(node, toCreate.Name, node, toCreate, analyzedArguments, diagnostics);
            result.WasCompilerGenerated = true;
            analyzedArguments.Free();
            return result;
        }
    }
}
