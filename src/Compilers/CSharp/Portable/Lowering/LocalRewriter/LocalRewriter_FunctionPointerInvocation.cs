// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            var rewrittenExpression = VisitExpression(node.InvokedExpression);
            Debug.Assert(rewrittenExpression != null);

            // There are target types so we can have handler conversions, but there are no attributes so contexts cannot
            // be involved.
            AssertNoImplicitInterpolatedStringHandlerConversions(node.Arguments, allowConversionsWithNoContext: true);
            MethodSymbol functionPointer = node.FunctionPointer.Signature;
            var argumentRefKindsOpt = node.ArgumentRefKindsOpt;
            BoundExpression? discardedReceiver = null;
            ArrayBuilder<LocalSymbol>? temps = null;
            var rewrittenArgs = VisitArgumentsAndCaptureReceiverIfNeeded(
                rewrittenReceiver: ref discardedReceiver,
                forceReceiverCapturing: false,
                node.Arguments,
                functionPointer,
                argsToParamsOpt: default,
                argumentRefKindsOpt: argumentRefKindsOpt,
                storesOpt: null,
                ref temps);

            Debug.Assert(discardedReceiver is null);

            if (node.InterceptableNameSyntax is { } nameSyntax && this._compilation.TryGetInterceptor(nameSyntax) is var (attributeLocation, _))
            {
                this._diagnostics.Add(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, attributeLocation, nameSyntax.Identifier.ValueText);
            }

            rewrittenArgs = MakeArguments(
                rewrittenArgs,
                functionPointer,
                expanded: false,
                argsToParamsOpt: default,
                ref argumentRefKindsOpt,
                ref temps,
                invokedAsExtensionMethod: false);

            BoundExpression rewrittenInvocation = node.Update(rewrittenExpression, rewrittenArgs, argumentRefKindsOpt, node.ResultKind, node.Type);

            if (temps.Count == 0)
            {
                temps.Free();
            }
            else
            {
                rewrittenInvocation = new BoundSequence(rewrittenInvocation.Syntax, temps.ToImmutableAndFree(), sideEffects: ImmutableArray<BoundExpression>.Empty, rewrittenInvocation, node.Type);
            }

            if (Instrument)
            {
                rewrittenInvocation = Instrumenter.InstrumentFunctionPointerInvocation(node, rewrittenInvocation);
            }

            return rewrittenInvocation;
        }
    }
}
