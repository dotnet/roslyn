// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis
{
    internal sealed class OperationMapBuilder : OperationWalker<Dictionary<SyntaxNode, IOperation>>
    {
        internal static readonly OperationMapBuilder Instance = new OperationMapBuilder();

        private OperationMapBuilder() { }

        public override object? Visit(IOperation? operation, Dictionary<SyntaxNode, IOperation> argument)
        {
            if (operation == null)
            {
                return null;
            }

            RecordOperation(operation, argument);

            return base.Visit(operation, argument);
        }

        public override object? VisitBinaryOperator([DisallowNull] IBinaryOperation? operation, Dictionary<SyntaxNode, IOperation> argument)
        {
            // In order to handle very large nested operators, we implement manual iteration here. Our operations are not order sensitive,
            // so we don't need to maintain a stack, just iterate through every level. Visit above will have already recorded the given operation.
            Debug.Assert(argument.ContainsKey(operation.Syntax) || operation.IsImplicit);
            do
            {
                Visit(operation.RightOperand, argument);
                if (operation.LeftOperand is IBinaryOperation nested)
                {
                    operation = nested;
                    RecordOperation(operation, argument);
                }
                else
                {
                    Visit(operation.LeftOperand, argument);
                    break;
                }
            } while (true);

            return null;
        }

        private static void RecordOperation(IOperation operation, Dictionary<SyntaxNode, IOperation> argument)
        {
            if (!operation.IsImplicit)
            {
                // IOperation invariant is that all there is at most 1 non-implicit node per syntax node.
                argument.Add(operation.Syntax, operation);
            }
        }
    }
}
