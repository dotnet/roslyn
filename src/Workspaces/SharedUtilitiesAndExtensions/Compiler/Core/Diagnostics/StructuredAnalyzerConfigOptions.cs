// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// <see cref="AnalyzerConfigOptions"/> that memoize structured (parsed) form of certain complex options to avoid parsing them multiple times.
/// Storages of these complex options may directly call the specialized getters to reuse the cached values.
/// </summary>
internal abstract partial class StructuredAnalyzerConfigOptions : AnalyzerConfigOptions, IOptionsReader
{
    public static readonly StructuredAnalyzerConfigOptions Empty = new EmptyImplementation();

    public abstract NamingStylePreferences GetNamingStylePreferences();

#if !CODE_STYLE
    public abstract CodeGenerationOptions GetCodeGenerationOptions(LanguageServices languageServices, CodeGenerationOptions? fallbackOptions);
#endif

    public static StructuredAnalyzerConfigOptions Create(ImmutableDictionary<string, string> options)
    {
        Contract.ThrowIfFalse(options.KeyComparer == KeyComparer);
        return new Implementation(new DictionaryAnalyzerConfigOptions(options));
    }

    public static StructuredAnalyzerConfigOptions Create(AnalyzerConfigOptions options)
        => new Implementation(options);

    public bool TryGetOption<T>(OptionKey2 optionKey, out T value)
        => this.TryGetEditorConfigOption(optionKey.Option, out value);

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
    // StructuredAnalyzerConfigOptions is defined in both Workspace and Code Style layers. It is not public and thus can't be shared between these two.
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
                options = new Implementation(configOptions);
                s_codeStyleStructuredOptions.Add(configOptions, options);
            }
        }

        return true;
    }
#endif
}
