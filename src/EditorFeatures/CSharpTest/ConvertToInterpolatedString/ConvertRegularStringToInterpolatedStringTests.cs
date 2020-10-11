// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    public class ConvertRegularStringToInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertRegularStringToInterpolatedStringRefactoringProvider();

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
        public async Task TestMissingOnRegularStringWithBracesAssignedToConst()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        const string v = [||]""string {"";
    }
}");
        }
    }
}
