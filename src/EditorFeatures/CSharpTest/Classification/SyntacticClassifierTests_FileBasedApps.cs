// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

[Trait(Traits.Feature, Traits.Features.Classification)]
public partial class SyntacticClassifierTests
{
    [Theory, CombinatorialData]
    public Task FBA_Sdk_01(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.Net.SDK
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            String(" "),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("Net"),
            Punctuation.Text("."),
            Identifier("SDK"));

    [Theory, CombinatorialData]
    public Task FBA_Sdk_02(TestHost testHost)
        => TestAsync("""
            #:sdk Microsoft.NET.Sdk.Web@10.0.100-preview.3
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            String(" "),
            Identifier("Microsoft"),
            Punctuation.Text("."),
            Identifier("NET"),
            Punctuation.Text("."),
            Identifier("Sdk"),
            Punctuation.Text("."),
            Identifier("Web"),
            Punctuation.Text("@"),
            String("10.0.100-preview.3"));

    [Theory, CombinatorialData]
    public Task FBA_Package_01(TestHost testHost)
        => TestAsync("""
            #:package Newtonsoft.Json@13.0.3
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("package"),
            String(" "),
            Identifier("Newtonsoft"),
            Punctuation.Text("."),
            Identifier("Json"),
            Punctuation.Text("@"),
            String("13.0.3"));

    [Theory, CombinatorialData]
    public Task FBA_Property_01(TestHost testHost)
        => TestAsync("""
            #:property LangVersion=preview
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("property"),
            String(" "),
            Identifier("LangVersion"),
            Punctuation.Text("="),
            String("preview"));

    [Theory, CombinatorialData]
    public Task FBA_Project_01(TestHost testHost)
        => TestAsync("""
            #:project ../path/to/lib.csproj
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("project"),
            String(" ../path/to/lib.csproj"));

    [Theory, CombinatorialData]
    public Task FBA_Include_01(TestHost testHost)
        => TestAsync("""
            #:include src/**/*.cs
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("include"),
            String(" src/**/*.cs"));

    [Theory, CombinatorialData]
    public Task FBA_Exclude_01(TestHost testHost)
        => TestAsync("""
            #:exclude obj/**
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("exclude"),
            String(" obj/**"));
}