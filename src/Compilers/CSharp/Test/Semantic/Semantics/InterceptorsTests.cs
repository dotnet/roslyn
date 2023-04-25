// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class InterceptorsTests : CSharpTestBase
{
    // PROTOTYPE(ic): Ensure that all `MethodSymbol.IsInterceptable` implementations have test coverage.

    // PROTOTYPE(ic): Possible test cases:
    //
    // * Intercept instance method with instance method in same class, base class, derived class
    // * Intercept with extern method
    // * Intercept an abstract or interface method
    // * Intercept a virtual or overridden method
    // * Intercept a non-extension call to a static method with a static method when one or both are extension methods
    // * Intercept a struct instance method with an extension method with by-value / by-ref this parameter
    // * An explicit interface implementation marked as interceptable

    // PROTOTYPE(ic): test intercepting an extension method with a non-extension method. Perhaps should be an error for simplicity even if calling in non-reduced form.

    private static readonly (string, string) s_attributesSource = ("""
        namespace System.Runtime.CompilerServices;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class InterceptableAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
        public sealed class InterceptsLocationAttribute : Attribute
        {
            public InterceptsLocationAttribute(string filePath, int line, int character)
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
    public void SelfInterception()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void Main()
                {
                    InterceptableMethod();
                }

                [Interceptable]
                [InterceptsLocation("Program.cs", 8, 9)]
                public static void InterceptableMethod() { Console.Write(1); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
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

    [Fact]
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(17,6): error CS27018: Cannot intercept because 'D.Interceptor1()' is not accessible within 'C.Main()'.
                //     [InterceptsLocation("Program.cs", 11, 9)]
                Diagnostic(ErrorCode.ERR_InterceptorNotAccessible, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("D.Interceptor1()", "C.Main()").WithLocation(17, 6));
    }

    [Fact]
    public void Accessibility_02()
    {
        // An interceptor declared within a file-local type can intercept a call even if the call site can't normally refer to the file-local type.
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

        var verifier = CompileAndVerify(new[] { (source1, "Program.cs"), (source2, "Other.cs"), s_attributesSource }, expectedOutput: "interceptor 1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FileLocalAttributeDefinitions_01()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M()
                {
                    Console.Write(1);
                }
            }

            namespace System.Runtime.CompilerServices
            {
                file class InterceptableAttribute : Attribute { }
                file class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int character) { }
                }
            }
            """;

        var verifier = CompileAndVerify((source, "Program.cs"), expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FileLocalAttributeDefinitions_02()
    {
        var source0 = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [Interceptable]
                public static void M() => throw null!;
            }

            namespace System.Runtime.CompilerServices
            {
                file class InterceptableAttribute : Attribute { }
            }
            """;

        var source1 = """
            using System;
            using System.Runtime.CompilerServices;

            C.M();

            static class D
            {
                [InterceptsLocation("File1.cs", 4, 3)]
                public static void M()
                {
                    Console.Write(1);
                }
            }

            namespace System.Runtime.CompilerServices
            {
                file class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int character) { }
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "File0.cs"), (source1, "File1.cs") }, expectedOutput: "1");
        verifier.VerifyDiagnostics();

        var comp0 = CreateCompilation((source0, "File0.cs"));
        comp0.VerifyEmitDiagnostics();

        var verifier1 = CompileAndVerify((source1, "File1.cs"), new[] { comp0.ToMetadataReference() }, expectedOutput: "1");
        verifier1.VerifyDiagnostics();

        // PROTOTYPE(ic): https://github.com/dotnet/roslyn/issues/67079
        // We are generally treating file-local definitions in source as matching the names of well-known attributes.
        // Once the type is emitted to metadata and read back in, we no longer recognize it as the same attribute due to name mangling.
        var verifier1_1 = CompileAndVerify((source1, "File1.cs"), new[] { comp0.EmitToImageReference() }, expectedOutput: "1");
        verifier1_1.VerifyDiagnostics(
            // File1.cs(8,6): warning CS27000: Call to 'C.M()' is intercepted, but the method is not marked with 'System.Runtime.CompilerServices.InterceptableAttribute'.
            //     [InterceptsLocation("File1.cs", 4, 3)]
            Diagnostic(ErrorCode.WRN_CallNotInterceptable, @"InterceptsLocation(""File1.cs"", 4, 3)").WithArguments("C.M()").WithLocation(8, 6));
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
    public void InterceptableInstanceMethod_InterceptorStaticMethod()
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
                public static C Interceptor1(C i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27007: Cannot intercept method 'C.InterceptableMethod(string)' with interceptor 'D.Interceptor1(C, string)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("C.InterceptableMethod(string)", "D.Interceptor1(C, string)").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptsLocationDuplicatePath()
    {
        var source0 = ("""
            public class D0
            {
                public static void M()
                {
                    C.InterceptableMethod("a");
                }
            }
            """, "Program.cs");

        var source1 = ("""
            public class D1
            {
                public static void M()
                {
                    C.InterceptableMethod("a");
                }
            }
            """, "Program.cs");

        var source2 = ("""
            using System.Runtime.CompilerServices;
            using System;

            D0.M();
            D1.M();

            public class C
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
            }

            public static class Interceptor
            {
                [InterceptsLocation("Program.cs", 5, 11)]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """, "Interceptor.cs");

        var comp = CreateCompilation(new[] { source0, source1, source2, s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Interceptor.cs(15,25): error CS27015: Cannot intercept a call in file with path 'Program.cs' because multiple files in the compilation have this path.
            //     [InterceptsLocation("Program.cs", 5, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorNonUniquePath, @"""Program.cs""").WithArguments("Program.cs").WithLocation(15, 25));
    }

    [Fact]
    public void DuplicateLocation_01()
    {
        var source = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() { }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M1() { }

                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M2() { }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(13, 6),
            // Program.cs(16,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(16, 6));
    }

    [Fact]
    public void DuplicateLocation_02()
    {
        var source0 = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() { }
            }
            """;

        var source1 = """
            using System.Runtime.CompilerServices;

            class D1
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M1() { }
            }
            """;

        var source2 = """
            using System.Runtime.CompilerServices;

            class D2
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M1() { }
            }
            """;

        var comp = CreateCompilation(new[] { (source0, "Program.cs"), (source1, "File1.cs"), (source2, "File2.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // File2.cs(5,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(5, 6),
            // File1.cs(5,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(5, 6)
            );
    }

    [Fact]
    public void DuplicateLocation_03()
    {
        // InterceptsLocationAttribute is not considered to *duplicate* an interception, even if it is inherited.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            var d = new D();
            d.M();

            class C
            {
                [Interceptable]
                public void M() => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public virtual void Interceptor() => throw null!;
            }

            class D : C
            {
                public override void Interceptor() => Console.Write(1);
            }

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class InterceptableAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
                public sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int character)
                    {
                    }
                }
            }
            """;

        var verifier = CompileAndVerify((source, "Program.cs"), expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void DuplicateLocation_04()
    {
        var source = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() { }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M1() { }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(13, 6),
            // Program.cs(14,6): error CS27016: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(14, 6));
    }

    [Fact]
    public void InterceptsLocationFromMetadata()
    {
        // Verify that `[InterceptsLocation]` on a method from metadata does not cause a call in the current compilation to be intercepted.
        var source0 = """
            using System.Runtime.CompilerServices;
            using System;

            public class C0
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                static void M0()
                {
                    InterceptableMethod("1");
                }
            }

            public static class D
            {
                [InterceptsLocation("Program.cs", 11, 9)]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp0 = CreateCompilation(new[] { (source0, "Program.cs"), s_attributesSource });
        comp0.VerifyEmitDiagnostics();

        var source1 = """
            using System.Runtime.CompilerServices;
            using System;

            class C1
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                static void Main()
                {
                    InterceptableMethod("1");
                }
            }
            """;

        var comp1 = CompileAndVerify(new[] { (source1, "Program.cs") }, new[] { comp0.ToMetadataReference() }, expectedOutput: "interceptable 1");
        comp1.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableDelegateConversion()
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
                    var del = c.InterceptableMethod;
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 21)]
                public static C Interceptor1(this C i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        compilation.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27014: Possible method name 'InterceptableMethod' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 15, 21)]
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 15, 21)").WithArguments("InterceptableMethod").WithLocation(21, 6)
            );
    }

    [Fact]
    public void InterceptableNameof()
    {
        var source = """
            using System.Runtime.CompilerServices;

            static class Program
            {
                public static void Main()
                {
                    _ = nameof(Main);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 7, 13)]
                public static void Interceptor1(object param) { }
            }
            """;
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        // PROTOTYPE(ic): this is syntactically an invocation but doesn't result in a BoundCall.
        // we should produce an error here, probably during lowering.
        compilation.VerifyEmitDiagnostics(
            );
    }

    [Fact]
    public void InterceptableDelegateInvocation()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M(() => Console.Write(0));

            static class C
            {
                public static void M(Action action)
                {
                    action();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 10, 9)]
                public static void Interceptor1(this Action action) { Console.Write(1); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        // PROTOTYPE(ic): perhaps give a more specific error here.
        // If/when we change "missing InterceptableAttribute" to an error, we might not need any specific error, because user cannot attribute the Invoke method.
        // I don't think we intend for delegate Invoke to be interceptable, but it doesn't seem harmful to allow it.
        verifier.VerifyDiagnostics(
            // Program.cs(16,6): warning CS27000: Call to 'Action.Invoke()' is intercepted, but the method is not marked with 'System.Runtime.CompilerServices.InterceptableAttribute'.
            //     [InterceptsLocation("Program.cs", 10, 9)]
            Diagnostic(ErrorCode.WRN_CallNotInterceptable, @"InterceptsLocation(""Program.cs"", 10, 9)").WithArguments("System.Action.Invoke()").WithLocation(16, 6)
            );
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
    public void InterceptableExtensionMethod_InterceptorStaticMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            var c = new C();
            c.InterceptableMethod();

            class C { }

            static class D
            {
                [Interceptable]
                public static void InterceptableMethod(this C c) => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public static void Interceptor1(C c) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(14,6): error CS27011: Interceptor must have a 'this' parameter matching parameter 'C c' on 'D.InterceptableMethod(C)'.
            //     [InterceptsLocation("Program.cs", 5, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 5, 3)").WithArguments("C c", "D.InterceptableMethod(C)").WithLocation(14, 6));
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
    public void ParameterNameDifference()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public void InterceptableMethod(string s1) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod(s1: "1");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(this C c, string s2) { Console.Write(s2); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ParameterNamesInDifferentOrder()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public void InterceptableMethod(string s1, string s2) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("1", "2");
                    c.InterceptableMethod(s2: "4", s1: "3");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                [InterceptsLocation("Program.cs", 16, 11)]
                public static void Interceptor1(this C c, string s2, string s1) { Console.Write(s2); Console.Write(s1); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1234");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void AttributeArgumentLabels_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public void InterceptableMethod() => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", character: 11, line: 15)]
                public static void Interceptor1(this C c) { Console.Write(1); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void AttributeArgumentLabels_02()
    {
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                [Interceptable]
                public void InterceptableMethod() => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", character: 1, line: 50)] // 1
                public static void Interceptor1(this C c) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyDiagnostics(
            // Program.cs(20,53): error CS27005: The given file has '22' lines, which is fewer than the provided line number '50'.
            //     [InterceptsLocation("Program.cs", character: 1, line: 50)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorLineOutOfRange, "line: 50").WithArguments("22", "50").WithLocation(20, 53)
            );
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
    public void InterceptsLocation_BadMethodKind()
    {
        var source = """
            using System.Runtime.CompilerServices;

            static class Program
            {
                [Interceptable]
                public static void InterceptableMethod(string param) { }

                public static void Main()
                {
                    InterceptableMethod("");
                    Interceptor1("");

                    var lambda = [InterceptsLocation("Program.cs", 13, 8)] (string param) => { }; // 1

                    [InterceptsLocation("Program.cs", 13, 8)] // 2
                    static void Interceptor1(string param) { }
                }

                public static string Prop
                {
                    [InterceptsLocation("Program.cs", 13, 8)] // 3
                    set { }
                }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyDiagnostics(
            // Program.cs(13,23): error CS27009: An interceptor method must be an ordinary member method.
            //         var lambda = [InterceptsLocation("Program.cs", 13, 8)] (string param) => { }; // 1
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(13, 23),
            // Program.cs(15,10): error CS27009: An interceptor method must be an ordinary member method.
            //         [InterceptsLocation("Program.cs", 13, 8)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(15, 10),
            // Program.cs(21,10): error CS27009: An interceptor method must be an ordinary member method.
            //         [InterceptsLocation("Program.cs", 13, 8)] // 3
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(21, 10)
            );
    }

    [Fact]
    public void Interceptable_BadMethodKind()
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
                    var lambda = [Interceptable] (string param) => { }; // 1

                    InterceptableMethod("call site");

                    [Interceptable] // 2
                    static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
                }

                public static string Prop
                {
                    [Interceptable] // 3
                    set { }
                }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyDiagnostics(
            // Program.cs(11,23): error CS27008: An interceptable method must be an ordinary member method.
            //         var lambda = [Interceptable] (string param) => { }; // 1
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "Interceptable").WithLocation(11, 23),
            // Program.cs(15,10): error CS27008: An interceptable method must be an ordinary member method.
            //         [Interceptable] // 2
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "Interceptable").WithLocation(15, 10),
            // Program.cs(21,10): error CS27008: An interceptable method must be an ordinary member method.
            //         [Interceptable] // 3
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "Interceptable").WithLocation(21, 10)
            );
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
            // Program.cs(20,6): warning CS27000: Call to 'C.InterceptableMethod(string)' is intercepted, but the method is not marked with 'System.Runtime.CompilerServices.InterceptableAttribute'.
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
                public static void InterceptableMethod<T>(T t) { Console.Write("0"); }
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
    public void InterceptableGeneric_03()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod<T>(T t) where T : class => throw null!;
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
                [InterceptsLocation("Program.cs", 14, 11)]
                public static void Interceptor1(string s) { Console.Write(s); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_04()
    {
        // No interceptor can satisfy a signature like `void InterceptableMethod<T2>(T2 t2)` where `T2` is a method type argument.
        // We would need to re-examine arity limitations and devise method type argument inference rules for interceptors to make this work.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod<T1>(T1 t) => throw null!;
            }

            static class Program
            {
                public static void M<T2>(T2 t)
                {
                    C.InterceptableMethod(t);
                    C.InterceptableMethod<T2>(t);
                    C.InterceptableMethod<object>(t);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)] // 1
                [InterceptsLocation("Program.cs", 15, 11)] // 2
                [InterceptsLocation("Program.cs", 16, 11)]
                public static void Interceptor1(object s) { Console.Write(s); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS27007: Cannot intercept method 'C.InterceptableMethod<T2>(T2)' with interceptor 'D.Interceptor1(object)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 14, 11)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 14, 11)").WithArguments("C.InterceptableMethod<T2>(T2)", "D.Interceptor1(object)").WithLocation(22, 6),
            // Program.cs(23,6): error CS27007: Cannot intercept method 'C.InterceptableMethod<T2>(T2)' with interceptor 'D.Interceptor1(object)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 15, 11)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("C.InterceptableMethod<T2>(T2)", "D.Interceptor1(object)").WithLocation(23, 6)
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
            // Program.cs(21,25): error CS27002: Cannot intercept: compilation does not contain a file with path 'BAD'.
            //     [InterceptsLocation("BAD", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilation, @"""BAD""").WithArguments("BAD").WithLocation(21, 25)
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
            // /Users/me/projects/Program.cs(21,25): error CS27003: Cannot intercept: compilation does not contain a file with path 'projects/Program.cs'. Did you mean to use path '/Users/me/projects/Program.cs'?
            //     [InterceptsLocation("projects/Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"""projects/Program.cs""").WithArguments("projects/Program.cs", "/Users/me/projects/Program.cs").WithLocation(21, 25)
            );
    }

    [Fact]
    public void InterceptsLocationBadPath_03()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C { }

            static class Program
            {
                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation(null, 15, 11)]
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,25): error CS27013: Interceptor cannot have a 'null' file path.
            //     [InterceptsLocation(null, 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorFilePathCannotBeNull, "null").WithLocation(20, 25)
            );
    }

    [Fact]
    public void InterceptsLocationBadPath_04()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C { }

            static class Program
            {
                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                public static void Main()
                {
                    var c = new C();
                    c.InterceptableMethod("call site");
                }
            }

            static class D
            {
                [InterceptsLocation("program.cs", 15, 11)]
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,25): error CS27002: Cannot intercept: compilation does not contain a file with path 'program.cs'.
            //     [InterceptsLocation("program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilation, @"""program.cs""").WithArguments("program.cs").WithLocation(20, 25)
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
                [InterceptsLocation("Program.cs", 25, 1)]
                [InterceptsLocation("Program.cs", 26, 1)]
                [InterceptsLocation("Program.cs", 100, 1)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27004: The provided line and character number does not refer to an interceptable method name, but rather to token '}'.
            //     [InterceptsLocation("Program.cs", 25, 1)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 25, 1)").WithArguments("}").WithLocation(21, 6),
            // Program.cs(22,39): error CS27005: The given file has '25' lines, which is fewer than the provided line number '26'.
            //     [InterceptsLocation("Program.cs", 26, 1)]
            Diagnostic(ErrorCode.ERR_InterceptorLineOutOfRange, "26").WithArguments("25", "26").WithLocation(22, 39),
            // Program.cs(23,39): error CS27005: The given file has '25' lines, which is fewer than the provided line number '100'.
            //     [InterceptsLocation("Program.cs", 100, 1)]
            Diagnostic(ErrorCode.ERR_InterceptorLineOutOfRange, "100").WithArguments("25", "100").WithLocation(23, 39)
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
                [InterceptsLocation("Program.cs", 16, 5)]
                [InterceptsLocation("Program.cs", 16, 6)]
                [InterceptsLocation("Program.cs", 16, 1000)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27004: The provided line and character number does not refer to an interceptable method name, but rather to token '}'.
            //     [InterceptsLocation("Program.cs", 16, 5)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 16, 5)").WithArguments("}").WithLocation(21, 6),
            // Program.cs(22,43): error CS27006: The given line is '5' characters long, which is fewer than the provided character number '6'.
            //     [InterceptsLocation("Program.cs", 16, 6)]
            Diagnostic(ErrorCode.ERR_InterceptorCharacterOutOfRange, "6").WithArguments("5", "6").WithLocation(22, 43),
            // Program.cs(23,43): error CS27006: The given line is '5' characters long, which is fewer than the provided character number '1000'.
            //     [InterceptsLocation("Program.cs", 16, 1000)]
            Diagnostic(ErrorCode.ERR_InterceptorCharacterOutOfRange, "1000").WithArguments("5", "1000").WithLocation(23, 43)
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
            // Program.cs(21,6): error CS27010: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '15' and character '11'?
            //     [InterceptsLocation("Program.cs", 15, 13)]
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 15, 13)").WithArguments("InterceptableMethod", "15", "11").WithLocation(21, 6)
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
            // Program.cs(20,6): error CS27010: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '12' and character '13'?
            //     [InterceptsLocation("Program.cs", 12, 11)] // intercept spaces before 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 12, 11)").WithArguments("InterceptableMethod", "12", "13").WithLocation(20, 6),
            // Program.cs(21,6): error CS27010: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '14' and character '11'?
            //     [InterceptsLocation("Program.cs", 14, 33)] // intercept spaces after 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 14, 33)").WithArguments("InterceptableMethod", "14", "11").WithLocation(21, 6)
        );
    }

    [Fact]
    public void InterceptsLocationBadPosition_06()
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
                    c.InterceptableMethod/*comment*/("call site");
                }

                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 11, 31)] // intercept comment after 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS27010: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '11' and character '11'?
            //     [InterceptsLocation("Program.cs", 11, 31)] // intercept comment after 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 11, 31)").WithArguments("InterceptableMethod", "11", "11").WithLocation(17, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_07()
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
                        // comment
                        InterceptableMethod("call site");
                }

                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 12, 13)] // intercept comment above 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(19,6): error CS27010: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '13' and character '13'?
                //     [InterceptsLocation("Program.cs", 12, 13)] // intercept comment above 'InterceptableMethod' token
                Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 12, 13)").WithArguments("InterceptableMethod", "13", "13").WithLocation(19, 6)
            );
    }

    [Fact]
    public void InterceptsLocationBadPosition_08()
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
                    c.InterceptableMethod("call site");
                }

                [Interceptable]
                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", -1, 1)] // 1
                [InterceptsLocation("Program.cs", 1, -1)] // 2
                [InterceptsLocation("Program.cs", -1, -1)] // 3
                [InterceptsLocation("Program.cs", 0, 1)] // 4
                [InterceptsLocation("Program.cs", 1, 0)] // 5 
                [InterceptsLocation("Program.cs", 0, 0)] // 6
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,39): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", -1, 1)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(17, 39),
            // Program.cs(18,42): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 1, -1)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(18, 42),
            // Program.cs(19,39): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", -1, -1)] // 3
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(19, 39),
            // Program.cs(20,39): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 0, 1)] // 4
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "0").WithLocation(20, 39),
            // Program.cs(21,42): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 1, 0)] // 5 
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "0").WithLocation(21, 42),
            // Program.cs(22,39): error CS27020: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 0, 0)] // 6
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "0").WithLocation(22, 39)
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
                // Program.cs(21,6): error CS27007: Cannot intercept method 'Program.InterceptableMethod(I1, string)' with interceptor 'D.Interceptor1(I1, int)' because the signatures do not match.
                //     [InterceptsLocation("Program.cs", 15, 11)]
                Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("Program.InterceptableMethod(I1, string)", "D.Interceptor1(I1, int)").WithLocation(21, 6)
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
    public void SignatureMismatch_04()
    {
        // Safe nullability difference
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                [Interceptable]
                public string? InterceptableMethod(string param) => throw null!;
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
                public static string Interceptor1(this C s, string? param) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void SignatureMismatch_05()
    {
        // Unsafe nullability difference
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                [Interceptable]
                public void Method1(string? param1) => throw null!;

                [Interceptable]
                public string Method2() => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.Method1("call site");
                    _ = c.Method2();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 17, 11)] // 1
                public static void Interceptor1(this C s, string param2) => throw null!;

                [InterceptsLocation("Program.cs", 18, 15)] // 2
                public static string? Interceptor2(this C s) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(24,6): warning CS27022: Nullability of reference types in type of parameter 'param2' doesn't match interceptable method 'C.Method1(string?)'.
            //     [InterceptsLocation("Program.cs", 17, 11)] // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor, @"InterceptsLocation(""Program.cs"", 17, 11)").WithArguments("param2", "C.Method1(string?)").WithLocation(24, 6),
            // Program.cs(27,6): warning CS27021: Nullability of reference types in return type doesn't match interceptable method 'C.Method2()'.
            //     [InterceptsLocation("Program.cs", 18, 15)] // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor, @"InterceptsLocation(""Program.cs"", 18, 15)").WithArguments("C.Method2()").WithLocation(27, 6)
            );

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, options: WithNullableDisable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(6,31): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            //     public void Method1(string? param1) => throw null!;
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(6, 31),
            // Program.cs(28,25): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            //     public static string? Interceptor2(this C s) => throw null!;
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(28, 25)
            );
    }

    [Fact]
    public void SignatureMismatch_06()
    {
        // 'dynamic' difference
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                [Interceptable]
                public void Method1(object param1) => throw null!;

                [Interceptable]
                public dynamic Method2() => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.Method1("call site");
                    _ = c.Method2();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 17, 11)] // 1
                public static void Interceptor1(this C s, dynamic param2) => throw null!;

                [InterceptsLocation("Program.cs", 18, 15)] // 2
                public static object Interceptor2(this C s) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(24,6): warning CS27017: Intercepting a call to 'C.Method1(object)' with interceptor 'D.Interceptor1(C, dynamic)', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 17, 11)] // 1
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 17, 11)").WithArguments("C.Method1(object)", "D.Interceptor1(C, dynamic)").WithLocation(24, 6),
            // Program.cs(27,6): warning CS27017: Intercepting a call to 'C.Method2()' with interceptor 'D.Interceptor2(C)', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 18, 15)] // 2
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 18, 15)").WithArguments("C.Method2()", "D.Interceptor2(C)").WithLocation(27, 6)
            );
    }

    [Fact]
    public void SignatureMismatch_07()
    {
        // tuple element name difference
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public void Method1((string x, string y) param1) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.Method1(default!);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1(this C s, (string a, string b) param2) => Console.Write(1);
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ScopedMismatch_01()
    {
        // Unsafe 'scoped' difference
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                [Interceptable]
                public static ref int InterceptableMethod(scoped ref int value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    int i = 0;
                    C.InterceptableMethod(ref i);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)] // 1
                public static ref int Interceptor1(ref int value) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS27019: Cannot intercept call to 'C.InterceptableMethod(scoped ref int)' with 'D.Interceptor1(ref int)' because of a difference in 'scoped' modifiers or '[UnscopedRef]' attributes.
            //     [InterceptsLocation("Program.cs", 14, 11)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorScopedMismatch, @"InterceptsLocation(""Program.cs"", 14, 11)").WithArguments("C.InterceptableMethod(scoped ref int)", "D.Interceptor1(ref int)").WithLocation(20, 6)
            );
    }

    [Fact]
    public void ScopedMismatch_02()
    {
        // safe 'scoped' difference
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static ref int InterceptableMethod(ref int value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    int i = 0;
                    _ = C.InterceptableMethod(ref i);
                }
            }

            static class D
            {
                static int i;

                [InterceptsLocation("Program.cs", 15, 15)]
                public static ref int Interceptor1(scoped ref int value)
                {
                    Console.Write(1);
                    return ref i;
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ScopedMismatch_03()
    {
        // safe '[UnscopedRef]' difference
        var source = """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static ref int InterceptableMethod([UnscopedRef] out int value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    _ = C.InterceptableMethod(out int i);
                }
            }

            static class D
            {
                static int i;

                [InterceptsLocation("Program.cs", 15, 15)]
                public static ref int Interceptor1(out int value)
                {
                    Console.Write(1);
                    value = 0;
                    return ref i;
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource, (UnscopedRefAttributeDefinition, "UnscopedRefAttribute.cs") }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ScopedMismatch_04()
    {
        // unsafe '[UnscopedRef]' difference
        var source = """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static ref int InterceptableMethod(out int value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod(out int i);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 11)] // 1
                public static ref int Interceptor1([UnscopedRef] out int value) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource, (UnscopedRefAttributeDefinition, "UnscopedRefAttribute.cs") }, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS27019: Cannot intercept call to 'C.InterceptableMethod(out int)' with 'D.Interceptor1(out int)' because of a difference in 'scoped' modifiers or '[UnscopedRef]' attributes.
            //     [InterceptsLocation("Program.cs", 15, 11)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorScopedMismatch, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("C.InterceptableMethod(out int)", "D.Interceptor1(out int)").WithLocation(21, 6)
            );
    }

    [Fact]
    public void ReferenceEquals_01()
    {
        // A call to 'object.ReferenceEquals(a, b)' is defined as being equivalent to '(object)a == b'.
        var source = """
            using System.Runtime.CompilerServices;

            static class D
            {
                [Interceptable]
                public static bool Interceptable(object? obj1, object? obj2) => throw null!;

                public static void M0(object? obj1, object? obj2)
                {
                    if (obj1 == obj2)
                       throw null!;
                }

                public static void M1(object? obj1, object? obj2)
                {
                    if (Interceptable(obj1, obj2))
                       throw null!;
                }

                public static void M2(object? obj1, object? obj2)
                {
                    if (Interceptable(obj1, obj2))
                       throw null!;
                }
            }

            namespace System
            {
                public class Object
                {
                    [InterceptsLocation("Program.cs", 16, 13)]
                    public static bool ReferenceEquals(object? obj1, object? obj2) => throw null!;

                    [InterceptsLocation("Program.cs", 22, 13)]
                    public static bool NotReferenceEquals(object? obj1, object? obj2) => throw null!;
                }
            
                public class Void { }
                public struct Boolean { }
                public class String { }
                public class Attribute { }
                public abstract class Enum { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets targets) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
                public class Exception { }
                public abstract class ValueType { }
                public struct Int32 { }
                public struct Byte { }
            }

            namespace System.Runtime.CompilerServices
            {
                public sealed class InterceptableAttribute : Attribute { }

                public sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int column)
                    {
                    }
                }
            }
            """;
        var verifier = CompileAndVerify(CreateEmptyCompilation((source, "Program.cs"), options: WithNullableEnable()), verify: Verification.Skipped);
        verifier.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

        var referenceEqualsCallIL = """
            {
              // Code size        7 (0x7)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  bne.un.s   IL_0006
              IL_0004:  ldnull
              IL_0005:  throw
              IL_0006:  ret
            }
            """;
        verifier.VerifyIL("D.M0", referenceEqualsCallIL);
        verifier.VerifyIL("D.M1", referenceEqualsCallIL);

        verifier.VerifyIL("D.M2", """
            {
              // Code size       12 (0xc)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "bool object.NotReferenceEquals(object, object)"
              IL_0007:  brfalse.s  IL_000b
              IL_0009:  ldnull
              IL_000a:  throw
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void ReferenceEquals_02()
    {
        // Intercept a call to object.ReferenceEquals
        var source = """
            using System.Runtime.CompilerServices;

            static class D
            {
                public static void M0(object? obj1, object? obj2)
                {
                    if (object.ReferenceEquals(obj1, obj2))
                       throw null!;
                }

                [InterceptsLocation("Program.cs", 7, 20)]
                public static bool Interceptor(object? obj1, object? obj2)
                {
                    return false;
                }
            }

            namespace System
            {
                public class Object
                {
                    [Interceptable]
                    public static bool ReferenceEquals(object? obj1, object? obj2) => throw null!;
                }
            
                public class Void { }
                public struct Boolean { }
                public class String { }
                public class Attribute { }
                public abstract class Enum { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets targets) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
                public class Exception { }
                public abstract class ValueType { }
                public struct Int32 { }
                public struct Byte { }
            }

            namespace System.Runtime.CompilerServices
            {
                public sealed class InterceptableAttribute : Attribute { }

                public sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int column)
                    {
                    }
                }
            }
            """;
        var verifier = CompileAndVerify(CreateEmptyCompilation((source, "Program.cs"), options: WithNullableEnable()), verify: Verification.Skipped);
        verifier.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

        verifier.VerifyIL("D.M0", """
            {
              // Code size       12 (0xc)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "bool D.Interceptor(object, object)"
              IL_0007:  brfalse.s  IL_000b
              IL_0009:  ldnull
              IL_000a:  throw
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void ParamsMismatch_01()
    {
        // Test when interceptable method has 'params' parameter.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod(params int[] value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod(1, 2, 3);
                    C.InterceptableMethod(4, 5, 6);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)]
                public static void Interceptor1(int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }

                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor2(params int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "123456");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ParamsMismatch_02()
    {
        // Test when interceptable method lacks 'params' parameter, and interceptor has one, and method is called as if it has one.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod(int[] value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod(1, 2, 3 ); // 1
                    C.InterceptableMethod(4, 5, 6); // 2
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)]
                public static void Interceptor1(int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }

                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor2(params int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
                // Program.cs(14,11): error CS1501: No overload for method 'InterceptableMethod' takes 3 arguments
                //         C.InterceptableMethod(1, 2, 3 ); // 1
                Diagnostic(ErrorCode.ERR_BadArgCount, "InterceptableMethod").WithArguments("InterceptableMethod", "3").WithLocation(14, 11),
                // Program.cs(15,11): error CS1501: No overload for method 'InterceptableMethod' takes 3 arguments
                //         C.InterceptableMethod(4, 5, 6); // 2
                Diagnostic(ErrorCode.ERR_BadArgCount, "InterceptableMethod").WithArguments("InterceptableMethod", "3").WithLocation(15, 11));
    }

    [Fact]
    public void ParamsMismatch_03()
    {
        // Test when interceptable method lacks 'params' parameter, and interceptor has one, and method is called in normal form.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod(int[] value) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    C.InterceptableMethod(new[] { 1, 2, 3 });
                    C.InterceptableMethod(new[] { 4, 5, 6 });
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 14, 11)]
                public static void Interceptor1(int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }

                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor2(params int[] value)
                {
                    foreach (var i in value)
                        Console.Write(i);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "123456");
        verifier.VerifyDiagnostics();
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

    [Fact]
    public void InterpolatedStringHandler_03()
    {
        // Verify that interpolated string attributes on an interceptor don't cause us to somehow pick a different argument.
        var code = """
using System;
using System.Runtime.CompilerServices;

var s1 = new S1(1);
var s2 = new S1(2);
S1.M(s1, s2, $"");

public struct S1
{
    public S1(int field) => Field = field;
    public int Field = 1;

    [Interceptable]
    public static void M(S1 s1, S1 s2, [InterpolatedStringHandlerArgument("s1")] CustomHandler c)
    {
        Console.Write(0);
    }
}

public static class S1Ext
{
    [InterceptsLocation("Program.cs", 6, 4)]
    public static void M1(S1 s2, S1 s3, [InterpolatedStringHandlerArgument("s2")] CustomHandler c)
    {
        Console.Write(2);
    }
}

partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, S1 s)
    {
        Console.Write(s.Field);
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
    public void LineDirective_01()
    {
        // Verify that line directives are not considered when deciding if a particular call is being intercepted.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod() { Console.Write("interceptable"); }

                public static void Main()
                {
                    #line 42 "OtherFile.cs"
                    InterceptableMethod();
                }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 12, 9)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "interceptor 1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void LineDirective_02()
    {
        // Verify that line directives are not considered when deciding if a particular call is being intercepted.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                [Interceptable]
                public static void InterceptableMethod() { Console.Write("interceptable"); }

                public static void Main()
                {
                    #line 42 "OtherFile.cs"
                    InterceptableMethod();
                }
            }

            class D
            {
                [InterceptsLocation("OtherFile.cs", 42, 9)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // OtherFile.cs(48,25): error CS27002: Cannot intercept: compilation does not contain a file with path 'OtherFile.cs'.
            //     [InterceptsLocation("OtherFile.cs", 42, 9)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilation, @"""OtherFile.cs""").WithArguments("OtherFile.cs").WithLocation(48, 25));
    }

    [Fact]
    public void ObsoleteInterceptor()
    {
        // Expect no Obsolete diagnostics to be reported
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() => throw null!;
            }

            class D
            {
                [Obsolete]
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void CallerInfo()
    {
        // CallerLineNumber, etc. on the interceptor doesn't affect the default arguments passed to an intercepted call.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                [Interceptable]
                public static void M(int lineNumber = 1) => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1([CallerLineNumber] int lineNumber = 0) => Console.Write(lineNumber);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableExplicitImplementation()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            var c = new C();
            ((I)c).M();

            interface I
            {
                void M();
            }

            class C : I
            {
                [Interceptable] // 1
                void I.M() => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 5, 8)]
                public static void Interceptor(this I i) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(
            // Program.cs(14,6): error CS27008: An interceptable method must be an ordinary member method.
            //     [Interceptable] // 1
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "Interceptable").WithLocation(14, 6)
            );
    }

    [Fact]
    public void InterceptorExtern()
    {
        var source = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
                [Interceptable]
                public static void M() => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                public static extern void Interceptor();
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, verify: Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size        6 (0x6)
              .maxstack  0
              IL_0000:  call       "void D.Interceptor()"
              IL_0005:  ret
            }
            """);
    }

    [Fact]
    public void InterceptorAbstract()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            var d = new D();
            d.M();

            abstract class C
            {
                [Interceptable]
                public void M() => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public abstract void Interceptor();
            }

            class D : C
            {
                public override void Interceptor() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptorInterface()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            I i = new C();
            i.M();

            interface I
            {
                [Interceptable]
                public void M();

                [InterceptsLocation("Program.cs", 5, 3)]
                void Interceptor();
            }

            class C : I
            {
                public void M() => throw null!;
                public void Interceptor() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }
}
