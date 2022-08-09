// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;

#if !CODE_STYLE
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeGeneration;
#endif

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract class SimplifierOptions
    {
        public static readonly CodeStyleOption2<bool> DefaultQualifyAccess = CodeStyleOption2<bool>.Default;
        public static readonly CodeStyleOption2<bool> DefaultPreferPredefinedTypeKeyword = new(value: true, notification: NotificationOption2.Silent);

        [DataContract]
        internal sealed record class CommonOptions
        {
            public static readonly CommonOptions Default = new();

            [DataMember] public CodeStyleOption2<bool> QualifyFieldAccess { get; init; } = DefaultQualifyAccess;
            [DataMember] public CodeStyleOption2<bool> QualifyPropertyAccess { get; init; } = DefaultQualifyAccess;
            [DataMember] public CodeStyleOption2<bool> QualifyMethodAccess { get; init; } = DefaultQualifyAccess;
            [DataMember] public CodeStyleOption2<bool> QualifyEventAccess { get; init; } = DefaultQualifyAccess;
            [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess { get; init; } = DefaultPreferPredefinedTypeKeyword;
            [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration { get; init; } = DefaultPreferPredefinedTypeKeyword;
        }

        public CommonOptions Common { get; init; } = CommonOptions.Default;

        public CodeStyleOption2<bool> QualifyFieldAccess => Common.QualifyFieldAccess;
        public CodeStyleOption2<bool> QualifyPropertyAccess => Common.QualifyPropertyAccess;
        public CodeStyleOption2<bool> QualifyMethodAccess => Common.QualifyMethodAccess;
        public CodeStyleOption2<bool> QualifyEventAccess => Common.QualifyEventAccess;
        public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess => Common.PreferPredefinedTypeKeywordInMemberAccess;
        public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration => Common.PreferPredefinedTypeKeywordInDeclaration;

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
        internal static SimplifierOptions.CommonOptions GetCommonSimplifierOptions(this AnalyzerConfigOptions options, SimplifierOptions.CommonOptions? fallbackOptions)
        {
            fallbackOptions ??= SimplifierOptions.CommonOptions.Default;
            return new()
            {
                QualifyFieldAccess = options.GetEditorConfigOption(CodeStyleOptions2.QualifyFieldAccess, fallbackOptions.QualifyFieldAccess),
                QualifyPropertyAccess = options.GetEditorConfigOption(CodeStyleOptions2.QualifyPropertyAccess, fallbackOptions.QualifyPropertyAccess),
                QualifyMethodAccess = options.GetEditorConfigOption(CodeStyleOptions2.QualifyMethodAccess, fallbackOptions.QualifyMethodAccess),
                QualifyEventAccess = options.GetEditorConfigOption(CodeStyleOptions2.QualifyEventAccess, fallbackOptions.QualifyEventAccess),
                PreferPredefinedTypeKeywordInMemberAccess = options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, fallbackOptions.PreferPredefinedTypeKeywordInMemberAccess),
                PreferPredefinedTypeKeywordInDeclaration = options.GetEditorConfigOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, fallbackOptions.PreferPredefinedTypeKeywordInDeclaration),
            };
        }

#if !CODE_STYLE
        public static SimplifierOptions GetSimplifierOptions(this AnalyzerConfigOptions options, SimplifierOptions? fallbackOptions, LanguageServices languageServices)
            => languageServices.GetRequiredService<ISimplificationService>().GetSimplifierOptions(options, fallbackOptions);

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetSimplifierOptions(fallbackOptions, document.Project.Services);
        }

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await document.GetSimplifierOptionsAsync(await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
    }
}
