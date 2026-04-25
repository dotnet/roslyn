// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorParserOptions
{
    public sealed class Builder
    {
        public RazorLanguageVersion LanguageVersion { get; }
        public RazorFileKind FileKind { get; }

        private Flags _flags;

        public ImmutableArray<DirectiveDescriptor> Directives { get; set => field = value.NullToEmpty(); }
        public CSharpParseOptions CSharpParseOptions { get; set => field = value ?? CSharpParseOptions.Default; }

        internal Builder(RazorLanguageVersion languageVersion, RazorFileKind fileKind)
        {
            LanguageVersion = languageVersion ?? DefaultLanguageVersion;
            FileKind = fileKind;
            Directives = [];
            CSharpParseOptions = CSharpParseOptions.Default;
            _flags = GetDefaultFlags(LanguageVersion, FileKind);
        }

        public bool DesignTime
        {
            get => _flags.IsFlagSet(Flags.DesignTime);
            set => _flags.UpdateFlag(Flags.DesignTime, value);
        }

        public bool ParseLeadingDirectives
        {
            get => _flags.IsFlagSet(Flags.ParseLeadingDirectives);
            set => _flags.UpdateFlag(Flags.ParseLeadingDirectives, value);
        }

        public bool UseRoslynTokenizer
        {
            get => _flags.IsFlagSet(Flags.UseRoslynTokenizer);
            set => _flags.UpdateFlag(Flags.UseRoslynTokenizer, value);
        }

        internal bool EnableSpanEditHandlers
        {
            get => _flags.IsFlagSet(Flags.EnableSpanEditHandlers);
            set => _flags.UpdateFlag(Flags.EnableSpanEditHandlers, value);
        }

        internal bool AllowMinimizedBooleanTagHelperAttributes
        {
            get => _flags.IsFlagSet(Flags.AllowMinimizedBooleanTagHelperAttributes);
            set => _flags.UpdateFlag(Flags.AllowMinimizedBooleanTagHelperAttributes, value);
        }

        internal bool AllowHtmlCommentsInTagHelpers
        {
            get => _flags.IsFlagSet(Flags.AllowHtmlCommentsInTagHelpers);
            set => _flags.UpdateFlag(Flags.AllowHtmlCommentsInTagHelpers, value);
        }

        internal bool AllowComponentFileKind
        {
            get => _flags.IsFlagSet(Flags.AllowComponentFileKind);
            set => _flags.UpdateFlag(Flags.AllowComponentFileKind, value);
        }

        internal bool AllowRazorInAllCodeBlocks
        {
            get => _flags.IsFlagSet(Flags.AllowRazorInAllCodeBlocks);
            set => _flags.UpdateFlag(Flags.AllowRazorInAllCodeBlocks, value);
        }

        internal bool AllowUsingVariableDeclarations
        {
            get => _flags.IsFlagSet(Flags.AllowUsingVariableDeclarations);
            set => _flags.UpdateFlag(Flags.AllowUsingVariableDeclarations, value);
        }

        internal bool AllowConditionalDataDashAttributes
        {
            get => _flags.IsFlagSet(Flags.AllowConditionalDataDashAttributes);
            set => _flags.UpdateFlag(Flags.AllowConditionalDataDashAttributes, value);
        }

        internal bool AllowCSharpInMarkupAttributeArea
        {
            get => _flags.IsFlagSet(Flags.AllowCSharpInMarkupAttributeArea);
            set => _flags.UpdateFlag(Flags.AllowCSharpInMarkupAttributeArea, value);
        }

        internal bool AllowNullableForgivenessOperator
        {
            get => _flags.IsFlagSet(Flags.AllowNullableForgivenessOperator);
            set => _flags.UpdateFlag(Flags.AllowNullableForgivenessOperator, value);
        }

        public RazorParserOptions ToOptions()
            => new(LanguageVersion, FileKind, Directives, CSharpParseOptions, _flags);
    }
}
