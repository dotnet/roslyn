// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Diagnostics.CSharp.Analyzers.WrapStatements;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests.WrapStatements
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpWrapStatementsDiagnosticAnalyzer,
        CSharpWrapStatementsCodeFixProvider>;

    public class WrapStatementsTests
    {
        [Fact]
        public async Task NoErrorOnWrappedStatement()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true)
            return;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnNonWrappedIfStatement()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true) [|return|];
    }
}";
            var fixedCode = @"class TestClass
{
    void M()
    {
        if (true)
            return;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnElseIf()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true)
            return;
        else if (true)
            return;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnElseWithNonIfStatementOnSameLine()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true)
            return;
        else [|return|];
    }
}";
            var fixedCode = @"class TestClass
{
    void M()
    {
        if (true)
            return;
        else
            return;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnIfWithSingleLineBlock()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true) [|{|] return; }
    }
}";
            var fixedCode = @"class TestClass
{
    void M()
    {
        if (true)
        {
            return;
        }
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task NoWrappingForMemberOrLambdaBlock()
        {
            var source = @"
using System;

class TestClass
{
    void M() { return; }
    void N()
    {
        Action a1 = () => { return; };
        Action a2 = delegate () { return; };
    }

    int Prop1 { get { return 1; } }
    int Prop2
    {
        get { return 1; }
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact]
        public async Task WrappingForLocalFunction()
        {
            var source = @"class TestClass
{
    void N()
    {
        void Local() [|{|] return; }
    }
}";
            var fixedCode = @"class TestClass
{
    void N()
    {
        void Local()
        {
            return;
        }
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnNonWrappedIfStatementWithEmptyBlock()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true) [|{|] }
    }
}";
            var fixedCode = @"class TestClass
{
    void M()
    {
        if (true)
        {
        }
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task WrapLambdaWithNestedStatement()
        {
            var source = @"
using System;

class TestClass
{
    void N()
    {
        Action a1 = () => { [|if|] (true) return; };
    }
}";
            var fixedCode = @"
using System;

class TestClass
{
    void N()
    {
        Action a1 = () =>
        {
            if (true)
                return;
        };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task FixAll1()
        {
            var source = @"class TestClass
{
    void M()
    {
        if (true) [|return|];
        if (true) [|return|];
    }
}";
            var fixedCode = @"class TestClass
{
    void M()
    {
        if (true)
            return;
        if (true)
            return;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
            }.RunAsync();
        }
    }
}
