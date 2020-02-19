// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            // PROTOTYPE(func-ptr): avoid rewriting if the invoked expression is an addressof that loads
            // the pointer

            var visitedInvokedExpression = VisitExpression(node.InvokedExpression);
            var visitedArguments = VisitList(node.Arguments);

            var stores = ArrayBuilder<BoundExpression>.GetInstance();
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();

            switch (visitedInvokedExpression)
            {
                case BoundLocal _:
                case BoundParameter _:
                case BoundPseudoVariable _:
                case var _ when node.Arguments.IsEmpty:
                    stores.Free();
                    temps.Free();
                    return updateNode(node, visitedInvokedExpression, visitedArguments);

                default:
                    var temp = _factory.StoreToTemp(visitedInvokedExpression, out BoundAssignmentOperator store);
                    stores.Add(store);
                    temps.Add(temp.LocalSymbol);
                    visitedInvokedExpression = temp;

                    return new BoundSequence(
                        node.Syntax,
                        temps.ToImmutableAndFree(),
                        stores.ToImmutableAndFree(),
                        updateNode(node, visitedInvokedExpression, visitedArguments),
                        node.Type); ;
            }

            static BoundFunctionPointerInvocation updateNode(BoundFunctionPointerInvocation node, BoundExpression visitedInvokedExpression, ImmutableArray<BoundExpression> visitedArguments)
            {
                return node.Update(visitedInvokedExpression, node.FunctionPointer, visitedArguments, node.ArgumentRefKindsOpt, node.Type);
            }
        }
    }
}
