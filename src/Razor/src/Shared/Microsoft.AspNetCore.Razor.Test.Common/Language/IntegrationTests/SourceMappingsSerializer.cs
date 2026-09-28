// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public static class SourceMappingsSerializer
{
    internal static string Serialize(RazorCSharpDocument csharpDocument, RazorSourceDocument sourceDocument)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var sourceMapping in csharpDocument.SourceMappingsSortedByGenerated)
        {
            builder.Append("Source Location: ");
            var sourceCode = GetCodeForSpan(sourceMapping.OriginalSpan, sourceDocument.Text);
            AppendMappingLocation(builder, sourceMapping.OriginalSpan, sourceCode);

            builder.Append("Generated Location: ");
            var generatedCode = GetCodeForSpan(sourceMapping.GeneratedSpan, csharpDocument.Text);
            AppendMappingLocation(builder, sourceMapping.GeneratedSpan, generatedCode);

            Assert.Equal(sourceCode, generatedCode, ignoreCase: true);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendMappingLocation(StringBuilder builder, SourceSpan location, string content)
    {
        builder
            .AppendLine(location.ToString())
            .Append('|')
            .Append(content)
            .AppendLine("|");
    }

    private static string GetCodeForSpan(SourceSpan location, SourceText content)
    {
        return content.ToString(location.AsTextSpan());
    }
}
