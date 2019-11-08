// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
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
                public readonly Scope Parent;

                public readonly ArrayBuilder<Scope> NestedScopes = ArrayBuilder<Scope>.GetInstance();

                /// <summary>
                /// A list of all closures (all lambdas and local functions) declared in this scope.
                /// </summary>
                public readonly ArrayBuilder<Closure> Closures = ArrayBuilder<Closure>.GetInstance();

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
                public readonly SetWithInsertionOrder<Symbol> DeclaredVariables = new SetWithInsertionOrder<Symbol>();

                /// <summary>
                /// The bound node representing this scope. This roughly corresponds to the bound
                /// node for the block declaring locals for this scope, although parameters of
                /// methods/closures are introduced into their Body's scope and do not get their
                /// own scope.
                /// </summary>
                public readonly BoundNode BoundNode;

                /// <summary>
                /// The closure that this scope is nested inside. Null if this scope is not nested
                /// inside a closure.
                /// </summary>
                public readonly Closure ContainingClosureOpt;

                /// <summary>
                /// Environments created in this scope to hold <see cref="DeclaredVariables"/>.
                /// </summary>
                public readonly ArrayBuilder<ClosureEnvironment> DeclaredEnvironments
                    = ArrayBuilder<ClosureEnvironment>.GetInstance();

                public Scope(Scope parent, BoundNode boundNode, Closure containingClosure)
                {
                    Debug.Assert(boundNode != null);

                    Parent = parent;
                    BoundNode = boundNode;
                    ContainingClosureOpt = containingClosure;
                }

                /// <summary>
                /// Is it safe to move any of the variables declared in this scope to the parent scope, 
                /// or would doing so change the meaning of the program?
                /// </summary>
                public bool CanMergeWithParent { get; internal set; } = true;

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
                    DeclaredEnvironments.Free();
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
                public readonly MethodSymbol OriginalMethodSymbol;

                /// <summary>
                /// Syntax for the block of the nested function.
                /// </summary>
                public readonly SyntaxReference BlockSyntax;

                public readonly PooledHashSet<Symbol> CapturedVariables = PooledHashSet<Symbol>.GetInstance();

                public readonly ArrayBuilder<ClosureEnvironment> CapturedEnvironments
                    = ArrayBuilder<ClosureEnvironment>.GetInstance();

                public ClosureEnvironment ContainingEnvironmentOpt;

                private bool _capturesThis;

                /// <summary>
                /// True if this closure directly or transitively captures 'this' (captures
                /// a local function which directly or indirectly captures 'this').
                /// Calculated in <see cref="MakeAndAssignEnvironments"/>.
                /// </summary>
                public bool CapturesThis
                {
                    get => _capturesThis;
                    set
                    {
                        Debug.Assert(value);
                        _capturesThis = value;
                    }
                }

                public SynthesizedClosureMethod SynthesizedLoweredMethod;

                public Closure(MethodSymbol symbol, SyntaxReference blockSyntax)
                {
                    Debug.Assert(symbol != null);
                    OriginalMethodSymbol = symbol;
                    BlockSyntax = blockSyntax;
                }

                public void Free()
                {
                    CapturedVariables.Free();
                    CapturedEnvironments.Free();
                }
            }

            public sealed class ClosureEnvironment
            {
                public readonly SetWithInsertionOrder<Symbol> CapturedVariables;

                /// <summary>
                /// True if this environment captures a reference to a class environment
                /// declared in a higher scope. Assigned by
                /// <see cref="ComputeLambdaScopesAndFrameCaptures()"/>
                /// </summary>
                public bool CapturesParent;

                public readonly bool IsStruct;
                internal SynthesizedClosureEnvironment SynthesizedEnvironment;

                public ClosureEnvironment(IEnumerable<Symbol> capturedVariables, bool isStruct)
                {
                    CapturedVariables = new SetWithInsertionOrder<Symbol>();
                    foreach (var item in capturedVariables)
                    {
                        CapturedVariables.Add(item);
                    }
                    IsStruct = isStruct;
                }
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
            /// Visit all the closures and return true when the <paramref name="func"/> returns
            /// true. Otherwise, returns false.
            /// </summary>
            public static bool CheckClosures(Scope scope, Func<Scope, Closure, bool> func)
            {
                foreach (var closure in scope.Closures)
                {
                    if (func(scope, closure))
                    {
                        return true;
                    }
                }

                foreach (var nested in scope.NestedScopes)
                {
                    if (CheckClosures(nested, func))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Visit the tree with the given root and run the <paramref name="action"/>
            /// </summary>
            public static void VisitScopeTree(Scope treeRoot, Action<Scope> action)
            {
                action(treeRoot);

                foreach (var nested in treeRoot.NestedScopes)
                {
                    VisitScopeTree(nested, action);
                }
            }

            /// <summary>
            /// Builds a tree of <see cref="Scope"/> nodes corresponding to a given method.
            /// <see cref="Build(BoundNode, MethodSymbol, HashSet{MethodSymbol}, DiagnosticBag)"/>
            /// visits the bound tree and translates information from the bound tree about
            /// variable scope, declared variables, and variable captures into the resulting
            /// <see cref="Scope"/> tree.
            /// 
            /// At the same time it sets <see cref="Scope.CanMergeWithParent"/>
            /// for each Scope. This is done by looking for <see cref="BoundGotoStatement"/>s 
            /// and <see cref="BoundConditionalGoto"/>s that jump from a point 
            /// after the beginning of a <see cref="Scope"/>, to a <see cref="BoundLabelStatement"/>
            /// before the start of the scope, but after the start of <see cref="Scope.Parent"/>.
            /// 
            /// All loops have been converted to gotos and labels by this stage,
            /// so we do not have to visit them to do so. Similarly all <see cref="BoundLabeledStatement"/>s
            /// have been converted to <see cref="BoundLabelStatement"/>s, so we do not have to 
            /// visit them.
            /// </summary>
            private class ScopeTreeBuilder : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                /// <summary>
                /// Do not set this directly, except when setting the root scope. 
                /// Instead use <see cref="PopScope"/> or <see cref="CreateAndPushScope"/>.
                /// </summary>
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

#if DEBUG
                /// <summary>
                /// Free variables are variables declared in expression statements that can then
                /// be captured in nested lambdas. Normally, captured variables must lowered as
                /// part of closure conversion, but expression tree variables are handled separately
                /// by the expression tree rewriter and are considered free for the purposes of
                /// closure conversion. For instance, an expression with a nested lambda, e.g.
                ///     x => y => x + y
                /// contains an expression variable, x, that should not be treated as a captured
                /// variable to be replaced by closure conversion. Instead, it should be left for
                /// expression tree conversion.
                /// </summary>
                private readonly HashSet<Symbol> _freeVariables = new HashSet<Symbol>();
#endif

                private readonly MethodSymbol _topLevelMethod;

                /// <summary>
                /// If a local function is in the set, at some point in the code it is converted
                /// to a delegate and should then not be optimized to a struct closure.
                /// Also contains all lambdas (as they are converted to delegates implicitly).
                /// </summary>
                private readonly HashSet<MethodSymbol> _methodsConvertedToDelegates;
                private readonly DiagnosticBag _diagnostics;

                /// <summary>
                /// For every label visited so far, this dictionary maps to a list of all scopes either visited so far, or currently being visited,
                /// that are both after the label, and are on the same level of the scope tree as the label.
                /// </summary>
                private readonly PooledDictionary<LabelSymbol, ArrayBuilder<Scope>> _scopesAfterLabel = PooledDictionary<LabelSymbol, ArrayBuilder<Scope>>.GetInstance();

                /// <summary>
                /// Contains a list of the labels visited so far for each scope. 
                /// The outer ArrayBuilder is a stack representing the chain of scopes from the root scope to the current scope,
                /// and for each item on the stack, the ArrayBuilder is the list of the labels visited so far for the scope.
                /// 
                /// Used by <see cref="CreateAndPushScope"/> to determine which labels a new child scope appears after.
                /// </summary>
                private readonly ArrayBuilder<ArrayBuilder<LabelSymbol>> _labelsInScope = ArrayBuilder<ArrayBuilder<LabelSymbol>>.GetInstance();

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
                    _labelsInScope.Push(ArrayBuilder<LabelSymbol>.GetInstance());
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
                    // Treat 'this' as a formal parameter of the top-level method
                    if (_topLevelMethod.TryGetThisParameter(out var thisParam) && (object)thisParam != null)
                    {
                        DeclareLocals(_currentScope, ImmutableArray.Create<Symbol>(thisParam));
                    }

                    Visit(_currentScope.BoundNode);

                    // Clean Up Resources

                    foreach (var scopes in _scopesAfterLabel.Values)
                    {
                        scopes.Free();
                    }
                    _scopesAfterLabel.Free();

                    Debug.Assert(_labelsInScope.Count == 1);
                    var labels = _labelsInScope.Pop();
                    labels.Free();
                    _labelsInScope.Free();
                }

                public override BoundNode VisitMethodGroup(BoundMethodGroup node)
                    => throw ExceptionUtilities.Unreachable;

                public override BoundNode VisitBlock(BoundBlock node)
                {
                    var oldScope = _currentScope;
                    PushOrReuseScope(node, node.Locals);
                    var result = base.VisitBlock(node);
                    PopScope(oldScope);
                    return result;
                }

                public override BoundNode VisitCatchBlock(BoundCatchBlock node)
                {
                    var oldScope = _currentScope;
                    PushOrReuseScope(node, node.Locals);
                    var result = base.VisitCatchBlock(node);
                    PopScope(oldScope);
                    return result;
                }

                public override BoundNode VisitSequence(BoundSequence node)
                {
                    var oldScope = _currentScope;
                    PushOrReuseScope(node, node.Locals);
                    var result = base.VisitSequence(node);
                    PopScope(oldScope);
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

                public override BoundNode VisitLabelStatement(BoundLabelStatement node)
                {
                    _labelsInScope.Peek().Add(node.Label);
                    _scopesAfterLabel.Add(node.Label, ArrayBuilder<Scope>.GetInstance());
                    return base.VisitLabelStatement(node);
                }

                public override BoundNode VisitGotoStatement(BoundGotoStatement node)
                {
                    CheckCanMergeWithParent(node.Label);

                    return base.VisitGotoStatement(node);
                }

                public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
                {
                    CheckCanMergeWithParent(node.Label);

                    return base.VisitConditionalGoto(node);
                }

                /// <summary>
                /// This is where we calculate <see cref="Scope.CanMergeWithParent"/>.
                /// <see cref="Scope.CanMergeWithParent"/> is always true unless we jump from after 
                /// the beginning of a scope, to a point in between the beginning of the parent scope, and the beginning of the scope
                /// </summary>
                /// <param name="jumpTarget"></param>
                private void CheckCanMergeWithParent(LabelSymbol jumpTarget)
                {
                    // since forward jumps can never effect Scope.SemanticallySafeToMergeIntoParent
                    // if we have not yet seen the jumpTarget, this is a forward jump, and can be ignored
                    if (_scopesAfterLabel.TryGetValue(jumpTarget, out var scopesAfterLabel))
                    {
                        foreach (var scope in scopesAfterLabel)
                        {
                            // this jump goes from a point after the beginning of the scope (as we have already visited or started visiting the scope), 
                            // to a point in between the beginning of the parent scope, and the beginning of the scope, so it is not safe to move
                            // variables in the scope to the parent scope.
                            scope.CanMergeWithParent = false;
                        }

                        // Prevent us repeating this process for all scopes if another jumps goes to the same label
                        scopesAfterLabel.Clear();
                    }
                }

                private BoundNode VisitClosure(MethodSymbol closureSymbol, BoundBlock body)
                {
                    Debug.Assert((object)closureSymbol != null);

                    // Closure is declared (lives) in the parent scope, but its
                    // variables are in a nested scope
                    var closure = new Closure(closureSymbol, body.Syntax.GetReference());
                    _currentScope.Closures.Add(closure);

                    var oldClosure = _currentClosure;
                    _currentClosure = closure;

                    var oldScope = _currentScope;
                    CreateAndPushScope(body);

                    // For the purposes of scoping, parameters live in the same scope as the
                    // closure block. Expression tree variables are free variables for the
                    // purposes of closure conversion
                    DeclareLocals(_currentScope, closureSymbol.Parameters, _inExpressionTree);

                    var result = _inExpressionTree
                        ? base.VisitBlock(body)
                        : VisitBlock(body);

                    PopScope(oldScope);
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

                    if (symbol is LocalSymbol { IsConst: true } local)
                    {
                        // consts aren't captured since they're inlined
                        return;
                    }

                    if (symbol is MethodSymbol method &&
                        _currentClosure.OriginalMethodSymbol == method)
                    {
                        // Is this recursion? If so there's no capturing
                        return;
                    }

                    Debug.Assert(symbol.ContainingSymbol != null);
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

                        if (_localToScope.TryGetValue(symbol, out var declScope))
                        {
                            declScope.DeclaredVariables.Add(symbol);
                        }
                        else
                        {
#if DEBUG
                            // Parameters and locals from expression tree lambdas
                            // are free variables
                            Debug.Assert(_freeVariables.Contains(symbol));
#endif
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
                /// Create a new nested scope under the current scope, and replace <see cref="_currentScope"/> with the new scope,
                /// or reuse the current scope if there's no change in the bound node for the nested scope.
                /// Records the given locals as declared in the aforementioned scope.
                /// </summary>
                private void PushOrReuseScope<TSymbol>(BoundNode node, ImmutableArray<TSymbol> locals)
                    where TSymbol : Symbol
                {
                    // We should never create a new scope with the same bound
                    // node. We can get into this situation for methods and
                    // closures where a new scope is created to add parameters
                    // and a new scope would be created for the method block,
                    // despite the fact that they should be the same scope.
                    if (!locals.IsEmpty && _currentScope.BoundNode != node)
                    {
                        CreateAndPushScope(node);
                    }

                    DeclareLocals(_currentScope, locals);
                }

                /// <summary>
                /// Creates a new nested scope which is a child of <see cref="_currentScope"/>,
                /// and replaces <see cref="_currentScope"/> with the new scope
                /// </summary>
                /// <param name="node"></param>
                private void CreateAndPushScope(BoundNode node)
                {
                    var scope = CreateNestedScope(_currentScope, _currentClosure);

                    foreach (var label in _labelsInScope.Peek())
                    {
                        _scopesAfterLabel[label].Add(scope);
                    }

                    _labelsInScope.Push(ArrayBuilder<LabelSymbol>.GetInstance());

                    _currentScope = scope;

                    Scope CreateNestedScope(Scope parentScope, Closure currentClosure)
                    {
                        Debug.Assert(parentScope.BoundNode != node);

                        var newScope = new Scope(parentScope, node, currentClosure);
                        parentScope.NestedScopes.Add(newScope);
                        return newScope;
                    }
                }

                /// <summary>
                /// Requires that scope is either the same as <see cref="_currentScope"/>,
                /// or is the <see cref="Scope.Parent"/> of <see cref="_currentScope"/>.
                /// Returns immediately in the first case,
                /// Replaces <see cref="_currentScope"/> with scope in the second.
                /// </summary>
                /// <param name="scope"></param>
                private void PopScope(Scope scope)
                {
                    if (scope == _currentScope)
                    {
                        return;
                    }

                    Debug.Assert(scope == _currentScope.Parent, $"{nameof(scope)} must be {nameof(_currentScope)} or {nameof(_currentScope)}.{nameof(_currentScope.Parent)}");

                    // Since it is forbidden to jump into a scope, 
                    // we can forget all information we have about labels in the child scope

                    var labels = _labelsInScope.Pop();

                    foreach (var label in labels)
                    {
                        var scopes = _scopesAfterLabel[label];
                        scopes.Free();
                        _scopesAfterLabel.Remove(label);
                    }

                    labels.Free();

                    _currentScope = _currentScope.Parent;
                }

                private void DeclareLocals<TSymbol>(Scope scope, ImmutableArray<TSymbol> locals, bool declareAsFree = false)
                    where TSymbol : Symbol
                {
                    foreach (var local in locals)
                    {
                        Debug.Assert(!_localToScope.ContainsKey(local));
                        if (declareAsFree)
                        {
#if DEBUG
                            Debug.Assert(_freeVariables.Add(local));
#endif
                        }
                        else
                        {
                            _localToScope.Add(local, scope);
                        }
                    }
                }
            }
        }
    }
}
