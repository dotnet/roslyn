// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// <see cref="AnalyzerConfigOptions"/> that memoize structured (parsed) form of certain complex options to avoid parsing them multiple times.
/// Storages of these complex options may directly call the specialized getters to reuse the cached values.
/// </summary>
internal sealed class StructuredAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly AnalyzerConfigOptions _options;
    private readonly Lazy<NamingStylePreferences> _lazyNamingStylePreferences;

    public StructuredAnalyzerConfigOptions(AnalyzerConfigOptions options)
    {
        _options = options;
        _lazyNamingStylePreferences = new Lazy<NamingStylePreferences>(() => EditorConfigNamingStyleParser.ParseDictionary(_options));
    }

    public StructuredAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        : this(new DictionaryAnalyzerConfigOptions(options))
    {
    }

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        => _options.TryGetValue(key, out value);

    public override IEnumerable<string> Keys
        => _options.Keys;

    public NamingStylePreferences GetNamingStylePreferences()
        => _lazyNamingStylePreferences.Value;

    public static bool TryGetStructuredOptions(AnalyzerConfigOptions configOptions, [NotNullWhen(true)] out StructuredAnalyzerConfigOptions? options)
    {
        if (configOptions is StructuredAnalyzerConfigOptions structuredOptions)
        {
            options = structuredOptions;
            return true;
        }

#if CODE_STYLE
        if (TryGetCorrespondingCodeStyleInstance(configOptions, out options))
        {
            return true;
        }
#endif

        options = null;
        return false;
    }

#if CODE_STYLE
    // StructuredAnalyzerConfigOptions is defined in both Worksapce and Code Style layers. It is not public and thus can't be shared between these two.
    // However, Code Style layer is compiled against the shared Workspace APIs. The ProjectState creates and holds onto an instance
    // of Workspace layer's version of StructuredAnalyzerConfigOptions. This version of the type is not directly usable by Code Style code.
    // We create a clone of this instance typed to the Code Style's version of StructuredAnalyzerConfigOptions.
    // The conditional weak table maintains 1:1 correspondence between these instances.
    // 
    // In addition, we also map Compiler created DictionaryAnalyzerConfigOptions to StructuredAnalyzerConfigOptions for analyzers that are invoked
    // from command line build.

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, StructuredAnalyzerConfigOptions> s_codeStyleStructuredOptions = new();
    private static readonly object s_codeStyleStructuredOptionsLock = new();

    private static bool TryGetCorrespondingCodeStyleInstance(AnalyzerConfigOptions configOptions, [NotNullWhen(true)] out StructuredAnalyzerConfigOptions? options)
    {
        if (s_codeStyleStructuredOptions.TryGetValue(configOptions, out options))
        {
            return true;
        }

        lock (s_codeStyleStructuredOptionsLock)
        {
            if (!s_codeStyleStructuredOptions.TryGetValue(configOptions, out options))
            {
                options = new StructuredAnalyzerConfigOptions(configOptions);
                s_codeStyleStructuredOptions.Add(configOptions, options);
            }
        }

        return true;
    }
#endif
}
