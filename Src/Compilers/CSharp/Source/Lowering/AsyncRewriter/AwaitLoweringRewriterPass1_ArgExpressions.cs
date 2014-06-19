
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass1
    {
        /// <summary>
        /// Spill an expression list with a receiver (e.g. array access, method call), where at least one of the
        /// receiver or the arguments contains an await expression.
        /// </summary>
        private Tuple<BoundExpression, ReadOnlyArray<BoundExpression>> SpillExpressionsWithReceiver(
            BoundExpression receiverOpt,
            ReadOnlyArray<BoundExpression> expressions,
            SpillBuilder spillBuilder,
            ReadOnlyArray<RefKind> refKindsOpt)
        {
            if (receiverOpt == null)
            {
                return Tuple.Create(default(BoundExpression), SpillExpressionList(spillBuilder, expressions));
            }

            // We have a non-null receiver, and an expression of the form:
            //     receiver[index1, index2, ..., indexN]
            //     or:
            //     receiver(arg1, arg2, ... argN)

            // Build a list containing the receiver and all expressions (in that order)
            var allExpressions = ReadOnlyArray<BoundExpression>.CreateFrom(receiverOpt).Concat(expressions);
            var allRefKinds = (refKindsOpt != null)
                ? ReadOnlyArray<RefKind>.CreateFrom(RefKind.None).Concat(refKindsOpt)
                : ReadOnlyArray<RefKind>.Empty;

            // Spill the expressions (and possibly the receiver):
            var allSpilledExpressions = SpillExpressionList(spillBuilder, allExpressions, allRefKinds);

            var spilledReceiver = allSpilledExpressions.First();
            var spilledArguments = allSpilledExpressions.RemoveFirst();
            return Tuple.Create(spilledReceiver, spilledArguments);
        }

        /// <summary>
        /// Spill a list of expressions (e.g. the arguments of a method call).
        /// 
        /// The expressions are processed right-to-left. Once an expression has been found that contains an await
        /// expression, all subsequent expressions are spilled.
        /// 
        /// Example:
        /// 
        ///     (1 + 2, await t1, Foo(), await t2, 3 + 4)
        /// 
        ///     becomes:
        /// 
        ///     Spill(
        ///         spill1 = 1 + 2,
        ///         spill2 = await t1,
        ///         spill3 = Foo(),
        ///         (spill1, spill2, spill3, await t2, 3 + 4))
        /// 
        /// NOTE: Consider nested array initializers:
        /// 
        ///     new int[] {
        ///         { 1, await t1 },
        ///         { 3, await t2 }
        ///     }
        /// 
        /// If the arguments of the top-level initializer had already been spilled, we would end up trying to spill
        /// something like this:
        /// 
        ///     new int[] {
        ///         Spill(
        ///             spill1 = 1,
        ///             { spill1, await t1 }),
        ///         Spill(
        ///             spill2 = 3,
        ///             { spill2, await t2 })
        ///     }
        /// 
        /// The normal rewriting would produce:
        /// 
        ///     Spill(
        ///         spill1 = 1,
        ///         spill3 = { spill1, await t1 },
        ///         spill2 = 3,
        ///         int[] a = new int[] {
        ///             spill3,
        ///             { spill2, await t2 }))
        /// 
        /// Which is invalid, because spill3 does not have a type.
        /// 
        /// To solve this problem the expression list spilled descends into nested array initializers.
        /// 
        /// </summary>
        private ReadOnlyArray<BoundExpression> SpillExpressionList(
            SpillBuilder outerSpillBuilder,
            ReadOnlyArray<BoundExpression> expressions,
            ReadOnlyArray<RefKind> refKindsOpt = default(ReadOnlyArray<RefKind>))
        {
            var spillBuilders = ArrayBuilder<SpillBuilder>.GetInstance();
            bool spilledFirstArg = false;

            ReadOnlyArray<BoundExpression> newArgs = SpillArgumentListInner(expressions, refKindsOpt, spillBuilders, ref spilledFirstArg);

            var spillBuilder = new SpillBuilder();

            spillBuilders.Reverse();
            foreach (var spill in spillBuilders)
            {
                spillBuilder.AddSpill(spill);
                spill.Free();
            }
            spillBuilders.Free();

            outerSpillBuilder.AddSpill(spillBuilder);
            spillBuilder.Free();

            return newArgs;
        }

        private ReadOnlyArray<BoundExpression> SpillArgumentListInner(
            ReadOnlyArray<BoundExpression> arguments,
            ReadOnlyArray<RefKind> refKindsOpt,
            ArrayBuilder<SpillBuilder> spillBuilders,
            ref bool spilledFirstArg)
        {
            var newArgsBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            var refKindIterator = refKindsOpt != null ? refKindsOpt.AsReverseEnumerable().GetEnumerator() : null;

            foreach (var arg in arguments.AsReverseEnumerable())
            {
                RefKind refKind = (refKindIterator == null) ? RefKind.None :
                    (refKindIterator.MoveNext()) ? refKindIterator.Current :
                    RefKind.None;

                if (arg.Kind == BoundKind.ArrayInitialization)
                {
                    // Descend into a nested array initializer:
                    var nestedInitializer = ((BoundArrayInitialization)arg);
                    var newInitializers = SpillArgumentListInner(nestedInitializer.Initializers, ReadOnlyArray<RefKind>.Empty, spillBuilders, ref spilledFirstArg);
                    newArgsBuilder.Add(nestedInitializer.Update(newInitializers));
                    continue;
                }

                if (arg.Kind == BoundKind.ArgListOperator)
                {
                    // Descend into arglist:
                    var argList = (BoundArgListOperator)arg;
                    var newArgs = SpillArgumentListInner(argList.Arguments, argList.ArgumentRefKindsOpt, spillBuilders, ref spilledFirstArg);
                    newArgsBuilder.Add(argList.Update(newArgs, argList.ArgumentRefKindsOpt, argList.Type));
                    continue;
                }

                var spillBuilder = new SpillBuilder();

                BoundExpression newExpression;
                if (!spilledFirstArg)
                {
                    if (arg.Kind == BoundKind.SpillSequence)
                    {
                        // We have found the right-most expression containing an await expression. Save the await
                        // result to a temp local
                        spilledFirstArg = true;
                        var spill = (BoundSpillSequence)arg;
                        spillBuilder.AddSpill(spill);
                        newExpression = spill.Value;
                    }
                    else
                    {
                        // We are to the right of any await-containing expressions. The args do not yet need to be
                        // spilled.
                        newExpression = arg;    
                    }
                }
                else
                {
                    // We are to the left of an await-containing expression. Spill the arg.

                    if (Unspillable(arg) ||
                        (arg.Kind == BoundKind.FieldAccess && Unspillable(((BoundFieldAccess)arg).ReceiverOpt)))
                    {
                        newExpression = arg;
                    }
                    else if (refKind != RefKind.None)
                    {
                        newExpression = SpillLValue(arg, spillBuilder);
                    }
                    else
                    {
                        var spillTemp = F.SpillTemp(arg.Type, arg);
                        spillBuilder.Temps.Add(spillTemp);
                        spillBuilder.Statements.Add(GenerateSpillInit(spillTemp));
                        newExpression = spillTemp;
                    }
                }

                newArgsBuilder.Add(newExpression);
                spillBuilders.Add(spillBuilder);
            }

            newArgsBuilder.Reverse();
            return newArgsBuilder.ToReadOnlyAndFree();
        }

        #region Visitors

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            ReadOnlyArray<BoundExpression> bounds = this.VisitList(node.Bounds);
            BoundArrayInitialization visitedInitializer = (BoundArrayInitialization)this.Visit(node.InitializerOpt);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(bounds) && (visitedInitializer == null || !RequiresSpill(visitedInitializer.Initializers)))
            {
                return node.Update(bounds, visitedInitializer, type);
            }

            var spillBuilder = new SpillBuilder();
            ReadOnlyArray<BoundExpression> newBounds = SpillExpressionList(spillBuilder, bounds);
            BoundArrayInitialization newInitializerOpt = (visitedInitializer == null) ? visitedInitializer :
                visitedInitializer.Update(SpillExpressionList(spillBuilder, visitedInitializer.Initializers));
            BoundArrayCreation newArrayCreation = node.Update(newBounds, newInitializerOpt, type);
            return spillBuilder.BuildSequenceAndFree(F, newArrayCreation);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            ReadOnlyArray<BoundExpression> arguments = (ReadOnlyArray<BoundExpression>)this.VisitList(node.Arguments);
            BoundExpression initializerExpressionOpt = (BoundExpression)this.Visit(node.InitializerExpressionOpt);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(arguments) && (initializerExpressionOpt == null || initializerExpressionOpt.Kind != BoundKind.SpillSequence))
            {
                return node.Update(node.Constructor, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.ConstantValueOpt, initializerExpressionOpt, type);
            }

            var spillBuilder = new SpillBuilder();
            ReadOnlyArray<BoundExpression> newArguments = SpillExpressionList(spillBuilder, arguments);

            BoundExpression newInitializerExpressionOpt;
            if (initializerExpressionOpt != null && initializerExpressionOpt.Kind == BoundKind.SpillSequence)
            {
                var spill = (BoundSpillSequence)initializerExpressionOpt;
                spillBuilder.AddSpill(spill);
                newInitializerExpressionOpt = spill.Value;
            }
            else
            {
                newInitializerExpressionOpt = initializerExpressionOpt;
            }

            BoundObjectCreationExpression newObjectCreation = node.Update(
                node.Constructor,
                newArguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.Expanded,
                node.ArgsToParamsOpt,
                node.ConstantValueOpt,
                newInitializerExpressionOpt,
                type);

            return spillBuilder.BuildSequenceAndFree(F, newObjectCreation);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            BoundExpression receiverOpt = (BoundExpression)this.Visit(node.ReceiverOpt);
            ReadOnlyArray<BoundExpression> arguments = this.VisitList(node.Arguments);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(arguments) && !RequiresSpill(receiverOpt))
            {
                return node.Update(
                    receiverOpt, 
                    node.Method, 
                    arguments, 
                    node.ArgumentNamesOpt, 
                    node.ArgumentRefKindsOpt, 
                    node.IsDelegateCall,
                    node.Expanded, 
                    node.InvokedAsExtensionMethod, 
                    node.ArgsToParamsOpt, 
                    node.ResultKind, type);
            }

            var spillBuilder = new SpillBuilder();
            var spillResult = SpillExpressionsWithReceiver(receiverOpt, arguments, spillBuilder, node.Method.ParameterRefKinds);

            var newCall = node.Update(
                spillResult.Item1,
                node.Method,
                spillResult.Item2,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.IsDelegateCall,
                node.Expanded,
                node.InvokedAsExtensionMethod,
                node.ArgsToParamsOpt,
                node.ResultKind,
                type);

            return spillBuilder.BuildSequenceAndFree(F, newCall);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            BoundExpression expression = (BoundExpression)this.Visit(node.Expression);
            ReadOnlyArray<BoundExpression> indices = (ReadOnlyArray<BoundExpression>)this.VisitList(node.Indices);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(indices) && expression.Kind != BoundKind.SpillSequence)
            {
                return node.Update(expression, indices, type);
            }

            var spillBuilder = new SpillBuilder();
            var spillResult = SpillExpressionsWithReceiver(expression, indices, spillBuilder, refKindsOpt: ReadOnlyArray<RefKind>.Empty);
            BoundExpression newBoundArrayAccess = node.Update(spillResult.Item1, spillResult.Item2, type);
            return spillBuilder.BuildSequenceAndFree(F, newBoundArrayAccess);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundExpression left = (BoundExpression)this.Visit(node.Left);
            BoundExpression right = (BoundExpression)this.Visit(node.Right);
            TypeSymbol type = this.VisitType(node.Type);
            
            if (!RequiresSpill(left, right))
            {
                return node.Update(node.OperatorKind, left, right, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, type);
            }

            var subExprs = ReadOnlyArray<BoundExpression>.CreateFrom(left, right);
            var spillBuilder = new SpillBuilder();
            var newArgs = SpillExpressionList(spillBuilder, subExprs);
            Debug.Assert(newArgs.Count == 2);

            var newBinaryOperator = node.Update(
                node.OperatorKind,
                newArgs[0],
                newArgs[1],
                node.ConstantValueOpt,
                node.MethodOpt,
                node.ResultKind,
                type);

            return spillBuilder.BuildSequenceAndFree(F, newBinaryOperator);
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            BoundExpression condition = (BoundExpression)this.Visit(node.Condition);
            BoundExpression consequence = (BoundExpression)this.Visit(node.Consequence);
            BoundExpression alternative = (BoundExpression)this.Visit(node.Alternative);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(condition, consequence, alternative))
            {
                return node.Update(condition, consequence, alternative, node.ConstantValueOpt, type);
            }

            var spillBuilder = new SpillBuilder();

            LocalSymbol resultLocal = F.SynthesizedLocal(type, null);
            spillBuilder.Locals.Add(resultLocal);

            BoundExpression newCondition;
            if (condition.Kind == BoundKind.SpillSequence)
            {
                var spill = (BoundSpillSequence)condition;
                spillBuilder.AddSpill(spill);
                newCondition = spill.Value;
            }
            else
            {
                newCondition = condition;
            }

            spillBuilder.Statements.Add(
                F.If(
                    condition: newCondition,
                    thenClause: CrushExpression(consequence, resultLocal),
                    elseClause: CrushExpression(alternative, resultLocal)));

            return spillBuilder.BuildSequenceAndFree(F, F.Local(resultLocal));

        }

        private BoundStatement CrushExpression(BoundExpression node, LocalSymbol resultLocal)
        {
            if (node.Kind == BoundKind.SpillSequence)
            {
                var spill = (BoundSpillSequence)node;
                return RewriteSpillSequenceAsBlock(spill, F.Assignment(F.Local(resultLocal), spill.Value));
            }
            else
            {
                return F.Assignment(F.Local(resultLocal), node);
            }
        }

        #endregion
    }
}
