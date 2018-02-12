// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseLocalFunction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseLocalFunction
{
    public partial class UseLocalFunctionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseLocalFunctionDiagnosticAnalyzer(), new CSharpUseLocalFunctionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingBeforeCSharp7()
        {
            await TestMissingAsync(
@"using System;

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
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingIfWrittenAfter()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingIfWrittenInside()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingForErrorType()
        {
            await TestMissingAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingForMultipleVariables()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingForField()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_SimpleLambda_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_ParenLambdaNoType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_ParenLambdaWithType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_AnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_SimpleLambda_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Func<int, int> [||]fibonacci = v =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_ParenLambdaNoType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Func<int, int> [||]fibonacci = (v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleInitialization_ParenLambdaWithType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Func<int, int> [||]fibonacci = (int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_SimpleLambda_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_SimpleLambda_Block_ExtraParens()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_ParenLambdaNoType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_ParenLambdaWithType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_AnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_SimpleLambda_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [||]fibonacci = (Func<int, int>)(v =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2));
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_ParenLambdaNoType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [||]fibonacci = (Func<int, int>)((v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2));
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestCastInitialization_ParenLambdaWithType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [||]fibonacci = (Func<int, int>)((int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2));
    }
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_WrongName()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_InitializedToOtherValue()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_Block_Uninitialized()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_Block_DefaultLiteral()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_Block_DefaultExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_Block_DefaultExpression_var()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_ParenLambdaNoType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_ParenLambdaWithType_Block()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_AnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_SimpleLambda_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_ParenLambdaNoType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSplitInitialization_ParenLambdaWithType_ExprBody()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v) =>
            v <= 1
                ? 1
                : fibonacci(v - 1, v - 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v)
        {
            bool isTrue(bool b) => b;

            return fibonacci(v - 1, v - 2);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v)
        {
            bool isTrue(bool b) => b;

            return fibonacci(v - 1, v - 2);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestFixAll3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v)
        {
            bool isTrue(bool b) => b;
            return fibonacci(v - 1, v - 2);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestFixAll4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        int fibonacci(int v)
        {
            int fibonacciHelper(int n) => fibonacci(n - 1) + fibonacci(v: n - 2);

            return fibonacciHelper(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestTrivia()
        {
            await TestInRegularAndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        // Leading trivia
        int fibonacci(int v)
        {
            if (v <= 1)
            {
                return 1;
            }

            return fibonacci(v - 1, v - 2);
        } // Trailing trivia
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestInWithParameters()
        {
            await TestInRegularAndScriptAsync(
@"
delegate void D(in int p);
class C
{
    void M()
    {
        D [||]lambda = (in int p) => throw null;
    }
}",
@"
delegate void D(in int p);
class C
{
    void M()
    {
        void lambda(in int p) => throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestRefReadOnlyWithReturnType()
        {
            await TestInRegularAndScriptAsync(
@"
delegate ref readonly int D();
class C
{
    void M()
    {
        D [||]lambda = () => throw null;
    }
}",
@"
delegate ref readonly int D();
class C
{
    void M()
    {
        ref readonly int lambda() => throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestMissingIfConvertedToNonDelegate()
        {
            await TestMissingAsync(
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestAvailableIfConvertedToDelegate()
        {
            await TestInRegularAndScript1Async(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Task f(string x)
        { return Task.CompletedTask; }
        Func<string, Task> actual = null;
        AssertSame(f, actual);
    }

    public static void AssertSame(Func<string, Task> expected, object actual) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestNotAvailableIfConvertedToSystemDelegate()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<object, string> [||]f = x => """";
        M(f);
    }

    public static void M(Delegate expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestNotAvailableIfConvertedToSystemMulticastDelegate()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<object, string> [||]f = x => """";
        M(f);
    }

    public static void M(MulticastDelegate expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestAvailableIfConvertedToCoContraVariantDelegate0()
        {
            await TestInRegularAndScript1Async(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<object, string> [||]f = x => """";
        M(f);
    }

    public static void M(Func<object, string> expected) { }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        string f(object x) => """";
        M(f);
    }

    public static void M(Func<object, string> expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestAvailableIfConvertedToCoContraVariantDelegate1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<object, string> [||]f = x => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        string f(object x) => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestAvailableIfConvertedToCoContraVariantDelegate2()
        {
            await TestInRegularAndScript1Async(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<string, string> [||]f = x => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        string f(string x) => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23118, "https://github.com/dotnet/roslyn/issues/23118")]
        public async Task TestAvailableIfConvertedToCoContraVariantDelegate3()
        {
            await TestInRegularAndScript1Async(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<object, object> [||]f = x => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        object f(object x) => """";
        M(f);
    }

    public static void M(Func<string, object> expected) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(22672, "https://github.com/dotnet/roslyn/issues/22672")]
        public async Task TestMissingIfAdded()
        {
            await TestMissingAsync(
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(22672, "https://github.com/dotnet/roslyn/issues/22672")]
        public async Task TestMissingIfUsedInMemberAccess1()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23150, "https://github.com/dotnet/roslyn/issues/23150")]
        public async Task TestMissingIfUsedInMemberAccess2()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(22672, "https://github.com/dotnet/roslyn/issues/22672")]
        public async Task TestMissingIfUsedInExpressionTree()
        {
            await TestMissingAsync(
@"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24344, "https://github.com/dotnet/roslyn/issues/24344")]
        public async Task TestMissingIfUsedInExpressionTree2()
        {
            await TestMissingAsync(
@"using System;
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23150, "https://github.com/dotnet/roslyn/issues/23150")]
        public async Task TestWithInvokeMethod1()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class Enclosing<T> where T : class
{
    delegate T MyDelegate(T t = null);

    public class Class
    {
        public void Caller()
        {
            T local(T x = null) => x;

            local();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23150, "https://github.com/dotnet/roslyn/issues/23150")]
        public async Task TestWithInvokeMethod2()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class Enclosing<T> where T : class
{
    delegate T MyDelegate(T t = null);

    public class Class
    {
        public void Caller(T t)
        {
            T local(T x = null) => x;

            Console.Write(local(t));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23150, "https://github.com/dotnet/roslyn/issues/23150")]
        public async Task TestWithInvokeMethod3()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class Enclosing<T> where T : class
{
    delegate T MyDelegate(T t = null);

    public class Class
    {
        public void Caller(T t)
        {
            T local(T x = null) => x;

            Console.Write(local(t));

            var val = local(t);
            local(t);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(23150, "https://github.com/dotnet/roslyn/issues/23150")]
        public async Task TestWithInvokeMethod4()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class Enclosing<T> where T : class
{
    delegate T MyDelegate(T t = null);

    public class Class
    {
        public void Caller(T t)
        {
            T local(T x = null) => x;

            Console.Write(local(t));

            var val = local(t);
            local(t);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24760, "https://github.com/dotnet/roslyn/issues/24760#issuecomment-364807853")]
        public async Task TestWithRecursiveInvokeMethod1()
        {
            await TestInRegularAndScript1Async(
 @"using System;

class C
{
    void M()
    {
        Action [||]local = null;
        local = () => local.Invoke();
    }
}",
 @"using System;

class C
{
    void M()
    {
        void local() => local();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24760, "https://github.com/dotnet/roslyn/issues/24760#issuecomment-364807853")]
        public async Task TestWithRecursiveInvokeMethod2()
        {
            await TestInRegularAndScript1Async(
 @"using System;

class C
{
    void M()
    {
        Action [||]local = null;
        local = () => {
            local.Invoke();
        }
    }
}",
 @"using System;

class C
{
    void M()
    {
        void local()
        {
            local();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithDefaultParameter1()
        {
           await TestInRegularAndScript1Async(
@"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        MyDelegate [||]local = (s) => s;
    }
}",
@"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        string local(string s = ""hello"") => s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24760, "https://github.com/dotnet/roslyn/issues/24760#issuecomment-364655480")]
        public async Task TestWithDefaultParameter2()
        {
            await TestInRegularAndScript1Async(
 @"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        MyDelegate [||]local = (string s) => s;
    }
}",
 @"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        string local(string s = ""hello"") => s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithDefaultParameter3()
        {
            await TestInRegularAndScript1Async(
 @"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        MyDelegate [||]local = delegate (string s) { return s; };
    }
}",
 @"class C
{
    delegate string MyDelegate(string arg = ""hello"");

    void M()
    {
        string local(string s = ""hello"")
        { return s; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24760, "https://github.com/dotnet/roslyn/issues/24760#issuecomment-364764542")]
        public async Task TestWithUnmatchingParameterList1()
        {
            await TestInRegularAndScript1Async(
 @"using System;

class C
{
    void M()
    {
        Action [||]x = (a, b) => {
        };
    }
}",
 @"using System;

class C
{
    void M()
    {
        void x(object a, object b)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithUnmatchingParameterList2()
        {
            await TestInRegularAndScript1Async(
 @"using System;

class C
{
    void M()
    {
        Action [||]x = (string a, int b) => {
        };
    }
}",
 @"using System;

class C
{
    void M()
    {
        void x(string a, int b)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithAsyncLambdaExpression()
        {
            await TestInRegularAndScript1Async(
 @"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Func<Task> [||]f = async () => await Task.Yield();
    }
}",
 @"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async Task f() => await Task.Yield();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithAsyncAnonymousMethod()
        {
            await TestInRegularAndScript1Async(
 @"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Func<Task<int>> [||]f = async delegate () {
            return 0;
        };
    }
}",
 @"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async Task<int> f()
        {
            return 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithParameterlessAnonymousMethod()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void M()
    {
        EventHandler [||]handler = delegate {
        };

        E += handler;
    }

    event EventHandler E;
}",
@"using System;

class C
{
    void M()
    {
        void handler(object sender, EventArgs e)
        {
        }

        E += handler;
    }

    event EventHandler E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24764, "https://github.com/dotnet/roslyn/issues/24764")]
        public async Task TestWithNamedArguments1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void M()
    {
        Action<string, int, int> [||]x = (a1, a2, a3) => {
        };

        x(arg1: null, 0, 0);
        x.Invoke(arg1: null, 0, 0);
    }
}",
@"using System;

class C
{
    void M()
    {
        void x(string a1, int a2, int a3)
        {
        }

        x(a1: null, 0, 0);
        x(a1: null, 0, 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        [WorkItem(24764, "https://github.com/dotnet/roslyn/issues/24764")]
        public async Task TestWithNamedArguments2()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        void x(string a1, int a2, int a3)
        {
            x(null, a3: 0, a2: 0);
            x(null, a3: 0, a2: 0);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithNamedArgumentsAndBrokenCode1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void M()
    {
        Action<string, int, int> [||]x = (int a1, string a2, string a3, a4) => {
        };

        x(null, arg3: 0, arg2: 0, arg4: 0);
        x.Invoke(null, arg3: 0, arg2: 0, arg4: 0);
        x.Invoke(0, null, null, null, null, null);
    }
}",
@"using System;

class C
{
    void M()
    {
        void x(int a1, string a2, string a3, object a4)
        {
        }

        x(null, a3: 0, a2: 0, arg4: 0);
        x(null, a3: 0, a2: 0, arg4: 0);
        x(0, null, null, null, null, null);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithNamedArgumentsAndBrokenCode2()
        {
            await TestInRegularAndScript1Async(
@"using System;

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
}",
@"using System;

class C
{
    void M()
    {
        void x(string a1, int a2)
        {
            x(null, arg3: 0, a2: 0);
            x(null, arg3: 0, a2: 0);
            x();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithNamedAndDefaultArguments()
        {
            await TestInRegularAndScript1Async(
@"class C
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
}",
@"class C
{
    delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

    void M()
    {
        string x(string a1, int a2 = 2, int a3 = 3)
        {
            x(null, a3: 42);
            return x(null, a3: 42);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestWithNamedAndDefaultArgumentsOnParameterlessAnonymousMethod()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

    void M()
    {
        MyDelegate [||]x = null;
        x = delegate
        {
            x(null, arg3: 42);
            return x.Invoke(null, arg3: 42);
        };
    }
}",
@"class C
{
    delegate string MyDelegate(string arg1, int arg2 = 2, int arg3 = 3);

    void M()
    {
        string x(string arg1, int arg2 = 2, int arg3 = 3)
        {
            x(null, arg3: 42);
            return x(null, arg3: 42);
        }
    }
}");
        }
    }
}
