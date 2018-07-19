# Visual Studio Extension Testing

This project allows Visual Studio extension developers to write integration tests that run inside an experimental
instance of Visual Studio.

# Installation and Use

## Requirements

*TODO*

## Install the test harness

### Install the *to be determined* package

*TODO*

### Configure the `appDomain` xUnit property

1. Add a file **xunit.runner.json** to the test project if it does not already exist
2. Set the **Copy to Output Directory** property for the file to **Copy if newer**
3. Update the file to set the `appDomain` property to `denied`:

    ```json
    {
      "appDomain": "denied"
    }
    ```

:link: See https://github.com/Microsoft/vs-extension-testing/issues/3

### Configure the test framework

#### Classic projects

Add the following to **AssemblyInfo.cs** to enable the test framework:

```csharp
using Xunit;

[assembly: TestFramework("Xunit.Harness.IdeTestFramework", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
```

#### SDK projects

SDK projects have the ability to automatically generate assembly attributes. This functionality can be leveraged to
configured the required test framework attribute. Simply add the following to your project file:

```xml
<ItemGroup>
  <AssemblyAttribute Include="Xunit.TestFrameworkAttribute">
    <_Parameter1>Xunit.Harness.IdeTestFramework</_Parameter1>
    <_Parameter2>Microsoft.VisualStudio.Extensibility.Testing.Xunit</_Parameter2>
  </AssemblyAttribute>
</ItemGroup>
```

### Configure extensions for deployment

Add the following to **AssemblyInfo.cs** to deploy extensions required for testing.

```csharp
using Xunit.Harness;

[assembly: RequireExtension("Extension.File.Name.vsix")]
```

## Ensure test discovery is enabled

Test projects using a customized xUnit test framework cannot currently be discovered while tests are being written. The
test discovery process that runs after a build completes will detect the required tests. Ensure this feature is enabled
by the following steps:

1. Open **Tools** &rarr; **Options...**
2. Select the **Test** page on the left
3. Ensure **Additionally discover tests from built assemblies after builds** is checked

Tests will be automatically discovered and **Test Explorer** updated after each successful build.

## Write tests

Apply the `[IdeFact]` attribute to tests that need to run in the IDE. After building the project, the tests will
appear in **Test Explorer** where they can be launched for running and/or debugging directly.
