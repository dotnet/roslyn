// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;

public abstract class RazorOnAutoInsertProviderTestBase(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    private protected abstract IOnAutoInsertProvider CreateProvider();

    protected void RunAutoInsertTest(
        string input,
        string expected,
        bool enableAutoClosingTags = true,
        RazorFileKind? fileKind = null,
        TagHelperCollection? tagHelpers = null)
    {
        // Arrange
        TestFileMarkupParser.GetPosition(input, out input, out var location);

        var source = SourceText.From(input);
        var position = source.GetPosition(location);

        var path = "file:///path/to/document.razor";
        var uri = new Uri(path);
        var codeDocument = CreateCodeDocument(source, uri.AbsolutePath, tagHelpers, fileKind);

        var provider = CreateProvider();

        // Act
        provider.TryResolveInsertion(position, codeDocument, enableAutoClosingTags: enableAutoClosingTags, out var edit);

        // Assert
        var edited = edit is null ? source : ApplyEdit(source, edit.TextEdit);
        var actual = edited.ToString();
        Assert.Equal(expected, actual);
    }

    private static SourceText ApplyEdit(SourceText source, TextEdit edit)
    {
        var change = source.GetTextChange(edit);
        return source.WithChanges(change);
    }

    private static RazorCodeDocument CreateCodeDocument(
        SourceText text,
        string path,
        TagHelperCollection? tagHelpers,
        RazorFileKind? fileKind = null)
    {
        var fileKindValue = fileKind ?? RazorFileKind.Component;
        tagHelpers ??= [];

        var sourceDocument = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(path, path));
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, fileKindValue, importSources: default, tagHelpers);
    }
}
