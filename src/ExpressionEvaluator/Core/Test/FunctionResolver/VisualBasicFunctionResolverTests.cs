// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    // Binding is shared between C# and VB, so these tests
    // only cover the differences between the languages.
    public class VisualBasicFunctionResolverTests : FunctionResolverTestBase
    {
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
            var resolver = Resolver.VisualBasicResolver;
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
                VerifySignatures(requestCSharp);
                resolver.EnableResolution(process, requestVB);
                VerifySignatures(requestVB, "C.F()");
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
                VerifySignatures(requestCSharp);
                VerifySignatures(requestVB, "C.F()");
                VerifySignatures(requestCPP);
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
            var compilation = CreateCompilation(source);
            using (var process = new Process(new Module(compilation.EmitToArray())))
            {
                var resolver = Resolver.VisualBasicResolver;
                Resolve(process, resolver, "P", "B.get_P()", "B.set_P(System.Object)", "C.get_P()", "D.set_P(System.Int32)");
                Resolve(process, resolver, "A.P");
                Resolve(process, resolver, "B.P", "B.get_P()", "B.set_P(System.Object)");
                Resolve(process, resolver, "B.P()");
                Resolve(process, resolver, "B.P(Object)");
                Resolve(process, resolver, "B.P(Of T)");
                Resolve(process, resolver, "C.P", "C.get_P()");
                Resolve(process, resolver, "C.P()");
                Resolve(process, resolver, "D.P", "D.set_P(System.Int32)");
                Resolve(process, resolver, "D.P()");
                Resolve(process, resolver, "D.P(Object)");
                Resolve(process, resolver, "get_P", "B.get_P()", "C.get_P()");
                Resolve(process, resolver, "set_P", "B.set_P(System.Object)", "D.set_P(System.Int32)");
                Resolve(process, resolver, "B.get_P()", "B.get_P()");
                Resolve(process, resolver, "B.set_P", "B.set_P(System.Object)");
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
                var resolver = Resolver.VisualBasicResolver;
                Resolve(process, resolver, "method", "A.method()", "A.Method(System.Object)", "B.Method()");
                Resolve(process, resolver, "Method", "A.method()", "A.Method(System.Object)", "B.Method()");
                Resolve(process, resolver, "[property]", "A.get_property()", "B.get_Property()", "B.set_Property(System.Object)");
                Resolve(process, resolver, "[Property]", "A.get_property()", "B.get_Property()", "B.set_Property(System.Object)");
                Resolve(process, resolver, "[PROPERTY]", "A.get_property()", "B.get_Property()", "B.set_Property(System.Object)");
                Resolve(process, resolver, "GET_PROPERTY", "A.get_property()", "B.get_Property()");
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
                var resolver = Resolver.VisualBasicResolver;
                Resolve(process, resolver, "Method", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "Three.Method", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "three.Method(three)", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "THREE.Method(THREE)", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "ONE.TWO.THREE.Method", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "one.two.three.Method(one.two.three)", "One.Two.Three.Method(One.Two.Three)", "one.two.THREE.Method(one.two.THREE)");
                Resolve(process, resolver, "THREE", "One.Two.Three..ctor()", "one.two.THREE..ctor()");
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
