// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.CodeAnalysis.Symbols;
using System;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal static class SynthesizedCollectionHelpers
{
    public static bool IsSynthesizedCollectionType(Type t) =>
        t.Name.StartsWith(CommonGeneratedNames.SynthesizedReadOnlyList_ReadOnlyListPrefix, StringComparison.Ordinal) ||
        t.Name.StartsWith(CommonGeneratedNames.SynthesizedReadOnlyList_ReadOnlyArrayPrefix, StringComparison.Ordinal) ||
        t.Name.StartsWith(CommonGeneratedNames.SynthesizedReadOnlyList_SingleElementPrefix, StringComparison.Ordinal);

    public static DkmClrType? TryGetCollectionDebugViewTypeForCollectionType(DkmClrType collectionType)
    {
        // Mscorlib_CollectionDebugView (internal) was renamed to ICollectionDebugView (public), we'll look for either one.
        const string ICollectionDebugViewName = "System.Collections.Generic.ICollectionDebugView`1";
        const string MscorlibCollectionDebugViewName = "System.Collections.Generic.Mscorlib_CollectionDebugView`1";

        foreach (var module in collectionType.AppDomain.GetClrModuleInstances())
        {
            // Both types are defined in the runtime module
            if (!module.ClrFlags.HasFlag(DkmClrModuleFlags.RuntimeModule))
            {
                continue;
            }

            var proxyType = module.TryResolveTypeName(ICollectionDebugViewName, collectionType.GenericArguments) ??
                            module.TryResolveTypeName(MscorlibCollectionDebugViewName, collectionType.GenericArguments);
            if (proxyType is not null)
            {
                return proxyType;
            }

            break;
        }

        return null;
    }
}
