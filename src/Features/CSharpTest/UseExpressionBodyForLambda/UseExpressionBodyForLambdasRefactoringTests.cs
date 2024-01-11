// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyForLambdasRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new UseExpressionBodyForLambdaCodeRefactoringProvider();

        private OptionsCollection UseExpressionBody
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);

        private OptionsCollection UseExpressionBodyDisabledDiagnostic
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private OptionsCollection UseBlockBody
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement);

        private OptionsCollection UseBlockBodyDisabledDiagnostic
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        [Fact]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [|=>|]
                        {
                            return x.ToString();
                        }
                    }
                }
                """, parameters: new TestParameters(options: UseExpressionBody));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [||]=>
                        {
                            return x.ToString();
                        };
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x => x.ToString();
                    }
                }
                """, parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [||]=>
                        {
                            return x.ToString();
                        };
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x => x.ToString();
                    }
                }
                """, parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task TestNotOfferedInMethod()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int [|Goo|]()
                    {
                        return 1;
                    }
                }
                """, parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [||]=> x.ToString();
                    }
                }
                """, parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [||]=> x.ToString();
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x =>
                        {
                            return x.ToString();
                        };
                    }
                }
                """, parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x [||]=> x.ToString();
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<int, string> f = x =>
                        {
                            return x.ToString();
                        };
                    }
                }
                """, parameters: new TestParameters(options: UseExpressionBody));
        }
    }
}
