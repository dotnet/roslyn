// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.UnitTests;

internal static class ProjectGuardFiles
{
    private static int _alreadyWritten = 0;

    internal static void EnsureWrittenToTemp()
    {
        if (Interlocked.CompareExchange(ref _alreadyWritten, value: 1, comparand: 0) != 0)
            return;

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "global.json"),
           """
           {
               "comment": "this file is empty to ensure we get the 'standard' behavior as if no global.json was specified in the first place"
           }
           """);

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "Directory.Build.props"),
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

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "Directory.Build.rsp"),
           """
           # This file intentionally left blank to avoid accidental import during testing
           """);

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "Directory.Build.targets"),
           """
           <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information. -->
           <Project>
               <!-- Intentionally left blank. This file is used to prevent accidental import of
                    Directory.Build.props from our repo during testing -->
           </Project>
           """);

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "NuGet.Config"),
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
}