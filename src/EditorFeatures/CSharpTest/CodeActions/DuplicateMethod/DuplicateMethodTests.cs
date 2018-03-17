// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.DuplicateMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.DuplicateMethod
{
    public class ConvertIfToSwitchTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpDuplicateMethodCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsDuplicateMethod)]
        public async Task TestDuplicateMethodWithAttributes()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsDuplicateMethod), Skip("""")]
    [Fact]
    public async Task [|M009|](int i)
    {
        await TestInRegularAndScriptAsync("""");
    }
}",
@"class C
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsDuplicateMethod), Skip("""")]
    [Fact]
    public async Task M009(int i)
    {
        await TestInRegularAndScriptAsync("""");
    }
    [Trait(Traits.Feature, Traits.Features.CodeActionsDuplicateMethod), Skip("""")]
    [Fact]
    public async Task M010(int i)
    {
        await TestInRegularAndScriptAsync("""");
    }
}", ignoreTrivia: false);
        }
    }
}
