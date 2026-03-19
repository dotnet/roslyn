// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseLocalFunction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseLocalFunction;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
public sealed partial class UseLocalFunctionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseLocalFunctionTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseLocalFunctionDiagnosticAnalyzer(), new CSharpUseLocalFunctionCodeFixProvider());

    private static readonly ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);

    [Fact]
    public Task TestMissingBeforeCSharp7()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task TestMissingIfWrittenAfter()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };

                    fibonacci = null;
                }
            }
            """);

    [Fact]
    public Task TestMissingIfWrittenInside()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        fibonacci = null;
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """);

    [Fact]
    public Task TestMissingForErrorType()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    // Func can't be bound.
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """);

    [Fact]
    public Task TestMissingForMultipleVariables()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }, fib2 = x => x;
                }
            }
            """);

    [Fact]
    public Task TestMissingForField()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_SimpleLambda_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_ParenLambdaNoType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = (v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_ParenLambdaWithType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = (int v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_AnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = delegate (int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_SimpleLambda_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_ParenLambdaNoType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = (v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestSimpleInitialization_ParenLambdaWithType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = (int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_SimpleLambda_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)(v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    });
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_SimpleLambda_Block_ExtraParens()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = ((Func<int, int>)(v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_ParenLambdaNoType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)((v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    });
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_ParenLambdaWithType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)((int v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    });
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_AnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)(delegate (int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    });
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_SimpleLambda_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)(v =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_ParenLambdaNoType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)((v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestCastInitialization_ParenLambdaWithType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = (Func<int, int>)((int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_WrongName()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fib = null;
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_InitializedToOtherValue()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = GetCallback();
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_Block_NoInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci;
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_Block_DefaultLiteral()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = default;
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """,
        // 7.1 is required for default literals, so 7.2 should be sufficient
        // and is used in other tests
        new TestParameters(parseOptions: CSharp72ParseOptions));

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_Block_DefaultExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = default(Func<int, int>);
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_Block_DefaultExpression_var()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var [||]fibonacci = default(Func<int, int>);
                    fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_ParenLambdaNoType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = (v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_ParenLambdaWithType_Block()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = (int v) =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_AnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = delegate (int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_SimpleLambda_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = v =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_ParenLambdaNoType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = (v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestSplitInitialization_ParenLambdaWithType_ExprBody()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = null;
                    fibonacci = (int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v) =>
                        v <= 1
                            ? 1
                            : fibonacci(v - 1, v - 2);
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> {|FixAllInDocument:fibonacci|} = v =>
                    {
                        Func<bool, bool> isTrue = b => b;

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        bool isTrue(bool b) => b;

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> fibonacci = v =>
                    {
                        Func<bool, bool> {|FixAllInDocument:isTrue|} = b => b;

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        bool isTrue(bool b) => b;

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAll3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> fibonacci = null;
                    fibonacci = v =>
                    {
                        Func<bool, bool> {|FixAllInDocument:isTrue|} = null;
                        isTrue = b => b;

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        bool isTrue(bool b) => b;
                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAll4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> {|FixAllInDocument:fibonacci|} = null;
                    fibonacci = v =>
                    {
                        Func<int, int> fibonacciHelper = null;
                        fibonacciHelper = n => fibonacci.Invoke(n - 1) + fibonacci(arg: n - 2);

                        return fibonacciHelper(v);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        int fibonacciHelper(int n) => fibonacci(n - 1) + fibonacci(v: n - 2);
                        return fibonacciHelper(v);
                    }
                }
            }
            """);

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    // Leading trivia
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }; // Trailing trivia
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    // Leading trivia
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    } // Trailing trivia
                }
            }
            """);

    [Fact]
    public Task TestInWithParameters()
        => TestInRegularAndScriptAsync(
            """
            delegate void D(in int p);
            class C
            {
                void M()
                {
                    D [||]lambda = (in int p) => throw null;
                }
            }
            """,
            """
            delegate void D(in int p);
            class C
            {
                void M()
                {
                    void lambda(in int p) => throw null;
                }
            }
            """,
        // Run with 7.2 to get read-only references
        new TestParameters(parseOptions: CSharp72ParseOptions));

    [Fact]
    public Task TestRefReadOnlyWithReturnType()
        => TestInRegularAndScriptAsync(
            """
            delegate ref readonly int D();
            class C
            {
                void M()
                {
                    D [||]lambda = () => throw null;
                }
            }
            """,
            """
            delegate ref readonly int D();
            class C
            {
                void M()
                {
                    static ref readonly int lambda() => throw null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestMissingIfConvertedToNonDelegate()
        => TestMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string, Task> [||]f = x => { return Task.CompletedTask; };
                    Func<string, Task> actual = null;
                    AssertSame(f, actual);
                }

                public static void AssertSame(object expected, object actual) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestAvailableIfConvertedToDelegate()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string, Task> [||]f = x => { return Task.CompletedTask; };
                    Func<string, Task> actual = null;
                    AssertSame(f, actual);
                }

                public static void AssertSame(Func<string, Task> expected, object actual) { }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    static Task f(string x) { return Task.CompletedTask; }
                    Func<string, Task> actual = null;
                    AssertSame(f, actual);
                }

                public static void AssertSame(Func<string, Task> expected, object actual) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestNotAvailableIfConvertedToSystemDelegate()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<object, string> [||]f = x => "";
                    M(f);
                }

                public static void M(Delegate expected) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestNotAvailableIfConvertedToSystemMulticastDelegate()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<object, string> [||]f = x => "";
                    M(f);
                }

                public static void M(MulticastDelegate expected) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestAvailableIfConvertedToCoContraVariantDelegate0()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<object, string> [||]f = x => "";
                    M(f);
                }

                public static void M(Func<object, string> expected) { }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    static string f(object x) => "";
                    M(f);
                }

                public static void M(Func<object, string> expected) { }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestAvailableIfConvertedToCoContraVariantDelegate1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<object, string> [||]f = x => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    static string f(object x) => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestAvailableIfConvertedToCoContraVariantDelegate2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string, string> [||]f = x => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    static string f(string x) => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23118")]
    public Task TestAvailableIfConvertedToCoContraVariantDelegate3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<object, object> [||]f = x => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    static object f(object x) => "";
                    M(f);
                }

                public static void M(Func<string, object> expected) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22672")]
    public Task TestMissingIfAdded()
        => TestMissingAsync(
            """
            using System;
            using System.Linq.Expressions;

            class Enclosing<T> where T : class
            {
              delegate T MyDelegate(T t = null);

              public class Class
              {
                public void Caller()
                {
                  MyDelegate [||]local = x => x;

                  var doubleDelegate = local + local;
                }
              }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22672")]
    public Task TestMissingIfUsedInMemberAccess1()
        => TestMissingAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller()
                    {
                        MyDelegate [||]local = x => x;

                        var str = local.ToString();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23150")]
    public Task TestMissingIfUsedInMemberAccess2()
        => TestMissingAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        MyDelegate[||]local = x => x;

                        Console.Write(local.Invoke(t));

                        var str = local.ToString();
                        local.Invoke(t);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22672")]
    public Task TestMissingIfUsedInExpressionTree()
        => TestMissingAsync(
            """
            using System;
            using System.Linq.Expressions;

            class Enclosing<T> where T : class
            {
              delegate T MyDelegate(T t = null);

              public class Class
              {
                public void Caller()
                {
                  MyDelegate [||]local = x => x;

                  Expression<Action> expression = () => local(null);
                }
              }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24344")]
    public Task TestMissingIfUsedInExpressionTree2()
        => TestMissingAsync(
            """
            using System;
            using System.Linq.Expressions;

            public class C
            {
                void Method(Action action) { }

                Expression<Action> Example()
                {
                    Action [||]action = () => Method(null);
                    return () => Method(action);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23150")]
    public Task TestWithInvokeMethod1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller()
                    {
                        MyDelegate [||]local = x => x;

                        local.Invoke();
                    }
                }
            }
            """,
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller()
                    {
                        static T local(T x = null) => x;

                        local();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23150")]
    public Task TestWithInvokeMethod2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        MyDelegate [||]local = x => x;

                        Console.Write(local.Invoke(t));
                    }
                }
            }
            """,
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        static T local(T x = null) => x;

                        Console.Write(local(t));
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23150")]
    public Task TestWithInvokeMethod3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        MyDelegate [||]local = x => x;

                        Console.Write(local.Invoke(t));

                        var val = local.Invoke(t);
                        local.Invoke(t);
                    }
                }
            }
            """,
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        static T local(T x = null) => x;

                        Console.Write(local(t));

                        var val = local(t);
                        local(t);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23150")]
    public Task TestWithInvokeMethod4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        MyDelegate [||]local = x => x;

                        Console.Write(local.Invoke(t));

                        var val = local.Invoke(t);
                        local(t);
                    }
                }
            }
            """,
            """
            using System;

            class Enclosing<T> where T : class
            {
                delegate T MyDelegate(T t = null);

                public class Class
                {
                    public void Caller(T t)
                    {
                        static T local(T x = null) => x;

                        Console.Write(local(t));

                        var val = local(t);
                        local(t);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24760#issuecomment-364807853")]
    public Task TestWithRecursiveInvokeMethod1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action [||]local = null;
                    local = () => local.Invoke();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void local() => local();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24760#issuecomment-364807853")]
    public Task TestWithRecursiveInvokeMethod2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action [||]local = null;
                    local = () => { local.Invoke(); }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void local() { local(); }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24760#issuecomment-364935495")]
    public Task TestWithNestedInvokeMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string, string> [||]a = s => s;
                    a.Invoke(a.Invoke(null));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string a(string s) => s;
                    a(a(null));
                }
            }
            """);

    [Fact]
    public Task TestWithNestedRecursiveInvokeMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string, string> [||]a = null;
                    a = s => a.Invoke(a.Invoke(s));
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string a(string s) => a(a(s));
                }
            }
            """);

    [Fact]
    public Task TestWithDefaultParameter1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    MyDelegate [||]local = (s) => s;
                }
            }
            """,
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    static string local(string s = "hello") => s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24760#issuecomment-364655480")]
    public Task TestWithDefaultParameter2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    MyDelegate [||]local = (string s) => s;
                }
            }
            """,
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    static string local(string s = "hello") => s;
                }
            }
            """);

    [Fact]
    public Task TestWithDefaultParameter3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    MyDelegate [||]local = delegate (string s) { return s; };
                }
            }
            """,
            """
            class C
            {
                delegate string MyDelegate(string arg = "hello");

                void M()
                {
                    static string local(string s = "hello") { return s; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24760#issuecomment-364764542")]
    public Task TestWithUnmatchingParameterList1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action [||]x = (a, b) => { };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(object a, object b) { }
                }
            }
            """);

    [Fact]
    public Task TestWithUnmatchingParameterList2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action [||]x = (string a, int b) => { };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(string a, int b) { }
                }
            }
            """);

    [Fact]
    public Task TestWithAsyncLambdaExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task> [||]f = async () => await Task.Yield();
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static async Task f() => await Task.Yield();
                }
            }
            """);

    [Fact]
    public Task TestWithAsyncAnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> [||]f = async delegate () { return 0; };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static async Task<int> f() { return 0; }
                }
            }
            """);

    [Fact]
    public Task TestWithParameterlessAnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    EventHandler [||]handler = delegate { };

                    E += handler;
                }

                event EventHandler E;
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void handler(object sender, EventArgs e) { }

                    E += handler;
                }

                event EventHandler E;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24764")]
    public Task TestWithNamedArguments1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action<string, int, int> [||]x = (a1, a2, a3) => { };

                    x(arg1: null, 0, 0);
                    x.Invoke(arg1: null, 0, 0);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(string a1, int a2, int a3) { }

                    x(a1: null, 0, 0);
                    x(a1: null, 0, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24764")]
    public Task TestWithNamedArguments2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action<string, int, int> [||]x = (a1, a2, a3) =>
                    {
                        x(null, arg3: 0, arg2: 0);
                        x.Invoke(null, arg3: 0, arg2: 0);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(string a1, int a2, int a3)
                    {
                        x(null, a3: 0, a2: 0);
                        x(null, a3: 0, a2: 0);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithNamedArgumentsAndBrokenCode1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action<string, int, int> [||]x = (int a1, string a2, string a3, a4) => { };

                    x(null, arg3: 0, arg2: 0, arg4: 0);
                    x.Invoke(null, arg3: 0, arg2: 0, arg4: 0);
                    x.Invoke(0, null, null, null, null, null);
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(int a1, string a2, string a3, object a4) { }

                    x(null, a3: 0, a2: 0, arg4: 0);
                    x(null, a3: 0, a2: 0, arg4: 0);
                    x(0, null, null, null, null, null);
                }
            }
            """);

    [Fact]
    public Task TestWithNamedArgumentsAndBrokenCode2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Action<string, int, int> [||]x = (a1, a2) =>
                    {
                        x(null, arg3: 0, arg2: 0);
                        x.Invoke(null, arg3: 0, arg2: 0);
                        x.Invoke();
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static void x(string a1, int a2)
                    {
                        x(null, arg3: 0, a2: 0);
                        x(null, arg3: 0, a2: 0);
                        x();
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithNamedAndDefaultArguments()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

                void M()
                {
                    MyDelegate [||]x = null;
                    x = (a1, a2, a3) =>
                    {
                        x(null, arg3: 42);
                        return x.Invoke(null, arg3: 42);
                    };
                }
            }
            """,
            """
            class C
            {
                delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

                void M()
                {
                    static string x(string a1, int a2 = 2, int a3 = 3)
                    {
                        x(null, a3: 42);
                        return x(null, a3: 42);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithNamedAndDefaultArgumentsAndNestedRecursiveInvocations_FixAll()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

                void M()
                {
                    var x = (MyDelegate)delegate
                    {
                        MyDelegate {|FixAllInDocument:a|} = null;
                        a = (string a1, int a2, int a3) =>
                        {
                            MyDelegate b = null;
                            b = (b1, b2, b3) =>
                                b.Invoke(arg1: b(null), arg2: a(arg1: a.Invoke(null)).Length, arg3: 42) +
                                b(arg1: b.Invoke(null), arg3: a(arg1: a.Invoke(null)).Length, arg2: 42);

                            return b(arg1: a1, b(arg1: a1).Length);
                        };

                        return a(arg1: null);
                    };

                    x(arg1: null);
                }
            }
            """,
            """
            class C
            {
                delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

                void M()
                {
                    string x(string arg1, int arg2 = 2, int arg3 = 3)
                    {
                        string a(string a1, int a2 = 2, int a3 = 3)
                        {
                            string b(string b1, int b2 = 2, int b3 = 3) =>
                                b(b1: b(null), b2: a(a1: a(null)).Length, b3: 42) +
                                b(b1: b(null), b3: a(a1: a(null)).Length, b2: 42);
                            return b(b1: a1, b(b1: a1).Length);
                        }

                        return a(a1: null);
                    }

                    x(arg1: null);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action [||]onUpdateSolutionCancel = () => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel() { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = a => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine2Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = async a => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine2MultiToken()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    Func<int, int[]> [||]onUpdateSolutionCancel = a => { return null; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    static int[] onUpdateSolutionCancel(int a) { return null; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine2MultiTokenAsync()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;
            class C
            {
                void Goo()
                {
                    Func<int, Task<int[]>> [||]onUpdateSolutionCancel = async a => { return null; };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            class C
            {
                void Goo()
                {
                    static async Task<int[]> onUpdateSolutionCancel(int a) { return null; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = (int a) => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine4()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = delegate (int a) { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSimpleInitialization_SingleLine4Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = async delegate (int a) { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action)(() => { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel() { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action<int>)(a => { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine2Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action<int>)(async a => { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action<int>)((int a) => { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine4()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action<int>)(delegate (int a) { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestCastInitialization_SingleLine4Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    var [||]onUpdateSolutionCancel = (Action<int>)(async delegate (int a) { buildCancelled = true; });
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = () => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel() { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = a => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine2Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = async a => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = (int a) => { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine4()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = delegate (int a) { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23872")]
    public Task TestSplitInitialization_SingleLine4Async()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    Action<int> [||]onUpdateSolutionCancel = null;
                    onUpdateSolutionCancel = async delegate (int a) { buildCancelled = true; };
                }
            }
            """,
            """
            using System;
            class C
            {
                void Goo()
                {
                    var buildCancelled = false;
                    async void onUpdateSolutionCancel(int a) { buildCancelled = true; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestNotAvailableIfTypeParameterChanged1()
        => TestMissingAsync(
            """
            using System;

            class Enclosing<T>
            {
                delegate T MyDelegate(T t);
                static void Callee(MyDelegate d) => d(default);

                public class Class<T>
                {
                    public void Caller()
                    {
                        MyDelegate [||]local = x => x;
                        Callee(local);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestNotAvailableIfTypeParameterChanged2()
        => TestMissingAsync(
            """
            using System;

            class Enclosing<T>
            {
                delegate T MyDelegate(T t);
                static void Callee(MyDelegate d) => d(default);

                public class Goo<T>
                {
                    public class Class
                    {
                        public void Caller()
                        {
                            MyDelegate [||]local = x => x;
                            Callee(local);
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestNotAvailableIfTypeParameterChanged3()
        => TestMissingAsync(
            """
            public class Class<T>
            {
                delegate T MyDelegate(T t);
                static void Callee(MyDelegate d) => d(default);

                public void Caller()
                {
                    void Some<T>(T t)
                    {
                        MyDelegate [||]local = x => x;
                        Callee(local);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestNotAvailableIfTypeParameterChanged4()
        => TestMissingAsync(
            """
            using System;

            class Enclosing<T>
            {
                delegate T MyDelegate(T t);
                static void Callee(MyDelegate d) => d(default);

                public class Goo<T>
                {
                    public class Class<U>
                    {
                        public void Caller()
                        {
                            MyDelegate [||]local = x => x;
                            Callee(local);
                        }
                    }
                }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/27950")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestAvailableIfTypeParameterNotChanged1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class DelegateEnclosing<T>
            {
                protected delegate T MyDelegate(T t);
            }

            class Enclosing<T> : DelegateEnclosing<T>
            {
                static void Callee(MyDelegate d) => d(default);

                public void Caller()
                {
                    MyDelegate [||]local = x => x;
                    Callee(local);
                }
            }
            """,
            """
            using System;

            class DelegateEnclosing<T>
            {
                protected delegate T MyDelegate(T t);
            }

            class Enclosing<T> : DelegateEnclosing<T>
            {
                static void Callee(MyDelegate d) => d(default);

                public void Caller()
                {
                    static T local(T x) => x;
                    Callee(local);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23149")]
    public Task TestAvailableIfTypeParameterNotChanged2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class DelegateEnclosing<T>
            {
                protected delegate T MyDelegate(T t);
            }

            class Enclosing<U> : DelegateEnclosing<U>
            {
                static void Callee(MyDelegate d) => d(default);

                public void Caller()
                {
                    MyDelegate [||]local = x => x;
                    Callee(local);
                }
            }
            """,
            """
            using System;

            class DelegateEnclosing<T>
            {
                protected delegate T MyDelegate(T t);
            }

            class Enclosing<U> : DelegateEnclosing<U>
            {
                static void Callee(MyDelegate d) => d(default);

                public void Caller()
                {
                    static U local(U x) => x;
                    Callee(local);
                }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56963")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/26526")]
    public Task TestAvailableWithCastIntroducedIfAssignedToVar()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string> [||]f = () => null;

                    var f2 = f;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string f() => null;

                    var f2 = (Func<string>)f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26526")]
    public Task TestAvailableWithCastIntroducedForGenericTypeInference1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, string> [||]f = _ => null;

                    Method(f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string f(int _) => null;

                    Method((Func<int, string>)f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26526")]
    public Task TestAvailableWithCastIntroducedForGenericTypeInference2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, string> [||]f = _ => null;

                    Method(f);
                }

                void Method<T>(Func<T, string> o)
                {
                }

                void Method(string o)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string f(int _) => null;

                    Method((Func<int, string>)f);
                }

                void Method<T>(Func<T, string> o)
                {
                }

                void Method(string o)
                {
                }
            }
            """);

    [Fact]
    public Task TestAvailableWithCastIntroducedForOverloadResolution()
        => TestInRegularAndScriptAsync(
            """
            using System;

            delegate string CustomDelegate();

            class C
            {
                void M()
                {
                    Func<string> [||]f = () => null;

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }

                void Method(CustomDelegate o)
                {
                }
            }
            """,
            """
            using System;

            delegate string CustomDelegate();

            class C
            {
                void M()
                {
                    static string f() => null;

                    Method((Func<string>)f);
                }

                void Method(Func<string> o)
                {
                }

                void Method(CustomDelegate o)
                {
                }
            }
            """);

    [Fact]
    public Task TestAvailableWithoutCastIfUnnecessaryForOverloadResolution()
        => TestInRegularAndScriptAsync(
            """
            using System;

            delegate string CustomDelegate(object arg);

            class C
            {
                void M()
                {
                    Func<string> [||]f = () => null;

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }

                void Method(CustomDelegate o)
                {
                }
            }
            """,
            """
            using System;

            delegate string CustomDelegate(object arg);

            class C
            {
                void M()
                {
                    static string f() => null;

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }

                void Method(CustomDelegate o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestNotAvailableWithInvalidDeclaration()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string> [||]f = () => null);

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestNotAvailableWithInvalidDeclaration2()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string> [||]f) = () => null;

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestNotAvailableWithInvalidDeclaration3()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<string>) [||]f = () => null;

                    Method(f);
                }

                void Method(Func<string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestWithInvalidUnrelatedCode()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, string> [||]f = _ => null;

                    Method(f);
                }

                void Method<T>(Func<T, string> o))
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string f(int _) => null;

                    Method((Func<int, string>)f);
                }

                void Method<T>(Func<T, string> o))
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestWithInvalidUnrelatedCode2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, string> [||]f = _ => null;

                    (Method(f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static string f(int _) => null;

                    (Method((Func<int, string>)f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestWithObsoleteCode()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, string> [||]f = _ => S();

                    Method(f);
                }

                [System.Obsolete]
                string S()
                {
                    return null;
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    string f(int _) => S();

                    Method((Func<int, string>)f);
                }

                [System.Obsolete]
                string S()
                {
                    return null;
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29793")]
    public Task TestWithDeclarationWarning()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
            #warning Declaration Warning
                    Func<int, string> [||]f = _ => null;

                    Method(f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
            #warning Declaration Warning
                    static string f(int _) => null;

                    Method((Func<int, string>)f);
                }

                void Method<T>(Func<T, string> o)
                {
                }
            }
            """);

    [Fact]
    public Task TestMakeStaticIfNoCaptures()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    static int fibonacci(int v)
                    {
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestDoNotMakeStaticIfCaptures()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> [||]fibonacci = v =>
                    {
                        M();
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    int fibonacci(int v)
                    {
                        M();
                        if (v <= 1)
                        {
                            return 1;
                        }

                        return fibonacci(v - 1, v - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithAsyncLambdaExpression_MakeStatic()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task> [||]f = async () => await Task.Yield();
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    static async Task f() => await Task.Yield();
                }
            }
            """);

    [Fact]
    public Task TestWithNullableParameterAndReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string?, string?> [||]f = s => s;
                }
            }
            """,
            """
            #nullable enable

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    static string? f(string? s) => s;
                }
            }
            """);

    [Fact]
    public Task TestWithDiscardParameters()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Func<int, string, int, long> [||]f = (_, _, a) => 1;
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    static long f(int _, string _, int a) => 1;
                }
            }
            """);

    [Fact]
    public Task TestWithOptionalParameter()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Func<int, int> [||]fibonacci = (int n = 1) =>
                    {
                        return n <= 1 ? 1 : fibonacci(n - 1) + fibonacci(n - 2);
                    };
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    static int fibonacci(int n = 1)
                    {
                        return n <= 1 ? 1 : fibonacci(n - 1) + fibonacci(n - 2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestWithParamsArray()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Func<int[], int> [||]last = (params int[] xs) =>
                    {
                        return x.Length == 1 ? xs[0] : last(xs[1..]);
                    };
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    static int last(params int[] xs)
                    {
                        return x.Length == 1 ? xs[0] : last(xs[1..]);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68439")]
    public Task TestImplicitlyTypedLambdaCSharp10()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    var [||]test = (int n) => n * 2;
                }
            }
            """,
            """
            class Program
            {
                static void Main()
                {
                    static int test(int n) => n * 2;
                }
            }
            """,
            new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68439")]
    public Task TestImplicitlyTypedLambdaCSharp9()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    var [||]test = (int n) => n * 2;
                }
            }
            """,
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));
}
