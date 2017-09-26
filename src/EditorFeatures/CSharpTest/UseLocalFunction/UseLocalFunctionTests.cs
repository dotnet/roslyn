﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task TestRefReadOnlyWithParameters()
        {
            await TestInRegularAndScriptAsync(
@"
delegate void D(ref readonly int p);
class C
{
    void M()
    {
        D [||]lambda = (ref readonly int p) => throw null;
    }
}",
@"
delegate void D(ref readonly int p);
class C
{
    void M()
    {
        void lambda(ref readonly int p) => throw null;
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
    }
}
