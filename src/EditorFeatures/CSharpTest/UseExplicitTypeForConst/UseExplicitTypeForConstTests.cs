// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExplicitTypeForConst
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTypeForConst)]
    public sealed class UseExplicitTypeForConstTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new UseExplicitTypeForConstCodeFixProvider());

        [Fact]
        public async Task TestWithIntLiteral()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = 0;
    }
}",
@"class C
{
    void M()
    {
        const int v = 0;
    }
}");
        }

        [Fact]
        public async Task TestWithStringConstant()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    const string s = null;
    void M()
    {
        const [|var|] v = s;
    }
}",
@"class C
{
    const string s = null;
    void M()
    {
        const string v = s;
    }
}");
        }

        [Fact]
        public async Task TestWithQualifiedType()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = default(System.Action);
    }
}",
@"class C
{
    void M()
    {
        const System.Action v = default(System.Action);
    }
}");
        }

        [Fact]
        public async Task TestWithNonConstantInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = System.Console.ReadLine();
    }
}",
@"class C
{
    void M()
    {
        const string v = System.Console.ReadLine();
    }
}");
        }

        [Fact]
        public async Task TestWithNonConstantTupleType()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = (0, true);
    }
}",
@"class C
{
    void M()
    {
        const (int, bool) v = (0, true);
    }
}");
        }

        [Fact]
        public async Task TestNotWithNullLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = null;
    }
}");
        }

        [Fact]
        public async Task TestNotWithDefaultLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = default;
    }
}");
        }

        [Fact]
        public async Task TestNotWithLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = () => { };
    }
}");
        }

        [Fact]
        public async Task TestNotWithAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = new { a = 0 };
    }
}");
        }

        [Fact]
        public async Task TestNotWithArrayInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v = { 0, 1 };
    }
}");
        }

        [Fact]
        public async Task TestNotWithMissingInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] v =
    }
}");
        }

        [Fact]
        public async Task TestNotWithClassVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    class var { }
    void M()
    {
        const [|var|] v = 0;
    }
}");
        }

        [Fact]
        public async Task TestNotOnField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    const [|var|] v = 0;
}");
        }

        [Fact]
        public async Task TestNotWithMultipleDeclarators()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        const [|var|] a = 0, b = 0;
    }
}");
        }
    }
}
