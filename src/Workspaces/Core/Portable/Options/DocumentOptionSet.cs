// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: DocumentOptionSet, Option<T>, PerLanguageOption<T>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// An <see cref="OptionSet"/> that comes from <see cref="Document.GetOptionsAsync(System.Threading.CancellationToken)"/>. It behaves just like a normal
/// <see cref="OptionSet"/> but remembers which language the <see cref="Document"/> is, so you don't have to
/// pass that information redundantly when calling <see cref="GetOption{T}(PerLanguageOption{T})"/>.
/// </summary>
public sealed class DocumentOptionSet : OptionSet
{
    private readonly OptionSet _underlyingOptions;
    private readonly StructuredAnalyzerConfigOptions? _configOptions;

    /// <summary>
    /// Cached internal values read from <see cref="_configOptions"/> or <see cref="_underlyingOptions"/>.
    /// </summary>
    private ImmutableDictionary<OptionKey, object?> _values;

    internal DocumentOptionSet(StructuredAnalyzerConfigOptions? configOptions, OptionSet underlyingOptions, string language)
        : this(configOptions, underlyingOptions, language, ImmutableDictionary<OptionKey, object?>.Empty)
    {
    }

    private DocumentOptionSet(StructuredAnalyzerConfigOptions? configOptions, OptionSet underlyingOptions, string language, ImmutableDictionary<OptionKey, object?> values)
    {
        Language = language;
        _configOptions = configOptions;
        _underlyingOptions = underlyingOptions;
        _values = values;
    }

    internal string Language { get; }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
    internal override object? GetInternalOptionValue(OptionKey optionKey)
    {
        // If we already know the document specific value, we're done
        if (_values.TryGetValue(optionKey, out var value))
        {
            return value;
        }

        if (TryGetAnalyzerConfigOption(optionKey, out value))
        {
            // Cache and return
            return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
        }

        // We don't have a document specific value, so forward
        return _underlyingOptions.GetInternalOptionValue(optionKey);
    }

    private bool TryGetAnalyzerConfigOption(OptionKey optionKey, out object? value)
    {
        if (_configOptions == null)
        {
            value = null;
            return false;
        }

        if (optionKey.Option is not IOption2 internallyDefinedOption)
        {
            value = null;
            return false;
        }

        // Naming style option is not public. We should not call this API internally.
        Contract.ThrowIfTrue(internallyDefinedOption.Type == typeof(NamingStylePreferences));

        if (!_configOptions.TryGetValue(internallyDefinedOption.Definition.ConfigName, out var stringValue))
        {
            value = null;
            return false;
        }

        // The option is in _configOptions so it must have editorconfig storage location:
        return internallyDefinedOption.Definition.Serializer.TryParse(stringValue, out value);
    }

    public T GetOption<T>(PerLanguageOption<T> option)
        => GetOption(option, Language);

    internal override OptionSet WithChangedOptionInternal(OptionKey optionKey, object? internalValue)
        => new DocumentOptionSet(_configOptions, _underlyingOptions, Language, _values.SetItem(optionKey, internalValue));

    /// <summary>
    /// Creates a new <see cref="DocumentOptionSet" /> that contains the changed value.
    /// </summary>
    public DocumentOptionSet WithChangedOption<T>(PerLanguageOption<T> option, T value)
        => (DocumentOptionSet)WithChangedOption(option, Language, value);
}
