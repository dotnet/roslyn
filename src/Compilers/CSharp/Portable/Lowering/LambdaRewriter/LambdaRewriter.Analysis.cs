// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            /// The root of the scope tree for this method.
            /// </summary>
            public readonly Scope ScopeTree;

            private readonly MethodSymbol _topLevelMethod;
            private readonly int _topLevelMethodOrdinal;
            private readonly VariableSlotAllocator _slotAllocatorOpt;
            private readonly TypeCompilationState _compilationState;

            private Analysis(
                Scope scopeTree,
                PooledHashSet<MethodSymbol> methodsConvertedToDelegates,
                MethodSymbol topLevelMethod,
                int topLevelMethodOrdinal,
                VariableSlotAllocator slotAllocatorOpt,
                TypeCompilationState compilationState)
            {
                ScopeTree = scopeTree;
                MethodsConvertedToDelegates = methodsConvertedToDelegates;
                _topLevelMethod = topLevelMethod;
                _topLevelMethodOrdinal = topLevelMethodOrdinal;
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
                    slotAllocatorOpt,
                    compilationState);

                analysis.MakeAndAssignEnvironments();
                analysis.ComputeLambdaScopesAndFrameCaptures();
                if (compilationState.Compilation.Options.OptimizationLevel == OptimizationLevel.Release)
                {
                    // This can affect when a variable is in scope whilst debugging, so only do this in release mode.
                    analysis.MergeEnvironments();
                }
                analysis.InlineThisOnlyEnvironments();
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
                        case BoundKind.FieldEqualsValue:
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
            private void ComputeLambdaScopesAndFrameCaptures()
            {
                VisitClosures(ScopeTree, (scope, closure) =>
                {
                    if (closure.CapturedEnvironments.Count > 0)
                    {
                        var capturedEnvs = PooledHashSet<ClosureEnvironment>.GetInstance();
                        capturedEnvs.AddAll(closure.CapturedEnvironments);

                        // Find the nearest captured class environment, if one exists
                        var curScope = scope;
                        while (curScope != null)
                        {
                            if (capturedEnvs.RemoveAll(curScope.DeclaredEnvironments))
                            {
                                // Right now we only create one environment per scope
                                Debug.Assert(curScope.DeclaredEnvironments.Count == 1);
                                var env = curScope.DeclaredEnvironments[0];
                                if (!env.IsStruct)
                                {
                                    closure.ContainingEnvironmentOpt = env;
                                    break;
                                }
                            }
                            curScope = curScope.Parent;
                        }

                        // Now we need to walk up the scopes to find environment captures
                        var oldEnv = curScope?.DeclaredEnvironments[0];
                        curScope = curScope?.Parent;
                        while (curScope != null)
                        {
                            if (capturedEnvs.Count == 0)
                            {
                                break;
                            }

                            var envs = curScope.DeclaredEnvironments.Where(e => !e.IsStruct);
                            if (!envs.IsEmpty())
                            {
                                // Right now we only create one environment per scope
                                Debug.Assert(envs.IsSingle());
                                var env = envs.First();
                                Debug.Assert(!oldEnv.IsStruct);
                                oldEnv.CapturesParent = true;
                                oldEnv = env;
                            }
                            capturedEnvs.RemoveAll(curScope.DeclaredEnvironments);
                            curScope = curScope.Parent;
                        }

                        if (capturedEnvs.Count > 0)
                        {
                            throw ExceptionUtilities.Unreachable;
                        }

                        capturedEnvs.Free();

                    }
                });
            }

            /// <summary>
            /// We may have ended up with a closure environment containing only
            /// 'this'. This is basically equivalent to the containing type itself,
            /// so we can inline the 'this' parameter into environments that
            /// reference this one or lower closures directly onto the containing
            /// type.
            /// </summary>
            private void InlineThisOnlyEnvironments()
            {
                // First make sure 'this' even exists
                if (!_topLevelMethod.TryGetThisParameter(out var thisParam) ||
                    thisParam == null)
                {
                    return;
                }

                var topLevelEnvs = ScopeTree.DeclaredEnvironments;

                // If it does exist, 'this' is always in the top-level environment
                if (topLevelEnvs.Count == 0)
                {
                    return;
                }

                Debug.Assert(topLevelEnvs.Count == 1);
                var env = topLevelEnvs[0];

                // The environment must contain only 'this' to be inlined
                if (env.CapturedVariables.Count > 1 ||
                    !env.CapturedVariables.Contains(thisParam))
                {
                    return;
                }

                if (env.IsStruct)
                {
                    // If everything that captures the 'this' environment
                    // lives in the containing type, we can remove the env
                    bool cantRemove = CheckClosures(ScopeTree, (scope, closure) =>
                    {
                        return closure.CapturedEnvironments.Contains(env) &&
                            closure.ContainingEnvironmentOpt != null;
                    });

                    if (!cantRemove)
                    {
                        RemoveEnv();
                    }
                }
                else
                {
                    // Class-based 'this' closures can move member functions to
                    // the top-level type and environments which capture the 'this'
                    // environment can capture 'this' directly.
                    // Note: the top-level type is treated as the initial containing
                    // environment, so by removing the 'this' environment, all
                    // nested environments which captured a pointer to the 'this'
                    // environment will now capture 'this'
                    RemoveEnv();
                    VisitClosures(ScopeTree, (scope, closure) =>
                    {
                        if (closure.ContainingEnvironmentOpt == env)
                        {
                            closure.ContainingEnvironmentOpt = null;
                        }
                    });
                }

                void RemoveEnv()
                {
                    topLevelEnvs.RemoveAt(topLevelEnvs.IndexOf(env));
                    VisitClosures(ScopeTree, (scope, closure) =>
                    {
                        var index = closure.CapturedEnvironments.IndexOf(env);
                        if (index >= 0)
                        {
                            closure.CapturedEnvironments.RemoveAt(index);
                        }
                    });
                }
            }

            private void MakeAndAssignEnvironments()
            {
                VisitScopeTree(ScopeTree, scope =>
                {
                    // Currently all variables declared in the same scope are added
                    // to the same closure environment
                    var variablesInEnvironment = scope.DeclaredVariables;

                    // Don't create empty environments
                    if (variablesInEnvironment.Count == 0)
                    {
                        return;
                    }

                    // First walk the nested scopes to find all closures which
                    // capture variables from this scope. They all need to capture
                    // this environment. This includes closures which captured local
                    // functions that capture those variables, so multiple passes may
                    // be needed. This will also decide if the environment is a struct
                    // or a class.
                    bool isStruct = true;
                    var closures = new SetWithInsertionOrder<Closure>();
                    bool addedItem;

                    // This loop is O(n), where n is the length of the chain
                    //   L_1 <- L_2 <- L_3 ...
                    // where L_1 represents a local function that directly captures the current
                    // environment, L_2 represents a local function that directly captures L_1,
                    // L_3 represents a local function that captures L_2, and so on.
                    //
                    // Each iteration of the loop runs a visitor that is proportional to the
                    // number of closures in nested scopes, so we hope that the total number
                    // of nested functions and function chains is small in any real-world code.
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
                    var env = new ClosureEnvironment(variablesInEnvironment, isStruct);
                    scope.DeclaredEnvironments.Add(env);

                    _topLevelMethod.TryGetThisParameter(out var thisParam);
                    foreach (var closure in closures)
                    {
                        closure.CapturedEnvironments.Add(env);
                        if (thisParam != null && env.CapturedVariables.Contains(thisParam))
                        {
                            closure.CapturesThis = true;
                        }
                    }
                });
            }

            /// <summary>
            /// Calculates all closures which directly or indirectly capture a scopes variables.
            /// </summary>
            /// <returns></returns>
            private PooledDictionary<Scope, PooledHashSet<Closure>> CalculateClosuresCapturingScopeVariables()
            {
                var closuresCapturingScopeVariables = PooledDictionary<Scope, PooledHashSet<Closure>>.GetInstance();

                // calculate closures which directly capture a scope

                var environmentsToScopes = PooledDictionary<ClosureEnvironment, Scope>.GetInstance();

                VisitScopeTree(ScopeTree, scope =>
                {
                    if (scope.DeclaredEnvironments.Count > 0)
                    {
                        // Right now we only create one environment per scope
                        Debug.Assert(scope.DeclaredEnvironments.Count == 1);

                        closuresCapturingScopeVariables[scope] = PooledHashSet<Closure>.GetInstance();
                        environmentsToScopes[scope.DeclaredEnvironments[0]] = scope;
                    }

                    foreach (var closure in scope.Closures)
                    {
                        foreach (var env in closure.CapturedEnvironments)
                        {
                            // A closure should only ever capture a scope which is an ancestor of its own,
                            // which we should have already visited
                            Debug.Assert(environmentsToScopes.ContainsKey(env));

                            closuresCapturingScopeVariables[environmentsToScopes[env]].Add(closure);
                        }
                    }
                });

                environmentsToScopes.Free();

                // if a closure captures a scope, which captures its parent, then the closure also captures the parents scope.
                // we update closuresCapturingScopeVariables to reflect this.
                foreach (var (scope, capturingClosures) in closuresCapturingScopeVariables)
                {
                    if (scope.DeclaredEnvironments.Count == 0)
                        continue;

                    var currentScope = scope;
                    while (currentScope.DeclaredEnvironments.Count == 0 || currentScope.DeclaredEnvironments[0].CapturesParent)
                    {
                        currentScope = currentScope.Parent;

                        if (currentScope == null)
                        {
                            throw ExceptionUtilities.Unreachable;
                        }

                        if (currentScope.DeclaredEnvironments.Count == 0 ||
                            currentScope.DeclaredEnvironments[0].IsStruct)
                        {
                            continue;
                        }

                        closuresCapturingScopeVariables[currentScope].AddAll(capturingClosures);
                    }
                }

                return closuresCapturingScopeVariables;
            }

            /// <summary>
            /// Must be called only after <see cref="MakeAndAssignEnvironments"/> and <see cref="ComputeLambdaScopesAndFrameCaptures"/>.
            /// 
            /// In order to reduce allocations, merge environments into a parent environment when it is safe to do so.
            /// This must be done whilst preserving semantics.
            /// 
            /// We also have to make sure not to extend the life of any variable.
            /// This means that we can only merge an environment into its parent if exactly the same closures directly or indirectly reference both environments.
            /// </summary>
            private void MergeEnvironments()
            {
                var closuresCapturingScopeVariables = CalculateClosuresCapturingScopeVariables();

                // now we merge environments into their parent environments if it is safe to do so
                foreach (var (scope, closuresCapturingScope) in closuresCapturingScopeVariables)
                {
                    if (closuresCapturingScope.Count == 0)
                        continue;

                    var scopeEnv = scope.DeclaredEnvironments[0];

                    // structs don't allocate, so no point merging them
                    if (scopeEnv.IsStruct)
                        continue;

                    var bestScope = scope;
                    var currentScope = scope;

                    // Walk up the scope tree, checking at each point if it is:
                    // a) semantically safe to merge the scope's environment into it's parent scope's environment
                    // b) doing so would not change GC behaviour
                    // Once either of these conditions fails, we merge into the closure environment furthest up the scope tree we've found so far
                    while (currentScope.Parent != null)
                    {
                        if (!currentScope.CanMergeWithParent)
                            break;

                        var parentScope = currentScope.Parent;

                        // Right now we only create one environment per scope
                        Debug.Assert(parentScope.DeclaredEnvironments.Count < 2);

                        // we skip any scopes which do not have any captured variables, and try to merge into the parent scope instead.
                        // We also skip any struct environments as they don't allocate, so no point merging them
                        if (parentScope.DeclaredEnvironments.Count == 0 || parentScope.DeclaredEnvironments[0].IsStruct)
                        {
                            currentScope = parentScope;
                            continue;
                        }

                        var closuresCapturingParentScope = closuresCapturingScopeVariables[parentScope];

                        // if more closures reference one scope's environments than the other scope's environments,
                        // then merging the two environments would increase the number of objects referencing some variables, 
                        // which may prevent the variables being garbage collected.
                        if (!closuresCapturingParentScope.SetEquals(closuresCapturingScope))
                            break;

                        bestScope = parentScope;

                        currentScope = parentScope;
                    }


                    if (bestScope == scope) // no better scope was found, so continue
                        continue;

                    // do the actual work of merging the closure environments

                    var targetEnv = bestScope.DeclaredEnvironments[0];

                    foreach (var variable in scopeEnv.CapturedVariables)
                    {
                        targetEnv.CapturedVariables.Add(variable);
                    }

                    scope.DeclaredEnvironments.Clear();

                    foreach (var closure in closuresCapturingScope)
                    {
                        closure.CapturedEnvironments.Remove(scopeEnv);

                        if (!closure.CapturedEnvironments.Contains(targetEnv))
                        {
                            closure.CapturedEnvironments.Add(targetEnv);
                        }

                        if (closure.ContainingEnvironmentOpt == scopeEnv)
                        {
                            closure.ContainingEnvironmentOpt = targetEnv;
                        }
                    }
                }

                // cleanup
                foreach (var set in closuresCapturingScopeVariables.Values)
                {
                    set.Free();
                }

                closuresCapturingScopeVariables.Free();
            }

            internal DebugId GetTopLevelMethodId()
            {
                return _slotAllocatorOpt?.MethodId ?? new DebugId(_topLevelMethodOrdinal, _compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            }

            internal DebugId GetClosureId(SyntaxNode syntax, ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
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

                int syntaxOffset = _topLevelMethod.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(syntax), syntax.SyntaxTree);
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
                ScopeTree.Free();
            }
        }
    }
}
