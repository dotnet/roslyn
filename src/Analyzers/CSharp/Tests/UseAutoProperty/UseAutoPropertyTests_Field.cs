// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
public sealed partial class UseAutoPropertyTests
{
    private readonly ParseOptions CSharp13 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);

    [Fact]
    public async Task TestFieldSimplestCase()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        return s.Trim();
                    }
                }
            }
            """,
            """
            class Class
            {
                string P
                {
                    get
                    {
                        return field.Trim();
                    }
                }
            }
            """, parseOptions: CSharp13);
    }

    [Fact]
    public async Task TestGetterWithMultipleStatements_Field()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        ;
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P
                {
                    get
                    {
                        ;
                        return field;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement_Field()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        ;
                        i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P
                {
                    get;
                    set
                    {
                        ;
                        field = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimpleFieldInExpressionBody()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|string s|];

                string P => s.Trim();
            }
            """,
            """
            class Class
            {
                string P => field.Trim();
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_NoClearChoice()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total => x + y;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_NoClearChoice2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total
                {
                    get => x + y;
                    set
                    {
                        x = value;
                        y = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFields_ClearChoice()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                int [|x|], y;

                int Total
                {
                    get => x + y;
                    set
                    {
                        x = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int y;

                int Total
                {
                    get => field + y;
                    set
                    {
                        field = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWhenAlreadyUsingField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|string s|];

                string P
                {
                    get
                    {
                        var v = field.Trim();
                        return s.Trim();
                    }
                }
            }
            """);
    }
}
