// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class RazorCodeDocumentFactory
{
    private const string CSHtmlFile = "test.cshtml";
    private const string RazorFile = "test.razor";

    public static string GetFileName(bool isRazorFile)
        => isRazorFile ? RazorFile : CSHtmlFile;

    public static RazorCodeDocument CreateCodeDocument(string text, bool isRazorFile, params TagHelperCollection tagHelpers)
    {
        return CreateCodeDocument(text, GetFileName(isRazorFile), tagHelpers);
    }

    public static RazorCodeDocument CreateCodeDocument(string text, string filePath, params TagHelperCollection tagHelpers)
    {
        tagHelpers ??= [];

        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureCodeGenerationOptions(builder =>
            {
                builder.UseEnhancedLinePragma = true;
            });

            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });

            RazorExtensions.Register(builder);
        });

        var fileKind = FileKinds.GetFileKindFromPath(filePath);

        return projectEngine.Process(sourceDocument, fileKind, importSources: default, tagHelpers);
    }
}
