// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.HiddenExplicitCast;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.HiddenExplicitCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.HiddenExplicitCast;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpHiddenExplicitCastDiagnosticAnalyzer,
    CSharpHiddenExplicitCastCodeFixProvider>;

public sealed class HiddenExplicitCastTests
{
    [Fact]
    public Task TestInheritanceAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                class Base { }
                class Derived : Base { }

                class Castable
                {
                    public static explicit operator Base(Castable c) => new Base();
                }

                class C
                {
                    static void Main(string[] args)
                    {
                        var v = [|(Derived)new Castable()|]; 
                    }
                }
                """,
            FixedCode = """
                class Base { }
                class Derived : Base { }
                
                class Castable
                {
                    public static explicit operator Base(Castable c) => new Base();
                }
                
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = (Derived)(Base)new Castable(); 
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNullableAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i = [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i = (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNumericAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                struct UnixTime
                {
                    public static explicit operator UnixTime(int i)
                    {
                        return default;
                    }
                }
                
                class C
                {
                    public UnixTime M(long i) => [|(UnixTime)i|];
                }
                """,
            FixedCode = """
                struct UnixTime
                {
                    public static explicit operator UnixTime(int i)
                    {
                        return default;
                    }
                }
                
                class C
                {
                    public UnixTime M(long i) => (UnixTime)(int)i;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestTopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
                S? s = new S();
                int i = [|(int)s|];

                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }
                """,
            FixedCode = """
                S? s = new S();
                int i = (int)(decimal?)s;

                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Fact]
    public Task TestArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        Goo([|(int)s|]);
                    }

                    static void Goo(int i) { }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        Goo((int)(decimal?)s);
                    }
                
                    static void Goo(int i) { }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestAssignment()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        i = [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        i = (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestReturn()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static int Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        return [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static int Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        return (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFieldInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int i = [|(int)new S()|];
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int i = (int)(decimal?)new S();
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestPropertyInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int X => [|(int)new S()|];
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int X => (int)(decimal?)new S();
                }
                """,
        }.RunAsync();
}
