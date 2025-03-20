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
                void M()
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
                int P => {|IDE3000:throw new NotImplementedException()|};
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
                public C()
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
                ~C()
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
                public int this[int index]
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
                public event EventHandler MyEvent
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
                public static C operator +(C a, C b)
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
                void M1()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void M2()
                {
                    {|IDE3000:throw new NotImplementedException("Not implemented");|}
                }

                void M3()
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

                int P1
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                }

                int P2
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                    set { {|IDE3000:throw new NotImplementedException();|} }
                }

                int this[int index]
                {
                    get { {|IDE3000:throw new NotImplementedException();|} }
                    set { {|IDE3000:throw new NotImplementedException();|} }
                }

                void M4()
                {
                    Action action = () => {|IDE3000:throw new NotImplementedException()|};
                    action();
                }

                void M5()
                {
                    Func<int> func = () => {|IDE3000:throw new NotImplementedException()|};
                    func();
                }

                void M6()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }

                void M7()
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
