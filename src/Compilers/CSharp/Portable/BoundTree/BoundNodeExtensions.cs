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

        public static bool VisitBinaryOperatorInterpolatedString<TArg, TInterpolatedStringType>(
            this BoundBinaryOperator binary,
            TArg arg,
            Func<TInterpolatedStringType, TArg, bool> visitor,
            Action<BoundBinaryOperator, TArg>? binaryOperatorCallback = null)
            where TInterpolatedStringType : BoundExpression
        {
            Debug.Assert(typeof(TInterpolatedStringType) == typeof(BoundUnconvertedInterpolatedString) || typeof(TInterpolatedStringType) == typeof(BoundInterpolatedString));
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();

            pushLeftNodes(binary, stack);

            while (stack.TryPop(out BoundBinaryOperator? current))
            {
                switch (current.Left)
                {
                    case BoundBinaryOperator op:
                        binaryOperatorCallback?.Invoke(op, arg);
                        break;
                    case TInterpolatedStringType interpolatedString:
                        if (!visitor(interpolatedString, arg))
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
                        binaryOperatorCallback?.Invoke(rightOperator, arg);
                        pushLeftNodes(rightOperator, stack);
                        break;
                    case TInterpolatedStringType interpolatedString:
                        if (!visitor(interpolatedString, arg))
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

            static void pushLeftNodes(BoundBinaryOperator binary, ArrayBuilder<BoundBinaryOperator> stack)
            {
                Debug.Assert(typeof(TInterpolatedStringType) == typeof(BoundInterpolatedString) || binary.IsUnconvertedInterpolatedStringAddition);
                BoundBinaryOperator? current = binary;
                while (current != null)
                {
                    stack.Push(current);
                    current = current.Left as BoundBinaryOperator;
                }
            }
        }
    }
}
