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
            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (9,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (11,18): error CS8129: No Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
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

            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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

            var comp = CreateStandardCompilation(source);
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (8,10): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("int", "byte").WithLocation(8, 10),
                // (8,13): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("int", "string").WithLocation(8, 13)
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

            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs, options: TestOptions.DebugExe);
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,11): error CS1525: Invalid expression term 'int'
                //         ((int, string)).ToString();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 11),
                // (6,16): error CS1525: Invalid expression term 'string'
                //         ((int, string)).ToString();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(6, 16)
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef });
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular.WithRefsFeature());
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular.WithRefsFeature());
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

            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular.WithRefsFeature());
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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
        System.Console.WriteLine($""{x} {y}"");
    }
}
";

            var comp = CreateStandardCompilation(source, assemblyName: "comp", options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");
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

            var comp = CreateStandardCompilation(source);
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef, CSharpRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var libMissingComp = CreateStandardCompilation(new string[] { libMissingSource }, assemblyName: "libMissingComp").VerifyDiagnostics();
            var libMissingRef = libMissingComp.EmitToImageReference();

            var libComp = CreateStandardCompilation(new string[] { libSource }, references: new[] { libMissingRef }, parseOptions: TestOptions.Regular).VerifyDiagnostics();
            var libRef = libComp.EmitToImageReference();

            var comp = CreateStandardCompilation(new string[] { source }, references: new[] { libRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (7,17): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var y = (x, x) = new C();
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(x, x)").WithArguments("System.ValueTuple`2").WithLocation(7, 17)
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs, options: TestOptions.DebugExe);
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,13): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         var (x1, x2) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(x1, x2)").WithArguments("tuples", "7.0").WithLocation(6, 13),
                // (7,9): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         (int x3, int x4) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x3, int x4)").WithArguments("tuples", "7.0").WithLocation(7, 9),
                // (8,18): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x5, var (x6, x7))").WithArguments("tuples", "7.0").WithLocation(8, 18),
                // (9,14): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x8, var (x9, x10))").WithArguments("tuples", "7.0").WithLocation(9, 14)
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,9): error CS0103: The name 'var' does not exist in the current context
                //         var (x1, x2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 9)
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         (string x1, var x2, int x3) = (null, "hello");
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var x2, int x3) = (null, ""hello"")").WithArguments("2", "3").WithLocation(6, 9)
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         (string x1, var y1) = (null, "hello", 3);
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var y1) = (null, ""hello"", 3)").WithArguments("3", "2").WithLocation(6, 9),
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,13): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         var (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x1, x2)").WithLocation(6, 13),
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,13): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         var (x3, x4) = (3, 4);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x3, x4)").WithLocation(7, 13),
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         (var (x1, x2), var x3) = (1, 2, 3);
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(var (x1, x2), var x3) = (1, 2, 3)").WithArguments("3", "2").WithLocation(6, 9),
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source);
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
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 20)
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
            var comp = CreateStandardCompilation(source);
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
            comp.VerifyDiagnostics();
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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
        System.Console.Write($""{x1} {x2} {x3}"");
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
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
            var comp = CreateStandardCompilation(source);
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
        public void DeconstructionMayBeEmbedded()
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // this is no longer considered a declaration statement,
                // but rather is an assignment expression. So no error.
                );
        }

        [Fact]
        public void AssignmentExpressionCanBeUsedInEmbeddedStatement()
        {
            var source = @"
class C1
{
    void M()
    {
        int x, y;
        if (true)
            (x, y) = (1, 2);
    }
}
";
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
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
            // They are not declared. So they should not be returned
            // by SemanticModel.GetSymbolInfo. Similarly, check that all designation syntax
            // forms declare deconstruction locals.
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
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            foreach (var node in nodes)
            {
                var si = model.GetSymbolInfo(node);
                var symbol = si.Symbol;
                if ((object)symbol != null)
                {
                    if (node is DeclarationExpressionSyntax)
                    {
                        Assert.Equal(SymbolKind.Local, symbol.Kind);
                        Assert.Equal(LocalDeclarationKind.DeconstructionVariable, ((LocalSymbol)symbol).DeclarationKind);
                    }
                    else
                    {
                        Assert.NotEqual(SymbolKind.Local, symbol.Kind);
                    }
                }

                symbol = model.GetDeclaredSymbol(node);
                if ((object)symbol != null)
                {
                    if (node is SingleVariableDesignationSyntax)
                    {
                        Assert.Equal(SymbolKind.Local, symbol.Kind);
                        Assert.Equal(LocalDeclarationKind.DeconstructionVariable, ((LocalSymbol)symbol).DeclarationKind);
                    }
                    else
                    {
                        Assert.NotEqual(SymbolKind.Local, symbol.Kind);
                    }
                }
            }
        }

        [Fact, WorkItem(14287, "https://github.com/dotnet/roslyn/issues/14287")]
        public void TupleDeconstructionStatementWithTypesCannotBeConst()
        {
            string source = @"
class C
{
    static void Main()
    {
        const (int x, int y) = (1, 2);
    }
}
";

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,30): error CS1001: Identifier expected
                //         const (int x, int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(6, 30),
                // (6,15): error CS0283: The type '(int x, int y)' cannot be declared const
                //         const (int x, int y) = (1, 2);
                Diagnostic(ErrorCode.ERR_BadConstType, "(int x, int y)").WithArguments("(int x, int y)").WithLocation(6, 15)
                );
        }

        [Fact, WorkItem(14287, "https://github.com/dotnet/roslyn/issues/14287")]
        public void TupleDeconstructionStatementWithoutTypesCannotBeConst()
        {
            string source = @"
class C
{
    static void Main()
    {
        const var (x, y) = (1, 2);
    }
}
";

            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'const' is not valid for this item
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "const").WithArguments("const").WithLocation(6, 9),
                // (6,19): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(6, 19),
                // (6,21): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(6, 21),
                // (6,24): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(6, 24),
                // (6,26): error CS1002: ; expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(6, 26),
                // (6,26): error CS1525: Invalid expression term '='
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 26),
                // (6,19): error CS8112: '(x, y)' is a local function and must therefore always have a body.
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "").WithArguments("(x, y)").WithLocation(6, 19),
                // (6,20): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(6, 20),
                // (6,23): error CS0246: The type or namespace name 'y' could not be found (are you missing a using directive or an assembly reference?)
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y").WithArguments("y").WithLocation(6, 23),
                // (6,15): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 15)
            );
        }

        [Fact, WorkItem(15934, "https://github.com/dotnet/roslyn/issues/15934")]
        public void PointerTypeInDeconstruction()
        {
            string source = @"
unsafe class C
{
    static void Main(C c)
    {
        (int* x1, int y1) = c;
        (var* x2, int y2) = c;
        (int*[] x3, int y3) = c;
        (var*[] x4, int y4) = c;
    }
    public void Deconstruct(out dynamic x, out dynamic y)
    {
        x = y = null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                options: TestOptions.UnsafeDebugDll);

            // The precise diagnostics here are not important, and may be sensitive to parser
            // adjustments. This is a test that we don't crash. The errors here are likely to
            // change as we adjust the parser and semantic analysis of error cases.
            comp.VerifyDiagnostics(
                // (6,10): error CS1525: Invalid expression term 'int'
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 10),
                // (8,10): error CS1525: Invalid expression term 'int'
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(8, 10),
                // (8,14): error CS1525: Invalid expression term '['
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "[").WithArguments("[").WithLocation(8, 14),
                // (8,15): error CS0443: Syntax error; value expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(8, 15),
                // (8,17): error CS1026: ) expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x3").WithLocation(8, 17),
                // (8,17): error CS1002: ; expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x3").WithLocation(8, 17),
                // (8,19): error CS1002: ; expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 19),
                // (8,19): error CS1513: } expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 19),
                // (8,27): error CS1002: ; expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 27),
                // (8,27): error CS1513: } expected
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 27),
                // (8,29): error CS1525: Invalid expression term '='
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(8, 29),
                // (9,14): error CS1525: Invalid expression term '['
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "[").WithArguments("[").WithLocation(9, 14),
                // (9,15): error CS0443: Syntax error; value expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(9, 15),
                // (9,17): error CS1026: ) expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x4").WithLocation(9, 17),
                // (9,17): error CS1002: ; expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x4").WithLocation(9, 17),
                // (9,19): error CS1002: ; expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(9, 19),
                // (9,19): error CS1513: } expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(9, 19),
                // (9,27): error CS1002: ; expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 27),
                // (9,27): error CS1513: } expected
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 27),
                // (9,29): error CS1525: Invalid expression term '='
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(9, 29),
                // (6,15): error CS0103: The name 'x1' does not exist in the current context
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 15),
                // (6,19): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y1").WithArguments("dynamic", "int").WithLocation(6, 19),
                // (6,9): error CS8184: A deconstruction cannot mix declarations and expressions on the left-hand-side.
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_MixedDeconstructionUnsupported, "(int* x1, int y1)").WithLocation(6, 9),
                // (7,10): error CS0103: The name 'var' does not exist in the current context
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 10),
                // (7,15): error CS0103: The name 'x2' does not exist in the current context
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(7, 15),
                // (7,19): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y2").WithArguments("dynamic", "int").WithLocation(7, 19),
                // (7,9): error CS8184: A deconstruction cannot mix declarations and expressions on the left-hand-side.
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_MixedDeconstructionUnsupported, "(var* x2, int y2)").WithLocation(7, 9),
                // (8,17): error CS0103: The name 'x3' does not exist in the current context
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(8, 17),
                // (9,10): error CS0103: The name 'var' does not exist in the current context
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(9, 10),
                // (9,17): error CS0103: The name 'x4' does not exist in the current context
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(9, 17),
                // (8,25): warning CS0168: The variable 'y3' is declared but never used
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y3").WithArguments("y3").WithLocation(8, 25),
                // (9,25): warning CS0168: The variable 'y4' is declared but never used
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y4").WithArguments("y4").WithLocation(9, 25)
                );
        }

        [Fact]
        public void DeclarationInsideNameof()
        {
            string source = @"
class Program
{
    static void Main()
    {
        string s = nameof((int x1, var x2) = (1, 2)).ToString();
        string s1 = x1, s2 = x2;
    }
}
";
            var comp = CreateStandardCompilation(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (6,28): error CS8185: A declaration is not allowed in this context.
                //         string s = nameof((int x1, var x2) = (1, 2)).ToString();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x1").WithLocation(6, 28),
                // (6,27): error CS8081: Expression does not have a name.
                //         string s = nameof((int x1, var x2) = (1, 2)).ToString();
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "(int x1, var x2) = (1, 2)").WithLocation(6, 27),
                // (7,21): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x1").WithArguments("int", "string").WithLocation(7, 21),
                // (7,30): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("int", "string").WithLocation(7, 30),
                // (7,21): error CS0165: Use of unassigned local variable 'x1'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(7, 21),
                // (7,30): error CS0165: Use of unassigned local variable 'x2'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(7, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(2, designations.Count());
            var refs = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>();

            var x1 = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("x1", x1.Name);
            Assert.Equal("System.Int32", ((LocalSymbol)x1).Type.ToTestDisplayString());
            Assert.Same(x1, model.GetSymbolInfo(refs.Where(r => r.Identifier.ValueText == "x1").Single()).Symbol);

            var x2 = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("x2", x2.Name);
            Assert.Equal("System.Int32", ((LocalSymbol)x2).Type.ToTestDisplayString());
            Assert.Same(x2, model.GetSymbolInfo(refs.Where(r => r.Identifier.ValueText == "x2").Single()).Symbol);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_01()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var (a,b), var c, int d);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)"),
                // (6,21): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c"),
                // (6,28): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 28),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (a,b), var c, int d)").WithLocation(6, 9),
                // (6,28): error CS0165: Use of unassigned local variable 'd'
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 28)
                );

            StandAlone_01_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        (var (a,b), var c, int d) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_01_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_01_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)a).DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)b).DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)c).DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)d).DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var a, var b), var c, System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_02()
        {
            string source1 = @"
(var (a,b), var c, int d);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,7): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a"),
                // (2,9): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b"),
                // (2,17): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c"),
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)"),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c"),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 20),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (a,b), var c, int d)").WithLocation(2, 1)
                );

            StandAlone_02_VerifySemanticModel(comp1);

            string source2 = @"
(var (a,b), var c, int d) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_02_VerifySemanticModel(comp2);
        }

        private static void StandAlone_02_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var a, var b), var c, System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_03()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var (_, _), var _, int _);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)"),
                // (6,22): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _"),
                // (6,29): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 29),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (_, _), var _, int _)").WithLocation(6, 9)
                );

            StandAlone_03_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        (var (_, _), var _, int _) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_03_VerifySemanticModel(comp2);
        }

        private static void StandAlone_03_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int _", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var, var), var, System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_04()
        {
            string source1 = @"
(var (_, _), var _, int _);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)"),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _"),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 21),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (_, _), var _, int _)").WithLocation(2, 1)
                );

            StandAlone_03_VerifySemanticModel(comp1);

            string source2 = @"
(var (_, _), var _, int _) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_03_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_05()
        {
            string source1 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (a,b), var c);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_05_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (a,b), var c) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_05_VerifySemanticModel(comp2);
        }

        private static void StandAlone_05_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32 a, System.Int32 b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_06()
        {
            string source1 = @"
using var = System.Int32;

(var (a,b), var c);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_06_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

(var (a,b), var c) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_06_VerifySemanticModel(comp2);
        }

        private static void StandAlone_06_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32 a, System.Int32 b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_07()
        {
            string source1 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (_, _), var _);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_07_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (_, _), var _) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_07_VerifySemanticModel(comp2);
        }

        private static void StandAlone_07_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_08()
        {
            string source1 = @"
using var = System.Int32;

(var (_, _), var _);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_07_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

(var (_, _), var _) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_07_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_09()
        {
            string source1 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (a,b), al c);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_09_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (a,b), al c) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_09_VerifySemanticModel(comp2);
        }

        private static void StandAlone_09_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al c", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            Assert.Equal("System.Int32 c", model.GetSymbolInfo(declaration).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_10()
        {
            string source1 = @"
using al = System.Int32;

(al (a,b), al c);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_10_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

(al (a,b), al c) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_10_VerifySemanticModel(comp2);
        }

        private static void StandAlone_10_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al c", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            Assert.Equal("System.Int32 Script.c", model.GetSymbolInfo(declaration).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_11()
        {
            string source1 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (_, _), al _);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_11_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (_, _), al _) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_11_VerifySemanticModel(comp2);
        }

        private static void StandAlone_11_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al _", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_12()
        {
            string source1 = @"
using al = System.Int32;

(al (_, _), al _);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_11_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

(al (_, _), al _) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_11_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_13()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        var (a, b);
        var (c, d)
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (7,19): error CS1002: ; expected
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 19),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (6,14): error CS0103: The name 'a' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 14),
                // (6,17): error CS0103: The name 'b' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(6, 17),
                // (7,9): error CS0103: The name 'var' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 9),
                // (7,14): error CS0103: The name 'c' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(7, 14),
                // (7,17): error CS0103: The name 'd' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(7, 17)
                );

            var tree = comp1.SyntaxTrees.First();
            Assert.False(tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Any());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_14()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        ((var (a,b), var c), int d);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,11): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)").WithLocation(6, 11),
                // (6,22): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c").WithLocation(6, 22),
                // (6,30): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 30),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (a,b), var c), int d)").WithLocation(6, 9),
                // (6,30): error CS0165: Use of unassigned local variable 'd'
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 30)
                );

            StandAlone_14_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        ((var (a,b), var c), int d) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_14_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_14_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)a).DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)b).DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)c).DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)d).DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (a,b), var c), int d)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);


            Assert.Equal("(var (a,b), var c)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_15()
        {
            string source1 = @"
((var (a,b), var c), int d);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,8): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a").WithLocation(2, 8),
                // (2,10): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b").WithLocation(2, 10),
                // (2,18): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c").WithLocation(2, 18),
                // (2,3): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)").WithLocation(2, 3),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c").WithLocation(2, 14),
                // (2,22): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 22),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (a,b), var c), int d)").WithLocation(2, 1)
                );

            StandAlone_15_VerifySemanticModel(comp1);

            string source2 = @"
((var (a,b), var c), int d) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_15_VerifySemanticModel(comp2);
        }

        private static void StandAlone_15_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (a,b), var c), int d)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            Assert.Equal("(var (a,b), var c)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_16()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        ((var (_, _), var _), int _);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,11): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)").WithLocation(6, 11),
                // (6,23): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _").WithLocation(6, 23),
                // (6,31): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 31),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (_, _), var _), int _)").WithLocation(6, 9)
                );

            StandAlone_16_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        ((var (_, _), var _), int _) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_16_VerifySemanticModel(comp2);
        }

        private static void StandAlone_16_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int _", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (_, _), var _), int _)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var, var), var), System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            Assert.Equal("(var (_, _), var _)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var, var), var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_17()
        {
            string source1 = @"
((var (_, _), var _), int _);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,3): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)").WithLocation(2, 3),
                // (2,15): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _").WithLocation(2, 15),
                // (2,23): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 23),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (_, _), var _), int _)").WithLocation(2, 1)
                );

            StandAlone_16_VerifySemanticModel(comp1);

            string source2 = @"
((var (_, _), var _), int _) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_16_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_18()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var ((a,b), c), int d);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((a,b), c)").WithLocation(6, 10),
                // (6,26): error CS8185: A declaration is not allowed in this context.
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 26),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((a,b), c), int d)").WithLocation(6, 9),
                // (6,26): error CS0165: Use of unassigned local variable 'd'
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 26)
                );

            StandAlone_18_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        (var ((a,b), c), int d) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_18_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_18_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)a).DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)b).DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)c).DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, ((LocalSymbol)d).DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((a,b), c)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int d", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_19()
        {
            string source1 = @"
(var ((a,b), c), int d);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,8): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a").WithLocation(2, 8),
                // (2,10): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b").WithLocation(2, 10),
                // (2,14): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c").WithLocation(2, 14),
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((a,b), c)").WithLocation(2, 2),
                // (2,18): error CS8185: A declaration is not allowed in this context.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 18),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((a,b), c), int d)").WithLocation(2, 1)
                );

            StandAlone_19_VerifySemanticModel(comp1);

            string source2 = @"
(var ((a,b), c), int d) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_19_VerifySemanticModel(comp2);
        }

        private static void StandAlone_19_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((a,b), c)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int d", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_20()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var ((_, _), _), int _);
    }
}
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((_, _), _)").WithLocation(6, 10),
                // (6,27): error CS8185: A declaration is not allowed in this context.
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 27),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((_, _), _), int _)").WithLocation(6, 9)
                );

            StandAlone_20_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        (var ((_, _), _), int _) = D;
    }
}
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });

            StandAlone_20_VerifySemanticModel(comp2);
        }

        private static void StandAlone_20_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((_, _), _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var, var), var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Null(typeInfo.Type);
            Assert.Null(typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var, var), var), System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_21()
        {
            string source1 = @"
(var ((_, _), _), int _);
";

            var comp1 = CreateStandardCompilation(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((_, _), _)").WithLocation(2, 2),
                // (2,19): error CS8185: A declaration is not allowed in this context.
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 19),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((_, _), _), int _)").WithLocation(2, 1)
                );

            StandAlone_20_VerifySemanticModel(comp1);

            string source2 = @"
(var ((_, _), _), int _) = D;
";

            var comp2 = CreateStandardCompilation(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Script);

            StandAlone_20_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DiscardVoid_01()
        {
            var source = @"class C
{
    static void Main()
    {
        (_, _) = (1, Main());
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (5,22): error CS8210: A tuple may not contain a value of type 'void'.
                //         (_, _) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 22)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_01()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, void y) = (1, Main());
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (5,17): error CS1547: Keyword 'void' cannot be used in this context
                //         (int x, void y) = (1, Main());
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 17),
                // (5,31): error CS8210: A tuple may not contain a value of type 'void'.
                //         (int x, void y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 31),
                // (5,17): error CS0029: Cannot implicitly convert type 'void' to 'void'
                //         (int x, void y) = (1, Main());
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "void y").WithArguments("void", "void").WithLocation(5, 17)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_02()
        {
            var source = @"class C
{
    static void Main()
    {
        var (x, y) = (1, Main());
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (5,26): error CS8210: A tuple may not contain a value of type 'void'.
                //         var (x, y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 26)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_03()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, void y) = (1, 2);
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (5,17): error CS1547: Keyword 'void' cannot be used in this context
                //         (int x, void y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 17),
                // (5,31): error CS0029: Cannot implicitly convert type 'int' to 'void'
                //         (int x, void y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "void").WithLocation(5, 31),
                // (5,17): error CS0029: Cannot implicitly convert type 'void' to 'void'
                //         (int x, void y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "void y").WithArguments("void", "void").WithLocation(5, 17)
                );
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var two = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "2").Single();
            var type = model.GetTypeInfo(two);
            Assert.Equal(SpecialType.System_Int32, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(two).Kind);
            var symbols = model.GetSymbolInfo(two);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)two.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_04()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, int y) = (1, Main());
    }
}";
            var comp = CreateStandardCompilation(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (5,30): error CS8210: A tuple may not contain a value of type 'void'.
                //         (int x, int y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 30)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }
    }
}
