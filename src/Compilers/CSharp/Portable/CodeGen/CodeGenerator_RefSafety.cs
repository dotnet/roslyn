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
            parameters: node.Method.Parameters,
            arguments: node.Arguments,
            argsToParamsOpt: node.ArgsToParamsOpt,
            expanded: node.Expanded);
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
            parameters: node.Constructor.Parameters,
            arguments: node.Arguments,
            argsToParamsOpt: node.ArgsToParamsOpt,
            expanded: node.Expanded);
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
            parameters: method.Parameters,
            arguments: node.Arguments,
            argsToParamsOpt: default,
            expanded: false);
    }

    private static bool MightEscapeTemporaryRefs(
        bool used,
        TypeSymbol returnType,
        RefKind returnRefKind,
        TypeSymbol? receiverType,
        ScopedKind? receiverScope,
        AddressKind? receiverAddressKind,
        bool isReceiverReadOnly,
        ImmutableArray<ParameterSymbol> parameters,
        ImmutableArray<BoundExpression> arguments,
        ImmutableArray<int> argsToParamsOpt,
        bool expanded)
    {
        Debug.Assert(receiverAddressKind is null || receiverType is not null);

        int writableRefs = 0;
        int readonlyRefs = 0;

        if (used && (returnRefKind != RefKind.None || returnType.IsRefLikeOrAllowsRefLikeType()))
        {
            writableRefs++;
        }

        if (receiverType is not null)
        {
            receiverScope ??= ScopedKind.None;
            if (receiverAddressKind is { } a && !IsAnyReadOnly(a) && receiverScope == ScopedKind.None)
            {
                writableRefs++;
            }
            else if (receiverType.IsRefLikeOrAllowsRefLikeType() && receiverScope != ScopedKind.ScopedValue)
            {
                if (isReceiverReadOnly || receiverType.IsReadOnly)
                {
                    readonlyRefs++;
                }
                else
                {
                    writableRefs++;
                }
            }
            else if (receiverAddressKind != null && receiverScope == ScopedKind.None)
            {
                readonlyRefs++;
            }
        }

        if (shouldReturnTrue(writableRefs, readonlyRefs))
        {
            return true;
        }

        for (var arg = 0; arg < arguments.Length; arg++)
        {
            var parameter = Binder.GetCorrespondingParameter(
                arg,
                parameters,
                argsToParamsOpt,
                expanded);

            if (parameter is not null)
            {
                if (parameter.RefKind.IsWritableReference() && parameter.EffectiveScope == ScopedKind.None)
                {
                    writableRefs++;
                }
                else if (parameter.Type.IsRefLikeOrAllowsRefLikeType() && parameter.EffectiveScope != ScopedKind.ScopedValue)
                {
                    if (parameter.Type.IsReadOnly || !parameter.RefKind.IsWritableReference())
                    {
                        readonlyRefs++;
                    }
                    else
                    {
                        writableRefs++;
                    }
                }
                else if (parameter.RefKind != RefKind.None && parameter.EffectiveScope == ScopedKind.None)
                {
                    readonlyRefs++;
                }
            }

            if (shouldReturnTrue(writableRefs, readonlyRefs))
            {
                return true;
            }
        }

        return false;

        static bool shouldReturnTrue(int writableRefs, int readonlyRefs)
        {
            return writableRefs > 0 && (writableRefs + readonlyRefs) > 1;
        }
    }
}
