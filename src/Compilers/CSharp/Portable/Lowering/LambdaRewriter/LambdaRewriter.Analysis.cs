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
            private Dictionary<Scope, HashSet<Closure>> _closuresCapturingScopeVariables;

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
                analysis.MergeEnvironments();
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
                var closuresCapturingScopeVariables = new Dictionary<Scope, HashSet<Closure>>();
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
                    var capturingClosures = new HashSet<Closure>();
                    foreach (var closure in closures)
                    {
                        capturingClosures.Add(closure);

                        closure.CapturedEnvironments.Add(env);
                        if (thisParam != null && env.CapturedVariables.Contains(thisParam))
                        {
                            closure.CapturesThis = true;
                        }
                    }

                    closuresCapturingScopeVariables.Add(scope, capturingClosures);
                });

                _closuresCapturingScopeVariables = closuresCapturingScopeVariables;
            }

            /// <summary>
            /// Must be called only after <see cref="MakeAndAssignEnvironments"/> and <see cref="ComputeLambdaScopesAndFrameCaptures"/>
            /// 
            /// In order to reduce allocations, merge environments into a parent environment when it is safe to do so.
            /// This must be done whilst preserving semantics.
            /// 
            /// We also have to make sure not to extend the life of any variable.
            /// This means that we can only merge an environment into its parent if exactly the same closures directly or indirectly reference both environments.
            /// </summary>
            private void MergeEnvironments()
            {
                // for now we don't analyze jumps to check if it is safe to optimize, and instead return.
                if (containsJumps(ScopeTree.BoundNode.Syntax))
                    return;

                // if a closure captures a scope, which captures its parent, then the closure also captures the parents scope.
                // we update _closuresCapturingScopeVariables to reflect this.
                foreach (var (scope, capturingClosures) in _closuresCapturingScopeVariables)
                {
                    if (scope.DeclaredEnvironments.Count == 0)
                        continue;
                    // Right now we only create one environment per scope
                    Debug.Assert(scope.DeclaredEnvironments.Count == 1);
                    var env = scope.DeclaredEnvironments[0];

                    if (env.CapturesParent)
                    {
                        var currentScope = scope.Parent;
                        while (true)
                        {
                            if (currentScope == null)
                            {
                                throw ExceptionUtilities.Unreachable;
                            }

                            if (currentScope.DeclaredEnvironments.Count == 0 || currentScope.DeclaredEnvironments[0].IsStruct)
                            {
                                currentScope = currentScope.Parent;
                                continue;
                            }

                            _closuresCapturingScopeVariables[currentScope].AddAll(capturingClosures);

                            // Right now we only create one environment per scope
                            Debug.Assert(scope.DeclaredEnvironments.Count == 1);
                            if (currentScope.DeclaredEnvironments[0].CapturesParent)
                            {
                                currentScope = currentScope.Parent;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                // now we merge environments into their parent environments if it is safe to do so
                foreach (var (scope, capturingClosures) in _closuresCapturingScopeVariables)
                {
                    if (capturingClosures.Count == 0)
                        continue;

                    var scopeEnv = scope.DeclaredEnvironments[0];
                    if (scopeEnv.IsStruct)
                        continue;

                    var bestScope = scope;
                    var currentScope = scope;

                    while (currentScope.Parent != null)
                    {
                        if (!semanticallySafeToUseParentEvironment(currentScope))
                            break;

                        var parentScope = currentScope.Parent;

                        if (!_closuresCapturingScopeVariables.TryGetValue(parentScope, out var parentCapturingClosures)
                            || parentScope.DeclaredEnvironments.Count == 0)
                        {
                            currentScope = parentScope;
                            continue;
                        }

                        // if more closures reference one scope's environments than the other scope's environments,
                        // then merging the two environments would increase the number of objects referencing some variables, 
                        // which may prevent the variables being garbage collected.
                        if (!parentCapturingClosures.SetEquals(capturingClosures))
                            break;

                        // Right now we only create one environment per scope
                        Debug.Assert(parentScope.DeclaredEnvironments.Count == 1);
                        if (!parentScope.DeclaredEnvironments[0].IsStruct)
                            bestScope = parentScope;

                        currentScope = parentScope;
                    }

                    if (bestScope == scope)
                        continue;

                    var targetEnv = bestScope.DeclaredEnvironments[0];

                    foreach (var variable in scopeEnv.CapturedVariables)
                    {
                        targetEnv.CapturedVariables.Add(variable);
                    }

                    scope.DeclaredEnvironments.Clear();

                    foreach (var closure in capturingClosures)
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

                    capturingClosures.Clear();
                }

                // Recursively checks if a syntax tree contains GOTO statements
                bool containsJumps(SyntaxNode node)
                {
                    if (node.Kind() == SyntaxKind.GotoStatement)
                        return true;

                    foreach (var childNode in node.ChildNodes())
                        if (containsJumps(childNode))
                            return true;

                    return false;
                }

                // Checks if (aside from GOTOs) it is semantically safe to merge a capture environment into it's parent's scope's capture environments.
                // So long as the scopes are passed through in a linear fashion it is always safe to do so. Only looping construct (for, foreach, while, do-while) require special rules.
                bool semanticallySafeToUseParentEvironment(Scope scope)
                {
                    switch (scope.BoundNode.Syntax.Kind())
                    {
                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.WhileStatement:
                            return false;
                        case SyntaxKind.ForStatement:
                            // Check if this is the initializer (which can be moved to the parent scope) or the condition/iterator (which cannot)
                            // To do so, we check if the variables declared in the scope appear before the first semicolon, or after the first semicolon in the for statement
                            var forStatement = (ForStatementSyntax)scope.BoundNode.Syntax;
                            var variables = scope.DeclaredVariables;

                            // A closure cannot capture a scope unless it contains some variables
                            Debug.Assert(variables.Count > 0);
                            var declaration = variables[0].DeclaringSyntaxReferences;

                            // A local can only be declared in one location
                            Debug.Assert(declaration.Length == 1);
                            if (forStatement.FirstSemicolonToken.Span.Start < declaration[0].Span.End)
                            {
                                return false;
                            }
                            goto default;
                        default:
                            // walk up the syntax tree till you reach the parent scope, and make sure no backwards jumps occur along the way
                            var currentSyntaxNode = scope.BoundNode.Syntax.Parent;
                            if(currentSyntaxNode == null)
                            {
                                throw ExceptionUtilities.Unreachable;
                            }
                            while (currentSyntaxNode.Position >= scope.Parent.BoundNode.Syntax.Position)
                            {
                                var kind = currentSyntaxNode.Kind();
                                if (kind == SyntaxKind.DoStatement || kind == SyntaxKind.ForStatement)
                                {
                                    return false;
                                }
                                if (kind == SyntaxKind.WhileStatement && currentSyntaxNode.Position > scope.Parent.BoundNode.Syntax.Position)
                                {
                                    return false;
                                }

                                if (currentSyntaxNode.Parent == null)
                                {
                                    if (currentSyntaxNode.Position == scope.Parent.BoundNode.Syntax.Position)
                                    {
                                        return true;
                                    }

                                    throw ExceptionUtilities.Unreachable;
                                }

                                currentSyntaxNode = currentSyntaxNode.Parent;
                            }

                            return true;
                    }
                }
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
                ScopeTree.Free();
            }
        }
    }
}
