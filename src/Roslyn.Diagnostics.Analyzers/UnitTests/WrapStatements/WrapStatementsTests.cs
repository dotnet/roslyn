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
    }
}
