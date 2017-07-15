// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertIfToSwitch
{
    public class ConvertIfToSwitchTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertIfToSwitchCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestUnreachableEndPoint()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 1 || i == 2 || i == 3)
            return;
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
            case 2:
            case 3:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestReachableEndPoint()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 1 || i == 2 || i == 3)
            M(i);
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
            case 2:
            case 3:
                M(i);
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMultipleCases_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 1 || 2 == i || i == 3) M(0);
        else if (i == 4 || 5 == i || i == 6) M(1);
        else M(2);
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
            case 2:
            case 3:
                M(0);
                break;
            case 4:
            case 5:
            case 6:
                M(1);
                break;
            default:
                M(2);
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMultipleCases_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is string s && s.Length > 0) M(0);
        else if (o is int i && i > 0) M(1);
        else return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s when s.Length > 0:
                M(0);
                break;
            case int i when i > 0:
                M(1);
                break;
            default:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestExpressionOrder()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (1 == i || i == 2 || 3 == i)
            return;
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
            case 2:
            case 3:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestConstantExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        const int A = 1, B = 2, C = 3;
        [||]if (A == i || B == i || C == i)
            return;
    }
}",
@"class C
{
    void M(int i)
    {
        const int A = 1, B = 2, C = 3;
        switch (i)
        {
            case A:
            case B:
            case C:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingOnNonConstantExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        int A = 1, B = 2, C = 3;
        [||]if (A == i || B == i || C == i)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingOnDifferentOperands()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int i, int j)
    {
        [||]if (i == 5 || 6 == j) {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingOnSingleCase()
        {
            await TestMissingAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 5) {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestIsExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is int || o is string || o is C)
            return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case int _:
            case string _:
            case C _:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestIsPatternExpression_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is int i)
                return;
            else if (o is string s)
                return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case int i:
                return;
            case string s:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestIsPatternExpression_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is string s && s.Length == 5)
                return;
            else if (o is int i)
                return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s when s.Length == 5:
                return;
            case int i:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestIsPatternExpression_03()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is string s && (s.Length > 5 && s.Length < 10))
                return;
            else if (o is int i)
                return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s when s.Length > 5 && s.Length < 10:
                return;
            case int i:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestIsPatternExpression_04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is string s && s.Length > 5 && s.Length < 10)
                return;
            else if (o is int i)
                return;
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s when s.Length > 5 && s.Length < 10:
                return;
            case int i:
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestPreserveTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object o)
    {
        [||]if (o is string s && s.Length > 5 &&
                                 s.Length < 10)
            {
                M(o:   0);

            }
            else if (o is int i)
            {
                M(o:   0);
            }
    }
}",
@"class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s when s.Length > 5 &&
                                 s.Length < 10:
                M(o:   0);
                break;
            case int i:
                M(o:   0);
                break;
        }
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingIfCaretDoesntIntersectWithTheIfKeyword()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        if [||](i == 3) {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestKeepBlockIfThereIsVariableDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 3)
        {
            var x = i;
        }
        else if (i == 4)
        {
        }
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 3:
                {
                    var x = i;
                    break;
                }
            case 4:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingOnBreak_01()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        while (true)
        {
            [||]if (i == 5) break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestMissingOnBreak_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        while (true)
        {
            [||]if (i == 5) M(b, i);
            else break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestNestedBreak()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int i)
    {
        [||]if (i == 1)
        {
            while (true)
            {
                break;
            }
        }
        else if (i == 2)
        {
        }
    }
}",
@"class C
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
                while (true)
                {
                    break;
                }
                break;
            case 2:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int? i)
    {
        [||]if (i == null) return 5;
        if (i == 0) return 6;
        return 7;
    }
}",
@"class C
{
    int M(int? i)
    {
        switch (i)
        {
            case null:
                return 5;
            case 0:
                return 6;
        }
        return 7;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int? i)
    {
        [||]if (i == null) return 5;
        if (i == 0) {}
        if (i == 1) return 6;
        return 7;
    }
}",
@"class C
{
    int M(int? i)
    {
        switch (i)
        {
            case null:
               return 5;
            case 0:
                break;
        }
        if (i == 1) return 6;
        return 7;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_03()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int? i)
    {
        while (true)
        {
            [||]if (i == null) return 5; else if (i == 1) return 1;
            if (i == 0) break;
            if (i == 1) return 6;
            return 7;
        }
    }
}",
@"class C
{
    int M(int? i)
    {
        while (true)
        {
            switch (i)
            {
                case null:
                    return 5;
                case 1:
                    return 1;
            }
            if (i == 0) break;
            if (i == 1) return 6;
            return 7;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    string M(object i)
    {
        [||]if (i == null || i as string == """") return null;
        if ((string)i == ""0"") return i as string;
        else return i.ToString();
    }
}",
@"class C
{
    string M(object i)
    {
        switch (i)
        {
            case null:
            case """":
                return null;
            case ""0"":
                return i as string;
            default:
                return i.ToString();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_05()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int i)
    {
        [||]if (i == 10) return 5;
        if (i == 20) return 6;
        if (i == i) return 0;
        reuturn 7;
    }
}",
@"class C
{
    int M(int i)
    {
        switch (i)
        {
            case 10:
                return 5;
            case 20:
                return 6;
        }
        if (i == i) return 0;
        reuturn 7;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_06()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int i)
    {
        [||]if (i == 10)
        {
            return 5;
        }
        else if (i == 20)
        {
            return 6;
        }
        if (i == i) 
        {
            return 0;
        }
        reuturn 7;
    }
}",
@"class C
{
    int M(int i)
    {
        switch (i)
        {
            case 10:
                return 5;
            case 20:
                return 6;
        }
        if (i == i) 
        {
            return 0;
        }
        reuturn 7;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)]
        public async Task TestSubsequentIfStatements_07()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int i)
    {
        [||]if (i == 5)
        {
            return 4;
        }
        else if (i == 1)
        {
            return 1;
        }

        if (i == 10)
        {
            return 5;
        }
        else if (i == i)
        {
            return 6;
        }
        else
        {
            return 0;
        }
        reuturn 7;
    }
}",
@"class C
{
    int M(int i)
    {
        switch (i)
        {
            case 5:
                return 4;
            case 1:
                return 1;
        }
        if (i == 10)
        {
            return 5;
        }
        else if (i == i)
        {
            return 6;
        }
        else
        {
            return 0;
        }
        reuturn 7;
    }
}");
        }
    }
}
