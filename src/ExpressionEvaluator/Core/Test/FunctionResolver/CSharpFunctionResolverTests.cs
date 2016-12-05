// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public class CSharpFunctionResolverTests : CSharpTestBase
    {
        [Fact]
        public void OnLoad()
        {
            var source =
@"class C
{
    static void F(object o) { }
    object F() => null;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            var module = new Module(compilation.EmitToArray());
            using (var process = new Process())
            {
                var resolver = new Resolver();
                var request = new Request(null, MemberSignatureParser.Parse("C.F"));
                resolver.EnableResolution(process, request);
                VerifySignatures(request);
                process.AddModule(module);
                resolver.OnModuleLoad(process, module);
                VerifySignatures(request, "C.F(System.Object)", "C.F()");
            }
        }

        /// <summary>
        /// ShouldEnableFunctionResolver should not be called
        /// until the first module is available for binding.
        /// </summary>
        [Fact]
        public void ShouldEnableFunctionResolver()
        {
            var sourceA =
@"class A
{
    static void F() { }
    static void G() { }
}";
            var sourceB =
@"class B
{
    static void F() { }
    static void G() { }
}";
            var bytesA = CreateCompilationWithMscorlib(sourceA).EmitToArray();
            var bytesB = CreateCompilationWithMscorlib(sourceB).EmitToArray();
            var resolver = new Resolver();

            // Two modules loaded before two global requests,
            // ... resolver enabled.
            var moduleA = new Module(bytesA, name: "A.dll");
            var moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: true, modules: new[] { moduleA, moduleB }))
            {
                var requestF = new Request(null, MemberSignatureParser.Parse("F"));
                var requestG = new Request(null, MemberSignatureParser.Parse("G"));
                Assert.Equal(0, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestF);
                Assert.Equal(1, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF, "A.F()", "B.F()");
                VerifySignatures(requestG, "A.G()", "B.G()");
            }

            // ... resolver disabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: false, modules: new[] { moduleA, moduleB }))
            {
                var requestF = new Request(null, MemberSignatureParser.Parse("F"));
                var requestG = new Request(null, MemberSignatureParser.Parse("G"));
                Assert.Equal(0, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestF);
                Assert.Equal(1, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
            }

            // Two modules loaded before two requests for same module,
            // ... resolver enabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: true, modules: new[] { moduleA, moduleB }))
            {
                var requestF = new Request("B.dll", MemberSignatureParser.Parse("F"));
                var requestG = new Request("B.dll", MemberSignatureParser.Parse("G"));
                Assert.Equal(0, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestF);
                Assert.Equal(1, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF, "B.F()");
                VerifySignatures(requestG, "B.G()");
            }

            // ... resolver disabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: false, modules: new[] { moduleA, moduleB }))
            {
                var requestF = new Request("B.dll", MemberSignatureParser.Parse("F"));
                var requestG = new Request("B.dll", MemberSignatureParser.Parse("G"));
                Assert.Equal(0, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestF);
                Assert.Equal(1, process.ShouldEnableRequests);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
            }

            // Two modules loaded after two global requests,
            // ... resolver enabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: true))
            {
                var requestF = new Request(null, MemberSignatureParser.Parse("F"));
                var requestG = new Request(null, MemberSignatureParser.Parse("G"));
                resolver.EnableResolution(process, requestF);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(0, process.ShouldEnableRequests);
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, process.ShouldEnableRequests);
                VerifySignatures(requestF, "A.F()");
                VerifySignatures(requestG, "A.G()");
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF, "A.F()", "B.F()");
                VerifySignatures(requestG, "A.G()", "B.G()");
            }

            // ... resolver enabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: false))
            {
                var requestF = new Request(null, MemberSignatureParser.Parse("F"));
                var requestG = new Request(null, MemberSignatureParser.Parse("G"));
                resolver.EnableResolution(process, requestF);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(0, process.ShouldEnableRequests);
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
            }

            // Two modules after two requests for same module,
            // ... resolver enabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: true))
            {
                var requestF = new Request("A.dll", MemberSignatureParser.Parse("F"));
                var requestG = new Request("A.dll", MemberSignatureParser.Parse("G"));
                resolver.EnableResolution(process, requestF);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(0, process.ShouldEnableRequests);
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, process.ShouldEnableRequests);
                VerifySignatures(requestF, "A.F()");
                VerifySignatures(requestG, "A.G()");
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF, "A.F()");
                VerifySignatures(requestG, "A.G()");
            }

            // ... resolver enabled.
            moduleA = new Module(bytesA, name: "A.dll");
            moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: false))
            {
                var requestF = new Request("A.dll", MemberSignatureParser.Parse("F"));
                var requestG = new Request("A.dll", MemberSignatureParser.Parse("G"));
                resolver.EnableResolution(process, requestF);
                resolver.EnableResolution(process, requestG);
                Assert.Equal(0, process.ShouldEnableRequests);
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(2, process.ShouldEnableRequests);
                VerifySignatures(requestF);
                VerifySignatures(requestG);
            }
        }

        [Fact]
        public void MissingMetadata()
        {
            var sourceA =
@"class A
{
    static void F1() { }
    static void F2() { }
    static void F3() { }
    static void F4() { }
}";
            var sourceC =
@"class C
{
    static void F1() { }
    static void F2() { }
    static void F3() { }
    static void F4() { }
}";
            var moduleA = new Module(CreateCompilationWithMscorlib(sourceA).EmitToArray(), name: "A.dll");
            var moduleB = new Module(default(ImmutableArray<byte>), name: "B.dll");
            var moduleC = new Module(CreateCompilationWithMscorlib(sourceC).EmitToArray(), name: "C.dll");

            using (var process = new Process())
            {
                var resolver = new Resolver();
                var requestAll = new Request(null, MemberSignatureParser.Parse("F1"));
                var requestA = new Request("A.dll", MemberSignatureParser.Parse("F2"));
                var requestB = new Request("B.dll", MemberSignatureParser.Parse("F3"));
                var requestC = new Request("C.dll", MemberSignatureParser.Parse("F4"));

                // Request to all modules.
                resolver.EnableResolution(process, requestAll);
                Assert.Equal(0, moduleA.GetMetadataCount);
                Assert.Equal(0, moduleB.GetMetadataCount);
                Assert.Equal(0, moduleC.GetMetadataCount);

                // Load module A (available).
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, moduleA.GetMetadataCount);
                Assert.Equal(0, moduleB.GetMetadataCount);
                Assert.Equal(0, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Load module B (missing).
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(1, moduleA.GetMetadataCount);
                Assert.Equal(1, moduleB.GetMetadataCount);
                Assert.Equal(0, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Load module C (available).
                process.AddModule(moduleC);
                resolver.OnModuleLoad(process, moduleC);
                Assert.Equal(1, moduleA.GetMetadataCount);
                Assert.Equal(1, moduleB.GetMetadataCount);
                Assert.Equal(1, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module A (available).
                resolver.EnableResolution(process, requestA);
                Assert.Equal(2, moduleA.GetMetadataCount);
                Assert.Equal(1, moduleB.GetMetadataCount);
                Assert.Equal(1, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA, "A.F2()");
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module B (missing).
                resolver.EnableResolution(process, requestB);
                Assert.Equal(2, moduleA.GetMetadataCount);
                Assert.Equal(2, moduleB.GetMetadataCount);
                Assert.Equal(1, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA, "A.F2()");
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module C (available).
                resolver.EnableResolution(process, requestC);
                Assert.Equal(2, moduleA.GetMetadataCount);
                Assert.Equal(2, moduleB.GetMetadataCount);
                Assert.Equal(2, moduleC.GetMetadataCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA, "A.F2()");
                VerifySignatures(requestB);
                VerifySignatures(requestC, "C.F4()");
            }
        }

        [Fact]
        public void ModuleName()
        {
            var sourceA =
@"public struct A
{
    static void F() { }
}";
            var nameA = GetUniqueName();
            var compilationA = CreateCompilationWithMscorlib(sourceA, assemblyName: nameA);
            var imageA = compilationA.EmitToArray();
            var refA = AssemblyMetadata.CreateFromImage(imageA).GetReference();
            var sourceB =
@"class B
{
    static void F(A a) { }
    static void Main() { }
}";
            var nameB = GetUniqueName();
            var compilationB = CreateCompilationWithMscorlib(sourceB, assemblyName: nameB, options: TestOptions.DebugExe, references: new[] { refA });
            var imageB = compilationB.EmitToArray();
            using (var process = new Process(new Module(imageA, nameA + ".dll"), new Module(imageB, nameB + ".exe")))
            {
                var signature = MemberSignatureParser.Parse("F");
                var resolver = new Resolver();

                // No module name.
                var request = new Request("", signature);
                resolver.EnableResolution(process, request);
                VerifySignatures(request, "A.F()", "B.F(A)");

                // DLL module name, uppercase.
                request = new Request(nameA.ToUpper() + ".DLL", signature);
                resolver.EnableResolution(process, request);
                VerifySignatures(request, "A.F()");

                // EXE module name.
                request = new Request(nameB + ".EXE", signature);
                resolver.EnableResolution(process, request);
                VerifySignatures(request, "B.F(A)");

                // EXE module name, lowercase.
                request = new Request(nameB.ToLower() + ".exe", signature);
                resolver.EnableResolution(process, request);
                VerifySignatures(request, "B.F(A)");

                // EXE module name, no extension.
                request = new Request(nameB, signature);
                resolver.EnableResolution(process, request);
                VerifySignatures(request);
            }
        }

        [Fact]
        public void Arrays()
        {
            var source =
@"class A
{
}
class B
{
    static void F(A o) { }
    static void F(A[] o) { }
    static void F(A[,,] o) { }
    static void F(A[,,][] o) { }
    static void F(A[][,,] o) { }
    static void F(A[][][] o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "B.F(A)", "B.F(A[])", "B.F(A[,,])", "B.F(A[][,,])", "B.F(A[,,][])", "B.F(A[][][])");
                Resolve(process, resolver, "F(A)", "B.F(A)");
                Resolve(process, resolver, "F(A[])", "B.F(A[])");
                Resolve(process, resolver, "F(A[][])");
                Resolve(process, resolver, "F(A[,])");
                Resolve(process, resolver, "F(A[,,])", "B.F(A[,,])");
                Resolve(process, resolver, "F(A[,,][])", "B.F(A[,,][])");
                Resolve(process, resolver, "F(A[][,,])", "B.F(A[][,,])");
                Resolve(process, resolver, "F(A[,][,,])");
                Resolve(process, resolver, "F(A[][][])", "B.F(A[][][])");
                Resolve(process, resolver, "F(A[][][][])");
            }
        }

        [Fact]
        public void Pointers()
        {
            var source =
@"class C
{
    static unsafe void F(int*[] p) { }
    static unsafe void F(int** q) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeDebugDll);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F(System.Int32*[])", "C.F(System.Int32**)");
                Resolve(process, resolver, "F(int)");
                Resolve(process, resolver, "F(int*)");
                Resolve(process, resolver, "F(int[])");
                Resolve(process, resolver, "F(int*[])", "C.F(System.Int32*[])");
                Resolve(process, resolver, "F(int**)", "C.F(System.Int32**)");
                Resolve(process, resolver, "F(Int32**)", "C.F(System.Int32**)");
                Resolve(process, resolver, "F(C<int*>)");
                Resolve(process, resolver, "F(C<int>*)");
            }
        }

        [Fact]
        public void Nullable()
        {
            var source =
@"struct S
{
    void F(S? o) { }
    static void F(int?[] o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "S.F(System.Nullable<S>)", "S.F(System.Nullable<System.Int32>[])");
                Resolve(process, resolver, "F(S)");
                Resolve(process, resolver, "F(S?)", "S.F(System.Nullable<S>)");
                Resolve(process, resolver, "F(S??)");
                Resolve(process, resolver, "F(int?[])", "S.F(System.Nullable<System.Int32>[])");
            }
        }

        [Fact]
        public void ByRef()
        {
            var source =
@"class @ref { }
class @out { }
class C
{
    static void F(@out a, @ref b) { }
    static void F(ref @out a, out @ref b) { b = null; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F(out, ref)", "C.F(ref out, ref ref)");
                Assert.Null(MemberSignatureParser.Parse("F(ref, out)"));
                Assert.Null(MemberSignatureParser.Parse("F(ref ref, out out)"));
                Resolve(process, resolver, "F(@out, @ref)", "C.F(out, ref)");
                Resolve(process, resolver, "F(@out, out @ref)");
                Resolve(process, resolver, "F(ref @out, @ref)");
                Resolve(process, resolver, "F(ref @out, out @ref)", "C.F(ref out, ref ref)");
                Resolve(process, resolver, "F(out @out, ref @ref)", "C.F(ref out, ref ref)");
            }
        }

        [Fact]
        public void Properties()
        {
            var source =
@"abstract class A
{
    abstract internal object P { get; set; }
}
class B
{
    object P { get; set; }
}
class C
{
    static object P { get; }
}
class D
{
    int P { set { } }
}
interface I
{
    object P { get; set; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "P", "B.get_P()", "B.set_P(System.Object)", "C.get_P()", "D.set_P(System.Int32)");
                Resolve(process, resolver, "A.P");
                Resolve(process, resolver, "B.P", "B.get_P()", "B.set_P(System.Object)");
                Resolve(process, resolver, "B.P()");
                Resolve(process, resolver, "B.P(object)");
                Resolve(process, resolver, "B.P<T>");
                Resolve(process, resolver, "C.P", "C.get_P()");
                Resolve(process, resolver, "C.P()");
                Resolve(process, resolver, "D.P", "D.set_P(System.Int32)");
                Resolve(process, resolver, "D.P()");
                Resolve(process, resolver, "D.P(object)");
                Resolve(process, resolver, "get_P", "B.get_P()", "C.get_P()");
                Resolve(process, resolver, "set_P", "B.set_P(System.Object)", "D.set_P(System.Int32)");
                Resolve(process, resolver, "B.get_P()", "B.get_P()");
                Resolve(process, resolver, "B.set_P", "B.set_P(System.Object)");
            }
        }

        [Fact]
        public void Constructors()
        {
            var source =
@"class A
{
    static A() { }
    A() { }
    A(object o) { }
}
class B
{
}
class C<T>
{
}
class D
{
    static object A => null;
    static void B<T>() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "A", "A..ctor()", "A..ctor(System.Object)", "D.get_A()");
                Resolve(process, resolver, "A.A", "A..ctor()", "A..ctor(System.Object)");
                Resolve(process, resolver, "B", "B..ctor()");
                Resolve(process, resolver, "B<T>", "D.B<T>()");
                Resolve(process, resolver, "C", "C<T>..ctor()");
                Resolve(process, resolver, "C<T>");
                Resolve(process, resolver, "C<T>.C", "C<T>..ctor()");
                Assert.Null(MemberSignatureParser.Parse(".ctor"));
                Assert.Null(MemberSignatureParser.Parse("A..ctor"));
            }
        }

        [Fact]
        public void GenericMethods()
        {
            var source =
@"class A
{
    static void F() { }
    void F<T, U>() { }
    static void F<T>() { }
}
class A<T>
{
    static void F() { }
    void F<U, V>() { }
}
class B : A<int>
{
    static void F<T>() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                // Note, Dev14 matches type ignoring type parameters. For instance,
                // "A<T>.F" will bind to A.F() and A<T>.F(), and "A.F" will bind to A.F()
                // and A<T>.F. However, Dev14 does expect method type parameters to
                // match. Here, we expect both type and method parameters to match.
                var resolver = new Resolver();
                Resolve(process, resolver, "A.F", "A.F()");
                Resolve(process, resolver, "A.F<U>()", "A.F<T>()");
                Resolve(process, resolver, "A.F<T>", "A.F<T>()");
                Resolve(process, resolver, "A.F<T, T>", "A.F<T, U>()");
                Assert.Null(MemberSignatureParser.Parse("A.F<>()"));
                Assert.Null(MemberSignatureParser.Parse("A.F<,>()"));
                Resolve(process, resolver, "A<T>.F", "A<T>.F()");
                Resolve(process, resolver, "A<_>.F<_>");
                Resolve(process, resolver, "A<_>.F<_, _>", "A<T>.F<U, V>()");
                Resolve(process, resolver, "B.F()");
                Resolve(process, resolver, "B.F<T>()", "B.F<T>()");
                Resolve(process, resolver, "B.F<T, U>()");
            }
        }

        [Fact]
        public void Namespaces()
        {
            var source =
@"namespace N
{
    namespace M
    {
        class A<T>
        {
            static void F() { }
        }
    }
}
namespace N
{
    class B
    {
        static void F() { }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "N.B.F()", "N.M.A<T>.F()");
                Resolve(process, resolver, "A<T>.F", "N.M.A<T>.F()");
                Resolve(process, resolver, "N.A<T>.F");
                Resolve(process, resolver, "M.A<T>.F", "N.M.A<T>.F()");
                Resolve(process, resolver, "N.M.A<T>.F", "N.M.A<T>.F()");
                Resolve(process, resolver, "N.B.F", "N.B.F()");
            }
        }

        [Fact]
        public void NestedTypes()
        {
            var source =
@"class A
{
    class B
    {
        static void F() { }
    }
    class B<T>
    {
        static void F<U>() { }
    }
}
class B
{
    static void F() { }
}
namespace N
{
    class A<T>
    {
        class B
        {
            static void F<U>() { }
        }
        class B<U>
        {
            static void F() { }
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                // See comment in GenericMethods regarding differences with Dev14.
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "B.F()", "A.B.F()", "N.A<T>.B<U>.F()");
                Resolve(process, resolver, "F<T>", "A.B<T>.F<U>()", "N.A<T>.B.F<U>()");
                Resolve(process, resolver, "A.F");
                Resolve(process, resolver, "A.B.F", "A.B.F()");
                Resolve(process, resolver, "A.B.F<T>");
                Resolve(process, resolver, "A.B<T>.F<U>", "A.B<T>.F<U>()");
                Resolve(process, resolver, "A<T>.B.F<U>", "N.A<T>.B.F<U>()");
                Resolve(process, resolver, "A<T>.B<U>.F", "N.A<T>.B<U>.F()");
                Resolve(process, resolver, "B.F", "B.F()", "A.B.F()");
                Resolve(process, resolver, "B.F<T>", "N.A<T>.B.F<U>()");
                Resolve(process, resolver, "B<T>.F", "N.A<T>.B<U>.F()");
                Resolve(process, resolver, "B<T>.F<U>", "A.B<T>.F<U>()");
                Assert.Null(MemberSignatureParser.Parse("A+B.F"));
                Assert.Null(MemberSignatureParser.Parse("A.B`1.F<T>"));
            }
        }

        [Fact]
        public void NamespacesAndTypes()
        {
            var source =
@"namespace A.B
{
    class T { }
    class C
    {
        static void F(C c) { }
        static void F(A.B<T>.C c) { }
    }
}
namespace A
{
    class B<T>
    {
        internal class C
        {
            static void F(C c) { }
            static void F(A.B.C c) { }
        }
    }
}
class A<T>
{
    internal class B
    {
        internal class C
        {
            static void F(C c) { }
            static void F(A.B<T>.C c) { }
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "A.B.C.F(A.B.C)", "A.B.C.F(A.B<A.B.T>.C)", "A.B<T>.C.F(A.B<T>.C)", "A.B<T>.C.F(A.B.C)", "A<T>.B.C.F(A<T>.B.C)", "A<T>.B.C.F(A.B<T>.C)");
                Resolve(process, resolver, "F(C)", "A.B.C.F(A.B.C)", "A.B.C.F(A.B<A.B.T>.C)", "A.B<T>.C.F(A.B.C)");
                Resolve(process, resolver, "F(B.C)", "A.B.C.F(A.B.C)", "A.B<T>.C.F(A.B.C)");
                Resolve(process, resolver, "F(B<T>.C)", "A.B.C.F(A.B<A.B.T>.C)");
                Resolve(process, resolver, "A.B.C.F", "A.B.C.F(A.B.C)", "A.B.C.F(A.B<A.B.T>.C)");
                Resolve(process, resolver, "A<T>.B.C.F", "A<T>.B.C.F(A<T>.B.C)", "A<T>.B.C.F(A.B<T>.C)");
                Resolve(process, resolver, "A.B<T>.C.F", "A.B<T>.C.F(A.B<T>.C)", "A.B<T>.C.F(A.B.C)");
                Resolve(process, resolver, "B<T>.C.F(B<T>.C)", "A.B<T>.C.F(A.B<T>.C)");
            }
        }

        [Fact]
        public void NamespacesAndTypes_More()
        {
            var source =
@"namespace A1.B
{
    class C
    {
        static void F(C c) { }
    }
}
namespace A2
{
    class B
    {
        internal class C
        {
            static void F(C c) { }
            static void F(A1.B.C c) { }
        }
    }
}
class A3
{
    internal class B
    {
        internal class C
        {
            static void F(C c) { }
            static void F(A2.B.C c) { }
        }
    }
}
namespace B
{
    class C
    {
        static void F(C c) { }
        static void F(A3.B.C c) { }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F(C)", "B.C.F(B.C)", "B.C.F(A3.B.C)", "A1.B.C.F(A1.B.C)", "A2.B.C.F(A2.B.C)", "A2.B.C.F(A1.B.C)", "A3.B.C.F(A3.B.C)", "A3.B.C.F(A2.B.C)");
                Resolve(process, resolver, "B.C.F(B.C)", "B.C.F(B.C)", "B.C.F(A3.B.C)", "A1.B.C.F(A1.B.C)", "A2.B.C.F(A2.B.C)", "A2.B.C.F(A1.B.C)", "A3.B.C.F(A3.B.C)", "A3.B.C.F(A2.B.C)");
                Resolve(process, resolver, "B.C.F(A1.B.C)", "A1.B.C.F(A1.B.C)", "A2.B.C.F(A1.B.C)");
                Resolve(process, resolver, "B.C.F(A2.B.C)", "A2.B.C.F(A2.B.C)", "A3.B.C.F(A2.B.C)");
                Resolve(process, resolver, "B.C.F(A3.B.C)", "B.C.F(A3.B.C)", "A3.B.C.F(A3.B.C)");
            }
        }

        [Fact]
        public void TypeParameters()
        {
            var source =
@"class A
{
    class B<T>
    {
        static void F<U>(B<T> t) { }
        static void F<U>(B<U> u) { }
    }
}
class A<T>
{
    class B<U>
    {
        static void F<V>(T t) { }
        static void F<V>(A<U> u) { }
        static void F<V>(B<V> v) { }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F<T>", "A.B<T>.F<U>(A.B<T>)", "A.B<T>.F<U>(A.B<U>)", "A<T>.B<U>.F<V>(T)", "A<T>.B<U>.F<V>(A<U>)", "A<T>.B<U>.F<V>(A<T>.B<V>)");
                Resolve(process, resolver, "B<T>.F<U>", "A.B<T>.F<U>(A.B<T>)", "A.B<T>.F<U>(A.B<U>)", "A<T>.B<U>.F<V>(T)", "A<T>.B<U>.F<V>(A<U>)", "A<T>.B<U>.F<V>(A<T>.B<V>)");
                Resolve(process, resolver, "F<T>(B<T>)", "A.B<T>.F<U>(A.B<U>)");
                Resolve(process, resolver, "F<U>(B<T>)"); // No T in signature to bind to.
                Resolve(process, resolver, "F<T>(B<U>)"); // No U in signature to bind to.
                Resolve(process, resolver, "B<X>.F<Y>(B<X>)", "A.B<T>.F<U>(A.B<T>)");
                Resolve(process, resolver, "B<X>.F<Y>(B<Y>)", "A.B<T>.F<U>(A.B<U>)");
                Resolve(process, resolver, "B<U>.F<V>(T)"); // No T in signature to bind to.
                Resolve(process, resolver, "B<U>.F<V>(A<U>)", "A<T>.B<U>.F<V>(A<U>)");
                Resolve(process, resolver, "B<U>.F<V>(B<V>)", "A.B<T>.F<U>(A.B<U>)");
                Resolve(process, resolver, "B<V>.F<U>(B<U>)", "A.B<T>.F<U>(A.B<U>)");
                Resolve(process, resolver, "A<X>.B<Y>.F<Z>(X)", "A<T>.B<U>.F<V>(T)");
                Resolve(process, resolver, "A<X>.B<Y>.F<Z>(B<Z>)", "A<T>.B<U>.F<V>(A<T>.B<V>)");
            }
        }

        [Fact]
        public void TypeReferences()
        {
            var sourceA =
@"public class A<T>
{
    public class B<U>
    {
        static void F<V, W>() { }
    }
}
namespace N
{
    public class C<T>
    {
    }
}";
            var compilationA = CreateCompilationWithMscorlib(sourceA);
            var bytesA = compilationA.EmitToArray();
            var refA = AssemblyMetadata.CreateFromImage(bytesA).GetReference();
            var sourceB =
@"class D<T>
{
    static void F<U, V>(N.C<A<U>.B<V>[]> b) { }
}";
            var compilationB = CreateCompilationWithMscorlib(sourceB, references: new[] { refA });
            var bytesB = compilationB.EmitToArray();
            using (var process = new Process(new Module(bytesA), new Module(bytesB)))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F<T, U>", "A<T>.B<U>.F<V, W>()", "D<T>.F<U, V>(N.C<A<U>.B<V>[]>)");
                Resolve(process, resolver, "F<T, U>(C)"); // No type argument for C<>
                Resolve(process, resolver, "F<T, U>(C<T>)"); // Incorrect type argument for C<>
                Resolve(process, resolver, "F<T, U>(C<B<U>>)"); // No array qualifier
                Resolve(process, resolver, "F<T, U>(C<B<T>[]>)"); // Incorrect type argument for B<>
                Resolve(process, resolver, "F<T, U>(C<B<U>[]>)", "D<T>.F<U, V>(N.C<A<U>.B<V>[]>)");
                Resolve(process, resolver, "F<T, U>(N.C<B<U>[]>)", "D<T>.F<U, V>(N.C<A<U>.B<V>[]>)");
                Resolve(process, resolver, "D<X>.F<Y, Z>", "D<T>.F<U, V>(N.C<A<U>.B<V>[]>)");
                Resolve(process, resolver, "D<X>.F<Y, Z>(C<A<Y>[]>)"); // No nested type B
                Resolve(process, resolver, "D<X>.F<Y, Z>(C<A<Y>.B<Z>[]>)", "D<T>.F<U, V>(N.C<A<U>.B<V>[]>)");
                Resolve(process, resolver, "D<X>.F<Y, Z>(C<A<Y>.B<Y>[]>)"); // Incorrect type argument for B<>.
            }
        }

        [Fact]
        public void Keywords_MethodName()
        {
            var source =
@"namespace @namespace
{
    struct @struct
    {
        object @public => 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Assert.Null(MemberSignatureParser.Parse("public"));
                Assert.Null(MemberSignatureParser.Parse("namespace.@struct.@public"));
                Assert.Null(MemberSignatureParser.Parse("@namespace.struct.@public"));
                Assert.Null(MemberSignatureParser.Parse("@namespace.@struct.public"));
                Resolve(process, resolver, "@public", "namespace.struct.get_public()");
                Resolve(process, resolver, "@namespace.@struct.@public", "namespace.struct.get_public()");
            }
        }

        [Fact]
        public void Keywords_MethodTypeParameter()
        {
            var source =
@"class @class<@in>
{
    static void F<@out>(@in i, @out o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Assert.Null(MemberSignatureParser.Parse("F<out>"));
                Assert.Null(MemberSignatureParser.Parse("F<in>"));
                Assert.Null(MemberSignatureParser.Parse("class<@in>.F<@out>"));
                Assert.Null(MemberSignatureParser.Parse("@class<in>.F<@out>"));
                Assert.Null(MemberSignatureParser.Parse("@class<@in>.F<out>"));
                Resolve(process, resolver, "F<@out>", "class<in>.F<out>(in, out)");
                Resolve(process, resolver, "F<@in>", "class<in>.F<out>(in, out)");
                Resolve(process, resolver, "@class<@in>.F<@out>", "class<in>.F<out>(in, out)");
                Resolve(process, resolver, "@class<@this>.F<@base>", "class<in>.F<out>(in, out)");
                Resolve(process, resolver, "@class<T>.F<U>", "class<in>.F<out>(in, out)");
            }
        }

        [Fact]
        public void Keywords_ParameterName()
        {
            var source =
@"namespace @namespace
{
    struct @struct
    {
    }
}
class C
{
    static void F(@namespace.@struct s) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Assert.Null(MemberSignatureParser.Parse("F(struct)"));
                Assert.Null(MemberSignatureParser.Parse("F(namespace.@struct)"));
                Resolve(process, resolver, "F(@struct)", "C.F(namespace.struct)");
                Resolve(process, resolver, "F(@namespace.@struct)", "C.F(namespace.struct)");
            }
        }

        [Fact]
        public void Keywords_ParameterTypeArgument()
        {
            var source =
@"class @this
{
    internal class @base
    {
    }
}
class @class<T>
{
}
class C
{
    static void F(@class<@this.@base> c) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Assert.Null(MemberSignatureParser.Parse("F(@class<base>)"));
                Assert.Null(MemberSignatureParser.Parse("F(@class<this.@base>)"));
                Assert.Null(MemberSignatureParser.Parse("F(@class<@this.base>)"));
                Resolve(process, resolver, "F(@class<@base>)", "C.F(class<this.base>)");
                Resolve(process, resolver, "F(@class<@this.@base>)", "C.F(class<this.base>)");
            }
        }

        [Fact]
        public void EscapedNames()
        {
            var source =
@"class @object { }
class Object { }
class C
{
    static void F(@object o) { }
    static void F(Object o) { }
    static void F(object o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F(object)", "C.F(Object)", "C.F(System.Object)");
                Resolve(process, resolver, "F(object)", "C.F(System.Object)");
                Resolve(process, resolver, "F(Object)", "C.F(Object)", "C.F(System.Object)");
                Resolve(process, resolver, "F(System.Object)", "C.F(System.Object)");
                Resolve(process, resolver, "F(@object)", "C.F(object)");
                Resolve(process, resolver, "F(@Object)", "C.F(Object)", "C.F(System.Object)");
            }
        }

        [Fact]
        public void SpecialTypes()
        {
            var source =
@"class C<T1, T2, T3, T4>
{
}
class C
{
    static void F(bool a, char b, sbyte c, byte d) { }
    static void F(short a, ushort b, int c, uint d) { }
    static void F(C<uint, long, ulong, float> o) { }
    static void F(C<double, string, object, decimal> o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F(bool, char, sbyte, byte)", "C.F(System.Boolean, System.Char, System.SByte, System.Byte)");
                Resolve(process, resolver, "F(System.Int16, System.UInt16, System.Int32, System.UInt32)", "C.F(System.Int16, System.UInt16, System.Int32, System.UInt32)");
                Resolve(process, resolver, "F(C<UInt32, Int64, UInt64, Single>)", "C.F(C<System.UInt32, System.Int64, System.UInt64, System.Single>)");
                Resolve(process, resolver, "F(C<double, string, object, decimal>)", "C.F(C<System.Double, System.String, System.Object, System.Decimal>)");
                Resolve(process, resolver, "F(bool, char, sbyte)");
                Resolve(process, resolver, "F(C<double, string, object, decimal, bool>)");
            }
        }

        [Fact]
        public void SpecialTypes_More()
        {
            var source =
@"class C
{
    static void F(System.IntPtr p) { }
    static void F(System.UIntPtr p) { }
    static void F(System.TypedReference r) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F(object)");
                Resolve(process, resolver, "F(IntPtr)", "C.F(System.IntPtr)");
                Resolve(process, resolver, "F(UIntPtr)", "C.F(System.UIntPtr)");
                Resolve(process, resolver, "F(TypedReference)", "C.F(System.TypedReference)");
            }
        }

        // Binding to "dynamic" type refs is not supported.
        // This is consistent with Dev14.
        [Fact]
        public void Dynamic()
        {
            var source =
@"class C<T>
{
}
class C
{
    static void F(dynamic d) { }
    static void F(C<dynamic[]> d) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, references: new[] { CSharpRef, SystemCoreRef });
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F(System.Object)", "C.F(C<System.Object[]>)");
                Resolve(process, resolver, "F(object)", "C.F(System.Object)");
                Resolve(process, resolver, "F(C<object[]>)", "C.F(C<System.Object[]>)");
                Resolve(process, resolver, "F(dynamic)");
                Resolve(process, resolver, "F(C<dynamic[]>)");
            }
        }

        [Fact]
        public void Iterator()
        {
            var source =
@"class C
{
    static System.Collections.IEnumerable F() { yield break; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F()");
            }
        }

        [Fact]
        public void Async()
        {
            var source =
@"using System.Threading.Tasks;
class C
{
    static async Task F()
    {
        await Task.Delay(0);
    }
}";
            var compilation = CreateCompilationWithMscorlib46(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "C.F()");
            }
        }

        [Fact(Skip = "global:: not supported")]
        public void Global()
        {
            var source =
@"class C
{
    static void F(N.C o) { }
    static void F(global::C o) { }
}
namespace N
{
    class C
    {
        static void F(N.C o) { }
        static void F(global::C o) { }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "C.F(C)", "C.F(N.C)", "C.F(C)", "N.C.F(N.C)", "N.C.F(C)");
                Resolve(process, resolver, "C.F(N.C)", "C.F(N.C)", "N.C.F(N.C)");
                Resolve(process, resolver, "global::C.F(C)", "C.F(N.C)", "C.F(C)"); // Dev14 does not bind global::
                Resolve(process, resolver, "C.F(global::C)", "C.F(C)", "N.C.F(C)"); // Dev14 does not bind global::
            }
        }

        // Since MetadataDecoder does not load referenced
        // assemblies, the arity or a type reference is determined
        // by the type name: e.g.: "C`2". If the arity from the name is
        // different from the number of generic type arguments, the
        // method signature containing the type reference is ignored.
        [Fact]
        public void UnexpectedArity()
        {
            var sourceA =
@".class public A<T> { }";
            var sourceB =
@"class B
{
    static void F(object o) { }
    static void F(A<object> a) { }
}";
            ImmutableArray<byte> bytesA;
            ImmutableArray<byte> pdbA;
            EmitILToArray(sourceA, appendDefaultHeader: true, includePdb: false, assemblyBytes: out bytesA, pdbBytes: out pdbA);
            var refA = AssemblyMetadata.CreateFromImage(bytesA).GetReference();
            var compilationB = CreateCompilationWithMscorlib(sourceB, references: new[] { refA });
            var bytesB = compilationB.EmitToArray();
            using (var process = new Process(new Module(bytesB)))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "B.F(System.Object)", "B.F([notsupported])");
                Resolve(process, resolver, "F(A<object>)");
            }
        }

        /// <summary>
        /// Should not resolve to P/Invoke methods.
        /// </summary>
        [Fact]
        public void PInvoke()
        {
            var source =
@"using System.Runtime.InteropServices;
class A
{
    [DllImport(""extern.dll"")]
    public static extern int F();
}
class B
{
    public static int F() => 0;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = new Resolver();
                Resolve(process, resolver, "F", "B.F()");
            }
        }

        private static void Resolve(Process process, Resolver resolver, string str, params string[] expectedSignatures)
        {
            var signature = MemberSignatureParser.Parse(str);
            Assert.NotNull(signature);
            var request = new Request(null, signature);
            resolver.EnableResolution(process, request);
            VerifySignatures(request, expectedSignatures);
        }

        private static void VerifySignatures(Request request, params string[] expectedSignatures)
        {
            var actualSignatures = request.GetResolvedAddresses().Select(a => GetMethodSignature(a.Module, a.Token));
            AssertEx.Equal(expectedSignatures, actualSignatures);
        }

        private static string GetMethodSignature(Module module, int token)
        {
            var reader = module.GetMetadataInternal();
            return GetMethodSignature(reader, MetadataTokens.MethodDefinitionHandle(token));
        }

        private static string GetMethodSignature(MetadataReader reader, MethodDefinitionHandle handle)
        {
            var methodDef = reader.GetMethodDefinition(handle);
            var builder = new StringBuilder();
            var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
            var allTypeParameters = typeDef.GetGenericParameters();
            AppendTypeName(builder, reader, typeDef);
            builder.Append('.');
            builder.Append(reader.GetString(methodDef.Name));
            var methodTypeParameters = methodDef.GetGenericParameters();
            AppendTypeParameters(builder, DecodeTypeParameters(reader, offset: 0, typeParameters: methodTypeParameters));
            var decoder = new MetadataDecoder(
                reader,
                GetTypeParameterNames(reader, allTypeParameters),
                0,
                GetTypeParameterNames(reader, methodTypeParameters));
            try
            {
                AppendParameters(builder, decoder.DecodeParameters(methodDef));
            }
            catch (NotSupportedException)
            {
                builder.Append("([notsupported])");
            }
            return builder.ToString();
        }

        private static ImmutableArray<string> GetTypeParameterNames(MetadataReader reader, GenericParameterHandleCollection handles)
        {
            return ImmutableArray.CreateRange(handles.Select(h => reader.GetString(reader.GetGenericParameter(h).Name)));
        }

        private static void AppendTypeName(StringBuilder builder, MetadataReader reader, TypeDefinition typeDef)
        {
            var declaringTypeHandle = typeDef.GetDeclaringType();
            int declaringTypeArity;
            if (declaringTypeHandle.IsNil)
            {
                declaringTypeArity = 0;
                var namespaceName = reader.GetString(typeDef.Namespace);
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    builder.Append(namespaceName);
                    builder.Append('.');
                }
            }
            else
            {
                var declaringType = reader.GetTypeDefinition(declaringTypeHandle);
                declaringTypeArity = declaringType.GetGenericParameters().Count;
                AppendTypeName(builder, reader, declaringType);
                builder.Append('.');
            }
            var typeName = reader.GetString(typeDef.Name);
            int index = typeName.IndexOf('`');
            if (index >= 0)
            {
                typeName = typeName.Substring(0, index);
            }
            builder.Append(typeName);
            AppendTypeParameters(builder, DecodeTypeParameters(reader, declaringTypeArity, typeDef.GetGenericParameters()));
        }

        private static void AppendTypeParameters(StringBuilder builder, ImmutableArray<string> typeParameters)
        {
            if (typeParameters.Length > 0)
            {
                builder.Append('<');
                AppendCommaSeparatedList(builder, typeParameters, (b, t) => b.Append(t));
                builder.Append('>');
            }
        }

        private static void AppendParameters(StringBuilder builder, ImmutableArray<ParameterSignature> parameters)
        {
            builder.Append('(');
            AppendCommaSeparatedList(builder, parameters, AppendParameter);
            builder.Append(')');
        }

        private static void AppendParameter(StringBuilder builder, ParameterSignature signature)
        {
            if (signature.IsByRef)
            {
                builder.Append("ref ");
            }
            AppendType(builder, signature.Type);
        }

        private static void AppendType(StringBuilder builder, TypeSignature signature)
        {
            switch (signature.Kind)
            {
                case TypeSignatureKind.GenericType:
                    {
                        var genericName = (GenericTypeSignature)signature;
                        AppendType(builder, genericName.QualifiedName);
                        AppendTypeArguments(builder, genericName.TypeArguments);
                    }
                    break;
                case TypeSignatureKind.QualifiedType:
                    {
                        var qualifiedName = (QualifiedTypeSignature)signature;
                        var qualifier = qualifiedName.Qualifier;
                        if (qualifier != null)
                        {
                            AppendType(builder, qualifier);
                            builder.Append('.');
                        }
                        builder.Append(qualifiedName.Name);
                    }
                    break;
                case TypeSignatureKind.ArrayType:
                    {
                        var arrayType = (ArrayTypeSignature)signature;
                        AppendType(builder, arrayType.ElementType);
                        builder.Append('[');
                        builder.Append(',', arrayType.Rank - 1);
                        builder.Append(']');
                    }
                    break;
                case TypeSignatureKind.PointerType:
                    AppendType(builder, ((PointerTypeSignature)signature).PointedAtType);
                    builder.Append('*');
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private static void AppendTypeArguments(StringBuilder builder, ImmutableArray<TypeSignature> typeArguments)
        {
            if (typeArguments.Length > 0)
            {
                builder.Append('<');
                AppendCommaSeparatedList(builder, typeArguments, AppendType);
                builder.Append('>');
            }
        }

        private static void AppendCommaSeparatedList<T>(StringBuilder builder, ImmutableArray<T> items, Action<StringBuilder, T> appendItem)
        {
            bool any = false;
            foreach (var item in items)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                appendItem(builder, item);
                any = true;
            }
        }

        private static ImmutableArray<string> DecodeTypeParameters(MetadataReader reader, int offset, GenericParameterHandleCollection typeParameters)
        {
            int arity = typeParameters.Count - offset;
            Debug.Assert(arity >= 0);
            if (arity == 0)
            {
                return ImmutableArray<string>.Empty;
            }
            var builder = ImmutableArray.CreateBuilder<string>(arity);
            for (int i = 0; i < arity; i++)
            {
                var handle = typeParameters[offset + i];
                var typeParameter = reader.GetGenericParameter(handle);
                builder.Add(reader.GetString(typeParameter.Name));
            }
            return builder.ToImmutable();
        }
    }
}
