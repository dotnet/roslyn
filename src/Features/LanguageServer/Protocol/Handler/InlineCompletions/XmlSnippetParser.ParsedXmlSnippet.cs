// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

internal partial class XmlSnippetParser
{
    internal class ParsedXmlSnippet
    {
        public ImmutableArray<SnippetPart> Parts { get; }
        public string DefaultText { get; }

        public ParsedXmlSnippet(ImmutableArray<SnippetPart> parts)
        {
            Parts = parts;

            using var _ = PooledStringBuilder.GetInstance(out var defaultSnippetBuilder);
            foreach (var part in parts)
            {
                var textToAdd = part.DefaultText;
                defaultSnippetBuilder.Append(textToAdd);
            }

            DefaultText = defaultSnippetBuilder.ToString();
        }
    }

    internal abstract record SnippetPart(string DefaultText);

    internal record SnippetFieldPart(string FieldName, string DefaultText, int? EditIndex) : SnippetPart(DefaultText);

    internal record SnippetFunctionPart(string FieldName, string DefaultText, int? EditIndex, string FunctionName, string? FunctionParam)
        : SnippetFieldPart(FieldName, DefaultText, EditIndex)
    {
        public async Task<SnippetFunctionPart> WithSnippetFunctionResultAsync(Document documentWithSnippet, TextSpan fieldSpan, CancellationToken cancellationToken)
        {
            var snippetFunctionService = documentWithSnippet.Project.GetRequiredLanguageService<SnippetFunctionService>();
            switch (FunctionName)
            {
                case "SimpleTypeName":
                    if (FunctionParam == null)
                    {
                        return this;
                    }

                    var simplifiedTypeName = await SnippetFunctionService.GetSimplifiedTypeNameAsync(documentWithSnippet, fieldSpan, FunctionParam, cancellationToken).ConfigureAwait(false);
                    if (simplifiedTypeName == null)
                    {
                        return this;
                    }

                    return this with { DefaultText = simplifiedTypeName };
                case "ClassName":
                    var className = await snippetFunctionService.GetContainingClassNameAsync(documentWithSnippet, fieldSpan.Start, cancellationToken).ConfigureAwait(false);
                    if (className == null)
                    {
                        return this;
                    }

                    return this with { DefaultText = className };
                case "GenerateSwitchCases":
                    // Generate switch cases requires a multi-step snippet interaction, where the snippet is inserted first then the
                    // client calls back to on commit so that we can generate the cases for the specified switch value.
                    // This is not yet supported via LSP.
                    return this;
                default:
                    return this;
            }
        }
    }

    /// <summary>
    /// To indicate cursor location we put in a multi-line comment so that we can
    /// find it after formatting.
    /// </summary>
    internal record SnippetCursorPart() : SnippetPart("/*$0*/");

    internal record SnippetStringPart(string Text) : SnippetPart(Text);
}
