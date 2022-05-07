// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

internal static class PEUtilities
{
    internal static void DeriveUseSiteInfoFromCompilerFeatureRequiredAttributes(ref UseSiteInfo<AssemblySymbol> result, Symbol symbol, PEModuleSymbol module, EntityHandle handle, CompilerFeatureRequiredFeatures allowedFeatures, MetadataDecoder decoder)
    {
        string? disallowedFeature = module.Module.GetFirstUnsupportedCompilerFeatureFromToken(handle, decoder, allowedFeatures);
        if (disallowedFeature != null)
        {
            // '{0}' requires compiler feature '{1}', which is not supported by this version of the C# compiler.
            result = result.AdjustDiagnosticInfo(new CSDiagnosticInfo(ErrorCode.ERR_UnsupportedCompilerFeature, symbol, disallowedFeature));
        }
    }
}
