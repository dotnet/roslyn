// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ProjectGuardFiles : IDisposable
{
    private readonly TempRoot _tempRoot = new();
    private readonly TempDirectory _tempDirectory;

    public ProjectGuardFiles()
    {
        _tempDirectory = _tempRoot.CreateDirectory();

        _tempDirectory.CreateFile("global.json").WriteAllText(
           """
           {
               "comment": "this file is empty to ensure we get the 'standard' behavior as if no global.json was specified in the first place"
           }
           """);

        _tempDirectory.CreateFile("Directory.Build.props").WriteAllText(
           """
           <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information. -->
           <Project>
               <PropertyGroup>
                   <!-- Attempt to make our test more deterministic by disabling any overrides that could be
                       accidentally installed on the developers machine
                   -->
                   <ImportUserLocationsByWildcardBeforeMicrosoftCommonProps>false</ImportUserLocationsByWildcardBeforeMicrosoftCommonProps>
                   <ImportUserLocationsByWildcardAfterMicrosoftCommonProps>false</ImportUserLocationsByWildcardAfterMicrosoftCommonProps>
                   <ImportUserLocationsByWildcardBeforeMicrosoftCSharpTargets>false</ImportUserLocationsByWildcardBeforeMicrosoftCSharpTargets>
                   <ImportUserLocationsByWildcardAfterMicrosoftCSharpTargets>false</ImportUserLocationsByWildcardAfterMicrosoftCSharpTargets>
                   <ImportUserLocationsByWildcardBeforeMicrosoftNetFrameworkProps>false</ImportUserLocationsByWildcardBeforeMicrosoftNetFrameworkProps>
                   <ImportUserLocationsByWildcardAfterMicrosoftNetFrameworkProps>false</ImportUserLocationsByWildcardAfterMicrosoftNetFrameworkProps>
               </PropertyGroup>
           </Project>
           """);

        _tempDirectory.CreateFile("Directory.Build.rsp").WriteAllText(
           """
           # This file intentionally left blank to avoid accidental import during testing
           """);

        _tempDirectory.CreateFile("Directory.Build.targets").WriteAllText(
           """
           <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information. -->
           <Project>
               <!-- Intentionally left blank. This file is used to prevent accidental import of
                    Directory.Build.props from our repo during testing -->
           </Project>
           """);

        _tempDirectory.CreateFile("NuGet.Config").WriteAllText(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                <!-- Provide a default package restore source for test projects. -->
                <packageRestore>
                    <add key="enabled" value="true" />
                </packageRestore>
                <packageSources>
                    <clear />
                    <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
                </packageSources>
            </configuration>
            """);
    }

    internal TempDirectory CreateSolutionDirectory()
        => _tempDirectory.CreateDirectory(Guid.NewGuid().ToString("N"));

    public void Dispose()
        => _tempRoot.Dispose();
}