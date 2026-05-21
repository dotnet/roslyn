// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorParserOptions
    : IEquatable<RazorParserOptions>
{
    private static RazorLanguageVersion DefaultLanguageVersion => RazorLanguageVersion.Latest;
    private static RazorFileKind DefaultFileKind => RazorFileKind.Legacy;

    public static RazorParserOptions Default { get; } = new(
        languageVersion: DefaultLanguageVersion,
        fileKind: DefaultFileKind,
        directives: [],
        csharpParseOptions: CSharpParseOptions.Default,
        flags: GetDefaultFlags(DefaultLanguageVersion, DefaultFileKind));

    public RazorLanguageVersion LanguageVersion { get; }
    internal RazorFileKind FileKind { get; }

    public ImmutableArray<DirectiveDescriptor> Directives { get; }
    public CSharpParseOptions CSharpParseOptions { get; }

    private readonly Flags _flags;

    private RazorParserOptions(
        RazorLanguageVersion languageVersion,
        RazorFileKind fileKind,
        ImmutableArray<DirectiveDescriptor> directives,
        CSharpParseOptions csharpParseOptions,
        Flags flags)
    {
        if (flags.IsFlagSet(Flags.ParseLeadingDirectives) &&
            flags.IsFlagSet(Flags.UseRoslynTokenizer))
        {
            throw new ArgumentException($"Cannot set {nameof(Flags.ParseLeadingDirectives)} and {nameof(Flags.UseRoslynTokenizer)} to true simultaneously.");
        }

        LanguageVersion = languageVersion ?? DefaultLanguageVersion;
        FileKind = fileKind;
        Directives = directives;
        CSharpParseOptions = csharpParseOptions;
        _flags = flags;
    }

    public static RazorParserOptions Create(RazorLanguageVersion languageVersion, RazorFileKind fileKind, Action<Builder>? configure = null)
    {
        var builder = new Builder(languageVersion, fileKind);
        configure?.Invoke(builder);

        return builder.ToOptions();
    }

    /// <summary>
    /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
    /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
    /// </summary>
    /// <remarks>
    /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
    /// In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public bool ParseLeadingDirectives
        => _flags.IsFlagSet(Flags.ParseLeadingDirectives);

    public bool UseRoslynTokenizer
        => _flags.IsFlagSet(Flags.UseRoslynTokenizer);

    internal bool EnableSpanEditHandlers
        => _flags.IsFlagSet(Flags.EnableSpanEditHandlers);

    internal bool AllowMinimizedBooleanTagHelperAttributes
        => _flags.IsFlagSet(Flags.AllowMinimizedBooleanTagHelperAttributes);

    internal bool AllowHtmlCommentsInTagHelpers
        => _flags.IsFlagSet(Flags.AllowHtmlCommentsInTagHelpers);

    internal bool AllowComponentFileKind
        => _flags.IsFlagSet(Flags.AllowComponentFileKind);

    internal bool AllowRazorInAllCodeBlocks
        => _flags.IsFlagSet(Flags.AllowRazorInAllCodeBlocks);

    internal bool AllowUsingVariableDeclarations
        => _flags.IsFlagSet(Flags.AllowUsingVariableDeclarations);

    internal bool AllowConditionalDataDashAttributes
        => _flags.IsFlagSet(Flags.AllowConditionalDataDashAttributes);

    internal bool AllowCSharpInMarkupAttributeArea
        => _flags.IsFlagSet(Flags.AllowCSharpInMarkupAttributeArea);

    internal bool AllowNullableForgivenessOperator
        => _flags.IsFlagSet(Flags.AllowNullableForgivenessOperator);

    public RazorParserOptions WithDirectives(params ImmutableArray<DirectiveDescriptor> value)
        => Directives.SequenceEqual(value)
            ? this
            : new(LanguageVersion, FileKind, value, CSharpParseOptions, _flags);

    public RazorParserOptions WithCSharpParseOptions(CSharpParseOptions value)
        => CSharpParseOptions.Equals(value)
            ? this
            : new(LanguageVersion, FileKind, Directives, value, _flags);

    public RazorParserOptions WithFlags(
        bool? parseLeadingDirectives = default,
        bool? useRoslynTokenizer = default,
        bool? enableSpanEditHandlers = default,
        bool? allowMinimizedBooleanTagHelperAttributes = default,
        bool? allowHtmlCommentsInTagHelpers = default,
        bool? allowComponentFileKind = default,
        bool? allowRazorInAllCodeBlocks = default,
        bool? allowUsingVariableDeclarations = default,
        bool? allowConditionalDataDashAttributes = default,
        bool? allowCSharpInMarkupAttributeArea = default,
        bool? allowNullableForgivenessOperator = default)
    {
        var flags = _flags;

        if (parseLeadingDirectives is bool parseLeadingDirectivesValue)
        {
            flags.UpdateFlag(Flags.ParseLeadingDirectives, parseLeadingDirectivesValue);
        }

        if (useRoslynTokenizer is bool useRoslynTokenizerValue)
        {
            flags.UpdateFlag(Flags.UseRoslynTokenizer, useRoslynTokenizerValue);
        }

        if (enableSpanEditHandlers is bool enableSpanEditHandlersValue)
        {
            flags.UpdateFlag(Flags.EnableSpanEditHandlers, enableSpanEditHandlersValue);
        }

        if (allowMinimizedBooleanTagHelperAttributes is bool allowMinimizedBooleanTagHelperAttributesValue)
        {
            flags.UpdateFlag(Flags.AllowMinimizedBooleanTagHelperAttributes, allowMinimizedBooleanTagHelperAttributesValue);
        }

        if (allowHtmlCommentsInTagHelpers is bool allowHtmlCommentsInTagHelpersValue)
        {
            flags.UpdateFlag(Flags.AllowHtmlCommentsInTagHelpers, allowHtmlCommentsInTagHelpersValue);
        }

        if (allowComponentFileKind is bool allowComponentFileKindValue)
        {
            flags.UpdateFlag(Flags.AllowComponentFileKind, allowComponentFileKindValue);
        }

        if (allowRazorInAllCodeBlocks is bool allowRazorInAllCodeBlocksValue)
        {
            flags.UpdateFlag(Flags.AllowRazorInAllCodeBlocks, allowRazorInAllCodeBlocksValue);
        }

        if (allowUsingVariableDeclarations is bool allowUsingVariableDeclarationsValue)
        {
            flags.UpdateFlag(Flags.AllowUsingVariableDeclarations, allowUsingVariableDeclarationsValue);
        }

        if (allowConditionalDataDashAttributes is bool allowConditionalDataDashAttributesValue)
        {
            flags.UpdateFlag(Flags.AllowConditionalDataDashAttributes, allowConditionalDataDashAttributesValue);
        }

        if (allowCSharpInMarkupAttributeArea is bool allowCSharpInMarkupAttributeAreaValue)
        {
            flags.UpdateFlag(Flags.AllowCSharpInMarkupAttributeArea, allowCSharpInMarkupAttributeAreaValue);
        }

        if (allowNullableForgivenessOperator is bool allowNullableForgivenessOperatorValue)
        {
            flags.UpdateFlag(Flags.AllowNullableForgivenessOperator, allowNullableForgivenessOperatorValue);
        }

        return flags == _flags
            ? this
            : new(LanguageVersion, FileKind, Directives, CSharpParseOptions, flags);
    }

    public bool Equals(RazorParserOptions? other)
    {
        return
            other is not null
            && _flags == other._flags
            && FileKind == other.FileKind
            && LanguageVersion == other.LanguageVersion
            && CSharpParseOptions == other.CSharpParseOptions
            && Directives.SequenceEqual(other.Directives, ReferenceEqualityComparer<DirectiveDescriptor>.Instance);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RazorParserOptions);
    }

    public override int GetHashCode()
    {
        var combiner = HashCodeCombiner.Start();
        combiner.Add(_flags);
        combiner.Add(FileKind);
        combiner.Add(LanguageVersion);
        combiner.Add(CSharpParseOptions);
        combiner.Add(Directives, ReferenceEqualityComparer<DirectiveDescriptor>.Instance);

        return combiner.CombinedHash;
    }
}
