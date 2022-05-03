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

    Unknown = 1 << 31
}

internal static class CompilerFeatureRequiredHelpers
{
    internal static string? GetUnsupportedCompilerFeature(EntityHandle token, PEModule containingModule, CompilerFeatureRequiredFeatures allowedFeatures)
    {
        Debug.Assert(!token.IsNil);
        Debug.Assert((allowedFeatures & CompilerFeatureRequiredFeatures.Unknown) == 0);

        var compilerRequiredFeatures = containingModule.GetCompilerFeatureRequiredFeatures(token);

        if (tryGetFirstUnsupportedFeature(compilerRequiredFeatures, out var unsupportedFeature))
        {
            return unsupportedFeature;
        }

        // Check the containing module and assembly as well, if the symbol itself was fine
        compilerRequiredFeatures = containingModule.GetCompilerFeatureRequiredFeatures(EntityHandle.ModuleDefinition);
        if (tryGetFirstUnsupportedFeature(compilerRequiredFeatures, out unsupportedFeature))
        {
            return unsupportedFeature;
        }

        compilerRequiredFeatures = containingModule.GetCompilerFeatureRequiredFeatures(EntityHandle.AssemblyDefinition);
        if (tryGetFirstUnsupportedFeature(compilerRequiredFeatures, out unsupportedFeature))
        {
            return unsupportedFeature;
        }

        // No unsupported features found
        return null;

        bool tryGetFirstUnsupportedFeature(ImmutableArray<string> features, [NotNullWhen(true)] out string? unsupportedFeature)
        {
            foreach (var feature in features)
            {
                if ((allowedFeatures & getFeatureKind(feature)) == 0)
                {
                    unsupportedFeature = feature;
                    return true;
                }
            }

            unsupportedFeature = null;
            return false;
        }


        static CompilerFeatureRequiredFeatures getFeatureKind(string feature)
            => feature switch
            {
                nameof(CompilerFeatureRequiredFeatures.RefStructs) => CompilerFeatureRequiredFeatures.RefStructs,
                nameof(CompilerFeatureRequiredFeatures.RequiredMembers) => CompilerFeatureRequiredFeatures.RequiredMembers,
                _ => CompilerFeatureRequiredFeatures.Unknown,
            };
    }
}
