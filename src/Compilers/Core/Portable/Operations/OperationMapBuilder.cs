// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis
{
    internal static class OperationMapBuilder
    {
        /// <summary>
        /// Populates a empty dictionary of SyntaxNode->IOperation, where every key corresponds to an explicit IOperation node.
        /// If there is a SyntaxNode with more than one explicit IOperation, this will throw.
        /// </summary>
        internal static void AddToMap(IOperation root, Dictionary<SyntaxNode, IOperation> dictionary)
        {
            Debug.Assert(dictionary.Count == 0);
            Walker.Instance.Visit(root, dictionary);
        }

        private sealed class Walker : OperationWalker<Dictionary<SyntaxNode, IOperation>>
        {
            internal static readonly Walker Instance = new Walker();

            public override object? DefaultVisit(IOperation operation, Dictionary<SyntaxNode, IOperation> argument)
            {
                RecordOperation(operation, argument);
                return base.DefaultVisit(operation, argument);
            }

            public override object? VisitBinaryOperator([DisallowNull] IBinaryOperation? operation, Dictionary<SyntaxNode, IOperation> argument)
            {
                // In order to handle very large nested operators, we implement manual iteration here. Our operations are not order sensitive,
                // so we don't need to maintain a stack, just iterate through every level.
                while (true)
                {
                    RecordOperation(operation, argument);
                    Visit(operation.RightOperand, argument);
                    if (operation.LeftOperand is IBinaryOperation nested)
                    {
                        operation = nested;
                    }
                    else
                    {
                        Visit(operation.LeftOperand, argument);
                        break;
                    }
                }

                return null;
            }

            internal override object? VisitNoneOperation(IOperation operation, Dictionary<SyntaxNode, IOperation> argument)
            {
                // OperationWalker skips these nodes by default, to avoid having public consumers deal with NoneOperation.
                // we need to deal with it here, however, so delegate to DefaultVisit.
                return DefaultVisit(operation, argument);
            }

            private static void RecordOperation(IOperation operation, Dictionary<SyntaxNode, IOperation> argument)
            {
                if (!operation.IsImplicit)
                {
                    // IOperation invariant is that all there is at most 1 non-implicit node per syntax node.
                    Debug.Assert(!argument.ContainsKey(operation.Syntax),
                        $"Duplicate operation node for {operation.Syntax}. Existing node is {(argument.TryGetValue(operation.Syntax, out var original) ? original.Kind : null)}, new node is {operation.Kind}.");
                    argument.Add(operation.Syntax, operation);
                }
            }
        }
    }
}
