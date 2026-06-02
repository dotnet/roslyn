// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

[Trait(Traits.Feature, Traits.Features.Classification)]
public partial class SyntacticClassifierTests
{
    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_01(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.Net.SDK
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("Net"),
            Punctuation.Text("."),
            Identifier("SDK"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_02(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.NET.Sdk.Web@10.0.100-preview.3
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("NET"),
            Punctuation.Text("."),
            Identifier("Sdk"),
            Punctuation.Text("."),
            Identifier("Web"),
            Punctuation.Text("@"),
            String("10.0.100-preview.3"));

    // sdk: @ separator with no version value after it
    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_03(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.NET.Sdk@
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("NET"),
            Punctuation.Text("."),
            Identifier("Sdk"),
            Punctuation.Text("@"));

    // sdk: duplicate @ separators
    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_04(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.NET.Sdk@1.0@extra
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("NET"),
            Punctuation.Text("."),
            Identifier("Sdk"),
            Punctuation.Text("@"),
            String("1.0@extra"));

    // sdk: no name, just @ and version
    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_05(TestHost testHost)
        => TestAsync("""
            #:sdk @1.0
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Punctuation.Text("@"),
            String("1.0"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_06(TestHost testHost)
        => TestAsync("""
            #:sdk Test 2.1.0
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Test 2"),
            Punctuation.Text("."),
            Identifier("1"),
            Punctuation.Text("."),
            Identifier("0"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task FileBasedApps_Sdk_07(TestHost testHost)
        => TestAsync($"""
            #:sdk{'\t'}Test 2.1.0
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            Identifier("Test 2"),
            Punctuation.Text("."),
            Identifier("1"),
            Punctuation.Text("."),
            Identifier("0"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task FileBasedApps_Package_01(TestHost testHost)
        => TestAsync("""
            #:package Newtonsoft.Json@13.0.3
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            Identifier("Newtonsoft"),
            Punctuation.Text("."),
            Identifier("Json"),
            Punctuation.Text("@"),
            String("13.0.3"));

    // package: @ separator with no version
    [Theory, CombinatorialData]
    public Task FileBasedApps_Package_02(TestHost testHost)
        => TestAsync("""
            #:package Newtonsoft.Json@
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            Identifier("Newtonsoft"),
            Punctuation.Text("."),
            Identifier("Json"),
            Punctuation.Text("@"));

    // package: no @ separator, name only
    [Theory, CombinatorialData]
    public Task FileBasedApps_Package_03(TestHost testHost)
        => TestAsync("""
            #:package Newtonsoft.Json
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            Identifier("Newtonsoft"),
            Punctuation.Text("."),
            Identifier("Json"));

    // package: duplicate @ separators
    [Theory, CombinatorialData]
    public Task FileBasedApps_Package_04(TestHost testHost)
        => TestAsync("""
            #:package Pkg@1.0@extra
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            Identifier("Pkg"),
            Punctuation.Text("@"),
            String("1.0@extra"));

    // package: no name, just @ and version
    [Theory, CombinatorialData]
    public Task FileBasedApps_Package_05(TestHost testHost)
        => TestAsync("""
            #:package @13.0.3
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            Punctuation.Text("@"),
            String("13.0.3"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Property_01(TestHost testHost)
        => TestAsync("""
            #:property LangVersion=preview
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Identifier("LangVersion"),
            Punctuation.Text("="),
            String("preview"));

    // property: no = separator, name only
    [Theory, CombinatorialData]
    public Task FileBasedApps_Property_02(TestHost testHost)
        => TestAsync("""
            #:property LangVersion
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Identifier("LangVersion"));

    // property: = separator with no value
    [Theory, CombinatorialData]
    public Task FileBasedApps_Property_03(TestHost testHost)
        => TestAsync("""
            #:property LangVersion=
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Identifier("LangVersion"),
            Punctuation.Text("="));

    // property: duplicate = separators
    [Theory, CombinatorialData]
    public Task FileBasedApps_Property_04(TestHost testHost)
        => TestAsync("""
            #:property Key=Value=Extra
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Identifier("Key"),
            Punctuation.Text("="),
            String("Value=Extra"));

    // property: no name, just = and value
    [Theory, CombinatorialData]
    public Task FileBasedApps_Property_05(TestHost testHost)
        => TestAsync("""
            #:property =preview
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Punctuation.Text("="),
            String("preview"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Project_01(TestHost testHost)
        => TestAsync("""
            #:project ../path/to/lib.csproj
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("project"),
            String("../path/to/lib.csproj"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Include_01(TestHost testHost)
        => TestAsync("""
            #:include src/**/*.cs
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("include"),
            String("src/**/*.cs"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Exclude_01(TestHost testHost)
        => TestAsync("""
            #:exclude obj/**
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("exclude"),
            String("obj/**"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_NoValue_01(TestHost testHost)
        => TestAsync("""
            #:sdk
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_NoValue_02(TestHost testHost)
        => TestAsync("""
            #:package
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_NoValue_03(TestHost testHost)
        => TestAsync("""
            #:property
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"));

    [Theory, CombinatorialData]
    public Task FileBasedApps_Unknown_01(TestHost testHost)
        => TestAsync("""
            #:unknown // comment
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("unknown"),
            String("// comment"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task FileBasedApps_Unknown_02(TestHost testHost)
        => TestAsync("""
            #:no-space
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("no-space"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    // Space between `#` and `:` causes the directive to not be treated as an ignored/FBA directive.
    [Theory, CombinatorialData]
    public Task FileBasedApps_Spaces_01(TestHost testHost)
        => TestAsync("""
            # :  property  name=value
            """,
            testHost,
            PPKeyword("#"),
            PPText(":  property  name=value"));

    // Space between `#:` and kind is allowed.
    [Theory, CombinatorialData]
    public Task FileBasedApps_Spaces_02(TestHost testHost)
        => TestAsync("""
            #:  property  name = value
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            Identifier("name "),
            PunctuationText("="),
            String(" value"));
}
