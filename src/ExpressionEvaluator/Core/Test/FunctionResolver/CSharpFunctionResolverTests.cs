// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public class CSharpFunctionResolverTests : FunctionResolverTestBase
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
            var compilation = CreateCompilation(source);
            var module = new Module(compilation.EmitToArray());
            using (var process = new Process())
            {
                var resolver = Resolver.CSharpResolver;
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
            var bytesA = CreateCompilation(sourceA).EmitToArray();
            var bytesB = CreateCompilation(sourceB).EmitToArray();
            var resolver = Resolver.CSharpResolver;

            // Two modules loaded before two global requests,
            // ... resolver enabled.
            var moduleA = new Module(bytesA, name: "A.dll");
            var moduleB = new Module(bytesB, name: "B.dll");
            using (var process = new Process(shouldEnable: true, modules: [moduleA, moduleB]))
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
            using (var process = new Process(shouldEnable: false, modules: [moduleA, moduleB]))
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
            using (var process = new Process(shouldEnable: true, modules: [moduleA, moduleB]))
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
            using (var process = new Process(shouldEnable: false, modules: [moduleA, moduleB]))
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

        /// <summary>
        /// Should only handle requests with expected language id or
        /// default language id or causality breakpoints.
        /// </summary>
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15119")]
        public void LanguageId()
        {
            var source =
@"class C
{
    static void F() { }
}";
            var bytes = CreateCompilation(source).EmitToArray();
            var resolver = Resolver.CSharpResolver;
            var unknownId = Guid.Parse("F02FB87B-64EC-486E-B039-D4A97F48858C");
            var csharpLanguageId = Guid.Parse("3f5162f8-07c6-11d3-9053-00c04fa302a1");
            var vbLanguageId = Guid.Parse("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
            var cppLanguageId = Guid.Parse("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");

            // Module loaded before requests.
            var module = new Module(bytes);
            using (var process = new Process(module))
            {
                var requestDefaultId = new Request(null, MemberSignatureParser.Parse("F"), Guid.Empty);
                var requestUnknown = new Request(null, MemberSignatureParser.Parse("F"), unknownId);
                var requestCausalityBreakpoint = new Request(null, MemberSignatureParser.Parse("F"), DkmLanguageId.CausalityBreakpoint);
                var requestMethodId = new Request(null, MemberSignatureParser.Parse("F"), DkmLanguageId.MethodId);
                var requestCSharp = new Request(null, MemberSignatureParser.Parse("F"), csharpLanguageId);
                var requestVB = new Request(null, MemberSignatureParser.Parse("F"), vbLanguageId);
                var requestCPP = new Request(null, MemberSignatureParser.Parse("F"), cppLanguageId);
                resolver.EnableResolution(process, requestDefaultId);
                VerifySignatures(requestDefaultId, "C.F()");
                resolver.EnableResolution(process, requestUnknown);
                VerifySignatures(requestUnknown);
                resolver.EnableResolution(process, requestCausalityBreakpoint);
                VerifySignatures(requestCausalityBreakpoint, "C.F()");
                resolver.EnableResolution(process, requestMethodId);
                VerifySignatures(requestMethodId);
                resolver.EnableResolution(process, requestCSharp);
                VerifySignatures(requestCSharp, "C.F()");
                resolver.EnableResolution(process, requestVB);
                VerifySignatures(requestVB);
                resolver.EnableResolution(process, requestCPP);
                VerifySignatures(requestCPP);
            }

            // Module loaded after requests.
            module = new Module(bytes);
            using (var process = new Process())
            {
                var requestDefaultId = new Request(null, MemberSignatureParser.Parse("F"), Guid.Empty);
                var requestUnknown = new Request(null, MemberSignatureParser.Parse("F"), unknownId);
                var requestCausalityBreakpoint = new Request(null, MemberSignatureParser.Parse("F"), DkmLanguageId.CausalityBreakpoint);
                var requestMethodId = new Request(null, MemberSignatureParser.Parse("F"), DkmLanguageId.MethodId);
                var requestCSharp = new Request(null, MemberSignatureParser.Parse("F"), csharpLanguageId);
                var requestVB = new Request(null, MemberSignatureParser.Parse("F"), vbLanguageId);
                var requestCPP = new Request(null, MemberSignatureParser.Parse("F"), cppLanguageId);
                resolver.EnableResolution(process, requestCPP);
                resolver.EnableResolution(process, requestVB);
                resolver.EnableResolution(process, requestCSharp);
                resolver.EnableResolution(process, requestMethodId);
                resolver.EnableResolution(process, requestCausalityBreakpoint);
                resolver.EnableResolution(process, requestUnknown);
                resolver.EnableResolution(process, requestDefaultId);
                process.AddModule(module);
                resolver.OnModuleLoad(process, module);
                VerifySignatures(requestDefaultId, "C.F()");
                VerifySignatures(requestUnknown);
                VerifySignatures(requestCausalityBreakpoint, "C.F()");
                VerifySignatures(requestMethodId);
                VerifySignatures(requestCSharp, "C.F()");
                VerifySignatures(requestVB);
                VerifySignatures(requestCPP);
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
            var moduleA = new Module(CreateCompilation(sourceA).EmitToArray(), name: "A.dll");
            var moduleB = new Module(default(ImmutableArray<byte>), name: "B.dll");
            var moduleC = new Module(CreateCompilation(sourceC).EmitToArray(), name: "C.dll");

            using (var process = new Process())
            {
                var resolver = Resolver.CSharpResolver;
                var requestAll = new Request(null, MemberSignatureParser.Parse("F1"));
                var requestA = new Request("A.dll", MemberSignatureParser.Parse("F2"));
                var requestB = new Request("B.dll", MemberSignatureParser.Parse("F3"));
                var requestC = new Request("C.dll", MemberSignatureParser.Parse("F4"));

                // Request to all modules.
                resolver.EnableResolution(process, requestAll);
                Assert.Equal(0, moduleA.MetadataAccessCount);
                Assert.Equal(0, moduleB.MetadataAccessCount);
                Assert.Equal(0, moduleC.MetadataAccessCount);

                // Load module A (available).
                process.AddModule(moduleA);
                resolver.OnModuleLoad(process, moduleA);
                Assert.Equal(1, moduleA.MetadataAccessCount);
                Assert.Equal(0, moduleB.MetadataAccessCount);
                Assert.Equal(0, moduleC.MetadataAccessCount);
                VerifySignatures(requestAll, "A.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Load module B (missing).
                process.AddModule(moduleB);
                resolver.OnModuleLoad(process, moduleB);
                Assert.Equal(1, moduleA.MetadataAccessCount);
                Assert.Equal(1, moduleB.MetadataAccessCount);
                Assert.Equal(0, moduleC.MetadataAccessCount);
                VerifySignatures(requestAll, "A.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Load module C (available).
                process.AddModule(moduleC);
                resolver.OnModuleLoad(process, moduleC);
                Assert.Equal(1, moduleA.MetadataAccessCount);
                Assert.Equal(1, moduleB.MetadataAccessCount);
                Assert.Equal(1, moduleC.MetadataAccessCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA);
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module A (available).
                resolver.EnableResolution(process, requestA);
                Assert.Equal(2, moduleA.MetadataAccessCount);
                Assert.Equal(1, moduleB.MetadataAccessCount);
                Assert.Equal(1, moduleC.MetadataAccessCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA, "A.F2()");
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module B (missing).
                resolver.EnableResolution(process, requestB);
                Assert.Equal(2, moduleA.MetadataAccessCount);
                Assert.Equal(2, moduleB.MetadataAccessCount);
                Assert.Equal(1, moduleC.MetadataAccessCount);
                VerifySignatures(requestAll, "A.F1()", "C.F1()");
                VerifySignatures(requestA, "A.F2()");
                VerifySignatures(requestB);
                VerifySignatures(requestC);

                // Request to module C (available).
                resolver.EnableResolution(process, requestC);
                Assert.Equal(2, moduleA.MetadataAccessCount);
                Assert.Equal(2, moduleB.MetadataAccessCount);
                Assert.Equal(2, moduleC.MetadataAccessCount);
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
            var compilationA = CreateCompilation(sourceA, assemblyName: nameA);
            var imageA = compilationA.EmitToArray();
            var refA = AssemblyMetadata.CreateFromImage(imageA).GetReference();
            var sourceB =
@"class B
{
    static void F(A a) { }
    static void Main() { }
}";
            var nameB = GetUniqueName();
            var compilationB = CreateCompilation(sourceB, assemblyName: nameB, options: TestOptions.DebugExe, references: new[] { refA });
            var imageB = compilationB.EmitToArray();
            using (var process = new Process(new Module(imageA, nameA + ".dll"), new Module(imageB, nameB + ".exe")))
            {
                var signature = MemberSignatureParser.Parse("F");
                var resolver = Resolver.CSharpResolver;

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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55475")]
        public void BadMetadata()
        {
            var source = "class A { static void F() {} }";
            var compilation = CreateCompilation(source);

            using var process = new Process(
                new Module(compilation.EmitToArray()),
                new Module(metadata: default), // emulates failure of the debugger to retrieve metadata
                new Module(metadata: TestResources.MetadataTests.Invalid.IncorrectCustomAssemblyTableSize_TooManyMethodSpecs.ToImmutableArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "F", "A.F()");
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
        public void Methods()
        {
            var source =
@"abstract class A
{
    abstract internal object F();
}
class B
{
    object F() => null;
}
class C
{
    static object F() => null;
}
interface I
{
    object F();
}";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "F", "B.F()", "C.F()");
            Resolve(process, resolver, "A.F");
            Resolve(process, resolver, "B.F", "B.F()");
            Resolve(process, resolver, "B.F()", "B.F()");
            Resolve(process, resolver, "B.F(object)");
            Resolve(process, resolver, "B.F<T>");
            Resolve(process, resolver, "C.F", "C.F()");
        }

        [Fact, WorkItem("https://github.com/MicrosoftDocs/visualstudio-docs/issues/4351")]
        [WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1303056")]
        public void Methods_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I { void F(); }
interface J { void F(); }

class C : I, J
{
    class I { void F() { } } 

    void global::I.F() { }
    void J.F() { }
}
";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "C.F", "C.global::I.F()", "C.J.F()");

            // resolves to the nested class members:
            Resolve(process, resolver, "C.I.F", "C.I.F()");

            // does not resolve
            Resolve(process, resolver, "C.J.F");
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
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
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

        [Fact, WorkItem("https://github.com/MicrosoftDocs/visualstudio-docs/issues/4351")]
        [WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1303056")]
        public void Properties_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I { int P { get; set; } }
interface J { int P { get; set; } }

class C : I, J
{
    class I { int P { get; set; } } 

    int global::I.P { get; set; }
    int J.P { get; set; }
}
";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "C.P", "C.global::I.get_P()", "C.global::I.set_P(System.Int32)", "C.J.get_P()", "C.J.set_P(System.Int32)");
            Resolve(process, resolver, "C.get_P", "C.global::I.get_P()", "C.J.get_P()");
            Resolve(process, resolver, "C.set_P", "C.global::I.set_P(System.Int32)", "C.J.set_P(System.Int32)");

            // resolves to the nested class members:
            Resolve(process, resolver, "C.I.P", "C.I.get_P()", "C.I.set_P(System.Int32)");
            Resolve(process, resolver, "C.I.get_P", "C.I.get_P()");
            Resolve(process, resolver, "C.I.set_P", "C.I.set_P(System.Int32)");

            // does not resolve
            Resolve(process, resolver, "C.J.P");
            Resolve(process, resolver, "C.J.get_P");
            Resolve(process, resolver, "C.J.set_P");
        }

        [Fact]
        public void Events()
        {
            var source = @"
abstract class A
{
    abstract internal event System.Action E;
}
class B
{
    event System.Action E;
}
class C
{
    static event System.Action E;
}
class D
{
    event System.Action E 
    {
        add {} 
        remove {} 
    }
}
interface I
{
    event System.Action E;
}";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "E", "B.add_E(System.Action)", "B.remove_E(System.Action)", "C.add_E(System.Action)", "C.remove_E(System.Action)", "D.add_E(System.Action)", "D.remove_E(System.Action)");
            Resolve(process, resolver, "A.E");
            Resolve(process, resolver, "B.E", "B.add_E(System.Action)", "B.remove_E(System.Action)");
            Resolve(process, resolver, "B.E()");
            Resolve(process, resolver, "B.E(System.Action)", "B.add_E(System.Action)", "B.remove_E(System.Action)");
            Resolve(process, resolver, "B.E<T>");
            Resolve(process, resolver, "C.E", "C.add_E(System.Action)", "C.remove_E(System.Action)");
            Resolve(process, resolver, "C.E(System.Action)", "C.add_E(System.Action)", "C.remove_E(System.Action)");
            Resolve(process, resolver, "D.E", "D.add_E(System.Action)", "D.remove_E(System.Action)");
            Resolve(process, resolver, "D.E()");
            Resolve(process, resolver, "D.E(System.Action)", "D.add_E(System.Action)", "D.remove_E(System.Action)");
            Resolve(process, resolver, "add_E", "B.add_E(System.Action)", "C.add_E(System.Action)", "D.add_E(System.Action)");
            Resolve(process, resolver, "remove_E", "B.remove_E(System.Action)", "C.remove_E(System.Action)", "D.remove_E(System.Action)");
            Resolve(process, resolver, "B.add_E(System.Action)", "B.add_E(System.Action)");
            Resolve(process, resolver, "B.remove_E", "B.remove_E(System.Action)");
        }

        [Fact, WorkItem("https://github.com/MicrosoftDocs/visualstudio-docs/issues/4351")]
        [WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1303056")]
        public void Events_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I { event System.Action E; }
interface J { event System.Action E; }

class C : I, J
{
    class I { event System.Action E { add {} remove {} } } 

    event System.Action global::I.E { add {} remove {} }
    event System.Action J.E { add {} remove {} }
}
";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "C.E", "C.global::I.add_E(System.Action)", "C.global::I.remove_E(System.Action)", "C.J.add_E(System.Action)", "C.J.remove_E(System.Action)");
            Resolve(process, resolver, "C.add_E", "C.global::I.add_E(System.Action)", "C.J.add_E(System.Action)");
            Resolve(process, resolver, "C.remove_E", "C.global::I.remove_E(System.Action)", "C.J.remove_E(System.Action)");

            // resolves to the nested class members:
            Resolve(process, resolver, "C.I.E", "C.I.add_E(System.Action)", "C.I.remove_E(System.Action)");
            Resolve(process, resolver, "C.I.add_E", "C.I.add_E(System.Action)");
            Resolve(process, resolver, "C.I.remove_E", "C.I.remove_E(System.Action)");

            // does not resolve
            Resolve(process, resolver, "C.J.E");
            Resolve(process, resolver, "C.J.add_E");
            Resolve(process, resolver, "C.J.remove_E");
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
}
class E
{
    static int x = 1;
}
";
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
                Resolve(process, resolver, "A", "A..cctor()", "A..ctor()", "A..ctor(System.Object)", "D.get_A()");
                Resolve(process, resolver, "A.A", "A..cctor()", "A..ctor()", "A..ctor(System.Object)");
                Resolve(process, resolver, "B", "B..ctor()");
                Resolve(process, resolver, "B<T>", "D.B<T>()");
                Resolve(process, resolver, "C", "C<T>..ctor()");
                Resolve(process, resolver, "C<T>");
                Resolve(process, resolver, "C<T>.C", "C<T>..ctor()");
                Resolve(process, resolver, "E", "E..ctor()", "E..cctor()");
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                // Note, Dev14 matches type ignoring type parameters. For instance,
                // "A<T>.F" will bind to A.F() and A<T>.F(), and "A.F" will bind to A.F()
                // and A<T>.F. However, Dev14 does expect method type parameters to
                // match. Here, we expect both type and method parameters to match.
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                // See comment in GenericMethods regarding differences with Dev14.
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
        public void DifferentCase_MethodsAndProperties()
        {
            var source =
@"class A
{
    static void method() { }
    static void Method(object o) { }
    object property => null;
}
class B
{
    static void Method() { }
    object Property { get; set; }
}";
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
                Resolve(process, resolver, "method", "A.method()");
                Resolve(process, resolver, "Method", "A.Method(System.Object)", "B.Method()");
                Resolve(process, resolver, "property", "A.get_property()");
                Resolve(process, resolver, "Property", "B.get_Property()", "B.set_Property(System.Object)");
                Resolve(process, resolver, "PROPERTY");
                Resolve(process, resolver, "get_property", "A.get_property()");
                Resolve(process, resolver, "GET_PROPERTY");
            }
        }

        [Fact]
        public void DifferentCase_NamespacesAndTypes()
        {
            var source =
@"namespace one.two
{
    class THREE
    {
        static void Method(THREE t) { }
    }
}
namespace One.Two
{
    class Three
    {
        static void Method(Three t) { }
    }
}";
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
                Resolve(process, resolver, "Method", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "Three.Method", "One.Two.Three.Method(One.Two.Three)");
                Resolve(process, resolver, "three.Method");
                Resolve(process, resolver, "Method(three)");
                Resolve(process, resolver, "THREE.Method(THREE)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "One.Two.Three.Method", "One.Two.Three.Method(One.Two.Three)");
                Resolve(process, resolver, "ONE.TWO.THREE.Method");
                Resolve(process, resolver, "Method(One.Two.Three)", "One.Two.Three.Method(One.Two.Three)");
                Resolve(process, resolver, "Method(one.two.THREE)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "Method(one.two.Three)");
                Resolve(process, resolver, "THREE", "one.two.THREE..ctor()");
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
            var compilationA = CreateCompilation(sourceA);
            var bytesA = compilationA.EmitToArray();
            var refA = AssemblyMetadata.CreateFromImage(bytesA).GetReference();
            var sourceB =
@"class D<T>
{
    static void F<U, V>(N.C<A<U>.B<V>[]> b) { }
}";
            var compilationB = CreateCompilation(sourceB, references: new[] { refA });
            var bytesB = compilationB.EmitToArray();
            using (var process = new Process(new Module(bytesA), new Module(bytesB)))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source, references: new[] { CSharpRef });
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
                var resolver = Resolver.CSharpResolver;
                Resolve(process, resolver, "F", "C.F()");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55242")]
        public void LocalFunctions()
        {
            var source = @"
void F()
{
    void G()
    {
    }
}

class F
{
    void G()
    {
        void G()
        {
        }
    }
}
";
            var compilation = CreateCompilation(source);
            using var process = new Process(new Module(compilation.EmitToArray()));

            var resolver = Resolver.CSharpResolver;
            Resolve(process, resolver, "G", "Program.<<Main>$>g__G|0_1()", "F.G()", "F.<G>g__G|0_0()");
            Resolve(process, resolver, "Program.G", "Program.<<Main>$>g__G|0_1()");
            Resolve(process, resolver, "F.G", "F.G()", "F.<G>g__G|0_0()");
            Resolve(process, resolver, "F.G.G");
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilationB = CreateCompilation(sourceB, references: new[] { refA });
            var bytesB = compilationB.EmitToArray();
            using (var process = new Process(new Module(bytesB)))
            {
                var resolver = Resolver.CSharpResolver;
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.CSharpResolver;
                Resolve(process, resolver, "F", "B.F()");
            }
        }

        private static void Resolve(Process process, Resolver resolver, string str, params string[] expectedSignatures)
        {
            var signature = MemberSignatureParser.Parse(str);
            Assert.NotNull(signature);
            Resolve(process, resolver, signature, expectedSignatures);
        }
    }
}
