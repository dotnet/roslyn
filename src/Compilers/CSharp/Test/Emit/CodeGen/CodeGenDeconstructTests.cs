// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class CodeGenDeconstructTests : CSharpTestBase
    {
        [Fact]
        public void Deconstruct()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructMethodMissing()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS8206: No Deconstruct instance or extension method was found for type 'C'.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructMethodAmbiguous()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }

    public void Deconstruct(out int a)
    {
        a = 1;
    }
}";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS8207: More than one Deconstruct instance or extension method was found for type 'C'.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_AmbiguousDeconstruct, "new C()").WithArguments("C").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructWrongParams()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        (x, y) = new C();
    }
    public void Deconstruct(out int a)
    {
        a = 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS8209: The Deconstruct method for type 'C.Deconstruct(out int)' doesn't have the number of parameters (2) needed for this deconstruction.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_DeconstructWrongParams, "new C()").WithArguments("C.Deconstruct(out int)", "2").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructWithLeftHandSideErrors()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x = 1;
        string y = ""hello"";
        (x.f, y.g) = new C();
    }
    public void Deconstruct() { }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,12): error CS1061: 'long' does not contain a definition for 'f' and no extension method 'f' accepting a first argument of type 'long' could be found (are you missing a using directive or an assembly reference?)
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "f").WithArguments("long", "f").WithLocation(8, 12),
                // (8,17): error CS1061: 'string' does not contain a definition for 'g' and no extension method 'g' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "g").WithArguments("string", "g").WithLocation(8, 17)
                );
        }

        [Fact]
        public void DeconstructWithInParam()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        int y;
        (x, y) = new C();
    }
    public void Deconstruct(out int x, int y) { x = 1; }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS8208: The Deconstruct method for type 'C.Deconstruct(out int, int)' must have only out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_DeconstructRequiresOutParams, "new C()").WithArguments("C.Deconstruct(out int, int)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructWithRefParam()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        int y;
        (x, y) = new C();
    }
    public void Deconstruct(ref int x, out int y) { x = 1; y = 2; }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS8208: The Deconstruct method for type 'C.Deconstruct(ref int, out int)' must have only out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_DeconstructRequiresOutParams, "new C()").WithArguments("C.Deconstruct(ref int, out int)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructCanHaveReturnType()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public int Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
        return 42;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructDataFlow()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        (x, y) = new C(); // x and y are assigned here, so no complaints on usage of un-initialized locals on the line below
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyExecutionOrder()
        {
            string source = @"
using System;
class C
{
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }

    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getDeconstructReceiver() { Console.WriteLine(""getDeconstructReceiver""); return this; }

    static void Main()
    {
        C c = new C();
        int z; // PROTOTYPE(tuples) this should be removed once the return-type issue is fixed
        (c.getHolderForX().x, c.getHolderForY().y, z) = c.getDeconstructReceiver();
    }
    public void Deconstruct(out D1 x, out D2 y, out int z) { x = new D1(); y = new D2(); z = 3; Console.WriteLine(""Deconstruct""); }
}
class D1
{
    public static implicit operator int(D1 d) { Console.WriteLine(""Conversion1""); return 1; }
}
class D2
{
    public static implicit operator int(D2 d) { Console.WriteLine(""Conversion2""); return 2; }
}
";

            string expected =
@"getHolderforX
getHolderforY
getDeconstructReceiver
Deconstruct
Conversion1
setX
Conversion2
setY
";
            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentVariableKinds()
        {
            string source = @"
class C
{
    int[] ArrayIndexer = new int[1];

    string property;
    string Property { set { property = value; } }

    string AutoProperty { get; set; }

    static void Main()
    {
        C c = new C();
        (c.ArrayIndexer[0], c.Property, c.AutoProperty) = new C();
        System.Console.WriteLine(c.ArrayIndexer[0] + "" "" + c.property + "" "" + c.AutoProperty);
    }

    public void Deconstruct(out int a, out string b, out string c)
    {
        a = 1;
        b = ""hello"";
        c = ""world"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void Dynamic()
        {
            string source = @"
class C
{
    dynamic Dynamic1;
    dynamic Dynamic2;

    static void Main()
    {
        C c = new C();
        (c.Dynamic1, c.Dynamic2) = new C();
        System.Console.WriteLine(c.Dynamic1 + "" "" + c.Dyanmic2);
    }

    public void Deconstruct(out int a, out dynamic b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentStaticVariableKinds()
        {
            string source = @"
class C
{
    static int[] ArrayIndexer = new int[1];

    static string property;
    static string Property { set { property = value; } }

    static string AutoProperty { get; set; }

    static void Main()
    {
        (C.ArrayIndexer[0], C.Property, C.AutoProperty) = new C();
        System.Console.WriteLine(C.ArrayIndexer[0] + "" "" + C.property + "" "" + C.AutoProperty);
    }

    public void Deconstruct(out int a, out string b, out string c)
    {
        a = 1;
        b = ""hello"";
        c = ""world"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello world", additionalRefs: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void DifferentVariableRefKinds()
        {
            string source = @"
class C
{
    static void Main()
    {
        long a = 1;
        int b;
        C.M(ref a, out b);
        System.Console.WriteLine(a + "" "" + b);
    }

    static void M(ref long a, out int b)
    {
        (a, b) = new C();
    }

    public void Deconstruct(out int x, out byte y)
    {
        x = 2;
        y = (byte)3;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "2 3", additionalRefs: new[] { SystemCoreRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConversionErrors()
        {
            string source = @"
class C
{
    static void Main()
    {
        byte x;
        string y;
        (x, y) = new C();
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new C()").WithArguments("int", "byte").WithLocation(8, 18),
                // (8,18): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C()").WithArguments("int", "string").WithLocation(8, 18)
                );
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void Nesting()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z;
        (x, (y, z)) = new C();
    }

    public void Deconstruct(out int a, out D d)
    {
        a = 1;
        d = new D();
    }
}
class D
{
    public void Deconstruct(out string b, out string c)
    {
        b = ""hello"";
        c = ""world"";
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            // expect a console output
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void ExpressionType()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        var type = ((x, y) = new C()).GetType();
        System.Console.WriteLine(type);
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(); // expect an error
        }

        [Fact]
        public void LambdaStillNotValidStatement()
        {
            string source = @"
class C
{
    static void Main()
    {
        (a) => a;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (a) => a;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(a) => a").WithLocation(6, 9)
                );
        }

        [Fact]
        public void LambdaWithBodyStillNotValidStatement()
        {
            string source = @"
class C
{
    static void Main()
    {
        (a, b) => { };
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (a, b) => { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(a, b) => { }").WithLocation(6, 9)
                );
        }

        [Fact]
        public void CastButNotCast()
        {
            // int and string must be types, so (int, string) must be type and ((int, string)) a cast, but then .String() cannot follow a cast...
            string source = @"
class C
{
    static void Main()
    {
        ((int, string)).ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (6,24): error CS1525: Invalid expression term '.'
                //         ((int, string)).ToString();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(6, 24)
                );
        }
    }
}