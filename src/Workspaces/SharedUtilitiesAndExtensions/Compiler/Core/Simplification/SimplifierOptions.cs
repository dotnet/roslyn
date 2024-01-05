// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Simplification
{
    internal record class SimplifierOptions
    {
        public static readonly CodeStyleOption2<bool> DefaultQualifyAccess = CodeStyleOption2.FalseWithSilentEnforcement;

        /// <summary>
        /// Language agnostic defaults.
        /// </summary>
        internal static readonly SimplifierOptions CommonDefaults = new();

        [DataMember] public CodeStyleOption2<bool> QualifyFieldAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyPropertyAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyMethodAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyEventAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;

        private protected SimplifierOptions()
        {
        }

        private protected SimplifierOptions(IOptionsReader options, SimplifierOptions fallbackOptions, string language)
        {
            QualifyFieldAccess = options.GetOption(CodeStyleOptions2.QualifyFieldAccess, language, fallbackOptions.QualifyFieldAccess);
            QualifyPropertyAccess = options.GetOption(CodeStyleOptions2.QualifyPropertyAccess, language, fallbackOptions.QualifyPropertyAccess);
            QualifyMethodAccess = options.GetOption(CodeStyleOptions2.QualifyMethodAccess, language, fallbackOptions.QualifyMethodAccess);
            QualifyEventAccess = options.GetOption(CodeStyleOptions2.QualifyEventAccess, language, fallbackOptions.QualifyEventAccess);
            PreferPredefinedTypeKeywordInMemberAccess = options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, language, fallbackOptions.PreferPredefinedTypeKeywordInMemberAccess);
            PreferPredefinedTypeKeywordInDeclaration = options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language, fallbackOptions.PreferPredefinedTypeKeywordInDeclaration);
        }

        public bool TryGetQualifyMemberAccessOption(SymbolKind symbolKind, [NotNullWhen(true)] out CodeStyleOption2<bool>? option)
        {
            option = symbolKind switch
            {
                SymbolKind.Field => QualifyFieldAccess,
                SymbolKind.Property => QualifyPropertyAccess,
                SymbolKind.Method => QualifyMethodAccess,
                SymbolKind.Event => QualifyEventAccess,
                _ => null,
            };

            return option != null;
        }

#if !CODE_STYLE
        public static SimplifierOptions GetDefault(LanguageServices languageServices)
            => languageServices.GetRequiredService<ISimplificationService>().DefaultOptions;
#endif
    }

    internal interface SimplifierOptionsProvider
#if !CODE_STYLE
        : OptionsProvider<SimplifierOptions>
#endif
    {
    }

    internal static partial class SimplifierOptionsProviders
    {
#if !CODE_STYLE
        public static SimplifierOptions GetSimplifierOptions(this IOptionsReader options, LanguageServices languageServices, SimplifierOptions? fallbackOptions)
            => languageServices.GetService<ISimplificationService>()?.GetSimplifierOptions(options, fallbackOptions) ?? fallbackOptions ?? SimplifierOptions.CommonDefaults;

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetSimplifierOptions(document.Project.Services, fallbackOptions);
        }

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await document.GetSimplifierOptionsAsync(await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
    }
}
