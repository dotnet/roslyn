// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class InterceptorsTests : CSharpTestBase
{
    private static (string, string) s_attributesSource = ("""
        namespace System.Runtime.CompilerServices;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class InterceptableAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class InterceptsLocationAttribute : Attribute
        {
            public InterceptsLocationAttribute(string filePath, int line, int column)
            {
            }
        }
        """, "attributes.cs");

    [Fact]
    public void IsInterceptable()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod() { Console.Write("interceptable"); }
                
                public static void NotInterceptable() { Console.Write("not interceptable"); }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, symbolValidator: verify, sourceSymbolValidator: verify);
        verifier.VerifyDiagnostics();

        void verify(ModuleSymbol module)
        {
            var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.InterceptableMethod");
            Assert.True(method.IsInterceptable);

            method = module.GlobalNamespace.GetMember<MethodSymbol>("C.NotInterceptable");
            Assert.False(method.IsInterceptable);
        }
    }

    [Fact]
    public void StaticInterceptable_StaticInterceptor_NoParameters()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod() { Console.Write("interceptable"); }

                public static void Main()
                {
                    InterceptableMethod();
                }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 10, 8)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor 1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableExtensionMethod_InterceptorExtensionMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {
                [Interceptable]
                public static I1 InterceptableMethod(this I1 i1, string param) { Console.Write("interceptable " + param); return i1; }

                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 10)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableExtensionMethod_InterceptorExtensionMethod1()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {
                [Interceptable]
                public static I1 InterceptableMethod(this I1 i1, Delegate param) { Console.Write("interceptable " + param); return i1; }

                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 10)]
                public static I1 InterceptorProgram1410(this I1 i1, Delegate param) { Console.Write("interceptor " + param); return i1; }

                
                [InterceptsLocation("Program.cs", 14, 10)] // prototype only exact location
                public static I1 InterceptorBind1(this I1 i1, Concrete c)
                { 
                    Log("starting thing");
                    i1.InterceptableMethod();
                    Log("ending thing");

                    coll.Select().Where().ToList(); // no-ops, pass things through. permit different return types?
                }
            }
            """;
        // look for Castle.DynamicProxy, AOP frameworks that work at runtime
        // IInterceptor
        // Configuration binding for ASP.NET
        // Regex.IsMatch("abc"), multiple calls with same string
        // System.Text.Json has AOP limitations. Intercept call to [de]serialize
        // Dependency injection. need internals for libraries being referenced.

        // Bind<T> interceptable
        // Bind<Concrete> interceptor

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableInstanceMethod_InterceptorExtensionMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1
            {
                [Interceptable]
                public I1 InterceptableMethod(string param) { Console.Write("interceptable " + param); return this; }
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 10)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableExtensionMethod_InterceptorExtensionMethod_Sequence()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {
                [Interceptable]
                public static I1 InterceptableMethod(this I1 i1, string param) { Console.Write("interceptable " + param); return i1; }

                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site")
                        .InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 10)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call siteinterceptable call site");
        verifier.VerifyDiagnostics();
    }
}