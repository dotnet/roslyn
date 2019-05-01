// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeLocalFunctionStatic
{
    public partial class MakeLocalFunctionStaticTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new MakeLocalFunctionStaticDiagnosticAnalyzer(), new MakeLocalFunctionStaticCodeFixProvider());

        private static ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAboveCSharp8()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        static int fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithOptionOff()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
new TestParameters(
    parseOptions: CSharp8ParseOptions,
    options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOptions.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfAlreadyStatic()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        static int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingPriorToCSharp8()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp72ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfCapturesValue()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(int i)
    {
        int [||]fibonacci(int n)
        {
            return i <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfCapturesThis()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            M();
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAsyncFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async Task<int> [||]fibonacci(int n)
        {
            return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
        }
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        static async Task<int> fibonacci(int n)
        {
            return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }
    }
}
