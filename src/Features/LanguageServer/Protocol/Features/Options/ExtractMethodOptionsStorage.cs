// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal static class ExtractMethodOptionsStorage
{
    public static ExtractMethodOptions GetExtractMethodOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            DontPutOutOrRefOnStruct: globalOptions.GetOption(DontPutOutOrRefOnStruct, language));

    public static ExtractMethodGenerationOptions GetExtractMethodGenerationOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => new(globalOptions.GetExtractMethodOptions(languageServices.Language),
               globalOptions.GetCodeGenerationOptions(languageServices),
               globalOptions.GetAddImportPlacementOptions(languageServices),
               globalOptions.GetNamingStylePreferencesProvider());

    public static ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetExtractMethodGenerationOptionsAsync(globalOptions.GetExtractMethodGenerationOptions(document.Project.LanguageServices), cancellationToken);

    public static readonly PerLanguageOption2<bool> DontPutOutOrRefOnStruct = new(
        "ExtractMethodOptions", "DontPutOutOrRefOnStruct", ExtractMethodOptions.Default.DontPutOutOrRefOnStruct,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")); // NOTE: the spelling error is what we've shipped and thus should not change
}
