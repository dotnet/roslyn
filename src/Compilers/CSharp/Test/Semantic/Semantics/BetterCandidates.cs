// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests for improved overload candidate selection.
    /// See also https://github.com/dotnet/csharplang/issues/98.
    /// </summary>
    public class BetterCandidates : CompilingTestBase
    {
        private CSharpCompilation CreateCompilationWithoutBetterCandidates(string source, CSharpCompilationOptions options = null)
        {
            return CreateStandardCompilation(source, options: options, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
        }
        private CSharpCompilation CreateCompilationWithBetterCandidates(string source, CSharpCompilationOptions options = null)
        {
            Debug.Assert(TestOptions.Regular.LanguageVersion >= MessageID.IDS_FeatureImprovedOverloadCandidates.RequiredVersion());
            return CreateStandardCompilation(source, options: options, parseOptions: TestOptions.Regular);
        }

        //When a method group contains both instance and static members, we discard the instance members if invoked with a static receiver.
        [Fact]
        public void TestStaticReceiver01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        Program.M(null);
    }
    public static void M(A a) { System.Console.WriteLine(1); }
    public void M(B b) { System.Console.WriteLine(2); }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         Program.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 17)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        Program p = new Program();
        p.M(null);
    }
    public static void M(A a) { System.Console.WriteLine(1); }
    public void M(B b) { System.Console.WriteLine(2); }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,11): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         p.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(6, 11)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //When a method group contains no receiver in a static context, we include only static members.
        // Type 1: in a static method
        [Fact]
        public void TestStaticContext01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        M(null);
    }
    public static void M(A a) { System.Console.WriteLine(1); }
    public void M(B b) { System.Console.WriteLine(2); }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        //When a method group contains no receiver in a static context, we include only static members.
        // Type 2: in a field initializer
        [Fact]
        public void TestStaticContext02()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        new Program();
    }
    public static int M(A a) { System.Console.WriteLine(1); return 1; }
    public int M(B b) { System.Console.WriteLine(2); return 2; }
    int X = M(null);
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (9,13): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //     int X = M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(9, 13)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        //When a method group contains no receiver in a static context, we include only static members.
        // Type 4: in a constructor-initializer
        [Fact]
        public void TestStaticContext04()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        new Program();
    }
    public Program() : this(M(null)) {}
    public Program(int x) {}
    public static int M(A a) { System.Console.WriteLine(1); return 1; }
    public int M(B b) { System.Console.WriteLine(2); return 2; }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (7,29): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //     public Program() : this(M(null)) {}
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(7, 29)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        //When a method group contains no receiver in a static context, we include only static members.
        // Type 5: in an attribute argument
        [Fact]
        public void TestStaticContext05()
        {
            var source =
@"public class Program
{
    public static int M(A a) { System.Console.WriteLine(1); return 1; }
    public int M(B b) { System.Console.WriteLine(2); return 2; }

    [My(M(null))]
    public int x;
}
public class A {}
public class B {}
public class MyAttribute : System.Attribute
{
    public MyAttribute(int value) {}
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //     [My(M(null))]
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(6, 9)
                );
            CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,9): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [My(M(null))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "M(null)").WithLocation(6, 9)
                );
        }

        //When a method group contains no receiver, we include both static and instance members in an other-than-static context. i.e. discard nothing.
        [Fact]
        public void TestInstanceContext01()
        {
            var source =
@"public class Program
{
    public void M()
    {
        M(null);
    }

    public static int M(A a) => 1;
    public int M(B b) => 2;
}
public class A {}
public class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 9)
                );
            CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 9)
                );
        }

        //When a method group receiver is ambiguously an instance or type due to a color-color situation, we include both instance and static candidates.
        [Fact]
        public void TestAmbiguousContext01()
        {
            var source =
@"public class Color
{
    public void M()
    {
        Color Color = null;
        Color.M(null);
    }

    public static int M(A a) => 1;
    public int M(B b) => 2;
}
public class A {}
public class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,15): error CS0121: The call is ambiguous between the following methods or properties: 'Color.M(A)' and 'Color.M(B)'
                //         Color.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Color.M(A)", "Color.M(B)").WithLocation(6, 15)
                );
            CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,15): error CS0121: The call is ambiguous between the following methods or properties: 'Color.M(A)' and 'Color.M(B)'
                //         Color.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Color.M(A)", "Color.M(B)").WithLocation(6, 15)
                );
        }

        //When a method group contains some generic methods whose type parameters do not satisfy their constraints, these members are removed from the candidate set.
        [Fact]
        public void TestConstraintFailed01()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(new A(), 0);
    }

    static void M<T>(T t1, int i) where T: B { System.Console.WriteLine(1); }
    static void M<T>(T t1, short s) { System.Console.WriteLine(2); }
}
public class A {}
public class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M<T>(T, int)'. There is no implicit reference conversion from 'A' to 'B'.
                //         M(new A(), 0);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("Program.M<T>(T, int)", "B", "T", "A").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //When a method group contains some generic methods whose type parameters do not satisfy their constraints, these members are removed from the candidate set.
        // Test that this permits overload resolution to use type parameter constraints "as a tie-breaker" to guide overload resolution.
        [Fact]
        public void TestConstraintFailed02()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(new A(), null);
        M(new B(), null);
    }

    static void M<T>(T t1, B b) where T: struct { System.Console.Write(""struct ""); }
    static void M<T>(T t1, X s) where T : class { System.Console.Write(""class ""); }
}
public struct A {}
public class B {}
public class X {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, B)' and 'Program.M<T>(T, X)'
                //         M(new A(), null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, B)", "Program.M<T>(T, X)").WithLocation(5, 9),
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, B)' and 'Program.M<T>(T, X)'
                //         M(new B(), null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, B)", "Program.M<T>(T, X)").WithLocation(6, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "struct class ");
        }

        //For a method group conversion, candidate methods whose return type doesn't match up with the delegate's return type are removed from the set.
        [Fact]
        public void TestReturnTypeMismatch01()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(Program.Q);
    }

    static void M(D1 d) { System.Console.WriteLine(1); }
    static void M(D2 d) { System.Console.WriteLine(2); }

    static void Q(A a) { }
    static void Q(B b) { }
}
delegate int D1(A a);
delegate void D2(B b);

class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(D1)' and 'Program.M(D2)'
                //         M(Q);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(D1)", "Program.M(D2)").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //For a method group conversion, candidate methods whose return ref kind doesn't match up with the delegate's return ref kind are removed from the set.
        [Fact]
        public void TestReturnRefMismatch01()
        {
            var source =
@"public class Program
{
    static int tmp;
    static void Main()
    {
        M(Q);
    }

    static void M(D1 d) { System.Console.WriteLine(1); }
    static void M(D2 d) { System.Console.WriteLine(2); }

    static ref int Q() { return ref tmp; }
}
delegate int D1();
delegate ref int D2();
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(D1)' and 'Program.M(D2)'
                //         M(Q);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(D1)", "Program.M(D2)").WithLocation(6, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //For a method group conversion, candidate methods whose return type doesn't match up with the delegate's return type are removed from the set.
        [Fact]
        public void TestReturnTypeMismatch02()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(new Z().Q);
    }

    static void M(D1 d) { System.Console.WriteLine(1); }
    static void M(D2 d) { System.Console.WriteLine(2); }
}
delegate int D1(A a);
delegate void D2(B b);

public class A {}
public class B {}
public class Z {}
public static class X
{
    public static void Q(this Z z, A a) {}
    public static void Q(this Z z, B b) {}
}
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : System.Attribute {}
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(D1)' and 'Program.M(D2)'
                //         M(new Z().Q);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(D1)", "Program.M(D2)").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        // Test suggested by @VSadov
        // 1) one candidate is generic, but candidate fails constraints, while another overload requires a conversion. Used to be an error, second should be picked now.
        [Fact]
        public void TestConstraintFailed03()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(new A(), 0);
    }

    static void M<T>(T t1, int i) where T: B { System.Console.WriteLine(1); }
    static void M(C c, short s) { System.Console.WriteLine(2); }
}
public class A {}
public class B {}
public class C { public static implicit operator C(A a) => null; }
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M<T>(T, int)'. There is no implicit reference conversion from 'A' to 'B'.
                //         M(new A(), 0);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("Program.M<T>(T, int)", "B", "T", "A").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        // Test suggested by @VSadov
        // 2) one candidate is generic without constraints, but we pass a ref-struct to it, which cannot be a generic type arg, another candidate requires a conversion and now works.
        [Fact]
        public void TestConstraintFailed04()
        {
            var source =
@"public class Program
{
    static void Main()
    {
        M(new A(), 0);
    }

    static void M<T>(T t1, int i) { System.Console.WriteLine(1); }
    static void M(C c, short s) { System.Console.WriteLine(2); }
}
public ref struct A {}
public class C { public static implicit operator C(A a) => null; }
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0306: The type 'A' may not be used as a type argument
                //         M(new A(), 0);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M").WithArguments("A").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        // Test suggested by @VSadov
        // 3) one candidate is generic without constraints, but we pass a pointer to it, which cannot be a generic type arg, another candidate requires a conversion and now works.
        [Fact]
        public void TestConstraintFailed05()
        {
            var source =
@"public class Program
{
    static unsafe void Main()
    {
        int *p = null;
        M(p, 0);
    }

    static void M<T>(T t1, int i) { System.Console.WriteLine(1); }
    static void M(C c, short s) { System.Console.WriteLine(2); }
}
public class C { public static unsafe implicit operator C(int* p) => null; }
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,9): error CS0306: The type 'int*' may not be used as a type argument
                //         M(p, 0);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M").WithArguments("int*").WithLocation(6, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(true)).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "2", verify: Verification.Skipped);
        }
    }
}
