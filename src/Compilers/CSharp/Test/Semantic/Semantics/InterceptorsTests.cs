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
    private static readonly (string, string) s_attributesSource = ("""
        namespace System.Runtime.CompilerServices;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class InterceptableAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
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
            Assert.Equal(MethodKind.Ordinary, method.MethodKind);

            method = module.GlobalNamespace.GetMember<MethodSymbol>("C.NotInterceptable");
            Assert.False(method.IsInterceptable);
            Assert.Equal(MethodKind.Ordinary, method.MethodKind);
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
                [InterceptsLocation("Program.cs", 11, 9)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor 1");
        verifier.VerifyDiagnostics();
    }

    [Fact(Skip = "PROTOTYPE(ic): produce an error here")]
    public void Accessibility_01()
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
                [InterceptsLocation("Program.cs", 11, 9)]
                private static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor 1", verify: Verification.Fails);
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void Accessibility_02()
    {
        var source1 = """
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
            """;

        var source2 = """
            using System.Runtime.CompilerServices;
            using System;

            file class D
            {
                [InterceptsLocation("Program.cs", 11, 9)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;

        // PROTOTYPE(ic): Consider if an error should be reported here. It's not possible to directly use 'Interceptor1' at the location of the intercepted call.
        var verifier = CompileAndVerify(new[] { (source1, "Program.cs"), (source2, "Other.cs"), s_attributesSource }, expectedOutput: "interceptor 1");
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
                [InterceptsLocation("Program.cs", 15, 11)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    // PROTOTYPE(ic): test a case where the original method has type parameter constraints.
    // PROTOTYPE(ic): for now we will just completely disallow type parameters in the interceptor.
    // PROTOTYPE(ic): test where interceptable is a constructed method or a retargeting method.
    // PROTOTYPE(ic): test where there are differences between 'scoped' modifiers and '[UnscopedRef]' attributes

    // PROTOTYPE(ic): Possible test cases:
    //
    // * Intercept instance method with instance method in same class, base class, derived class
    // * Intercept with extern method
    // * Intercept with abstract or interface method
    // * Intercept an abstract or interface method
    // * Intercept a virtual or overridden method
    // * Intercept a non-extension call to a static method with a static method when one or both are extension methods
    // * Intercept a struct instance method with an extension method with by-value / by-ref this parameter
    // * An explicit interface implementation marked as interceptable
    // * Intercept a generic method call when the type parameters are / are not substituted

    [Fact]
    public void InterceptableInstanceMethod_InterceptorExtensionMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public C InterceptableMethod(string param) { Console.Write("interceptable " + param); return this; }
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
                [InterceptsLocation("Program.cs", 15, 11)]
                public static C Interceptor1(this C i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void QualifiedNameAtCallSite()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static C InterceptableMethod(C c, string param) { Console.Write("interceptable " + param); return c; }
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    C.InterceptableMethod(c, "call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static C Interceptor1(C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableStaticMethod_InterceptorExtensionMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static C InterceptableMethod(C c, string param) { Console.Write("interceptable " + param); return c; }
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    C.InterceptableMethod(c, "call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableStaticMethod_InterceptorInstanceMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod("call site");
                }
            }
            
            class C
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                [InterceptsLocation("Program.cs", 8, 11)]
                public void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS27012: Interceptor must not have a 'this' parameter because 'C.InterceptableMethod(string)' does not have a 'this' parameter.
            //     [InterceptsLocation("Program.cs", 8, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorMustNotHaveThisParameter, @"InterceptsLocation(""Program.cs"", 8, 11)").WithArguments("C.InterceptableMethod(string)").WithLocation(17, 6));
    }

    // PROTOTYPE(ic): test intercepting an extension method with a non-extension method. Perhaps should be an error for simplicity even if calling in non-reduced form.

    // PROTOTYPE(ic): test when parameter names differ between interceptable and interceptor.
    [Fact]
    public void ArgumentLabels()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public void InterceptableMethod(string s1, string s2) { Console.Write(s1 + s2); }
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod(s2: "World", s1: "Hello ");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(this C c, string s1, string s2) { Console.Write("interceptor " + s1 + s2); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor Hello World");
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
                [InterceptsLocation("Program.cs", 15, 11)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call siteinterceptable call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableFromMetadata()
    {
        var source1 = """
            using System.Runtime.CompilerServices;
            using System;

            public class C
            {
                [Interceptable]
                public C InterceptableMethod(string param) { Console.Write("interceptable " + param); return this; }
            }
            """;

        var source2 = """
            using System.Runtime.CompilerServices;
            using System;

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
                [InterceptsLocation("Program.cs", 9, 11)]
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;

        var comp1 = CreateCompilation(new[] { (source1, "File1.cs"), s_attributesSource });
        comp1.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify((source2, "Program.cs"), references: new[] { comp1.ToMetadataReference() }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void LocalFunctionInterceptor()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                public static void Main()
                {
                    InterceptableMethod("call site");

                    [InterceptsLocation("Program.cs", 13, 8)]
                    static void Interceptor1(string param) { Console.Write("interceptor " + param); }
                }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyDiagnostics(
            // Program.cs(16,10): error CS27009: An interceptor method must be an ordinary member method.
            //         [InterceptsLocation("Program.cs", 13, 8)]
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(16, 10),
            // Program.cs(17,21): warning CS8321: The local function 'Interceptor1' is declared but never used
            //         static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Interceptor1").WithArguments("Interceptor1").WithLocation(17, 21)
            );
    }

    // PROTOTYPE(ic): duplicates
    // PROTOTYPE(ic): intercept with instance method
    // PROTOTYPE(ic): intercept with instance base method

    [Fact]
    public void LocalFunctionInterceptable()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {
                public static void Main()
                {
                    InterceptableMethod("call site");

                    [Interceptable]
                    static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
                }

                [InterceptsLocation("Program.cs", 11, 9)]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyDiagnostics(
            // Program.cs(13,10): error CS27008: An interceptable method must be an ordinary member method.
            //         [Interceptable]
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "Interceptable").WithLocation(13, 10));
    }

    [Fact]
    public void CallNotInterceptable()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public C InterceptableMethod(string param) { Console.Write("interceptable " + param); return this; }
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
                [InterceptsLocation("Program.cs", 14, 11)]
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics(
            // Program.cs(20,6): warning CS27000: Cannot intercept a call to 'C.InterceptableMethod(string)' because it is not marked with 'System.Runtime.CompilerServices.InterceptableAttribute'.
            //     [InterceptsLocation("Program.cs", 14, 11)]
            Diagnostic(ErrorCode.WRN_CallNotInterceptable, @"InterceptsLocation(""Program.cs"", 14, 11)").WithArguments("C.InterceptableMethod(string)").WithLocation(20, 6)
            );
    }

    [Fact]
    public void InterceptorCannotBeGeneric_01()
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
                [InterceptsLocation("Program.cs", 16, 11)]
                public static I1 Interceptor1<T>(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(22,6): error CS27001: Method 'D.Interceptor1<T>(I1, string)' cannot be used as an interceptor because it or its containing type has type parameters.
                //     [InterceptsLocation("Program.cs", 16, 11)]
                Diagnostic(ErrorCode.ERR_InterceptorCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 16, 11)").WithArguments("D.Interceptor1<T>(I1, string)").WithLocation(22, 6));
    }

    [Fact]
    public void InterceptorCannotBeGeneric_02()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod("call site");
                }
            }

            static class D<T>
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27001: Method 'D<T>.Interceptor1(string)' cannot be used as an interceptor because it or its containing type has type parameters.
            //     [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("D<T>.Interceptor1(string)").WithLocation(21, 6));
    }

    [Fact]
    public void InterceptorCannotBeGeneric_03()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod("call site");
                }
            }

            static class Outer<T>
            {
                static class D
                {
                    [InterceptsLocation("Program.cs", 15, 11)]
                    public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
                }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(23,10): error CS27001: Method 'Outer<T>.D.Interceptor1(string)' cannot be used as an interceptor because it or its containing type has type parameters.
            //         [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("Outer<T>.D.Interceptor1(string)").WithLocation(23, 10)
            );
    }

    [Fact]
    public void InterceptableGeneric_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod<T1, T2>(T1 t) { Console.Write("0"); }
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod<string>("1");
                    C.InterceptableMethod("2");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)]
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(string s) { Console.Write(s); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_02()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod<T>(T t) { Console.Write("0"); }
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod<string>("1");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 30)]
                [InterceptsLocation("Program.cs", 14, 31)]
                [InterceptsLocation("Program.cs", 14, 37)]
                public static void Interceptor1(string s) { Console.Write(s); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS27004: The provided line and character number does not refer to an interceptable method name, but rather to token '<'.
            //     [InterceptsLocation("Program.cs", 14, 30)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 14, 30)").WithArguments("<").WithLocation(20, 6),
            // Program.cs(21,6): error CS27004: The provided line and character number does not refer to an interceptable method name, but rather to token 'string'.
            //     [InterceptsLocation("Program.cs", 14, 31)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 14, 31)").WithArguments("string").WithLocation(21, 6),
            // Program.cs(22,6): error CS27004: The provided line and character number does not refer to an interceptable method name, but rather to token '>'.
            //     [InterceptsLocation("Program.cs", 14, 37)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 14, 37)").WithArguments(">").WithLocation(22, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadAttributeArguments_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            static class D
            {
                [InterceptsLocation("Program.cs", 1, "10")]
                [InterceptsLocation("Program.cs", 1, 1, 9999)]
                [InterceptsLocation("Program.cs", ERROR, 1)]
                [InterceptsLocation()]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(6,42): error CS1503: Argument 3: cannot convert from 'string' to 'int'
            //     [InterceptsLocation("Program.cs", 1, "10")]
            Diagnostic(ErrorCode.ERR_BadArgType, @"""10""").WithArguments("3", "string", "int").WithLocation(6, 42),
            // Program.cs(7,6): error CS1729: 'InterceptsLocationAttribute' does not contain a constructor that takes 4 arguments
            //     [InterceptsLocation("Program.cs", 1, 1, 9999)]
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"InterceptsLocation(""Program.cs"", 1, 1, 9999)").WithArguments("System.Runtime.CompilerServices.InterceptsLocationAttribute", "4").WithLocation(7, 6),
            // Program.cs(8,39): error CS0103: The name 'ERROR' does not exist in the current context
            //     [InterceptsLocation("Program.cs", ERROR, 1)]
            Diagnostic(ErrorCode.ERR_NameNotInContext, "ERROR").WithArguments("ERROR").WithLocation(8, 39),
            // Program.cs(9,6): error CS7036: There is no argument given that corresponds to the required parameter 'filePath' of 'InterceptsLocationAttribute.InterceptsLocationAttribute(string, int, int)'
            //     [InterceptsLocation()]
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "InterceptsLocation()").WithArguments("filePath", "System.Runtime.CompilerServices.InterceptsLocationAttribute.InterceptsLocationAttribute(string, int, int)").WithLocation(9, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPath_01()
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
                [InterceptsLocation("BAD", 15, 11)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27002: Cannot intercept: compilation does not contain a file with path 'BAD'.
            //     [InterceptsLocation("BAD", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilation, @"InterceptsLocation(""BAD"", 15, 11)").WithArguments("BAD").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPath_02()
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
                [InterceptsLocation("projects/Program.cs", 15, 11)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "/Users/me/projects/Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // /Users/me/projects/Program.cs(21,6): error CS27003: Cannot intercept: compilation does not contain a file with path 'projects/Program.cs'. Did you mean to use path '/Users/me/projects/Program.cs'?
                //     [InterceptsLocation("projects/Program.cs", 15, 11)]
                Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"InterceptsLocation(""projects/Program.cs"", 15, 11)").WithArguments("projects/Program.cs", "/Users/me/projects/Program.cs").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_01()
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
                [InterceptsLocation("Program.cs", 100, 1)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(21,6): error CS27005: The given file has '23' lines, which is fewer than the provided line number '100'.
                //     [InterceptsLocation("Program.cs", 100, 1)]
                Diagnostic(ErrorCode.ERR_InterceptorLineOutOfRange, @"InterceptsLocation(""Program.cs"", 100, 1)").WithArguments("23", "100").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_02()
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
                [InterceptsLocation("Program.cs", 16, 1000)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27006: The given line is '5' characters long, which is fewer than the provided character number '1000'.
            //     [InterceptsLocation("Program.cs", 15, 1000)]
            Diagnostic(ErrorCode.ERR_InterceptorCharacterOutOfRange, @"InterceptsLocation(""Program.cs"", 16, 1000)").WithArguments("5", "1000").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_03()
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
                [InterceptsLocation("Program.cs", 15, 9)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(21,6): error CS27004: The provided line and character number does not refer to an interceptable method, but rather to token 'c'.
                //     [InterceptsLocation("Program.cs", 15, 9)]
                Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 15, 9)").WithArguments("c").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_04()
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
                [InterceptsLocation("Program.cs", 15, 13)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27010: The provided character number does not refer to the start of method name token 'InterceptableMethod'. Consider using character number '11' instead.
            //     [InterceptsLocation("Program.cs", 15, 13)]
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 15, 13)").WithArguments("InterceptableMethod", "11").WithLocation(21, 6)
        );
    }

    [Fact]
    public void InterceptsLocationBadPosition_05()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C { }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.
                        InterceptableMethod("call site");

                    c.InterceptableMethod    ("call site");
                }

                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 12, 11)] // intercept spaces before 'InterceptableMethod' token
                [InterceptsLocation("Program.cs", 14, 33)] // intercept spaces after 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS27010: The provided character number does not refer to the start of method name token 'InterceptableMethod'. Consider using character number '13' instead.
            //     [InterceptsLocation("Program.cs", 12, 11)] // intercept spaces before 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 12, 11)").WithArguments("InterceptableMethod", "13").WithLocation(20, 6),
            // Program.cs(21,6): error CS27010: The provided character number does not refer to the start of method name token 'InterceptableMethod'. Consider using character number '11' instead.
            //     [InterceptsLocation("Program.cs", 14, 33)] // intercept spaces after 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 14, 33)").WithArguments("InterceptableMethod", "11").WithLocation(21, 6)
        );
    }

    [Fact]
    public void SignatureMismatch_01()
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
                [InterceptsLocation("Program.cs", 15, 11)]
                public static I1 Interceptor1(this I1 i1, int param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(15,11): error CS27007: Cannot intercept method 'Program.InterceptableMethod(I1, string)' with interceptor 'D.Interceptor1(I1, int)' because the signatures do not match.
                //         c.InterceptableMethod("call site");
                Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, "InterceptableMethod").WithArguments("Program.InterceptableMethod(I1, string)", "D.Interceptor1(I1, int)").WithLocation(15, 11)
            );
    }

    [Fact]
    public void SignatureMismatch_02()
    {
        // Instance method receiver type differs from interceptor 'this' parameter type.
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
                [InterceptsLocation("Program.cs", 16, 11)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS27011: Interceptor must have a 'this' parameter matching parameter 'C this' on 'C.InterceptableMethod(string)'.
            //     [InterceptsLocation("Program.cs", 16, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 16, 11)").WithArguments("C this", "C.InterceptableMethod(string)").WithLocation(22, 6)
            );
    }

    // PROTOTYPE(ic): test interceptable explicit interface implementation (should error).
    // PROTOTYPE(ic): test interceptor with 'ref this' to match a struct interceptable method.

    [Fact]
    public void SignatureMismatch_03()
    {
        // Instance method 'this' parameter ref kind differs from interceptor 'this' parameter ref kind.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            struct S
            {
                [Interceptable]
                public void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
            }

            static class Program
            {
                public static void Main()
                {
                    var s = new S();
                    s.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(this S s, string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27011: Interceptor must have a 'this' parameter matching parameter 'ref S this' on 'S.InterceptableMethod(string)'.
            //     [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("ref S this", "S.InterceptableMethod(string)").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterpolatedStringHandler_01()
    {
        // Verify that interpolated string-related attributes on an intercepted call use the attributes from the interceptable method.
        var code = """
using System;
using System.Runtime.CompilerServices;

var s = new S1();
s.M($"");

public struct S1
{
    public S1() { }
    public int Field = 1;

    [Interceptable]
    public void M([InterpolatedStringHandlerArgument("")] CustomHandler c)
    {
        Console.Write(0);
    }
}

public static class S1Ext
{
    [InterceptsLocation("Program.cs", 5, 3)]
    public static void M1(ref this S1 s1, CustomHandler c)
    {
        Console.Write(2);
    }
}

partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, S1 s)
    {
        Console.Write(1);
    }
}
""";
        var verifier = CompileAndVerify(
            new[]
            {
                (code, "Program.cs"),
                (InterpolatedStringHandlerArgumentAttribute, "a.cs"),
                (GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), "b.cs"),
                s_attributesSource
            },
            expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterpolatedStringHandler_02()
    {
        // Verify that interpolated string-related attributes are ignored on an interceptor in an intercepted call.
        var code = """
using System;
using System.Runtime.CompilerServices;

var s = new S1();
s.M($"");

public struct S1
{
    public S1() { }
    public int Field = 1;

    [Interceptable]
    public void M(CustomHandler c)
    {
        Console.Write(0);
    }
}

public static class S1Ext
{
    [InterceptsLocation("Program.cs", 5, 3)]
    public static void M1(ref this S1 s1, [InterpolatedStringHandlerArgument("s1")] CustomHandler c)
    {
        Console.Write(1);
    }
}

partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, S1 s)
    {
        throw null!; // we don't expect this to be called
    }
}
""";
        var verifier = CompileAndVerify(
            new[]
            {
                (code, "Program.cs"),
                (InterpolatedStringHandlerArgumentAttribute, "a.cs"),
                (GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), "b.cs"),
                s_attributesSource
            },
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }
}
