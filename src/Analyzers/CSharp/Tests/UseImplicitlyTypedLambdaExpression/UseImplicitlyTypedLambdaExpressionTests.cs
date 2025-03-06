// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseImplicitlyTypedLambdaExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseImplicitlyTypedLambdaExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseImplicitlyTypedLambdaExpressionDiagnosticAnalyzer,
    CSharpUseImplicitlyTypedLambdaExpressionCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
public sealed class UseImplicitlyTypedLambdaExpressionTests
{
    private static readonly LanguageVersion CSharp14 = LanguageVersionExtensions.CSharpNext;

    [Fact]
    public async Task TestAssignedToObject()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        object a = (int x) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCastedToDelegate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    object a = (Delegate)((int x) => { });
                }
            }
            """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCastedToObject()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    object a = (object)((int x) => { });
                }
            }
            """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignedToDelegate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Delegate a = (int x) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignedToVar()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        var a = (int x) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignedToStronglyTypedDelegate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = [|(|]int x) => { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = x => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExplicitReturnType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = void (int x) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithDefaultVAlue()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = (int x = 1) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCastToStronglyTypedDelegate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Delegate a = (Action<int>)([|(|]int x) => { });
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Delegate a = (Action<int>)(x => { });
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCreationOfStronglyTypedDelegate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Delegate a = new Action<int>([|(|]int x) => { });
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Delegate a = new Action<int>(x => { });
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestArgument()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(Action<int> action)
                    {
                        M([|(|]int x) => { });
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(Action<int> action)
                    {
                        M(x => { });
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestOverloadResolution()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(Action<int> action)
                    {
                        M((int x) => { });
                    }

                    void M(Action<string> action)
                    {
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestModifier_CSharp13()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                delegate void D(ref int i);

                class C
                {
                    void M()
                    {
                        D d = (ref int i) => { };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();
    }

    [Fact]
    public async Task TestModifier_CSharp14()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                delegate void D(ref int i);

                class C
                {
                    void M()
                    {
                        D d = [|(|]ref int i) => { };
                    }
                }
                """,
            FixedCode = """
                using System;

                delegate void D(ref int i);

                class C
                {
                    void M()
                    {
                        D d = [|(|]ref i) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = [|(|]int x) =>
                        {
                            Action<int> b = [|(|]int y) => { };
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int> a = x =>
                        {
                            Action<int> b = y => { };
                        };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestParams()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                delegate void D(params int[] x);

                class C
                {
                    void M()
                    {
                        D d = [|(|]params int[] x) => { };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                delegate void D(params int[] x);

                class C
                {
                    void M()
                    {
                        D d = x => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiLine()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int, int, int> a =
                            [|(|]int x,
                             int y,
                             int z) => { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Action<int, int, int> a =
                            (x,
                             y,
                             z) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAttribute()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute
                {
                }

                class C
                {
                    void M()
                    {
                        Action<int> d = [|(|][X] int i) => { };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute
                {
                }

                class C
                {
                    void M()
                    {
                        Action<int> d = [|(|][X] i) => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }
}
