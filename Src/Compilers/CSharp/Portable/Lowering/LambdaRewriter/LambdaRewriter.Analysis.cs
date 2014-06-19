// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class LambdaRewriter
    {
        /// <summary>
        /// Perform a first analysis pass in preparation for removing all lambdas from a method body.  The entry point is Analyze.
        /// The results of analysis are placed in the fields seenLambda, blockParent, variableBlock, captured, and captures.
        /// </summary>
        internal sealed class Analysis : BoundTreeWalker
        {
            private readonly MethodSymbol topLevelMethod;

            private MethodSymbol currentParent;
            private BoundNode currentBlock;

            // Some syntactic forms have an "implicit" receiver.  When we encounter them, we set this to the
            // syntax.  That way, in case we need to report an error about the receiver, we can use this
            // syntax for the location when the receiver was implicit.
            private CSharpSyntaxNode syntaxWithReceiver;

            /// <summary>
            /// Set to true while we are analyzing the interior of an expression lambda.
            /// </summary>
            private bool inExpressionLambda;

            /// <summary>
            /// Set to true of any lambda expressions were seen in the analyzed method body.
            /// </summary>
            public bool SeenLambda { get; private set; }

            /// <summary>
            /// For each statement that defines variables, identifies the nearest enclosing statement that defines variables.
            /// </summary>
            public readonly Dictionary<BoundNode, BoundNode> blockParent = new Dictionary<BoundNode, BoundNode>();

            /// <summary>
            /// For each captured variable, identifies the statement in which it will be moved to a frame class.  This is
            /// normally the block where the variable is introduced, but method parameters are moved
            /// to a frame class within the body of the method.
            /// </summary>
            public readonly Dictionary<Symbol, BoundNode> variableBlock = new Dictionary<Symbol, BoundNode>();

            /// <summary>
            /// The set of captured variables seen in the method body.
            /// </summary>
            public readonly HashSet<Symbol> variablesCaptured = new HashSet<Symbol>();

            /// <summary>
            /// The syntax nodes associated with each captured variable.
            /// </summary>
            public readonly MultiDictionary<Symbol, CSharpSyntaxNode> capturedSyntax = new MultiDictionary<Symbol, CSharpSyntaxNode>();

            /// <summary>
            /// The set of variables that were declared anywhere inside an expression lambda.
            /// </summary>
            public readonly HashSet<Symbol> declaredInsideExpressionLambda = new HashSet<Symbol>();

            /// <summary>
            /// For each lambda in the code, the set of variables that it captures.
            /// </summary>
            public readonly MultiDictionary<LambdaSymbol, Symbol> captures = new MultiDictionary<LambdaSymbol, Symbol>();

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
            public HashSet<BoundNode> needsParentFrame;

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
            public Dictionary<LambdaSymbol, BoundNode> lambdaScopes;

            private Analysis(MethodSymbol method)
            {
                this.currentParent = this.topLevelMethod = method;
            }

            public static Analysis Analyze(BoundNode node, MethodSymbol method)
            {
                var analysis = new Analysis(method);
                analysis.Analyze(node);
                return analysis;
            }

            private void Analyze(BoundNode node)
            {
                currentBlock = FindNodeToAnalyze(node);

                if ((object)topLevelMethod != null)
                {
                    foreach (ParameterSymbol parameter in topLevelMethod.Parameters)
                    {
                        // parameters are counted as if they are inside the block
                        variableBlock[parameter] = currentBlock;
                        if (inExpressionLambda) declaredInsideExpressionLambda.Add(parameter);
                    }
                }

                Visit(node);
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
                lambdaScopes = new Dictionary<LambdaSymbol, BoundNode>(ReferenceEqualityComparer.Instance);
                needsParentFrame = new HashSet<BoundNode>();

                foreach (var lambda in captures.Keys)
                {
                    // get innermost and outermost scopes from which a lambda captures

                    int innermostScopeDepth = -1;
                    BoundNode innermostScope = null;

                    int outermostScopeDepth = int.MaxValue;
                    BoundNode outermostScope = null;

                    foreach (var v in captures[lambda])
                    {
                        BoundNode curBlock = null;
                        int curBlockDepth;

                        if (!variableBlock.TryGetValue(v, out curBlock))
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
                    //   Note that is is completely irrelevant how deeply the lexical scope of the lambda was originally nested.
                    if (innermostScope != null)
                    {
                        lambdaScopes.Add(lambda, innermostScope);

                        while (innermostScope != outermostScope)
                        {
                            needsParentFrame.Add(innermostScope);
                            blockParent.TryGetValue(innermostScope, out innermostScope);
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
                    if (!blockParent.TryGetValue(node, out node))
                    {
                        break;
                    }
                }

                return result;
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                var locals = node.Locals;

                if (locals.IsDefaultOrEmpty)
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
                var previousBlock = currentBlock;
                currentBlock = node;
                if (currentBlock != previousBlock) // not top-level node of the method
                {
                    // (Except for the top-level block) record the parent-child block structure
                    blockParent[currentBlock] = previousBlock;
                }

                if (!locals.IsDefaultOrEmpty)
                {
                    foreach (var local in locals)
                    {
                        variableBlock[local] = currentBlock;
                        if (inExpressionLambda) declaredInsideExpressionLambda.Add(local);
                    }
                }

                return previousBlock;
            }

            private void PopBlock(BoundNode previousBlock)
            {
                currentBlock = previousBlock;
            }

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                Debug.Assert(node.OuterLocals.IsEmpty);
                var localsOpt = node.InnerLocalsOpt;
                if (localsOpt.IsDefaultOrEmpty)
                {
                    // no variables declared inside the switch statement.
                    return base.VisitSwitchStatement(node);
                }

                var previousBlock = PushBlock(node, localsOpt);
                var result = base.VisitSwitchStatement(node);
                PopBlock(previousBlock);
                return result;
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                if (node.LocalsOpt.IsDefaultOrEmpty)
                {
                    // ignore blocks that declare no variables.
                    return base.VisitBlock(node);
                }

                VisitBlockInternal(node);
                return node;
            }

            private void VisitBlockInternal(BoundBlock node)
            {
                var previousBlock = PushBlock(node, node.LocalsOpt);
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

            public override BoundNode VisitLambda(BoundLambda node)
            {
                Debug.Assert((object)node.Symbol != null);
                SeenLambda = true;
                var oldParent = currentParent;
                var oldBlock = currentBlock;
                currentParent = node.Symbol;
                currentBlock = node.Body;
                blockParent[currentBlock] = oldBlock;
                var wasInExpressionLambda = inExpressionLambda;
                inExpressionLambda = inExpressionLambda || node.Type.IsExpressionTree();

                // for the purpose of constructing frames parameters are scoped as if they are inside the lambda block
                foreach (var parameter in node.Symbol.Parameters)
                {
                    variableBlock[parameter] = currentBlock;
                    if (inExpressionLambda) declaredInsideExpressionLambda.Add(parameter);
                }

                if (!node.Body.LocalsOpt.IsDefaultOrEmpty)
                {
                    foreach (var local in node.Body.LocalsOpt)
                    {
                        variableBlock[local] = currentBlock;
                        if (inExpressionLambda) declaredInsideExpressionLambda.Add(local);
                    }
                }

                var result = base.VisitBlock(node.Body);
                inExpressionLambda = wasInExpressionLambda;
                currentParent = oldParent;
                currentBlock = oldBlock;
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

                LambdaSymbol lambda = currentParent as LambdaSymbol;
                if ((object)lambda != null && symbol.ContainingSymbol != lambda)
                {
                    variablesCaptured.Add(symbol);
                    capturedSyntax.Add(symbol, syntax);

                    // mark the variable as captured in each enclosing lambda up to the variable's point of declaration.
                    for (; (object)lambda != null && symbol.ContainingSymbol != lambda; lambda = lambda.ContainingSymbol as LambdaSymbol)
                    {
                        captures.Add(lambda, symbol);
                    }
                }
            }

            private BoundNode VisitSyntaxWithReceiver(CSharpSyntaxNode syntax, BoundNode receiver)
            {
                var previousSyntax = syntaxWithReceiver;
                syntaxWithReceiver = syntax;
                var result = Visit(receiver);
                syntaxWithReceiver = previousSyntax;
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
                syntaxWithReceiver = node.Syntax;
                return base.VisitPropertyAccess(node);
            }

            public override BoundNode VisitFieldAccess(BoundFieldAccess node)
            {
                syntaxWithReceiver = node.Syntax;
                return base.VisitFieldAccess(node);
            }

            public override BoundNode VisitEventAccess(BoundEventAccess node)
            {
                syntaxWithReceiver = node.Syntax;
                return base.VisitEventAccess(node);
            }

            public override BoundNode VisitThisReference(BoundThisReference node)
            {
                var thisParam = this.topLevelMethod.ThisParameter;
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
                ReferenceVariable(node.Syntax, this.topLevelMethod.ThisParameter);
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
