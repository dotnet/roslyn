// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
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
        internal sealed partial class Analysis
        {
            /// <summary>
            /// If a local function is in the set, at some point in the code it is converted to a delegate and should then not be optimized to a struct closure.
            /// Also contains all lambdas (as they are converted to delegates implicitly).
            /// </summary>
            public readonly PooledHashSet<MethodSymbol> MethodsConvertedToDelegates;

            /// <summary>
            /// True if the method signature can be rewritten to contain ref/out parameters.
            /// </summary>
            public bool CanTakeRefParameters(MethodSymbol closure) => !(closure.IsAsync
                                                                        || closure.IsIterator
                                                                        // We can't rewrite delegate signatures
                                                                        || MethodsConvertedToDelegates.Contains(closure));

            /// <summary>
            /// Blocks that are positioned between a block declaring some lifted variables
            /// and a block that contains the lambda that lifts said variables.
            /// If such block itself requires a closure, then it must lift parent frame pointer into the closure
            /// in addition to whatever else needs to be lifted.
            /// <see cref="ComputeLambdaScopesAndFrameCaptures"/> needs to be called to compute this.
            /// </summary>
            public readonly PooledHashSet<BoundNode> NeedsParentFrame = PooledHashSet<BoundNode>.GetInstance();

            /// <summary>
            /// The root of the scope tree for this method.
            /// </summary>
            public readonly Scope ScopeTree;

            private readonly MethodSymbol _topLevelMethod;
            private readonly int _topLevelMethodOrdinal;
            private readonly MethodSymbol _substitutedSourceMethod; 
            private readonly VariableSlotAllocator _slotAllocatorOpt;
            private readonly TypeCompilationState _compilationState;

            private Analysis(
                Scope scopeTree,
                PooledHashSet<MethodSymbol> methodsConvertedToDelegates,
                MethodSymbol topLevelMethod,
                int topLevelMethodOrdinal,
                MethodSymbol substitutedSourceMethod,
                VariableSlotAllocator slotAllocatorOpt,
                TypeCompilationState compilationState)
            {
                ScopeTree = scopeTree;
                MethodsConvertedToDelegates = methodsConvertedToDelegates;
                _topLevelMethod = topLevelMethod;
                _topLevelMethodOrdinal = topLevelMethodOrdinal;
                _substitutedSourceMethod = substitutedSourceMethod;
                _slotAllocatorOpt = slotAllocatorOpt;
                _compilationState = compilationState;
            }

            public static Analysis Analyze(
                BoundNode node,
                MethodSymbol method,
                int topLevelMethodOrdinal,
                MethodSymbol substitutedSourceMethod,
                VariableSlotAllocator slotAllocatorOpt,
                TypeCompilationState compilationState,
                ArrayBuilder<ClosureDebugInfo> closureDebugInfo,
                DiagnosticBag diagnostics)
            {
                var methodsConvertedToDelegates = PooledHashSet<MethodSymbol>.GetInstance();
                var scopeTree = ScopeTreeBuilder.Build(
                    node,
                    method,
                    methodsConvertedToDelegates,
                    diagnostics);
                Debug.Assert(scopeTree != null);

                var analysis = new Analysis(
                    scopeTree,
                    methodsConvertedToDelegates,
                    method,
                    topLevelMethodOrdinal,
                    substitutedSourceMethod,
                    slotAllocatorOpt,
                    compilationState);

                analysis.RemoveUnneededReferences(method.ThisParameter);
                analysis.MakeAndAssignEnvironments(closureDebugInfo);
                analysis.ComputeLambdaScopesAndFrameCaptures(method.ThisParameter);
                return analysis;
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
            /// Must be called only after <see cref="Closure.CapturedEnvironments"/>
            /// has been calculated.
            ///
            /// Finds the most optimal capture environment to place a closure in. 
            /// This roughly corresponds to the 'highest' Scope in the tree where all
            /// the captured variables for this closure are in scope. This minimizes
            /// the number of indirections we may have to traverse to access captured
            /// variables.
            /// </summary>
            private void ComputeLambdaScopesAndFrameCaptures(ParameterSymbol thisParam)
            {
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
                    // If the closure only captures this, put the method directly in the
                    // top-level method's containing type
                    if (closure.CapturedVariables.Count == 1 &&
                        closure.CapturedVariables.Single() is ParameterSymbol param &&
                        param.IsThis)
                    {
                        return (null, null);
                    }

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
                        if (!(capturedVars.RemoveAll(curScope.DeclaredVariables) ||
                              capturedVars.RemoveAll(curScope.Closures.Select(c => c.OriginalMethodSymbol))))
                        {
                            continue;
                        }

                        outermost = curScope;
                        if (innermost == null)
                        {
                            innermost = curScope;
                        }
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
                    //   its innermost scope is the root Scope (method locals and parameters) 
                    //   and outermost Scope is null
                    //   Such lambda will be placed in a closure frame that corresponds to the method's outer block
                    //   and this frame will also lift original `this` as a field when created by its parent.
                    //   Note that it is completely irrelevant how deeply the lexical scope of the lambda was originally nested.
                    if (innermost != null)
                    {
                        closure.ContainingEnvironmentOpt = innermost.DeclaredEnvironments[0];

                        while (innermost != outermost)
                        {
                            NeedsParentFrame.Add(innermost.BoundNode);
                            innermost = innermost.Parent;
                        }
                    }
                }
            }

            private void MakeAndAssignEnvironments(ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
            {
                VisitScopeTree(ScopeTree, scope =>
                {
                    if (scope.DeclaredVariables.Count > 0)
                    {
                        // First walk the nested scopes to find all closures which
                        // capture variables from this scope. They all need to capture
                        // this environment. This includes closures which captured local
                        // functions that capture those variables, so multiple passes may
                        // be needed. This will also decide if the environment is a struct
                        // or a class.
                        bool isStruct = true;
                        var closures = new SetWithInsertionOrder<Closure>();
                        bool addedItem;
                        do
                        {
                            addedItem = false;
                            VisitClosures(scope, (closureScope, closure) =>
                            {
                                if (!closures.Contains(closure) &&
                                    (closure.CapturedVariables.Overlaps(scope.DeclaredVariables) ||
                                     closure.CapturedVariables.Overlaps(closures.Select(c => c.OriginalMethodSymbol))))
                                {
                                    closures.Add(closure);
                                    addedItem = true;
                                    isStruct &= CanTakeRefParameters(closure.OriginalMethodSymbol);
                                }
                            });
                        } while (addedItem == true);

                        // Next create the environment and add it to the declaration scope
                        // Currently all variables declared in the same scope are added
                        // to the same closure environment
                        var env = MakeEnvironment(scope, scope.DeclaredVariables, isStruct);
                        scope.DeclaredEnvironments.Add(env);

                        foreach (var closure in closures)
                        {
                            closure.CapturedEnvironments.Add(env);
                        }
                    }
                });

                ClosureEnvironment MakeEnvironment(Scope scope, IEnumerable<Symbol> capturedVariables, bool isStruct)
                {
                    var scopeBoundNode = scope.BoundNode;

                    var syntax = scopeBoundNode.Syntax;
                    Debug.Assert(syntax != null);

                    DebugId methodId = GetTopLevelMethodId();
                    DebugId closureId = GetClosureId(syntax, closureDebugInfo);

                    var containingMethod = scope.ContainingClosureOpt?.OriginalMethodSymbol ?? _topLevelMethod;
                    if ((object)_substitutedSourceMethod != null && containingMethod == _topLevelMethod)
                    {
                        containingMethod = _substitutedSourceMethod;
                    }

                    return new ClosureEnvironment(
                        capturedVariables,
                        _topLevelMethod,
                        containingMethod,
                        isStruct,
                        syntax,
                        methodId,
                        closureId);
                }
            }

            internal DebugId GetTopLevelMethodId()
            {
                return _slotAllocatorOpt?.MethodId ?? new DebugId(_topLevelMethodOrdinal, _compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            }

            private DebugId GetClosureId(SyntaxNode syntax, ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
            {
                Debug.Assert(syntax != null);

                DebugId closureId;
                DebugId previousClosureId;
                if (_slotAllocatorOpt != null && _slotAllocatorOpt.TryGetPreviousClosure(syntax, out previousClosureId))
                {
                    closureId = previousClosureId;
                }
                else
                {
                    closureId = new DebugId(closureDebugInfo.Count, _compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
                }

                int syntaxOffset = _topLevelMethod.CalculateLocalSyntaxOffset(syntax.SpanStart, syntax.SyntaxTree);
                closureDebugInfo.Add(new ClosureDebugInfo(syntaxOffset, closureId));

                return closureId;
            }

            /// <summary>
            /// Walk up the scope tree looking for a variable declaration.
            /// </summary>
            public static Scope GetVariableDeclarationScope(Scope startingScope, Symbol variable)
            {
                if (variable is ParameterSymbol p && p.IsThis)
                {
                    return null;
                }

                var currentScope = startingScope;
                while (currentScope != null)
                {
                    switch (variable.Kind)
                    {
                        case SymbolKind.Parameter:
                        case SymbolKind.Local:
                            if (currentScope.DeclaredVariables.Contains(variable))
                            {
                                return currentScope;
                            }
                            break;

                        case SymbolKind.Method:
                            foreach (var closure in currentScope.Closures)
                            {
                                if (closure.OriginalMethodSymbol == variable)
                                {
                                    return currentScope;
                                }
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(variable.Kind);
                    }
                    currentScope = currentScope.Parent;
                }
                return null;
            }

            /// <summary>
            /// Find the parent <see cref="Scope"/> of the <see cref="Scope"/> corresponding to
            /// the given <see cref="BoundNode"/>.
            /// </summary>
            public static Scope GetScopeParent(Scope treeRoot, BoundNode scopeNode)
            {
                var correspondingScope = GetScopeWithMatchingBoundNode(treeRoot, scopeNode);
                return correspondingScope.Parent;
            }

            /// <summary>
            /// Finds a <see cref="Scope" /> with a matching <see cref="BoundNode"/>
            /// as the one given.
            /// </summary>
            public static Scope GetScopeWithMatchingBoundNode(Scope treeRoot, BoundNode node)
            {
                return Helper(treeRoot) ?? throw ExceptionUtilities.Unreachable;

                Scope Helper(Scope currentScope)
                {
                    if (currentScope.BoundNode == node)
                    {
                        return currentScope;
                    }

                    foreach (var nestedScope in currentScope.NestedScopes)
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

            public void Free()
            {
                MethodsConvertedToDelegates.Free();
                NeedsParentFrame.Free();
                ScopeTree.Free();
            }
        }
    }
}
