// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
public sealed class ConvertRegularStringToInterpolatedStringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ConvertRegularStringToInterpolatedStringRefactoringProvider();

    [Fact]
    public Task TestMissingOnRegularStringWithNoBraces()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]"string";
                }
            }
            """);

    [Fact]
    public Task TestOnRegularStringWithBraces()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]"string {";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string {{";
                }
            }
            """);

    [Fact]
    public Task TestOnRegularStringWithBracesAndEscapedCharacters()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]"string { \r\n \t";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string {{ \r\n \t";
                }
            }
            """);

    [Fact]
    public Task TestMissingOnInterpolatedString()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var i = 0;
                    var v = $[||]"string {i}";
                }
            }
            """);

    [Fact]
    public Task TestOnVerbatimStringWithBraces()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = @[||]"string
            }";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $@"string
            }}";
                }
            }
            """);

    [Fact]
    public Task TestOnVerbatimStringWithBracesAndEscapedQuotes()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = @[||]"string ""foo""
            }";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $@"string ""foo""
            }}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52243")]
    public Task TestMissingOnRegularStringWithBracesAssignedToConstBeforeCSharp10()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    const string v = [||]"string {";
                }
            }
            """, new(new CSharpParseOptions(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52243")]
    public Task TestOnRegularStringWithBracesAssignedToConstForCSharp10AndNewer()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    const string v = [||]"string {";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    const string v = $"string {{";
                }
            }
            """, new(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10)));

    [Fact]
    public Task TestMissingOnUnterminatedStringWithBraces()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]"string {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52243")]
    public Task TestMissingOnAttributeStringParameterWithBracesBeforeCSharp10()
        => TestMissingInRegularAndScriptAsync(
            """
            [System.Diagnostics.DebuggerDisplay([||]"FirstName={FirstName}, LastName={LastName}")]
            public class C
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
            """, new(new CSharpParseOptions(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52243")]
    public Task TestOnAttributeStringParameterWithBracesForCSharp10AndNewer()
        => TestInRegularAndScriptAsync(
            """
            [System.Diagnostics.DebuggerDisplay([||]"FirstName={FirstName}, LastName={LastName}")]
            public class C
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
            """,
            """
            [System.Diagnostics.DebuggerDisplay($"FirstName={{FirstName}}, LastName={{LastName}}")]
            public class C
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
            """, new(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10)));

    [Fact]
    public Task TestMissingOnRegularStringWithBracesAndCursorOutOfBounds()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                void M()
                {
                    var v [||]= "string {";
                }
            }
            """);
}
