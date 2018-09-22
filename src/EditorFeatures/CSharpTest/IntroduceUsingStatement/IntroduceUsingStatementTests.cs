// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceUsingStatement
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceUsingStatement)]
    public sealed class IntroduceUsingStatementTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpIntroduceUsingStatementCodeRefactoringProvider();

        [Theory]
        [InlineData("[|\r\n        var name = disposable; |]")]
        [InlineData("v[||]ar name = disposable;")]
        [InlineData("var[||] name = disposable;")]
        [InlineData("var [||]name = disposable;")]
        [InlineData("var na[||]me = disposable;")]
        [InlineData("var name[||] = disposable;")]
        [InlineData("var name [||]= disposable;")]
        [InlineData("var name =[||] disposable;")]
        [InlineData("var name = [||]disposable;")]
        [InlineData("[|var name = disposable;|]")]
        [InlineData("var name = disposable[||];")]
        [InlineData("var name = disposable;[||]")]
        [InlineData("var name = disposable[||]")]
        public async Task RefactoringIsAvailableForSelection(string declaration)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        " + declaration + @"
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var name = disposable)
        {
        }
    }
}");
        }

        [Theory]
        [InlineData("var name = d[||]isposable;")]
        [InlineData("var name = disposabl[||]e;")]
        [InlineData("var name=[|disposable|];")]
        [InlineData("System.IDisposable name =[||]")]
        public async Task RefactoringIsNotAvailableForSelection(string declaration)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        " + declaration + @"
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForConstrainedGenericTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"class C<T> where T : System.IDisposable
{
    void M(T disposable)
    {
        var x = disposable;[||]
    }
}",
@"class C<T> where T : System.IDisposable
{
    void M(T disposable)
    {
        using (var x = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsNotAvailableForUnconstrainedGenericTypeParameter()
        {
            await TestMissingAsync(
@"class C<T>
{
    void M(T disposable)
    {
        var x = disposable;[||]
    }
}");
        }
    }
}
