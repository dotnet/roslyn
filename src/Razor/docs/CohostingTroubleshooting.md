# Troubleshooting the Cohosted Razor Editor in Visual Studio

This document covers known issues with the cohosted Razor editor in Visual Studio and their solutions.

Most issues with the cohosted editor stem from the Razor source generator not being enabled or not functioning correctly. When this happens, the editor may show no IntelliSense, report missing components, or appear to not recognize Razor files at all. Check the Output window under the **Razor Logger Output** category for diagnostic messages.

## Known Issues

### Razor Source Generator Disabled via MSBuild Properties

If your project file disables the Razor source generator by setting either of these properties to `false`:

```xml
<UseRazorSourceGenerator>false</UseRazorSourceGenerator>
```

or

```xml
<RazorCompileOnBuild>false</RazorCompileOnBuild>
```

the cohosted editor will not function correctly. Setting `RazorCompileOnBuild` to `false` implicitly disables the Razor source generator as well. A message will be logged to the Output window under the **Razor Logger Output** category indicating that there are no additional files in the project.

**Solution:** Set these properties to `true` (or remove them, as `true` is the default):

```xml
<UseRazorSourceGenerator>true</UseRazorSourceGenerator>
<RazorCompileOnBuild>true</RazorCompileOnBuild>
```

If your CI or build pipeline requires the source generator to be disabled, you can condition it on design-time builds so the editor still works:

```xml
<UseRazorSourceGenerator Condition="'$(DesignTimeBuild)' == 'true'">true</UseRazorSourceGenerator>
<RazorCompileOnBuild Condition="'$(DesignTimeBuild)' == 'true'">true</RazorCompileOnBuild>
```

### Projects Targeting .NET 5.0 or Below

If your project targets `net5.0` or an earlier framework, the Razor source generator is not enabled by default. These frameworks are [out of support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) and there are no plans to support them in the cohosted editor at this time.

**Workaround:** Install an older version of Visual Studio (which can be installed side-by-side with newer versions) to work with these projects.

### Projects Using the `MSBuild.SDK.SystemWeb` SDK

The community `MSBuild.SDK.SystemWeb` SDK has never been officially supported and previously worked by opting in to a legacy editor that has since been deprecated.

The only officially supported path for ASP.NET projects on .NET Framework is to use the classic `.csproj` format project files.

**Workarounds:**
- For a potential fix to unblock the cohosting editor in these scenarios, see [dotnet/razor#12332](https://github.com/dotnet/razor/issues/12332).
- Install an older version of Visual Studio (e.g., 2022) side-by-side with the current version.

### Projects Using Umbraco Templates

Some Umbraco templates do not add additional items and are missing other metadata that the Razor source generator requires, which prevents the cohosted editor from functioning correctly.

A fix has been contributed to the Umbraco templates ([Umbraco-CMS#21861](https://github.com/umbraco/Umbraco-CMS/pull/21861)) and is expected to ship in Umbraco 17.3.

See also [dotnet/razor#12331](https://github.com/dotnet/razor/issues/12331) for more details.

### Default Content Items Disabled

The Razor source generator requires `.razor` and `.cshtml` files to be registered as additional items in your project so that Roslyn can see them and pass them to the generator. The Web and Razor SDKs do this by default, but it can be turned off in your project file. If any of the following properties are set to `false`:

```xml
<EnableDefaultContentItems>false</EnableDefaultContentItems>
<EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
<EnableDefaultRazorComponentItems>false</EnableDefaultRazorComponentItems>
```

the source generator will not receive the Razor files and the cohosted editor will not function correctly.

**Solution:** Set these properties back to `true`.

If this causes problems with your CI or build pipeline, you can condition them on design-time builds so the editor still works:

```xml
<EnableDefaultContentItems Condition="'$(DesignTimeBuild)' == 'true'">true</EnableDefaultContentItems>
<EnableDefaultRazorGenerateItems Condition="'$(DesignTimeBuild)' == 'true'">true</EnableDefaultRazorGenerateItems>
<EnableDefaultRazorComponentItems Condition="'$(DesignTimeBuild)' == 'true'">true</EnableDefaultRazorComponentItems>
```

Alternatively, you can manually add the files as additional items:

```xml
<ItemGroup>
  <AdditionalFiles Include="**/*.razor" />
  <AdditionalFiles Include="**/*.cshtml" />
</ItemGroup>
```

### Pinning to Specific .NET 8 SDK Versions via `global.json`

If your project uses a `global.json` that pins to a specific .NET 8 SDK version (e.g., `8.0.201`, and possibly others), the Razor source generator may not work because the SDK looks for an older filename.

This was fixed in subsequent .NET 8 SDK versions (e.g., `8.0.317` or `8.0.414`). The SDK versions known to exhibit this problem are all out of support.

**Solution:** Install a newer .NET 8 SDK and allow your project to roll forward. Given the affected versions are out of support, upgrading is advisable regardless.
