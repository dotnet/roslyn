// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.Test;

using VerifyCS = CSharpSourceGeneratorVerifier<CSharp.CSharpResxGenerator>;
using VerifyVB = VisualBasicSourceGeneratorVerifier<VisualBasic.VisualBasicResxGenerator>;

public sealed class ResxGeneratorTests
{
    private const string ResxHeader = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
            <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
            <xsd:element name="root" msdata:IsDataSet="true">
              <xsd:complexType>
                <xsd:choice maxOccurs="unbounded">
                  <xsd:element name="metadata">
                    <xsd:complexType>
                      <xsd:sequence>
                        <xsd:element name="value" type="xsd:string" minOccurs="0" />
                      </xsd:sequence>
                      <xsd:attribute name="name" use="required" type="xsd:string" />
                      <xsd:attribute name="type" type="xsd:string" />
                      <xsd:attribute name="mimetype" type="xsd:string" />
                      <xsd:attribute ref="xml:space" />
                    </xsd:complexType>
                  </xsd:element>
                  <xsd:element name="assembly">
                    <xsd:complexType>
                      <xsd:attribute name="alias" type="xsd:string" />
                      <xsd:attribute name="name" type="xsd:string" />
                    </xsd:complexType>
                  </xsd:element>
                  <xsd:element name="data">
                    <xsd:complexType>
                      <xsd:sequence>
                        <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                        <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
                      </xsd:sequence>
                      <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
                      <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
                      <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
                      <xsd:attribute ref="xml:space" />
                    </xsd:complexType>
                  </xsd:element>
                  <xsd:element name="resheader">
                    <xsd:complexType>
                      <xsd:sequence>
                        <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                      </xsd:sequence>
                      <xsd:attribute name="name" type="xsd:string" use="required" />
                    </xsd:complexType>
                  </xsd:element>
                </xsd:choice>
              </xsd:complexType>
            </xsd:element>
          </xsd:schema>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <resheader name="version">
            <value>2.0</value>
          </resheader>
          <resheader name="reader">
            <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
          <resheader name="writer">
            <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
          </resheader>
        """;
    private const string ResxFooter = """
        </root>
        """;

    [Theory]
    [InlineData(CSharpLanguageVersion.CSharp5, Skip = "Expression-bodied members are not supported in C# 5")]
    [InlineData(CSharpLanguageVersion.CSharp6)]
    [InlineData(CSharpLanguageVersion.CSharp7)]
    [InlineData(CSharpLanguageVersion.CSharp8)]
    [InlineData(CSharpLanguageVersion.CSharp9)]
    public async Task SingleString_DefaultCSharpAsync(CSharpLanguageVersion languageVersion)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: languageVersion.ToString())
        {
            LanguageVersion = languageVersion,
            TestState =
            {
                Sources = { "" },
                AdditionalFiles = { ("/0/Resources.resx", code) },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Fact]
    public async Task TwoResourcesSameName_DefaultCSharpAsync()
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test()
        {
            TestState =
            {
                Sources = { "" },
                AdditionalFiles =
                {
                    ("/0/First/Resources.resx", code),
                    ("/0/Second/Resources.resx", code),
                },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    build_property.RootNamespace = TestProject

                    [/0/First/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = First/

                    [/0/Second/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = Second/
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Fact]
    public async Task TwoResourcesSameName_DefaultVisualBasicAsync()
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test()
        {
            TestState =
            {
                Sources = { "" },
                AdditionalFiles =
                {
                    ("/0/First/Resources.resx", code),
                    ("/0/Second/Resources.resx", code),
                },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    build_property.RootNamespace = TestProject

                    [/0/First/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = First/

                    [/0/Second/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = Second/
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Fact]
    public async Task SingleString_DefaultVisualBasicAsync()
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Fact]
    public async Task SingleString_DisableCodeGenAsync()
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { "" },
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", """
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.GenerateSource = false
                    """),
                },
            },
        }.RunAsync();

        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { "" },
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", """
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.GenerateSource = false
                    """),
                },
            },
        }.RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_RootNamespaceCSharpAsync(string rootNamespace)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: rootNamespace)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    build_property.RootNamespace = {rootNamespace}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_RootNamespaceVisualBasicAsync(string rootNamespace)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: rootNamespace)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    build_property.RootNamespace = {rootNamespace}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_RelativeDirCSharpAsync(string relativeDir)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: relativeDir)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = {relativeDir}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_RelativeDirVisualBasicAsync(string relativeDir)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: relativeDir)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.RelativeDir = {relativeDir}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_ClassNameCSharpAsync(string className)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: className)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.ClassName = {className}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NS")]
    [InlineData("NS1.NS2")]
    public async Task SingleString_ClassNameVisualBasicAsync(string className)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: className)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.ClassName = {className}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_OmitGetResourceStringCSharpAsync(bool omitGetResourceString)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        var customGetResourceString = """
            #nullable enable

            namespace TestProject
            {
                internal static partial class Resources
                {
                    internal static string? GetResourceString(string resourceKey, string? defaultValue = null) => throw null!;
                }
            }
            """;

        await new VerifyCS.Test(identifier: omitGetResourceString.ToString())
        {
            TestState =
            {
                Sources = { omitGetResourceString ? customGetResourceString : "" },
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.OmitGetResourceString = {(omitGetResourceString ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_OmitGetResourceStringVisualBasicAsync(bool omitGetResourceString)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        var customGetResourceString = """
            Namespace Global.TestProject
                Friend Partial Class Resources
                    Friend Shared Function GetResourceString(resourceKey As String, Optional defaultValue As String = Nothing)
                        Return ""
                    End Function
                End Class
            End Namespace
            """;

        await new VerifyVB.Test(identifier: omitGetResourceString.ToString())
        {
            TestState =
            {
                Sources = { omitGetResourceString ? customGetResourceString : "" },
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.OmitGetResourceString = {(omitGetResourceString ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_AsConstantsCSharpAsync(bool asConstants)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: asConstants.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.AsConstants = {(asConstants ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_AsConstantsVisualBasicAsync(bool asConstants)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: asConstants.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.AsConstants = {(asConstants ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_IncludeDefaultValuesCSharpAsync(bool includeDefaultValues)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: includeDefaultValues.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.IncludeDefaultValues = {(includeDefaultValues ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_IncludeDefaultValuesVisualBasicAsync(bool includeDefaultValues)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: includeDefaultValues.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.IncludeDefaultValues = {(includeDefaultValues ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_EmitFormatMethodsCSharpAsync(
        [CombinatorialValues("0", "x", "replacement")] string placeholder,
        bool emitFormatMethods)
    {
        var code = ResxHeader
            + $$"""
              <data name="Name" xml:space="preserve">
                <value>value {{{placeholder}}}</value>
                <comment>comment</comment>
              </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: $"{placeholder}_{emitFormatMethods}")
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.EmitFormatMethods = {(emitFormatMethods ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData(true, Skip = "Not yet supported")]
    [InlineData(false)]
    public async Task SingleString_EmitFormatMethodsVisualBasicAsync(bool emitFormatMethods)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: emitFormatMethods.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.EmitFormatMethods = {(emitFormatMethods ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_PublicCSharpAsync(bool publicResource)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyCS.Test(identifier: publicResource.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.Public = {(publicResource ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SingleString_PublicVisualBasicAsync(bool publicResource)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        await new VerifyVB.Test(identifier: publicResource.ToString())
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.Public = {(publicResource ? "true" : "false")}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("CS1591")]
    [InlineData("CS1591, IDE0010")]
    [InlineData(" , CS1591, IDE0010 ")]
    public async Task SingleString_NoWarnsCSharpAsync(string noWarn)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        var id = string.Join("_", noWarn.Replace(" ", "").Split(",", System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries));

        await new VerifyCS.Test(identifier: id)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.NoWarn = {noWarn}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("CS1591")]
    [InlineData("CS1591, IDE0010")]
    [InlineData(" , CS1591, IDE0010 ")]
    public async Task SingleString_NoWarnsVisualBasicAsync(string noWarn)
    {
        var code = ResxHeader
            + """
            <data name="Name" xml:space="preserve">
              <value>value</value>
              <comment>comment</comment>
            </data>
            """
            + ResxFooter;

        var id = string.Join("_", noWarn.Replace(" ", "").Split(",", System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries));

        await new VerifyVB.Test(identifier: id)
        {
            TestState =
            {
                AdditionalFiles = { ("/0/Resources.resx", code) },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                    is_global = true

                    [/0/Resources.resx]
                    build_metadata.AdditionalFiles.NoWarn = {noWarn}
                    """),
                },
            },
        }.AddGeneratedSources().RunAsync();
    }
}
