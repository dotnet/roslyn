// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class CompilationErrorTests : CompilingTestBase
    {
        [Fact]
        public void UserDefinedOperatorCollisionErrors()
        {
            // DELIBERATE SPEC VIOLATION: 
            //
            // Though the spec does not say so, the native and Roslyn compilers both enforce
            // signature uniqueness rules as though the user-defined operators were declared
            // in source as their underlying "op_whatever" methods. With one exception:
            // user-defined conversion operators are allowed to overload on return type.
            //
            // See the tests below for the details.

            var text =
@"
class C
{
    // These two collide by signature
    public static C operator + (C c1, C c2) { return c1; }
    public static C op_Addition(C c1, C c2) { return c1; } 

    // But this one does not.
    public static int op_Addition(int c1, int c2) { return c2; }
    
    // These two collide by name; the error is reported on the latter.
    public static C operator - (C c1, C c2) { return c1; }
    public int op_Subtraction;

    // These two collide by name; the error is reported on the latter.
    public int op_Division;
    public static C operator / (C c1, C c2) { return c1; }

    // These two collide by name; the error is reported on the operator
    // regardless of which one comes first.
    public static C operator * (C c1, C c2) { return c1; }
    private class op_Multiply {} 

    // These two collide because they have different return types but
    // identical parameters types. The behavior is that the error
    // given says that they collide because of the name op_Modulus,
    // rather than there being a custom error message as there is for
    // the following scenario.
    public static C operator % (C c1, C c2) { return c1; }
    public static int operator % (C c1, C c2) { return 0; }

    // These collide because operators do not consider whether they
    // are implicit or explicit, only what the target and source types are.
    public static implicit operator string(C c) { return null; }
    public static explicit operator string(C c) { return null; }
}
class D
{
    // These do *not* collide even though they have identical names
    // and differ in return type.
    public static implicit operator string(D d) { return null; }
    public static implicit operator int(D d) { return 0; }
}
class E
{
    // However, these *do* collide because one of them is a method
    // and the other is treated as a method for the purpose of finding
    // a collision. The fact that the return types differ is relevant.
    public static implicit operator string(E e) { return null; }
    public static int op_Implicit(E e) { return 0; }
    // But this one does not collide, even though *as a conversion* 
    // the two conversions would collide:
    public static string op_Explicit(E e) { return null; }
}
class F
{
    // These do collide, same as above. Note that the error is
    // reported on the latter.
    public static int op_Implicit(F f) { return 0; }
    public static implicit operator string(F f) { return null; }
}
class G
{
    // User-defined conversions collide with other members.
    public static implicit operator string(G g) { return null; }
    public int op_Implicit { get; set; }
}
class H
{
    // User-defined conversions collide with nested types
    public static implicit operator string(H h) { return null; }
    private class op_Implicit {}
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,21): error CS0111: Type 'C' already defines a member called 'op_Addition' with the same parameter types
                //     public static C op_Addition(C c1, C c2) { return c1; } 
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Addition").WithArguments("op_Addition", "C"),
                // (13,16): error CS0102: The type 'C' already contains a definition for 'op_Subtraction'
                //     public int op_Subtraction;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "op_Subtraction").WithArguments("C", "op_Subtraction"),
                // (17,30): error CS0102: The type 'C' already contains a definition for 'op_Division'
                //     public static C operator / (C c1, C c2) { return c1; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "/").WithArguments("C", "op_Division"),
                // (21,30): error CS0102: The type 'C' already contains a definition for 'op_Multiply'
                //     public static C operator * (C c1, C c2) { return c1; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "*").WithArguments("C", "op_Multiply"),
                // (30,32): error CS0111: Type 'C' already defines a member called 'op_Modulus' with the same parameter types
                //     public static int operator % (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "%").WithArguments("op_Modulus", "C"),
                // (35,37): error CS0557: Duplicate user-defined conversion in type 'C'
                //     public static explicit operator string(C c) { return null; }
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "string").WithArguments("C"),
                // (50,23): error CS0111: Type 'E' already defines a member called 'op_Implicit' with the same parameter types
                //     public static int op_Implicit(E e) { return 0; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Implicit").WithArguments("op_Implicit", "E"),
                // (60,37): error CS0111: Type 'F' already defines a member called 'op_Implicit' with the same parameter types
                //     public static implicit operator string(F f) { return null; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "string").WithArguments("op_Implicit", "F"),
                // (66,16): error CS0102: The type 'G' already contains a definition for 'op_Implicit'
                //     public int op_Implicit { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "op_Implicit").WithArguments("G", "op_Implicit"),
                // (71,37): error CS0102: The type 'H' already contains a definition for 'op_Implicit'
                //     public static implicit operator string(H h) { return null; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "string").WithArguments("H", "op_Implicit"),
                // (13,16): warning CS0649: Field 'C.op_Subtraction' is never assigned to, and will always have its default value 0
                //     public int op_Subtraction;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "op_Subtraction").WithArguments("C.op_Subtraction", "0"),
                // (16,16): warning CS0649: Field 'C.op_Division' is never assigned to, and will always have its default value 0
                //     public int op_Division;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "op_Division").WithArguments("C.op_Division", "0")
                );
        }

        [Fact]
        public void UserDefinedOperatorBodyErrors()
        {
            // User-defined operators have the same behavior as other methods;
            // for example, they must return a value compatible with their declared
            // return type and have an unreachable end point.

            var text =
@"
class C
{
    public class D {}
    public static D operator + (C c1, C c2) { return c1; }
    public static explicit operator int (C c) { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (5,54): error CS0029: Cannot implicitly convert type 'C' to 'C.D'
//     public static D operator + (C c1, C c2) { return c1; }
Diagnostic(ErrorCode.ERR_NoImplicitConv, "c1").WithArguments("C", "C.D"),

// (6,37): error CS0161: 'C.explicit operator int(C)': not all code paths return a value
//     public static explicit operator int (C c) { }
Diagnostic(ErrorCode.ERR_ReturnExpected, "int").WithArguments("C.explicit operator int(C)")
                );
        }

        [Fact]
        public void UserDefinedOperatorModifierErrors()
        {
            var text =
@"
partial class C
{
    partial public static int operator + (C c1, C c2) { return 0; }
    abstract public int operator - (C c1, C c2) { return 0; }
    sealed public int operator << (C c1, int c2) { return 0; }
    new public static int operator >> (C c1, int c2) { return 0; }
    readonly public static int operator * (C c1, C c2) { return 0; }
    volatile public static int operator % (C c1, C c2) { return 0; }
    virtual public int operator - (C c1) { return 0; }
    override public int operator ~ (C c1) { return 0; }
    public public public static int operator & (C c1, C c2) { return 0; }
    extern static public int operator ^ (C c1, C c2) { return 1; }
    static public int operator + (C c1);
    new public static int operator >>> (C c1, int c2) { return 0; }
}
";

            // UNDONE: May be unsafe

            // UNDONE: Native compiler squiggles "operator +". Roslyn squiggles "operator". But perhaps
            // UNDONE: the better thing to actually squiggle is the offending token.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public static int operator + (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public static int operator + (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (5,34): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract public int operator - (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("abstract").WithLocation(5, 34),
                // (5,34): error CS0558: User-defined operator 'C.operator -(C, C)' must be declared static and public
                //     abstract public int operator - (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "-").WithArguments("C.operator -(C, C)").WithLocation(5, 34),
                // (6,32): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed public int operator << (C c1, int c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "<<").WithArguments("sealed").WithLocation(6, 32),
                // (6,32): error CS0558: User-defined operator 'C.operator <<(C, int)' must be declared static and public
                //     sealed public int operator << (C c1, int c2) { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "<<").WithArguments("C.operator <<(C, int)").WithLocation(6, 32),
                // (7,36): error CS0106: The modifier 'new' is not valid for this item
                //     new public static int operator >> (C c1, int c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, ">>").WithArguments("new").WithLocation(7, 36),
                // (8,41): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly public static int operator * (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("readonly").WithLocation(8, 41),
                // (9,41): error CS0106: The modifier 'volatile' is not valid for this item
                //     volatile public static int operator % (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "%").WithArguments("volatile").WithLocation(9, 41),
                // (10,33): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual public int operator - (C c1) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(10, 33),
                // (10,33): error CS0558: User-defined operator 'C.operator -(C)' must be declared static and public
                //     virtual public int operator - (C c1) { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "-").WithArguments("C.operator -(C)").WithLocation(10, 33),
                // (11,34): error CS0106: The modifier 'override' is not valid for this item
                //     override public int operator ~ (C c1) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("override").WithLocation(11, 34),
                // (11,34): error CS0558: User-defined operator 'C.operator ~(C)' must be declared static and public
                //     override public int operator ~ (C c1) { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "~").WithArguments("C.operator ~(C)").WithLocation(11, 34),
                // (12,12): error CS1004: Duplicate 'public' modifier
                //     public public public static int operator & (C c1, C c2) { return 0; }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(12, 12),
                // (13,39): error CS0179: 'C.operator ^(C, C)' cannot be extern and declare a body
                //     extern static public int operator ^ (C c1, C c2) { return 1; }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "^").WithArguments("C.operator ^(C, C)").WithLocation(13, 39),
                // (14,32): error CS0501: 'C.operator +(C)' must declare a body because it is not marked abstract, extern, or partial
                //     static public int operator + (C c1);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "+").WithArguments("C.operator +(C)").WithLocation(14, 32),
                // (15,36): error CS0106: The modifier 'new' is not valid for this item
                //     new public static int operator >>> (C c1, int c2) { return 0; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, ">>>").WithArguments("new").WithLocation(15, 36)
                );
        }

        [Fact]
        public void UserDefinedOperatorAccessibilityErrors()
        {
            var text =
@"
public class C
{
    private class D {}

    public static D operator + (C c1, C c2) { return null; }
    public static int operator - (C c, D d) { return 0; }
    public static explicit operator C(D d) { return null; }
}
";
            // UNDONE: Roslyn squiggles just the "operator"; Native compiler squiggles the "operator +".
            // UNDONE: Consider matching the native compiler behavior, or, even better, squiggle the
            // UNDONE: offending type.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (6,30): error CS0056: Inconsistent accessibility: return type 'C.D' is less accessible than operator 'C.operator +(C, C)'
//     public static D operator + (C c1, C c2) { return null; }
Diagnostic(ErrorCode.ERR_BadVisOpReturn, "+").WithArguments("C.operator +(C, C)", "C.D"),

// (7,32): error CS0057: Inconsistent accessibility: parameter type 'C.D' is less accessible than operator 'C.operator -(C, C.D)'
//     public static int operator - (C c, D d) { return 0; }
Diagnostic(ErrorCode.ERR_BadVisOpParam, "-").WithArguments("C.operator -(C, C.D)", "C.D"),

// (8,37): error CS0057: Inconsistent accessibility: parameter type 'C.D' is less accessible than operator 'C.explicit operator C(C.D)'
//     public static explicit operator C(D d) { return null; }
Diagnostic(ErrorCode.ERR_BadVisOpParam, "C").WithArguments("C.explicit operator C(C.D)", "C.D")
                );
        }

        [Fact]
        public void UserDefinedOperatorConstraintViolationErrors()
        {
            var text =
@"
public class C
{
    public class D<T> where T : class {}
    public static D<int> operator + (C c1, C c2) { return null; }
    public static int operator - (C c, D<double> d) { return 0; }
    public static explicit operator C(D<decimal> d) { return null; }
}
";
            // UNDONE: The squiggles are not in the ideal places here. The native compiler squiggles the entire
            // UNDONE: name of the operator in all cases, which is bad. Roslyn squiggles the "operator"
            // UNDONE: token if the return type is bad and the parameter name if the parameter type is 
            // UNDONE: bad. This seems no better; surely the right thing to squiggle is the offending type.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (5,35): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C.D<T>'
//     public static D<int> operator + (C c1, C c2) { return null; }
Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "+").WithArguments("C.D<T>", "T", "int"),

// (6,50): error CS0452: The type 'double' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C.D<T>'
//     public static int operator - (C c, D<double> d) { return 0; }
Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "d").WithArguments("C.D<T>", "T", "double"),

// (7,50): error CS0452: The type 'decimal' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C.D<T>'
//     public static explicit operator C(D<decimal> d) { return null; }
Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "d").WithArguments("C.D<T>", "T", "decimal")
                );
        }
    }
}
