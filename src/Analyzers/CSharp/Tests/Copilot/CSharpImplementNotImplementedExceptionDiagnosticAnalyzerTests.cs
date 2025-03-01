// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
        EmptyCodeFixProvider>;

    public class CSharpImplementNotImplementedExceptionDiagnosticAnalyzerTests
    {
        [Fact]
        public async Task TestThrowNotImplementedException()
        {
            var testCode = """
                using System;

                class C
                {
                    void M()
                    {
                        [|throw new NotImplementedException();|]
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = testCode,
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic().WithSpan(7, 9, 7, 45)
                },
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestDifferentFlavorsOfThrowNotImplementedException()
        {
            var testCode = """
                using System;

                class C
                {
                    void M1()
                    {
                        [|throw new NotImplementedException("Not implemented");|]
                    }

                    void M2()
                    {
                        [|throw new NotImplementedException("Not implemented");|]
                    }

                    void M3()
                    {
                        try
                        {
                            // Some code
                        }
                        catch (Exception)
                        {
                            [|throw new NotImplementedException();|]
                        }
                    }

                    int P1
                    {
                        get { [|throw new NotImplementedException();|] }
                    }

                    int P2
                    {
                        get { [|throw new NotImplementedException();|] }
                        set { [|throw new NotImplementedException();|] }
                    }

                    int this[int index]
                    {
                        get { [|throw new NotImplementedException();|] }
                        set { [|throw new NotImplementedException();|] }
                    }

                    void M4()
                    {
                        Action action = () => [|throw new NotImplementedException();|];
                        action();
                    }

                    void M5()
                    {
                        Func<int> func = () => [|throw new NotImplementedException();|];
                        func();
                    }

                    void M6()
                    {
                        [|throw new NotImplementedException();|]
                    }

                    void M7()
                    {
                        [|throw new NotImplementedException("Not implemented");|]
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = testCode,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }
    }
}
