// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseInterpolatedString;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseInterpolatedString;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseInterpolatedStringDiagnosticAnalyzer,
    CSharpUseInterpolatedStringCodeFixProvider>;

public sealed class CSharpUseInterpolatedStringTests
{
    [Fact]
    public Task KeepInterpolatedStrings() => new VerifyCS.Test
    {
        TestCode = """
        class Program
        {
            private string MyString => $"My{42}String";
        }
        """,
    }.RunAsync();

    [Fact]
    public Task KeepInterpolatedVerbatimStrings() => new VerifyCS.Test
    {
        TestCode = """
        class Program
        {
            private string MyString1 => @$"My{42}String";
            private string MyString2 => $@"My{42}String";
        }
        """,
    }.RunAsync();

    [Fact]
    public Task KeepInterpolatedSingleLineRawStrings() => new VerifyCS.Test
    {
        TestCode = """"
        class Program
        {
            private string MyString1 => $"""My{42}String""";
            private string MyString2 => $"""My{42}String""";
        }
        """",
        LanguageVersion = LanguageVersion.CSharp11,
    }.RunAsync();

    [Fact]
    public Task KeepInterpolatedMultiLineRawStrings() => new VerifyCS.Test
    {
        TestCode = """"
        class Program
        {
            private string MyString1 => $"""
                My{42}String
                """;
            private string MyString2 => $"""
                My{42}String
                """;
        }
        """",
        LanguageVersion = LanguageVersion.CSharp11,
    }.RunAsync();

    [Fact]
    public Task FixRegularStrings() => new VerifyCS.Test
    {
        TestCode = """
        class Program
        {
            private string MyString1 => "MyString";
            private string MyString2 => "My{String";
            private string MyString3 => "My}String";
            private string MyString4 => "My{S}tring";
            private string MyString5 => "My}S{tring";
        }
        """,
        FixedCode = """
        class Program
        {
            private string MyString1 => $"MyString";
            private string MyString2 => $"My{{String";
            private string MyString3 => $"My}}String";
            private string MyString4 => $"My{{S}}tring";
            private string MyString5 => $"My}}S{{tring";
        }
        """,
        ExpectedDiagnostics =
        {
            // /0/Test0.cs(3,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(3, 33, 3, 43),
            // /0/Test0.cs(4,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(4, 33, 4, 44),
            // /0/Test0.cs(5,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(5, 33, 5, 44),
            // /0/Test0.cs(6,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(6, 33, 6, 45),
            // /0/Test0.cs(7,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(7, 33, 7, 45),
        }
    }.RunAsync();

    [Fact]
    public Task FixVerbatimStrings() => new VerifyCS.Test
    {
        TestCode = """
        class Program
        {
            private string MyString1 => @"MyString";
            private string MyString2 => @"My{String";
            private string MyString3 => @"My}String";
            private string MyString4 => @"My{S}tring";
            private string MyString5 => @"My}S{tring";
        }
        """,
        FixedCode = """
        class Program
        {
            private string MyString1 => $@"MyString";
            private string MyString2 => $@"My{{String";
            private string MyString3 => $@"My}}String";
            private string MyString4 => $@"My{{S}}tring";
            private string MyString5 => $@"My}}S{{tring";
        }
        """,
        ExpectedDiagnostics =
        {
            // /0/Test0.cs(3,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(3, 33, 3, 44),
            // /0/Test0.cs(4,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(4, 33, 4, 45),
            // /0/Test0.cs(5,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(5, 33, 5, 45),
            // /0/Test0.cs(6,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(6, 33, 6, 46),
            // /0/Test0.cs(7,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(7, 33, 7, 46),
        }
    }.RunAsync();

    [Fact]
    public Task FixSingleLineRawStrings() => new VerifyCS.Test
    {
        TestCode = """"
        class Program
        {
            private string MyString1 => """MyString""";
            private string MyString2 => """My{String""";
            private string MyString3 => """My}String""";
            private string MyString4 => """My{S}tring""";
            private string MyString5 => """My}S{tring""";
        }
        """",
        FixedCode = """"
        class Program
        {
            private string MyString1 => $"""MyString""";
            private string MyString2 => $"""My{{String""";
            private string MyString3 => $"""My}}String""";
            private string MyString4 => $"""My{{S}}tring""";
            private string MyString5 => $"""My}}S{{tring""";
        }
        """",
        LanguageVersion = LanguageVersion.CSharp11,
        ExpectedDiagnostics =
        {
            // /0/Test0.cs(3,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(3, 33, 3, 47),
            // /0/Test0.cs(4,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(4, 33, 4, 48),
            // /0/Test0.cs(5,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(5, 33, 5, 48),
            // /0/Test0.cs(6,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(6, 33, 6, 49),
            // /0/Test0.cs(7,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(7, 33, 7, 49),
        }
    }.RunAsync();

    [Fact]
    public Task FixMultiLineRawStrings() => new VerifyCS.Test
    {
        TestCode = """"
        class Program
        {
            private string MyString1 => """
                MyRawString
                """;
            private string MyString2 => """
                My{String
                """;
            private string MyString3 => """
                My}String
                """;
            private string MyString4 => """
                My{S}tring
                """;
            private string MyString5 => """
                My}S{tring
                """;
        }
        """",
        FixedCode = """"
        class Program
        {
            private string MyString1 => $"""
                MyRawString
                """;
            private string MyString2 => $"""
                My{{String
                """;
            private string MyString3 => $"""
                My}}String
                """;
            private string MyString4 => $"""
                My{{S}}tring
                """;
            private string MyString5 => $"""
                My}}S{{tring
                """;
        }
        """",
        LanguageVersion = LanguageVersion.CSharp11,
        ExpectedDiagnostics =
        {
            // /0/Test0.cs(3,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(3, 33, 5, 12),
            // /0/Test0.cs(6,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(6, 33, 8, 12),
            // /0/Test0.cs(9,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(9, 33, 11, 12),
            // /0/Test0.cs(12,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(12, 33, 14, 12),
            // /0/Test0.cs(15,33): hidden IDE0330: String can be converted to interpolated string
            VerifyCS.Diagnostic().WithSpan(15, 33, 17, 12),
        }
    }.RunAsync();
}
