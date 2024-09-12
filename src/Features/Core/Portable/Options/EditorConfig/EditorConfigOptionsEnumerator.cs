// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;

namespace Microsoft.CodeAnalysis.Options;

[Export(typeof(EditorConfigOptionsEnumerator)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorConfigOptionsEnumerator(
    [ImportMany] IEnumerable<Lazy<IEditorConfigOptionsEnumerator, LanguageMetadata>> optionEnumerators)
{
    public IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetOptions(string language, bool includeUnsupported = false)
        => optionEnumerators
            .Where(e => e.Metadata.Language == language)
            .SelectMany(e => e.Value.GetOptions(includeUnsupported));

    internal static IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetLanguageAgnosticEditorConfigOptions(bool includeUnsupported)
    {
        yield return (WorkspacesResources.Core_EditorConfig_Options, FormattingOptions2.EditorConfigOptions);

        if (includeUnsupported)
        {
            // note: the feature string is ignored for unsupported options:
            yield return ("unsupported", FormattingOptions2.UndocumentedOptions);
            yield return ("unsupported", JsonDetectionOptionsStorage.UnsupportedOptions);
            yield return ("unsupported", FormatStringValidationOptionStorage.UnsupportedOptions);
            yield return ("unsupported", RegexOptionsStorage.UnsupportedOptions);
            yield return ("unsupported", SymbolSearchOptionsStorage.UnsupportedOptions);
        }

        yield return (FeaturesResources.NET_Code_Actions,
        [
            .. ImplementTypeOptionsStorage.EditorConfigOptions,
            .. MemberDisplayOptionsStorage.EditorConfigOptions,
            .. SymbolSearchOptionsStorage.EditorConfigOptions,
        ]);

        yield return (WorkspacesResources.dot_NET_Coding_Conventions,
        [
            .. GenerationOptions.EditorConfigOptions,
            .. CodeStyleOptions2.EditorConfigOptions
        ]);
    }
}
