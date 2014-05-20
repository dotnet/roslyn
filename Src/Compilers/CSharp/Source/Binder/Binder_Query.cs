// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        const string transparentIdentifierPrefix = "<>h__TransparentIdentifier";

        internal BoundExpression BindQuery(QueryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var fromClause = node.FromClause;

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var boundFromExpression = BindLeftOfPotentialColorColorMemberAccess(fromClause.Expression, ref useSiteDiagnostics) ??
                this.BindExpression(fromClause.Expression, diagnostics);
            diagnostics.Add(fromClause.Expression, useSiteDiagnostics);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (boundFromExpression.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, fromClause.Expression.Location);
                boundFromExpression = BadExpression(fromClause.Expression, new[] { boundFromExpression });
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

        BoundExpression BindQueryInternal1(QueryTranslationState state, DiagnosticBag diagnostics)
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

        BoundExpression BindQueryInternal2(QueryTranslationState state, DiagnosticBag diagnostics)
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
            switch (state.selectOrGroup.Kind)
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
                        result = MakeQueryInvocation(state.selectOrGroup, e, "GroupBy", Args(lambdaLeft, lambdaRight), d);
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
                            Args<BoundNode>(state.fromExpression), state.fromExpression.Type);
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

        void ReduceQuery(QueryTranslationState state, DiagnosticBag diagnostics)
        {
            var topClause = state.clauses.Pop();
            switch (topClause.Kind)
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
                    throw ExceptionUtilities.UnexpectedValue(topClause.Kind);
            }
        }

        void ReduceWhere(WhereClauseSyntax where, QueryTranslationState state, DiagnosticBag diagnostics)
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

        void ReduceJoin(JoinClauseSyntax join, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            var e2 = BindValue(join.InExpression, diagnostics, BindValueKind.RValue);

            // If the from expression is of the type dynamic we can't infer the types for any lambdas that occur in the query.
            // Only if there are none we could bind the query but we report an error regardless since such queries are not useful.
            if (e2.HasDynamicType())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicQuery, join.InExpression.Location);
                e2 = BadExpression(join.InExpression, new[] { e2 });
            }

            BoundExpression castInvocation = null;
            if (join.Type != null)
            {
                // A join clause that explicitly specifies a range variable type
                //     join T x in e on k1 equals k2
                // is translated into
                //     join x in ( e ) . Cast < T > ( ) on k1 equals k2
                var castType = BindTypeArgument(join.Type, diagnostics);
                castInvocation = MakeQueryInvocation(join, e2, "Cast", join.Type, castType, diagnostics);
                e2 = castInvocation;
            }
            var args = ArrayBuilder<BoundExpression>.GetInstance();
            args.Add(e2);
            var lambda1 = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, join.LeftExpression);
            args.Add(lambda1);
            var x1 = state.rangeVariable;
            var x2 = state.AddRangeVariable(this, join.Identifier, diagnostics);
            var lambda2 = MakeQueryUnboundLambda(QueryTranslationState.RangeVariableMap(x2), x2, join.RightExpression);
            args.Add(lambda2);

            if (state.clauses.IsEmpty() && state.selectOrGroup.Kind == SyntaxKind.SelectClause)
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
                    var lambda3 = MakeQueryUnboundLambda(state.RangeVariableMap(), Args(x1, x2), select.Expression);
                    args.Add(lambda3);
                    invocation = MakeQueryInvocation(join, state.fromExpression, "Join", args.ToImmutableAndFree(), diagnostics);
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
                    var lambda3 = MakeQueryUnboundLambda(state.RangeVariableMap(), Args(x1, g), select.Expression);
                    args.Add(lambda3);
                    invocation = MakeQueryInvocation(join, state.fromExpression, "GroupJoin", args.ToImmutableAndFree(), diagnostics);
                    var arguments = invocation.Arguments.ToArray();
                    // record the into clause in the bound tree
                    arguments[arguments.Length - 1] = MakeQueryClause(join.Into, arguments[arguments.Length - 1], g);
                    invocation = invocation.Update(
                        receiverOpt: invocation.ReceiverOpt,
                        method: invocation.Method,
                        arguments: Args(arguments));
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
                    var lambda3 = MakePairLambda(join, state, x1, x2);
                    args.Add(lambda3);
                    invocation = MakeQueryInvocation(join, state.fromExpression, "Join", args.ToImmutableAndFree(), diagnostics);
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
                    var lambda3 = MakePairLambda(join, state, x1, g);
                    args.Add(lambda3);
                    invocation = MakeQueryInvocation(join, state.fromExpression, "GroupJoin", args.ToImmutableAndFree(), diagnostics);
                    var arguments = invocation.Arguments.ToArray();
                    arguments[arguments.Length - 1] = MakeQueryClause(join.Into, arguments[arguments.Length - 1], g);
                    invocation = invocation.Update(
                        receiverOpt: invocation.ReceiverOpt,
                        method: invocation.Method,
                        arguments: Args(arguments));
                }

                state.fromExpression = MakeQueryClause(join, invocation, x2, invocation, castInvocation);
            }
        }

        void ReduceOrderBy(OrderByClauseSyntax orderby, QueryTranslationState state, DiagnosticBag diagnostics)
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
                string methodName = (first ? "OrderBy" : "ThenBy") + (ordering.Kind == SyntaxKind.DescendingOrdering ? "Descending" : "");
                var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), state.rangeVariable, ordering.Expression);
                var invocation = MakeQueryInvocation(ordering, state.fromExpression, methodName, lambda, diagnostics);
                state.fromExpression = MakeQueryClause(ordering, invocation, queryInvocation: invocation);
                first = false;
            }

            state.fromExpression = MakeQueryClause(orderby, state.fromExpression);
        }

        void ReduceFrom(FromClauseSyntax from, QueryTranslationState state, DiagnosticBag diagnostics)
        {
            var x1 = state.rangeVariable;
            TypeSymbol castType = from.Type == null ? null : BindTypeArgument(from.Type, diagnostics);
            BoundExpression lambda1 = MakeQueryUnboundLambda(state.RangeVariableMap(), x1, from.Expression, from.Type, castType);
            var x2 = state.AddRangeVariable(this, from.Identifier, diagnostics);

            if (state.clauses.IsEmpty() && state.selectOrGroup.Kind == SyntaxKind.SelectClause)
            {
                // A query expression with a second from clause followed by a select clause
                //     from x1 in e1
                //     from x2 in e2
                //     select v
                // is translated into
                //     ( e1 ) . SelectMany( x1 => e2 , ( x1 , x2 ) => v )
                var select = state.selectOrGroup as SelectClauseSyntax;
                BoundExpression lambda2 = MakeQueryUnboundLambda(state.RangeVariableMap(), Args(x1, x2), select.Expression);
                var invocation = MakeQueryInvocation(from, state.fromExpression, "SelectMany", Args(lambda1, lambda2), diagnostics);
                BoundExpression castInvocation = (object)castType != null ? ExtractCastInvocation(invocation) : null;
                var arguments = invocation.Arguments.ToArray();
                // Adjust the second-to-last parameter to be a query clause.  (if it was an extension method, an extra parameter was added)
                arguments[arguments.Length - 2] = MakeQueryClause(from, arguments[arguments.Length - 2], x2, invocation, castInvocation);
                invocation = invocation.Update(invocation.ReceiverOpt, invocation.Method, Args(arguments));
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
                var lambda2 = MakePairLambda(from, state, x1, x2);
                var invocation = MakeQueryInvocation(from, state.fromExpression, "SelectMany", Args(lambda1, lambda2), diagnostics);
                BoundExpression castInvocation = (object)castType != null ? ExtractCastInvocation(invocation) : null;
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

        UnboundLambda MakePairLambda(CSharpSyntaxNode node, QueryTranslationState state, RangeVariableSymbol x1, RangeVariableSymbol x2)
        {
            LambdaBodyResolver resolver = (LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag d) =>
            {
                var x1Expression = new BoundParameter(node, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };
                var x2Expression = new BoundParameter(node, lambdaSymbol.Parameters[1]) { WasCompilerGenerated = true };
                var construction = MakePair(node, x1.Name, x1Expression, x2.Name, x2Expression, state, d);
                return lambdaBodyBinder.WrapExpressionLambdaBody(ImmutableArray<LocalSymbol>.Empty, construction, node, d);
            };
            var result = MakeQueryUnboundLambda(state.RangeVariableMap(), Args(x1, x2), node, resolver);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x1.Name);
            var x2m = state.allRangeVariables[x2];
            x2m[x2m.Count - 1] = x2.Name;
            return result;
        }

        void ReduceLet(LetClauseSyntax let, QueryTranslationState state, DiagnosticBag diagnostics)
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
            LambdaBodyResolver resolver = (LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag d) =>
            {
                var xExpression = new BoundParameter(let, lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };

                var expressionBinder = new ScopedExpressionBinder(lambdaBodyBinder, let.Expression);
                var yExpression = expressionBinder.BindValue(let.Expression, d, BindValueKind.RValue);
                SourceLocation errorLocation = new SourceLocation(let.SyntaxTree, new TextSpan(let.Identifier.SpanStart, let.Expression.Span.End - let.Identifier.SpanStart));
                if (!yExpression.HasAnyErrors && !yExpression.HasExpressionType())
                {
                    MessageID id = MessageID.IDS_NULL;
                    if (yExpression.Kind == BoundKind.UnboundLambda)
                    {
                        id = ((UnboundLambda)yExpression).MessageID;
                    }
                    else if (yExpression.Kind == BoundKind.MethodGroup)
                    {
                        id = MessageID.IDS_MethodGroup;
                    }
                    else
                    {
                        Debug.Assert(yExpression.IsLiteralNull(), "How did we successfully bind an expression without a type?");
                    }

                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, id.Localize());
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(yExpression), CreateErrorType());
                }
                else if (!yExpression.HasAnyErrors && yExpression.Type.SpecialType == SpecialType.System_Void)
                {
                    Error(d, ErrorCode.ERR_QueryRangeVariableAssignedBadValue, errorLocation, yExpression.Type);
                    yExpression = new BoundBadExpression(yExpression.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(yExpression), yExpression.Type);
                }

                var construction = MakePair(let, x.Name, xExpression, let.Identifier.ValueText, yExpression, state, d);
                return lambdaBodyBinder.WrapExpressionLambdaBody(expressionBinder.Locals, construction, let, d);
            };
            var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x, let.Expression, resolver);
            state.rangeVariable = state.TransparentRangeVariable(this);
            state.AddTransparentIdentifier(x.Name);
            var y = state.AddRangeVariable(this, let.Identifier, diagnostics);
            state.allRangeVariables[y].Add(let.Identifier.ValueText);
            var invocation = MakeQueryInvocation(let, state.fromExpression, "Select", lambda, diagnostics);
            state.fromExpression = MakeQueryClause(let, invocation, y, invocation);
        }

        static ImmutableArray<T> Args<T>(params T[] args)
        {
            return ImmutableArray.Create<T>(args);
        }

        BoundQueryClause MakeQueryClause(
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

        BoundExpression MakePair(CSharpSyntaxNode node, string field1Name, BoundExpression field1Value, string field2Name, BoundExpression field2Value, QueryTranslationState state, DiagnosticBag diagnostics)
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
            return MakeConstruction(node, anonymousType, Args(field1Value, field2Value), diagnostics);
        }

        TypeSymbol TypeOrError(BoundExpression e)
        {
            return e.Type ?? CreateErrorType();
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax expression)
        {
            // generate the unbound lambda expression (parameters) => expression
            return MakeQueryUnboundLambda(qvm, parameters, expression, null, null);
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression)
        {
            return MakeQueryUnboundLambda(qvm, Args(parameter), expression);
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, RangeVariableSymbol parameter, ExpressionSyntax expression, TypeSyntax castTypeSyntaxOpt, TypeSymbol castTypeOpt)
        {
            return MakeQueryUnboundLambda(qvm, Args(parameter), expression, castTypeSyntaxOpt, castTypeOpt);
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax expression, TypeSyntax castTypeSyntaxOpt, TypeSymbol castTypeOpt)
        {
            var state = ((object)castTypeOpt == null)
                ? new QueryUnboundLambdaState(null, this, qvm, parameters, expression)
                : new QueryUnboundLambdaState(null, this, qvm, parameters, expression, castTypeSyntaxOpt, castTypeOpt);
            var lambda = new UnboundLambda(expression, state, false) { WasCompilerGenerated = true };
            state.SetUnboundLambda(lambda);
            return lambda;
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, RangeVariableSymbol parameter, CSharpSyntaxNode node, LambdaBodyResolver resolver)
        {
            return MakeQueryUnboundLambda(qvm, Args(parameter), node, resolver);
        }

        UnboundLambda MakeQueryUnboundLambda(RangeVariableMap qvm, ImmutableArray<RangeVariableSymbol> parameters, CSharpSyntaxNode node, LambdaBodyResolver resolver)
        {
            var state = new QueryUnboundLambdaState(null, this, qvm, parameters, resolver);
            var lambda = new UnboundLambda(node, state, false) { WasCompilerGenerated = true };
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
