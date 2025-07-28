// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class AnalyzerReferenceExtensions
{
    extension(AnalyzerReference analyzerFileReference)
    {
        public bool HasAnalyzersOrSourceGenerators(string language)
        => !analyzerFileReference.GetAnalyzers(language).IsDefaultOrEmpty ||
           !analyzerFileReference.GetGenerators(language).IsDefaultOrEmpty;
    }
}
