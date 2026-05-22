// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class CSharpCodeActionTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task GenerateConstructor()
    {
        var input = """

            <div></div>

            @code
            {
                public class [||]Goo
                {
                }
            }

            """;

        var expected = """
            
            <div></div>
            
            @code
            {
                public class Goo
                {
                    public Goo()
                    {
                    }
                }
            }

            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers);
    }

    [Fact]
    public async Task UseExpressionBodiedMember()
    {
        var input = """
            @using System.Linq

            <div></div>

            @code
            {
                [|{|selection:|}void M(string[] args)|]
                {
                    args.ToString();
                }
            }

            """;

        var expected = """
            @using System.Linq

            <div></div>
            
            @code
            {
                void M(string[] args) => args.ToString();
            }

            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.UseExpressionBody);
    }

    [Fact]
    public async Task IntroduceLocal()
    {
        var input = """
            @using System.Linq

            <div></div>

            @code
            {
                void M(string[] args)
                {
                    if (args.First()[||].Length > 0)
                    {
                    }
                    if (args.First().Length > 0)
                    {
                    }
                }
            }

            """;

        var expected = """
            @using System.Linq

            <div></div>
            
            @code
            {
                void M(string[] args)
                {
                    int length = args.First().Length;
                    if (length > 0)
                    {
                    }
                    if (args.First().Length > 0)
                    {
                    }
                }
            }

            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.IntroduceVariable);
    }

    [Fact]
    public async Task IntroduceLocal_All()
    {
        var input = """
            @using System.Linq

            <div></div>

            @code
            {
                void M(string[] args)
                {
                    if (args.First()[||].Length > 0)
                    {
                    }
                    if (args.First().Length > 0)
                    {
                    }
                }
            }

            """;

        var expected = """
            @using System.Linq

            <div></div>
            
            @code
            {
                void M(string[] args)
                {
                    int length = args.First().Length;
                    if (length > 0)
                    {
                    }
                    if (length > 0)
                    {
                    }
                }
            }

            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.IntroduceVariable, childActionIndex: 1);
    }

    [Fact]
    public async Task ConvertConcatenationToInterpolatedString_CSharpStatement()
    {
        var input = """
            @{
                var x = "he[||]l" + "lo" + Environment.NewLine + "world";
            }
            """;

        var expected = """
            @{
                var x = $"hello{Environment.NewLine}world";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString);
    }

    [Fact]
    public async Task ConvertConcatenationToInterpolatedString_ExplicitExpression()
    {
        var input = """
            @("he[||]l" + "lo" + Environment.NewLine + "world")
            """;

        var expected = """
            @($"hello{Environment.NewLine}world")
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString);
    }

    [Fact]
    public async Task ConvertConcatenationToInterpolatedString_CodeBlock()
    {
        var input = """
            @code
            {
                private string _x = "he[||]l" + "lo" + Environment.NewLine + "world";
            }
            """;

        var expected = """
            @code
            {
                private string _x = $"hello{Environment.NewLine}world";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString);
    }

    [Fact]
    public async Task ConvertBetweenRegularAndVerbatimInterpolatedString_CodeBlock()
    {
        var input = """
            @code
            {
                private string _x = $@"h[||]ello world";
            }
            """;

        var expected = """
            @code
            {
                private string _x = $"hello world";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString);
    }

    [Fact]
    public async Task ConvertBetweenRegularAndVerbatimInterpolatedString_CodeBlock2()
    {
        var input = """
            @code
            {
                private string _x = $"h[||]ello\\nworld";
            }
            """;

        var expected = """
            @code
            {
                private string _x = $@"hello\nworld";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString);
    }

    [Fact]
    public async Task ConvertBetweenRegularAndVerbatimString_CodeBlock()
    {
        var input = """
            @code
            {
                private string _x = @"h[||]ello world";
            }
            """;

        var expected = """
            @code
            {
                private string _x = "hello world";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString);
    }

    [Fact]
    public async Task ConvertBetweenRegularAndVerbatimString_CodeBlock2()
    {
        var input = """
            @code
            {
                private string _x = "h[||]ello\\nworld";
            }
            """;

        var expected = """
            @code
            {
                private string _x = @"hello\nworld";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString);
    }

    [Fact]
    public async Task ConvertPlaceholderToInterpolatedString_CodeBlock()
    {
        var input = """
            @code
            {
                private string _x = [|string.Format("hello{0}world", Environment.NewLine)|];
            }
            """;

        var expected = """
            @code
            {
                private string _x = $"hello{Environment.NewLine}world";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString);
    }

    [Fact]
    public async Task ConvertToInterpolatedString_CodeBlock()
    {
        var input = """
            @code
            {
                private string _x = [||]"hello {";
            }
            """;

        var expected = """
            @code
            {
                private string _x = $"hello {{";
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString);
    }

    [Fact]
    public async Task AddDebuggerDisplay()
    {
        var input = """
            @code {
                class Goo[||]
                {
                    
                }
            }
            """;

        var expected = """
            @using System.Diagnostics
            @code {
                [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
                class Goo
                {
                    private string GetDebuggerDisplay()
                    {
                        return ToString();
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.AddDebuggerDisplay);
    }

    [Fact]
    public async Task AddDebuggerDisplay_Legacy()
    {
        var input = """
            @functions {
                class Goo[||]
                {
                    
                }
            }
            """;

        var expected = """
            @using System.Diagnostics
            @functions {
                [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
                class Goo
                {
                    private string GetDebuggerDisplay()
                    {
                        return ToString();
                    }
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.AddDebuggerDisplay, fileKind: AspNetCore.Razor.Language.RazorFileKind.Legacy);
    }

    [Fact]
    public async Task AddDebuggerDisplay_CodeBlockOnSingleLine()
    {
        var input = """
            @code { class Goo[||] { } }
            """;

        var expected = """
            @using System.Diagnostics
            @code {
                [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
                class Goo {
                    private string GetDebuggerDisplay()
                    {
                        return ToString();
                    }
                } 
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeRefactoringProviderNames.AddDebuggerDisplay);
    }

    [Fact]
    public async Task AddDebuggerDisplay_FunctionsBlockOnSingleLine_Legacy()
    {
        var input = """
            @functions { class Goo[||] { } }
            """;

        var expected = """
            @using System.Diagnostics
            @functions {
                [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
                class Goo {
                    private string GetDebuggerDisplay()
                    {
                        return ToString();
                    }
                } 
            }
            """;

        await VerifyCodeActionAsync(
            input,
            expected,
            RazorPredefinedCodeRefactoringProviderNames.AddDebuggerDisplay,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task RemoveUnusedVariable_Local()
    {
        var input = """
            @code
            {
                void M()
                {
                    int {|IDE0059:[||]x|} = 5;
                }
            }
            """;

        var expected = """
            @code
            {
                void M()
                {
                }
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.RemoveUnusedVariable);
    }

    [Fact]
    public async Task RemoveUnusedVariable_ExplicitStatement_SingleLine()
    {
        var input = """
            @{ var {|IDE0059:[||]x|} = 1; var y = 2; }
            """;

        var expected = """
            @{
                var y = 2; 
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.RemoveUnusedVariable);
    }

    [Fact]
    public async Task RemoveUnusedVariable_ExplicitStatement_SingleLine_OnlyVariable()
    {
        var input = """
            @{ var {|IDE0059:[||]message|} = "Hello World"; }
            """;

        var expected = """
            @{}
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.RemoveUnusedVariable);
    }

    [Fact]
    public async Task RemoveUnusedVariable_ExplicitStatement_MultiLine()
    {
        var input = """
            @{
                var {|IDE0059:[||]x|} = 1;
                var y = 2;
            }
            """;

        var expected = """
            @{
                var y = 2;
            }
            """;

        await VerifyCodeActionAsync(input, expected, RazorPredefinedCodeFixProviderNames.RemoveUnusedVariable);
    }
}
