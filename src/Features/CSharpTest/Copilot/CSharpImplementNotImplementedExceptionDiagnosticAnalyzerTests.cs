// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
    EmptyCodeFixProvider>;

public sealed class CSharpImplementNotImplementedExceptionDiagnosticAnalyzerTests
{
    [Fact]
    public async Task TestThrowNotImplementedExceptionInStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                int {|IDE3000:P|} => {|IDE3000:throw new NotImplementedException()|};
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public {|IDE3000:C|}()
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInDestructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                ~{|IDE3000:C|}()
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInIndexer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public int {|IDE3000:this|}[int index]
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                    set { {|IDE3000:throw new NotImplementedException();|} }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInEvent()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public event EventHandler {|IDE3000:MyEvent|}
                {
                    add { {|IDE3000:throw new NotImplementedException();|} }
                    remove { {|IDE3000:throw new NotImplementedException();|} }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestThrowNotImplementedExceptionInOperator()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                public static C operator {|IDE3000:+|}(C a, C b)
                {
                    {|IDE3000:throw new NotImplementedException();|} 
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestDifferentFlavorsOfThrowNotImplementedException()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void {|IDE3000:M1|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void {|IDE3000:M2|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void {|IDE3000:M3|}()
                {
                    try
                    {
                        // Some code
                    }
                    catch (Exception)
                    {
                        {|IDE3000:throw new NotImplementedException();|}
                    }
                }

                int {|IDE3000:P1|}
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                }

                int {|IDE3000:P2|}
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                    set { {|IDE3000:throw new NotImplementedException();|} }
                }

                int {|IDE3000:this|}[int index]
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                    set { {|IDE3000:throw new NotImplementedException();|} }
                }

                void {|IDE3000:M4|}()
                {
                    Action action = () => {|IDE3000:throw new NotImplementedException()|};
                    action();
                }

                void {|IDE3000:M5|}()
                {
                    Func<int> func = () => {|IDE3000:throw new NotImplementedException()|};
                    func();
                }

                void {|IDE3000:M6|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }

                void {|IDE3000:M7|}()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }
}
