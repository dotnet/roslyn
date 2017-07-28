﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LambdaRewriter
    {
        internal sealed partial class Analysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            /// <summary>
            /// This is the core node for a Scope tree, which stores all semantically meaningful
            /// information about declared variables, closures, and environments in each scope.
            /// It can be thought of as the essence of the bound tree -- stripping away many of
            /// the unnecessary details stored in the bound tree and just leaving the pieces that
            /// are important for closure conversion. The root scope is the method scope for the
            /// method being analyzed and has a null <see cref="Parent" />.
            /// </summary>
            [DebuggerDisplay("{ToString(), nq}")]
            public sealed class Scope
            {
                public Scope Parent { get; }

                public ArrayBuilder<Scope> NestedScopes { get; } = ArrayBuilder<Scope>.GetInstance();

                /// <summary>
                /// A list of all closures (all lambdas and local functions) declared in this scope.
                /// </summary>
                public ArrayBuilder<Closure> Closures { get; } = ArrayBuilder<Closure>.GetInstance();

                /// <summary>
                /// A list of all locals or parameters that were declared in this scope and captured
                /// in this scope or nested scopes. "Declared" refers to the start of the variable
                /// lifetime (which, at this point in lowering, should be equivalent to lexical scope).
                /// </summary>
                /// <remarks>
                /// It's important that this is a set and that enumeration order is deterministic. We loop
                /// over this list to generate proxies and if we loop out of order this will cause
                /// non-deterministic compilation, and if we generate duplicate proxies we'll generate
                /// wasteful code in the best case and incorrect code in the worst.
                /// </remarks>
                public SetWithInsertionOrder<Symbol> DeclaredVariables { get; } = new SetWithInsertionOrder<Symbol>();

                /// <summary>
                /// The bound node representing this scope. This roughly corresponds to the bound
                /// node for the block declaring locals for this scope, although parameters of
                /// methods/closures are introduced into their Body's scope and do not get their
                /// own scope.
                /// </summary>
                public BoundNode BoundNode { get; }

                /// <summary>
                /// The closure that this scope is nested inside. Null if this scope is not nested
                /// inside a closure.
                /// </summary>
                public Closure ContainingClosureOpt { get; }

                public Scope(Scope parent, BoundNode boundNode, Closure containingClosure)
                {
                    Debug.Assert(boundNode != null);

                    Parent = parent;
                    BoundNode = boundNode;
                    ContainingClosureOpt = containingClosure;
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
                }

                public override string ToString() => BoundNode.Syntax.GetText().ToString();
            }

            /// <summary>
            /// The Closure type represents a lambda or local function and stores
            /// information related to that closure. After initially building the
            /// <see cref="Scope"/> tree the only information available is
            /// <see cref="OriginalMethodSymbol"/> and <see cref="CapturedVariables"/>.
            /// Subsequent passes are responsible for translating captured
            /// variables into captured environments and for calculating
            /// the rewritten signature of the method.
            /// </summary>
            public sealed class Closure
            {
                /// <summary>
                /// The method symbol for the original lambda or local function.
                /// </summary>
                public MethodSymbol OriginalMethodSymbol { get; }

                public PooledHashSet<Symbol> CapturedVariables { get; } = PooledHashSet<Symbol>.GetInstance();

                public Closure(MethodSymbol symbol)
                {
                    Debug.Assert(symbol != null);
                    OriginalMethodSymbol = symbol;
                }

                public void Free()
                {
                    CapturedVariables.Free();
                }
            }

            /// <summary>
            /// Optimizes local functions such that if a local function only references other local functions
            /// that capture no variables, we don't need to create capture environments for any of them.
            /// </summary>
            private void RemoveUnneededReferences(ParameterSymbol thisParam)
            {
                var methodGraph = new MultiDictionary<MethodSymbol, MethodSymbol>();
                var capturesThis = new HashSet<MethodSymbol>();
                var capturesVariable = new HashSet<MethodSymbol>();
                var visitStack = new Stack<MethodSymbol>();
                VisitClosures(ScopeTree, (scope, closure) =>
                {
                    foreach (var capture in closure.CapturedVariables)
                    {
                        if (capture is MethodSymbol localFunc)
                        {
                            methodGraph.Add(localFunc, closure.OriginalMethodSymbol);
                        }
                        else if (capture == thisParam)
                        {
                            if (capturesThis.Add(closure.OriginalMethodSymbol))
                            {
                                visitStack.Push(closure.OriginalMethodSymbol);
                            }
                        }
                        else if (capturesVariable.Add(closure.OriginalMethodSymbol) &&
                                 !capturesThis.Contains(closure.OriginalMethodSymbol))
                        {
                            visitStack.Push(closure.OriginalMethodSymbol);
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

                VisitClosures(ScopeTree, (scope, closure) =>
                {
                    if (!capturesVariable.Contains(closure.OriginalMethodSymbol))
                    {
                        closure.CapturedVariables.Clear();
                    }
                    if (capturesThis.Contains(closure.OriginalMethodSymbol))
                    {
                        closure.CapturedVariables.Add(thisParam);
                    }
                });
            }

            /// <summary>
            /// Visit all closures in all nested scopes and run the <paramref name="action"/>.
            /// </summary>
            public static void VisitClosures(Scope scope, Action<Scope, Closure> action)
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

            /// <summary>
            /// Builds a tree of <see cref="Scope"/> nodes corresponding to a given method.
            /// <see cref="Build(BoundNode, MethodSymbol, HashSet{MethodSymbol}, DiagnosticBag)"/>
            /// visits the bound tree and translates information from the bound tree about
            /// variable scope, declared variables, and variable captures into the resulting
            /// <see cref="Scope"/> tree.
            /// </summary>
            private class ScopeTreeBuilder : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                private Scope _currentScope;
                /// <summary>
                /// Null if we're not inside a closure scope, otherwise the nearest closure scope
                /// </summary>
                private Closure _currentClosure = null;
                private bool _inExpressionTree = false;

                /// <summary>
                /// A mapping from all captured vars to the scope they were declared in. This
                /// is used when recording captured variables as we must know what the lifetime
                /// of a captured variable is to determine the lifetime of its capture environment.
                /// </summary>
                private readonly SmallDictionary<Symbol, Scope> _localToScope = new SmallDictionary<Symbol, Scope>();

                private readonly MethodSymbol _topLevelMethod;

                /// <summary>
                /// If a local function is in the set, at some point in the code it is converted
                /// to a delegate and should then not be optimized to a struct closure.
                /// Also contains all lambdas (as they are converted to delegates implicitly).
                /// </summary>
                private readonly HashSet<MethodSymbol> _methodsConvertedToDelegates;
                private readonly DiagnosticBag _diagnostics;

                private ScopeTreeBuilder(
                    Scope rootScope,
                    MethodSymbol topLevelMethod,
                    HashSet<MethodSymbol> methodsConvertedToDelegates,
                    DiagnosticBag diagnostics)
                {
                    Debug.Assert(rootScope != null);
                    Debug.Assert(topLevelMethod != null);
                    Debug.Assert(methodsConvertedToDelegates != null);
                    Debug.Assert(diagnostics != null);

                    _currentScope = rootScope;
                    _topLevelMethod = topLevelMethod;
                    _methodsConvertedToDelegates = methodsConvertedToDelegates;
                    _diagnostics = diagnostics;
                }

                public static Scope Build(
                    BoundNode node,
                    MethodSymbol topLevelMethod,
                    HashSet<MethodSymbol> methodsConvertedToDelegates,
                    DiagnosticBag diagnostics)
                {
                    // This should be the top-level node
                    Debug.Assert(node == FindNodeToAnalyze(node));
                    Debug.Assert(topLevelMethod != null);

                    var rootScope = new Scope(parent: null, boundNode: node, containingClosure: null);
                    var builder = new ScopeTreeBuilder(
                        rootScope,
                        topLevelMethod,
                        methodsConvertedToDelegates,
                        diagnostics);
                    builder.Build();
                    return rootScope;
                }

                private void Build()
                {
                    // Set up the current method locals
                    DeclareLocals(_currentScope, _topLevelMethod.Parameters);
                    Visit(_currentScope.BoundNode);
                }

                public override BoundNode VisitMethodGroup(BoundMethodGroup node)
                    => throw ExceptionUtilities.Unreachable;

                public override BoundNode VisitBlock(BoundBlock node)
                {
                    var oldScope = _currentScope;
                    _currentScope = CreateOrReuseScope(node, node.Locals);
                    var result = base.VisitBlock(node);
                    _currentScope = oldScope;
                    return result; 
                }

                public override BoundNode VisitCatchBlock(BoundCatchBlock node)
                {
                    var oldScope = _currentScope;
                    _currentScope = CreateOrReuseScope(node, node.Locals);
                    var result = base.VisitCatchBlock(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitSequence(BoundSequence node)
                {
                    var oldScope = _currentScope;
                    _currentScope = CreateOrReuseScope(node, node.Locals);
                    var result = base.VisitSequence(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
                {
                    var oldScope = _currentScope;
                    _currentScope = CreateOrReuseScope(node, node.InnerLocals);
                    var result = base.VisitSwitchStatement(node);
                    _currentScope = oldScope;
                    return result;
                }

                public override BoundNode VisitLambda(BoundLambda node)
                {
                    var oldInExpressionTree = _inExpressionTree;
                    _inExpressionTree |= node.Type.IsExpressionTree();

                    _methodsConvertedToDelegates.Add(node.Symbol.OriginalDefinition);
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
                        AddIfCaptured(node.Method.OriginalDefinition, node.Syntax);
                    }
                    return base.VisitCall(node);
                }

                public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
                {
                    if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
                    {
                        // Use OriginalDefinition to strip generic type parameters
                        var method = node.MethodOpt.OriginalDefinition;
                        AddIfCaptured(method, node.Syntax);
                        _methodsConvertedToDelegates.Add(method);
                    }
                    return base.VisitDelegateCreationExpression(node);
                }

                public override BoundNode VisitParameter(BoundParameter node)
                {
                    AddIfCaptured(node.ParameterSymbol, node.Syntax);
                    return base.VisitParameter(node);
                }

                public override BoundNode VisitLocal(BoundLocal node)
                {
                    AddIfCaptured(node.LocalSymbol, node.Syntax);
                    return base.VisitLocal(node);
                }

                public override BoundNode VisitBaseReference(BoundBaseReference node)
                {
                    AddIfCaptured(_topLevelMethod.ThisParameter, node.Syntax);
                    return base.VisitBaseReference(node);
                }

                public override BoundNode VisitThisReference(BoundThisReference node)
                {
                    var thisParam = _topLevelMethod.ThisParameter;
                    if (thisParam != null)
                    {
                        AddIfCaptured(thisParam, node.Syntax);
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
                    _currentScope = CreateNestedScope(body, _currentScope, _currentClosure);

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

                private void AddIfCaptured(Symbol symbol, SyntaxNode syntax)
                {
                    Debug.Assert(
                        symbol.Kind == SymbolKind.Local ||
                        symbol.Kind == SymbolKind.Parameter ||
                        symbol.Kind == SymbolKind.Method);

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

                    if (symbol.ContainingSymbol != _currentClosure.OriginalMethodSymbol)
                    {
                        // Restricted types can't be hoisted, so they are not permitted to be captured
                        AddDiagnosticIfRestrictedType(symbol, syntax);

                        // Record the captured variable where it's captured
                        var scope = _currentScope;
                        var closure = _currentClosure;
                        while (closure != null && symbol.ContainingSymbol != closure.OriginalMethodSymbol)
                        {
                            closure.CapturedVariables.Add(symbol);

                            // Also mark captured in enclosing scopes
                            while (scope.ContainingClosureOpt == closure)
                            {
                                scope = scope.Parent;
                            }
                            closure = scope.ContainingClosureOpt;
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
                            declScope.DeclaredVariables.Add(symbol);
                        }
                        else
                        {
                            // Parameters and locals from expression tree lambdas
                            // don't get recorded
                            Debug.Assert(_inExpressionTree);
                        }
                    }
                }

                /// <summary>
                /// Add a diagnostic if the type of a captured variable is a restricted type
                /// </summary>
                private void AddDiagnosticIfRestrictedType(Symbol capturedVariable, SyntaxNode syntax)
                {
                    TypeSymbol type;
                    switch (capturedVariable.Kind)
                    {
                        case SymbolKind.Local:
                            type = ((LocalSymbol)capturedVariable).Type;
                            break;
                        case SymbolKind.Parameter:
                            type = ((ParameterSymbol)capturedVariable).Type;
                            break;
                        default:
                            // This should only be called for captured variables, and captured
                            // variables must be a method, parameter, or local symbol
                            Debug.Assert(capturedVariable.Kind == SymbolKind.Method);
                            return;
                    }

                    if (type.IsRestrictedType() == true)
                    {
                        _diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, type);
                    }
                }

                /// <summary>
                /// Create a new nested scope under the current scope, or reuse the current
                /// scope if there's no change in the bound node for the nested scope.
                /// Records the given locals as declared in the aforementioned scope.
                /// </summary>
                private Scope CreateOrReuseScope<TSymbol>(BoundNode node, ImmutableArray<TSymbol> locals)
                    where TSymbol : Symbol
                {
                    Scope scope;
                    if (locals.IsEmpty || _currentScope.BoundNode == node)
                    {
                        // We should never create a new scope with the same bound
                        // node. We can get into this situation for methods and
                        // closures where a new scope is created to add parameters
                        // and a new scope would be created for the method block,
                        // despite the fact that they should be the same scope.
                        scope = _currentScope;
                    }
                    else
                    {
                        scope = CreateNestedScope(node, _currentScope, _currentClosure);
                    }
                    DeclareLocals(scope, locals);
                    return scope;
                }

                private static Scope CreateNestedScope(BoundNode node, Scope parentScope, Closure currentClosure)
                {
                    Debug.Assert(parentScope.BoundNode != node);

                    var newScope = new Scope(parentScope, node, currentClosure);
                    parentScope.NestedScopes.Add(newScope);
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
