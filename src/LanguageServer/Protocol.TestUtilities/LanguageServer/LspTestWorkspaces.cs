// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;

internal static class LspTestWorkspaces
{
    public static LspWorkspaceContent SimpleProject
        => LspWorkspaceContent.Empty
            .WithFile("Project.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Library</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """)
            .WithLoadPath("Project.csproj")
            .WithRestore();

    public static LspWorkspaceContent CreateConsoleApplication(
        string projectName,
        int sharedDocumentCount = 0,
        int uniqueDocumentCount = 0,
        int declarationsPerDocument = 1,
        string? uniqueDocumentPrefix = null)
    {
        var content = LspWorkspaceContent.Empty
            .WithFile($"{projectName}.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """)
            .WithFile("Program.cs", $$"""
                Console.WriteLine("Hello from {{projectName}}");
                """)
            .WithLoadPath($"{projectName}.csproj")
            .WithRestore();

        for (var i = 0; i < sharedDocumentCount; i++)
            content = content.WithFile($"Shared{i:D4}.cs", CreateDocument("SharedWorkspace0", i, declarationsPerDocument));

        for (var i = 0; i < uniqueDocumentCount; i++)
            content = content.WithFile($"Unique{i:D4}.cs", CreateDocument(uniqueDocumentPrefix ?? projectName, i, declarationsPerDocument));

        return content;
    }

    private static string CreateDocument(string prefix, int documentIndex, int declarationsPerDocument)
    {
        var builder = new StringBuilder();
        builder.AppendLine("namespace BenchmarkDocuments;");

        for (var declarationIndex = 0; declarationIndex < declarationsPerDocument; declarationIndex++)
        {
            builder.AppendLine($$"""
                internal sealed class {{prefix}}Document{{documentIndex}}Type{{declarationIndex}}
                {
                    public int Value { get; } = {{declarationIndex}};
                    public string GetDescription() => "{{prefix}}-{{documentIndex}}-{{declarationIndex}}";
                }
                """);
        }

        return builder.ToString();
    }
}
