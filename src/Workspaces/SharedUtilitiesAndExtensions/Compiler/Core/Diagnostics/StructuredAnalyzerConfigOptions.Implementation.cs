// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

#if !CODE_STYLE
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class StructuredAnalyzerConfigOptions
{
    internal sealed class Implementation : StructuredAnalyzerConfigOptions
    {
        private readonly AnalyzerConfigOptions _options;
        private readonly Lazy<NamingStylePreferences> _lazyNamingStylePreferences;

#if !CODE_STYLE
        private CodeGenerationOptions? _csharpCodeGenerationOptions;
        private CodeGenerationOptions? _visualBasicCodeGenerationOptions;
#endif

        public Implementation(AnalyzerConfigOptions options)
        {
            _options = options;
            _lazyNamingStylePreferences = new Lazy<NamingStylePreferences>(() => EditorConfigNamingStyleParser.ParseDictionary(_options));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _options.TryGetValue(key, out value);

        public override IEnumerable<string> Keys
            => _options.Keys;

        public override NamingStylePreferences GetNamingStylePreferences()
            => _lazyNamingStylePreferences.Value;

#if !CODE_STYLE
        public override CodeGenerationOptions GetCodeGenerationOptions(LanguageServices languageServices, CodeGenerationOptions? fallbackOptions)
        {
            ref var options =
                ref languageServices.Language == LanguageNames.CSharp ? ref _csharpCodeGenerationOptions :
                ref languageServices.Language == LanguageNames.VisualBasic ? ref _visualBasicCodeGenerationOptions : ref Unsafe.NullRef<CodeGenerationOptions?>();

            Contract.ThrowIfNull(options);

            options ??= ((IOptionsReader)this).GetCodeGenerationOptions(languageServices, fallbackOptions);
            return options;
        }
#endif
    }
}
