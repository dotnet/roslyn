// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct CallingConventionInfo
    {
        internal readonly Cci.CallingConvention CallKind;
        internal readonly ImmutableHashSet<CustomModifier>? UnmanagedCallingConventionTypes;

        public CallingConventionInfo(Cci.CallingConvention callKind, ImmutableHashSet<CustomModifier> unmanagedCallingConventionTypes)
        {
            Debug.Assert(unmanagedCallingConventionTypes.IsEmpty || callKind == Cci.CallingConvention.Unmanaged);
            CallKind = callKind;
            UnmanagedCallingConventionTypes = unmanagedCallingConventionTypes;
        }
    }
}
