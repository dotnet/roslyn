// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

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
}
