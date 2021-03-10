// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.IntroduceVariable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.IntroduceParameter
{
    public class IntroduceParameterTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpIntroduceParameterService();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => GetNestedActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithNoMethodCallsCase()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z) 
    {
        int m = [|x * y * z;|]
    }
}";

            var expected =
@"using System;
class TestClass
{
    void M(int x, int y, int z, int v) 
    {
        int m = {|Rename:v|};
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionCaseWithLocal()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z) 
    {
        int l = 5;
        int m = [|l * y * z;|]
    }
}";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionCaseWithSingleMethodCall()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x, z * y* x);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionCaseWithSingleMethodCallInLocalFunction()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = M2(x, z);
                        
        int M2(int x, int y)
        {
            int val = [|x * y|];
            return val;
        }
    }
}";

            var expected =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = M2(x, z, x * z);
                        
        int M2(int x, int y, int v)
        {
            int val = {|Rename:v|};
            return val;
        }
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestHighlightIncompleteExpressionCaseWithSingleMethodCall()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = 5 * [|x * y * z|];
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionCaseWithMultipleMethodCall()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
    }

    void M1(int x, int y, int z) 
    {
        M(a + b, 5, x);
        M(z, y, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(a + b, 5, x, (a + b)* 5* x);
        M(z, y, x, z * y* x);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionAllOccurrences()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
        int f = x * y * z;
    }

    void M1(int x, int y, int z) 
    {
        M(a + b, 5, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
        int f = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(a + b, 5, x, (a + b)* 5* x);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithNoMethodCallTrampoline()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z) 
    {
        int m = [|x * y * z;|]
    }
}";

            var expected =
@"using System;
class TestClass
{
    private int M_v(int x, int y, int z)
    {
        return x * y * z;
    }

    void M(int x, int y, int z, int v) 
    {
        int m = {|Rename:v|};
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithSingleMethodCallTrampoline()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    private int M_v(int x, int y, int z)
    {
        return x * y * z;
    }

    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x, M_v(z, y, x));
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithSingleMethodCallTrampolineAllOccurrences()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
        int l = x * y * z;
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    private int M_v(int x, int y, int z)
    {
        return x * y * z;
    }

    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
        int l = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x, M_v(z, y, x));
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithNoMethodCallOverload()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z) 
    {
        int m = [|x * y * z;|]
    }
}";

            var expected =
@"using System;
class TestClass
{
    private void M(int x, int y, int z)
    {
        M(x, y, z, x * y * z);
    }

    void M(int x, int y, int z, int v) 
    {
        int m = {|Rename:v|};
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionWithSingleMethodCallOverload()
        {
            var code =
@"using System;
class TestClass
{
    void M(int x, int y, int z)
    {
        int m = [|x * y * z|];
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            var expected =
@"using System;
class TestClass
{
    private void M(int x, int y, int z)
    {
        M(x, y, z, x * y * z);
    }

    void M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
    }

    void M1(int x, int y, int z) 
    {
        M(z, y, x);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
        public async Task TestSimpleExpressionCaseWithRecursiveCall()
        {
            var code =
@"using System;
class TestClass
{
    int M(int x, int y, int z)
    {
        int m = [|x * y * z|];
        return M(x, x, z);
    }
}";

            var expected =
@"using System;
class TestClass
{
    int M(int x, int y, int z, int v)
    {
        int m = {|Rename:v|};
        return M(x, x, z, x * x* z);
    }
}";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }
    }
}
