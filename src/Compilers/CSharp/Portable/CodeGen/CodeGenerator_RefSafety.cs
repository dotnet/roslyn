// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen;

internal partial class CodeGenerator
{
    private static bool MightEscapeTemporaryRefs(BoundCall node, bool used, AddressKind? receiverAddressKind)
    {
        return MightEscapeTemporaryRefs(
            used: used,
            returnType: node.Type,
            returnRefKind: node.Method.RefKind,
            receiverType: !node.Method.RequiresInstanceReceiver ? null : node.ReceiverOpt?.Type,
            receiverScope: node.Method.TryGetThisParameter(out var thisParameter) ? thisParameter?.EffectiveScope : null,
            receiverAddressKind: receiverAddressKind,
            isReceiverReadOnly: node.Method.IsEffectivelyReadOnly,
            parameters: node.Method.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(BoundObjectCreationExpression node, bool used)
    {
        return MightEscapeTemporaryRefs(
            used: used,
            returnType: node.Type,
            returnRefKind: RefKind.None,
            receiverType: null,
            receiverScope: null,
            receiverAddressKind: null,
            isReceiverReadOnly: false,
            parameters: node.Constructor.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(BoundFunctionPointerInvocation node, bool used)
    {
        FunctionPointerMethodSymbol method = node.FunctionPointer.Signature;
        return MightEscapeTemporaryRefs(
            used: used,
            returnType: node.Type,
            returnRefKind: method.RefKind,
            receiverType: null,
            receiverScope: null,
            receiverAddressKind: null,
            isReceiverReadOnly: false,
            parameters: method.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(
        bool used,
        TypeSymbol returnType,
        RefKind returnRefKind,
        TypeSymbol? receiverType,
        ScopedKind? receiverScope,
        AddressKind? receiverAddressKind,
        bool isReceiverReadOnly,
        ImmutableArray<ParameterSymbol> parameters)
    {
        Debug.Assert(receiverAddressKind is null || receiverType is not null);

        // We check the signature of the method, counting potential `ref` sources and destinations
        // to determine whether a `ref` can be captured by the method.
        // The emit layer then uses this information to avoid reusing temporaries that are passed by ref to such methods.

        // number of outputs that can capture `ref`s
        int refTargets = 0;
        // number of inputs that can contain `ref`s
        int refSources = 0;

        if (used && (returnRefKind != RefKind.None || returnType.IsRefLikeOrAllowsRefLikeType()))
        {
            // If returning by ref or returning a ref struct, the result might capture `ref`s.
            refTargets++;
        }

        if (receiverType is not null)
        {
            receiverScope ??= ScopedKind.None;
            if (receiverType.IsRefLikeOrAllowsRefLikeType() && receiverScope != ScopedKind.ScopedValue)
            {
                refSources++;
                if (!isReceiverReadOnly && !receiverType.IsReadOnly)
                {
                    refTargets++;
                }
            }
            else if (receiverAddressKind != null && receiverScope == ScopedKind.None)
            {
                refSources++;
            }
        }

        if (shouldReturnTrue(refTargets, refSources))
        {
            return true;
        }

        foreach (var parameter in parameters)
        {
            if (parameter.Type.IsRefLikeOrAllowsRefLikeType() && parameter.EffectiveScope != ScopedKind.ScopedValue)
            {
                refSources++;
                if (!parameter.Type.IsReadOnly && parameter.RefKind.IsWritableReference())
                {
                    refTargets++;
                }
            }
            else if (parameter.RefKind != RefKind.None && parameter.EffectiveScope == ScopedKind.None)
            {
                refSources++;
            }

            if (shouldReturnTrue(refTargets, refSources))
            {
                return true;
            }
        }

        return false;

        static bool shouldReturnTrue(int writableRefs, int readableRefs)
        {
            // If there is at least one output and at least one input, a `ref` can be captured.
            return writableRefs > 0 && readableRefs > 0;
        }
    }
}
