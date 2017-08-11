// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LambdaRewriter
    {
        /// <summary>
        /// Perform a first analysis pass in preparation for removing all lambdas from a method body.  The entry point is Analyze.
        /// The results of analysis are placed in the fields seenLambda, blockParent, variableBlock, captured, and captures.
        /// </summary>
        internal sealed partial class Analysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly MethodSymbol _topLevelMethod;

            private MethodSymbol _currentParent;
            private BoundNode _currentScope;

            /// <summary>
            /// Set to true while we are analyzing the interior of an expression lambda.
            /// </summary>
            private bool _inExpressionLambda;

            /// <summary>
            /// Set to true of any lambda expressions were seen in the analyzed method body.
            /// </summary>
            public bool SeenLambda { get; private set; }

            /// <summary>
            /// For each scope that defines variables, identifies the nearest enclosing scope that defines variables.
            /// </summary>
            public readonly Dictionary<BoundNode, BoundNode> ScopeParent = new Dictionary<BoundNode, BoundNode>();

            /// <summary>
            /// For each captured variable, identifies the scope in which it will be moved to a frame class. This is
            /// normally the node where the variable is introduced, but method parameters are moved
            /// to a frame class within the body of the method.
            /// </summary>
            public readonly Dictionary<Symbol, BoundNode> VariableScope = new Dictionary<Symbol, BoundNode>();

            /// <summary>
            /// For each value in variableScope, identifies the closest owning method, lambda, or local function.
            /// </summary>
            public readonly Dictionary<BoundNode, MethodSymbol> ScopeOwner = new Dictionary<BoundNode, MethodSymbol>();

            /// <summary>
            /// The syntax nodes associated with each captured variable.
            /// </summary>
            public MultiDictionary<Symbol, SyntaxNode> CapturedVariables = new MultiDictionary<Symbol, SyntaxNode>();

            /// <summary>
            /// If a local function is in the set, at some point in the code it is converted to a delegate and should then not be optimized to a struct closure.
            /// Also contains all lambdas (as they are converted to delegates implicitly).
            /// </summary>
            public readonly HashSet<MethodSymbol> MethodsConvertedToDelegates = new HashSet<MethodSymbol>();

            /// <summary>
            /// True if the method signature can be rewritten to contain ref/out parameters.
            /// </summary>
            public bool CanTakeRefParameters(MethodSymbol closure) => !(closure.IsAsync
                                                                        || closure.IsIterator
                                                                        // We can't rewrite delegate signatures
                                                                        || MethodsConvertedToDelegates.Contains(closure));

            /// <summary>
            /// Any scope that a method that <see cref="CanTakeRefParameters(MethodSymbol)"/> doesn't close over.
            /// If a scope is in this set, don't use a struct closure.
            /// </summary>
            public readonly HashSet<BoundNode> ScopesThatCantBeStructs = new HashSet<BoundNode>();

            /// <summary>
            /// Blocks that are positioned between a block declaring some lifted variables
            /// and a block that contains the lambda that lifts said variables.
            /// If such block itself requires a closure, then it must lift parent frame pointer into the closure
            /// in addition to whatever else needs to be lifted.
            /// 
            /// NOTE: This information is computed in addition to the regular analysis of the tree and only needed for rewriting.
            /// If someone only needs diagnostics or information about captures, this information is not necessary.
            /// <see cref="ComputeLambdaScopesAndFrameCaptures"/> needs to be called to compute this.
            /// </summary>
            public HashSet<BoundNode> NeedsParentFrame;

            /// <summary>
            /// Optimized locations of lambdas. 
            /// 
            /// Lambda does not need to be placed in a frame that corresponds to its lexical scope if lambda does not reference any local state in that scope.
            /// It is advantageous to place lambdas higher in the scope tree, ideally in the innermost scope of all scopes that contain variables captured by a given lambda.
            /// Doing so reduces indirections needed when captured locals are accessed. For example locals from the innermost scope can be accessed with no indirection at all.
            /// 
            /// NOTE: This information is computed in addition to the regular analysis of the tree and only needed for rewriting.
            /// If someone only needs diagnostics or information about captures, this information is not necessary.
            /// <see cref="ComputeLambdaScopesAndFrameCaptures"/> needs to be called to compute this.
            /// </summary>
            public Dictionary<MethodSymbol, BoundNode> LambdaScopes;

            /// <summary>
            /// The root of the scope tree for this method.
            /// </summary>
            public Scope ScopeTree { get; private set; }

            private Analysis(MethodSymbol method)
            {
                Debug.Assert((object)method != null);

                _currentParent = _topLevelMethod = method;
            }

            public static Analysis Analyze(BoundNode node, MethodSymbol method)
            {
                var analysis = new Analysis(method);
                analysis.Analyze(node);
                return analysis;
            }

            private void Analyze(BoundNode node)
            {
                ScopeTree = ScopeTreeBuilder.Build(node, this);
                _currentScope = FindNodeToAnalyze(node);

                Debug.Assert(!_inExpressionLambda);
                Debug.Assert((object)_topLevelMethod != null);
                Debug.Assert((object)_currentParent != null);

                foreach (ParameterSymbol parameter in _topLevelMethod.Parameters)
                {
                    // parameters are counted as if they are inside the block
                    VariableScope[parameter] = _currentScope;
                }

                Visit(node);

                // scopeOwner may already contain the same key/value if _currentScope is a BoundBlock.
                MethodSymbol shouldBeCurrentParent;
                if (ScopeOwner.TryGetValue(_currentScope, out shouldBeCurrentParent))
                {
                    // Check to make sure the above comment is right.
                    Debug.Assert(_currentParent == shouldBeCurrentParent);
                }
                else
                {
                    ScopeOwner.Add(_currentScope, _currentParent);
                }
            }

            private static BoundNode FindNodeToAnalyze(BoundNode node)
            {
                while (true)
                {
                    switch (node.Kind)
                    {
                        case BoundKind.SequencePoint:
                            node = ((BoundSequencePoint)node).StatementOpt;
                            break;

                        case BoundKind.SequencePointWithSpan:
                            node = ((BoundSequencePointWithSpan)node).StatementOpt;
                            break;

                        case BoundKind.Block:
                        case BoundKind.StatementList:
                        case BoundKind.FieldInitializer:
                            return node;

                        case BoundKind.GlobalStatementInitializer:
                            return ((BoundGlobalStatementInitializer)node).Statement;

                        default:
                            // Other node types should not appear at the top level
                            throw ExceptionUtilities.UnexpectedValue(node.Kind);
                    }
                }
            }

            /// <summary>
            /// Create the optimized plan for the location of lambda methods and whether scopes need access to parent scopes
            ///  </summary>
            internal void ComputeLambdaScopesAndFrameCaptures()
            {
                RemoveUnneededReferences();

                LambdaScopes = new Dictionary<MethodSymbol, BoundNode>(ReferenceEqualityComparer.Instance);
                NeedsParentFrame = new HashSet<BoundNode>();

                VisitClosures(ScopeTree, (scope, closure) =>
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
                            var (found, _) = GetVisibleClosure(closureScope, localFunc);
                            capturedVars.AddAll(found.CapturedVariables);
                        }
                    }

                    for (var curScope = closureScope;
                         curScope != null && capturedVars.Count > 0;
                         curScope = curScope.Parent)
                    {
                        if (!(capturedVars.Overlaps(curScope.DeclaredVariables) ||
                              capturedVars.Overlaps(curScope.Closures.Select(c => c.OriginalMethodSymbol))))
                        {
                            continue;
                        }

                        outermost = curScope;
                        if (innermost == null)
                        {
                            innermost = curScope;
                        }

                        capturedVars.RemoveAll(curScope.DeclaredVariables);
                        capturedVars.RemoveAll(curScope.Closures.Select(c => c.OriginalMethodSymbol));
                    }

                    // If any captured variables are left, they're captured above method scope
                    if (capturedVars.Count > 0)
                    {
                        outermost = null;
                    }

                    capturedVars.Free();

                    return (innermost, outermost);
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
                        LambdaScopes.Add(closure.OriginalMethodSymbol, innermost.BoundNode);

                        // Disable struct closures on methods converted to delegates, as well as on async and iterator methods.
                        var markAsNoStruct = !CanTakeRefParameters(closure.OriginalMethodSymbol);
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

            /// <summary>
            /// Walk up the scope tree looking for a closure.
            /// </summary>
            /// <returns>
            /// A tuple of the found <see cref="Closure"/> and the <see cref="Scope"/> it was found in.
            /// </returns>
            public static (Closure, Scope) GetVisibleClosure(Scope startingScope, MethodSymbol closureSymbol)
            {
                var currentScope = startingScope;
                while (currentScope != null)
                {
                    foreach (var closure in currentScope.Closures)
                    {
                        if (closure.OriginalMethodSymbol == closureSymbol)
                        {
                            return (closure, currentScope);
                        }
                    }
                    currentScope = currentScope.Parent;
                }
                throw ExceptionUtilities.Unreachable;
            }

            /// <summary>
            /// Finds a <see cref="Closure"/> with a matching original symbol somewhere in the given scope or nested scopes.
            /// </summary>
            public static Closure GetClosureInTree(Scope treeRoot, MethodSymbol closureSymbol)
            {
                return Helper(treeRoot) ?? throw ExceptionUtilities.Unreachable;

                Closure Helper(Scope scope)
                {
                    foreach (var closure in scope.Closures)
                    {
                        if (closure.OriginalMethodSymbol == closureSymbol)
                        {
                            return closure;
                        }
                    }

                    foreach (var nestedScope in scope.NestedScopes)
                    {
                        var found = Helper(nestedScope);
                        if (found != null)
                        {
                            return found;
                        }
                    }

                    return null;
                }
            }

            /// <summary>
            /// Compute the nesting depth of a given block.
            /// Top-most block (where method locals and parameters are defined) are at the depth 0.
            /// </summary>
            private int BlockDepth(BoundNode node)
            {
                // TODO: this could be precomputed and stored by analysis phase
                int result = -1;
                while (node != null)
                {
                    result = result + 1;
                    if (!ScopeParent.TryGetValue(node, out node))
                    {
                        break;
                    }
                }

                return result;
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                var locals = node.Locals;

                if (locals.IsEmpty)
                {
                    return base.VisitCatchBlock(node);
                }

                var previousBlock = PushBlock(node, locals);
                var result = base.VisitCatchBlock(node);
                PopBlock(previousBlock);
                return node;
            }

            private BoundNode PushBlock(BoundNode node, ImmutableArray<LocalSymbol> locals)
            {
                // blocks are not allowed in expression lambda
                Debug.Assert(!_inExpressionLambda);

                var previousBlock = _currentScope;
                _currentScope = node;
                if (_currentScope != previousBlock) // not top-level node of the method
                {
                    // (Except for the top-level block) record the parent-child block structure
                    ScopeParent[_currentScope] = previousBlock;
                }

                foreach (var local in locals)
                {
                    VariableScope[local] = _currentScope;
                }

                ScopeOwner.Add(_currentScope, _currentParent);

                return previousBlock;
            }

            private void PopBlock(BoundNode previousBlock)
            {
                _currentScope = previousBlock;
            }

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                var locals = node.InnerLocals;
                if (locals.IsEmpty)
                {
                    // no variables declared inside the switch statement.
                    return base.VisitSwitchStatement(node);
                }

                var previousBlock = PushBlock(node, locals);
                var result = base.VisitSwitchStatement(node);
                PopBlock(previousBlock);
                return result;
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                if (node.Locals.IsEmpty)
                {
                    // ignore blocks that declare no variables.
                    return base.VisitBlock(node);
                }

                VisitBlockInternal(node);
                return node;
            }

            private void VisitBlockInternal(BoundBlock node)
            {
                var previousBlock = PushBlock(node, node.Locals);
                base.VisitBlock(node);
                PopBlock(previousBlock);
            }

            public override BoundNode VisitSequence(BoundSequence node)
            {
                if (node.Locals.IsDefaultOrEmpty)
                {
                    // ignore blocks that declare no variables.
                    return base.VisitSequence(node);
                }

                var previousBlock = PushBlock(node, node.Locals);
                var result = base.VisitSequence(node);
                PopBlock(previousBlock);
                return result;
            }

            public override BoundNode VisitCall(BoundCall node)
            {
                if (node.Method.MethodKind == MethodKind.LocalFunction)
                {
                    // Use OriginalDefinition to strip generic type parameters
                    ReferenceVariable(node.Syntax, node.Method.OriginalDefinition);
                }
                return base.VisitCall(node);
            }

            public override BoundNode VisitLambda(BoundLambda node)
            {
                MethodsConvertedToDelegates.Add(node.Symbol.OriginalDefinition);
                return VisitLambdaOrFunction(node);
            }

            public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
            {
                if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
                {
                    // Use OriginalDefinition to strip generic type parameters
                    ReferenceVariable(node.Syntax, node.MethodOpt.OriginalDefinition);
                    MethodsConvertedToDelegates.Add(node.MethodOpt.OriginalDefinition);
                }
                return base.VisitDelegateCreationExpression(node);
            }

            public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
            {
                VariableScope[node.Symbol] = _currentScope;
                return VisitLambdaOrFunction(node);
            }

            private BoundNode VisitLambdaOrFunction(IBoundLambdaOrFunction node)
            {
                Debug.Assert((object)node.Symbol != null);
                SeenLambda = true;
                var oldParent = _currentParent;
                var oldBlock = _currentScope;
                _currentParent = node.Symbol;
                _currentScope = node.Body;
                ScopeParent[_currentScope] = oldBlock;
                ScopeOwner.Add(_currentScope, _currentParent);
                var wasInExpressionLambda = _inExpressionLambda;
                _inExpressionLambda = _inExpressionLambda || ((node as BoundLambda)?.Type.IsExpressionTree() ?? false);

                if (!_inExpressionLambda)
                {
                    // for the purpose of constructing frames parameters are scoped as if they are inside the lambda block
                    foreach (var parameter in node.Symbol.Parameters)
                    {
                        VariableScope[parameter] = _currentScope;
                    }

                    foreach (var local in node.Body.Locals)
                    {
                        VariableScope[local] = _currentScope;
                    }
                }

                var result = base.VisitBlock(node.Body);
                _inExpressionLambda = wasInExpressionLambda;
                _currentParent = oldParent;
                _currentScope = oldBlock;
                return result;
            }

            private void ReferenceVariable(SyntaxNode syntax, Symbol symbol)
            {
                if (symbol is LocalSymbol localSymbol && localSymbol.IsConst)
                {
                    // "constant variables" need not be captured
                    return;
                }

                // "symbol == lambda" could happen if we're recursive
                if (_currentParent is MethodSymbol lambda && symbol != lambda && symbol.ContainingSymbol != lambda)
                {
                    CapturedVariables.Add(symbol, syntax);
                }
            }

            private static bool IsClosure(MethodSymbol symbol)
            {
                switch (symbol.MethodKind)
                {
                    case MethodKind.LambdaMethod:
                    case MethodKind.LocalFunction:
                        return true;

                    default:
                        return false;
                }
            }

            public override BoundNode VisitMethodGroup(BoundMethodGroup node)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override BoundNode VisitThisReference(BoundThisReference node)
            {
                var thisParam = _topLevelMethod.ThisParameter;
                if (thisParam != null)
                {
                    ReferenceVariable(node.Syntax, thisParam);
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

            public override BoundNode VisitBaseReference(BoundBaseReference node)
            {
                ReferenceVariable(node.Syntax, _topLevelMethod.ThisParameter);
                return base.VisitBaseReference(node);
            }

            public override BoundNode VisitParameter(BoundParameter node)
            {
                ReferenceVariable(node.Syntax, node.ParameterSymbol);
                return base.VisitParameter(node);
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                ReferenceVariable(node.Syntax, node.LocalSymbol);
                return base.VisitLocal(node);
            }
        }
    }
}
