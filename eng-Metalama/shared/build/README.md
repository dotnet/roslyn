# PostSharp Engineering: Build Features

Make sure you have read and understood [PostSharp Engineering](../README.md) before reading this doc.

## Table of contents

- [PostSharp Engineering: Build Features](#postsharp-engineering-build-features)
  - [Table of contents](#table-of-contents)
  - [Executable scripts](#executable-scripts)
    - [Build.ps1](#buildps1)
    - [Kill.ps1](#killps1)
  - [Imported scripts](#imported-scripts)
    - [AssemblyMetadata.targets](#assemblymetadatatargets)
    - [BuildOptions.props](#buildoptionsprops)
    - [TeamCity.targets](#teamcitytargets)
    - [SourceLink.props](#sourcelinkprops)
  - [NuGet packages metadata](#nuget-packages-metadata)
    - [Installation and configuration](#installation-and-configuration)
  - [Versioning, building and packaging](#versioning-building-and-packaging)
    - [Installation and configuration](#installation-and-configuration-1)
    - [Usage](#usage)
      - [Product package version and package version suffix configuration](#product-package-version-and-package-version-suffix-configuration)
      - [Package dependencies versions configuration](#package-dependencies-versions-configuration)
      - [Local build and testing](#local-build-and-testing)
      - [Local package referencing](#local-package-referencing)
  - [Continuous integration](#continuous-integration)
    - [Instalation](#instalation)

## Executable scripts

### Build.ps1

This is the main build script providing support for build, packaging and testing, both local and withing a CI/CD pipeline.

### Kill.ps1

Kills all processes which might hold any files from the repository.

## Importable scripts

The scripts listed below are meant to be imported in
- `Directory.Build.props` (*.props)
- `Directory.Build.targets` (*.targets)

### AssemblyMetadata.targets

Adds package versions to assembly metadata.

### BuildOptions.props

Sets the compiler options like language version, nullability and other build options like output path.

### TeamCity.targets

Enables build and tests reporting to TeamCity.

### SourceLink.props

Enables SourceLink support.

### Coverage.props

Enabled code coverage. This script should be imported in test projects only (not in projects being tested). This script adds a package to _coverlet_ so there is no need to have in in test projects (and these references should be removed).

## NuGet packages metadata

This section describes centralized NuGet packages metadata management.

### Installation and configuration

1. Create `eng\Packaging.props` file. The content should look like this:

```xml
<Project>

    <!-- Properties of NuGet packages-->
    <PropertyGroup>
        <Authors>PostSharp Technologies</Authors>
        <PackageProjectUrl>https://github.com/postsharp/Metalama</PackageProjectUrl>
        <PackageTags>PostSharp Metalama AOP</PackageTags>
        <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
        <PackageIcon>PostSharpIcon.png</PackageIcon>
        <PackageLicenseFile>LICENSE.md</PackageLicenseFile>
    </PropertyGroup>

    <!-- Additional content of NuGet packages -->
    <ItemGroup>
        <None Include="$(MSBuildThisFileDirectory)..\PostSharpIcon.png" Visible="false" Pack="true" PackagePath="" />
        <None Include="$(MSBuildThisFileDirectory)..\LICENSE.md" Visible="false" Pack="true" PackagePath="" />
        <None Include="$(MSBuildThisFileDirectory)..\THIRD-PARTY-NOTICES.TXT" Visible="false" Pack="true" PackagePath="" />
    </ItemGroup>

</Project>
```

2. Make sure that all the files referenced in the previous step exist.

3. Import the file from the first step in `Directory.Build.props`:

```
  <Import Project="eng\Packaging.props" />
```

Now all the packages created from the repository will contain the metadata configured in the `eng\Packaging.props` file.

## Versioning, building and packaging

This section describes centralized main version and dependencies version management, cross-repo dependencies, building and packaging.

### Installation and configuration

In this how-to, we use the name `[Product]` as a placeholder for the name of the product contained in a specific repository containing the `eng\shared` subtree.

#### Step 1: .gitignore

Add `eng\[Product]Version.props` to `.gitignore`.

#### Step 2: MainVersion.props

Create `eng\MainVersion.props` file. The content should look like:

```xml
<Project>
    <PropertyGroup>
        <MainVersion>0.3.6</MainVersion>
        <PackageVersionSuffix>-preview</PackageVersionSuffix>
    </PropertyGroup>
</Project>
```

#### Step 3. Versions.props

Create `eng\Versions.props` file. The content should look like:

```xml
<Project>

    <!-- Version of [Product]. -->
    <Import Project="MainVersion.props" Condition="!Exists('[Product]Version.props')" />
    
    <PropertyGroup>
        <[Product]Version>$(MainVersion)$(PackageVersionSuffix)</[Product]Version>
        <[Product]AssemblyVersion>$(MainVersion)</[Product]AssemblyVersion>
    </PropertyGroup>

    <!-- Versions of dependencies -->
    <PropertyGroup>
        <RoslynVersion>3.8.0</RoslynVersion>
        <MetalamaCompilerVersion>3.8.12-preview</MetalamaCompilerVersion>
        <MicrosoftCSharpVersion>4.7.0</MicrosoftCSharpVersion>
    </PropertyGroup>

    <!-- Overrides by local settings -->
    <Import Project="../artifacts/private/[Product]Version.props" Condition="Exists('../artifacts/private/[Product]Version.props')" />
    <Import Project="Dependencies.props" Condition="Exists('Dependencies.props')" />

    <!-- Other properties depending on the versions set above -->
    <PropertyGroup>
        <AssemblyVersion>$([Product]AssemblyVersion)</AssemblyVersion>
        <Version>$([Product]Version)</Version>
    </PropertyGroup>
    

</Project>
```

#### Step 4. Directory.Build.props

Add the following imports to `Directory.Build.props`:

```xml
  <Import Project="eng\Versions.props" />
  <Import Project="eng\shared\build\BuildOptions.props" />
```

#### Step 5. Directory.Build.targets

Add the following imports to `Directory.Build.targets`:

```xml
  <Import Project="eng\shared\build\TeamCity.targets" />
```

#### Step 6. Create the front-end build project

Create a file `eng\src\Build.csproj` with the following content:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net5.0</TargetFramework>
        <AssemblyName>Build</AssemblyName>
        <GenerateDocumentationFile>false</GenerateDocumentationFile>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <NoWarn>SA0001;CS8002</NoWarn>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\shared\src\PostSharp.Engineering.BuildTools.csproj"/>
    </ItemGroup>

</Project>
```

Create also a file `eng\src\Program.cs` with content that varies according to your repo. You can use all the power of C# and PowerShell to customize the
build. Note that in the `PublicArtifacts`, the strings `$(Configuration)` and `$(PackageVersion)`, and only those strings, are replaced by their value. 

```cs
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Commands.Build;
using Spectre.Console.Cli;
using System.Collections.Immutable;

namespace BuildMetalama
{
    internal class Program
    {
        private static int Main( string[] args )
        {
            var product = new Product
            {
                ProductName = "Metalama",
                Solutions = ImmutableArray.Create<Solution>(
                    new DotNetSolution( "Metalama.sln" )
                    {
                        SupportsTestCoverage = true
                    },
                    new DotNetSolution( "Tests\\Metalama.Framework.TestApp\\Metalama.Framework.TestApp.sln" )
                    {
                        IsTestOnly = true
                    } ),
                PublicArtifacts = ImmutableArray.Create(
                    "bin\\$(Configuration)\\Metalama.Framework.$(PackageVersion).nupkg",
                    "bin\\$(Configuration)\\Metalama.TestFramework.$(PackageVersion).nupkg",
                    "bin\\$(Configuration)\\Metalama.Framework.Redist.$(PackageVersion).nupkg",
                    "bin\\$(Configuration)\\Metalama.Framework.Sdk.$(PackageVersion).nupkg",
                    "bin\\$(Configuration)\\Metalama.Framework.Impl.$(PackageVersion).nupkg",
                    "bin\\$(Configuration)\\Metalama.Framework.DesignTime.Contracts.$(PackageVersion).nupkg" )
            };
            var commandApp = new CommandApp();
            commandApp.AddProductCommands( product );

            return commandApp.Run( args );
        }
    }
}
```

#### Step 7. Create Build.ps1, the front-end build script

Create `Build.ps1` file in the repo root directory. The content should look like:

```powershell
if ( $env:VisualStudioVersion -eq $null ) {
    Import-Module "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
    Enter-VsDevShell -VsInstallPath "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\" -StartInPath $(Get-Location)
}

& dotnet run --project "$PSScriptRoot\eng\src\Build.csproj" -- $args
exit $LASTEXITCODE
```

### Usage

#### Product package version and package version suffix configuration

The product package version and package version suffix configuration is centralized in the `eng\MainVersion.props` script via the `MainVersion` and `PackageVersionSuffix` properties, respectively. For RTM products, leave the `PackageVersionSuffix` property value empty.

#### Package dependencies versions configuration

Package dependencies versions configuration is centralized in the `eng\Versions.props` script. Each dependency version is configured in a property named `<[DependencyName]Version>`, eg. `<SystemCollectionsImmutableVersion>`.

This property value is then available in all MSBuild project files in the repository and can be used in the `PackageReference` items. For example:

```
<ItemGroup>
    <PackageReference Include="System.Collections.Immutable" Version="$(SystemCollectionsImmutableVersion)" />
</ItemGroup>
```

#### Build and testing locally

For details, do `Build.ps1` in PowerShell. 


#### Referencing a package in another repository

Dependencies must be checked out under the same root directory (typically `c:\src`) under their canonic name.

Then, use `Build.ps1 dependencies local` to specify which dependencies should be run locally.

This will generate `eng/Dependencies.props`, which you should have imported in `eng/Versions.props`.

## Continuous integration

We use TeamCity as our CI/CD pipeline at the moment. The following sections describe a common way to set up continuous integration on TeamCity. See [PostSharp Engineering: Deployment Features](../deploy/README.md#continuous-deployment) for information about continuous deployment.

### Installation

1. Create a new (sub)project using manual setup.
   
2. Set up versioned settings if necessary.

3. Add a VCS root.

4. Create build configurations. Set build agent requirements and triggers as needed.

#### "Debug Build and Test" build configuration
   
Create "Debug Build and Test" build configuration using manual build steps configuration.

##### Build steps:

| # | Name | Type | Configuration |
| - | ---- | ---- | ------------- |
| 1 | Debug Build and Test | PowerShell | Format stderr output as: error; Script: file; Script file: `Build.ps1`; Script arguments: `test --numbered-build %build.number%` |

##### Artifact paths:

```
artifacts\** => artifacts
```

#### "Release Build and Test" build configuration

Create "Release Build and Test" build configuration using manual build steps configuration.

##### Build steps:

| # | Name | Type | Configuration |
| - | ---- | ---- | ------------- |
| 1 | Local Release Signed Build And Test | PowerShell | Format stderr output as: error; Script: file; Script file: `Build.ps1`; Script arguments: `test --configuration Release --sign` |
| 2 | Public Release Signed Build | PowerShell | Format stderr output as: error; Script: file; Script file: `Build.ps1`; Script arguments: `build --public-build -configuration Release --sign --zip` |

The tests are not performed on the public release build, as some tests may require NuGet packages, which leads to a package version conflict. The tests then may use different packages with the same package version producing false test results.

#### Artifact paths:

```
artifacts\** => artifacts
```

#### Snapshot dependencies:
- Debug Build and Test

#### Required environment variables:
- SIGNSERVER_SECRET


## Deployment


#### "Publish Debug to Internal Feed" deployment configuration 

Create "Publish Debug to Internal Feed" deployment configuration using manual build steps configuration.

##### Build steps:

| # | Name       | Type | Configuration                                                                                                                                                                                   |
|---|------------|------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Deploy | Powershell | Command: `Build.ps1 deploy` |

##### Snapshot dependencies:

- Debug Build and Test

##### Artifact dependencies:

- Debug Build and Test
    
    ```
    +:artifacts/**/* => artifacts
    ```

#### Required environment variables:
- INTERNAL_NUGET_PUSH_URL
- INTERNAL_NUGET_API_KEY
- NUGET_ORG_API_KEY

#### "Publish Release to NuGet.Org and Internal Feed"  deployment configuration

Create "Publish Release to NuGet.Org and Internal Feed" deployment configuration using manual build steps configuration.

##### Build steps:

| # | Name                  | Type | Configuration                                                                                                                                                                                     |
|---|-----------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Deploy     | Powershell | Command: `Build.ps1 deploy --public`  |

##### Snapshot dependencies:

- Debug Build and Test
- Release Build and Test

###### Artifact dependencies:

- Release Build and Test

```
+:artifacts/**/* => artifacts
```

#### Required environment variables:
- INTERNAL_NUGET_PUSH_URL
- INTERNAL_NUGET_API_KEY