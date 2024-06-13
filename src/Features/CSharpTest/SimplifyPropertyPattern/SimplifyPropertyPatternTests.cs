// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyPropertyPattern
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpSimplifyPropertyPatternDiagnosticAnalyzer,
        CSharpSimplifyPropertyPatternCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyPropertyPattern)]
    public class SimplifyPropertyPatternTests
    {
        [Fact]
        public async Task NotInCSharp9()
        {
            var code = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: { EntryPoint: { CallingConvention: CallingConventions.Any } } })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task InCSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { CallingConvention: CallingConventions.Any } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.CallingConvention: CallingConventions.Any })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithoutPropertyPattern1()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: not null })
                        {

                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithoutPropertyPattern2()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint: not null })
                        {

                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithTypePatterm()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: Assembly { EntryPoint: not null } })
                        {

                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithOuterDesignation()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: { EntryPoint: not null } A })
                        {

                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithoutInnerSubpatterns()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: { } })
                        {

                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithMultipleInnerSubpatterns()
        {
            var testCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly: { EntryPoint: { }, Location: { } } })
                        {

                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithInnerDesignation()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { EntryPoint: { } E } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint: { } E })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { [|DeclaringType:|] { Name: "" } } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation2()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { DeclaringType.Name: "" } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation3()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint.DeclaringType:|] { Name: "" } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation4()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { EntryPoint.DeclaringType.Name: "" } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation5()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly.EntryPoint:|] { [|DeclaringType:|] { Name: "" } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation6()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly.EntryPoint:|] { DeclaringType.Name: "" } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task Test_Permutation7()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly.EntryPoint.DeclaringType:|] { Name: "" } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.DeclaringType.Name: "" })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMultiLine1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is
                            {
                                [|Assembly:|]
                                {
                                    EntryPoint:
                                    { }
                                }
                            })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is
                            {
                                Assembly.EntryPoint:
                                { }
                            })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { CallingConvention: CallingConventions.Any } } })
                        {

                        }

                        if (t is { [|Assembly:|] { [|EntryPoint:|] { CallingConvention: CallingConventions.Any } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.CallingConvention: CallingConventions.Any })
                        {

                        }

                        if (t is { Assembly.EntryPoint.CallingConvention: CallingConventions.Any })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOuterDiagnostic()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { CallingConvention: CallingConventions.Any } } })
                        {

                        }
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { Assembly.EntryPoint.CallingConvention: CallingConventions.Any })
                        {

                        }
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
                CodeFixTestBehaviors = Testing.CodeFixTestBehaviors.FixOne,
                DiagnosticSelector = ds => ds[0],
            }.RunAsync();
        }

        [Fact]
        public async Task TestInnerDiagnostic()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                using System;
                using System.Reflection;

                class C
                {
                    void S(Type t)
                    {
                        if (t is { [|Assembly:|] { [|EntryPoint:|] { CallingConvention: CallingConventions.Any } } })
                        {

                        }
                    }
                }
                """,
                FixedState =
                {
                    Sources =
                    {
                        """
                        using System;
                        using System.Reflection;

                        class C
                        {
                            void S(Type t)
                            {
                                if (t is { Assembly: { EntryPoint.CallingConvention: CallingConventions.Any } })
                                {

                                }
                            }
                        }
                        """,
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(8,20): info IDE0170: Simplify property pattern
                        VerifyCS.Diagnostic("IDE0170").WithSpan(8, 20, 8, 29).WithSpan(8, 20, 8, 86).WithSeverity(DiagnosticSeverity.Info),
                    }
                },
                LanguageVersion = LanguageVersion.CSharp10,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck,
                DiagnosticSelector = ds => ds[1],
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57674")]
        public async Task TestTuplePattern()
        {
            var testCode = """
                record R(int Prop);

                class C
                {
                    void S(R r)
                    {
                        _ = (A: r, r) is (A: { Prop: { } }, _);
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57674")]
        public async Task TestPositionalPattern()
        {
            var testCode = """
                record R(R Child, int Value);

                class C
                {
                    void S(R r)
                    {
                        _ = r is R(Child: { Child: { } }, _);
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            }.RunAsync();
        }
    }
}
