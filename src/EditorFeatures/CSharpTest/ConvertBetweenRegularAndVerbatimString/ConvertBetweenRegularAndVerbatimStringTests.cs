// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertForEachToFor
{
    public class ConvertBetweenRegularAndVerbatimStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
        public async Task EmptyRegularString()
        {
            await TestMissingAsync(@"
class Test
{
    void Method()
    {
        var v = ""[||]"";
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
        public async Task EmptyVerbatimString()
        {
            await TestInRegularAndScript1Async(@"
class Test
{
    void Method()
    {
        var v = @""[||]"";
    }
}
",
@"
class Test
{
    void Method()
    {
        var v = """";
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
        public async Task RegularStringWithBasicText()
        {
            await TestMissingAsync(@"
class Test
{
    void Method()
    {
        var v = ""[||]a"";
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
        public async Task VerbatimStringWithBasicText()
        {
            await TestInRegularAndScript1Async(@"
class Test
{
    void Method()
    {
        var v = @""[||]a"";
    }
}
",
@"
class Test
{
    void Method()
    {
        var v = ""a"";
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
        public async Task RegularStringWithUnicodeEscape()
        {
            await TestMissingAsync(@"
class Test
{
    void Method()
    {
        var v = ""[||]\u0001"";
    }
}
");
        }
    }
}
