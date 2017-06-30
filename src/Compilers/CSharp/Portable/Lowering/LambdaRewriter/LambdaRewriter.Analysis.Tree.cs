// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LambdaRewriter
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed partial class Analysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private sealed class Scope
            {
                public Scope Parent { get; }

                public ArrayBuilder<Scope> NestedScopes { get; } = ArrayBuilder<Scope>.GetInstance();

                public ArrayBuilder<Closure> Closures { get; } = ArrayBuilder<Closure>.GetInstance();

                public ArrayBuilder<Symbol> DeclaredCapturedVariables { get; } = ArrayBuilder<Symbol>.GetInstance();

                public BoundNode BoundNode { get; }

                public ArrayBuilder<LambdaFrame> Environments { get; } = ArrayBuilder<LambdaFrame>.GetInstance();

                public Closure ContainingClosure { get; }

                public Scope(Scope parent, BoundNode boundNode, Closure containingClosure)
                {
                    Parent = parent;
                    BoundNode = boundNode;
                    ContainingClosure = containingClosure;
                }

                public void Free()
                {
                    foreach (var scope in NestedScopes)
                    {
                        scope.Free();
                    }
                    NestedScopes.Free();

                    foreach (var closure in Closures)
                    {
                        closure.Free();
                    }
                    Closures.Free();
                    DeclaredCapturedVariables.Free();
                    Environments.Free();
                }
            }

            private sealed class Closure
            {
                public MethodSymbol Symbol { get; }

                public PooledHashSet<Symbol> CapturedVariables { get; } = PooledHashSet<Symbol>.GetInstance();

                public ArrayBuilder<LambdaFrame> CapturedEnvironments { get; } = ArrayBuilder<LambdaFrame>.GetInstance();

                public NamedTypeSymbol ContainingType { get; set; }

                public Closure(MethodSymbol symbol)
                {
                    Symbol = symbol;
                }

                public void Free()
                {
                    CapturedVariables.Free();
                    CapturedEnvironments.Free();
                }
            }

            private void RemoveUnneededReferences2()
            {
                var methodGraph = new MultiDictionary<MethodSymbol, MethodSymbol>();
                var capturesThis = new HashSet<MethodSymbol>();
                var capturesVariable = new HashSet<MethodSymbol>();
                var visitStack = new Stack<MethodSymbol>();
                VisitClosures(_scopeTree, (scope, closure) =>
                {
                    foreach (var capture in closure.CapturedVariables)
                    {
                        if (capture is MethodSymbol localFunc)
                        {
                            methodGraph.Add(localFunc, closure.Symbol);
                        }
                        else if (capture == _topLevelMethod.ThisParameter)
                        {
                            if (capturesThis.Add(closure.Symbol))
                            {
                                visitStack.Push(closure.Symbol);
                            }
                        }
                        else if (capturesVariable.Add(closure.Symbol) && !capturesThis.Contains(closure.Symbol))
                        {
                            visitStack.Push(closure.Symbol);
                        }
                    }
                });

                while (visitStack.Count > 0)
                {
                    var current = visitStack.Pop();
                    var setToAddTo = capturesVariable.Contains(current) ? capturesVariable : capturesThis;
                    foreach (var capturesCurrent in methodGraph[current])
                    {
                        if (setToAddTo.Add(capturesCurrent))
                        {
                            visitStack.Push(capturesCurrent);
                        }
                    }
                }

                VisitClosures(_scopeTree, (scope, closure) =>
                {
                    if (!capturesVariable.Contains(closure.Symbol))
                    {
                        closure.CapturedVariables.Clear();
                    }
                    if (capturesThis.Contains(closure.Symbol))
                    {
                        closure.CapturedVariables.Add(_topLevelMethod.ThisParameter);
                    }
                });
            }

            /// <summary>
            /// Visit all closures in all nested scopes and run the <paramref name="action"/>.
            /// </summary>
            private static void VisitClosures(Scope scope, Action<Scope, Closure> action)
            {
                foreach (var closure in scope.Closures)
                {
                    action(scope, closure);
                }
                foreach (var nested in scope.NestedScopes)
                {
                    VisitClosures(nested, action);
                }
            }

            private void ComputeLambdaScopesAndFrameCaptures2()
            {
                LambdaScopes = new Dictionary<MethodSymbol, BoundNode>(ReferenceEqualityComparer.Instance);
                NeedsParentFrame = new HashSet<BoundNode>();

                VisitClosures(_scopeTree, (scope, closure) =>
                {
                    if (closure.CapturedVariables.Count > 0)
                    {
                        (Scope innermost, Scope outermost) = FindLambdaScopeRange(closure, scope);
                        RecordClosureScope(innermost, outermost, closure);
                    }
                });

                (Scope innermost, Scope outermost) FindLambdaScopeRange(Closure closure, Scope closureScope)
                {
                    Scope innermost = null;
                    Scope outermost = null;

                    var capturedVars = PooledHashSet<Symbol>.GetInstance();
                    capturedVars.AddAll(closure.CapturedVariables);

                    // If any of the captured variables are local functions we'll need
                    // to add the captured variables of that local function to the current
                    // set. This has the effect of ensuring that if the local function
                    // captures anything "above" the current scope then parent frame
                    // is itself captured (so that the current lambda can call that
                    // local function).
                    foreach (var captured in closure.CapturedVariables)
                    {
                        if (captured is LocalFunctionSymbol localFunc)
                        {
                            capturedVars.AddAll(FindClosureInScope(closureScope, localFunc).CapturedVariables);
                        }
                    }

                    for (var curScope = closureScope;
                         curScope != null && capturedVars.Count > 0;
                         curScope = curScope.Parent)
                    {
                        if (!(capturedVars.Overlaps(curScope.DeclaredCapturedVariables) ||
                              capturedVars.Overlaps(curScope.Closures.Select(c => c.Symbol))))
                        {
                            continue;
                        }

                        outermost = curScope;
                        if (innermost == null)
                        {
                            innermost = curScope;
                        }

                        capturedVars.RemoveAll(curScope.DeclaredCapturedVariables);
                        capturedVars.RemoveAll(curScope.Closures.Select(c => c.Symbol));
                    }

                    // If any captured variables are left, they're captured above method scope
                    if (capturedVars.Count > 0)
                    {
                        outermost = null;
                    }

                    capturedVars.Free();

                    return (innermost, outermost);
                }

                // Walk up the scope tree looking for a closure
                Closure FindClosureInScope(Scope startingScope, MethodSymbol closureSymbol)
                {
                    var currentScope = startingScope;
                    while (currentScope != null)
                    {
                        var found = currentScope.Closures.SingleOrDefault(c => c.Symbol == closureSymbol);
                        if (found != null)
                        {
                            return found;
                        }
                        currentScope = currentScope.Parent;
                    }
                    return null;
                }

                void RecordClosureScope(Scope innermost, Scope outermost, Closure closure)
                {
                    // 1) if there is innermost scope, lambda goes there as we cannot go any higher.
                    // 2) scopes in [innermostScope, outermostScope) chain need to have access to the parent scope.
                    //
                    // Example: 
                    //   if a lambda captures a method's parameter and `this`, 
                    //   its innermost scope depth is 0 (method locals and parameters) 
                    //   and outermost scope is -1
                    //   Such lambda will be placed in a closure frame that corresponds to the method's outer block
                    //   and this frame will also lift original `this` as a field when created by its parent.
                    //   Note that it is completely irrelevant how deeply the lexical scope of the lambda was originally nested.
                    if (innermost != null)
                    {
                        LambdaScopes.Add(closure.Symbol, innermost.BoundNode);

                        // Disable struct closures on methods converted to delegates, as well as on async and iterator methods.
                        var markAsNoStruct = !CanTakeRefParameters(closure.Symbol);
                        if (markAsNoStruct)
                        {
                            ScopesThatCantBeStructs.Add(innermost.BoundNode);
                        }

                        while (innermost != outermost)
                        {
                            NeedsParentFrame.Add(innermost.BoundNode);
                            innermost = innermost.Parent;
                            if (markAsNoStruct && innermost != null)
                            {
                                ScopesThatCantBeStructs.Add(innermost.BoundNode);
                            }
                        }
                    }

                }
            }

            private class ScopeTreeBuilder : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                private Scope _currentScope;
                // Null if we're not inside a closure scope, otherwise the nearest closure scope
                private Closure _currentClosure;
                private bool _inExpressionTree = false;

                // A mapping from all captured vars to the scope they were declared in. This
                // is used when recording captured variables as we must know what the lifetime
                // of a captured variable is to determine the lifetime of its capture environment.
                private readonly SmallDictionary<Symbol, Scope> _localToScope = new SmallDictionary<Symbol, Scope>();

                private readonly Analysis _analysis;

                private ScopeTreeBuilder(Scope rootScope, Analysis analysis)
                {
                    _currentScope = rootScope;
                    _analysis = analysis;
                    _currentClosure = null;
                }

                public static Scope Build(BoundNode node, Analysis analysis)
                {
                    // This should be the top-level node
                    Debug.Assert(node == FindNodeToAnalyze(node));

                    var rootScope = new Scope(parent: null, boundNode: node, containingClosure: null);
                    var builder = new ScopeTreeBuilder(rootScope, analysis);
                    builder.Build();
                    return rootScope;
                }

                private void Build()
                {
                    // Set up the current method locals
                    DeclareLocals(_currentScope, _analysis._topLevelMethod.Parameters);
                    Visit(_currentScope.BoundNode);
                }

                public override BoundNode VisitMethodGroup(BoundMethodGroup node)
                    => throw ExceptionUtilities.Unreachable;

                public override BoundNode VisitBlock(BoundBlock node)
                {
                    if (node.Locals.IsDefaultOrEmpty)
                    {
                        // Skip introducing a new scope if there are no new locals
                        return base.VisitBlock(node);
                    }

                    var oldScope = _currentScope;
                    _currentScope = NewNestedScope(node);
                    DeclareLocals(_currentScope, node.Locals);
                    var result = base.VisitBlock(node);
                    _currentScope = oldScope;
                    return result; 
                }

                public override BoundNode VisitCatchBlock(BoundCatchBlock node)
                {
                    var oldScope = _currentScope;
                    _currentScope = NewNestedScope(node);
                    DeclareLocals(_currentScope, node.Locals);
                    var result = base.VisitCatchBlock(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitSequence(BoundSequence node)
                {
                    var oldScope = _currentScope;
                    _currentScope = NewNestedScope(node);
                    DeclareLocals(_currentScope, node.Locals);
                    var result = base.VisitSequence(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
                {
                    if (node.InnerLocals.IsDefaultOrEmpty)
                    {
                        // Skip introducing a new scope if there are no new locals
                        return base.VisitSwitchStatement(node);
                    }

                    var oldScope = _currentScope;
                    _currentScope = NewNestedScope(node);
                    DeclareLocals(_currentScope, node.InnerLocals);
                    var result = base.VisitSwitchStatement(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitLambda(BoundLambda node)
                {
                    var oldInExpressionTree = _inExpressionTree;
                    _inExpressionTree |= node.Type.IsExpressionTree();

                    _analysis.MethodsConvertedToDelegates.Add(node.Symbol.OriginalDefinition);
                    var result = VisitClosure(node.Symbol, node.Body);

                    _inExpressionTree = oldInExpressionTree;
                    return result;
                }

                public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
                    => VisitClosure(node.Symbol.OriginalDefinition, node.Body);

                public override BoundNode VisitCall(BoundCall node)
                {
                    if (node.Method.MethodKind == MethodKind.LocalFunction)
                    {
                        // Use OriginalDefinition to strip generic type parameters
                        AddIfCaptured(node.Method.OriginalDefinition);
                    }
                    return base.VisitCall(node);
                }

                public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
                {
                    if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
                    {
                        // Use OriginalDefinition to strip generic type parameters
                        var method = node.MethodOpt.OriginalDefinition;
                        AddIfCaptured(method);
                        _analysis.MethodsConvertedToDelegates.Add(method);
                    }
                    return base.VisitDelegateCreationExpression(node);
                }

                public override BoundNode VisitParameter(BoundParameter node)
                {
                    AddIfCaptured(node.ParameterSymbol);
                    return base.VisitParameter(node);
                }

                public override BoundNode VisitLocal(BoundLocal node)
                {
                    AddIfCaptured(node.LocalSymbol);
                    return base.VisitLocal(node);
                }

                public override BoundNode VisitBaseReference(BoundBaseReference node)
                {
                    AddIfCaptured(_analysis._topLevelMethod.ThisParameter);
                    return base.VisitBaseReference(node);
                }

                public override BoundNode VisitThisReference(BoundThisReference node)
                {
                    var thisParam = _analysis._topLevelMethod.ThisParameter;
                    if (thisParam != null)
                    {
                        AddIfCaptured(thisParam);
                    }
                    else
                    {
                        // This can occur in a delegate creation expression because the method group
                        // in the argument can have a "this" receiver even when "this"
                        // is not captured because a static method is selected.  But we do preserve
                        // the method group and its receiver in the bound tree.
                        // No need to capture "this" in such case.

                        // TODO: Why don't we drop "this" while lowering if method is static? 
                        //       Actually, considering that method group expression does not evaluate to a particular value 
                        //       why do we have it in the lowered tree at all?
                    }

                    return base.VisitThisReference(node);
                }

                private BoundNode VisitClosure(MethodSymbol closureSymbol, BoundBlock body)
                {
                    Debug.Assert((object)closureSymbol != null);

                    // Closure is declared (lives) in the parent scope, but its
                    // variables are in a nested scope
                    var closure = new Closure(closureSymbol);
                    _currentScope.Closures.Add(closure);

                    var oldClosure = _currentClosure;
                    _currentClosure = closure;

                    var oldScope = _currentScope;
                    _currentScope = NewNestedScope(body);

                    BoundNode result;
                    if (!_inExpressionTree)
                    {
                        // For the purposes of scoping, parameters live in the same scope as the
                        // closure block
                        DeclareLocals(_currentScope, closureSymbol.Parameters);
                        result = VisitBlock(body);
                    }
                    else
                    {
                        result = base.VisitBlock(body);
                    }

                    _currentScope = oldScope;
                    _currentClosure = oldClosure;
                    return result;
                }

                private void AddIfCaptured(Symbol symbol)
                {
                    if (_currentClosure == null)
                    {
                        // Can't be captured if we're not in a closure
                        return;
                    }

                    if (symbol is LocalSymbol local && local.IsConst)
                    {
                        // consts aren't captured since they're inlined
                        return;
                    }

                    if (symbol.ContainingSymbol != _currentClosure.Symbol)
                    {
                        // Record the captured variable where it's captured
                        var scope = _currentScope;
                        var closure = _currentClosure;
                        while (closure != null && symbol.ContainingSymbol != closure.Symbol)
                        {
                            closure.CapturedVariables.Add(symbol);

                            // Also mark captured in enclosing scopes
                            while (scope.ContainingClosure == closure)
                            {
                                scope = scope.Parent;
                            }
                            closure = scope.ContainingClosure;
                        }

                        // Also record where the captured variable lives

                        // No need to record where local functions live: that was recorded
                        // in the Closures list in each scope
                        if (symbol.Kind == SymbolKind.Method)
                        {
                            return;
                        }

                        // The 'this' parameter isn't declared in method scope
                        if (symbol is ParameterSymbol param && param.IsThis)
                        {
                            return;
                        }

                        if (_localToScope.TryGetValue(symbol, out var declScope))
                        {
                            declScope.DeclaredCapturedVariables.Add(symbol);
                        }
                        else
                        {
                            // Parameters and locals from expression tree lambdas
                            // don't get recorded
                            Debug.Assert(_inExpressionTree);
                        }
                    }
                }

                private Scope NewNestedScope(BoundNode node)
                {
                    // We should never create a new scope with the same bound
                    // node. We can get into this situation for methods and
                    // closures where a new scope is created to add parameters
                    // and a new scope would be created for the method block,
                    // despite the fact that they should be the same scope.
                    if (_currentScope.BoundNode == node)
                    {
                        return _currentScope;
                    }

                    var newScope = new Scope(_currentScope, node, _currentClosure);
                    _currentScope.NestedScopes.Add(newScope);
                    return newScope;
                }

                private void DeclareLocals<TSymbol>(Scope scope, ImmutableArray<TSymbol> locals)
                    where TSymbol : Symbol
                {
                    foreach (var local in locals)
                    {
                        Debug.Assert(!_localToScope.ContainsKey(local));
                        _localToScope[local] = scope;
                    }
                }
            }
        }
    }
}
