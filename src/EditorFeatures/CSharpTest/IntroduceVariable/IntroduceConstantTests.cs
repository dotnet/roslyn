// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceVariable
{
    public class IntroduceConstantTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new IntroduceVariableCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(47772, "https://github.com/dotnet/roslyn/issues/47772")]
        public async Task DonotIntroduceConstantForConstant_Local()
        {
            await TestMissingAsync(
@"
class C
{
    void M()
    {
        const int foo = [|10|];
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(47772, "https://github.com/dotnet/roslyn/issues/47772")]
        public async Task DonotIntroduceConstantForConstant_Member()
        {
            await TestMissingAsync(
@"
class C
{
    const int foo = [|10|];
}
");
        }
    }
}
