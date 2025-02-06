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
            thisParameterSymbol: node.Method.TryGetThisParameter(out var thisParameter) ? thisParameter : null,
            receiverAddressKind: receiverAddressKind,
            parameters: node.Method.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(BoundObjectCreationExpression node, bool used)
    {
        return MightEscapeTemporaryRefs(
            used: used,
            returnType: node.Type,
            returnRefKind: RefKind.None,
            thisParameterSymbol: null,
            receiverAddressKind: null,
            parameters: node.Constructor.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(BoundFunctionPointerInvocation node, bool used)
    {
        FunctionPointerMethodSymbol method = node.FunctionPointer.Signature;
        return MightEscapeTemporaryRefs(
            used: used,
            returnType: node.Type,
            returnRefKind: method.RefKind,
            thisParameterSymbol: null,
            receiverAddressKind: null,
            parameters: method.Parameters);
    }

    private static bool MightEscapeTemporaryRefs(
        bool used,
        TypeSymbol returnType,
        RefKind returnRefKind,
        ParameterSymbol? thisParameterSymbol,
        AddressKind? receiverAddressKind,
        ImmutableArray<ParameterSymbol> parameters)
    {
        Debug.Assert(receiverAddressKind is null || thisParameterSymbol is not null);

        // We check the signature of the method, counting potential `ref` sources and destinations
        // to determine whether a `ref` can be captured by the method.
        // The emit layer then uses this information to avoid reusing temporaries that are passed by ref to such methods.

        // whether we have any outputs that can capture `ref`s
        bool anyRefTargets = false;
        // whether we have any inputs that can contain `ref`s
        bool anyRefSources = false;

        if (used && (returnRefKind != RefKind.None || returnType.IsRefLikeOrAllowsRefLikeType()))
        {
            // If returning by ref or returning a ref struct, the result might capture `ref`s.
            anyRefTargets = true;
        }

        if (thisParameterSymbol is not null && processParameter(thisParameterSymbol, anyRefSources: ref anyRefSources, anyRefTargets: ref anyRefTargets))
        {
            return true;
        }

        foreach (var parameter in parameters)
        {
            if (processParameter(parameter, anyRefSources: ref anyRefSources, anyRefTargets: ref anyRefTargets))
            {
                return true;
            }
        }

        return false;

        // Returns true if we can return 'true' early.
        static bool processParameter(ParameterSymbol parameter, ref bool anyRefSources, ref bool anyRefTargets)
        {
            if (parameter.Type.IsRefLikeOrAllowsRefLikeType() && parameter.EffectiveScope != ScopedKind.ScopedValue)
            {
                anyRefSources = true;
                if (!parameter.Type.IsReadOnly && parameter.RefKind.IsWritableReference())
                {
                    anyRefTargets = true;
                }
            }
            else if (parameter.RefKind != RefKind.None && parameter.EffectiveScope == ScopedKind.None)
            {
                anyRefSources = true;
            }

            // If there is at least one output and at least one input, a `ref` can be captured.
            return anyRefTargets && anyRefSources;
        }
    }
}
