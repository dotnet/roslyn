
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmanagedCallersOnlyAttributeData
    {
        internal static readonly UnmanagedCallersOnlyAttributeData Uninitialized = new UnmanagedCallersOnlyAttributeData(callingConventionTypes: ImmutableHashSet<INamedTypeSymbolInternal>.Empty, isValid: false);

        public readonly ImmutableHashSet<INamedTypeSymbolInternal> CallingConventionTypes;

        public bool IsValid { get; }

        public UnmanagedCallersOnlyAttributeData(ImmutableHashSet<INamedTypeSymbolInternal> callingConventionTypes, bool isValid)
        {
            CallingConventionTypes = callingConventionTypes;
            IsValid = isValid;
        }
    }
}
