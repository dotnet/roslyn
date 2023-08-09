// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    [ExportEmbeddedLanguageQuickInfoProvider("Regex", new[] { LanguageNames.CSharp }, new[] { "Regex", "Regexp" }), Shared]
    internal class RegexQuickInfoProvider : IEmbeddedLanguageQuickInfoProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RegexQuickInfoProvider()
        {
        }

        public QuickInfoItem? GetQuickInfo(QuickInfoContext context, SemanticModel semanticModel, SyntaxToken token)
        {
            //string url = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference";
            //return GetQuickInfo(context, semanticModel, token);
            var item = QuickInfoItem.Create(token.Span, sections: new[]
                {
                    QuickInfoSection.Create(QuickInfoSectionKinds.Description, new[]
                    {
                        new TaggedText(TextTags.Space, ""),
                        new TaggedText(TextTags.Text,"This is a Regular Expression."),
                        new TaggedText(TextTags.LineBreak, " "),
                        new TaggedText(TextTags.Text,FeaturesResources.View_regular_expression_Documentation ,TaggedTextStyle.None, "https://aka.ms/RegexQuickRef","https://aka.ms/RegexQuickRef"),
                    }.ToImmutableArray())
                }.ToImmutableArray());
            return item;
        }
    }
}
