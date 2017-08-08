// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            /// Any scope that a method that <see cref="CanTakeRefParameters(MethodSymbol)"/> doesn't close over.
            /// If a scope is in this set, don't use a struct closure.
            /// </summary>
            public readonly PooledHashSet<BoundNode> ScopesThatCantBeStructs = PooledHashSet<BoundNode>.GetInstance();

            /// <summary>
            /// Blocks that are positioned between a block declaring some lifted variables
            /// and a block that contains the lambda that lifts said variables.
            /// If such block itself requires a closure, then it must lift parent frame pointer into the closure
            /// in addition to whatever else needs to be lifted.
            /// <see cref="ComputeLambdaScopesAndFrameCaptures"/> needs to be called to compute this.
            /// </summary>
            public readonly PooledHashSet<BoundNode> NeedsParentFrame = PooledHashSet<BoundNode>.GetInstance();

            /// <summary>
            /// Optimized locations of lambdas. 
            /// 
            /// Lambda does not need to be placed in a frame that corresponds to its lexical scope if lambda does not reference any local state in that scope.
            /// It is advantageous to place lambdas higher in the scope tree, ideally in the innermost scope of all scopes that contain variables captured by a given lambda.
            /// Doing so reduces indirections needed when captured locals are accessed. For example locals from the innermost scope can be accessed with no indirection at all.
            /// <see cref="ComputeLambdaScopesAndFrameCaptures"/> needs to be called to compute this.
            /// </summary>
            public readonly SmallDictionary<MethodSymbol, BoundNode> LambdaScopes =
                new SmallDictionary<MethodSymbol, BoundNode>(ReferenceEqualityComparer.Instance);

            /// <summary>
            /// The root of the scope tree for this method.
            /// </summary>
            public readonly Scope ScopeTree;

            private Analysis(Scope scopeTree, PooledHashSet<MethodSymbol> methodsConvertedToDelegates)
            {
                ScopeTree = scopeTree;
                MethodsConvertedToDelegates = methodsConvertedToDelegates;
            }

            public static Analysis Analyze(BoundNode node, MethodSymbol method, DiagnosticBag diagnostics)
            {
                var methodsConvertedToDelegates = PooledHashSet<MethodSymbol>.GetInstance();
                var scopeTree = ScopeTreeBuilder.Build(
                    node,
                    method,
                    methodsConvertedToDelegates,
                    diagnostics);
                return new Analysis(scopeTree, methodsConvertedToDelegates);
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
            internal void ComputeLambdaScopesAndFrameCaptures(ParameterSymbol thisParam)
            {
                RemoveUnneededReferences(thisParam);

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
                ScopesThatCantBeStructs.Free();
                NeedsParentFrame.Free();
                ScopeTree.Free();
            }
        }
    }
}
