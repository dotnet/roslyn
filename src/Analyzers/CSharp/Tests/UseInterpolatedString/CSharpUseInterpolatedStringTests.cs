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
    public async Task KeepInterpolatedStrings()
    {
        const string CodeWithInterpolatedString = """
            class Program
            {
                private string MyString => $"My{42}String";
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(CodeWithInterpolatedString, CodeWithInterpolatedString);
    }

    [Fact]
    public async Task KeepInterpolatedVerbatimStrings()
    {
        const string CodeWithInterpolatedVerbatimStrings = """
            class Program
            {
                private string MyString1 => @$"My{42}String";
                private string MyString2 => $@"My{42}String";
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(CodeWithInterpolatedVerbatimStrings, CodeWithInterpolatedVerbatimStrings);
    }

    [Fact]
    public async Task KeepInterpolatedSingleLineRawStrings()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task KeepInterpolatedMultiLineRawStrings()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task FixRegularStrings()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class Program
                {
                    private string MyString1 => [|"MyString"|];
                    private string MyString2 => [|"My{String"|];
                    private string MyString3 => [|"My}String"|];
                    private string MyString4 => [|"My{S}tring"|];
                    private string MyString5 => [|"My}S{tring"|];
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
        }.RunAsync();
    }

    [Fact]
    public async Task FixVerbatimStrings()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class Program
                {
                    private string MyString1 => [|@"MyString"|];
                    private string MyString2 => [|@"My{String"|];
                    private string MyString3 => [|@"My}String"|];
                    private string MyString4 => [|@"My{S}tring"|];
                    private string MyString5 => [|@"My}S{tring"|];
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
        }.RunAsync();
    }

    [Fact]
    public async Task FixSingleLineRawStrings()
    {
        await new VerifyCS.Test
        {
            TestCode = """"
                class Program
                {
                    private string MyString1 => [|"""MyString"""|];
                    private string MyString2 => [|"""My{String"""|];
                    private string MyString3 => [|"""My}String"""|];
                    private string MyString4 => [|"""My{S}tring"""|];
                    private string MyString5 => [|"""My}S{tring"""|];
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
        }.RunAsync();
    }

    [Fact]
    public async Task FixMultiLineRawStrings()
    {
        await new VerifyCS.Test
        {
            TestCode = """"
                class Program
                {
                    private string MyString1 => [|"""
                        MyRawString
                        """|];
                    private string MyString2 => [|"""
                        My{String
                        """|];
                    private string MyString3 => [|"""
                        My}String
                        """|];
                    private string MyString4 => [|"""
                        My{S}tring
                        """|];
                    private string MyString5 => [|"""
                        My}S{tring
                        """|];
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
        }.RunAsync();
    }
}
