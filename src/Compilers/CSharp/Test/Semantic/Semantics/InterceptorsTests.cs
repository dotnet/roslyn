﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class InterceptorsTests : CSharpTestBase
{
    private static readonly (string, string) s_attributesSource = ("""
        namespace System.Runtime.CompilerServices;

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
        public sealed class InterceptsLocationAttribute : Attribute
        {
            public InterceptsLocationAttribute(string filePath, int line, int character)
            {
            }
        }
        """, "attributes.cs");

    private static readonly CSharpParseOptions RegularWithInterceptors = TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "global");

    [Fact]
    public void FeatureFlag()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M() => Console.Write(1);
            }
            """;

        var sadCaseDiagnostics = new[]
        {
            // Program.cs(13,6): error CS9206: An interceptor cannot be declared in the global namespace.
            //     [InterceptsLocation("Program.cs", 4, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorGlobalNamespace, @"InterceptsLocation(""Program.cs"", 4, 3)").WithLocation(13, 6)
        };
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource });
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreview-experimental"));
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreview", "false"));
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("interceptorspreview"));
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreview", "Global"));
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreview", "global.a"));
        comp.VerifyEmitDiagnostics(sadCaseDiagnostics);

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FeatureFlag_Granular_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            namespace NS1
            {
                class D
                {
                    [InterceptsLocation("Program.cs", 4, 3)]
                    public static void M() => Console.Write(1);
                }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "NS"));
        comp.VerifyEmitDiagnostics(
            // Program.cs(15,10): error CS9137: The 'interceptors' experimental feature is not enabled. Add '<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1</InterceptorsPreviewNamespaces>' to your project.
            //         [InterceptsLocation("Program.cs", 4, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorsFeatureNotEnabled, @"InterceptsLocation(""Program.cs"", 4, 3)").WithArguments("<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1</InterceptorsPreviewNamespaces>").WithLocation(15, 10));

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "NS1.NS2"));
        comp.VerifyEmitDiagnostics(
            // Program.cs(15,10): error CS9137: The 'interceptors' experimental feature is not enabled. Add '<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1</InterceptorsPreviewNamespaces>' to your project.
            //         [InterceptsLocation("Program.cs", 4, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorsFeatureNotEnabled, @"InterceptsLocation(""Program.cs"", 4, 3)").WithArguments("<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1</InterceptorsPreviewNamespaces>").WithLocation(15, 10));

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "NS1"), expectedOutput: "1");
        verifier.VerifyDiagnostics();

        verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "NS1;NS2"), expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FeatureFlag_Granular_02()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            namespace NS1.NS2
            {
                class D
                {
                    [InterceptsLocation("Program.cs", 4, 3)]
                    public static void M() => Console.Write(1);
                }
            }
            """;

        sadCase("NS2");
        sadCase("true");
        sadCase(" NS1");
        sadCase(";");
        sadCase("");
        sadCase("NS1 ;");
        sadCase("NS1..NS2;");
        sadCase("ns1");
        sadCase("NS2.NS1");
        sadCase("$NS1&");

        happyCase("NS1");
        happyCase("NS1;");
        happyCase(";NS1");
        happyCase("NS1.NS2");
        happyCase("NS2;NS1.NS2");

        void sadCase(string featureValue)
        {
            var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", featureValue));
            comp.VerifyEmitDiagnostics(
                // Program.cs(15,10): error CS9137: The 'interceptors' experimental feature is not enabled. Add '<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1.NS2</InterceptorsPreviewNamespaces>' to your project.
                //         [InterceptsLocation("Program.cs", 4, 3)]
                Diagnostic(ErrorCode.ERR_InterceptorsFeatureNotEnabled, @"InterceptsLocation(""Program.cs"", 4, 3)").WithArguments("<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);NS1.NS2</InterceptorsPreviewNamespaces>").WithLocation(15, 10));
        }

        void happyCase(string featureValue)
        {
            var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", featureValue), expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }
    }

    [Fact]
    public void FeatureFlag_Granular_03()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M() => Console.Write(1);
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", ""));
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS9206: An interceptor cannot be declared in the global namespace.
            //     [InterceptsLocation("Program.cs", 4, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorGlobalNamespace, @"InterceptsLocation(""Program.cs"", 4, 3)").WithLocation(13, 6));
    }

    [Fact]
    public void FeatureFlag_Granular_04()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            namespace global
            {
                class D
                {
                    [InterceptsLocation("Program.cs", 4, 3)]
                    public static void M() => Console.Write(1);
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "global"), expectedOutput: "1");
        verifier.VerifyDiagnostics();

        verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("interceptorspreviewnamespaces", "global"), expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FeatureFlag_Granular_05()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {
                public static void M() => throw null!;
            }

            namespace global.B
            {
                class D
                {
                    [InterceptsLocation("Program.cs", 4, 3)]
                    public static void M() => Console.Write(1);
                }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreviewNamespaces", "global.A"));
        comp.VerifyEmitDiagnostics(
            // Program.cs(15,10): error CS9137: The 'interceptors' experimental feature is not enabled in this namespace. Add '<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);global.B</InterceptorsPreviewNamespaces>' to your project.
            //         [InterceptsLocation("Program.cs", 4, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorsFeatureNotEnabled, @"InterceptsLocation(""Program.cs"", 4, 3)").WithArguments("<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);global.B</InterceptorsPreviewNamespaces>").WithLocation(15, 10));
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


                [InterceptsLocation("Program.cs", 8, 9)]
                public static void InterceptableMethod() { Console.Write(1); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor 1");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
                // Program.cs(17,6): error CS9155: Cannot intercept because 'D.Interceptor1()' is not accessible within 'C.Main()'.
                //     [InterceptsLocation("Program.cs", 11, 9)]
                Diagnostic(ErrorCode.ERR_InterceptorNotAccessible, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("D.Interceptor1()", "C.Main()").WithLocation(17, 6));
    }

    [Fact]
    public void Accessibility_02()
    {
        // An interceptor declared within a file-local type can intercept a call even if the call site can't normally refer to the file-local type.
        var source1 = """
            using System;

            class C
            {

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
                [InterceptsLocation("Program.cs", 10, 9)]
                public static void Interceptor1() { Console.Write("interceptor 1"); }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source1, "Program.cs"), (source2, "Other.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor 1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void FileLocalAttributeDefinitions_01()
    {
        // Treat a file-local declaration of InterceptsLocationAttribute as a well-known attribute within the declaring compilation.
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {
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
                file class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int character) { }
                }
            }
            """;

        var verifier = CompileAndVerify((source, "Program.cs"), parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    /// <summary>
    /// File-local InterceptsLocationAttribute from another compilation is not considered to *duplicate* an interception, even if it is inherited.
    /// See also <see cref="DuplicateLocation_03"/>.
    /// </summary>
    [Fact]
    public void FileLocalAttributeDefinitions_02()
    {
        var source1 = """
            using System.Runtime.CompilerServices;
            using System;

            var c = new C();
            c.M();

            public class C
            {
                public void M() => Console.Write(1);

                [InterceptsLocation("Program.cs", 5, 3)]
                public virtual void Interceptor() => throw null!;
            }

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
                file sealed class InterceptsLocationAttribute : Attribute
                {
                    public InterceptsLocationAttribute(string filePath, int line, int character)
                    {
                    }
                }
            }
            """;

        // Inherited attribute on 'override void Interceptor' from other compilation doesn't cause a call in this compilation to be intercepted.
        var source2 = """


            // leading blank lines for alignment with the call in the other compilation.
            var d = new D();
            d.M();

            class D : C
            {
                public override void Interceptor() => throw null!;
            }
            """;

        var comp1 = CreateCompilation((source1, "Program.cs"), parseOptions: RegularWithInterceptors);
        comp1.VerifyEmitDiagnostics();

        var comp2Verifier = CompileAndVerify((source2, "Program.cs"), references: new[] { comp1.ToMetadataReference() }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        comp2Verifier.VerifyDiagnostics();

        comp2Verifier = CompileAndVerify((source2, "Program.cs"), references: new[] { comp1.EmitToImageReference() }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        comp2Verifier.VerifyDiagnostics();
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableExtensionMethod_InterceptorExtensionMethod_NormalForm()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            interface I1 { }
            class C : I1 { }

            static class Program
            {

                public static I1 InterceptableMethod(this I1 i1, string param) { Console.Write("interceptable " + param); return i1; }

                public static void Main()
                {
                    var c = new C();
                    InterceptableMethod(c, "call site");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 15, 9)]
                public static I1 Interceptor1(this I1 i1, string param) { Console.Write("interceptor " + param); return i1; }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9144: Cannot intercept method 'C.InterceptableMethod(string)' with interceptor 'D.Interceptor1(C, string)' because the signatures do not match.
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

                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }
            }

            public static class Interceptor
            {
                [InterceptsLocation("Program.cs", 5, 11)]
                public static void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """, "Interceptor.cs");

        var comp = CreateCompilation(new[] { source0, source1, source2, s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Interceptor.cs(15,25): error CS9152: Cannot intercept a call in file with path 'Program.cs' because multiple files in the compilation have this path.
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

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(13, 6),
            // Program.cs(16,6): error CS9153: The indicated call is intercepted multiple times.
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

        var comp = CreateCompilation(new[] { (source0, "Program.cs"), (source1, "File1.cs"), (source2, "File2.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // File2.cs(5,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(5, 6),
            // File1.cs(5,6): error CS9153: The indicated call is intercepted multiple times.
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

        var verifier = CompileAndVerify((source, "Program.cs"), parseOptions: RegularWithInterceptors, expectedOutput: "1");
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

                public static void M() { }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                [InterceptsLocation("Program.cs", 3, 3)]
                public static void M1() { }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(13, 6),
            // Program.cs(14,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 3, 3)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 3, 3)").WithLocation(14, 6));
    }

    [Fact]
    public void InterceptorVirtual_01()
    {
        // Intercept a method call with a call to a virtual method on the same type.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            c = new D();
            c.M();

            class C
            {

                public void M() => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                [InterceptsLocation("Program.cs", 8, 3)]
                public virtual void Interceptor() => Console.Write("C");
            }

            class D : C
            {
                public override void Interceptor() => Console.Write("D");
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

        var verifier = CompileAndVerify((source, "Program.cs"), parseOptions: RegularWithInterceptors, expectedOutput: "CD");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptorVirtual_02()
    {
        // Intercept a call with a virtual method call on the base type.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D d = new D();
            d.M();

            class C
            {
                [InterceptsLocation("Program.cs", 5, 3)]
                public virtual void Interceptor() => throw null!;
            }

            class D : C
            {

                public void M() => throw null!;

                public override void Interceptor() => throw null!;
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

        var comp = CreateCompilation((source, "Program.cs"), parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(9,6): error CS9148: Interceptor must have a 'this' parameter matching parameter 'D this' on 'D.M()'.
            //     [InterceptsLocation("Program.cs", 5, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 5, 3)").WithArguments("D this", "D.M()").WithLocation(9, 6));
    }

    [Fact]
    public void InterceptorOverride_01()
    {
        // Intercept a call with a call to an override method on a derived type.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D d = new D();
            d.M();

            class C
            {

                public void M() => throw null!;

                public virtual void Interceptor() => throw null!;
            }

            class D : C
            {
                [InterceptsLocation("Program.cs", 5, 3)] // 1
                public override void Interceptor() => throw null!;
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

        var comp = CreateCompilation((source, "Program.cs"), parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS9148: Interceptor must have a 'this' parameter matching parameter 'C this' on 'C.M()'.
            //     [InterceptsLocation("Program.cs", 5, 3)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 5, 3)").WithArguments("C this", "C.M()").WithLocation(17, 6));
    }

    [Fact]
    public void InterceptorOverride_02()
    {
        // Intercept a call with an override method on the same type.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D d = new D();
            d.M();

            class C
            {
                public virtual void Interceptor() => throw null!;
            }

            class D : C
            {

                public void M() => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
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

        var verifier = CompileAndVerify((source, "Program.cs"), parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void EmitMetadataOnly_01()
    {
        // We can emit a ref assembly even though there are duplicate interceptions.
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {
                public static void Main()
                {
                    C.M();
                }


                public static void M() { }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 7, 11)]
                public static void M1() { }

                [InterceptsLocation("Program.cs", 7, 11)]
                public static void M2() { }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, emitOptions: EmitOptions.Default.WithEmitMetadataOnly(true));
        verifier.VerifyDiagnostics();

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(16,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 7, 11)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 7, 11)").WithLocation(16, 6),
            // Program.cs(19,6): error CS9153: The indicated call is intercepted multiple times.
            //     [InterceptsLocation("Program.cs", 7, 11)]
            Diagnostic(ErrorCode.ERR_DuplicateInterceptor, @"InterceptsLocation(""Program.cs"", 7, 11)").WithLocation(19, 6));
    }

    [Fact]
    public void EmitMetadataOnly_02()
    {
        // We can't emit a ref assembly when a problem is found with an InterceptsLocationAttribute in the declaration phase.
        // Strictly, we should perhaps allow this emit anyway, but it doesn't feel urgent to do so.
        var source = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {

                public static void M() { }
            }

            class D
            {
                [InterceptsLocation("Program.cs", 3, 4)]
                public static void M1() { }

            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(EmitOptions.Default.WithEmitMetadataOnly(true),
            // Program.cs(13,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '('.
            //     [InterceptsLocation("Program.cs", 3, 4)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 3, 4)").WithArguments("(").WithLocation(13, 6));
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
        var comp0 = CreateCompilation(new[] { (source0, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp0.VerifyEmitDiagnostics();

        var source1 = """

            using System;

            class C1
            {

                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                static void Main()
                {
                    InterceptableMethod("1");
                }
            }
            """;

        var comp1 = CompileAndVerify(new[] { (source1, "Program.cs") }, new[] { comp0.ToMetadataReference() }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptable 1");
        comp1.VerifyDiagnostics();

        comp1 = CompileAndVerify(new[] { (source1, "Program.cs") }, new[] { comp0.EmitToImageReference() }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptable 1");
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
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        compilation.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9151: Possible method name 'InterceptableMethod' cannot be intercepted because it is not being invoked.
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
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        compilation.VerifyEmitDiagnostics(
            // Program.cs(7,13): error CS9160: A nameof operator cannot be intercepted.
            //         _ = nameof(Main);
            Diagnostic(ErrorCode.ERR_InterceptorCannotInterceptNameof, "nameof").WithLocation(7, 13)
            );
    }

    [Fact]
    public void InterceptableNameof_MethodCall()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            static class Program
            {
                public static void Main()
                {
                    _ = nameof(F);
                }

                private static object F = 1;


                public static string nameof(object param) => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 8, 13)]
                public static string Interceptor1(object param)
                {
                    Console.Write(1);
                    return param.ToString();
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableDoubleUnderscoreReservedIdentifiers()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            static class Program
            {
                public static void Main()
                {
                    M1(__arglist(1, 2, 3));

                    int i = 0;
                    TypedReference tr = __makeref(i);
                    ref int ri = ref __refvalue(tr, int);
                    Type t = __reftype(tr);
                }

                static void M1(__arglist) { }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 8, 12)] // __arglist
                [InterceptsLocation("Program.cs", 11, 29)] // __makeref
                [InterceptsLocation("Program.cs", 12, 26)] // __refvalue
                [InterceptsLocation("Program.cs", 13, 18)] // __reftype
                public static void Interceptor1(int x, int y, int z) { }
            }
            """;
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        compilation.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '__arglist'.
            //     [InterceptsLocation("Program.cs", 8, 12)] // __arglist
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 8, 12)").WithArguments("__arglist").WithLocation(21, 6),
            // Program.cs(22,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '__makeref'.
            //     [InterceptsLocation("Program.cs", 11, 29)] // __makeref
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 11, 29)").WithArguments("__makeref").WithLocation(22, 6),
            // Program.cs(23,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '__refvalue'.
            //     [InterceptsLocation("Program.cs", 12, 26)] // __refvalue
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 12, 26)").WithArguments("__refvalue").WithLocation(23, 6),
            // Program.cs(24,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '__reftype'.
            //     [InterceptsLocation("Program.cs", 13, 18)] // __reftype
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 13, 18)").WithArguments("__reftype").WithLocation(24, 6)
            );
    }

    [Fact]
    public void InterceptableDelegateInvocation_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M(() => Console.Write(1));
            C.M1((() => Console.Write(1), 0));

            static class C
            {
                public static void M(Action action)
                {
                    action();
                }

                public static void M1((Action action, int) pair)
                {
                    pair.action();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 11, 9)]
                [InterceptsLocation("Program.cs", 16, 14)]
                public static void Interceptor1(this Action action) { action(); Console.Write(2); }
            }
            """;
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        compilation.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS9207: Cannot intercept 'action' because it is not an invocation of an ordinary member method.
            //     [InterceptsLocation("Program.cs", 11, 9)]
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("action").WithLocation(22, 6),
            // Program.cs(23,6): error CS9207: Cannot intercept 'action' because it is not an invocation of an ordinary member method.
            //     [InterceptsLocation("Program.cs", 16, 14)]
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 16, 14)").WithArguments("action").WithLocation(23, 6));
    }

    [Fact]
    public void InterceptableDelegateInvocation_02()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M(() => Console.Write(1));
            C.M1((() => Console.Write(1), 0));

            static class C
            {
                public static void M(Action action)
                {
                    action!();
                }

                public static void M1((Action action, int) pair)
                {
                    pair.action!();
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 11, 9)]
                [InterceptsLocation("Program.cs", 16, 14)]
                public static void Interceptor1(this Action action) { action(); Console.Write(2); }
            }
            """;
        var compilation = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        compilation.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS9151: Possible method name 'action' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 11, 9)]
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("action").WithLocation(22, 6),
            // Program.cs(23,6): error CS9151: Possible method name 'action' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 16, 14)]
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 16, 14)").WithArguments("action").WithLocation(23, 6));
    }

    [Fact]
    public void QualifiedNameAtCallSite()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {

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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
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

                public static void InterceptableMethod(this C c) => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public static void Interceptor1(C c) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(14,6): error CS9148: Interceptor must have a 'this' parameter matching parameter 'C c' on 'D.InterceptableMethod(C)'.
            //     [InterceptsLocation("Program.cs", 5, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, @"InterceptsLocation(""Program.cs"", 5, 3)").WithArguments("C c", "D.InterceptableMethod(C)").WithLocation(14, 6));
    }

    [Fact]
    public void InterceptableExtensionMethod_InterceptorStaticMethod_NormalForm()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            var c = new C();
            D.InterceptableMethod(c);

            class C { }

            static class D
            {

                public static void InterceptableMethod(this C c) => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public static void Interceptor1(C c) => Console.Write(1);
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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

                public static void InterceptableMethod(string param) { Console.Write("interceptable " + param); }

                [InterceptsLocation("Program.cs", 8, 11)]
                public void Interceptor1(string param) { Console.Write("interceptor " + param); }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS9149: Interceptor must not have a 'this' parameter because 'C.InterceptableMethod(string)' does not have a 'this' parameter.
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor Hello World");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1234");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void AttributeArgumentLabels_02()
    {
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {

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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyDiagnostics(
            // Program.cs(20,53): error CS9142: The given file has '22' lines, which is fewer than the provided line number '50'.
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call siteinterceptable call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableFromMetadata()
    {
        var source1 = """

            using System;

            public class C
            {

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

        var comp1 = CreateCompilation(new[] { (source1, "File1.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp1.VerifyEmitDiagnostics();

        var verifier = CompileAndVerify((source2, "Program.cs"), references: new[] { comp1.ToMetadataReference() }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor call site");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptsLocation_BadMethodKind()
    {
        var source = """
            using System.Runtime.CompilerServices;

            static class Program
            {

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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyDiagnostics(
            // Program.cs(13,23): error CS9146: An interceptor method must be an ordinary member method.
            //         var lambda = [InterceptsLocation("Program.cs", 13, 8)] (string param) => { }; // 1
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(13, 23),
            // Program.cs(15,10): error CS9146: An interceptor method must be an ordinary member method.
            //         [InterceptsLocation("Program.cs", 13, 8)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(15, 10),
            // Program.cs(21,10): error CS9146: An interceptor method must be an ordinary member method.
            //         [InterceptsLocation("Program.cs", 13, 8)] // 3
            Diagnostic(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 13, 8)").WithLocation(21, 10)
            );
    }

    [Fact]
    public void InterceptableMethod_BadMethodKind_01()
    {
        var source = """
            using System.Runtime.CompilerServices;

            class Program
            {
                public static unsafe void Main()
                {
                    // property
                    _ = Prop;

                    // constructor
                    new Program();
                }

                public static int Prop { get; }

                [InterceptsLocation("Program.cs", 8, 13)] // 1
                [InterceptsLocation("Program.cs", 11, 9)] // 2, 'new'
                [InterceptsLocation("Program.cs", 11, 13)] // 3, 'Program'
                static void Interceptor1() { }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: TestOptions.UnsafeDebugExe);
        comp.VerifyDiagnostics(
            // Program.cs(16,6): error CS9151: Possible method name 'Prop' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 8, 13)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 8, 13)").WithArguments("Prop").WithLocation(16, 6),
            // Program.cs(17,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token 'new'.
            //     [InterceptsLocation("Program.cs", 11, 9)] // 2, 'new'
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("new").WithLocation(17, 6),
            // Program.cs(18,6): error CS9151: Possible method name 'Program' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 11, 13)] // 3, 'Program'
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 11, 13)").WithArguments("Program").WithLocation(18, 6)
            );
    }

    [Fact]
    public void InterceptableMethod_BadMethodKind_02()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            class Program
            {
                public static unsafe void Main()
                {
                    // delegate
                    Action a = () => throw null!;
                    a();

                    // local function
                    void local() => throw null!;
                    local();

                    // fnptr invoke
                    delegate*<void> fnptr = &Interceptor1;
                    fnptr();
                }

                public static int Prop { get; }

                [InterceptsLocation("Program.cs", 10, 9)] // 1
                [InterceptsLocation("Program.cs", 14, 9)] // 2
                [InterceptsLocation("Program.cs", 18, 9)] // 3
                static void Interceptor1() { }
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: TestOptions.UnsafeDebugExe);
        comp.VerifyEmitDiagnostics(
            // Program.cs(23,6): error CS9207: Cannot intercept 'a' because it is not an invocation of an ordinary member method.
            //     [InterceptsLocation("Program.cs", 10, 9)] // 1
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 10, 9)").WithArguments("a").WithLocation(23, 6),
            // Program.cs(24,6): error CS9207: Cannot intercept 'local' because it is not an invocation of an ordinary member method.
            //     [InterceptsLocation("Program.cs", 14, 9)] // 2
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 14, 9)").WithArguments("local").WithLocation(24, 6),
            // Program.cs(25,6): error CS9207: Cannot intercept 'fnptr' because it is not an invocation of an ordinary member method.
            //     [InterceptsLocation("Program.cs", 18, 9)] // 3
            Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, @"InterceptsLocation(""Program.cs"", 18, 9)").WithArguments("fnptr").WithLocation(25, 6)
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS9138: Method 'D.Interceptor1<T>(I1, string)' must be non-generic to match 'C.InterceptableMethod(string)'.
            //     [InterceptsLocation("Program.cs", 16, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 16, 11)").WithArguments("D.Interceptor1<T>(I1, string)", "C.InterceptableMethod(string)").WithLocation(22, 6));
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9138: Method 'D<T>.Interceptor1(string)' cannot be used as an interceptor because its containing type has type parameters.
            //     [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorContainingTypeCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("D<T>.Interceptor1(string)").WithLocation(21, 6));
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(23,10): error CS9138: Method 'Outer<T>.D.Interceptor1(string)' cannot be used as an interceptor because its containing type has type parameters.
            //         [InterceptsLocation("Program.cs", 15, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorContainingTypeCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("Outer<T>.D.Interceptor1(string)").WithLocation(23, 10)
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "12");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '<'.
            //     [InterceptsLocation("Program.cs", 14, 30)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 14, 30)").WithArguments("<").WithLocation(20, 6),
            // Program.cs(21,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token 'string'.
            //     [InterceptsLocation("Program.cs", 14, 31)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 14, 31)").WithArguments("string").WithLocation(21, 6),
            // Program.cs(22,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '>'.
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_04()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {

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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS9144: Cannot intercept method 'C.InterceptableMethod<T2>(T2)' with interceptor 'D.Interceptor1(object)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 14, 11)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 14, 11)").WithArguments("C.InterceptableMethod<T2>(T2)", "D.Interceptor1(object)").WithLocation(22, 6),
            // Program.cs(23,6): error CS9144: Cannot intercept method 'C.InterceptableMethod<T2>(T2)' with interceptor 'D.Interceptor1(object)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 15, 11)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 15, 11)").WithArguments("C.InterceptableMethod<T2>(T2)", "D.Interceptor1(object)").WithLocation(23, 6)
            );
    }

    [Fact]
    public void InterceptableGeneric_05()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.Usage(1);
            C.Usage(2);

            class C
            {
                public static void InterceptableMethod<T1>(T1 t) => throw null!;

                public static void Usage<T2>(T2 t)
                {
                    C.InterceptableMethod(t);
                    C.InterceptableMethod<T2>(t);
                    C.InterceptableMethod<object>(t);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 13, 11)]
                [InterceptsLocation("Program.cs", 14, 11)]
                [InterceptsLocation("Program.cs", 15, 11)]
                public static void Interceptor1<T>(T t) { Console.Write(t); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "111222");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_06()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void InterceptableMethod<T1>(T1 t) => throw null!;

                public static void Usage()
                {
                    C.InterceptableMethod("abc");
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 10, 11)] // 1
                public static void Interceptor1<T>(T t) where T : struct => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(16,6): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'D.Interceptor1<T>(T)'
            //     [InterceptsLocation("Program.cs", 10, 11)] // 1
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, @"InterceptsLocation(""Program.cs"", 10, 11)").WithArguments("D.Interceptor1<T>(T)", "T", "string").WithLocation(16, 6));
    }

    [Fact]
    public void InterceptableGeneric_07()
    {
        // original containing type is generic
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D.Usage(1);
            D.Usage(2);

            class C<T1>
            {
                public static void InterceptableMethod(T1 t) => throw null!;
            }

            static class D
            {
                public static void Usage<T2>(T2 t)
                {
                    C<T2>.InterceptableMethod(t);
                    C<object>.InterceptableMethod(t);
                }

                [InterceptsLocation("Program.cs", 16, 15)]
                [InterceptsLocation("Program.cs", 17, 19)]
                public static void Interceptor1<T>(T t) { Console.Write(t); }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1122");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_09()
    {
        // original containing type and method are generic
        // interceptor has arity 2
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D.Usage(1, "a");
            D.Usage(2, "b");

            class C<T1>
            {
                public static void InterceptableMethod<T2>(T1 t1, T2 t2) => throw null!;
            }

            static class D
            {
                public static void Usage<T1, T2>(T1 t1, T2 t2)
                {
                    C<T1>.InterceptableMethod(t1, t2);
                    C<object>.InterceptableMethod<object>(t1, t2);
                }

                [InterceptsLocation("Program.cs", 16, 15)]
                [InterceptsLocation("Program.cs", 17, 19)]
                public static void Interceptor1<T1, T2>(T1 t1, T2 t2)
                {
                    Console.Write(t1);
                    Console.Write(t2);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1a1a2b2b");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_10()
    {
        // original containing type and method are generic
        // interceptor has arity 1

        // Note: the behavior in this scenario might push us toward using a "unification" model for generic interceptors.
        // All the cases supported in our current design would also be supported by unification, so we should be able to add it later.
        var source = """
            using System.Runtime.CompilerServices;

            class C<T1>
            {
                public static void InterceptableMethod<T2>(T1 t1, T2 t2) => throw null!;
            }

            static class D
            {
                public static void Usage<T>(object obj, T t)
                {
                    C<object>.InterceptableMethod(obj, t);
                }

                [InterceptsLocation("Program.cs", 12, 19)] // 1
                public static void Interceptor1<T>(object obj, T t) => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(15,6): error CS9177: Method 'D.Interceptor1<T>(object, T)' must be non-generic or have arity 2 to match 'C<object>.InterceptableMethod<T>(object, T)'.
            //     [InterceptsLocation("Program.cs", 12, 19)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorArityNotCompatible, @"InterceptsLocation(""Program.cs"", 12, 19)").WithArguments("D.Interceptor1<T>(object, T)", "2", "C<object>.InterceptableMethod<T>(object, T)").WithLocation(15, 6));
    }

    [Fact]
    public void InterceptableGeneric_11()
    {
        // original containing type and method are generic
        // interceptor has arity 0
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C<T1>
            {
                public static void InterceptableMethod<T2>(T1 t1, T2 t2) => throw null!;
            }

            static class D
            {
                public static void Main()
                {
                    C<int>.InterceptableMethod(1, "a");
                    C<int>.InterceptableMethod<string>(2, "b");
                }

                [InterceptsLocation("Program.cs", 13, 16)]
                [InterceptsLocation("Program.cs", 14, 16)]
                public static void Interceptor1(int i, string s)
                {
                    Console.Write(i);
                    Console.Write(s);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1a2b");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_12()
    {
        // original grandparent type and method are generic
        // interceptor has arity 2
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D.Usage(1, "a");
            D.Usage(2, "b");

            class Outer<T1>
            {
                public class C
                {
                    public static void InterceptableMethod<T2>(T1 t1, T2 t2) => throw null!;
                }
            }

            static class D
            {
                public static void Usage<T1, T2>(T1 t1, T2 t2)
                {
                    Outer<T1>.C.InterceptableMethod(t1, t2);
                    Outer<object>.C.InterceptableMethod<object>(t1, t2);
                }

                [InterceptsLocation("Program.cs", 19, 21)]
                [InterceptsLocation("Program.cs", 20, 25)]
                public static void Interceptor1<T1, T2>(T1 t1, T2 t2)
                {
                    Console.Write(t1);
                    Console.Write(t2);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1a1a2b2b");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_13()
    {
        // original grandparent type, containing type, and method are generic
        // interceptor has arity 3
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D.Usage(1, 2, 3);
            D.Usage(4, 5, 6);

            class Outer<T1>
            {
                public class C<T2>
                {
                    public static void InterceptableMethod<T3>(T1 t1, T2 t2, T3 t3) => throw null!;
                }
            }

            static class D
            {
                public static void Usage<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
                {
                    Outer<T1>.C<T2>.InterceptableMethod(t1, t2, t3);
                    Outer<object>.C<object>.InterceptableMethod<object>(t1, t2, t3);
                }

                [InterceptsLocation("Program.cs", 19, 25)]
                [InterceptsLocation("Program.cs", 20, 33)]
                public static void Interceptor1<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
                {
                    Console.Write(t1);
                    Console.Write(t2);
                    Console.Write(t3);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "123123456456");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_14()
    {
        // containing type has 2 type parameters, method is generic
        // interceptor has arity 3
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            D.Usage(1, 2, 3);
            D.Usage(4, 5, 6);

            class C<T1, T2>
            {
                public static void InterceptableMethod<T3>(T1 t1, T2 t2, T3 t3) => throw null!;
            }

            static class D
            {
                public static void Usage<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
                {
                    C<T1, T2>.InterceptableMethod(t1, t2, t3);
                    C<object, object>.InterceptableMethod<object>(t1, t2, t3);
                }

                [InterceptsLocation("Program.cs", 16, 19)]
                [InterceptsLocation("Program.cs", 17, 27)]
                public static void Interceptor1<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
                {
                    Console.Write(t1);
                    Console.Write(t2);
                    Console.Write(t3);
                }
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "123123456456");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptableGeneric_15()
    {
        // original method is non-generic, interceptor is generic
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.Original();

            class C
            {
                public static void Original() => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 4, 3)] // 1
                public static void Interceptor1<T>() => throw null!;
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(13,6): error CS9178: Method 'D.Interceptor1<T>()' must be non-generic to match 'C.Original()'.
            //     [InterceptsLocation("Program.cs", 4, 3)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorCannotBeGeneric, @"InterceptsLocation(""Program.cs"", 4, 3)").WithArguments("D.Interceptor1<T>()", "C.Original()").WithLocation(13, 6));
    }

    [Fact]
    public void InterceptableGeneric_16()
    {
        var source = """
            #nullable enable

            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void InterceptableMethod<T1>(T1 t) => throw null!;

                public static void Main()
                {
                    C.InterceptableMethod<string?>(null);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 12, 11)] // 1
                public static void Interceptor1<T>(T t) where T : notnull => Console.Write(1);
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics(
            // Program.cs(18,6): warning CS8714: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'D.Interceptor1<T>(T)'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
            //     [InterceptsLocation("Program.cs", 12, 11)] // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, @"InterceptsLocation(""Program.cs"", 12, 11)").WithArguments("D.Interceptor1<T>(T)", "T", "string?").WithLocation(18, 6));
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,25): error CS9139: Cannot intercept: compilation does not contain a file with path 'BAD'.
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
        var comp = CreateCompilation(new[] { (source, "/Users/me/projects/Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // /Users/me/projects/Program.cs(21,25): error CS9140: Cannot intercept: compilation does not contain a file with path 'projects/Program.cs'. Did you mean to use path '/Users/me/projects/Program.cs'?
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,25): error CS9150: Interceptor cannot have a 'null' file path.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,25): error CS9139: Cannot intercept: compilation does not contain a file with path 'program.cs'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '}'.
            //     [InterceptsLocation("Program.cs", 25, 1)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 25, 1)").WithArguments("}").WithLocation(21, 6),
            // Program.cs(22,39): error CS9142: The given file has '25' lines, which is fewer than the provided line number '26'.
            //     [InterceptsLocation("Program.cs", 26, 1)]
            Diagnostic(ErrorCode.ERR_InterceptorLineOutOfRange, "26").WithArguments("25", "26").WithLocation(22, 39),
            // Program.cs(23,39): error CS9142: The given file has '25' lines, which is fewer than the provided line number '100'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9141: The provided line and character number does not refer to an interceptable method name, but rather to token '}'.
            //     [InterceptsLocation("Program.cs", 16, 5)]
            Diagnostic(ErrorCode.ERR_InterceptorPositionBadToken, @"InterceptsLocation(""Program.cs"", 16, 5)").WithArguments("}").WithLocation(21, 6),
            // Program.cs(22,43): error CS9143: The given line is '5' characters long, which is fewer than the provided character number '6'.
            //     [InterceptsLocation("Program.cs", 16, 6)]
            Diagnostic(ErrorCode.ERR_InterceptorCharacterOutOfRange, "6").WithArguments("5", "6").WithLocation(22, 43),
            // Program.cs(23,43): error CS9143: The given line is '5' characters long, which is fewer than the provided character number '1000'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
                // Program.cs(21,6): error CS9141: The provided line and character number does not refer to an interceptable method, but rather to token 'c'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9147: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '15' and character '11'?
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


                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 12, 11)] // intercept spaces before 'InterceptableMethod' token
                [InterceptsLocation("Program.cs", 14, 33)] // intercept spaces after 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS9147: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '12' and character '13'?
            //     [InterceptsLocation("Program.cs", 12, 11)] // intercept spaces before 'InterceptableMethod' token
            Diagnostic(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, @"InterceptsLocation(""Program.cs"", 12, 11)").WithArguments("InterceptableMethod", "12", "13").WithLocation(20, 6),
            // Program.cs(21,6): error CS9147: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '14' and character '11'?
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


                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 11, 31)] // intercept comment after 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS9147: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '11' and character '11'?
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


                public static C InterceptableMethod(this C c, string param) { Console.Write("interceptable " + param); return c; }

                [InterceptsLocation("Program.cs", 12, 13)] // intercept comment above 'InterceptableMethod' token
                public static C Interceptor1(this C c, string param) { Console.Write("interceptor " + param); return c; }
            }

            static class CExt
            {
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
                // Program.cs(19,6): error CS9147: The provided line and character number does not refer to the start of token 'InterceptableMethod'. Did you mean to use line '13' and character '13'?
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

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,39): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", -1, 1)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(17, 39),
            // Program.cs(18,42): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 1, -1)] // 2
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(18, 42),
            // Program.cs(19,39): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", -1, -1)] // 3
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "-1").WithLocation(19, 39),
            // Program.cs(20,39): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 0, 1)] // 4
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "0").WithLocation(20, 39),
            // Program.cs(21,42): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
            //     [InterceptsLocation("Program.cs", 1, 0)] // 5 
            Diagnostic(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, "0").WithLocation(21, 42),
            // Program.cs(22,39): error CS9157: Line and character numbers provided to InterceptsLocationAttribute must be positive.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
                // Program.cs(21,6): error CS9144: Cannot intercept method 'Program.InterceptableMethod(I1, string)' with interceptor 'D.Interceptor1(I1, int)' because the signatures do not match.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(22,6): error CS9148: Interceptor must have a 'this' parameter matching parameter 'C this' on 'C.InterceptableMethod(string)'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9148: Interceptor must have a 'this' parameter matching parameter 'ref S this' on 'S.InterceptableMethod(string)'.
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: WithNullableEnable());
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

                public void Method1(string? param1) => throw null!;


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

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(24,6): warning CS9159: Nullability of reference types in type of parameter 'param2' doesn't match interceptable method 'C.Method1(string?)'.
            //     [InterceptsLocation("Program.cs", 17, 11)] // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor, @"InterceptsLocation(""Program.cs"", 17, 11)").WithArguments("param2", "C.Method1(string?)").WithLocation(24, 6),
            // Program.cs(27,6): warning CS9158: Nullability of reference types in return type doesn't match interceptable method 'C.Method2()'.
            //     [InterceptsLocation("Program.cs", 18, 15)] // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor, @"InterceptsLocation(""Program.cs"", 18, 15)").WithArguments("C.Method2()").WithLocation(27, 6)
            );

        comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: WithNullableDisable());
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

                public void Method1(object param1) => throw null!;


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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(24,6): warning CS9154: Intercepting a call to 'C.Method1(object)' with interceptor 'D.Interceptor1(C, dynamic)', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 17, 11)] // 1
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 17, 11)").WithArguments("C.Method1(object)", "D.Interceptor1(C, dynamic)").WithLocation(24, 6),
            // Program.cs(27,6): warning CS9154: Intercepting a call to 'C.Method2()' with interceptor 'D.Interceptor2(C)', but the signatures do not match.
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
                public void Method1((string a, string b) param1) => throw null!;
                public void Method2((string x, string y) param1) => throw null!;
                public void Method3((string, string) param1) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();

                    c.Method1(default!);
                    c.Method2(default!);
                    c.Method3(default!);

                    c.Method1(default!);
                    c.Method2(default!);
                    c.Method3(default!);

                    c.Method1(default!);
                    c.Method2(default!);
                    c.Method3(default!);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 17, 11)]
                [InterceptsLocation("Program.cs", 18, 11)] // 1
                [InterceptsLocation("Program.cs", 19, 11)] // 2
                public static void Interceptor1(this C s, (string a, string b) param2) => Console.Write(1);

                [InterceptsLocation("Program.cs", 21, 11)] // 3
                [InterceptsLocation("Program.cs", 22, 11)]
                [InterceptsLocation("Program.cs", 23, 11)] // 4
                public static void Interceptor2(this C s, (string x, string y) param2) => Console.Write(2);

                [InterceptsLocation("Program.cs", 25, 11)] // 5
                [InterceptsLocation("Program.cs", 26, 11)] // 6
                [InterceptsLocation("Program.cs", 27, 11)]
                public static void Interceptor3(this C s, (string, string) param2) => Console.Write(3);
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "111222333");
        verifier.VerifyDiagnostics(
            // Program.cs(34,6): warning CS9154: Intercepting a call to 'C.Method2((string x, string y))' with interceptor 'D.Interceptor1(C, (string a, string b))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 18, 11)] // 1
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 18, 11)").WithArguments("C.Method2((string x, string y))", "D.Interceptor1(C, (string a, string b))").WithLocation(34, 6),
            // Program.cs(35,6): warning CS9154: Intercepting a call to 'C.Method3((string, string))' with interceptor 'D.Interceptor1(C, (string a, string b))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 19, 11)] // 2
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 19, 11)").WithArguments("C.Method3((string, string))", "D.Interceptor1(C, (string a, string b))").WithLocation(35, 6),
            // Program.cs(38,6): warning CS9154: Intercepting a call to 'C.Method1((string a, string b))' with interceptor 'D.Interceptor2(C, (string x, string y))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 21, 11)] // 3
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 21, 11)").WithArguments("C.Method1((string a, string b))", "D.Interceptor2(C, (string x, string y))").WithLocation(38, 6),
            // Program.cs(40,6): warning CS9154: Intercepting a call to 'C.Method3((string, string))' with interceptor 'D.Interceptor2(C, (string x, string y))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 23, 11)] // 4
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 23, 11)").WithArguments("C.Method3((string, string))", "D.Interceptor2(C, (string x, string y))").WithLocation(40, 6),
            // Program.cs(43,6): warning CS9154: Intercepting a call to 'C.Method1((string a, string b))' with interceptor 'D.Interceptor3(C, (string, string))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 25, 11)] // 5
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 25, 11)").WithArguments("C.Method1((string a, string b))", "D.Interceptor3(C, (string, string))").WithLocation(43, 6),
            // Program.cs(44,6): warning CS9154: Intercepting a call to 'C.Method2((string x, string y))' with interceptor 'D.Interceptor3(C, (string, string))', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 26, 11)] // 6
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 26, 11)").WithArguments("C.Method2((string x, string y))", "D.Interceptor3(C, (string, string))").WithLocation(44, 6)
            );
    }

    [Fact]
    public void SignatureMismatch_08()
    {
        // nint/IntPtr difference
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {

                public void Method1(nint param1) => throw null!;
                public void Method2(IntPtr param1) => throw null!;
            }

            static class Program
            {
                public static void Main()
                {
                    var c = new C();
                    c.Method1(default!);
                    c.Method2(default!);

                    c.Method2(default!);
                    c.Method1(default!);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 16, 11)] // 1
                [InterceptsLocation("Program.cs", 17, 11)]
                public static void Interceptor1(this C s, IntPtr param2) => Console.Write(1);

                [InterceptsLocation("Program.cs", 19, 11)] // 2
                [InterceptsLocation("Program.cs", 20, 11)]
                public static void Interceptor2(this C s, nint param2) => Console.Write(2);
            }
            """;
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1122");
        verifier.VerifyDiagnostics(
            // Program.cs(26,6): warning CS9154: Intercepting a call to 'C.Method1(nint)' with interceptor 'D.Interceptor1(C, IntPtr)', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 16, 11)] // 1
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 16, 11)").WithArguments("C.Method1(nint)", "D.Interceptor1(C, System.IntPtr)").WithLocation(26, 6),
            // Program.cs(30,6): warning CS9154: Intercepting a call to 'C.Method2(IntPtr)' with interceptor 'D.Interceptor2(C, nint)', but the signatures do not match.
            //     [InterceptsLocation("Program.cs", 19, 11)] // 2
            Diagnostic(ErrorCode.WRN_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 19, 11)").WithArguments("C.Method2(System.IntPtr)", "D.Interceptor2(C, nint)").WithLocation(30, 6));
    }

    [Fact]
    public void SignatureMismatch_09()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            static class Program
            {
                public static void InterceptableMethod(ref readonly int x) => Console.Write("interceptable " + x);

                public static void Main()
                {
                    int x = 5;
                    InterceptableMethod(in x);
                }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 11, 9)]
                public static void Interceptor(in int x) => Console.Write("interceptor " + x);
            }
            """;
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(17,6): error CS9144: Cannot intercept method 'Program.InterceptableMethod(ref readonly int)' with interceptor 'D.Interceptor(in int)' because the signatures do not match.
            //     [InterceptsLocation("Program.cs", 11, 9)]
            Diagnostic(ErrorCode.ERR_InterceptorSignatureMismatch, @"InterceptsLocation(""Program.cs"", 11, 9)").WithArguments("Program.InterceptableMethod(ref readonly int)", "D.Interceptor(in int)").WithLocation(17, 6));
    }

    [Fact]
    public void ScopedMismatch_01()
    {
        // Unsafe 'scoped' difference
        var source = """
            using System.Runtime.CompilerServices;

            class C
            {

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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(20,6): error CS9156: Cannot intercept call to 'C.InterceptableMethod(scoped ref int)' with 'D.Interceptor1(ref int)' because of a difference in 'scoped' modifiers or '[UnscopedRef]' attributes.
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource, (UnscopedRefAttributeDefinition, "UnscopedRefAttribute.cs") }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource, (UnscopedRefAttributeDefinition, "UnscopedRefAttribute.cs") }, parseOptions: RegularWithInterceptors, options: WithNullableEnable());
        comp.VerifyEmitDiagnostics(
            // Program.cs(21,6): error CS9156: Cannot intercept call to 'C.InterceptableMethod(out int)' with 'D.Interceptor1(out int)' because of a difference in 'scoped' modifiers or '[UnscopedRef]' attributes.
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
        var verifier = CompileAndVerify(CreateEmptyCompilation((source, "Program.cs"), parseOptions: RegularWithInterceptors, options: WithNullableEnable()), verify: Verification.Skipped);
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
        var verifier = CompileAndVerify(CreateEmptyCompilation((source, "Program.cs"), parseOptions: RegularWithInterceptors, options: WithNullableEnable()), verify: Verification.Skipped);
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "123456");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "123456");
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
            parseOptions: RegularWithInterceptors,
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
            parseOptions: RegularWithInterceptors,
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
            parseOptions: RegularWithInterceptors,
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
        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "interceptor 1");
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
        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // OtherFile.cs(48,25): error CS9139: Cannot intercept: compilation does not contain a file with path 'OtherFile.cs'.
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

                public static void M() => throw null!;
            }

            class D
            {
                [Obsolete]
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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

                public static void M(int lineNumber = 1) => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1([CallerLineNumber] int lineNumber = 0) => Console.Write(lineNumber);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void DefaultArguments_01()
    {
        // Default parameter values on the interceptor doesn't affect the default arguments passed to an intercepted call.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M();

            class C
            {

                public static void M(int lineNumber = 1) => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1(int lineNumber = 0) => Console.Write(lineNumber);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void DefaultArguments_02()
    {
        // Interceptor cannot add a default argument when original method lacks it.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C.M(); // 1

            class C
            {
                public static void M(int lineNumber) => throw null!;
            }

            class D
            {
                [InterceptsLocation("Program.cs", 4, 3)]
                public static void M1(int lineNumber = 0) => Console.Write(lineNumber);
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(4,3): error CS7036: There is no argument given that corresponds to the required parameter 'lineNumber' of 'C.M(int)'
            // C.M(); // 1
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("lineNumber", "C.M(int)").WithLocation(4, 3));
    }

    [Fact]
    public void InterceptorExtern()
    {
        var source = """
            using System.Runtime.CompilerServices;

            C.M();

            class C
            {

                public static void M() => throw null!;
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 3, 3)]
                public static extern void Interceptor();
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, verify: Verification.Skipped);
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

                public void M() => throw null!;

                [InterceptsLocation("Program.cs", 5, 3)]
                public abstract void Interceptor();
            }

            class D : C
            {
                public override void Interceptor() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
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

        var verifier = CompileAndVerify(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InterceptGetEnumerator()
    {
        var source = """
            using System.Collections;
            using System.Runtime.CompilerServices;

            var myEnumerable = new MyEnumerable();
            foreach (var item in myEnumerable)
            {
            }

            class MyEnumerable : IEnumerable
            {
                public IEnumerator GetEnumerator() => throw null!;
            }

            static class MyEnumerableExt
            {
                [InterceptsLocation("Program.cs", 5, 22)] // 1
                public static IEnumerator GetEnumerator1(this MyEnumerable en) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(16,6): error CS9151: Possible method name 'myEnumerable' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 5, 22)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 5, 22)").WithArguments("myEnumerable").WithLocation(16, 6));
    }

    [Fact]
    public void InterceptDispose()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            var myDisposable = new MyDisposable();
            using (myDisposable)
            {
            }

            class MyDisposable : IDisposable
            {
                public void Dispose() => throw null!;
            }

            static class MyDisposeExt
            {
                [InterceptsLocation("Program.cs", 5, 8)] // 1
                public static void Dispose1(this MyDisposable md) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(16,6): error CS9151: Possible method name 'myDisposable' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 5, 8)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 5, 8)").WithArguments("myDisposable").WithLocation(16, 6)
            );
    }

    [Fact]
    public void InterceptDeconstruct()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            var myDeconstructable = new MyDeconstructable();
            var (x, y) = myDeconstructable;

            class MyDeconstructable
            {
                public void Deconstruct(out int x, out int y) => throw null!;
            }

            static class MyDeconstructableExt
            {
                [InterceptsLocation("Program.cs", 5, 14)] // 1
                public static void Deconstruct1(this MyDeconstructable md, out int x, out int y) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // Program.cs(14,6): error CS9151: Possible method name 'myDeconstructable' cannot be intercepted because it is not being invoked.
            //     [InterceptsLocation("Program.cs", 5, 14)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorNameNotInvoked, @"InterceptsLocation(""Program.cs"", 5, 14)").WithArguments("myDeconstructable").WithLocation(14, 6)
            );
    }

    [Fact]
    public void PathMapping_01()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation("/_/Program.cs", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathPrefix = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path\""" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, "/_/"));

        var verifier = CompileAndVerify(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)),
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void PathMapping_02()
    {
        // Attribute uses a physical path, but we expected a mapped path.
        // Diagnostic can suggest using the mapped path instead.
        var pathPrefix = PlatformInformation.IsWindows ? @"C:\My\Machine\Specific\Path\" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var source = $$"""
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation(@"{{path}}", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, "/_/"));

        var comp = CreateCompilation(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)));
        comp.VerifyEmitDiagnostics(
                // C:\My\Machine\Specific\Path\Program.cs(11,25): error CS9145: Cannot intercept: Path 'C:\My\Machine\Specific\Path\Program.cs' is unmapped. Expected mapped path '/_/Program.cs'.
                //     [InterceptsLocation(@"C:\My\Machine\Specific\Path\Program.cs", 5, 3)]
                Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithUnmappedCandidate, $@"@""{path}""").WithArguments(path, "/_/Program.cs").WithLocation(11, 25)
            );
    }

    [Fact]
    public void PathMapping_03()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation(@"\_\Program.cs", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathPrefix = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path\""" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, "/_/"));

        var comp = CreateCompilation(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)));
        comp.VerifyEmitDiagnostics(
            // C:\My\Machine\Specific\Path\Program.cs(11,25): error CS9140: Cannot intercept: compilation does not contain a file with path '\_\Program.cs'. Did you mean to use path '/_/Program.cs'?
            //     [InterceptsLocation(@"\_\Program.cs", 5, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"@""\_\Program.cs""").WithArguments(@"\_\Program.cs", "/_/Program.cs").WithLocation(11, 25));
    }

    [Fact]
    public void PathMapping_04()
    {
        // Test when unmapped file paths are distinct, but mapped paths are equal.
        var source1 = """
            using System.Runtime.CompilerServices;
            using System;

            namespace NS1;

            class C
            {
                public static void M0()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;

                [InterceptsLocation(@"/_/Program.cs", 11, 9)]
                public void Interceptor() => Console.Write(1);
            }
            """;

        var source2 = """
            using System.Runtime.CompilerServices;
            using System;

            namespace NS2;

            class C
            {
                public static void M0()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;
            }
            """;

        var pathPrefix1 = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path1\""" : "/My/Machine/Specific/Path1/";
        var pathPrefix2 = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path2\""" : "/My/Machine/Specific/Path2/";
        var path1 = pathPrefix1 + "Program.cs";
        var path2 = pathPrefix2 + "Program.cs";
        var pathMap = ImmutableArray.Create(
            new KeyValuePair<string, string>(pathPrefix1, "/_/"),
            new KeyValuePair<string, string>(pathPrefix2, "/_/")
            );

        var comp = CreateCompilation(
            new[] { (source1, path1), (source2, path2), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugDll.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)));
        comp.VerifyEmitDiagnostics(
            // C:\My\Machine\Specific\Path1\Program.cs(16,25): error CS9152: Cannot intercept a call in file with path '/_/Program.cs' because multiple files in the compilation have this path.
            //     [InterceptsLocation(@"/_/Program.cs", 11, 9)]
            Diagnostic(ErrorCode.ERR_InterceptorNonUniquePath, @"@""/_/Program.cs""").WithArguments("/_/Program.cs").WithLocation(16, 25));
    }

    [Fact]
    public void PathMapping_05()
    {
        // Pathmap replacement contains backslashes, and attribute path contains backslashes.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation(@"\_\Program.cs", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathPrefix = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path\""" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, @"\_\"));

        var verifier = CompileAndVerify(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)),
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void PathMapping_06()
    {
        // Pathmap mixes slashes and backslashes, attribute path is normalized to slashes
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation(@"/_/Program.cs", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathPrefix = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path\""" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, @"\_/"));

        var comp = CreateCompilation(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)));
        comp.VerifyEmitDiagnostics(
            // C:\My\Machine\Specific\Path\Program.cs(11,25): error CS9140: Cannot intercept: compilation does not contain a file with path '/_/Program.cs'. Did you mean to use path '\_/Program.cs'?
            //     [InterceptsLocation(@"/_/Program.cs", 5, 3)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"@""/_/Program.cs""").WithArguments("/_/Program.cs", @"\_/Program.cs").WithLocation(11, 25));
    }

    [Fact]
    public void PathMapping_07()
    {
        // Pathmap replacement mixes slashes and backslashes, attribute path matches it
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            C c = new C();
            c.M();

            class C
            {
                public void M() => throw null!;

                [InterceptsLocation(@"\_/Program.cs", 5, 3)]
                public void Interceptor() => Console.Write(1);
            }
            """;
        var pathPrefix = PlatformInformation.IsWindows ? """C:\My\Machine\Specific\Path\""" : "/My/Machine/Specific/Path/";
        var path = pathPrefix + "Program.cs";
        var pathMap = ImmutableArray.Create(new KeyValuePair<string, string>(pathPrefix, @"\_/"));

        var verifier = CompileAndVerify(
            new[] { (source, path), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            options: TestOptions.DebugExe.WithSourceReferenceResolver(
                new SourceFileResolver(ImmutableArray<string>.Empty, null, pathMap)),
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void PathNormalization_01()
    {
        // No pathmap is present and slashes in the attribute match the FilePath on the syntax tree.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void Main()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;

                [InterceptsLocation("src/Program.cs", 9, 11)]
                public void Interceptor() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(
            new[] { (source, "src/Program.cs"), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void PathNormalization_02()
    {
        // No pathmap is present and backslashes in the attribute match the FilePath on the syntax tree.
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void Main()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;

                [InterceptsLocation(@"src\Program.cs", 9, 11)]
                public void Interceptor() => Console.Write(1);
            }
            """;

        var verifier = CompileAndVerify(
            new[] { (source, @"src\Program.cs"), s_attributesSource },
            parseOptions: RegularWithInterceptors,
            expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void PathNormalization_03()
    {
        // Relative paths do not have slashes normalized when pathmap is not present
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void Main()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;

                [InterceptsLocation(@"src/Program.cs", 9, 11)]
                public void Interceptor() => Console.Write(1);
            }
            """;

        var comp = CreateCompilation(new[] { (source, @"src\Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // src\Program.cs(14,25): error CS9140: Cannot intercept: compilation does not contain a file with path 'src/Program.cs'. Did you mean to use path 'src\Program.cs'?
            //     [InterceptsLocation(@"src/Program.cs", 9, 11)]
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"@""src/Program.cs""").WithArguments("src/Program.cs", @"src\Program.cs").WithLocation(14, 25));
    }

    [Fact]
    public void PathNormalization_04()
    {
        // Absolute paths do not have slashes normalized when no pathmap is present
        // Note that any such normalization step would be specific to Windows
        var source = """
            using System.Runtime.CompilerServices;
            using System;

            class C
            {
                public static void Main()
                {
                    C c = new C();
                    c.M();
                }

                public void M() => throw null!;

                [InterceptsLocation("C:/src/Program.cs", 9, 11)] // 1
                public void Interceptor() => Console.Write(1);
            }
            """;

        var comp = CreateCompilation(new[] { (source, @"C:\src\Program.cs"), s_attributesSource }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
            // C:\src\Program.cs(14,25): error CS9140: Cannot intercept: compilation does not contain a file with path 'C:/src/Program.cs'. Did you mean to use path 'C:\src\Program.cs'?
            //     [InterceptsLocation("C:/src/Program.cs", 9, 11)] // 1
            Diagnostic(ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate, @"""C:/src/Program.cs""").WithArguments("C:/src/Program.cs", @"C:\src\Program.cs").WithLocation(14, 25));
    }

    [Fact]
    public void InterceptorUnmanagedCallersOnly()
    {
        var source = """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System;

            C.Interceptable();

            class C
            {
                public static void Interceptable() { }
            }

            static class D
            {
                [InterceptsLocation("Program.cs", 5, 3)]
                [UnmanagedCallersOnly]
                public static void Interceptor() { }
            }
            """;

        var comp = CreateCompilation(new[] { (source, "Program.cs"), s_attributesSource, (UnmanagedCallersOnlyAttributeDefinition, "UnmanagedCallersOnlyAttribute.cs") }, parseOptions: RegularWithInterceptors);
        comp.VerifyEmitDiagnostics(
                // Program.cs(14,6): error CS9161: An interceptor cannot be marked with 'UnmanagedCallersOnlyAttribute'.
                //     [InterceptsLocation("Program.cs", 5, 3)]
                Diagnostic(ErrorCode.ERR_InterceptorCannotUseUnmanagedCallersOnly, @"InterceptsLocation(""Program.cs"", 5, 3)").WithLocation(14, 6));
    }
}
