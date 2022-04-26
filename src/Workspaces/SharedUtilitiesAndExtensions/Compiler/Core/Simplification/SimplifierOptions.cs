﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeActions;

#if !CODE_STYLE
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract class SimplifierOptions
    {
        public static readonly CodeStyleOption2<bool> DefaultQualifyAccess = CodeStyleOption2<bool>.Default;
        public static readonly CodeStyleOption2<bool> DefaultPreferPredefinedTypeKeyword = new(value: true, notification: NotificationOption2.Silent);

        [DataMember(Order = 0)]
        public readonly CodeStyleOption2<bool> QualifyFieldAccess;

        [DataMember(Order = 1)]
        public readonly CodeStyleOption2<bool> QualifyPropertyAccess;

        [DataMember(Order = 2)]
        public readonly CodeStyleOption2<bool> QualifyMethodAccess;

        [DataMember(Order = 3)]
        public readonly CodeStyleOption2<bool> QualifyEventAccess;

        [DataMember(Order = 4)]
        public readonly CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess;

        [DataMember(Order = 5)]
        public readonly CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration;

        protected const int BaseMemberCount = 6;

        protected SimplifierOptions(
            CodeStyleOption2<bool>? qualifyFieldAccess,
            CodeStyleOption2<bool>? qualifyPropertyAccess,
            CodeStyleOption2<bool>? qualifyMethodAccess,
            CodeStyleOption2<bool>? qualifyEventAccess,
            CodeStyleOption2<bool>? preferPredefinedTypeKeywordInMemberAccess,
            CodeStyleOption2<bool>? preferPredefinedTypeKeywordInDeclaration)
        {
            QualifyFieldAccess = qualifyFieldAccess ?? DefaultQualifyAccess;
            QualifyPropertyAccess = qualifyPropertyAccess ?? DefaultQualifyAccess;
            QualifyMethodAccess = qualifyMethodAccess ?? DefaultQualifyAccess;
            QualifyEventAccess = qualifyEventAccess ?? DefaultQualifyAccess;
            PreferPredefinedTypeKeywordInMemberAccess = preferPredefinedTypeKeywordInMemberAccess ?? DefaultPreferPredefinedTypeKeyword;
            PreferPredefinedTypeKeywordInDeclaration = preferPredefinedTypeKeywordInDeclaration ?? DefaultPreferPredefinedTypeKeyword;
        }

        public CodeStyleOption2<bool> QualifyMemberAccess(SymbolKind symbolKind)
            => symbolKind switch
            {
                SymbolKind.Field => QualifyFieldAccess,
                SymbolKind.Property => QualifyPropertyAccess,
                SymbolKind.Method => QualifyMethodAccess,
                SymbolKind.Event => QualifyEventAccess,
                _ => throw ExceptionUtilities.UnexpectedValue(symbolKind),
            };

#if !CODE_STYLE
        public static SimplifierOptions GetDefault(HostLanguageServices languageServices)
            => languageServices.GetRequiredService<ISimplificationService>().DefaultOptions;

        public static SimplifierOptions Create(OptionSet options, HostWorkspaceServices services, SimplifierOptions? fallbackOptions, string language)
        {
            var simplificationService = services.GetRequiredLanguageService<ISimplificationService>(language);
            var configOptions = options.AsAnalyzerConfigOptions(services.GetRequiredService<IOptionService>(), language);
            return simplificationService.GetSimplifierOptions(configOptions, fallbackOptions);
        }
#endif
    }

#if !CODE_STYLE
    internal static class SimplifierOptionsProviders
    {
        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, SimplifierOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return SimplifierOptions.Create(documentOptions, document.Project.Solution.Workspace.Services, fallbackOptions, document.Project.Language);
        }

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await GetSimplifierOptionsAsync(document, fallbackOptionsProvider(document.Project.LanguageServices).CleanupOptions?.SimplifierOptions ?? SimplifierOptions.GetDefault(document.Project.LanguageServices), cancellationToken).ConfigureAwait(false);

        public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, CodeCleanupOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
            => await GetSimplifierOptionsAsync(document, (await fallbackOptionsProvider(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false)).SimplifierOptions, cancellationToken).ConfigureAwait(false);

    }
#endif
}
