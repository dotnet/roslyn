
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmanagedCallersOnlyAttributeData
    {
        internal static readonly UnmanagedCallersOnlyAttributeData Uninitialized = new UnmanagedCallersOnlyAttributeData(callingConventionTypes: ImmutableHashSet<INamedTypeSymbolInternal>.Empty, isValid: false);
        internal static readonly UnmanagedCallersOnlyAttributeData AttributePresentDataNotBound = new UnmanagedCallersOnlyAttributeData(callingConventionTypes: ImmutableHashSet<INamedTypeSymbolInternal>.Empty, isValid: false);
        private static readonly UnmanagedCallersOnlyAttributeData PlatformDefault = new UnmanagedCallersOnlyAttributeData(callingConventionTypes: ImmutableHashSet<INamedTypeSymbolInternal>.Empty, isValid: true);

        internal static UnmanagedCallersOnlyAttributeData Create(ImmutableHashSet<INamedTypeSymbolInternal>? callingConventionTypes, bool isValid)
            => (callingConventionTypes, isValid) switch
            {
                (null, true) or ({ IsEmpty: true }, true) => PlatformDefault,
                _ => new UnmanagedCallersOnlyAttributeData(callingConventionTypes ?? ImmutableHashSet<INamedTypeSymbolInternal>.Empty, isValid)
            };


        public readonly ImmutableHashSet<INamedTypeSymbolInternal> CallingConventionTypes;
        public readonly bool IsValid;

        private UnmanagedCallersOnlyAttributeData(ImmutableHashSet<INamedTypeSymbolInternal> callingConventionTypes, bool isValid)
        {
            CallingConventionTypes = callingConventionTypes;
            IsValid = isValid;
        }

        internal static bool IsCallConvsTypedConstant(string key, in TypedConstant value)
        {
            return key == "CallConvs"
                   && value.Kind == TypedConstantKind.Array
                   && value.Values.All(v => v.Kind == TypedConstantKind.Type);
        }
    }
}
