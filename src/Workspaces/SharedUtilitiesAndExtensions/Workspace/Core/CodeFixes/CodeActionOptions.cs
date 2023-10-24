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
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
    /// </summary>
    [DataContract]
    internal sealed record class CodeActionOptions
    {
#if CODE_STYLE
        public static readonly CodeActionOptionsProvider DefaultProvider = new DelegatingCodeActionOptionsProvider(GetDefault);
#else
        public static readonly CodeActionOptionsProvider DefaultProvider = new DelegatingCodeActionOptionsProvider(static ls => GetDefault(ls));
#endif

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

        public const int DefaultConditionalExpressionWrappingLength = 120;

        public const int DefaultCollectionExpressionWrappingLength = 120;

#if !CODE_STYLE
        [DataMember] public required CodeCleanupOptions CleanupOptions { get; init; }
        [DataMember] public required CodeGenerationOptions CodeGenerationOptions { get; init; }
        [DataMember] public required IdeCodeStyleOptions CodeStyleOptions { get; init; }
        [DataMember] public SymbolSearchOptions SearchOptions { get; init; } = SymbolSearchOptions.Default;
        [DataMember] public ImplementTypeOptions ImplementTypeOptions { get; init; } = ImplementTypeOptions.Default;
        [DataMember] public ExtractMethodOptions ExtractMethodOptions { get; init; } = ExtractMethodOptions.Default;
        [DataMember] public bool HideAdvancedMembers { get; init; } = false;
        [DataMember] public int WrappingColumn { get; init; } = DefaultWrappingColumn;
        [DataMember] public int ConditionalExpressionWrappingLength { get; init; } = DefaultConditionalExpressionWrappingLength;
        [DataMember] public int CollectionExpressionWrappingLength { get; init; } = DefaultCollectionExpressionWrappingLength;

        public static CodeActionOptions GetDefault(LanguageServices languageServices)
            => new()
            {
                CleanupOptions = CodeCleanupOptions.GetDefault(languageServices),
                CodeGenerationOptions = CodeGenerationOptions.GetDefault(languageServices),
                CodeStyleOptions = IdeCodeStyleOptions.GetDefault(languageServices)
            };
#else
        public static CodeActionOptions GetDefault(LanguageServices languageServices)
            => new();
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
        OrganizeImportsOptionsProvider,
#endif
        SyntaxFormattingOptionsProvider,
        SimplifierOptionsProvider,
        AddImportPlacementOptionsProvider
    {
        CodeActionOptions GetOptions(LanguageServices languageService);
    }

    internal abstract class AbstractCodeActionOptionsProvider : CodeActionOptionsProvider
    {
        public abstract CodeActionOptions GetOptions(LanguageServices languageServices);

#if !CODE_STYLE
        ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.FormattingOptions.LineFormatting);

        ValueTask<DocumentFormattingOptions> OptionsProvider<DocumentFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.DocumentFormattingOptions);

        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.FormattingOptions);

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.SimplifierOptions);

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.AddImportOptions);

        ValueTask<OrganizeImportsOptions> OptionsProvider<OrganizeImportsOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions.GetOrganizeImportsOptions());

        ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CleanupOptions);

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CodeGenerationOptions);

        ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetOptions(languageServices).CodeGenerationOptions.NamingStyle);

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        {
            var codeActionOptions = GetOptions(languageServices);
            return ValueTaskFactory.FromResult(new CleanCodeGenerationOptions()
            {
                GenerationOptions = codeActionOptions.CodeGenerationOptions,
                CleanupOptions = codeActionOptions.CleanupOptions
            });
        }

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        {
            var codeActionOptions = GetOptions(languageServices);
            return ValueTaskFactory.FromResult(new CodeAndImportGenerationOptions()
            {
                GenerationOptions = codeActionOptions.CodeGenerationOptions,
                AddImportOptions = codeActionOptions.CleanupOptions.AddImportOptions
            });
        }
#endif
    }

    internal sealed class DelegatingCodeActionOptionsProvider(Func<LanguageServices, CodeActionOptions> @delegate) : AbstractCodeActionOptionsProvider
    {
        public override CodeActionOptions GetOptions(LanguageServices languageService)
            => @delegate(languageService);
    }

    internal static class CodeActionOptionsProviders
    {
        internal static CodeActionOptionsProvider GetOptionsProvider(this CodeFixContext context)
#if CODE_STYLE
            => CodeActionOptions.DefaultProvider;
#else
            => context.Options;
#endif

#if CODE_STYLE
        internal static CodeActionOptionsProvider GetOptionsProvider(this FixAllContext _)
            => CodeActionOptions.DefaultProvider;
#else
        internal static CodeActionOptionsProvider GetOptionsProvider(this IFixAllContext context)
            => context.State.CodeActionOptionsProvider;
#endif

#if !CODE_STYLE
        public static ImplementTypeGenerationOptions GetImplementTypeGenerationOptions(this CodeActionOptionsProvider provider, LanguageServices languageServices)
            => new(provider.GetOptions(languageServices).ImplementTypeOptions, provider);

        public static ExtractMethodGenerationOptions GetExtractMethodGenerationOptions(this CodeActionOptionsProvider provider, LanguageServices languageServices)
        {
            var codeActionOptions = provider.GetOptions(languageServices);
            return new()
            {
                CodeGenerationOptions = codeActionOptions.CodeGenerationOptions,
                ExtractOptions = codeActionOptions.ExtractMethodOptions,
                AddImportOptions = codeActionOptions.CleanupOptions.AddImportOptions,
                LineFormattingOptions = codeActionOptions.CleanupOptions.FormattingOptions.LineFormatting
            };
        }
#endif
    }
}
