// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract class SimplifierOptions
    {
        public readonly CodeStyleOption2<bool> QualifyFieldAccess;
        public readonly CodeStyleOption2<bool> QualifyPropertyAccess;
        public readonly CodeStyleOption2<bool> QualifyMethodAccess;
        public readonly CodeStyleOption2<bool> QualifyEventAccess;
        public readonly CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess;
        public readonly CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration;

        protected SimplifierOptions(
            CodeStyleOption2<bool> qualifyFieldAccess,
            CodeStyleOption2<bool> qualifyPropertyAccess,
            CodeStyleOption2<bool> qualifyMethodAccess,
            CodeStyleOption2<bool> qualifyEventAccess,
            CodeStyleOption2<bool> preferPredefinedTypeKeywordInMemberAccess,
            CodeStyleOption2<bool> preferPredefinedTypeKeywordInDeclaration)
        {
            QualifyFieldAccess = qualifyFieldAccess;
            QualifyPropertyAccess = qualifyPropertyAccess;
            QualifyMethodAccess = qualifyMethodAccess;
            QualifyEventAccess = qualifyEventAccess;
            PreferPredefinedTypeKeywordInMemberAccess = preferPredefinedTypeKeywordInMemberAccess;
            PreferPredefinedTypeKeywordInDeclaration = preferPredefinedTypeKeywordInDeclaration;
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
        public static SimplifierOptions Create(OptionSet options, HostWorkspaceServices services, string language)
        {
            var simplificationService = services.GetRequiredLanguageService<ISimplificationService>(language);
            var configOptions = options.AsAnalyzerConfigOptions(services.GetRequiredService<IOptionService>(), language);
            return simplificationService.GetSimplifierOptions(configOptions);
        }

        public static async Task<SimplifierOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
        }
#endif
    }
}
