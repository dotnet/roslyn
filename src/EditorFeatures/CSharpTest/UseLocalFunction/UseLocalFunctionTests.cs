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
}", ignoreTrivia: false);
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
}", ignoreTrivia: false);
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
}", ignoreTrivia: false);
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
}", ignoreTrivia: false);
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
}", ignoreTrivia: false);
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
}", ignoreTrivia: false);
        }
    }
}
