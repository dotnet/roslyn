// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class FileBasedProgramTests : TestBase
{
    private const string CurrentTargetFramework = "net10.0";

    [Fact]
    public void Directives()
    {
        VerifyConversion(
            inputCSharp: """
                #!/program
                #:sdk Microsoft.NET.Sdk
                #:sdk Aspire.Hosting.Sdk 9.1.0
                #:property TargetFramework net11.0
                #:package System.CommandLine 2.0.0-beta4.22272.1
                #:property LangVersion preview
                Console.WriteLine();
                """,
            expectedProject: $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <Sdk Name="Aspire.Hosting.Sdk" Version="9.1.0" />

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
                  </ItemGroup>

                </Project>

                """,
            expectedCSharp: """
                Console.WriteLine();
                """);
    }

    [Fact]
    public void Directives_Variable()
    {
        VerifyConversion(
            inputCSharp: """
                #:package MyPackage $(MyProp)
                #:property MyProp MyValue
                """,
            expectedProject: $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <MyProp>MyValue</MyProp>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="MyPackage" Version="$(MyProp)" />
                  </ItemGroup>

                </Project>

                """,
            expectedCSharp: "");
    }

    [Fact]
    public void Directives_Separators()
    {
        VerifyConversion(
            inputCSharp: """
                #:property Prop1   One=a/b
                #:property Prop2   Two/a=b
                #:sdk First 1.0=a/b
                #:sdk Second 2.0/a=b
                #:sdk Third 3.0=a/b
                #:package P1 1.0/a=b
                #:package P2 2.0/a=b
                """,
            expectedProject: $"""
                <Project Sdk="First/1.0=a/b">

                  <Sdk Name="Second" Version="2.0/a=b" />
                  <Sdk Name="Third" Version="3.0=a/b" />

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Prop1>One=a/b</Prop1>
                    <Prop2>Two/a=b</Prop2>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="P1" Version="1.0/a=b" />
                    <PackageReference Include="P2" Version="2.0/a=b" />
                  </ItemGroup>

                </Project>

                """,
            expectedCSharp: "");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("SDK")]
    public void Directives_Unknown(string directive)
    {
        VerifyConversion(
            inputCSharp: $"""
                #:sdk Test
                #:{directive} Test
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(2,1): error CS9308: Unrecognized directive 'invalid'.
                // #:invalid Test
                Diagnostic(ErrorCode.ERR_UnrecognizedDirective).WithArguments(directive).WithLocation(2, 1),
            ]);
    }

    [Fact]
    public void Directives_Empty()
    {
        VerifyConversion(
            inputCSharp: """
                #:
                #:sdk Test
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(1,1): error CS9308: Unrecognized directive ''.
                // #:
                Diagnostic(ErrorCode.ERR_UnrecognizedDirective).WithArguments("").WithLocation(1, 1),
            ]);
    }

    [Theory, CombinatorialData]
    public void Directives_EmptyName(
        [CombinatorialValues("sdk", "property", "package")] string directive,
        [CombinatorialValues(" ", "")] string value)
    {
        VerifyConversion(
            inputCSharp: $"""
                #:{directive}{value}
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(1,1): error CS9309: Missing name of 'sdk'.
                // #:sdk
                Diagnostic(ErrorCode.ERR_MissingDirectiveName).WithArguments(directive).WithLocation(1, 1),
            ]);
    }

    [Fact]
    public void Directives_MissingPropertyValue()
    {
        VerifyConversion(
            inputCSharp: """
                #:property Test
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(1,1): error CS9310: The property directive needs to have two parts separated by a space like 'PropertyName PropertyValue'
                // #:property Test
                Diagnostic(ErrorCode.ERR_PropertyDirectiveMissingParts).WithLocation(1, 1),
            ]);
    }

    [Fact]
    public void Directives_InvalidPropertyName()
    {
        VerifyConversion(
            inputCSharp: """
                #:property Name" Value
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(1,1): error CS9311: Invalid property name: The '"' character, hexadecimal value 0x22, cannot be included in a name.
                // #:property Name" Value
                Diagnostic(ErrorCode.ERR_PropertyDirectiveInvalidName).WithArguments(@"The '""' character, hexadecimal value 0x22, cannot be included in a name.").WithLocation(1, 1),
            ]);
    }

    [Fact]
    public void Directives_Escaping()
    {
        VerifyConversion(
            inputCSharp: """
                #:property Prop <test">
                #:sdk <test"> ="<>test
                #:package <test"> ="<>test
                """,
            expectedProject: $"""
                <Project Sdk="&lt;test&quot;&gt;/=&quot;&lt;&gt;test">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Prop>&lt;test&quot;&gt;</Prop>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="&lt;test&quot;&gt;" Version="=&quot;&lt;&gt;test" />
                  </ItemGroup>

                </Project>

                """,
            expectedCSharp: "");
    }

    [Fact]
    public void Directives_Whitespace()
    {
        VerifyConversion(
            inputCSharp: """
                    #:   sdk   TestSdk
                #:property Name   Value   
                #:property NugetPackageDescription "My package with spaces"
                 #  !  /test
                  #!  /program   x   
                 # :property Name Value
                """,
            expectedProject: $"""
                <Project Sdk="TestSdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Name>Value</Name>
                    <NugetPackageDescription>&quot;My package with spaces&quot;</NugetPackageDescription>
                  </PropertyGroup>

                </Project>

                """,
            expectedCSharp: """
                 #  !  /test
                  #!  /program   x   
                 # :property Name Value
                """);
    }

    [Fact]
    public void Directives_Whitespace_Invalid()
    {
        VerifyConversion(
            inputCSharp: $"""
                #:   property   Name{'\t'}     Value
                """,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(1,1): error CS9311: Invalid property name: The '\t' character, hexadecimal value 0x09, cannot be included in a name.
                // #:   property   Name\t     Value
                Diagnostic(ErrorCode.ERR_PropertyDirectiveInvalidName).WithArguments("The '\t' character, hexadecimal value 0x09, cannot be included in a name.").WithLocation(1, 1),
            ]);
    }

    /// <summary>
    /// <c>#:</c> directives after C# code are ignored.
    /// </summary>
    [Fact]
    public void Directives_AfterToken()
    {
        string source = """
            #:property Prop 1
            #define X
            #:property Prop 2
            Console.WriteLine();
            #:property Prop 3
            """;

        VerifyConversion(
            inputCSharp: source,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(5,1): error CS9312: This directive cannot be converted. Run the file to see more details.
                // #:property Prop 3
                Diagnostic(ErrorCode.ERR_CannotConvertDirective).WithLocation(5, 1),
            ]);

        VerifyConversion(
            inputCSharp: source,
            force: true,
            expectedProject: $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Prop>1</Prop>
                    <Prop>2</Prop>
                  </PropertyGroup>

                </Project>

                """,
            expectedCSharp: """
                #define X
                Console.WriteLine();
                #:property Prop 3
                """);
    }

    /// <summary>
    /// <c>#:</c> directives after <c>#if</c> are ignored.
    /// </summary>
    [Fact]
    public void Directives_AfterIf()
    {
        string source = """
            #:property Prop 1
            #define X
            #:property Prop 2
            #if X
            #:property Prop 3
            #endif
            #:property Prop 4
            """;

        VerifyConversion(
            inputCSharp: source,
            expectedProject: null,
            expectedCSharp: null,
            expectedDiagnostics:
            [
                // /app/Program.cs(5,1): error CS9312: This directive cannot be converted. Run the file to see more details.
                // #:property Prop 3
                Diagnostic(ErrorCode.ERR_CannotConvertDirective).WithLocation(5, 1),
                // /app/Program.cs(7,1): error CS9312: This directive cannot be converted. Run the file to see more details.
                // #:property Prop 4
                Diagnostic(ErrorCode.ERR_CannotConvertDirective).WithLocation(7, 1),
            ]);

        VerifyConversion(
            inputCSharp: source,
            force: true,
            expectedProject: $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Prop>1</Prop>
                    <Prop>2</Prop>
                  </PropertyGroup>

                </Project>

                """,
            expectedCSharp: """
                #define X
                #if X
                #:property Prop 3
                #endif
                #:property Prop 4
                """);
    }

    /// <summary>
    /// Comments are not currently converted.
    /// </summary>
    [Fact]
    public void Directives_Comments()
    {
        VerifyConversion(
            inputCSharp: """
                // License for this file
                #:sdk MySdk
                // This package is needed for Json
                #:package MyJson
                // #:package Unused
                /* Custom props: */
                #:property Prop 1
                #:property Prop 2
                Console.Write();
                """,
            expectedProject: $"""
                <Project Sdk="MySdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>{CurrentTargetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <PropertyGroup>
                    <Prop>1</Prop>
                    <Prop>2</Prop>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="MyJson" />
                  </ItemGroup>

                </Project>

                """,
            expectedCSharp: """
                // License for this file
                // This package is needed for Json
                // #:package Unused
                /* Custom props: */
                Console.Write();
                """);
    }

    // Cannot put this `#if` around the whole test file currently due to https://github.com/dotnet/roslyn/issues/78157.
#if NET9_0_OR_GREATER
    /// <param name="actualProject">
    /// <see langword="null"/> means the conversion failed before project writer has been created.
    /// </param>
    private static void Convert(
        string inputCSharp,
        out string? actualProject,
        out string? actualCSharp,
        out ImmutableArray<Diagnostic> actualDiagnostics,
        bool force)
    {
#pragma warning disable RSEXPERIMENTAL006 // 'VirtualProject' is experimental
        var virtualProject = new FileBasedPrograms.VirtualProject("/app/Program.cs");
        actualDiagnostics = virtualProject.ParseDirectives(
            virtualProject.EntryPointFileFullPath,
            SourceText.From(inputCSharp, Encoding.UTF8),
            reportAllErrors: true);
        if (force || actualDiagnostics.Length == 0)
        {
            var csprojWriter = new StringWriter();
            virtualProject.EmitConverted(csprojWriter);
            actualProject = csprojWriter.ToString();

            actualCSharp = virtualProject.ConvertSourceText(virtualProject.EntryPointFileFullPath)?.ToString();
        }
        else
        {
            actualProject = null;
            actualCSharp = null;
        }
#pragma warning restore RSEXPERIMENTAL006 // 'VirtualProject' is experimental
    }
#endif

    /// <param name="expectedProject">
    /// <see langword="null"/> means the conversion should have failed before project writer has been created.
    /// </param>
    /// <param name="expectedCSharp">
    /// <see langword="null"/> means the conversion should not touch the C# content.
    /// </param>
    private static void VerifyConversion(
        string inputCSharp,
        string? expectedProject,
        string? expectedCSharp,
        bool force = false,
        params DiagnosticDescription[] expectedDiagnostics)
    {
#if NET9_0_OR_GREATER
        Convert(
            inputCSharp,
            out var actualProject,
            out var actualCSharp,
            out var actualDiagnostics,
            force: force);
        AssertEx.Equal(expectedProject, actualProject);
        AssertEx.Equal(expectedCSharp, actualCSharp);
        actualDiagnostics.Verify(expectedDiagnostics);
#endif
    }
}
