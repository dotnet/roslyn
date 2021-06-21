// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            var rewrittenExpression = VisitExpression(node.InvokedExpression);

            // There are target types so we can have handler conversions, but there are no attributes so contexts cannot
            // be involved.
            AssertNoImplicitInterpolatedStringHandlerConversions(node.Arguments, allowConversionsWithNoContext: true);

            MethodSymbol functionPointer = node.FunctionPointer.Signature;
            var argumentRefKindsOpt = node.ArgumentRefKindsOpt;
            ImmutableArray<BoundExpression> rewrittenArgs = VisitArguments(
                node.Syntax,
                node.Arguments,
                functionPointer,
                expanded: false,
                argsToParamsOpt: default,
                ref argumentRefKindsOpt,
                out ImmutableArray<LocalSymbol> temps,
                ref rewrittenExpression,
                invokedAsExtensionMethod: false,
                enableCallerInfo: ThreeState.False);

            Debug.Assert(rewrittenExpression != null);
            node = node.Update(rewrittenExpression, rewrittenArgs, argumentRefKindsOpt, node.ResultKind, node.Type);

            if (temps.IsDefaultOrEmpty)
            {
                return node;
            }

            return new BoundSequence(node.Syntax, temps, sideEffects: ImmutableArray<BoundExpression>.Empty, node, node.Type);
        }
    }
}
