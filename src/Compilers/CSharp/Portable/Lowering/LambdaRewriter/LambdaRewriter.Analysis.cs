// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LambdaRewriter
    {
        /// <summary>
        /// Perform a first analysis pass in preparation for removing all lambdas from a method body.  The entry point is Analyze.
        /// The results of analysis are placed in the fields seenLambda, blockParent, variableBlock, captured, and captures.
        /// </summary>
        internal sealed class Analysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly MethodSymbol _topLevelMethod;

            private MethodSymbol _currentParent;
            private BoundNode _currentScope;

            // Some syntactic forms have an "implicit" receiver.  When we encounter them, we set this to the
            // syntax.  That way, in case we need to report an error about the receiver, we can use this
            // syntax for the location when the receiver was implicit.
            private CSharpSyntaxNode _syntaxWithReceiver;

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
            public MultiDictionary<Symbol, CSharpSyntaxNode> CapturedVariables = new MultiDictionary<Symbol, CSharpSyntaxNode>();

            /// <summary>
            /// For each lambda in the code, the set of variables that it captures.
            /// </summary>
            public MultiDictionary<MethodSymbol, Symbol> CapturedVariablesByLambda = new MultiDictionary<MethodSymbol, Symbol>();

            /// <summary>
            /// If a local function is in the set, at some point in the code it is converted to a delegate and should then not be optimized to a struct closure.
            /// Also contains all lambdas (as they are converted to delegates implicitly).
            /// </summary>
            public readonly HashSet<MethodSymbol> MethodsConvertedToDelegates = new HashSet<MethodSymbol>();

            /// <summary>
            /// Any scope that a method in <see cref="MethodsConvertedToDelegates"/> closes over. If a scope is in this set, don't use a struct closure.
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
            /// Optimizes local functions such that if a local function only references other local functions without closures, it itself doesn't need a closure.
            /// </summary>
            private void RemoveUnneededReferences()
            {
                // Note: methodGraph is the inverse of the dependency graph
                var methodGraph = new MultiDictionary<MethodSymbol, MethodSymbol>();
                var capturesThis = new HashSet<MethodSymbol>();
                var capturesVariable = new HashSet<MethodSymbol>();
                var visitStack = new Stack<MethodSymbol>();
                foreach (var methodKvp in CapturedVariablesByLambda)
                {
                    foreach (var value in methodKvp.Value)
                    {
                        var method = value as MethodSymbol;
                        if (method != null)
                        {
                            methodGraph.Add(method, methodKvp.Key);
                        }
                        else if (value == _topLevelMethod.ThisParameter)
                        {
                            if (capturesThis.Add(methodKvp.Key))
                            {
                                visitStack.Push(methodKvp.Key);
                            }
                        }
                        else if (capturesVariable.Add(methodKvp.Key) && !capturesThis.Contains(methodKvp.Key)) // if capturesThis contains methodKvp, it's already in the stack.
                        {
                            visitStack.Push(methodKvp.Key);
                        }
                    }
                }

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

                var capturedVariablesNew = new MultiDictionary<Symbol, CSharpSyntaxNode>();
                foreach (var old in CapturedVariables)
                {
                    var method = old.Key as MethodSymbol;
                    // don't add if it's a method that only captures 'this'
                    if (method == null || capturesVariable.Contains(method))
                    {
                        foreach (var oldValue in old.Value)
                        {
                            capturedVariablesNew.Add(old.Key, oldValue);
                        }
                    }
                }
                CapturedVariables = capturedVariablesNew;
                var capturedVariablesByLambdaNew = new MultiDictionary<MethodSymbol, Symbol>();
                foreach (var old in CapturedVariablesByLambda)
                {
                    if (capturesVariable.Contains(old.Key))
                    {
                        foreach (var oldValue in old.Value)
                        {
                            capturedVariablesByLambdaNew.Add(old.Key, oldValue);
                        }
                    }
                    else if (capturesThis.Contains(old.Key))
                    {
                        capturedVariablesByLambdaNew.Add(old.Key, _topLevelMethod.ThisParameter);
                    }
                }
                CapturedVariablesByLambda = capturedVariablesByLambdaNew;
            }

            /// <summary>
            /// Create the optimized plan for the location of lambda methods and whether scopes need access to parent scopes
            ///  </summary>
            internal void ComputeLambdaScopesAndFrameCaptures()
            {
                LambdaScopes = new Dictionary<MethodSymbol, BoundNode>(ReferenceEqualityComparer.Instance);
                NeedsParentFrame = new HashSet<BoundNode>();

                RemoveUnneededReferences();

                foreach (var kvp in CapturedVariablesByLambda)
                {
                    // get innermost and outermost scopes from which a lambda captures

                    int innermostScopeDepth = -1;
                    BoundNode innermostScope = null;

                    int outermostScopeDepth = int.MaxValue;
                    BoundNode outermostScope = null;

                    foreach (var variables in kvp.Value)
                    {
                        BoundNode curBlock = null;
                        int curBlockDepth;

                        if (!VariableScope.TryGetValue(variables, out curBlock))
                        {
                            // this is something that is not defined in a block, like "Me"
                            // Since it is defined outside of the method, the depth is -1
                            curBlockDepth = -1;
                        }
                        else
                        {
                            curBlockDepth = BlockDepth(curBlock);
                        }

                        if (curBlockDepth > innermostScopeDepth)
                        {
                            innermostScopeDepth = curBlockDepth;
                            innermostScope = curBlock;
                        }

                        if (curBlockDepth < outermostScopeDepth)
                        {
                            outermostScopeDepth = curBlockDepth;
                            outermostScope = curBlock;
                        }
                    }

                    // 1) if there is innermost scope, lambda goes there as we cannot go any higher.
                    // 2) scopes in [innermostScope, outermostScope) chain need to have access to the parent scope.
                    //
                    // Example: 
                    //   if a lambda captures a method//s parameter and Me, 
                    //   its innermost scope depth is 0 (method locals and parameters) 
                    //   and outermost scope is -1
                    //   Such lambda will be placed in a closure frame that corresponds to the method//s outer block
                    //   and this frame will also lift original Me as a field when created by its parent.
                    //   Note that it is completely irrelevant how deeply the lexical scope of the lambda was originally nested.
                    if (innermostScope != null)
                    {
                        LambdaScopes.Add(kvp.Key, innermostScope);

                        // Disable struct closures on methods converted to delegates, as well as on async and iterator methods.
                        var markAsNoStruct = MethodsConvertedToDelegates.Contains(kvp.Key) || kvp.Key.IsAsync || kvp.Key.IsIterator;
                        if (markAsNoStruct)
                        {
                            ScopesThatCantBeStructs.Add(innermostScope);
                        }

                        while (innermostScope != outermostScope)
                        {
                            NeedsParentFrame.Add(innermostScope);
                            ScopeParent.TryGetValue(innermostScope, out innermostScope);
                            if (markAsNoStruct)
                            {
                                ScopesThatCantBeStructs.Add(innermostScope);
                            }
                        }
                    }
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
                var local = node.LocalOpt;

                if ((object)local == null)
                {
                    return base.VisitCatchBlock(node);
                }

                var previousBlock = PushBlock(node, ImmutableArray.Create(local));
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

            private void ReferenceVariable(CSharpSyntaxNode syntax, Symbol symbol)
            {
                var localSymbol = symbol as LocalSymbol;
                if ((object)localSymbol != null && localSymbol.IsConst)
                {
                    // "constant variables" need not be captured
                    return;
                }

                // using generic MethodSymbol here and not LambdaSymbol because of local functions
                MethodSymbol lambda = _currentParent as MethodSymbol;
                // "symbol == lambda" could happen if we're recursive
                if ((object)lambda != null && symbol != lambda && symbol.ContainingSymbol != lambda)
                {
                    CapturedVariables.Add(symbol, syntax);

                    // mark the variable as captured in each enclosing lambda up to the variable's point of declaration.
                    for (; (object)lambda != null && symbol != lambda && symbol.ContainingSymbol != lambda; lambda = lambda.ContainingSymbol as MethodSymbol)
                    {
                        CapturedVariablesByLambda.Add(lambda, symbol);
                    }
                }
            }

            private BoundNode VisitSyntaxWithReceiver(CSharpSyntaxNode syntax, BoundNode receiver)
            {
                var previousSyntax = _syntaxWithReceiver;
                _syntaxWithReceiver = syntax;
                var result = Visit(receiver);
                _syntaxWithReceiver = previousSyntax;
                return result;
            }

            public override BoundNode VisitMethodGroup(BoundMethodGroup node)
            {
                // We only get here in error cases, as normally the enclosing node is a method group conversion
                // whose visit (below) doesn't call this.  So we don't know which method is to be selected, and
                // therefore don't know if the receiver is used. Assume if the receiver was provided, it is used.
                var receiverOpt = node.ReceiverOpt;
                if (receiverOpt != null)
                {
                    return VisitSyntaxWithReceiver(node.Syntax, receiverOpt);
                }
                return null;
            }

            public override BoundNode VisitConversion(BoundConversion node)
            {
                if (node.ConversionKind == ConversionKind.MethodGroup)
                {
                    if (node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
                    {
                        // Use OriginalDefinition to strip generic type parameters
                        ReferenceVariable(node.Syntax, node.SymbolOpt.OriginalDefinition);
                        MethodsConvertedToDelegates.Add(node.SymbolOpt.OriginalDefinition);
                    }
                    if (node.IsExtensionMethod || ((object)node.SymbolOpt != null && !node.SymbolOpt.IsStatic))
                    {
                        return VisitSyntaxWithReceiver(node.Syntax, ((BoundMethodGroup)node.Operand).ReceiverOpt);
                    }
                    return null;
                }
                else
                {
                    return base.VisitConversion(node);
                }
            }

            public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
            {
                _syntaxWithReceiver = node.Syntax;
                return base.VisitPropertyAccess(node);
            }

            public override BoundNode VisitFieldAccess(BoundFieldAccess node)
            {
                _syntaxWithReceiver = node.Syntax;
                return base.VisitFieldAccess(node);
            }

            public override BoundNode VisitEventAccess(BoundEventAccess node)
            {
                _syntaxWithReceiver = node.Syntax;
                return base.VisitEventAccess(node);
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
