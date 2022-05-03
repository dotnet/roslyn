// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal static class DocumentationCommentOptionsStorage
{
    public static DocumentationCommentOptions GetDocumentationCommentOptions(this IGlobalOptionService globalOptions, DocumentOptionSet documentOptions)
      => new(
          AutoXmlDocCommentGeneration: globalOptions.GetOption(AutoXmlDocCommentGeneration, documentOptions.Language),
          TabSize: documentOptions.GetOption(FormattingOptions.TabSize),
          UseTabs: documentOptions.GetOption(FormattingOptions.UseTabs),
          NewLine: documentOptions.GetOption(FormattingOptions.NewLine));

    public static DocumentationCommentOptions GetDocumentationCommentOptions(this IGlobalOptionService globalOptions, SyntaxFormattingOptions formattingOptions, string language)
      => new(
          AutoXmlDocCommentGeneration: globalOptions.GetOption(AutoXmlDocCommentGeneration, language),
          TabSize: formattingOptions.TabSize,
          UseTabs: formattingOptions.UseTabs,
          NewLine: formattingOptions.NewLine);

    public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(
        "DocumentationCommentOptions", "AutoXmlDocCommentGeneration", defaultValue: true,
        storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.AutoComment" : "TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation"));

}
