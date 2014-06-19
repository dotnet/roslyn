using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    partial class Binder
    {

        private partial class QueryBinder : Binder
        {
            const string transparentIdentifierPrefix = "<>h__TransparentIdentifier";

            public QueryBinder(Binder next)
                : base(next)
            {
            }

            internal BoundExpression BindQueryInternal1(QueryExpressionSyntax node, DiagnosticBag diagnostics)
            {
                var fromClause = (FromClauseSyntax)node.Clauses[0];
                QueryTranslationState state = new QueryTranslationState();
                state.fromSyntax = fromClause;
                state.fromExpression = BindValue(fromClause.Expression, BindValueKind.RValue, diagnostics);
                state.queryVariable = state.AddQueryVariable(this, fromClause.Identifier);
                for (int i = node.Clauses.Count - 1; i > 0; i--)
                {
                    state.clauses.Push(node.Clauses[i]);
                }

                state.selectOrGroup = node.SelectOrGroup;

                // A from clause that explicitly specifies a range variable type
                //     from T x in e
                // is translated into
                //     from x in ( e ) . Cast < T > ( )
                MethodSymbol castMethod = null;
                if (fromClause.TypeOpt != null)
                {
                    var typeRestriction = BindType(fromClause.TypeOpt, diagnostics);
                    var cast = MakeInvocation(state.fromSyntax, state.fromExpression, "Cast", fromClause.TypeOpt, typeRestriction, diagnostics);
                    castMethod = cast.Method;
                    state.fromExpression = cast;
                }

                state.fromExpression = new BoundQueryClause(
                    syntax: fromClause,
                    syntaxTree: this.SyntaxTree,
                    value: state.fromExpression,
                    definedSymbol: state.queryVariable,
                    queryMethod: null,
                    castMethod: castMethod,
                    type: state.fromExpression.Type);

                // If the query is a degenerate one the form "from x in e select x", but in source,
                // then we go ahead and generate the select anyway.  We do this by skipping BindQueryInternal2,
                // whose job it is to (reduce away the whole query and) optimize away degenerate queries.
                BoundExpression result = (fromClause.TypeOpt == null && IsDegenerateQuery(state))
                    ? FinalTranslation(state, diagnostics)
                    : BindQueryInternal2(state, diagnostics);

                QueryContinuationSyntax continuation = node.ContinuationOpt;
                while (continuation != null)
                {
                    // A query expression with a continuation
                    //     from … into x …
                    // is translated into
                    //     from x in ( from … ) …
                    state.Clear();
                    state.fromExpression = result;
                    var x = state.AddQueryVariable(this, continuation.Identifier);
                    state.fromExpression = new BoundQueryClause(
                        syntax: continuation,
                        syntaxTree: SyntaxTree,
                        value: state.fromExpression,
                        definedSymbol: x,
                        queryMethod: null,
                        castMethod: null,
                        type: state.fromExpression.Type);
                    state.queryVariable = x;
                    Debug.Assert(state.clauses.IsEmpty());
                    var clauses = continuation.Query.Clauses;
                    for (int i = clauses.Count - 1; i >= 0; i--)
                    {
                        state.clauses.Push(clauses[i]);
                    }

                    state.selectOrGroup = continuation.Query.SelectOrGroup;
                    result = BindQueryInternal2(state, diagnostics);
                    continuation = continuation.Query.ContinuationOpt;
                }

                state.Free();
                return result;
            }

            private bool IsDegenerateQuery(QueryTranslationState state)
            {
                if (!state.clauses.IsEmpty()) return false;

                // Some translations complete the whole query, which they flag by setting selectOrGroup to null.
                if (state.selectOrGroup == null) return true;

                // A degenerate query is of the form "from x in e select x".
                var select = state.selectOrGroup as SelectClauseSyntax;
                if (select == null) return false;
                var identifier = select.Expression as IdentifierNameSyntax;
                return identifier != null && state.queryVariable.Name == identifier.Identifier.ValueText;
            }

            BoundExpression BindQueryInternal2(QueryTranslationState state, DiagnosticBag diagnostics)
            {
                // we continue reducing the query until it is reduced away.
                while (true)
                {
                    if (state.clauses.IsEmpty())
                    {
                        if (IsDegenerateQuery(state))
                        {
                            // A query expression of the form
                            //     from x in e select x
                            // is translated into
                            //     ( e )
                            return state.fromExpression;
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
                            var x = state.queryVariable;
                            var e = state.fromExpression;
                            var v = selectClause.Expression;
                            var lambda = MakeQueryUnboundLambda(state.QueryVariableMap(), x, v);
                            var result = MakeInvocation(state.selectOrGroup, e, "Select", lambda, diagnostics);
                            return new BoundQueryClause(
                                syntax: selectClause,
                                syntaxTree: SyntaxTree,
                                value: result,
                                definedSymbol: null,
                                queryMethod: null,
                                castMethod: null,
                                type: result.Type);
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
                            var x = state.queryVariable;
                            var e = state.fromExpression;
                            var v = groupClause.GroupExpression;
                            var k = groupClause.ByExpression;
                            var vId = v as IdentifierNameSyntax;
                            BoundCall result;
                            if (vId != null && vId.Identifier.ValueText == x.Name)
                            {
                                var lambda = MakeQueryUnboundLambda(state.QueryVariableMap(), x, k);
                                result = MakeInvocation(state.selectOrGroup, e, "GroupBy", lambda, diagnostics);
                            }
                            else
                            {
                                BoundExpression lambdaLeft = MakeQueryUnboundLambda(state.QueryVariableMap(), x, k);
                                BoundExpression lambdaRight = MakeQueryUnboundLambda(state.QueryVariableMap(), x, v);
                                result = MakeInvocation(state.selectOrGroup, e, "GroupBy", Args(lambdaLeft, lambdaRight), diagnostics);
                            }
                            return new BoundQueryClause(
                                syntax: groupClause,
                                syntaxTree: syntaxTree,
                                value: result,
                                definedSymbol: null,
                                queryMethod: result.Method,
                                castMethod: null,
                                type: result.Type);
                        }
                    default:
                        {
                            // there should have been a syntax error if we get here.
                            return new BoundBadExpression(
                                state.selectOrGroup, SyntaxTree, LookupResultKind.OverloadResolutionFailure, ReadOnlyArray<Symbol>.Empty,
                                Args<BoundNode>(state.fromExpression), state.fromExpression.Type);
                        }
                }
            }

            void ReduceQuery(QueryTranslationState state, DiagnosticBag diagnostics)
            {
                var topClause = state.clauses.Pop();
                if (topClause.Kind == SyntaxKind.WhereClause)
                {
                    // A query expression with a where clause
                    //     from x in e
                    //     where f
                    //     …
                    // is translated into
                    //     from x in ( e ) . Where ( x => f )
                    var where = topClause as WhereClauseSyntax;
                    var lambda = MakeQueryUnboundLambda(state.QueryVariableMap(), state.queryVariable, where.Condition);
                    var invocation = MakeInvocation(where, state.fromExpression, "Where", lambda, diagnostics);
                    state.fromExpression = new BoundQueryClause(
                        syntax: where,
                        syntaxTree: SyntaxTree,
                        value: invocation,
                        definedSymbol: null,
                        queryMethod: invocation.Method,
                        castMethod: null,
                        type: invocation.Type);
                }
                else if (topClause.Kind == SyntaxKind.JoinClause && state.clauses.IsEmpty() && state.selectOrGroup.Kind == SyntaxKind.SelectClause)
                {
                    var join = topClause as JoinClauseSyntax;
                    var select = state.selectOrGroup as SelectClauseSyntax;
                    var joinArgs = ArrayBuilder<BoundExpression>.GetInstance();
                    var e2 = BindValue(join.InExpression, BindValueKind.RValue, diagnostics);
                    MethodSymbol castMethod = null;
                    if (join.TypeOpt != null)
                    {
                        // A join clause that explicitly specifies a range variable type
                        //     join T x in e on k1 equals k2
                        // is translated into
                        //     join x in ( e ) . Cast < T > ( ) on k1 equals k2
                        var castType = BindType(join.TypeOpt, diagnostics);
                        var invocation = MakeInvocation(join, e2, "Cast", join.TypeOpt, castType, diagnostics);
                        castMethod = invocation.Method;
                        e2 = invocation;
                    }

                    joinArgs.Add(e2);
                    var lambda1 = MakeQueryUnboundLambda(state.QueryVariableMap(), state.queryVariable, join.LeftExpression);
                    joinArgs.Add(lambda1);
                    var x2 = state.AddQueryVariable(this, join.Identifier);
                    var lambda2 = MakeQueryUnboundLambda(state.QueryVariableMap(x2), x2, join.RightExpression); // TODO: ensure no other query variables in scope but x2.
                    joinArgs.Add(lambda2);
                    BoundExpression result;
                    if (join.IntoOpt == null)
                    {
                        // A query expression with a join clause without an into followed by a select clause
                        //     from x1 in e1
                        //     join x2 in e2 on k1 equals k2
                        //     select v
                        // is translated into
                        //     ( e1 ) . Join( e2 , x1 => k1 , x2 => k2 , ( x1 , x2 ) => v )
                        var lambda3 = MakeQueryUnboundLambda(state.QueryVariableMap(), Args(state.queryVariable, x2), select.Expression);
                        // TODO: lambda3's body should be surrounded by a BoundQueryClause for the select clause.
                        joinArgs.Add(lambda3);
                        var invocation = MakeInvocation(join, state.fromExpression, "Join", joinArgs.ToReadOnlyAndFree(), diagnostics);
                        result = new BoundQueryClause(
                            syntax: join,
                            syntaxTree: SyntaxTree,
                            value: invocation,
                            definedSymbol: x2,
                            queryMethod: invocation.Method,
                            castMethod: castMethod,
                            type: invocation.Type);
                    }
                    else
                    {
                        // A query expression with a join clause with an into followed by a select clause
                        //     from x1 in e1
                        //     join x2 in e2 on k1 equals k2 into g
                        //     select v
                        // is translated into
                        //     ( e1 ) . GroupJoin( e2 , x1 => k1 , x2 => k2 , ( x1 , g ) => v )
                        var g = state.AddQueryVariable(this, join.IntoOpt.Identifier);
                        // binder.queryVariable = g; // TODO: where to record the info symbol?
                        var lambda3 = MakeQueryUnboundLambda(state.QueryVariableMap(), Args(state.queryVariable, g), select.Expression);
                        // TODO: lambda3's body should be surrounded by a BoundQueryClause for the select clause.
                        joinArgs.Add(lambda3);
                        var invocation = MakeInvocation(join, state.fromExpression, "GroupJoin", joinArgs.ToReadOnlyAndFree(), diagnostics);
                        var newArguments = Args(invocation.Arguments[0], invocation.Arguments[1], invocation.Arguments[2],
                            new BoundQueryClause( // record the into clause's symbol in the bound tree
                                syntax: join.IntoOpt,
                                syntaxTree: SyntaxTree,
                                value: invocation.Arguments[3],
                                definedSymbol: g,
                                queryMethod: null,
                                castMethod: null,
                                type: invocation.Arguments[3].Type));
                        invocation = invocation.Update(
                            receiverOpt: invocation.ReceiverOpt,
                            method: invocation.Method,
                            arguments: newArguments);
                        result = new BoundQueryClause(
                            syntax: join,
                            syntaxTree: SyntaxTree,
                            value: invocation,
                            definedSymbol: x2,
                            queryMethod: invocation.Method,
                            castMethod: castMethod,
                            type: invocation.Type);
                    }

                    state.Clear(); // this completes the whole query
                    state.fromExpression = result;
                }
                else if (topClause.Kind == SyntaxKind.OrderByClause)
                {
                    // A query expression with an orderby clause
                    //     from x in e
                    //     orderby k1 , k2 , … , kn
                    //     …
                    // is translated into
                    //     from x in ( e ) . 
                    //     OrderBy ( x => k1 ) . 
                    //     ThenBy ( x => k2 ) .
                    //     … .
                    //     ThenBy ( x => kn )
                    //     …
                    // If an ordering clause specifies a descending direction indicator,
                    // an invocation of OrderByDescending or ThenByDescending is produced instead.
                    var orderby = topClause as OrderByClauseSyntax;
                    bool first = true;
                    Symbol lastMethod = null;
                    foreach (var ordering in orderby.Orderings)
                    {
                        string methodName = (first ? "OrderBy" : "ThenBy") + (ordering.Kind == SyntaxKind.DescendingOrdering ? "Descending" : "");
                        var lambda = MakeQueryUnboundLambda(state.QueryVariableMap(), state.queryVariable, ordering.Expression);
                        var invocation = MakeInvocation(ordering, state.fromExpression, methodName, lambda, diagnostics);
                        lastMethod = invocation.Method;
                        state.fromExpression = new BoundQueryClause(
                            syntax: ordering, syntaxTree: SyntaxTree, value: invocation,
                            definedSymbol: null, queryMethod: invocation.Method, castMethod: null, type: invocation.Type);
                        first = false;
                    }

                    state.fromExpression = new BoundQueryClause(
                        syntax: orderby, syntaxTree: SyntaxTree, value: state.fromExpression,
                        definedSymbol: null, queryMethod: lastMethod, castMethod: null, type: state.fromExpression.Type);
                }
                else if (topClause.Kind == SyntaxKind.FromClause && state.clauses.IsEmpty() && state.selectOrGroup.Kind == SyntaxKind.SelectClause)
                {
                    // A query expression with a second from clause followed by a select clause
                    //     from x1 in e1
                    //     from x2 in e2
                    //     select v
                    // is translated into
                    //     ( e1 ) . SelectMany( x1 => e2 , ( x1 , x2 ) => v )
                    var fromClause = topClause as FromClauseSyntax;
                    var select = state.selectOrGroup as SelectClauseSyntax;
                    var x1 = state.queryVariable;
                    TypeSymbol castType = fromClause.TypeOpt == null ? null : BindType(fromClause.TypeOpt, diagnostics);
                    BoundExpression lambda1 = MakeQueryUnboundLambda(state.QueryVariableMap(), x1, fromClause.Expression, fromClause.TypeOpt, castType);
                    // TODO: wrap the bound version of lambda1 in a BoundQueryClause for the from clause defining  x2.
                    var x2 = state.AddQueryVariable(this, fromClause.Identifier);
                    BoundExpression lambda2 = MakeQueryUnboundLambda(state.QueryVariableMap(), Args(x1, x2), select.Expression);
                    var result = MakeInvocation(fromClause, state.fromExpression, "SelectMany", Args(lambda1, lambda2), diagnostics);
                    // TODO: extract the Cast<T>() operation from the bound version of lambda1 (this invocation's first argument) and store it in e2Binder.castMethod
                    state.Clear();
                    state.fromExpression = new BoundQueryClause(
                        syntax: select, syntaxTree: SyntaxTree, value: result,
                        definedSymbol: null, queryMethod: result.Method, castMethod: null, type: result.Type);
                }
                else if (topClause.Kind == SyntaxKind.LetClause)
                {
                    var let = topClause as LetClauseSyntax;
                    var x = state.queryVariable;

                    // A query expression with a let clause
                    //     from x in e
                    //     let y = f
                    //     …
                    // is translated into
                    //     from * in ( e ) . Select ( x => new { x , y = f } )
                    //     …

                    // We use a slightly different translation strategy.  We produce
                    //     from * in ( e ) . Select ( x => new Pair<X,Y>(x, f) )
                    // Where X is the type of x, and Y is the type of the expression f.
                    // Subsequently, x (or members of x, if it is a transparent identifier)
                    // are accessed as TRID.Item1 (or members of that), and y is accessed
                    // as TRID.Item2, where TRID is the compiler-generated identifier used
                    // to represent the transparent identifier in the result.  We place
                    // this mapping into the state and then, subsequently, into the binder
                    // for any further clauses.
                    LambdaBodyResolver resolver = (LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag d) =>
                    {
                        var xExpression = new BoundParameter(let, lambdaBodyBinder.SyntaxTree, lambdaSymbol.Parameters[0]);
                        var yExpression = lambdaBodyBinder.BindValue(let.Expression, BindValueKind.RValue, d);
                        var construction = MakePair(let, xExpression, yExpression, d);
                        return lambdaBodyBinder.WrapExpressionLambdaBody(construction, let, d);
                    };
                    var lambda = MakeQueryUnboundLambda(state.QueryVariableMap(), x, let.Expression, resolver);
                    state.queryVariable = state.TransparentQueryVariable(this);
                    var invocation = MakeInvocation(let, state.fromExpression, "Select", lambda, diagnostics);
                    state.AddTransparentIdentifier("Item1");
                    var y = state.AddQueryVariable(this, let.Identifier);
                    state.allQueryVariables[y].Add("Item2");
                    state.fromExpression = new BoundQueryClause(
                        syntax: let, syntaxTree: SyntaxTree, value: invocation,
                        definedSymbol: y, queryMethod: invocation.Method, castMethod: null, type: invocation.Type);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_NotYetImplementedInRoslyn, Location(topClause), "query expression");
                    var result = state.fromExpression; // short circuit any remaining reductions
                    state.Clear();
                    state.fromExpression = result;
                }
            }

            static ReadOnlyArray<T> Args<T>(params T[] args)
            {
                return ReadOnlyArray<T>.CreateFrom(args);
            }

            BoundExpression MakePair(SyntaxNode node, BoundExpression item1, BoundExpression item2, DiagnosticBag diagnostics)
            {
                var pairType = GetWellKnownType(WellKnownType.System_Tuple2, diagnostics, node);
                var constructedPairType = pairType.Construct(item1.Type, item2.Type);
                return MakeConstruction(node, constructedPairType, Args(item1, item2), diagnostics);
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, ReadOnlyArray<QueryVariableSymbol> parameters, ExpressionSyntax expression)
            {
                // generate the unbound lambda expression (parameters) => expression
                return MakeQueryUnboundLambda(qvm, parameters, expression, null, null);
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, QueryVariableSymbol parameter, ExpressionSyntax expression)
            {
                return MakeQueryUnboundLambda(qvm, Args(parameter), expression);
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, QueryVariableSymbol parameter, ExpressionSyntax expression, TypeSyntax castTypeSyntaxOpt, TypeSymbol castTypeOpt)
            {
                return MakeQueryUnboundLambda(qvm, Args(parameter), expression, castTypeSyntaxOpt, castTypeOpt);
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, ReadOnlyArray<QueryVariableSymbol> parameters, ExpressionSyntax expression, TypeSyntax castTypeSyntaxOpt, TypeSymbol castTypeOpt)
            {
                var state = (castTypeOpt == null)
                    ? new QueryUnboundLambdaState(null, this, qvm, parameters, expression)
                    : new QueryUnboundLambdaState(null, this, qvm, parameters, expression, castTypeSyntaxOpt, castTypeOpt);
                var lambda = new UnboundLambda(expression, SyntaxTree, state, false);
                state.SetUnboundLambda(lambda);
                return lambda;
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, QueryVariableSymbol parameter, SyntaxNode node, LambdaBodyResolver resolver)
            {
                return MakeQueryUnboundLambda(qvm, Args(parameter), node, resolver);
            }

            UnboundLambda MakeQueryUnboundLambda(QueryVariableMap qvm, ReadOnlyArray<QueryVariableSymbol> parameters, SyntaxNode node, LambdaBodyResolver resolver)
            {
                var state = new QueryUnboundLambdaState(null, this, qvm, parameters, resolver);
                var lambda = new UnboundLambda(node, SyntaxTree, state, false);
                state.SetUnboundLambda(lambda);
                return lambda;
            }

        }

        protected BoundCall MakeInvocation(SyntaxNode node, BoundExpression receiver, string methodName, BoundExpression arg, DiagnosticBag diagnostics)
        {
            return MakeInvocation(node, receiver, methodName, default(SeparatedSyntaxList<TypeSyntax>), ReadOnlyArray<TypeSymbol>.Empty, ReadOnlyArray.Singleton(arg), diagnostics);
        }

        protected BoundCall MakeInvocation(SyntaxNode node, BoundExpression receiver, string methodName, ReadOnlyArray<BoundExpression> args, DiagnosticBag diagnostics)
        {
            return MakeInvocation(node, receiver, methodName, default(SeparatedSyntaxList<TypeSyntax>), ReadOnlyArray<TypeSymbol>.Empty, args, diagnostics);
        }

        protected BoundCall MakeInvocation(SyntaxNode node, BoundExpression receiver, string methodName, TypeSyntax typeArgSyntax, TypeSymbol typeArg, DiagnosticBag diagnostics)
        {
            return MakeInvocation(node, receiver, methodName, new SeparatedSyntaxList<TypeSyntax>(new SyntaxNodeOrTokenList(typeArgSyntax)), ReadOnlyArray.Singleton(typeArg), ReadOnlyArray<BoundExpression>.Empty, diagnostics);
        }

        protected BoundCall MakeInvocation(SyntaxNode node, BoundExpression receiver, string methodName, SeparatedSyntaxList<TypeSyntax> typeArgsSyntax, ReadOnlyArray<TypeSymbol> typeArgs, ReadOnlyArray<BoundExpression> args, DiagnosticBag diagnostics)
        {
            var boundExpression = BindInstanceMemberAccess(node, node, receiver, methodName, typeArgs.Count, typeArgsSyntax, typeArgs, true, diagnostics);
            boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
            var analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.AddRange(args);
            var result = BindInvocationExpression(node, node, methodName, boundExpression, analyzedArguments, diagnostics);
            analyzedArguments.Free();
            return result;
        }

        protected BoundExpression MakeConstruction(SyntaxNode node, NamedTypeSymbol toCreate, ReadOnlyArray<BoundExpression> args, DiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.AddRange(args);
            var result = BindClassCreationExpression(node, toCreate.Name, node, toCreate, analyzedArguments, toCreate.InstanceConstructors, diagnostics);
            analyzedArguments.Free();
            return result;
        }

    }
}