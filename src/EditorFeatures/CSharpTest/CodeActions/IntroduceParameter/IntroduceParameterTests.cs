// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceVariable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
                    void M(int x, int y, int z, {|Rename:int v|}) 
                    {
                        int m = v;
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
                    void M(int x, int y, int z, {|Rename:int v|})
                    {
                        int m = v;
                    }

                    void M1(int x, int y, int z) 
                    {
                        M(z, y, x);
                    }
                }";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
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
                    void M(int x, int y, int z, {|Rename:int v|})
                    {
                        int m = v;
                    }

                    void M1(int x, int y, int z) 
                    {
                        M(a + b, 5, x);
                        M(z, y, x);
                    }
                }";

            await TestInRegularAndScriptAsync(code, expected, index: 0);
        }
    }
}
