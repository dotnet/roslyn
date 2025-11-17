// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests for improved overload candidate selection.
    /// See also https://github.com/dotnet/csharplang/issues/98.
    /// </summary>
    public class BetterCandidates : CompilingTestBase
    {
        private CSharpCompilation CreateCompilationWithoutBetterCandidates(string source, CSharpCompilationOptions options = null, MetadataReference[] references = null)
        {
            return CreateCompilation(source, options: options, references: references, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
        }
        private CSharpCompilation CreateCompilationWithBetterCandidates(string source, CSharpCompilationOptions options = null, MetadataReference[] references = null)
        {
            Debug.Assert(TestOptions.Regular.LanguageVersion >= MessageID.IDS_FeatureImprovedOverloadCandidates.RequiredVersion());
            return CreateCompilation(source, options: options, references: references, parseOptions: TestOptions.Regular);
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

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver02()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        Program p = new Program();
        p.Main2();
    }
    void Main2()
    {
        this.M(null);
    }
    public static void M(A a) { System.Console.WriteLine(1); }
    public void M(B b) { System.Console.WriteLine(2); }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (10,14): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         this.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(10, 14)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver02b()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        D d = new D();
        d.Main2();
    }
    public static void M(A a) { System.Console.WriteLine(1); }
    public void M(B b) { System.Console.WriteLine(2); }
}
class D : Program
{
    public void Main2()
    {
        base.M(null);
    }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (15,14): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         base.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(15, 14)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver03()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        new MyCollection { null };
    }
}
class A {}
class B {}
class MyCollection : System.Collections.IEnumerable
{
    public static void Add(A a) { System.Console.WriteLine(1); }
    public void Add(B b) { System.Console.WriteLine(2); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,28): error CS0121: The call is ambiguous between the following methods or properties: 'MyCollection.Add(A)' and 'MyCollection.Add(B)'
                //         new MyCollection { null };
                Diagnostic(ErrorCode.ERR_AmbigCall, "null").WithArguments("MyCollection.Add(A)", "MyCollection.Add(B)").WithLocation(5, 28)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "2");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver04()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var c = new MyCollection();
        foreach (var q in c) { }
    }
}
class A {}
class B {}
class MyCollection : System.Collections.IEnumerable
{
    public static System.Collections.IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        System.Console.Write(1);
        return new MyEnumerator();
    }
}
class MyEnumerator : System.Collections.IEnumerator
{
    object System.Collections.IEnumerator.Current => throw null;
    bool System.Collections.IEnumerator.MoveNext()
    {
        System.Console.WriteLine(2);
        return false;
    }
    void System.Collections.IEnumerator.Reset() => throw null;
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,27): warning CS0279: 'MyCollection' does not implement the 'collection' pattern. 'MyCollection.GetEnumerator()' is not a public instance or extension method.
                //         foreach (var q in c) { }
                Diagnostic(ErrorCode.WRN_PatternNotPublicOrNotInstance, "c").WithArguments("MyCollection", "collection", "MyCollection.GetEnumerator()").WithLocation(6, 27)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "12");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver05()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var c = new MyCollection();
        foreach (var q in c) { }
    }
}
class A {}
class B {}
class MyCollection
{
    public MyEnumerator GetEnumerator()
    {
        return new MyEnumerator();
    }
}
class MyEnumerator
{
    public object Current => throw null;
    public bool MoveNext()
    {
        System.Console.WriteLine(2);
        return false;
    }
    public static bool MoveNext() => throw null;
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (26,24): error CS0111: Type 'MyEnumerator' already defines a member called 'MoveNext' with the same parameter types
                //     public static bool MoveNext() => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "MoveNext").WithArguments("MoveNext", "MyEnumerator").WithLocation(26, 24),
                // (6,27): error CS0202: foreach requires that the return type 'MyEnumerator' of 'MyCollection.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                //         foreach (var q in c) { }
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "c").WithArguments("MyEnumerator", "MyCollection.GetEnumerator()").WithLocation(6, 27)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (26,24): error CS0111: Type 'MyEnumerator' already defines a member called 'MoveNext' with the same parameter types
                //     public static bool MoveNext() => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "MoveNext").WithArguments("MoveNext", "MyEnumerator").WithLocation(26, 24)
                );
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver06()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var o = new MyDeconstructable();
        (var a, var b) = o;
        System.Console.WriteLine(a);
    }
}
class MyDeconstructable
{
    public void Deconstruct(out int a, out int b) => (a, b) = (1, 2);
    public static void Deconstruct(out long a, out long b) => (a, b) = (3, 4);
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,26): error CS0121: The call is ambiguous between the following methods or properties: 'MyDeconstructable.Deconstruct(out int, out int)' and 'MyDeconstructable.Deconstruct(out long, out long)'
                //         (var a, var b) = o;
                Diagnostic(ErrorCode.ERR_AmbigCall, "o").WithArguments("MyDeconstructable.Deconstruct(out int, out int)", "MyDeconstructable.Deconstruct(out long, out long)").WithLocation(6, 26)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver07()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        M(new MyTask<int>(3)).GetAwaiter().GetResult();
    }
    static async System.Threading.Tasks.Task M(MyTask<int> x)
    {
        var z = await x;
        System.Console.WriteLine(z);
    }
}

public class MyTask<TResult>
{
    MyTaskAwaiter<TResult> awaiter;
    public MyTask(TResult value)
    {
        this.awaiter = new MyTaskAwaiter<TResult>(value);
    }
    public static MyTaskAwaiter<TResult> GetAwaiter() => null;
}
public class MyTaskAwaiter<TResult> : System.Runtime.CompilerServices.INotifyCompletion
{
    TResult value;
    public MyTaskAwaiter(TResult value)
    {
        this.value = value;
    }
    public bool IsCompleted { get => true; }
    public TResult GetResult() => value;
    public void OnCompleted(System.Action continuation) => throw null;
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (9,17): error CS1986: 'await' requires that the type MyTask<int> have a suitable GetAwaiter method
                //         var z = await x;
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await x").WithArguments("MyTask<int>").WithLocation(9, 17)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (9,17): error CS1986: 'await' requires that the type MyTask<int> have a suitable GetAwaiter method
                //         var z = await x;
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await x").WithArguments("MyTask<int>").WithLocation(9, 17)
                );
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver08()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        M(new MyTask<int>(3)).GetAwaiter().GetResult();
    }
    static async System.Threading.Tasks.Task M(MyTask<int> x)
    {
        var z = await x;
        System.Console.WriteLine(z);
    }
}

public class MyTask<TResult>
{
    MyTaskAwaiter<TResult> awaiter;
    public MyTask(TResult value)
    {
        this.awaiter = new MyTaskAwaiter<TResult>(value);
    }
    public MyTaskAwaiter<TResult> GetAwaiter() => awaiter;
}
public struct MyTaskAwaiter<TResult> : System.Runtime.CompilerServices.INotifyCompletion
{
    TResult value;
    public MyTaskAwaiter(TResult value)
    {
        this.value = value;
    }
    public bool IsCompleted { get => true; }
    public static TResult GetResult() => throw null;
    public void OnCompleted(System.Action continuation) => throw null;
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (9,17): error CS0176: Member 'MyTaskAwaiter<int>.GetResult()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var z = await x;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await x").WithArguments("MyTaskAwaiter<int>.GetResult()").WithLocation(9, 17)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (9,17): error CS0176: Member 'MyTaskAwaiter<int>.GetResult()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var z = await x;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await x").WithArguments("MyTaskAwaiter<int>.GetResult()").WithLocation(9, 17)
                );
        }

        //When a method group contains both instance and static members, we discard the static members if invoked with an instance receiver.
        [Fact]
        public void TestInstanceReceiver09()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var q = from x in new Q() select x;
    }
}
class Q
{
    public static object Select(Func<A, A> y)
    {
        Console.WriteLine(1);
        return null;
    }
    public object Select(Func<B, B> y)
    {
        Console.WriteLine(2);
        return null;
    }
}
class A {}
class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,35): error CS1940: Multiple implementations of the query pattern were found for source type 'Q'.  Ambiguous call to 'Select'.
                //         var q = from x in new Q() select x;
                Diagnostic(ErrorCode.ERR_QueryMultipleProviders, "select x").WithArguments("Q", "Select").WithLocation(6, 35)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                );
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

        //When a method group contains no receiver in a static context, we include only static members.
        // In a default parameter value
        [Fact]
        public void TestStaticContext06()
        {
            var source =
@"public class Program
{
    public static int M(A a) { System.Console.WriteLine(1); return 1; }
    public int M(B b) { System.Console.WriteLine(2); return 2; }
    public void Q(int x = M(null))
    {
    }
}
public class A {}
public class B {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (5,27): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //     public void Q(int x = M(null))
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 27)
                );
            CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (5,27): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //     public void Q(int x = M(null))
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(5, 27)
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

        //For a method group conversion, candidate methods whose return ref kind doesn't match up with the delegate's return ref kind are removed from the set.
        [Fact]
        public void TestReturnRefMismatch02()
        {
            var source =
@"public class Program
{
    static int tmp = 2;
    static void Main()
    {
        M(Q);
    }

    static void M(D1 d) { System.Console.WriteLine(1); }
    static void M(D2 d) { System.Console.WriteLine(2); }

    static int Q() { return tmp; }
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
            CompileAndVerify(compilation, expectedOutput: "1");
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
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics();
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
                // (5,9): error CS9244: The type 'A' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Program.M<T>(T, int)'
                //         M(new A(), 0);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("Program.M<T>(T, int)", "T", "A").WithLocation(5, 9)
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

        [ClrOnlyFact]
        public void IndexedPropertyTest01()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class C
    Public Property P(a As A) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Public Shared Property P(b As B) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
Public Class A
End Class
Public Class B
End Class
";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class D : C
{
    static void Main()
    {
    }
    void M()
    {
        object o;
        o = P[null];
        P[null] = o;
        o = this.P[null];
        base.P[null] = o;
        o = D.P[null];   // C# does not support static indexed properties
        D.P[null] = o;   // C# does not support static indexed properties
    }
}";
            CreateCompilationWithoutBetterCandidates(source2, references: new[] { reference1 }, options: TestOptions.ReleaseExe.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (13,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.P[A]'
                //         o = D.P[null];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "D.P[null]").WithArguments("C.P[A]").WithLocation(13, 13),
                // (14,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.P[A]'
                //         D.P[null] = o;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "D.P[null]").WithArguments("C.P[A]").WithLocation(14, 9)
                );
            CreateCompilationWithBetterCandidates(source2, references: new[] { reference1 }, options: TestOptions.ReleaseExe.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (13,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.P[A]'
                //         o = D.P[null];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "D.P[null]").WithArguments("C.P[A]").WithLocation(13, 13),
                // (14,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.P[A]'
                //         D.P[null] = o;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "D.P[null]").WithArguments("C.P[A]").WithLocation(14, 9)
                );
        }

        [Fact]
        public void TestAmbiguous01()
        {
            // test semantic model in the face of ambiguities even when there are static/instance violations
            var source =
@"class Program
{
    public static void Main()
    {
        Program p = null;
        Program.M(null); // two static candidates
        p.M(null);       // two instance candidates
        M(null);         // two static candidates
    }
    void Q()
    {
        Program Program = null;
        M(null);         // four candidates
        Program.M(null); // four candidates
    }
    public static void M(A a) => throw null;
    public void M(B b) => throw null;
    public static void M(C c) => throw null;
    public void M(D d) => throw null;
}
class A {}
class B {}
class C {}
class D {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         Program.M(null); // two static candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(6, 17),
                // (7,11): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         p.M(null);       // two instance candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(7, 11),
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);         // two static candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(8, 9),
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);         // four candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(13, 9),
                // (14,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         Program.M(null); // four candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(14, 17)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(C)'
                //         Program.M(null); // two static candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(C)").WithLocation(6, 17),
                // (7,11): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(B)' and 'Program.M(D)'
                //         p.M(null);       // two instance candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(B)", "Program.M(D)").WithLocation(7, 11),
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(C)'
                //         M(null);         // two static candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(C)").WithLocation(8, 9),
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null);         // four candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(13, 9),
                // (14,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         Program.M(null); // four candidates
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(14, 17)
                );
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            var invocations = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(5, invocations.Length);

            var symbolInfo = model.GetSymbolInfo(invocations[0].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(invocations[1].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(invocations[2].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(invocations[3].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(invocations[4].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());
        }

        [Fact]
        public void TestAmbiguous02()
        {
            // test semantic model in the face of ambiguities even when there are constraint violations
            var source =
@"class Program
{
    public static void Main()
    {
        M(1, null);
    }
    public static void M<T>(T t, A a) where T : Constraint => throw null;
    public static void M<T>(T t, B b) => throw null;
    public static void M<T>(T t, C c) where T : Constraint => throw null;
    public static void M<T>(T t, D d) => throw null;
}
class A {}
class B {}
class C {}
class D {}
class Constraint {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, A)' and 'Program.M<T>(T, B)'
                //         M(1, null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, A)", "Program.M<T>(T, B)").WithLocation(5, 9)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M<T>(T, B)' and 'Program.M<T>(T, D)'
                //         M(1, null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M<T>(T, B)", "Program.M<T>(T, D)").WithLocation(5, 9)
                );
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            var invocations = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(1, invocations.Length);

            var symbolInfo = model.GetSymbolInfo(invocations[0].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void Program.M<System.Int32>(System.Int32 t, A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Program.M<System.Int32>(System.Int32 t, B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void Program.M<System.Int32>(System.Int32 t, C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void Program.M<System.Int32>(System.Int32 t, D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());
        }

        [Fact]
        public void TestAmbiguous03()
        {
            // test semantic model in the face of ambiguities even when there are constraint violations
            var source =
@"class Program
{
    public static void Main()
    {
        1.M(null);
    }
}
public class A {}
public class B {}
public class C {}
public class D {}
public class Constraint {}
public static class Extensions
{
    public static void M<T>(this T t, A a) where T : Constraint => throw null;
    public static void M<T>(this T t, B b) => throw null;
    public static void M<T>(this T t, C c) where T : Constraint => throw null;
    public static void M<T>(this T t, D d) => throw null;
}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,11): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions.M<T>(T, A)' and 'Extensions.M<T>(T, B)'
                //         1.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Extensions.M<T>(T, A)", "Extensions.M<T>(T, B)").WithLocation(5, 11)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,11): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions.M<T>(T, B)' and 'Extensions.M<T>(T, D)'
                //         1.M(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Extensions.M<T>(T, B)", "Extensions.M<T>(T, D)").WithLocation(5, 11)
                );
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            var invocations = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(1, invocations.Length);

            var symbolInfo = model.GetSymbolInfo(invocations[0].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void System.Int32.M<System.Int32>(A a)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void System.Int32.M<System.Int32>(B b)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("void System.Int32.M<System.Int32>(C c)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("void System.Int32.M<System.Int32>(D d)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());
        }

        [Fact]
        public void TestAmbiguous04()
        {
            // test semantic model in the face of ambiguities even when there are return type mismatches
            var source =
@"class Program
{
    public static void Main()
    {
        Invoked(Argument);
    }
    public static void Invoked(Delegate d)
    {
    }
    public delegate A Delegate(IZ c);

    static B Argument(IQ x) => null;
    static D Argument(IW x) => null;
    static C Argument(IX x) => null;
    static D Argument(IY x) => null;

}
class A {} class B: A {} class C: A {}
class D {}
interface IQ {}
interface IW {}
interface IX {}
interface IY {}
interface IZ: IQ, IW, IX, IY {}
";
            CreateCompilationWithoutBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Argument(IQ)' and 'Program.Argument(IW)'
                //         Invoked(Argument);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Argument").WithArguments("Program.Argument(IQ)", "Program.Argument(IW)").WithLocation(5, 17)
                );
            var compilation = CreateCompilationWithBetterCandidates(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (5,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Argument(IQ)' and 'Program.Argument(IX)'
                //         Invoked(Argument);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Argument").WithArguments("Program.Argument(IQ)", "Program.Argument(IX)").WithLocation(5, 17)
                );
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            var invocations = compilation.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(1, invocations.Length);

            var symbolInfo = model.GetSymbolInfo(invocations[0].ArgumentList.Arguments[0].Expression);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("B Program.Argument(IQ x)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("D Program.Argument(IW x)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal("C Program.Argument(IX x)", symbolInfo.CandidateSymbols[2].ToTestDisplayString());
            Assert.Equal("D Program.Argument(IY x)", symbolInfo.CandidateSymbols[3].ToTestDisplayString());
        }
    }
}
