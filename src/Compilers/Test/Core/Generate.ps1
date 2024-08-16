function Add-TargetFramework($name, $packagePath, $list)
{
  $resourceTypeName = "Resources" + $name
  $script:codeContent += @"
        public static class $resourceTypeName
        {

"@;

  $refContent = @"
        public static class $name
        {

"@

  $refAllContent = @"
            public static ReferenceInfo[] All => new[]
            {

"@

  $name = $name.ToLower()
  foreach ($dllPath in $list)
  {
    if ($dllPath.Contains('#'))
    {
      $all = $dllPath.Split('#')
      $dllName = $all[0]
      $dllPath = $all[1]
      $dll = Split-Path -leaf $dllPath
      $logicalName = "$($dllName.ToLower()).$($name).$($dll)";
    }
    else
    {
      $dll = Split-Path -leaf $dllPath
      $dllName = $dll.Substring(0, $dll.Length - 4)
      $logicalName = "$($name).$($dll)";
    }

    $dllFileName = "$($dllName).dll"
    $link = "Resources\ReferenceAssemblies\$name\$dll"
    $script:targetsContent += @"
        <EmbeddedResource Include="$packagePath\$dllPath">
          <LogicalName>$logicalName</LogicalName>
          <Link>$link</Link>
        </EmbeddedResource>

"@

    $propName = $dllName.Replace(".", "");
    $fieldName = "_" + $propName
    $script:codeContent += @"
            private static byte[] $fieldName;
            public static byte[] $propName => ResourceLoader.GetOrCreateResource(ref $fieldName, "$logicalName");

"@

    $refAllContent += @"
                new ReferenceInfo("$dllFileName", $propName),

"@

    $refContent += @"
            public static PortableExecutableReference $propName { get; } = AssemblyMetadata.CreateFromImage($($resourceTypeName).$($propName)).GetReference(display: "$dll ($name)", filePath: "$dllFileName");

"@

  }

  $script:codeContent += $refAllContent
  $script:codeContent += @"
            };
        }

"@

    $script:codeContent += $refContent;
    $script:codeContent += @"
        }

"@
}

$targetsContent = @"
<Project>
    <ItemGroup>

"@;

$codeContent = @"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a generated file, please edit Generate.ps1 to change the contents

#nullable disable

using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public static class TestMetadata
    {
        public readonly struct ReferenceInfo
        {
            public string FileName { get; }
            public byte[] ImageBytes { get; }
            public ReferenceInfo(string fileName, byte[] imageBytes)
            {
                FileName = fileName;
                ImageBytes = imageBytes;
            }
        }

"@


Add-TargetFramework "Net20" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net20)\build\.NETFramework\v2.0' @(
  'mscorlib.dll',
  'System.dll',
  'Microsoft.VisualBasic.dll')

Add-TargetFramework "Net40" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net40)\build\.NETFramework\v4.0' @(
  'mscorlib.dll',
  'System.dll',
  'System.Core.dll',
  'System.Data.dll',
  'System.Xml.dll',
  'System.Xml.Linq.dll',
  'Microsoft.VisualBasic.dll',
  'Microsoft.CSharp.dll'
)

Add-TargetFramework "Net451" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net451)\build\.NETFramework\v4.5.1' @(
  'mscorlib.dll',
  'System.dll',
  'System.Configuration.dll',
  'System.Core.dll',
  'System.Data.dll',
  'System.Drawing.dll',
  'System.EnterpriseServices.dll',
  'System.Runtime.Serialization.dll',
  'System.Windows.Forms.dll',
  'System.Web.Services.dll',
  'System.Xml.dll',
  'System.Xml.Linq.dll',
  'Microsoft.CSharp.dll',
  'Microsoft.VisualBasic.dll',
  'Facades\System.ObjectModel.dll',
  'Facades\System.Runtime.dll',
  'Facades\System.Runtime.InteropServices.WindowsRuntime.dll',
  'Facades\System.Threading.dll',
  'Facades\System.Threading.Tasks.dll'
)

Add-TargetFramework "MicrosoftCSharp" '$(PkgMicrosoft_CSharp)' @(
  'Netstandard10#ref\netstandard1.0\Microsoft.CSharp.dll'
  'Netstandard13Lib#lib\netstandard1.3\Microsoft.CSharp.dll'
)

Add-TargetFramework "MicrosoftVisualBasic" '$(PkgMicrosoft_VisualBasic)\ref' @(
  'Netstandard11#netstandard1.1\Microsoft.VisualBasic.dll'
)

Add-TargetFramework "SystemThreadingTasksExtensions" '$(PkgSystem_Threading_Tasks_Extensions)' @(
  'PortableLib#\lib\portable-net45+win8+wp8+wpa81\System.Threading.Tasks.Extensions.dll',
  'NetStandard20Lib#\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll'
)

Add-TargetFramework "BuildExtensions" '$(PkgMicrosoft_NET_Build_Extensions)\msbuildExtensions\Microsoft\Microsoft.NET.Build.Extensions' @(
  'NetStandardToNet461#net461\lib\netstandard.dll'
)

$targetsContent += @"
  </ItemGroup>
</Project>
"@;

$codeContent += @"
    }
}
"@

try {
  Push-Location $PSScriptRoot
  $targetsContent | Out-File "Generated.targets" -Encoding Utf8
  $codeContent | Out-File "Generated.cs" -Encoding Utf8
}
finally {
  Pop-Location
}
