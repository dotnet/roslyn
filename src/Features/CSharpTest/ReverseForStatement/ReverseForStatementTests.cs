// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ReverseForStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReverseForStatement
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
    public class ReverseForStatementTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new CSharpReverseForStatementCodeRefactoringProvider();

        [Fact]
        public async Task TestMissingWithoutInitializer()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (; i < args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutCondition()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; ; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutIncrementor()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; )
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutVariableReferencedInCondition()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; j < args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutVariableReferencedInIncrementor()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; j++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutVariableInitializer()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i; i < args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithMismatchedConditionAndIncrementor1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithMismatchedConditionAndIncrementor2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i >= args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostIncrement1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length - 1; i >= 0; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostIncrementConstants1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < 10; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = 10 - 1; i >= 0; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostDecrementConstants1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 10 - 1; i >= 0; i--)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestIncrementPreIncrement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; ++i)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length - 1; i >= 0; --i)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestIncrementAddAssignment()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; i += 1)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length - 1; i >= 0; i -= 1)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithNonOneIncrementValue()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i < args.Length; i += 2)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostDecrement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = args.Length - 1; i >= 0; i--)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = 0; i < args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostIncrementEquals1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; i <= args.Length; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length; i >= 0; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostDecrementEquals()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = args.Length; i >= 0; i--)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = 0; i <= args.Length; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (/*t1*/int/*t2*/i/*t3*/=/*t4*/0/*t5*/;/*t6*/i/*t7*/</*t8*/args.Length/*t9*/;/*t10*/i/*t11*/++/*t12*/)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (/*t1*/int/*t2*/i/*t3*/=/*t4*/args.Length/*t9*/- 1;/*t6*/i/*t7*/>=/*t8*/0/*t5*/;/*t10*/i/*t11*/--/*t12*/)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostIncrementSwappedConditions()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; args.Length > i; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length - 1; 0 <= i; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestPostIncrementEqualsSwappedConditions()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (int i = 0; args.Length >= i; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (int i = args.Length; 0 <= i; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestByteOneMin()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (byte i = 1; i <= 10; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (byte i = 10; i >= 1; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt16OneMin()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ushort i = 1; i <= 10; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (ushort i = 10; i >= 1; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt32OneMin()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (uint i = 1; i <= 10; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (uint i = 10; i >= 1; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt64OneMin()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ulong i = 1; i <= 10; i++)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(string[] args)
                    {
                        for (ulong i = 10; i >= 1; i--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestByteZeroMin()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (byte i = 0; i <= 10; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt16ZeroMin()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ushort i = 0; i <= 10; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt32ZeroMin()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (uint i = 0; i <= 10; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt64ZeroMin()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ulong i = 0; i <= 10; i++)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestByteMax()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (byte x = byte.MaxValue; x >= 10; x--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt16Max()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ushort x = ushort.MaxValue; x >= 10; x--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt32Max()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (uint x = uint.MaxValue; x >= 10; x--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUInt64Max()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (ulong x = ulong.MaxValue; x >= 10; x--)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestByteZeroMinReverse()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string[] args)
                    {
                        [||]for (byte i = 10; i >= 0; i--)
                        {
                        }
                    }
                }
                """);
        }
    }
}
