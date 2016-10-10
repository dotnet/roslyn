// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class DeconstructionTests : CompilingTestBase
    {
        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

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
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("C", "Deconstruct").WithLocation(8, 18),
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
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
    public void Deconstruct(out int a) // too few arguments
    {
        a = 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,18): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("Deconstruct", "2").WithLocation(8, 18),
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructWrongParams2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x, y;
        (x, y) = new C();
    }
    public void Deconstruct(out int a, out int b, out int c) // too many arguments
    {
        a = b = c = 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (7,18): error CS7036: There is no argument given that corresponds to the required formal parameter 'c' of 'C.Deconstruct(out int, out int, out int)'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("c", "C.Deconstruct(out int, out int, out int)").WithLocation(7, 18),
                // (7,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(7, 18)
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
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,12): error CS1061: 'long' does not contain a definition for 'f' and no extension method 'f' accepting a first argument of type 'long' could be found (are you missing a using directive or an assembly reference?)
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "f").WithArguments("long", "f").WithLocation(8, 12),
                // (8,17): error CS1061: 'string' does not contain a definition for 'g' and no extension method 'g' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "g").WithArguments("string", "g").WithLocation(8, 17),
                // (8,22): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("Deconstruct", "2").WithLocation(8, 22),
                // (8,22): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x.f, y.g) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 22)
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
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS1615: Argument 2 may not be passed with the 'out' keyword
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "(x, y) = new C()").WithArguments("2", "out").WithLocation(8, 9),
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
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
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_BadArgRef, "(x, y) = new C()").WithArguments("1", "ref").WithLocation(8, 9),
                // (8,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DeconstructManually()
        {
            string source = @"
struct C
{
    static void Main()
    {
        long x;
        string y;
        C c = new C();

        c.Deconstruct(out x, out y); // error
        (x, y) = c;
    }

    void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (10,27): error CS1503: Argument 1: cannot convert from 'out long' to 'out int'
                //         c.Deconstruct(out x, out y); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "out long", "out int").WithLocation(10, 27)
                );
        }

        [Fact]
        public void DeconstructMethodHasOptionalParam()
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

    public void Deconstruct(out int a, out string b, int c = 42) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void BadDeconstructShadowsBaseDeconstruct()
        {
            string source = @"
class D
{
    public void Deconstruct(out int a, out string b) { a = 2; b = ""world""; }
}
class C : D
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b, int c = 42) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (13,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(13, 18)
                );
        }

        [Fact]
        public void DeconstructMethodHasParams()
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

    public void Deconstruct(out int a, out string b, params int[] c) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void DeconstructMethodHasArglist()
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

    public void Deconstruct(out int a, out string b, __arglist) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.Deconstruct(out int, out string, __arglist)'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("__arglist", "C.Deconstruct(out int, out string, __arglist)").WithLocation(9, 18),
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void DeconstructDelegate()
        {
            string source = @"
public delegate void D1(out int x, out int y);

class C
{
    public D1 Deconstruct; // not a Deconstruct operator

    static void Main()
    {
        int x, y;
        (x, y) = new C() { Deconstruct = DeconstructMethod };
    }

    public static void DeconstructMethod(out int a, out int b) { a = 1; b = 2; }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (11,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C() { Deconstruct = DeconstructMethod };
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C() { Deconstruct = DeconstructMethod }").WithArguments("C", "2").WithLocation(11, 18)
                );
        }

        [Fact]
        public void DeconstructDelegate2()
        {
            string source = @"
public delegate void D1(out int x, out int y);

class C
{
    public D1 Deconstruct;

    static void Main()
    {
        int x, y;
        (x, y) = new C() { Deconstruct = DeconstructMethod };
    }

    public static void DeconstructMethod(out int a, out int b) { a = 1; b = 2; }

    public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (16,17): error CS0102: The type 'C' already contains a definition for 'Deconstruct'
                //     public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Deconstruct").WithArguments("C", "Deconstruct").WithLocation(16, 17),
                // (11,28): error CS1913: Member 'Deconstruct' cannot be initialized. It is not a field or property.
                //         (x, y) = new C() { Deconstruct = DeconstructMethod };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "Deconstruct").WithArguments("Deconstruct").WithLocation(11, 28),
                // (6,15): warning CS0649: Field 'C.Deconstruct' is never assigned to, and will always have its default value null
                //     public D1 Deconstruct;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Deconstruct").WithArguments("C.Deconstruct", "null").WithLocation(6, 15)
                );
        }

        [Fact]
        public void DeconstructEvent()
        {
            string source = @"
public delegate void D1(out int x, out int y);

class C
{
    public event D1 Deconstruct;  // not a Deconstruct operator

    static void Main()
    {
        long x;
        int y;
        C c = new C();
        c.Deconstruct += DeconstructMethod;
        (x, y) = c;
    }

    public static void DeconstructMethod(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (14,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = c;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "c").WithArguments("C", "2").WithLocation(14, 18),
                // (6,21): warning CS0067: The event 'C.Deconstruct' is never used
                //     public event D1 Deconstruct;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Deconstruct").WithArguments("C.Deconstruct").WithLocation(6, 21)
                );
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
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
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
        System.Console.Write(type.ToString());
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "System.ValueTuple`2[System.Int32,System.Int32]");
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

            var comp = CreateCompilationWithMscorlib(source);
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

            var comp = CreateCompilationWithMscorlib(source);
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

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,24): error CS1525: Invalid expression term '.'
                //         ((int, string)).ToString();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(6, 24)
                );
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningMethod2()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        (M(), M()) = new C();
        System.Console.Write(i);
    }

    static ref int M()
    {
        System.Console.Write(""M "");
        return ref i;
    }

    void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "M M 43", additionalRefs: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (4,16): warning CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
                );
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
static class D
{
    public static void Deconstruct(this int input, out int output, out int output2) { output = input; output2 = input; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
            comp.VerifyDiagnostics(
                // (7,18): error CS0165: Use of unassigned local variable 'x'
                //         (x, x) = x;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(7, 18)
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

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics(
                // (7,18): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (x, x) = null;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(7, 18)
                );
        }

        [Fact]
        public void ErrorRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        (x, x) = undeclared;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics(
                // (7,18): error CS0103: The name 'undeclared' does not exist in the current context
                //         (x, x) = undeclared;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "undeclared").WithArguments("undeclared").WithLocation(7, 18)
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

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithRefsFeature());
            comp.VerifyDiagnostics(
                // (7,18): error CS1061: 'void' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'void' could be found (are you missing a using directive or an assembly reference?)
                //         (x, x) = M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M()").WithArguments("void", "Deconstruct").WithLocation(7, 18),
                // (7,18): error CS8129: No Deconstruct instance or extension method was found for type 'void', with 2 out parameters.
                //         (x, x) = M();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "M()").WithArguments("void", "2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void AssigningTupleWithNoConversion()
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
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (9,22): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "string").WithLocation(9, 22)
                );
        }

        [Fact]
        public void NotAssignable()
        {
            string source = @"
class C
{
    static void Main()
    {
        (1, P) = (1, 2);
    }
    static int P { get { return 1; } }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (1, P) = (1, 2);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "1").WithLocation(6, 10),
                // (6,13): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //         (1, P) = (1, 2);
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(6, 13)
                );
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

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (22,9): error CS8128: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "(x, y) = (1, 2)").WithArguments("Item2", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(22, 9)
                );
        }

        [Fact]
        public void AssignUsingAmbiguousDeconstruction()
        {
            string source = @"
class Base
{
    public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
    public void Deconstruct(out long a, out long b) { a = 1; b = 2; }
}
class C : Base
{
    static void Main()
    {
        int x, y;
        (x, y) = new C();

        System.Console.WriteLine(x + "" "" + y);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (12,18): error CS0121: The call is ambiguous between the following methods or properties: 'Base.Deconstruct(out int, out int)' and 'Base.Deconstruct(out long, out long)'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_AmbigCall, "new C()").WithArguments("Base.Deconstruct(out int, out int)", "Base.Deconstruct(out long, out long)").WithLocation(12, 18),
                // (12,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(12, 18)
                );
        }

        [Fact]
        public void DeconstructIsDynamicField()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        (x, y) = new C();

    }
    public dynamic Deconstruct = null;
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef });
            comp.VerifyDiagnostics(
                // (7,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void DeconstructIsField()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        (x, y) = new C();

    }
    public object Deconstruct = null;
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,18): error CS1955: Non-invocable member 'C.Deconstruct' cannot be used like a method.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "new C()").WithArguments("C.Deconstruct").WithLocation(7, 18),
                // (7,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void CannotDeconstructRefTuple22()
        {
            string template = @"
using System;
class C
{
    static void Main()
    {
        int VARIABLES; // int x1, x2, ...
        (VARIABLES) = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)));
    }

    public static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) =>
        new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
}
";
            var tuple = String.Join(", ", Enumerable.Range(1, 22).Select(n => n.ToString()));
            var variables = String.Join(", ", Enumerable.Range(1, 22).Select(n => $"x{n}"));

            var source = template.Replace("VARIABLES", variables).Replace("TUPLE", tuple);

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,113): error CS1501: No overload for method 'Deconstruct' takes 22 arguments
                //         (x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22) = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)));
                Diagnostic(ErrorCode.ERR_BadArgCount, "CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)))").WithArguments("Deconstruct", "22").WithLocation(8, 113),
                // (8,113): error CS8129: No Deconstruct instance or extension method was found for type 'Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int>>>>', with 22 out parameters.
                //         (x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22) = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)))").WithArguments("System.Tuple<int, int, int, int, int, int, int, System.Tuple<int, int, int, int, int, int, int, System.Tuple<int, int, int, int, int, int, int, System.Tuple<int>>>>", "22").WithLocation(8, 113)
                );
        }

        [Fact]
        public void DeconstructUsingDynamicMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        dynamic c = new C();
        (x, y) = c;
    }
    public void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (10,18): error CS8133: Cannot deconstruct dynamic objects.
                //         (x, y) = c;
                Diagnostic(ErrorCode.ERR_CannotDeconstructDynamic, "c").WithLocation(10, 18)
                );
        }

        [Fact]
        public void DeconstructMethodInaccessible()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        (x, y) = new C1();
    }
}
class C1
{
    protected void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (9,18): error CS0122: 'C1.Deconstruct(out int, out string)' is inaccessible due to its protection level
                //         (x, y) = new C1();
                Diagnostic(ErrorCode.ERR_BadAccess, "new C1()").WithArguments("C1.Deconstruct(out int, out string)").WithLocation(9, 18),
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C1', with 2 out parameters.
                //         (x, y) = new C1();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C1()").WithArguments("C1", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void DeconstructHasUseSiteError()
        {
            string libMissingSource = @"public class Missing { }";

            string libSource = @"
public class C
{
    public void Deconstruct(out Missing a, out Missing b) { a = new Missing(); b = new Missing(); }
}
";

            string source = @"
class C1
{
    static void Main()
    {
        object x, y;
        (x, y) = new C();
    }
}
";
            var libMissingComp = CreateCompilationWithMscorlib(new string[] { libMissingSource }, assemblyName: "libMissingComp").VerifyDiagnostics();
            var libMissingRef = libMissingComp.EmitToImageReference();

            var libComp = CreateCompilationWithMscorlib(new string[] { libSource }, references: new[] { libMissingRef }, parseOptions: TestOptions.Regular).VerifyDiagnostics();
            var libRef = libComp.EmitToImageReference();

            var comp = CreateCompilationWithMscorlib(new string[] { source }, references: new[] { libRef });
            comp.VerifyDiagnostics(
                // (7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'libMissingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()").WithArguments("Missing", "libMissingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
                // (7,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void StaticDeconstruct()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        (x, y) = new C();
    }
    public static void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (9,18): error CS0176: Member 'C.Deconstruct(out int, out string)' cannot be accessed with an instance reference; qualify it with a type name instead
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "new C()").WithArguments("C.Deconstruct(out int, out string)").WithLocation(9, 18),
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 18)
                );
        }

        [Fact]
        public void AssignmentTypeIsValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x; string y;

        var z1 = ((x, y) = new C()).ToString();

        var z2 = ((x, y) = new C());
        var z3 = (x, y) = new C();

        System.Console.Write($""{z1} {z2.ToString()} {z3.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "(1, hello) (1, hello) (1, hello)", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NestedAssignmentTypeIsValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1; string x2; int x3;

        var y = ((x1, x2), x3) = (new C(), 3);

        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "((1, hello), 3)", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AssignmentReturnsLongValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x;
        var y = (x, x, x, x, x, x, x, x, x) = new C();
        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int x1, out int x2, out int x3, out int x4, out int x5, out int x6, out int x7, out int x8, out int x9)
    {
        x1 = x2 = x3 = x4 = x5 = x6 = x7 = x8 = 1;
        x9 = 9;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "(1, 1, 1, 1, 1, 1, 1, 1, 9)", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var y = nodes.OfType<VariableDeclaratorSyntax>().Skip(1).First();

            Assert.Equal("y = (x, x, x, x, x, x, x, x, x) = new C()", y.ToFullString());

            Assert.Equal("(System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64) y",
                model.GetDeclaredSymbol(y).ToTestDisplayString());
        }

        [Fact]
        public void DeconstructWithoutValueTupleLibrary()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x;
        var y = (x, x) = new C();
        System.Console.Write(y.ToString());
    }

    public void Deconstruct(out int x1, out int x2)
    {
        x1 = x2 = 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (7,17): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var y = (x, x) = new C();
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(x, x) = new C()").WithArguments("System.ValueTuple`2").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ChainedAssignment()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1, x2;
        var y = (x1, x1) = (x2, x2) = new C();
        System.Console.Write($""{y.ToString()} {x1} {x2}"");
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(1, 1) 1 1");
        }

        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,24): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         (x, (y, z)) = (null, (null, null));
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 24),
                // (8,31): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         (x, (y, z)) = (null, (null, null));
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 31),
                // (8,37): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         (x, (y, z)) = (null, (null, null));
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 37)
                );
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
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,9): error CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
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

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,9): error CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (x, (y, z, w)) = Pair.Create(42, (43, 44));
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, (y, z, w)) = Pair.Create(42, (43, 44))").WithArguments("2", "3").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DeconstructionTooFewElements()
        {
            string source = @"
class C
{
    static void Main()
    {
        for ((var (x, y)) = Pair.Create(1, 2); ;) { }
    }
}
" + commonSource;

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,20): error CS0103: The name 'x' does not exist in the current context
                //         for ((var (x, y)) = Pair.Create(1, 2); ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 20),
                // (6,23): error CS0103: The name 'y' does not exist in the current context
                //         for ((var (x, y)) = Pair.Create(1, 2); ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 23),
                // (6,15): error CS0103: The name 'var' does not exist in the current context
                //         for ((var (x, y)) = Pair.Create(1, 2); ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 15)
                );
        }

        [Fact]
        public void DeconstructionDeclarationInCSharp6()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = Pair.Create(1, 2);
        (int x3, int x4) = Pair.Create(1, 2);
        foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
        for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
    }
}
" + commonSource;

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         var (x1, x2) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "var (x1, x2)").WithArguments("tuples", "7").WithLocation(6, 9),
                // (7,9): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         (int x3, int x4) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x3, int x4)").WithArguments("tuples", "7").WithLocation(7, 9),
                // (8,18): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x5, var (x6, x7))").WithArguments("tuples", "7").WithLocation(8, 18),
                // (9,14): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x8, var (x9, x10))").WithArguments("tuples", "7").WithLocation(9, 14),
                // (8,32): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x6'.
                //         foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x6").WithArguments("x6").WithLocation(8, 32),
                // (8,36): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x7'.
                //         foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x7").WithArguments("x7").WithLocation(8, 36),
                // (9,28): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x9'.
                //         for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x9").WithArguments("x9").WithLocation(9, 28),
                // (9,32): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x10'.
                //         for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x10").WithArguments("x10").WithLocation(9, 32)
                );
        }

        [Fact]
        public void DeclareLocalTwice()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x1) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,18): error CS0128: A local variable named 'x1' is already defined in this scope
                //         var (x1, x1) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(6, 18)
                );
        }

        [Fact]
        public void DeclareLocalTwice2()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x1 = null;
        var (x1, x2) = (1, 2);
        System.Console.WriteLine(x1);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,14): error CS0128: A local variable named 'x1' is already defined in this scope
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(7, 14)
                );
        }

        [Fact]
        public void VarMethodMissing()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 1;
        int x2 = 1;
        var (x1, x2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,9): error CS0103: The name 'var' does not exist in the current context
                //         var (x1, x2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 9)
                );
        }

        [Fact]
        public void IncompleteDeclarationIsSeenAsTupleType()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, string x2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,28): error CS1001: Identifier expected
                //         (int x1, string x2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 28)
                );
        }

        [Fact]
        public void UseBeforeDeclared()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, int x2) = M(x1);
    }
    static (int, int) M(int a) { return (1, 2); }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,30): error CS0165: Use of unassigned local variable 'x1'
                //         (int x1, int x2) = M(x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 30)
                );
        }

        [Fact]
        public void DeclareWithVoidType()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, int x2) = M(x1);
    }
    static void M(int a) { }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,28): error CS1061: 'void' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'void' could be found (are you missing a using directive or an assembly reference?)
                //         (int x1, int x2) = M(x1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M(x1)").WithArguments("void", "Deconstruct").WithLocation(6, 28),
                // (6,28): error CS8129: No Deconstruct instance or extension method was found for type 'void', with 2 out parameters.
                //         (int x1, int x2) = M(x1);
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "M(x1)").WithArguments("void", "2").WithLocation(6, 28),
                // (6,30): error CS0165: Use of unassigned local variable 'x1'
                //         (int x1, int x2) = M(x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 30)
                );
        }

        [Fact]
        public void UseBeforeDeclared2()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(x1);
        (int x1, int x2) = (1, 2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,34): error CS0841: Cannot use local variable 'x1' before it is declared
                //         System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 34)
                );
        }

        [Fact]
        public void NullAssignmentInDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int x1, int x2) = null;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,28): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (int x1, int x2) = null;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 28)
                );
        }

        [Fact]
        public void NullAssignmentInVarDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = null;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,24): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         var (x1, x2) = null;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 24),
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         var (x1, x2) = null;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 14),
                // (6,18): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         var (x1, x2) = null;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 18)
                );
        }

        [Fact]
        public void TypelessDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (1, null);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         var (x1, x2) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 14),
                // (6,18): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         var (x1, x2) = (1, null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 18)
                );
        }

        [Fact]
        public void TypeMergingWithMultipleAmbiguousVars()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, (byte x2, var x3), var x4) = (null, (2, null), null);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,35): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x3'.
                //         (string x1, (byte x2, var x3), var x4) = (null, (2, null), null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x3").WithArguments("x3").WithLocation(6, 35),
                // (6,44): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x4'.
                //         (string x1, (byte x2, var x3), var x4) = (null, (2, null), null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x4").WithArguments("x4").WithLocation(6, 44)
                );
        }

        [Fact]
        public void TypeMergingWithTooManyLeftNestings()
        {
            string source = @"
class C
{
    static void Main()
    {
        ((string x1, byte x2, var x3), int x4) = (null, 4);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,51): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         ((string x1, byte x2, var x3), int x4) = (null, 4);
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 51),
                // (6,35): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x3'.
                //         ((string x1, byte x2, var x3), int x4) = (null, 4);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x3").WithArguments("x3").WithLocation(6, 35)
                );
        }

        [Fact]
        public void TypeMergingWithTooManyRightNestings()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, var x2) = (null, (null, 2));
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         (string x1, var x2) = (null, (null, 2));
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 25)
                );
        }

        [Fact]
        public void TypeMergingWithTooManyLeftVariables()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, var x2, int x3) = (null, ""hello"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (string x1, var x2, int x3) = (null, "hello");
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var x2, int x3) = (null, ""hello"");").WithArguments("2", "3").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TypeMergingWithTooManyRightElements()
        {
            string source = @"
class C
{
    static void Main()
    {
        (string x1, var y1) = (null, ""hello"", 3);
        (string x2, var y2) = (null, ""hello"", null);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         (string x1, var y1) = (null, "hello", 3);
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var y1) = (null, ""hello"", 3);").WithArguments("3", "2").WithLocation(6, 9),
                // (7,47): error CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (string x2, var y2) = (null, "hello", null);
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(7, 47),
                // (7,25): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y2'.
                //         (string x2, var y2) = (null, "hello", null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y2").WithArguments("y2").WithLocation(7, 25)
                );
        }

        [Fact]
        public void DeclarationVarFormWithActualVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (1, 2);
    }
}
class var { }
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,14): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x1").WithLocation(6, 14),
                // (6,18): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x2").WithLocation(6, 18),
                // (6,25): error CS0029: Cannot implicitly convert type 'int' to 'var'
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "var").WithLocation(6, 25),
                // (6,28): error CS0029: Cannot implicitly convert type 'int' to 'var'
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "var").WithLocation(6, 28)
                );
        }

        [Fact]
        public void DeclarationVarFormWithAliasedVarType()
        {
            string source = @"
using var = D;
class C
{
    static void Main()
    {
        var (x3, x4) = (3, 4);
    }
}
class D
{
    public override string ToString() { return ""var""; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,14): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         var (x3, x4) = (3, 4);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x3").WithLocation(7, 14),
                // (7,18): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         var (x3, x4) = (3, 4);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x4").WithLocation(7, 18),
                // (7,25): error CS0029: Cannot implicitly convert type 'int' to 'D'
                //         var (x3, x4) = (3, 4);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "3").WithArguments("int", "D").WithLocation(7, 25),
                // (7,28): error CS0029: Cannot implicitly convert type 'int' to 'D'
                //         var (x3, x4) = (3, 4);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "4").WithArguments("int", "D").WithLocation(7, 28)
                );
        }

        [Fact]
        public void DeclarationWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        (var (x1, x2), var x3) = (1, 2, 3);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         (var (x1, x2), var x3) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(var (x1, x2), var x3) = (1, 2, 3);").WithArguments("3", "2").WithLocation(6, 9),
                // (6,15): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         (var (x1, x2), var x3) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 15),
                // (6,19): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         (var (x1, x2), var x3) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 19)
                );
        }

        [Fact]
        public void DeclarationWithCircularity1()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (1, x1);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,28): error CS0841: Cannot use local variable 'x1' before it is declared
                //         var (x1, x2) = (1, x1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 28),
                // (6,28): error CS0165: Use of unassigned local variable 'x1'
                //         var (x1, x2) = (1, x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 28)
                );
        }

        [Fact]
        public void DeclarationWithCircularity2()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (x2, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS0841: Cannot use local variable 'x2' before it is declared
                //         var (x1, x2) = (x2, 2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 25),
                // (6,25): error CS0165: Use of unassigned local variable 'x2'
                //         var (x1, x2) = (x2, 2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(6, 25)
                );
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningVarInvocation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        int x = 0, y = 0;
        var(x, y) = 42; // parsed as deconstruction
        System.Console.WriteLine(i);
    }
    static ref int var(int a, int b) { return ref i; }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS0128: A local variable named 'x' is already defined in this scope
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(9, 13),
                // (9,16): error CS0128: A local variable named 'y' is already defined in this scope
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(9, 16),
                // (9,21): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "42").WithArguments("int", "Deconstruct").WithLocation(9, 21),
                // (9,21): error CS8129: No Deconstruct instance or extension method was found for type 'int', with 2 out parameters.
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "42").WithArguments("int", "2").WithLocation(9, 21),
                // (9,13): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(9, 13),
                // (9,16): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                //         var(x, y) = 42; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(9, 16),
                // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13),
                // (8,20): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 20),
                // (4,16): warning CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/12468"), CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12468, "https://github.com/dotnet/roslyn/issues/12468")]
        public void RefReturningVarInvocation2()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        int x = 0, y = 0;
        @var(x, y) = 42; // parsed as invocation
        System.Console.Write(i + "" "");
        (var(x, y)) = 43; // parsed as invocation
        System.Console.Write(i + "" "");
        (var(x, y) = 44); // parsed as invocation
        System.Console.Write(i);
    }
    static ref int var(int a, int b) { return ref i; }
}
";
            // The correct expectation is for the code to compile and execute
            //var comp = CompileAndVerify(source, expectedOutput: "42 43 44");
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (11,9): error CS8134: Deconstruction must contain at least two variables.
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructTooFewElements, "(var(x, y)) = 43").WithLocation(11, 9),
                // (13,20): error CS1026: ) expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "=").WithLocation(13, 20),
                // (13,24): error CS1002: ; expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(13, 24),
                // (13,24): error CS1513: } expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(13, 24),
                // (9,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(9, 14),
                // (9,9): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@var").WithArguments("var").WithLocation(9, 9),
                // (9,14): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x").WithLocation(9, 14),
                // (9,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(9, 17),
                // (9,9): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@var").WithArguments("var").WithLocation(9, 9),
                // (9,17): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "y").WithLocation(9, 17),
                // (9,22): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "42").WithArguments("int", "Deconstruct").WithLocation(9, 22),
                // (9,22): error CS8129: No Deconstruct instance or extension method was found for type 'int', with 2 out parameters.
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "42").WithArguments("int", "2").WithLocation(9, 22),
                // (11,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(11, 14),
                // (11,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(11, 17),
                // (11,23): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "43").WithArguments("int", "Deconstruct").WithLocation(11, 23),
                // (11,23): error CS8129: No Deconstruct instance or extension method was found for type 'int', with 1 out parameters.
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "43").WithArguments("int", "1").WithLocation(11, 23),
                // (13,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(13, 14),
                // (13,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(13, 17),
                // (13,22): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "44").WithArguments("int", "Deconstruct").WithLocation(13, 22),
                // (13,22): error CS8129: No Deconstruct instance or extension method was found for type 'int', with 1 out parameters.
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "44").WithArguments("int", "1").WithLocation(13, 22),
                // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13),
                // (8,20): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 20)
                );
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningInvocation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        int x = 0, y = 0;
        M(x, y) = 42;
        System.Console.WriteLine(i);
    }
    static ref int M(int a, int b) { return ref i; }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "42");
            comp.VerifyDiagnostics(
                // (4,16): warning CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
                );
        }

        [Fact]
        public void DeclarationWithTypeInsideVarForm()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (int x1, x2) = (1, 2);
        var (var x3, x4) = (1, 2);
        var (x5, var (x6, x7)) = (1, (2, 3));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var (int x1, x2)").WithLocation(6, 9),
                // (6,14): error CS1525: Invalid expression term 'int'
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 14),
                // (6,18): error CS1003: Syntax error, ',' expected
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x1").WithArguments(",", "").WithLocation(6, 18),
                // (7,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var (var x3, x4)").WithLocation(7, 9),
                // (7,18): error CS1003: Syntax error, ',' expected
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x3").WithArguments(",", "").WithLocation(7, 18),
                // (8,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var (x5, var (x6, x7))").WithLocation(8, 9),
                // (6,18): error CS0103: The name 'x1' does not exist in the current context
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 18),
                // (6,22): error CS0103: The name 'x2' does not exist in the current context
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(6, 22),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (7,14): error CS0103: The name 'var' does not exist in the current context
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 14),
                // (7,18): error CS0103: The name 'x3' does not exist in the current context
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(7, 18),
                // (7,22): error CS0103: The name 'x4' does not exist in the current context
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(7, 22),
                // (7,9): error CS0103: The name 'var' does not exist in the current context
                //         var (var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 9),
                // (8,14): error CS0103: The name 'x5' does not exist in the current context
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(8, 14),
                // (8,23): error CS0103: The name 'x6' does not exist in the current context
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(8, 23),
                // (8,27): error CS0103: The name 'x7' does not exist in the current context
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(8, 27),
                // (8,18): error CS0103: The name 'var' does not exist in the current context
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 18),
                // (8,9): error CS0103: The name 'var' does not exist in the current context
                //         var (x5, var (x6, x7)) = (1, (2, 3));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 9)
                );
        }

        [Fact]
        public void ForWithCircularity1()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (var (x1, x2) = (1, x1); ; ) { }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,33): error CS0841: Cannot use local variable 'x1' before it is declared
                //         for (var (x1, x2) = (1, x1); ; ) { }
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 33),
                // (6,33): error CS0165: Use of unassigned local variable 'x1'
                //         for (var (x1, x2) = (1, x1); ; ) { }
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 33)
                );
        }

        [Fact]
        public void ForWithCircularity2()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (var (x1, x2) = (x2, 2); ; ) { }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,30): error CS0841: Cannot use local variable 'x2' before it is declared
                //         for (var (x1, x2) = (x2, 2); ; ) { }
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 30),
                // (6,30): error CS0165: Use of unassigned local variable 'x2'
                //         for (var (x1, x2) = (x2, 2); ; ) { }
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(6, 30)
                );
        }

        [Fact]
        public void ForEachNameConflict()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 1;
        foreach ((int x1, int x2) in M()) { }
        System.Console.Write(x1);
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,23): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach ((int x1, int x2) in M())
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(7, 23)
                );
        }

        [Fact]
        public void ForEachNameConflict2()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M(out int x1)) { }
    }
    static (int, int)[] M(out int a) { a = 1; return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,23): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         foreach ((int x1, int x2) in M(out int x1)) { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(6, 23)
                );
        }

        [Fact]
        public void ForEachNameConflict3()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M())
        {
            int x1 = 1;
            System.Console.Write(x1);
        }
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,17): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int x1 = 1;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(8, 17)
                );
        }

        [Fact]
        public void ForEachUseBeforeDeclared()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M(x1)) { }
    }
    static (int, int)[] M(int a) { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,40): error CS0103: The name 'x1' does not exist in the current context
                //         foreach ((int x1, int x2) in M(x1))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 40)
                );
        }

        [Fact]
        public void ForEachUseOutsideScope()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M()) { }
        System.Console.Write(x1);
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,30): error CS0103: The name 'x1' does not exist in the current context
                //         System.Console.Write(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 30)
                );
        }

        [Fact]
        public void ForEachNoIEnumerable()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in 1)
        {
            System.Console.WriteLine(x1 + "" "" + x2);
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,34): error CS1579: foreach statement cannot operate on variables of type 'int' because 'int' does not contain a public definition for 'GetEnumerator'
                //         foreach (var (x1, x2) in 1)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "1").WithArguments("int", "GetEnumerator").WithLocation(6, 34),
                // (6,23): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         foreach (var (x1, x2) in 1)
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 23),
                // (6,27): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         foreach (var (x1, x2) in 1)
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 27)
                );
        }

        [Fact]
        public void ForEachIterationVariablesAreReadonly()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, var (x2, x3)) in new[] { (1, (1, 1)) })
        {
            x1 = 1;
            x2 = 2;
            x3 = 3;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'x1' because it is a 'foreach iteration variable'
                //             x1 = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x1").WithArguments("x1", "foreach iteration variable").WithLocation(8, 13),
                // (9,13): error CS1656: Cannot assign to 'x2' because it is a 'foreach iteration variable'
                //             x2 = 2;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x2").WithArguments("x2", "foreach iteration variable").WithLocation(9, 13),
                // (10,13): error CS1656: Cannot assign to 'x3' because it is a 'foreach iteration variable'
                //             x3 = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x3").WithArguments("x3", "foreach iteration variable").WithLocation(10, 13)
                );
        }

        [Fact]
        public void ForEachScoping()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M(x1)) { }
    }
    static (int, int) M(int i) { return (1, 2); }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,36): error CS0103: The name 'x1' does not exist in the current context
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 36),
                // (6,23): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 23),
                // (6,27): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 27)
                );
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
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GetTypeInfoForTupleLiteral()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x1 = (1, 2);
        var (x2, x3) = (1, 2);
    }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var literal1 = nodes.OfType<TupleExpressionSyntax>().First();
                Assert.Equal("(int, int)", model.GetTypeInfo(literal1).Type.ToDisplayString());

                var literal2 = nodes.OfType<TupleExpressionSyntax>().Skip(1).First();
                Assert.Equal("(int, int)", model.GetTypeInfo(literal2).Type.ToDisplayString());
            };

            var verifier = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void DeclarationWithCircularity3()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (M(out x2), M(out x1));
    }
    static T M<T>(out T x) { x = default(T); return x; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //         var (x1, x2) = (M(out x2), M(out x1));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 31),
                // (6,42): error CS0841: Cannot use local variable 'x1' before it is declared
                //         var (x1, x2) = (M(out x2), M(out x1));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 42)
                );
        }

        [Fact, WorkItem(13081, "https://github.com/dotnet/roslyn/issues/13081")]
        public void GettingDiagnosticsWhenValueTupleIsMissing()
        {
            var source = @"
class C1
{
    static void Test(int arg1, (byte, byte) arg2)
    {
        foreach ((int, int) e in new (int, int)[10])
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (4,32): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     static void Test(int arg1, (byte, byte) arg2)
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(byte, byte)").WithArguments("System.ValueTuple`2").WithLocation(4, 32),
                // (6,38): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         foreach ((int, int) e in new (int, int)[10])
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(6, 38),
                // (6,18): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         foreach ((int, int) e in new (int, int)[10])
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(6, 18)
                );
            // no crash
        }

        [Fact]
        public void DeclarationCannotBeEmbedded()
        {
            var source = @"
class C1
{
    void M()
    {
        if (true)
            var (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var (x, y) = (1, 2);").WithLocation(7, 13)
                );
        }

        [Fact]
        public void DeconstructObsoleteWarning()
        {
            var source = @"
class C
{
    void M()
    {
       (int y1, int y2) = new C();
    }
    [System.Obsolete()]
    void Deconstruct(out int x1, out int x2) { x1 = 1; x2 = 2; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,27): warning CS0612: 'C.Deconstruct(out int, out int)' is obsolete
                //        (int y1, int y2) = new C();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.Deconstruct(out int, out int)").WithLocation(6, 27)
                );
        }

        [Fact]
        public void DeconstructObsoleteError()
        {
            var source = @"
class C
{
    void M()
    {
       (int y1, int y2) = new C();
    }
    [System.Obsolete(""Deprecated"", error: true)]
    void Deconstruct(out int x1, out int x2) { x1 = 1; x2 = 2; }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,27): error CS0619: 'C.Deconstruct(out int, out int)' is obsolete: 'Deprecated'
                //        (int y1, int y2) = new C();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new C()").WithArguments("C.Deconstruct(out int, out int)", "Deprecated").WithLocation(6, 27)
                );
        }

        [Fact]
        public void DeconstructionLocalsDeclaredNotUsed()
        {
            // Check that there are no *use sites* within this code for local variables.
            // They are declared herein, but nowhere used. So they should not be returned
            // by SemanticModel.GetSymbolInfo.
            string source = @"
class Program
{
    static void Main()
    {
        var (x1, y1) = (1, 2);

        (var x2, var y2) = (1, 2);
    }

    static void M((int, int) t)
    {
        var (x3, y3) = t;

        (var x4, var y4) = t;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            foreach (var node in nodes)
            {
                var si = model.GetSymbolInfo(node);
                var symbol = si.Symbol;
                if (symbol == null) continue;
                Assert.NotEqual(SymbolKind.Local, symbol.Kind);
            }
        }

    }
}
