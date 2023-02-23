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
#if CODE_STYLE
    internal
#else
    public
#endif
    interface ISimplifierOptions
    {
        bool QualifyFieldAccess { get; }
        bool QualifyPropertyAccess { get; }
        bool QualifyMethodAccess { get; }
        bool QualifyEventAccess { get; }
        bool PreferPredefinedTypeKeywordInMemberAccess { get; }
        bool PreferPredefinedTypeKeywordInDeclaration { get; }
    }

#if CODE_STYLE
    internal
#else
    public
#endif
    record class SimplifierOptions : ISimplifierOptions
    {
        internal static readonly SimplifierOptions CommonDefaults = new();

        [DataMember] public bool QualifyFieldAccess { get; init; } = SimplifierStyleOptions.CommonDefaults.QualifyFieldAccess.Value;
        [DataMember] public bool QualifyPropertyAccess { get; init; } = SimplifierStyleOptions.CommonDefaults.QualifyPropertyAccess.Value;
        [DataMember] public bool QualifyMethodAccess { get; init; } = SimplifierStyleOptions.CommonDefaults.QualifyMethodAccess.Value;
        [DataMember] public bool QualifyEventAccess { get; init; } = SimplifierStyleOptions.CommonDefaults.QualifyEventAccess.Value;
        [DataMember] public bool PreferPredefinedTypeKeywordInMemberAccess { get; init; } = SimplifierStyleOptions.CommonDefaults.PreferPredefinedTypeKeywordInMemberAccess.Value;
        [DataMember] public bool PreferPredefinedTypeKeywordInDeclaration { get; init; } = SimplifierStyleOptions.CommonDefaults.PreferPredefinedTypeKeywordInDeclaration.Value;

        private protected SimplifierOptions()
        {
        }
    }

    internal record class SimplifierStyleOptions : ISimplifierOptions
    {
        public static readonly CodeStyleOption2<bool> DefaultQualifyAccess = CodeStyleOption2.FalseWithSilentEnforcement;

        /// <summary>
        /// Language agnostic defaults.
        /// </summary>
        internal static readonly SimplifierStyleOptions CommonDefaults = new();

        [DataMember] public CodeStyleOption2<bool> QualifyFieldAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyPropertyAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyMethodAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> QualifyEventAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
        [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;

        bool ISimplifierOptions.QualifyFieldAccess => QualifyFieldAccess.Value;
        bool ISimplifierOptions.QualifyPropertyAccess => QualifyPropertyAccess.Value;
        bool ISimplifierOptions.QualifyMethodAccess => QualifyMethodAccess.Value;
        bool ISimplifierOptions.QualifyEventAccess => QualifyEventAccess.Value;
        bool ISimplifierOptions.PreferPredefinedTypeKeywordInMemberAccess => PreferPredefinedTypeKeywordInMemberAccess.Value;
        bool ISimplifierOptions.PreferPredefinedTypeKeywordInDeclaration => PreferPredefinedTypeKeywordInDeclaration.Value;

        private protected SimplifierStyleOptions()
        {
        }

        private protected SimplifierStyleOptions(IOptionsReader options, SimplifierStyleOptions fallbackOptions, string language)
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
        public static SimplifierStyleOptions GetDefault(LanguageServices languageServices)
            => languageServices.GetRequiredService<ISimplificationService>().DefaultOptions;
#endif
    }

    internal interface SimplifierOptionsProvider
#if !CODE_STYLE
        : OptionsProvider<SimplifierStyleOptions>
#endif
    {
    }

    internal static partial class Extensions
    {
        public static bool? TryGetQualifyMemberAccessOption(this ISimplifierOptions options, SymbolKind symbolKind)
            => symbolKind switch
            {
                SymbolKind.Field => options.QualifyFieldAccess,
                SymbolKind.Property => options.QualifyPropertyAccess,
                SymbolKind.Method => options.QualifyMethodAccess,
                SymbolKind.Event => options.QualifyEventAccess,
                _ => null,
            };

#if !CODE_STYLE
        public static SimplifierStyleOptions GetSimplifierOptions(this IOptionsReader options, SimplifierStyleOptions? fallbackOptions, LanguageServices languageServices)
            => languageServices.GetRequiredService<ISimplificationService>().GetSimplifierOptions(options, fallbackOptions);

        public static async ValueTask<SimplifierStyleOptions> GetSimplifierOptionsAsync(this Document document, SimplifierStyleOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetSimplifierOptions(fallbackOptions, document.Project.Services);
        }

        public static async ValueTask<SimplifierStyleOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await document.GetSimplifierOptionsAsync(await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
    }
}
