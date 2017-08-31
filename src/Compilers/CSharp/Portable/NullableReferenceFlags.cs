// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE(NullableReferenceTypes): Configuration for prototype only.
    [Flags]
    internal enum NullableReferenceFlags
    {
        None = 0,
        IncludeNonNullWarnings = 0x1,
        InferLocalNullability = 0x2,
        AllowMemberOptOut = 0x4,
        AllowAssemblyOptOut = 0x8,
        Enabled = 0x1000,
    }

    internal static class NullableReferenceFlagsExtensions
    {
        internal static NullableReferenceFlags GetNullableReferenceFlags(this CSharpParseOptions options)
        {
            if ((object)options != null && options.IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking))
            {
                if (options.Features.TryGetValue("staticNullChecking", out var value) &&
                    int.TryParse(value, out var flags))
                {
                    return NullableReferenceFlags.Enabled | (NullableReferenceFlags)flags;
                }
                return NullableReferenceFlags.Enabled;
            }
            return NullableReferenceFlags.None;
        }

        internal static NullableReferenceFlags GetNullableReferenceFlags(this CSharpCompilation compilation)
        {
            return ((CSharpParseOptions)compilation.SyntaxTrees.FirstOrDefault()?.Options).GetNullableReferenceFlags();
        }
    }
}
