// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal BoundExpression BindQuery2(QueryExpression2Syntax node, DiagnosticBag diagnostics)
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

            QueryTranslationState2 state = new QueryTranslationState2();
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

            state.Free();
            return MakeQueryClause(node, result);
        }

        private BoundExpression BindQueryInternal1(QueryTranslationState2 state, DiagnosticBag diagnostics)
        {
            // If the query is a degenerate one the form "from x in e select x", but in source,
            // then we go ahead and generate the select anyway.  We do this by skipping BindQueryInternal2,
            // whose job it is to (reduce away the whole query and) optimize away degenerate queries.
            return IsDegenerateQuery(state) ? FinalTranslation(state, diagnostics) : BindQueryInternal2(state, diagnostics);
        }

        private static bool IsDegenerateQuery(QueryTranslationState2 state)
        {
            if (!state.clauses.IsEmpty()) return false;

            // A degenerate query is of the form "from x in e select x".
            var select = state.selectOrGroup as SelectClause2Syntax;
            if (select == null) return false;
            var name = select.Expression as IdentifierNameSyntax;
            return name != null && state.rangeVariable.Name == name.Identifier.ValueText;
        }

        private BoundExpression BindQueryInternal2(QueryTranslationState2 state, DiagnosticBag diagnostics)
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

        private BoundExpression FinalTranslation(QueryTranslationState2 state, DiagnosticBag diagnostics)
        {
            Debug.Assert(state.clauses.IsEmpty());
            switch (state.selectOrGroup.Kind())
            {
                case SyntaxKind.SelectClause2:
                    {
                        // A query expression of the form
                        //     from x in e select v
                        // is translated into
                        //     ( e ) . Select ( x => v )
                        var selectClause = (SelectClause2Syntax)state.selectOrGroup;
                        var x = state.rangeVariable;
                        var e = state.fromExpression;
                        var v = selectClause.Expression;
                        var lambda = MakeQueryUnboundLambda(state.RangeVariableMap(), x, v);
                        var result = MakeQueryInvocation(state.selectOrGroup, e, "Select", lambda, diagnostics);
                        return MakeQueryClause(selectClause, result, queryInvocation: result);
                    }
                default:
                    {
                        // there should have been a syntax error if we get here.
                        return new BoundBadExpression(
                            state.selectOrGroup, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty,
                            ImmutableArray.Create(state.fromExpression), state.fromExpression.Type);
                    }
            }
        }

        private void ReduceQuery(QueryTranslationState2 state, DiagnosticBag diagnostics)
        {
            var topClause = state.clauses.Pop();
            switch (topClause.Kind())
            {
                case SyntaxKind.WhereClause2:
                    ReduceWhere((WhereClause2Syntax)topClause, state, diagnostics);
                    break;
                case SyntaxKind.FromClause2:
                    ReduceFrom((FromClause2Syntax)topClause, state, diagnostics);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(topClause.Kind());
            }
        }

        private void ReduceWhere(WhereClause2Syntax where, QueryTranslationState2 state, DiagnosticBag diagnostics)
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

        private void ReduceFrom(FromClause2Syntax from, QueryTranslationState2 state, DiagnosticBag diagnostics)
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

            if (state.clauses.IsEmpty() && state.selectOrGroup.IsKind(SyntaxKind.SelectClause2))
            {
                var select = (SelectClause2Syntax)state.selectOrGroup;

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

        private UnboundLambda MakePairLambda(CSharpSyntaxNode node, QueryTranslationState2 state, RangeVariableSymbol x1, RangeVariableSymbol x2)
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

        private BoundExpression MakePair(CSharpSyntaxNode node, string field1Name, BoundExpression field1Value, string field2Name, BoundExpression field2Value, QueryTranslationState2 state, DiagnosticBag diagnostics)
        {
            if (field1Name == field2Name)
            {
                // we will generate a diagnostic elsewhere
                field2Name = state.TransparentRangeVariableName();
                field2Value = new BoundBadExpression(field2Value.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create(field2Value), field2Value.Type, true);
            }

            AnonymousTypeDescriptor typeDescriptor = new AnonymousTypeDescriptor(
                                                            ImmutableArray.Create<AnonymousTypeField>(
                                                                new AnonymousTypeField(field1Name, field1Value.Syntax.Location,
                                                                                       TypeSymbolWithAnnotations.Create(TypeOrError(field1Value))),
                                                                new AnonymousTypeField(field2Name, field2Value.Syntax.Location,
                                                                                        TypeSymbolWithAnnotations.Create(TypeOrError(field2Value)))
                                                            ),
                                                            node.Location
                                                     );

            AnonymousTypeManager manager = this.Compilation.AnonymousTypeManager;
            NamedTypeSymbol anonymousType = manager.ConstructAnonymousTypeSymbol(typeDescriptor);
            return MakeConstruction(node, anonymousType, ImmutableArray.Create(field1Value, field2Value), diagnostics);
        }
    }
}
