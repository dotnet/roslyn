// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis;

[Flags]
internal enum CompilerFeatureRequiredFeatures
{
    None = 0,
    RefStructs = 1 << 0,
    RequiredMembers = 1 << 1,
}

/// <summary>
/// Simple string wrapper for ensuring 
/// </summary>
internal sealed class UnsupportedCompilerFeature
{
    public static readonly UnsupportedCompilerFeature Sentinel = new UnsupportedCompilerFeature("");
    private static readonly UnsupportedCompilerFeature None = new UnsupportedCompilerFeature(null);

    public readonly string? FeatureName;

    public static UnsupportedCompilerFeature Create(string? featureName)
    {
        if (featureName is null)
        {
            return None;
        }

        return new UnsupportedCompilerFeature(featureName);
    }

    private UnsupportedCompilerFeature(string? featureName)
    {
        FeatureName = featureName;
    }
}
