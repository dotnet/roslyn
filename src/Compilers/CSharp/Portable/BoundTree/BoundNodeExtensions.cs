// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class BoundNodeExtensions
    {
        // Return if any node in an array of nodes has errors.
        public static bool HasErrors<T>(this ImmutableArray<T> nodeArray)
            where T : BoundNode
        {
            if (nodeArray.IsDefault)
                return false;

            for (int i = 0, n = nodeArray.Length; i < n; ++i)
            {
                if (nodeArray[i].HasErrors)
                    return true;
            }

            return false;
        }

        // Like HasErrors property, but also returns false for a null node. 
        public static bool HasErrors([NotNullWhen(true)] this BoundNode? node)
        {
            return node != null && node.HasErrors;
        }

        public static bool IsConstructorInitializer(this BoundStatement statement)
        {
            Debug.Assert(statement != null);
            if (statement!.Kind == BoundKind.ExpressionStatement)
            {
                BoundExpression expression = ((BoundExpressionStatement)statement).Expression;
                if (expression.Kind == BoundKind.Sequence && ((BoundSequence)expression).SideEffects.IsDefaultOrEmpty)
                {
                    // in case there is a pattern variable declared in a ctor-initializer, it gets wrapped in a bound sequence.
                    expression = ((BoundSequence)expression).Value;
                }

                return expression.Kind == BoundKind.Call && ((BoundCall)expression).IsConstructorInitializer();
            }

            return false;
        }

        public static bool IsConstructorInitializer(this BoundCall call)
        {
            Debug.Assert(call != null);
            MethodSymbol method = call!.Method;
            BoundExpression? receiverOpt = call!.ReceiverOpt;
            return method.MethodKind == MethodKind.Constructor &&
                receiverOpt != null &&
                (receiverOpt.Kind == BoundKind.ThisReference || receiverOpt.Kind == BoundKind.BaseReference);
        }

        public static T MakeCompilerGenerated<T>(this T node) where T : BoundNode
        {
            node.WasCompilerGenerated = true;
            return node;
        }

        public static bool ContainsAwaitExpression(this ImmutableArray<BoundExpression> expressions)
        {
            var visitor = new ContainsAwaitVisitor();
            foreach (var expression in expressions)
            {
                visitor.Visit(expression);
                if (visitor.ContainsAwait)
                {
                    return true;
                }
            }

            return false;
        }

        private class ContainsAwaitVisitor : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            public bool ContainsAwait = false;

            public override BoundNode? Visit(BoundNode? node) => ContainsAwait ? null : base.Visit(node);

            public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
            {
                ContainsAwait = true;
                return null;
            }
        }

        /// <summary>
        /// Visits the binary operator tree of interpolated string additions in a depth-first pre-order visit,
        /// meaning parent, left, then right.
        /// <paramref name="stringCallback"/> controls whether to continue the visit by returning true or false:
        /// if true, the visit will continue. If false, the walk will be cut off.
        /// </summary>
        public static bool VisitBinaryOperatorInterpolatedString<TInterpolatedStringType, TArg>(
            this BoundBinaryOperator binary,
            TArg arg,
            Func<TInterpolatedStringType, TArg, bool> stringCallback,
            Action<BoundBinaryOperator, TArg>? binaryOperatorCallback = null)
            where TInterpolatedStringType : BoundInterpolatedStringBase
        {
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();

            pushLeftNodes(binary, stack, arg, binaryOperatorCallback);

            while (stack.TryPop(out BoundBinaryOperator? current))
            {
                switch (current.Left)
                {
                    case BoundBinaryOperator:
                        break;
                    case TInterpolatedStringType interpolatedString:
                        if (!stringCallback(interpolatedString, arg))
                        {
                            return false;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(current.Left.Kind);
                }

                switch (current.Right)
                {
                    case BoundBinaryOperator rightOperator:
                        pushLeftNodes(rightOperator, stack, arg, binaryOperatorCallback);
                        break;
                    case TInterpolatedStringType interpolatedString:
                        if (!stringCallback(interpolatedString, arg))
                        {
                            return false;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(current.Right.Kind);
                }
            }

            Debug.Assert(stack.Count == 0);
            stack.Free();
            return true;

            static void pushLeftNodes(BoundBinaryOperator binary, ArrayBuilder<BoundBinaryOperator> stack, TArg arg, Action<BoundBinaryOperator, TArg>? binaryOperatorCallback)
            {
                Debug.Assert(typeof(TInterpolatedStringType) == typeof(BoundInterpolatedString) || binary.IsUnconvertedInterpolatedStringAddition);
                BoundBinaryOperator? current = binary;
                while (current != null)
                {
                    binaryOperatorCallback?.Invoke(current, arg);
                    stack.Push(current);
                    current = current.Left as BoundBinaryOperator;
                }
            }
        }

        /// <summary>
        /// Rewrites a BoundBinaryOperator composed of interpolated strings (either converted or unconverted) iteratively, without
        /// recursion on the left side of the tree. Nodes of the tree are rewritten in a depth-first post-order fashion, meaning
        /// left, then right, then parent.
        /// </summary>
        /// <param name="binary">The original top of the binary operations.</param>
        /// <param name="arg">The callback args.</param>
        /// <param name="interpolatedStringFactory">
        /// Rewriter for the BoundInterpolatedString or BoundUnconvertedInterpolatedString parts of the binary operator. Passed the callback
        /// parameter, the original interpolated string, and the index of the interpolated string in the tree.
        /// </param>
        /// <param name="binaryOperatorFactory">
        /// Rewriter for the BoundBinaryOperator parts fo the binary operator. Passed the callback parameter, the original binary operator, and
        /// the rewritten left and right components.
        /// </param>
        public static TResult RewriteInterpolatedStringAddition<TInterpolatedStringType, TArg, TResult>(
            this BoundBinaryOperator binary,
            TArg arg,
            Func<TInterpolatedStringType, int, TArg, TResult> interpolatedStringFactory,
            Func<BoundBinaryOperator, TResult, TResult, TArg, TResult> binaryOperatorFactory)
            where TInterpolatedStringType : BoundInterpolatedStringBase
        {
            int i = 0;

            var result = doRewrite(binary, arg, interpolatedStringFactory, binaryOperatorFactory, ref i);

            return result;

            static TResult doRewrite(
                BoundBinaryOperator binary,
                TArg arg,
                Func<TInterpolatedStringType, int, TArg, TResult> interpolatedStringFactory,
                Func<BoundBinaryOperator, TResult, TResult, TArg, TResult> binaryOperatorFactory,
                ref int i)
            {
                TResult? result = default;
                var originalStack = ArrayBuilder<BoundBinaryOperator>.GetInstance();

                pushLeftNodes(binary, originalStack);

                while (originalStack.TryPop(out var currentBinary))
                {
                    Debug.Assert(currentBinary.Left is TInterpolatedStringType || result != null);
                    TResult rewrittenLeft = currentBinary.Left switch
                    {
                        TInterpolatedStringType interpolatedString => interpolatedStringFactory(interpolatedString, i++, arg),
                        BoundBinaryOperator => result!,
                        _ => throw ExceptionUtilities.UnexpectedValue(currentBinary.Left.Kind)
                    };

                    // For simplicity, we use recursion for binary operators on the right side of the tree. We're not traditionally concerned
                    // with long chains of operators on the right side, as without parentheses we'll naturally make a tree that is deep on the
                    // left side. If this ever changes, we can make this algorithm a more complex post-order iterative rewrite instead.

                    var rewrittenRight = currentBinary.Right switch
                    {
                        TInterpolatedStringType interpolatedString => interpolatedStringFactory(interpolatedString, i++, arg),
                        BoundBinaryOperator binaryOperator => doRewrite(binaryOperator, arg, interpolatedStringFactory, binaryOperatorFactory, ref i),
                        _ => throw ExceptionUtilities.UnexpectedValue(currentBinary.Right.Kind)
                    };

                    result = binaryOperatorFactory(currentBinary, rewrittenLeft, rewrittenRight, arg);
                }

                Debug.Assert(result != null);
                originalStack.Free();

                return result;
            }

            static void pushLeftNodes(BoundBinaryOperator binary, ArrayBuilder<BoundBinaryOperator> stack)
            {
                BoundBinaryOperator? current = binary;
                while (current != null)
                {
                    Debug.Assert(typeof(TInterpolatedStringType) == typeof(BoundInterpolatedString) || binary.IsUnconvertedInterpolatedStringAddition);
                    stack.Push(current);
                    current = current.Left as BoundBinaryOperator;
                }
            }
        }

        public static InterpolatedStringHandlerData GetInterpolatedStringHandlerData(this BoundExpression e, bool throwOnMissing = true)
            => e switch
            {
                BoundBinaryOperator { InterpolatedStringHandlerData: { } d } => d,
                BoundInterpolatedString { InterpolationData: { } d } => d,
                BoundBinaryOperator or BoundInterpolatedString when !throwOnMissing => default,
                BoundBinaryOperator or BoundInterpolatedString => throw ExceptionUtilities.Unreachable(),
                _ => throw ExceptionUtilities.UnexpectedValue(e.Kind),
            };
    }
}
