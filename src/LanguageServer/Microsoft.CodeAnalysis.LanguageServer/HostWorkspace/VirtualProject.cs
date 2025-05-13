// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Soon this will be replaced invoke dotnet run-api command implemented in https://github.com/dotnet/sdk/pull/48749
/// </summary>
internal static class VirtualCSharpFileBasedProgramProject
{
    /// <summary>
    /// Adjusts a path to a file-based program for use in passing the virtual project to msbuild.
    /// (msbuild needs the path to end in .csproj to recognize as a C# project and apply all the standard props/targets to it.)
    /// </summary>
    internal static string GetVirtualProjectPath(string documentFilePath)
        => Path.ChangeExtension(documentFilePath, ".csproj");

    internal static (string virtualProjectXml, bool isFileBasedProgram) MakeVirtualProjectContent(string documentFilePath, SourceText text)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(documentFilePath));
        // NB: this is a temporary solution for running our heuristic.
        // When we adopt the dotnet run-api, we need to get rid of this or adjust it to be more sustainable (e.g. using the appropriate document to get a syntax tree)
        var tree = CSharpSyntaxTree.ParseText(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path: documentFilePath);
        var root = tree.GetRoot();
        var isFileBasedProgram = root.GetLeadingTrivia().Any(SyntaxKind.IgnoredDirectiveTrivia) || root.ChildNodes().Any(node => node.IsKind(SyntaxKind.GlobalStatement));

        var virtualProjectXml = $"""
            <Project Sdk="Microsoft.NET.Sdk">

                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <Features>$(Features);FileBasedProgram</Features>
                    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                </PropertyGroup>

                <ItemGroup>
                    <Compile Include="{SecurityElement.Escape(documentFilePath)}" />
                </ItemGroup>

                <!--
                  Override targets which don't work with project files that are not present on disk.
                  See https://github.com/NuGet/Home/issues/14148.
                -->

                <Target Name="_FilterRestoreGraphProjectInputItems"
                        DependsOnTargets="_LoadRestoreGraphEntryPoints"
                        Returns="@(FilteredRestoreGraphProjectInputItems)">
                  <ItemGroup>
                    <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                  </ItemGroup>
                </Target>

                <Target Name="_GetAllRestoreProjectPathItems"
                        DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                        Returns="@(_RestoreProjectPathItems)">
                  <ItemGroup>
                    <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                  </ItemGroup>
                </Target>

                <Target Name="_GenerateRestoreGraph"
                        DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                        Returns="@(_RestoreGraphEntry)">
                  <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
                </Target>
            </Project>
            """;

        return (virtualProjectXml, isFileBasedProgram);
    }
}
