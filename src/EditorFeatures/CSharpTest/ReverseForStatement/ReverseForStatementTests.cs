// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ReverseForStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReverseForStatement
{
    public class ReverseForStatementTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpReverseForStatementCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutInitializer()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (; i < args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutCondition()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; ; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutIncrementor()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; )
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutVariableReferencedInCondition()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; j < args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutVariableReferencedInIncrementor()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; j++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithoutVariableInitializer()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i; i < args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithMismatchedConditionAndIncrementor1()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; i--)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithMismatchedConditionAndIncrementor2()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i >= args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostIncrement1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; i++)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length - 1; i >= 0; i--)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestIncrementPreIncrement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; ++i)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length - 1; i >= 0; --i)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestIncrementAddAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; i += 1)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length - 1; i >= 0; i -= 1)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestMissingWithNonOneIncrementValue()
        {
            await TestMissingAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i < args.Length; i += 2)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostDecrement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = args.Length - 1; i >= 0; i--)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostIncrementEquals1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; i <= args.Length; i++)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length; i >= 0; i--)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostDecrementEquals()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = args.Length; i >= 0; i--)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = 0; i <= args.Length; i++)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (/*t1*/int/*t2*/i/*t3*/=/*t4*/0/*t5*/;/*t6*/i/*t7*/</*t8*/args.Length/*t9*/;/*t10*/i/*t11*/++/*t12*/)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (/*t1*/int/*t2*/i/*t3*/=/*t4*/args.Length/*t9*/- 1;/*t6*/i/*t7*/>=/*t8*/0/*t5*/;/*t10*/i/*t11*/--/*t12*/)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostIncrementSwappedConditions()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; args.Length > i; i++)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length - 1; 0 <= i; i--)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public async Task TestPostIncrementEqualsSwappedConditions()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(string[] args)
    {
        [||]for (int i = 0; args.Length >= i; i++)
        {
        }
    }
}",
@"class C
{
    void M(string[] args)
    {
        for (int i = args.Length; 0 <= i; i--)
        {
        }
    }
}");
        }
    }
}
