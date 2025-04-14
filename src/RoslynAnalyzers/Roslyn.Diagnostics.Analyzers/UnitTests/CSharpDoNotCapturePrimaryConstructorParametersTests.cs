// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.DoNotCapturePrimaryConstructorParametersAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CSharpDoNotCapturePrimaryConstructorParametersTests
    {
        [Fact]
        public async Task ErrorOnCapture_InMethod()
        {
            var source = """
                class C(int i)
                {
                    private int M() => [|i|];
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnCapture_InProperty()
        {
            var source = """
                class C(int i)
                {
                    private int P
                    {
                        get => [|i|];
                        set => [|i|] = value;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnCapture_InIndexer()
        {
            var source = """
                class C(int i)
                {
                    private int this[int param]
                    {
                        get => [|i|];
                        set => [|i|] = value;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnCapture_InEvent()
        {
            var source = """
                class C(int i)
                {
                    public event System.Action E
                    {
                        add => _ = [|i|];
                        remove => _ = [|i|];
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnCapture_UseInSubsequentConstructor()
        {
            var source = """
                class C(int i)
                {
                    C(bool b) : this(1)
                    {
                        _ = i;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(5,13): error RS0103: Primary constructor parameter 'i' should not be implicitly captured
                    VerifyCS.Diagnostic().WithSpan(5, 13, 5, 14).WithArguments("i"),
                    // /0/Test0.cs(5,13): error CS9105: Cannot use primary constructor parameter 'int i' in this context.
                    DiagnosticResult.CompilerError("CS9105").WithSpan(5, 13, 5, 14).WithArguments("int i"),
                }
            }.RunAsync();
        }

        [Fact]
        public async Task NoError_PassToBase()
        {
            var source = """
                class Base(int i);
                class Derived(int i) : Base(i);
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task NoError_FieldInitializer()
        {
            var source = """
                class C(int i)
                {
                    public int I = i;
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task NoError_PropertyInitializer()
        {
            var source = """
                class C(int i)
                {
                    public int I { get; set; } = i;
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task NoError_CapturedInLambda()
        {
            var source = """
                using System;
                public class Base(Action action);
                public class Derived(int i) : Base(() => Console.WriteLine(i));
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task NoError_LocalFunctionParameterReference()
        {
            var source = """
                using System;
                class C
                {
                    void M()
                    {
                        Nested1(1);

                        void Nested1(int i)
                        {
                            Nested2();

                            void Nested2() => Console.WriteLine(i);
                        }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }
    }
}
