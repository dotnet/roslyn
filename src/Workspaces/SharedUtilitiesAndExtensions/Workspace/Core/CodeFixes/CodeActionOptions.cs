// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
    /// </summary>
    [DataContract]
    internal record class CodeActionOptions
    {
        public static readonly CodeActionOptions Default = new();
        public static readonly CodeActionOptionsProvider DefaultProvider = Default.CreateProvider();

#if !CODE_STYLE
        [DataMember(Order = 0)] public SymbolSearchOptions SearchOptions { get; init; }
        [DataMember(Order = 1)] public ImplementTypeOptions ImplementTypeOptions { get; init; }
        [DataMember(Order = 2)] public ExtractMethodOptions ExtractMethodOptions { get; init; }
        [DataMember(Order = 3)] public CodeCleanupOptions? CleanupOptions { get; init; }
        [DataMember(Order = 4)] public CodeGenerationOptions? CodeGenerationOptions { get; init; }
        [DataMember(Order = 5)] public bool HideAdvancedMembers { get; init; }
        [DataMember(Order = 6)] public bool IsBlocking { get; init; }
        [DataMember(Order = 7)] public int WrappingColumn { get; init; }

        /// <summary>
        /// Default value of 120 was picked based on the amount of code in a github.com diff at 1080p.
        /// That resolution is the most common value as per the last DevDiv survey as well as the latest
        /// Steam hardware survey.  This also seems to a reasonable length default in that shorter
        /// lengths can often feel too cramped for .NET languages, which are often starting with a
        /// default indentation of at least 16 (for namespace, class, member, plus the final construct
        /// indentation).
        /// 
        /// TODO: Currently the option has no storage and always has its default value. See https://github.com/dotnet/roslyn/pull/30422#issuecomment-436118696.
        /// </summary>
        public const int DefaultWrappingColumn = 120;

        public CodeActionOptions(
            SymbolSearchOptions? SearchOptions = null,
            ImplementTypeOptions? ImplementTypeOptions = null,
            ExtractMethodOptions? ExtractMethodOptions = null,
            CodeCleanupOptions? CleanupOptions = null,
            CodeGenerationOptions? CodeGenerationOptions = null,
            bool HideAdvancedMembers = false,
            bool IsBlocking = false,
            int WrappingColumn = DefaultWrappingColumn)
        {
            this.SearchOptions = SearchOptions ?? SymbolSearchOptions.Default;
            this.ImplementTypeOptions = ImplementTypeOptions ?? ImplementType.ImplementTypeOptions.Default;
            this.ExtractMethodOptions = ExtractMethodOptions ?? ExtractMethod.ExtractMethodOptions.Default;
            this.CleanupOptions = CleanupOptions;
            this.CodeGenerationOptions = CodeGenerationOptions;
            this.HideAdvancedMembers = HideAdvancedMembers;
            this.IsBlocking = IsBlocking;
            this.WrappingColumn = WrappingColumn;
        }

        public CodeActionOptions()
            : this(SearchOptions: null)
        {
        }
#endif

        public CodeActionOptionsProvider CreateProvider()
            => new DelegatingCodeActionOptionsProvider(_ => this);
    }

    internal interface CodeActionOptionsProvider :
#if !CODE_STYLE
        CodeCleanupOptionsProvider,
        CodeGenerationOptionsProvider,
        CleanCodeGenerationOptionsProvider,
        CodeAndImportGenerationOptionsProvider,
#endif
        SyntaxFormattingOptionsProvider,
        SimplifierOptionsProvider,
        AddImportPlacementOptionsProvider
    {
        CodeActionOptions GetOptions(HostLanguageServices languageService);
    }

    internal abstract class AbstractCodeActionOptionsProvider : CodeActionOptionsProvider
    {
        public abstract CodeActionOptions GetOptions(HostLanguageServices languageService);

#if !CODE_STYLE
        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions?.FormattingOptions ?? SyntaxFormattingOptions.GetDefault(languageServices));

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions?.SimplifierOptions ?? SimplifierOptions.GetDefault(languageServices));

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions?.AddImportOptions ?? AddImportPlacementOptions.Default);

        ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions ?? CodeCleanupOptions.GetDefault(languageServices));

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CodeGenerationOptions ?? CodeGenerationOptions.GetDefault(languageServices));

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        {
            var codeActionOptions = GetOptions(languageServices);
            return ValueTaskFactory.FromResult(new CleanCodeGenerationOptions(
                codeActionOptions.CodeGenerationOptions ?? CodeGenerationOptions.GetDefault(languageServices),
                codeActionOptions.CleanupOptions ?? CodeCleanupOptions.GetDefault(languageServices)));
        }

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        {
            var codeActionOptions = GetOptions(languageServices);
            return ValueTaskFactory.FromResult(new CodeAndImportGenerationOptions(
                codeActionOptions.CodeGenerationOptions ?? CodeGenerationOptions.GetDefault(languageServices),
                codeActionOptions.CleanupOptions?.AddImportOptions ?? AddImportPlacementOptions.Default));
        }
#endif
    }

    internal sealed class DelegatingCodeActionOptionsProvider : AbstractCodeActionOptionsProvider
    {
        private readonly Func<HostLanguageServices, CodeActionOptions> _delegate;

        public DelegatingCodeActionOptionsProvider(Func<HostLanguageServices, CodeActionOptions> @delegate)
            => _delegate = @delegate;

        public override CodeActionOptions GetOptions(HostLanguageServices languageService)
            => _delegate(languageService);
    }

    internal static class CodeActionOptionsProviders
    {
        internal static CodeActionOptionsProvider GetOptionsProvider(this CodeFixContext context)
#if CODE_STYLE
            => CodeActionOptions.DefaultProvider;
#else
            => context.Options;
#endif

        internal static CodeActionOptionsProvider GetOptionsProvider(this FixAllContext context)
#if CODE_STYLE
            => CodeActionOptions.DefaultProvider;
#else
            => context.State.CodeActionOptionsProvider;
#endif

        internal static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, ISyntaxFormatting syntaxFormatting, SyntaxFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxFormatting.GetFormattingOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
            var fallbackFormattingOptions = await fallbackOptionsProvider.GetOptionsAsync(document.Project.GetExtendedLanguageServices(), cancellationToken).ConfigureAwait(false);
            return await document.GetSyntaxFormattingOptionsAsync(fallbackFormattingOptions, cancellationToken).ConfigureAwait(false);
#endif
        }

        internal static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, ISimplification simplification, SimplifierOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return simplification.GetSimplifierOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
            var fallbackFormattingOptions = await fallbackOptionsProvider.GetOptionsAsync(document.Project.GetExtendedLanguageServices(), cancellationToken).ConfigureAwait(false);
            return await document.GetSimplifierOptionsAsync(fallbackFormattingOptions, cancellationToken).ConfigureAwait(false);
#endif
        }

        internal static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, IAddImportsService addImportsService, CodeActionOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var configOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            return AddImportPlacementOptions.Create(configOptions, addImportsService, allowInHiddenRegions: false, fallbackOptions: AddImportPlacementOptions.Default);
#else
            return await document.GetAddImportPlacementOptionsAsync(fallbackOptionsProvider, cancellationToken).ConfigureAwait(false);
#endif
        }

#if !CODE_STYLE
        public static ImplementTypeGenerationOptions GetImplementTypeGenerationOptions(this CodeActionOptionsProvider provider, HostLanguageServices languageServices)
            => new(provider.GetOptions(languageServices).ImplementTypeOptions, provider);

        public static ExtractMethodGenerationOptions GetExtractMethodGenerationOptions(this CodeActionOptionsProvider provider, HostLanguageServices languageServices)
        {
            var codeActionOptions = provider.GetOptions(languageServices);
            return new(
                codeActionOptions.ExtractMethodOptions,
                codeActionOptions.CodeGenerationOptions ?? CodeGenerationOptions.GetDefault(languageServices),
                codeActionOptions.CleanupOptions?.AddImportOptions ?? AddImportPlacementOptions.Default,
                new NamingStylePreferencesProvider(languageServices => NamingStylePreferences.Default)); // TODO: https://github.com/dotnet/roslyn/issues/60849
        }
#endif
    }
}
