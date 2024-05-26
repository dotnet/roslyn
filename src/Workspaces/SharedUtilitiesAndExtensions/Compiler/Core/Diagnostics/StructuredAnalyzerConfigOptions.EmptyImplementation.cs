// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class StructuredAnalyzerConfigOptions
{
    private sealed class EmptyImplementation : StructuredAnalyzerConfigOptions
    {
        public override NamingStylePreferences GetNamingStylePreferences()
            => NamingStylePreferences.Empty;

#if !CODE_STYLE
        public override CodeGenerationOptions GetCodeGenerationOptions(LanguageServices languageServices, CodeGenerationOptions? fallbackOptions)
            => CodeGenerationOptions.CommonDefaults;
#endif

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }

        public override IEnumerable<string> Keys
            => [];
    }
}
