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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReverseForStatement;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
public sealed class ReverseForStatementTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpReverseForStatementCodeRefactoringProvider();

    [Fact]
    public Task TestMissingWithoutInitializer()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutCondition()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutIncrementor()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutVariableReferencedInCondition()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutVariableReferencedInIncrementor()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutVariableInitializer()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithMismatchedConditionAndIncrementor1()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithMismatchedConditionAndIncrementor2()
        => TestMissingAsync(
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

    [Fact]
    public Task TestPostIncrement1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostIncrementConstants1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostDecrementConstants1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIncrementPreIncrement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIncrementAddAssignment()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingWithNonOneIncrementValue()
        => TestMissingAsync(
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

    [Fact]
    public Task TestPostDecrement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostIncrementEquals1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostDecrementEquals()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostIncrementSwappedConditions()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPostIncrementEqualsSwappedConditions()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestByteOneMin()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUInt16OneMin()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUInt32OneMin()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUInt64OneMin()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestByteZeroMin()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt16ZeroMin()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt32ZeroMin()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt64ZeroMin()
        => TestMissingAsync(
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

    [Fact]
    public Task TestByteMax()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt16Max()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt32Max()
        => TestMissingAsync(
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

    [Fact]
    public Task TestUInt64Max()
        => TestMissingAsync(
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

    [Fact]
    public Task TestByteZeroMinReverse()
        => TestMissingAsync(
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
