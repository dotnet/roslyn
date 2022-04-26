// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
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
#if CODE_STYLE
    /// <summary>
    /// Empty type to avoid excessive ifdefs.
    /// </summary>
    internal readonly struct CodeActionOptions
    {
    }
#else
    /// <summary>
    /// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
    /// </summary>
    [DataContract]
    internal readonly record struct CodeActionOptions
    {
        [DataMember(Order = 0)] public SymbolSearchOptions SearchOptions { get; init; }
        [DataMember(Order = 1)] public ImplementTypeOptions ImplementTypeOptions { get; init; }
        [DataMember(Order = 2)] public ExtractMethodOptions ExtractMethodOptions { get; init; }
        [DataMember(Order = 3)] public CodeCleanupOptions? CleanupOptions { get; init; }
        [DataMember(Order = 4)] public bool HideAdvancedMembers { get; init; }
        [DataMember(Order = 5)] public bool IsBlocking { get; init; }
        [DataMember(Order = 6)] public int WrappingColumn { get; init; }

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
            bool HideAdvancedMembers = false,
            bool IsBlocking = false,
            int WrappingColumn = DefaultWrappingColumn)
        {
            this.SearchOptions = SearchOptions ?? SymbolSearchOptions.Default;
            this.ImplementTypeOptions = ImplementTypeOptions ?? ImplementType.ImplementTypeOptions.Default;
            this.ExtractMethodOptions = ExtractMethodOptions ?? ExtractMethod.ExtractMethodOptions.Default;
            this.CleanupOptions = CleanupOptions;
            this.HideAdvancedMembers = HideAdvancedMembers;
            this.IsBlocking = IsBlocking;
            this.WrappingColumn = WrappingColumn;
        }

        public CodeActionOptions()
            : this(SearchOptions: null)
        {
        }

        public static readonly CodeActionOptions Default = new();
    }
#endif

    internal delegate CodeActionOptions CodeActionOptionsProvider(HostLanguageServices languageService);

    internal static class CodeActionOptionsProviders
    {
        internal static CodeActionOptionsProvider GetOptionsProvider(this CodeFixContext context)
#if CODE_STYLE
            => _ => default;
#else
            => context.Options;
#endif

        internal static CodeActionOptionsProvider GetOptionsProvider(this FixAllContext context)
#if CODE_STYLE
            => _ => default;
#else
            => context.State.CodeActionOptionsProvider;
#endif

        internal static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, ISyntaxFormatting syntaxFormatting, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxFormatting.GetFormattingOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
            var fallbackFormattingOptions = fallbackOptions(document.Project.GetExtendedLanguageServices()).CleanupOptions?.FormattingOptions;
            return await document.GetSyntaxFormattingOptionsAsync(fallbackFormattingOptions, cancellationToken).ConfigureAwait(false);
#endif
        }

        internal static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, ISimplification simplification, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return simplification.GetSimplifierOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
            var fallbackFormattingOptions = fallbackOptions(document.Project.GetExtendedLanguageServices()).CleanupOptions?.SimplifierOptions;
            return await document.GetSimplifierOptionsAsync(fallbackFormattingOptions, cancellationToken).ConfigureAwait(false);
#endif
        }
    }
}
