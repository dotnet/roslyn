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

    public static DocumentationCommentOptions GetDocumentationCommentOptions(this IGlobalOptionService globalOptions, LineFormattingOptions lineFormatting, string language)
      => new()
      {
          LineFormatting = lineFormatting,
          AutoXmlDocCommentGeneration = globalOptions.GetOption(AutoXmlDocCommentGeneration, language),
      };

    public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(
        "dotnet_auto_xml_doc_comment_generation", DocumentationCommentOptions.Default.AutoXmlDocCommentGeneration);

}
