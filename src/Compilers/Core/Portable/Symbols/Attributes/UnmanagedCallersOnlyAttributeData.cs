
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmanagedCallersOnlyAttributeData
    {
        internal static readonly UnmanagedCallersOnlyAttributeData Uninitialized = new UnmanagedCallersOnlyAttributeData(callingConventionTypes: default, isValid: false);

        public readonly ImmutableHashSet<INamedTypeSymbolInternal>? CallingConventionTypes;

#pragma warning disable 8775 // Invariant is verified in the constructor
        [MemberNotNullWhen(true, nameof(CallingConventionTypes))]
        public bool IsValid { get; }
#pragma warning restore 8775

        public UnmanagedCallersOnlyAttributeData(ImmutableHashSet<INamedTypeSymbolInternal>? callingConventionTypes, bool isValid)
        {
            Debug.Assert(callingConventionTypes is not null || !isValid);
            CallingConventionTypes = callingConventionTypes;
            IsValid = isValid;
        }
    }
}
