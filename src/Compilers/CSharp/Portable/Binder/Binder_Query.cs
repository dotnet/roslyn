// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        internal BoundExpression BindQuery(QueryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var fromClause = node.FromClause;
            var boundFromExpression = BindLeftOfPotentialColorColorMemberAccess(fromClause.Expression, diagnostics);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (boundFromExpression.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, fromClause.Expression.Location);
                boundFromExpression = BadExpression(fromClause.Expression, boundFromExpression);
            }

            QueryTranslationState state = new QueryTranslationState();
            state.fromExpression = MakeMemberAccessValue(boundFromExpression, diagnostics);

            var x = state.rangeVariable = state.AddRangeVariable(this, fromClause.Identifier, diagnostics);
            for (int i = node.Body.Clauses.Count - 1; i >= 0; i--)
            {
                state.clauses.Push(node.Body.Clauses[i]);
            }

            state.selectOrGroup = node.Body.SelectOrGroup;

            // A from clause that explicitly specifies a range variable type
            //     from T x in e
            // is translated into
            //     from x in ( e ) . Cast < T > ( )
            BoundExpression cast = null;
            if (fromClause.Type != null)
            {
                var typeRestriction = BindTypeArgument(fromClause.Type, diagnostics);
                cast = MakeQueryInvocation(fromClause, state.fromExpression, "Cast", fromClause.Type, typeRestriction, diagnostics);
                state.fromExpression = cast;
            }

            state.fromExpression = MakeQueryClause(fromClause, state.fromExpression, x, castInvocation: cast);
            BoundExpression result = BindQueryInternal1(state, diagnostics);
            for (QueryContinuationSyntax continuation = node.Body.Continuation; continuation != null; continuation = continuation.Body.Continuation)
            {
                // A query expression with a continuation
                //     from ... into x ...
                // is translated into
                //     from x in ( from ... ) ...
                state.Clear();
                state.fromExpression = result;
                x = state.rangeVariable = state.AddRangeVariable(this, continuation.Identifier, diagnostics);
                Debug.Assert(state.clauses.IsEmpty());
                var clauses = continuation.Body.Clauses;
                for (int i = clauses.Count - 1; i >= 0; i--)
                {
                    state.clauses.Push(clauses[i]);
                }

                state.selectOrGroup = continuation.Body.SelectOrGroup;
                result = BindQueryInternal1(state, diagnostics);
                result = MakeQueryClause(continuation.Body, result, x);
                result = MakeQueryClause(continuation, result, x);
            }

            state.Free();
            return MakeQueryClause(node, result);
        }

        private BoundExpression BindQueryInternal1(QueryTranslationState state, DiagnosticBag diagnostics)
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

        private BoundExpression BindQueryInternal2(QueryTranslationState state, DiagnosticBag diagnostics)
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
                        DiagnosticBag discarded = DiagnosticBag.GetInstance();
                        var unoptimized = FinalTranslation(state, discarded);
                        discarded.Free();

                        if (unoptimized.HasAnyErrors && !result.HasAnyErrors) unoptimized = null;
                        return MakeQueryClause(state.selectOrGroup, result, unoptimizedForm: unoptimized);
                    }

                    return FinalTranslation(state, diagnostics);
                }

                ReduceQuery(state, diagnostics);
            }
        }

        private BoundExpression FinalTranslation(QueryTranslationState state, DiagnosticBag diagnostics)
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
                        var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x, v);
                        var result = MakeQueryInvocation(state.selectOrGroup, e, "Select", lambda, diagnostics);
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
                        var lambdaLeft = MakeQueryUnboundLambda(state.RangeVariableMap(), x, k);

                        // this is the unoptimized form (when v is not the identifier x)
                        var d = DiagnosticBag.GetInstance();
                        BoundExpression lambdaRight = MakeQueryUnboundLambda(state.RangeVariableMap(), x, v);
                        result = MakeQueryInvocation(state.selectOrGroup, e, "GroupBy", ImmutableArray.Create(lambdaLeft, lambdaRight), d);
                        // k and v appear reversed in the invocation, so we reorder their evaluation
                        result = ReverseLastTwoParameterOrder(result);

                        BoundExpression unoptimizedForm = null;
                        if (vId != null && vId.Identifier.ValueText == x.Name)
                        {
                            // The optimized form.  We store the unoptimized form for analysis
                            unoptimizedForm = result;
                            result = MakeQueryInvocation(state.selectOrGroup, e, "GroupBy", lambdaLeft, diagnostics);
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
                        return new BoundBadExpression(
                            state.selectOrGroup, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty,
                            ImmutableArray.Create<BoundNode>(state.fromExpression), state.fromExpression.Type);
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
            return result.Update(
                result.ReceiverOpt, result.Method, arguments.ToImmutableAndFree(), default(ImmutableArray<string>),
                default(ImmutableArray<RefKind>), result.IsDelegateCall, result.Expanded, result.InvokedAsExtensionMethod,
                argsToParams.ToImmutableAndFree(), result.ResultKind, result.Type);
        }

        private void ReduceQuery(QueryTranslationState state, DiagnosticBag diagnostics)
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

        private void ReduceWhere(WhereClauseSyntax where, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            // A query expression with a where clause
            //     from x in e
            //     where f
            //     ...
            // is translated into
            //     from x in ( e ) . Where ( x => f )
            var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, where.Condition);
            var invocation = MakeQueryInvocation(where, state.fromExpression, "Where", lambda, diagnostics);
            state.fromExpression = MakeQueryClause(where, invocation, queryInvocation: invocation);
        }

        private void ReduceJoin(JoinClauseSyntax join, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            var inExpression = BindValue(join.InExpression, diagnostics, BindValueKind.RValue);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (inExpression.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, join.InExpression.Location);
                inExpression = BadExpression(join.InExpression, inExpression);
            }

            BoundExpression castInvocation = null;
            if (join.Type != null)
            {
                // A join clause that explicitly specifies a range variable type
                //     join T x in e on k1 equals k2
                // is translated into
                //     join x in ( e ) . Cast < T > ( ) on k1 equals k2
                var castType = BindTypeArgument(join.Type, diagnostics);
                castInvocation = MakeQueryInvocation(join, inExpression, "Cast", join.Type, castType, diagnostics);
                inExpression = castInvocation;
            }

            var outerKeySelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, join.LeftExpression);

            var x1 = state.rangeVariable;
            var x2 = state.AddRangeVariable(this, join.Identifier, diagnostics);
            var innerKeySelectorLambda = MakeQueryUnboundLambda(QueryTranslationState.RangeVariableMap(x2), x2, join.RightExpression);

            if (state.clauses.IsEmpty() && state.selectOrGroup.Kind() == SyntaxKind.SelectClause)
            {
                var select = state.selectOrGroup as SelectClauseSyntax;
                BoundCall invocation;
                if (join.Into == null)
                {
                    // A query expression with a join clause without an into followed by a select clause
                    //     from x1 in e1
                    //     join x2 in e2 on k1 equals k2
                    //     select v
                    // is translated into
                    //     ( e1 ) . Join( e2 , x1 => k1 , x2 => k2 , ( x1 , x2 ) => v )
                    var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), select.Expression);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        "Join",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics);
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

                    var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, g), select.Expression);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        "GroupJoin",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics);

                    // record the into clause in the bound tree
                    var arguments = invocation.Arguments;
                    arguments = arguments.SetItem(arguments.Length - 1, MakeQueryClause(join.Into, arguments[arguments.Length - 1], g));

                    invocation = invocation.Update(invocation.ReceiverOpt, invocation.Method, arguments);
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
                    var resultSelectorLambda = MakePairLambda(join, state, x1, x2);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        "Join",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics);
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
                    var resultSelectorLambda = MakePairLambda(join, state, x1, g);

                    invocation = MakeQueryInvocation(
                        join,
                        state.fromExpression,
                        "GroupJoin",
                        ImmutableArray.Create(inExpression, outerKeySelectorLambda, innerKeySelectorLambda, resultSelectorLambda),
                        diagnostics);

                    var arguments = invocation.Arguments;
                    arguments = arguments.SetItem(arguments.Length - 1, MakeQueryClause(join.Into, arguments[arguments.Length - 1], g));

                    invocation = invocation.Update(invocation.ReceiverOpt, invocation.Method, arguments);
                }

                state.fromExpression = MakeQueryClause(join, invocation, x2, invocation, castInvocation);
            }
        }

        private void ReduceOrderBy(OrderByClauseSyntax orderby, QueryTranslationState state, DiagnosticBag diagnostics)
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
                var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, ordering.Expression);
                var invocation = MakeQueryInvocation(ordering, state.fromExpression, methodName, lambda, diagnostics);
                state.fromExpression = MakeQueryClause(ordering, invocation, queryInvocation: invocation);
                first = false;
            }

            state.fromExpression = MakeQueryClause(orderby, state.fromExpression);
        }

        private void ReduceFrom(FromClauseSyntax from, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            var x1 = state.rangeVariable;

            BoundExpression collectionSelectorLambda;
            if (from.Type == null)
            {
                collectionSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x1, from.Expression);
            }
            else
            {
                collectionSelectorLambda = MakeQueryUnboundLambdaWithCast(state.RangeVariableMap(), x1, from.Expression, from.Type, BindTypeArgument(from.Type, diagnostics));
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
                var resultSelectorLambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), select.Expression);

                var invocation = MakeQueryInvocation(
                    from,
                    state.fromExpression,
                    "SelectMany",
                    ImmutableArray.Create(collectionSelectorLambda, resultSelectorLambda),
                    diagnostics);

                // Adjust the second-to-last parameter to be a query clause (if it was an extension method, an extra parameter was added)
                BoundExpression castInvocation = (from.Type != null) ? ExtractCastInvocation(invocation) : null;

                var arguments = invocation.Arguments;
                invocation = invocation.Update(
                    invocation.ReceiverOpt,
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
                var resultSelectorLambda = MakePairLambda(from, state, x1, x2);

                var invocation = MakeQueryInvocation(
                    from,
                    state.fromExpression,
                    "SelectMany",
                    ImmutableArray.Create(collectionSelectorLambda, resultSelectorLambda),
                    diagnostics);

                BoundExpression castInvocation = (from.Type != null) ? ExtractCastInvocation(invocation) : null;
                state.fromExpression = MakeQueryClause(from, invocation, x2, invocation, castInvocation);
            }
        }

        private static BoundExpression ExtractCastInvocation(BoundCall invocation)
        {
            int index = invocation.InvokedAsExtensionMethod ? 1 : 0;
            var c1 = invocation.Arguments[index] as BoundConversion;
            var l1 = c1 != null ? c1.Operand as BoundLambda : null;
            var r1 = l1 != null ? l1.Body.Statements[0] as BoundReturnStatement : null;
            var i1 = r1 != null ? r1.ExpressionOpt as BoundCall : null;
            return i1;
        }

        private UnboundLambda MakePairLambda(CSharpSyntaxNode node, QueryTranslationState state, RangeVariableSymbol x1, RangeVariableSymbol x2)
        {
            Debug.Assert(LambdaUtilities.IsQueryPairLambda(node));

            LambdaBodyFactory bodyFactory = (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag d) =>
            {
                var x1Expression = new BoundParameter(node, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };
                var x2Expression = new BoundParameter(node, lambdaSymbol.Parameters[1]) { WasCompilerGenerated = true };
                var construction = MakePair(node, x1.Name, x1Expression, x2.Name, x2Expression, state, d);
                return lambdaBodyBinder.CreateBlockFromExpression(node, ImmutableArray<LocalSymbol>.Empty, RefKind.None, construction, null, d);
            };

            var result = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x1, x2), node, bodyFactory);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x1.Name);
            var x2m = state.allRangeVariables[x2];
            x2m[x2m.Count - 1] = x2.Name;
            return result;
        }

        private void ReduceLet(LetClauseSyntax let, QueryTranslationState state, DiagnosticBag diagnostics)
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
            LambdaBodyFactory bodyFactory = (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag d) =>
            {
                var xExpression = new BoundParameter(let, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };

                lambdaBodyBinder = lambdaBodyBinder.GetBinder(let.Expression);
                Debug.Assert(lambdaBodyBinder != null);

                var yExpression = lambdaBodyBinder.BindValue(let.Expression, d, BindValueKind.RValue);
                SourceLocation errorLocation = new SourceLocation(let.SyntaxTree, new TextSpan(let.Identifier.SpanStart, let.Expression.Span.End - let.Identifier.SpanStart));
                if (!yExpression.HasAnyErrors && !yExpression.HasExpressionType())
                {
                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, yExpression.Display);
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(yExpression), CreateErrorType());
                }
                else if (!yExpression.HasAnyErrors && yExpression.Type.SpecialType == SpecialType.System_Void)
                {
                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, yExpression.Type);
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(yExpression), yExpression.Type);
                }

                var construction = MakePair(let, x.Name, xExpression, let.Identifier.ValueText, yExpression, state, d);

                // The bound block represents a closure scope for transparent identifiers captured in the let clause.
                // Such closures shall be associated with the lambda body expression.
                return lambdaBodyBinder.CreateBlockFromExpression(let.Expression, lambdaBodyBinder.GetDeclaredLocalsForScope(let.Expression), RefKind.None, construction, null, d);
            };

            var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), ImmutableArray.Create(x), let.Expression, bodyFactory);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x.Name);
            var y = state.AddRangeVariable(this, let.Identifier, diagnostics);
            state.allRangeVariables[y].Add(let.Identifier.ValueText);
            var invocation = MakeQueryInvocation(let, state.fromExpression, "Select", lambda, diagnostics);
            state.fromExpression = MakeQueryClause(let, invocation, y, invocation);
        }

        private BoundQueryClause MakeQueryClause(
            CSharpSyntaxNode syntax,
            BoundExpression expression,
            RangeVariableSymbol definedSymbol = null,
            BoundExpression queryInvocation = null,
            BoundExpression castInvocation = null,
            BoundExpression unoptimizedForm = null)
        {
            if (unoptimizedForm != null && unoptimizedForm.HasAnyErrors && !expression.HasAnyErrors) unoptimizedForm = null;
            return new BoundQueryClause(
                syntax: syntax, value: expression,
                definedSymbol: definedSymbol,
                queryInvocation: queryInvocation,
                binder: this,
                castInvocation: castInvocation, unoptimizedForm: unoptimizedForm,
                type: TypeOrError(expression));
        }

        private BoundExpression MakePair(CSharpSyntaxNode node, string field1Name, BoundExpression field1Value, string field2Name, BoundExpression field2Value, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            if (field1Name == field2Name)
            {
                // we will generate a diagnostic elsewhere
                field2Name = state.TransparentRangeVariableName();
                field2Value = new BoundBadExpression(field2Value.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(field2Value), field2Value.Type, true);
            }

            AnonymousTypeDescriptor typeDescriptor = new AnonymousTypeDescriptor(
                                                            ImmutableArray.Create<AnonymousTypeField>(
                                                                new AnonymousTypeField(field1Name, field1Value.Syntax.Location, TypeOrError(field1Value)),
                                                                new AnonymousTypeField(field2Name, field2Value.Syntax.Location, TypeOrError(field2Value))
                                                            ),
                                                            node.Location
                                                     );

            AnonymousTypeManager manager = this.Compilation.AnonymousTypeManager;
            NamedTypeSymbol anonymousType = manager.ConstructAnonymousTypeSymbol(typeDescriptor);
            return MakeConstruction(node, anonymousType, ImmutableArray.Create(field1Value, field2Value), diagnostics);
        }

        private TypeSymbol TypeOrError(BoundExpression e)
        {
            return e.Type ?? CreateErrorType();
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression)
        {
            return MakeQueryUnboundLambda(qvm, ImmutableArray.Create(parameter), expression);
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax expression)
        {
            return MakeQueryUnboundLambda(expression, new QueryUnboundLambdaState(this, qvm, parameters, (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics) =>
            {
                return lambdaBodyBinder.BindLambdaExpressionAsBlock(RefKind.None, expression, diagnostics);
            }));
        }

        private UnboundLambda MakeQueryUnboundLambdaWithCast(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression, TypeSyntax castTypeSyntax, TypeSymbol castType)
        {
            return MakeQueryUnboundLambda(expression, new QueryUnboundLambdaState(this, qvm, ImmutableArray.Create(parameter), (LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics) =>
            {
                lambdaBodyBinder = lambdaBodyBinder.GetBinder(expression);
                Debug.Assert(lambdaBodyBinder != null);

                BoundExpression boundExpression = lambdaBodyBinder.BindValue(expression, diagnostics, BindValueKind.RValue);

                // We transform the expression from "expr" to "expr.Cast<castTypeOpt>()".
                boundExpression = lambdaBodyBinder.MakeQueryInvocation(expression, boundExpression, "Cast", castTypeSyntax, castType, diagnostics);

                return lambdaBodyBinder.CreateBlockFromExpression(expression, lambdaBodyBinder.GetDeclaredLocalsForScope(expression), RefKind.None, boundExpression, expression, diagnostics);
            }));
        }

        private UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, CSharpSyntaxNode node, LambdaBodyFactory bodyFactory)
        {
            return MakeQueryUnboundLambda(node, new QueryUnboundLambdaState(this, qvm, parameters, bodyFactory));
        }

        private UnboundLambda MakeQueryUnboundLambda(CSharpSyntaxNode node, QueryUnboundLambdaState state)
        {
            Debug.Assert(node is ExpressionSyntax || LambdaUtilities.IsQueryPairLambda(node));
            var lambda = new UnboundLambda(node, state, hasErrors: false) { WasCompilerGenerated = true };
            state.SetUnboundLambda(lambda);
            return lambda;
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, string methodName, BoundExpression arg, DiagnosticBag diagnostics)
        {
            return MakeQueryInvocation(node, receiver, methodName, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeSymbol>), ImmutableArray.Create(arg), diagnostics);
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, string methodName, ImmutableArray<BoundExpression> args, DiagnosticBag diagnostics)
        {
            return MakeQueryInvocation(node, receiver, methodName, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeSymbol>), args, diagnostics);
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, string methodName, TypeSyntax typeArgSyntax, TypeSymbol typeArg, DiagnosticBag diagnostics)
        {
            return MakeQueryInvocation(node, receiver, methodName, new SeparatedSyntaxList<TypeSyntax>(new SyntaxNodeOrTokenList(typeArgSyntax, 0)), ImmutableArray.Create(typeArg), ImmutableArray<BoundExpression>.Empty, diagnostics);
        }

        protected BoundCall MakeQueryInvocation(CSharpSyntaxNode node, BoundExpression receiver, string methodName, SeparatedSyntaxList<TypeSyntax> typeArgsSyntax, ImmutableArray<TypeSymbol> typeArgs, ImmutableArray<BoundExpression> args, DiagnosticBag diagnostics)
        {
            // clean up the receiver
            var ultimateReceiver = receiver;
            while (ultimateReceiver.Kind == BoundKind.QueryClause) ultimateReceiver = ((BoundQueryClause)ultimateReceiver).Value;
            if ((object)ultimateReceiver.Type == null)
            {
                if (ultimateReceiver.HasAnyErrors || node.HasErrors)
                {
                    // report no additional errors
                }
                else if (ultimateReceiver.IsLiteralNull())
                {
                    diagnostics.Add(ErrorCode.ERR_NullNotValid, node.Location);
                }
                else if (ultimateReceiver.Kind == BoundKind.Lambda || ultimateReceiver.Kind == BoundKind.UnboundLambda)
                {
                    // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.
                    diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, MessageID.IDS_AnonMethod.Localize(), methodName);
                }
                else if (ultimateReceiver.Kind == BoundKind.MethodGroup)
                {
                    var methodGroup = (BoundMethodGroup)ultimateReceiver;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);
                    diagnostics.AddRange(resolution.Diagnostics);
                    if (resolution.HasAnyErrors)
                    {
                        receiver = this.BindMemberAccessBadResult(methodGroup);
                    }
                    else
                    {
                        Debug.Assert(!resolution.IsEmpty);
                        diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, MessageID.IDS_SK_METHOD.Localize(), methodName);
                    }
                    resolution.Free();
                }

                receiver = new BoundBadExpression(receiver.Syntax, LookupResultKind.NotAValue, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(receiver), CreateErrorType());
            }
            else if (receiver.Type.SpecialType == SpecialType.System_Void)
            {
                if (!receiver.HasAnyErrors && !node.HasErrors)
                {
                    diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, "void", methodName);
                }

                receiver = new BoundBadExpression(receiver.Syntax, LookupResultKind.NotAValue, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(receiver), CreateErrorType());
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
        }

        protected BoundExpression MakeConstruction(CSharpSyntaxNode node, NamedTypeSymbol toCreate, ImmutableArray<BoundExpression> args, DiagnosticBag diagnostics)
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
