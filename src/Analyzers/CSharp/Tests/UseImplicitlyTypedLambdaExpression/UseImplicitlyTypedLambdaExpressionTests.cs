// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
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
    private static readonly LanguageVersion CSharp14 = LanguageVersion.Preview;

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
                        Func<int> a = [|(|]int x) => { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Func<int> a = x => { };
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();
    }
}
