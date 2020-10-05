// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            var rewrittenExpression = VisitExpression(node.InvokedExpression);
            var rewrittenArgs = VisitList(node.Arguments);

            MethodSymbol functionPointer = node.FunctionPointer.Signature;
            var argumentRefKindsOpt = node.ArgumentRefKindsOpt;
            rewrittenArgs = MakeArguments(
                node.Syntax,
                rewrittenArgs,
                functionPointer,
                functionPointer,
                expanded: false,
                argsToParamsOpt: default,
                ref argumentRefKindsOpt,
                out ImmutableArray<LocalSymbol> temps,
                invokedAsExtensionMethod: false,
                enableCallerInfo: ThreeState.False);

            node = node.Update(rewrittenExpression, rewrittenArgs, argumentRefKindsOpt, node.ResultKind, node.Type);

            if (temps.IsDefaultOrEmpty)
            {
                return node;
            }

            return new BoundSequence(node.Syntax, temps, sideEffects: ImmutableArray<BoundExpression>.Empty, node, node.Type);
        }
    }
}
