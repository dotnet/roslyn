// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal static class DocumentationCommentOptionsStorage
{
    public static async ValueTask<DocumentationCommentOptions> GetDocumentationCommentOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
    {
        var lineFormattingOptions = await document.GetLineFormattingOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(false);
        return new()
        {
            LineFormatting = lineFormattingOptions,
            AutoXmlDocCommentGeneration = globalOptions.GetOption(AutoXmlDocCommentGeneration, document.Project.Language),
        };
    }

    public static DocumentationCommentOptions GetDocumentationCommentOptions(this IGlobalOptionService globalOptions, LineFormattingOptions lineFormatting, string language)
      => new()
      {
          LineFormatting = lineFormatting,
          AutoXmlDocCommentGeneration = globalOptions.GetOption(AutoXmlDocCommentGeneration, language),
      };

    public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(
        "DocumentationCommentOptions", "AutoXmlDocCommentGeneration", DocumentationCommentOptions.Default.AutoXmlDocCommentGeneration,
        storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.AutoComment" : "TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation"));

}
