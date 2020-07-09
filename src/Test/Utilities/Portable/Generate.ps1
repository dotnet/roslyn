function Add-TargetFramework($name, $packagePath, $list)
{
  $refName = [char]::toupper($name[0]) + $name.Substring(1)
  $resourceTypeName = "Resources" + $refName
  $script:codeContent += @"
        public static class $resourceTypeName
        {

"@;

  $refContent = @"
        public static class $refName
        {

"@

  foreach ($dllPath in $list)
  {
    if ($dllPath.Contains('#'))
    {
      $all = $dllPath.Split('#')
      $dllName = $all[0]
      $dllPath = $all[1]
      $dll = Split-Path -leaf $dllPath
    }
    else
    {
      $dll = Split-Path -leaf $dllPath
      $dllName = $dll.Substring(0, $dll.Length - 4)
    }

    $logicalName = "$($name).$($dll)";
    $script:targetsContent += @"
        <EmbeddedResource Include="$packagePath\$dllPath">
          <LogicalName>$logicalName</LogicalName>
        </EmbeddedResource>

"@

    $propName = $dllName.Replace(".", "");
    $fieldName = "_" + $propName
    $script:codeContent += @"
            private static byte[] $fieldName;
            public static byte[] $propName => ResourceLoader.GetOrCreateResource(ref $fieldName, "$logicalName");

"@

    $refContent += @"
            public static PortableExecutableReference $propName { get; } = AssemblyMetadata.CreateFromImage($($resourceTypeName).$($propName)).GetReference(display: "$dll ($name)");

"@

  }

  $script:codeContent += @"
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

using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public static class TestMetadata
    {

"@

$net20 = @(
  'mscorlib.dll',
  'System.dll',
  'Microsoft.VisualBasic.dll'
)

Add-TargetFramework "net20" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net20)\build\.NETFramework\v2.0' $net20

$net35 = @(
  'System.Core.dll'
)

Add-TargetFramework "net35" '$(Pkgjnm2_ReferenceAssemblies_net35)\build\.NETFramework\v3.5' $net35

$net40 = @(
  'mscorlib.dll',
  'System.dll',
  'System.Core.dll',
  'System.Data.dll',
  'System.Xml.dll',
  'System.Xml.Linq.dll',
  'Microsoft.VisualBasic.dll',
  'Microsoft.CSharp.dll'
)

Add-TargetFramework "net40" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net40)\build\.NETFramework\v4.0' $net40

$net451 = @(
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

Add-TargetFramework "net451" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net451)\build\.NETFramework\v4.5.1' $net451

$net461 = @(
  'mscorlib.dll',
  'System.dll',
  'System.Core.dll',
  'Facades\System.Runtime.dll',
  'Facades\System.Threading.Tasks.dll',
  'Microsoft.CSharp.dll',
  'Microsoft.VisualBasic.dll'
)

Add-TargetFramework "net461" '$(PkgMicrosoft_NETFramework_ReferenceAssemblies_net461)\build\.NETFramework\v4.6.1' $net461

$tasksExtensions = @(
  'System.Threading.Tasks.Extensions.dll'
)

Add-TargetFramework "ValueTask" '$(PkgSystem_Threading_Tasks_Extensions)\lib\portable-net45+win8+wp8+wpa81' $tasksExtensions

$buildExtensions = @(
  'NetStandard461#net461\lib\netstandard.dll'
)

Add-TargetFramework "BuildExtensions" '$(PkgMicrosoft_NET_Build_Extensions)\msbuildExtensions\Microsoft\Microsoft.NET.Build.Extensions' $buildExtensions

$targetsContent += @"
  </ItemGroup>
</Project>
"@;

$codeContent += @"
    }
}
"@

$targetsContent | Out-File "Generated.targets" -Encoding Utf8
$codeContent | Out-File "Generated.cs" -Encoding Utf8
