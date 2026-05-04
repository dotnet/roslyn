// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal static class DocumentationCommentOptionsStorage
{

    public static DocumentationCommentOptions GetDocumentationCommentOptions(this IGlobalOptionService globalOptions, LineFormattingOptions lineFormatting, string language)
      => new()
      {
          LineFormatting = lineFormatting,
          AutoXmlDocCommentGeneration = globalOptions.GetOption(AutoXmlDocCommentGeneration, language),
          GenerateSummaryTagOnSingleLine = globalOptions.GetOption(GenerateSummaryTagOnSingleLine, language),
          GenerateOnlySummaryTag = globalOptions.GetOption(GenerateOnlySummaryTag, language),
      };

    public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(
        "dotnet_auto_xml_doc_comment_generation", DocumentationCommentOptions.Default.AutoXmlDocCommentGeneration);

    public static readonly PerLanguageOption2<bool> GenerateSummaryTagOnSingleLine = new(
        "dotnet_generate_summary_tag_on_single_line", DocumentationCommentOptions.Default.GenerateSummaryTagOnSingleLine);

    public static readonly PerLanguageOption2<bool> GenerateOnlySummaryTag = new(
        "dotnet_generate_only_summary_tag", DocumentationCommentOptions.Default.GenerateOnlySummaryTag);

}
