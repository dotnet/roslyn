// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification;

internal interface ISimplifierOptionsStorage : ILanguageService
{
    SimplifierOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class SimplifierOptionsStorage
{
    public static ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetSimplifierOptionsAsync(globalOptions.GetSimplifierOptions(document.Project.Services), cancellationToken);

    public static SimplifierOptions GetSimplifierOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => languageServices.GetRequiredService<ISimplifierOptionsStorage>().GetOptions(globalOptions);

    public static SimplifierOptions.CommonOptions GetCommonSimplifierOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            QualifyFieldAccess = globalOptions.GetOption(CodeStyleOptions2.QualifyFieldAccess, language),
            QualifyPropertyAccess = globalOptions.GetOption(CodeStyleOptions2.QualifyPropertyAccess, language),
            QualifyMethodAccess = globalOptions.GetOption(CodeStyleOptions2.QualifyMethodAccess, language),
            QualifyEventAccess = globalOptions.GetOption(CodeStyleOptions2.QualifyEventAccess, language),
            PreferPredefinedTypeKeywordInMemberAccess = globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, language),
            PreferPredefinedTypeKeywordInDeclaration = globalOptions.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language)
        };
}
