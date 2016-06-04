// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class CodeGenDeconstructTests : CSharpTestBase
    {
        const string commonSource =
@"public class Pair<T1, T2>
{
    T1 item1;
    T2 item2;

    public Pair(T1 item1, T2 item2)
    {
        this.item1 = item1;
        this.item2 = item2;
    }

    public void Deconstruct(out T1 item1, out T2 item2)
    {
        System.Console.WriteLine($""Deconstructing {ToString()}"");
        item1 = this.item1;
        item2 = this.item2;
    }

    public override string ToString() { return $""({item1.ToString()}, {item2.ToString()})""; }
}

public static class Pair
{
    public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return new Pair<T1, T2>(item1, item2); }
}

public class Integer
{
    public int state;
    public override string ToString() { return state.ToString(); }
    public Integer(int i) { state = i; }
    public static implicit operator LongInteger(Integer i) { System.Console.WriteLine($""Converting {i}""); return new LongInteger(i.state); }
}

public class LongInteger
{
    long state;
    public LongInteger(long l) { state = l; }
    public override string ToString() { return state.ToString(); }
}";

        [Fact]
        public void SimpleAssign()
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
            comp.VerifyIL("C.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  3
  .locals init (string V_0, //y
                int V_1,
                string V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_1
  IL_0007:  ldloca.s   V_2
  IL_0009:  call       ""void C.Deconstruct(out int, out string)""
  IL_000e:  ldloc.1
  IL_000f:  conv.i8
  IL_0010:  ldloc.2
  IL_0011:  stloc.0
  IL_0012:  box        ""long""
  IL_0017:  ldstr      "" ""
  IL_001c:  ldloc.0
  IL_001d:  call       ""string string.Concat(object, object, object)""
  IL_0022:  call       ""void System.Console.WriteLine(string)""
  IL_0027:  ret
}");
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
        public void AssignmentWithLeftHandSideErrors()
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
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "g").WithArguments("string", "g").WithLocation(8, 17),
                // (8,22): error CS8209: The Deconstruct method for type 'C.Deconstruct()' doesn't have the number of parameters (2) needed for this deconstruction.
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_DeconstructWrongParams, "new C()").WithArguments("C.Deconstruct()", "2").WithLocation(8, 22)
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
        public void AssignmentDataFlow()
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
        (c.getHolderForX().x, c.getHolderForY().y) = c.getDeconstructReceiver();
    }
    public void Deconstruct(out D1 x, out D2 y) { x = new D1(); y = new D2(); Console.WriteLine(""Deconstruct""); }
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

        [Fact]
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
        (c.Dynamic1, c.Dynamic2) = c;
        System.Console.WriteLine(c.Dynamic1 + "" "" + c.Dynamic2);
    }

    public void Deconstruct(out int a, out dynamic b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { SystemCoreRef, CSharpRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
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

        [Fact]
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
                // (8,9): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(x, y) = new C()").WithArguments("int", "byte").WithLocation(8, 9),
                // (8,9): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(x, y) = new C()").WithArguments("int", "string").WithLocation(8, 9)
                );
        }

        [Fact]
        public void ExpressionType()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        var type = ((x, y) = new C()).GetType();
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (7,38): error CS0023: Operator '.' cannot be applied to operand of type 'void'
                //         var type = ((x, y) = new C()).GetType();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "void").WithLocation(7, 38)
                );
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

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningMethod()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        (M(), M()) = new C();
        System.Console.WriteLine($""Final i is {i}"");
    }

    static ref int M()
    {
        System.Console.WriteLine($""M (previous i is {i})"");
        return ref i;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }
}
";
            var expected =
@"M (previous i is 0)
M (previous i is 0)
Deconstruct
Final i is 43
";

            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningProperty()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        (P, P) = new C();
        System.Console.WriteLine($""Final i is {i}"");
    }

    static ref int P
    {
        get
        {
            System.Console.WriteLine($""P (previous i is {i})"");
            return ref i;
        }
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }
}
";
            var expected =
@"P (previous i is 0)
P (previous i is 0)
Deconstruct
Final i is 43
";

            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningMethod2()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        (M(), M()) = new C();
    }

    static ref int M()
    {
        System.Console.WriteLine(""M"");
        return ref i;
    }

    void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics();

            // This error is wrong
            // (4,16): warning CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
            //     static int i;
            //Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void RefReturningMethodFlow()
        {
            string source = @"
struct C
{
    static C i;
    static C P { get { System.Console.WriteLine(""getP""); return i; } set { System.Console.WriteLine(""setP""); i = value; } }

    static void Main()
    {
        (M(), M()) = P;
    }

    static ref C M()
    {
        System.Console.WriteLine($""M (previous i is {i})"");
        return ref i;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 42;
        y = 43;
    }

    public static implicit operator C(int x)
    {
        System.Console.WriteLine(""conversion"");
        return new C();
    }
}
";

            var expected =
@"M (previous i is C)
M (previous i is C)
getP
Deconstruct
conversion
conversion";

            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UninitializedRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        (x, x) = x;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (7,18): error CS8206: No Deconstruct instance or extension method was found for type 'int'.
                //         (x, x) = x;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "x").WithArguments("int").WithLocation(7, 18),
                // (7,18): error CS0165: Use of unassigned local variable 'x'
                //         (x, x) = x;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(7, 18)
                );
        }

        [Fact]
        public void Indexers()
        {
            string source = @"
class C
{
    static SomeArray array;

    static void Main()
    {
        int y;
        (Foo()[Bar()], y) = new C();
        System.Console.WriteLine($""Final array values[2] {array.values[2]}"");
    }

    static SomeArray Foo()
    {
        System.Console.WriteLine($""Foo"");
        array = new SomeArray();
        return array;
    }

    static int Bar()
    {
        System.Console.WriteLine($""Bar"");
        return 2;
    }

    void Deconstruct(out int x, out int y)
    {
        System.Console.WriteLine(""Deconstruct"");
        x = 101;
        y = 102;
    }
}
class SomeArray
{
    public int[] values;
    public SomeArray() { values = new [] { 42, 43, 44 }; }
    public int this[int index] {
        get { System.Console.WriteLine($""indexGet (with value {values[index]})""); return values[index]; }
        set { System.Console.WriteLine($""indexSet (with value {value})""); values[index] = value; }
    }
}
";
            var expected =
@"Foo
Bar
Deconstruct
indexSet (with value 101)
Final array values[2] 101
";
            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void NullRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        (x, x) = null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics(
                // (7,9): error CS8210: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (x, x) = null;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "(x, x) = null").WithLocation(7, 9),
                // (7,10): error CS0165: Use of unassigned local variable 'x'
                //         (x, x) = null;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(7, 10)
                );
        }

        [Fact]
        public void VoidRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        (x, x) = M();
    }
    static void M() { }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics(
                // (7,18): error CS8206: No Deconstruct instance or extension method was found for type 'void'.
                //         (x, x) = M();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "M()").WithArguments("void").WithLocation(7, 18)
                );
        }

        [Fact]
        public void AssigningTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = (1, ""hello"");
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void AssigningTuple2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = M();
        System.Console.WriteLine(x + "" "" + y);
    }

    static System.ValueTuple<int, string> M()
    {
        return (1, ""hello"");
    }
}
";
            // Should not give this error: (9,18): error CS0029: Cannot implicitly convert type '(int, string)' to '(long, string)'
            var comp = CompileAndVerify(source, expectedOutput: "1 hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningLongTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (1, 1, 1, 1, 1, 1, 1, 1, 4, 2);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      141 (0x8d)
  .maxstack  10
  .locals init (long V_0, //x
                int V_1) //y
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.1
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.4
  IL_0009:  ldc.i4.2
  IL_000a:  newobj     ""System.ValueTuple<int, int, int>..ctor(int, int, int)""
  IL_000f:  newobj     ""System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>..ctor(int, int, int, int, int, int, int, (int, int, int))""
  IL_0014:  dup
  IL_0015:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item1""
  IL_001a:  conv.i8
  IL_001b:  stloc.0
  IL_001c:  dup
  IL_001d:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item2""
  IL_0022:  conv.i8
  IL_0023:  stloc.0
  IL_0024:  dup
  IL_0025:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item3""
  IL_002a:  conv.i8
  IL_002b:  stloc.0
  IL_002c:  dup
  IL_002d:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item4""
  IL_0032:  conv.i8
  IL_0033:  stloc.0
  IL_0034:  dup
  IL_0035:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item5""
  IL_003a:  conv.i8
  IL_003b:  stloc.0
  IL_003c:  dup
  IL_003d:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item6""
  IL_0042:  conv.i8
  IL_0043:  stloc.0
  IL_0044:  dup
  IL_0045:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Item7""
  IL_004a:  conv.i8
  IL_004b:  stloc.0
  IL_004c:  dup
  IL_004d:  ldfld      ""(int, int, int) System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Rest""
  IL_0052:  ldfld      ""int System.ValueTuple<int, int, int>.Item1""
  IL_0057:  conv.i8
  IL_0058:  stloc.0
  IL_0059:  dup
  IL_005a:  ldfld      ""(int, int, int) System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Rest""
  IL_005f:  ldfld      ""int System.ValueTuple<int, int, int>.Item2""
  IL_0064:  conv.i8
  IL_0065:  stloc.0
  IL_0066:  ldfld      ""(int, int, int) System.ValueTuple<int, int, int, int, int, int, int, (int, int, int)>.Rest""
  IL_006b:  ldfld      ""int System.ValueTuple<int, int, int>.Item3""
  IL_0070:  stloc.1
  IL_0071:  ldloc.0
  IL_0072:  box        ""long""
  IL_0077:  ldstr      "" ""
  IL_007c:  ldloc.1
  IL_007d:  box        ""int""
  IL_0082:  call       ""string string.Concat(object, object, object)""
  IL_0087:  call       ""void System.Console.WriteLine(string)""
  IL_008c:  ret
}
");
        }

        [Fact]
        public void AssigningLongTupleWithNames()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9, j: 10);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "9 10", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningLongTuple2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        int y;

        (x, x, x, x, x, x, x, x, x, y) = (1, 1, 1, 1, 1, 1, 1, 1, 4, (byte)2);
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTypelessTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x = ""goodbye"";
        string y;

        (x, y) = (null, ""hello"");
        System.Console.WriteLine($""{x}{y}"");
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "hello", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (string V_0, //x
                string V_1) //y
  IL_0000:  ldstr      ""goodbye""
  IL_0005:  stloc.0
  IL_0006:  ldnull
  IL_0007:  ldstr      ""hello""
  IL_000c:  newobj     ""System.ValueTuple<string, string>..ctor(string, string)""
  IL_0011:  dup
  IL_0012:  ldfld      ""string System.ValueTuple<string, string>.Item1""
  IL_0017:  stloc.0
  IL_0018:  ldfld      ""string System.ValueTuple<string, string>.Item2""
  IL_001d:  stloc.1
  IL_001e:  ldstr      ""{0}{1}""
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  call       ""string string.Format(string, object, object)""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
  IL_002f:  ret
}
");
        }

        [Fact]
        public void TupleWithNoConversion()
        {
            string source = @"
class C
{
    static void Main()
    {
        byte x;
        string y;

        (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (9,9): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(x, y) = (1, 2)").WithArguments("int", "byte").WithLocation(9, 9),
                // (9,9): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(x, y) = (1, 2)").WithArguments("int", "string").WithLocation(9, 9)
                );
        }

        [Fact]
        public void AssigningIntoProperties()
        {
            string source = @"
class C
{
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }

    static void Main()
    {
        (x, y) = new C();
        System.Console.WriteLine(y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expected =
@"setX 1
hello";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssigningTupleIntoProperties()
        {
            string source = @"
class C
{
    static long x { set { System.Console.WriteLine($""setX {value}""); } }
    static string y { get; set; }

    static void Main()
    {
        (x, y) = (1, ""hello"");
        System.Console.WriteLine(y);
    }
}
";
            string expected =
@"setX 1
hello";
            var comp = CompileAndVerify(source, expectedOutput: expected, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Swap()
        {
            string source = @"
class C
{
    static int x = 2;
    static int y = 4;

    static void Main()
    {
        Swap();
        System.Console.WriteLine(x + "" "" + y);
    }

    static void Swap()
    {
        (x, y) = (y, x);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "4 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Swap", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldsfld     ""int C.y""
  IL_0005:  ldsfld     ""int C.x""
  IL_000a:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000f:  dup
  IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0015:  stsfld     ""int C.x""
  IL_001a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_001f:  stsfld     ""int C.y""
  IL_0024:  ret
}
");
        }


        [Fact]
        public void TupleWithUseSiteError()
        {
            string source = @"

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
        }
    }
}
class C
{
    static void Main()
    {
        int x;
        int y;

        (x, y) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (22,9): error CS8205: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "(x, y) = (1, 2)").WithArguments("Item2", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(22, 9)
                );
        }

        [Fact]
        public void CircularFlow()
        {
            string source = @"
class C
{
    static void Main()
    {
        (object i, object ii) x = (1,2);
        object y;

        (x.ii, y) = x;
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void CircularFlow2()
        {
            string source = @"
class C
{
    static void Main()
    {
        (object i, object ii) x = (1,2);
        object y;

        ref var a = ref x;

        (a.ii, y) = x;
        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 1) 2", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature().WithRefsFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssignmentTypeIsVoid()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;

        ((x, y) = new C()).ToString();

        var z = ((x, y) = new C());
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,27): error CS0023: Operator '.' cannot be applied to operand of type 'void'
                //         ((x, y) = new C()).ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, ".").WithArguments(".", "void").WithLocation(8, 27),
                // (10,13): error CS0815: Cannot assign void to an implicitly-typed variable
                //         var z = ((x, y) = new C());
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "z = ((x, y) = new C())").WithArguments("void").WithLocation(10, 13)
                );
        }

        [Fact]
        public void NestedTupleAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = (1, (""a"", ""b""));
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedTypelessTupleAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x, y, z;

        (x, (y, z)) = (null, (null, null));
        System.Console.WriteLine(""nothing"" + x + y + z);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "nothing", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void NestedTypelessTupleAssignment2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z; // int cannot be null

        (x, (y, z)) = (null, (null, null));
        System.Console.WriteLine(""nothing"" + x + y + z);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "nothing", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedDeconstructAssignment()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = new D1();
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out D2 item2)
    {
        item1 = 1;
        item2 = new D2();
    }
}
class D2
{
    public void Deconstruct(out string item1, out string item2)
    {
        item1 = ""a"";
        item2 = ""b"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedMixedAssignment1()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z;

        (x, (y, z)) = (1, new D1());
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out int item2)
    {
        item1 = 2;
        item2 = 3;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 2 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedMixedAssignment2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y, z;

        (x, (y, z)) = new D1();
        System.Console.WriteLine(x + "" "" + y + "" "" + z);
    }
}
class D1
{
    public void Deconstruct(out int item1, out (string, string) item2)
    {
        item1 = 1;
        item2 = (""a"", ""b"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1 a b", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyNestedExecutionOrder()
        {
            string source = @"
using System;
class C
{
    int x { set { Console.WriteLine($""setX""); } }
    int y { set { Console.WriteLine($""setY""); } }
    int z { set { Console.WriteLine($""setZ""); } }

    C getHolderForX() { Console.WriteLine(""getHolderforX""); return this; }
    C getHolderForY() { Console.WriteLine(""getHolderforY""); return this; }
    C getHolderForZ() { Console.WriteLine(""getHolderforZ""); return this; }
    C getDeconstructReceiver() { Console.WriteLine(""getDeconstructReceiver""); return this; }

    static void Main()
    {
        C c = new C();
        (c.getHolderForX().x, (c.getHolderForY().y, c.getHolderForZ().z)) = c.getDeconstructReceiver();
    }
    public void Deconstruct(out D1 x, out C1 t) { x = new D1(); t = new C1(); Console.WriteLine(""Deconstruct1""); }
}
class C1
{
    public void Deconstruct(out D2 y, out D3 z) { y = new D2(); z = new D3(); Console.WriteLine(""Deconstruct2""); }
}
class D1
{
    public static implicit operator int(D1 d) { Console.WriteLine(""Conversion1""); return 1; }
}
class D2
{
    public static implicit operator int(D2 d) { Console.WriteLine(""Conversion2""); return 2; }
}
class D3
{
    public static implicit operator int(D3 d) { Console.WriteLine(""Conversion3""); return 3; }
}
";

            string expected =
@"getHolderforX
getHolderforY
getHolderforZ
getDeconstructReceiver
Deconstruct1
Deconstruct2
Conversion1
setX
Conversion2
setY
Conversion3
setZ
";
            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void VerifyNestedExecutionOrder2()
        {
            string source = @"
using System;
class C
{
    static LongInteger x1 { set { Console.WriteLine($""setX1 {value}""); } }
    static LongInteger x2 { set { Console.WriteLine($""setX2 {value}""); } }
    static LongInteger x3 { set { Console.WriteLine($""setX3 {value}""); } }
    static LongInteger x4 { set { Console.WriteLine($""setX4 {value}""); } }
    static LongInteger x5 { set { Console.WriteLine($""setX5 {value}""); } }
    static LongInteger x6 { set { Console.WriteLine($""setX6 {value}""); } }
    static LongInteger x7 { set { Console.WriteLine($""setX7 {value}""); } }

    static void Main()
    {
        ((x1, (x2, x3)), ((x4, x5), (x6, x7))) = Pair.Create(Pair.Create(new Integer(1), Pair.Create(new Integer(2), new Integer(3))),
                                                      Pair.Create(Pair.Create(new Integer(4), new Integer(5)), Pair.Create(new Integer(6), new Integer(7))));
    }
}
" + commonSource;

            string expected =
@"Deconstructing ((1, (2, 3)), ((4, 5), (6, 7)))
Deconstructing (1, (2, 3))
Deconstructing (2, 3)
Deconstructing ((4, 5), (6, 7))
Deconstructing (4, 5)
Deconstructing (6, 7)
Converting 1
setX1 1
Converting 2
setX2 2
Converting 3
setX3 3
Converting 4
setX4 4
Converting 5
setX5 5
Converting 6
setX6 6
Converting 7
setX7 7";
            var comp = CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MixOfAssignments()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        C a, b, c;
        c = new C();
        (x, y) = a = b = c;
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

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void AssignWithPostfixOperator()
        {
            string source = @"
class C
{
    int state = 1;

    static void Main()
    {
        long x;
        string y;
        C c = new C();
        (x, y) = c++;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = state;
        b = ""hello"";
    }

    public static C operator ++(C c1)
    {
        return new C() { state = 2 };
    }
}
";
            // PROTOTYPE(tuples) we expect "2 hello" instead
            var comp = CompileAndVerify(source, expectedOutput: "1 hello", parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z;

        (x, y, z) = MakePair();
    }

    public static (int, int) MakePair()
    {
        return (42, 42);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,9): error CS8211: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (x, y, z) = MakePair();
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, y, z) = MakePair()").WithArguments("2", "3").WithLocation(8, 9)
                );
        }

        [Fact]
        public void NestedTupleWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z, w;

        (x, (y, z, w)) = Pair.Create(42, (43, 44));
    }
}
" + commonSource;

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithTuplesFeature());
            comp.VerifyDiagnostics(
                // (8,9): error CS8211: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (x, (y, z, w)) = Pair.Create(42, (43, 44));
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, (y, z, w)) = Pair.Create(42, (43, 44))").WithArguments("2", "3").WithLocation(8, 9)
                );
        }
    }
}