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
        public Task ErrorOnCapture_InMethod()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    private int M() => [|i|];
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task ErrorOnCapture_InProperty()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    private int P
                    {
                        get => [|i|];
                        set => [|i|] = value;
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task ErrorOnCapture_InIndexer()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    private int this[int param]
                    {
                        get => [|i|];
                        set => [|i|] = value;
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task ErrorOnCapture_InEvent()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    public event System.Action E
                    {
                        add => _ = [|i|];
                        remove => _ = [|i|];
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task ErrorOnCapture_UseInSubsequentConstructor()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    C(bool b) : this(1)
                    {
                        _ = i;
                    }
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
                ExpectedDiagnostics = {
                    // /0/Test0.cs(5,13): error RS0103: Primary constructor parameter 'i' should not be implicitly captured
                    VerifyCS.Diagnostic().WithSpan(5, 13, 5, 14).WithArguments("i"),
                    // /0/Test0.cs(5,13): error CS9105: Cannot use primary constructor parameter 'int i' in this context.
                    DiagnosticResult.CompilerError("CS9105").WithSpan(5, 13, 5, 14).WithArguments("int i"),
                }
            }.RunAsync();

        [Fact]
        public Task NoError_PassToBase()
            => new VerifyCS.Test
            {
                TestCode = """
                class Base(int i);
                class Derived(int i) : Base(i);
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task NoError_FieldInitializer()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    public int I = i;
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task NoError_PropertyInitializer()
            => new VerifyCS.Test
            {
                TestCode = """
                class C(int i)
                {
                    public int I { get; set; } = i;
                }
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task NoError_CapturedInLambda()
            => new VerifyCS.Test
            {
                TestCode = """
                using System;
                public class Base(Action action);
                public class Derived(int i) : Base(() => Console.WriteLine(i));
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();

        [Fact]
        public Task NoError_LocalFunctionParameterReference()
            => new VerifyCS.Test
            {
                TestCode = """
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
                """,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
    }
}
