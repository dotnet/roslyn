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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    public class ConvertRegularStringToInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ConvertRegularStringToInterpolatedStringRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnRegularStringWithNoBraces()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnRegularStringWithBraces()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string {"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string {{"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnRegularStringWithBracesAndEscapedCharacters()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string { \r\n \t"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string {{ \r\n \t"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnInterpolatedString()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var i = 0;
        var v = $[||]""string {i}"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnVerbatimStringWithBraces()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = @[||]""string
}"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""string
}}"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnVerbatimStringWithBracesAndEscapedQuotes()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = @[||]""string """"foo""""
}"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""string """"foo""""
}}"";
    }
}");
        }

        [WorkItem(52243, "https://github.com/dotnet/roslyn/issues/52243")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnRegularStringWithBracesAssignedToConstBeforeCSharp10()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        const string v = [||]""string {"";
    }
}", new(new CSharpParseOptions(LanguageVersion.CSharp9)));
        }

        [WorkItem(52243, "https://github.com/dotnet/roslyn/issues/52243")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnRegularStringWithBracesAssignedToConstForCSharp10AndNewer()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        const string v = [||]""string {"";
    }
}",
@"public class C
{
    void M()
    {
        const string v = $""string {{"";
    }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnUnterminatedStringWithBraces()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string {
    }
}");
        }

        [WorkItem(52243, "https://github.com/dotnet/roslyn/issues/52243")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnAttributeStringParameterWithBracesBeforeCSharp10()
        {
            await TestMissingInRegularAndScriptAsync(
@"[System.Diagnostics.DebuggerDisplay([||]""FirstName={FirstName}, LastName={LastName}"")]
public class C
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}", new(new CSharpParseOptions(LanguageVersion.CSharp9)));
        }

        [WorkItem(52243, "https://github.com/dotnet/roslyn/issues/52243")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestOnAttributeStringParameterWithBracesForCSharp10AndNewer()
        {
            await TestInRegularAndScriptAsync(
@"[System.Diagnostics.DebuggerDisplay([||]""FirstName={FirstName}, LastName={LastName}"")]
public class C
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}",
@"[System.Diagnostics.DebuggerDisplay($""FirstName={{FirstName}}, LastName={{LastName}}"")]
public class C
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnRegularStringWithBracesAndCursorOutOfBounds()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v [||]= ""string {"";
    }
}");
        }
    }
}
