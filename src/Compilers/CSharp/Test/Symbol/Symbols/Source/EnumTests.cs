// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public class EnumTests : CSharpTestBase
    {
        // The value of first enumerator, and the value of each successive enumerator 
        [Fact]
        public void ValueOfFirst()
        {
            var text =
@"enum Suits 
{ 
    ValueA, 
    ValueB, 
    ValueC, 
    ValueD 
} 
";
            VerifyEnumsValue(text, "Suits", 0, 1, 2, 3);
        }

        // The value can be explicated initialized 
        [Fact]
        public void ExplicateInit()
        {
            var text =
@"public enum Suits 
{ 
ValueA = -1, 
ValueB = 2, 
ValueC = 3, 
ValueD = 4, 
}; 
";
            VerifyEnumsValue(text, "Suits", -1, 2, 3, 4);
        }

        // The value can be explicated and implicit initialized 
        [Fact]
        public void MixedInit()
        {
            var text =
@"public enum Suits 
{ 
ValueA, 
ValueB = 10, 
ValueC, 
ValueD, 
}; 
";
            VerifyEnumsValue(text, "Suits", 0, 10, 11, 12);
        }

        // Enumerator initializers must be of integral or enumeration type 
        [Fact]
        public void OutOfUnderlyingRange()
        {
            var text =
@"public enum Suits : byte 
{ 
ValueA = ""3"", // Can't implicitly convert 
ValueB = 2.2, // Can't implicitly convert 
ValueC = 257 // Out of underlying range 
}; 
";
            var comp = CreateCompilation(text);
            VerifyEnumsValue(comp, "Suits", SpecialType.System_Byte, null, (byte)2, null);

            comp.VerifyDiagnostics(
                // (3,10): error CS0029: Cannot implicitly convert type 'string' to 'byte'
                // ValueA = "3", // Can't implicitly convert 
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""3""").WithArguments("string", "byte").WithLocation(3, 10),
                // (4,10): error CS0266: Cannot implicitly convert type 'double' to 'byte'. An explicit conversion exists (are you missing a cast?)
                // ValueB = 2.2, // Can't implicitly convert 
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "2.2").WithArguments("double", "byte").WithLocation(4, 10),
                // (5,10): error CS0031: Constant value '257' cannot be converted to a 'byte'
                // ValueC = 257 // Out of underlying range 
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "257").WithArguments("257", "byte").WithLocation(5, 10)
                );

            text =
@"enum Suits : short { a, b, c, d = -65536, e, f }";
            comp = CreateCompilation(text);
            VerifyEnumsValue(comp, "Suits", SpecialType.System_Int16, (short)0, (short)1, (short)2, null, null, null);

            comp.VerifyDiagnostics(
                // (1,35): error CS0031: Constant value '-65536' cannot be converted to a 'short'
                // enum Suits : short { a, b, c, d = -65536, e, f }
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "-65536").WithArguments("-65536", "short").WithLocation(1, 35)
                );
        }

        // Explicit associated value 
        [Fact]
        public void ExplicitAssociated()
        {
            var text =
@"class C<T>
{
    const int field = 100;
    enum TestEnum
    { 
         A, 
         B = A,  // another member
        C = D,  // another member
        D = (byte)11,    // type can be implicitly converted to underlying type
        E = 'a',         // type can be implicitly converted to underlying type
        F = 3 + 5,       // expression
        G = field,       // const field
        TestEnum,        // its own type name
        var,             // contextual keyword
        T,               // Type parameter
     };
     enum EnumB { B = TestEnum.T };
}    
";
            VerifyEnumsValue(text, "C.TestEnum", 0, 0, 11, 11, 97, 8, 100, 101, 102, 103);
            VerifyEnumsValue(text, "C.EnumB", 103);
            text =
@"class c1
{
    public static int StaticField = 10;
    public static readonly int ReadonlyField = 100;
    enum EnumTest { A = StaticField, B = ReadonlyField };
}
";
            VerifyEnumsValue(text, "c1.EnumTest", null, null);
        }

        [WorkItem(539167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539167")]
        // No enum-body 
        [Fact]
        public void NoEnumBody_01()
        {
            var text =
@"enum Figure ;";
            VerifyEnumsValue(text, "Figure");
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NoEnumBody_02()
        {
            var text =
@"enum Figure : int ;";
            VerifyEnumsValue(text, "Figure");
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EnumEOFBeforeMembers()
        {
            var text =
@"enum E";
            VerifyEnumsValue(text, "E");
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_LbraceExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_RbraceExpected });
        }

        [Fact]
        public void EnumEOFWithinMembers()
        {
            var text =
@"enum E {";
            VerifyEnumsValue(text, "E");
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_RbraceExpected });
        }

        // No enum-body 
        [Fact]
        public void NullEnumBody()
        {
            var text =
@"enum Figure { }";
            VerifyEnumsValue(text, "Figure");
        }

        // No identifier
        [Fact]
        public void CS1001ERR_IdentifierExpected_NoIDForEnum()
        {
            var text =
@"enum { One, Two, Three };";
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_IdentifierExpected });
        }

        // Same identifier for enum members
        [Fact]
        public void CS0102ERR_DuplicateNameInClass_SameIDForEnum()
        {
            var text =
@"enum TestEnum { One, One }";
            VerifyEnumsValue(text, "TestEnum", 0, 1);
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInClass });
        }

        // Modifiers for enum
        [Fact]
        public void CS0109WRN_NewNotRequired_ModifiersForEnum()
        {
            var text =
@"class Program
{
    protected enum Figure1 { One = 1 };         // OK
    new public enum Figure2 { Zero = 0 };       // new + protection modifier is OK 
    abstract enum Figure3 { Zero };             // abstract not valid
    private private enum Figure4 { One = 1 };   // Duplicate modifier is not OK
    private public enum Figure5 { };  // More than one protection modifiers is not OK
    sealed enum Figure0 { Zero };               // sealed not valid
    new enum Figure { Zero };                   // OK
}";
            //VerifyEnumsValue(text, "TestEnum", 0, 1);
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,19): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract enum Figure3 { Zero };             // abstract not valid
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Figure3").WithArguments("abstract").WithLocation(5, 19),
                // (6,13): error CS1004: Duplicate 'private' modifier
                //     private private enum Figure4 { One = 1 };   // Duplicate modifier is not OK
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "private").WithArguments("private").WithLocation(6, 13),
                // (7,25): error CS0107: More than one protection modifier
                //     private public enum Figure5 { };  // More than one protection modifiers is not OK
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Figure5").WithLocation(7, 25),
                // (8,17): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed enum Figure0 { Zero };               // sealed not valid
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Figure0").WithArguments("sealed").WithLocation(8, 17),
                // (9,14): warning CS0109: The member 'Program.Figure' does not hide an accessible member. The new keyword is not required.
                //     new enum Figure { Zero };                   // OK
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Figure").WithArguments("Program.Figure").WithLocation(9, 14),
                // (4,21): warning CS0109: The member 'Program.Figure2' does not hide an accessible member. The new keyword is not required.
                //     new public enum Figure2 { Zero = 0 };       // new + protection modifier is OK 
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Figure2").WithArguments("Program.Figure2").WithLocation(4, 21)
                );
        }

        [WorkItem(527757, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527757")]
        // Modifiers for enum member
        [Fact()]
        public void CS1041ERR_IdentifierExpectedKW_ModifiersForEnumMember()
        {
            var text =
@"enum ColorA
{
    public Red
}
";
            //VerifyEnumsValue(text, "ColorA", 0);
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,2): error CS1513: } expected
                // {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (3,12): error CS0116: A namespace does not directly contain members such as fields or methods
                //     public Red
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Red"),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
            text =
@"enum ColorA
{
void goo()
    {}
}
";
            VerifyEnumsValue(text, "ColorA", 0);
            var comp1 = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp1.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_IdentifierExpectedKW },
                new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SyntaxError });
        }

        // Flag Attribute and Enumerate a Enum
        [Fact]
        public void FlagOnEnum()
        {
            var text =
@"
    [System.Flags]
    public enum Suits
    {
        ValueA = 1,
        ValueB = 2,
        ValueC = 4,
        ValueD = 8,
        Combi = ValueA | ValueB
    }
";
            VerifyEnumsValue(text, "Suits", 1, 2, 4, 8, 3);
        }

        // Customer Attribute on Enum declaration
        [Fact]
        public void AttributeOnEnum()
        {
            var text =
@"
  class Attr1 : System.Attribute
    {
    }
    [Attr1]
    enum Figure { One, Two, Three };
";
            VerifyEnumsValue(text, "Figure", 0, 1, 2);
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics());
        }

        // Convert integer to Enum instance
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void ConvertOnEnum()
        {
            var source =
@"
using System;
class c1
{
    public enum Suits
    {
        ValueA = 1,
        ValueB = 2,
        ValueC = 4,
        ValueD = 2,
        ValueE = 2,
    }
    static void Main(string[] args)
    {
        Suits S = (Suits)Enum.ToObject(typeof(Suits), 2);
        Console.WriteLine(S == Suits.ValueB);
        Console.WriteLine(S == Suits.ValueE);
        Suits S1 = (Suits)Enum.ToObject(typeof(Suits), -1);
        Console.WriteLine(S1.ToString()); // -1
    }
}
";
            VerifyEnumsValue(source, "c1.Suits", 1, 2, 4, 2, 2);

            CompileAndVerify(source, expectedOutput: @"True
True
-1
");
        }

        // Enum used in switch
        [Fact]
        public void CS0152ERR_DuplicateCaseLabel_SwitchInEnum()
        {
            var source =
@"
class c1
{
    public enum Suits
    {
        ValueA,
        ValueB,
        ValueC,
    }
    public void main()
    {
        Suits s = Suits.ValueA;
        switch (s)
        {
            case Suits.ValueA:
                break;
            case Suits.ValueB:
                break;
            case Suits.ValueC:
                break;
            default:
                break;
        }
    }
}
";
            var comp = CreateCompilation(source);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics());
            source =
@"
class c1
{
    public enum Suits
    {
        ValueA = 2,
        ValueB,
        ValueC = 2,
    }
    public void main()
    {
        Suits s = Suits.ValueA;
        switch (s)
        {
            case Suits.ValueA:
                break;
            case Suits.ValueB:
                break;
            case Suits.ValueC:
                break;
            default:
                break;
        }
    }
}
";
            comp = CreateCompilation(source);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateCaseLabel });
        }

        // The literal 0 implicitly converts to any enum type. 
        [ClrOnlyFact]
        public void ZeroInEnum()
        {
            var source =
@"
using System;
class c1
{
    enum Gender : byte { Male = 2 }
    static void Main(string[] args)
    {
        Gender s = 0;
        Console.WriteLine(s); 
        s = -0;
        Console.WriteLine(s);
        s = 0.0e+999;
        Console.WriteLine(s);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
0
0
0
");
        }

        // Derived.
        [Fact]
        public void CS0527ERR_NonInterfaceInInterfaceList_DerivedFromEnum()
        {
            var text =
@"
enum A { Red }
struct C : A{}
interface D : A{}
";

            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList });
        }

        // Enums can Not be declared in nested enum declaration
        [Fact]
        public void CS1022ERR_EOFExpected_NestedFromEnum()
        {
            var text =
@"
public enum Num
{
    {	
        public enum Figure { Zero };	
    }	
}
";
            VerifyEnumsValue(text, "Num");
            VerifyEnumsValue(text, "Figure", 0);
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(), new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_EOFExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_IdentifierExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_RbraceExpected });
        }

        // Enums can be declared anywhere
        [Fact]
        public void DeclEnum()
        {
            var text =
@"
namespace ns	
{	
    enum Gender { Male }
}
struct B
{
    enum Gender { Male }
}
";
            VerifyEnumsValue(text, "ns.Gender", 0);
            VerifyEnumsValue(text, "B.Gender", 0);
        }

        // Enums obey local scope rules
        [Fact]
        public void DeclEnum_01()
        {
            var text =
@"
namespace ns
{
    enum E1 { yes = 1, no = yes - 1 };
    public class mine
    {
        public enum E1 { yes = 1, no = yes - 1 };
    }
}
";
            VerifyEnumsValue(text, "ns.E1", 1, 0);
            VerifyEnumsValue(text, "ns.mine.E1", 1, 0);
        }

        // Nullable Enums 
        [Fact]
        public void NullableOfEnum()
        {
            var source =
@"
enum EnumA { };
enum EnumB : long { Num = 1000 };
class c1
{
    static public void Main(string[] args)
    {
        EnumA a = 0;
        EnumA? c = null;
        a = (EnumA)c;
    }
}
";
            VerifyEnumsValue(source, "EnumB", 1000L);
        }

        // Operator on null and enum 
        [Fact]
        public void OperatorOnNullableAndEnum()
        {
            var source =
@"class c1
{
    MyEnum? e = null & MyEnum.One;
}
enum MyEnum
{
    One
}";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (3,17): warning CS0458: The result of the expression is always 'null' of type 'MyEnum?'
                //     MyEnum? e = null & MyEnum.One;
                Diagnostic(ErrorCode.WRN_AlwaysNull, "null & MyEnum.One").WithArguments("MyEnum?")
                );
        }

        [WorkItem(5030, "DevDiv_Projects/Roslyn")]
        // Operator on enum 
        [Fact]
        public void CS0019ERR_BadBinaryOps_OperatorOnEnum()
        {
            var source =
@"
class c1
{
    static public void Main(string[] args)
    {
        Enum1 e1 = e1 + 5L;
        Enum2 e2 = e1 + e2;
        e1 = Enum1.A1 + Enum1.B1;
        bool b1 = e1 == 1;
        bool b7 = e1 == e2;
        e1++;                 // OK
        --e2;                 // OK
        e1 = e1 ^ Enum1.A1;   // OK
        e1 ^= Enum1.B1;       // OK
        var s = sizeof(Enum1); // OK
    }
}
public enum Enum1 { A1 = 1, B1 = 2 };
public enum Enum2 : byte { A2, B2 };
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (6,20): error CS0019: Operator '+' cannot be applied to operands of type 'Enum1' and 'long'
                //         Enum1 e1 = e1 + 5L;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "e1 + 5L").WithArguments("+", "Enum1", "long"),

                // (7,20): error CS0019: Operator '+' cannot be applied to operands of type 'Enum1' and 'Enum2'
                //         Enum2 e2 = e1 + e2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "e1 + e2").WithArguments("+", "Enum1", "Enum2"),

                // (8,14): error CS0019: Operator '+' cannot be applied to operands of type 'Enum1' and 'Enum1'
                //         e1 = Enum1.A1 + Enum1.B1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "Enum1.A1 + Enum1.B1").WithArguments("+", "Enum1", "Enum1"),

                // (9,19): error CS0019: Operator '==' cannot be applied to operands of type 'Enum1' and 'int'
                //         bool b1 = e1 == 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "e1 == 1").WithArguments("==", "Enum1", "int"),

                // (10,19): error CS0019: Operator '==' cannot be applied to operands of type 'Enum1' and 'Enum2'
                //         bool b7 = e1 == e2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "e1 == e2").WithArguments("==", "Enum1", "Enum2"),

                // (6,20): error CS0165: Use of unassigned local variable 'e1'
                //         Enum1 e1 = e1 + 5L;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "e1").WithArguments("e1"),

                // (7,25): error CS0165: Use of unassigned local variable 'e2'
                //         Enum2 e2 = e1 + e2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "e2").WithArguments("e2"),

                // (15,13): warning CS0219: The variable 's' is assigned but its value is never used
                //         var s = sizeof(Enum1); // OK
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s"));
        }

        [WorkItem(5030, "DevDiv_Projects/Roslyn")]
        // Operator on enum member 
        [ClrOnlyFact]
        public void OperatorOnEnumMember()
        {
            var source =
@"
using System;
class c1
{
    static public void Main(string[] args)
    {
        E s = E.one;
        var b1 = E.three > E.two;
        var b2 = E.three < E.two;
        var b3 = E.three == E.two;
        var b4 = E.three != E.two;
        var b5 = s > E.two;
        var b6 = s < E.two;
        var b7 = s == E.two;
        var b8 = s != E.two;
        Console.WriteLine(b1);
        Console.WriteLine(b2);
        Console.WriteLine(b3);
        Console.WriteLine(b4);
        Console.WriteLine(b5);
        Console.WriteLine(b6);
        Console.WriteLine(b7);
        Console.WriteLine(b8);
    }
}
public enum E { one = 1, two = 2, three = 3 };
";
            CompileAndVerify(source, expectedOutput: @"
True
False
False
True
False
True
False
True
");
        }

        // CLS-Compliant
        [Fact]
        public void CS3009WRN_CLS_BadBase_CLSCompliantOnEnum()
        {
            var text =
@"
[assembly: System.CLSCompliant(true)]
public class c1
{
    public enum COLORS : uint { RED, GREEN, BLUE };
}
";

            var comp = CreateCompilation(text);
            VerifyEnumsValue(comp, "c1.COLORS", SpecialType.System_UInt32, 0u, 1u, 2u);
            comp.VerifyDiagnostics(
                // (5,17): warning CS3009: 'c1.COLORS': base type 'uint' is not CLS-compliant
                //     public enum COLORS : uint { RED, GREEN, BLUE };
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "COLORS").WithArguments("c1.COLORS", "uint"));
        }

        [WorkItem(539178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539178")]
        // No underlying type after ':' 
        [Fact]
        public void CS3031ERR_TypeExpected_NoUnderlyingTypeForEnum()
        {
            var text =
@"enum Figure : { One, Two, Three }
";
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_TypeExpected },
                new ErrorDescription { Code = (int)ErrorCode.ERR_IntegralTypeExpected });
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int32, 0, 1, 2);
        }

        [Fact]
        public void CS1008ERR_IntegralTypeExpected()
        {
            var text =
@"enum Figure : System.Int16 { One, Two, Three }
";
            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics()); // ok
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int16, (short)0, (short)1, (short)2);

            text =
@"class C { }
enum Figure : C { One, Two, Three }
";
            comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_IntegralTypeExpected });
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int32, 0, 1, 2);
        }

        // 'partial' as Enum name
        [Fact]
        public void partialAsEnumName()
        {
            var text =
@"
partial class EnumPartial
{
    internal enum @partial
    { }
    partial M;
}
";
            VerifyEnumsValue(text, "EnumPartial.partial");
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,13): warning CS0169: The field 'EnumPartial.M' is never used
                //     partial M;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "M").WithArguments("EnumPartial.M")
                );

            var classEnum = comp.SourceModule.GlobalNamespace.GetMembers("EnumPartial").Single() as NamedTypeSymbol;
            var member = classEnum.GetMembers("M").Single() as FieldSymbol;
            Assert.Equal(TypeKind.Enum, member.Type.TypeKind);
        }

        // Enum as an optional parameter 
        [Fact]
        public void CS1763ERR_NotNullRefDefaultParameter_EnumAsOptionalParameter()
        {
            var text =
@"
enum ABC { a, b, c }
class c1
{
    public int Goo(ABC o = ABC.a | ABC.b)
    {
        return 0;
    }
    public int Moo(object o = ABC.a)
    {
        return 1;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (9,27): error CS1763: 'o' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                //     public int Moo(object o = ABC.a)
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "o").WithArguments("o", "object"));
        }

        [WorkItem(540765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540765")]
        [Fact]
        public void TestInitializeWithEnumMemberEnumConst()
        {
            var text = @"
class Test
{
    public enum E0 : short
    {
        Member1
    }
    const E0 e0 = E0.Member1;
    public enum E1
    {
        Member1, Member2 = e1, Member3 = e0
    }
    const E1 e1 = E1.Member1;
}";
            CreateCompilation(text).VerifyDiagnostics(); // No Errors
        }

        [WorkItem(540765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540765")]
        [Fact]
        public void TestInitializeWithEnumMemberEnumConst2()
        {
            var text = @"
class Test
{
    const E1 e = E1.Member1;
    public enum E1
    {
        Member2 = e, Member1
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
            // (4,14): error CS0110: The evaluation of the constant value for 'Test.e' involves a circular definition
                Diagnostic(ErrorCode.ERR_CircConstValue, "e").WithArguments("Test.e")); // No Errors
        }

        [WorkItem(540765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540765")]
        [Fact]
        public void TestInitializeWithEnumMemberEnumConst3()
        {
            var text = @"
class Test
{
    const E1 e = E1.Member1;
    public enum E1
    {
        Member1,
        Member2 = e //fine
    }
    public enum E2
    {
        Member = e //fine
    }
    public enum E3
    {
        Member = (E3)e //CS0266
    }
    public enum E4
    {
        Member = (e) //fine
    }
    public enum E5
    {
        Member = (e) + 1 //fine
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
            // (16,18): error CS0266: Cannot implicitly convert type 'Test.E3' to 'int'. An explicit conversion exists (are you missing a cast?)
            //         Member = (E3)e
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(E3)e").WithArguments("Test.E3", "int"));
        }

        [WorkItem(540771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540771")]
        [Fact]
        public void TestUseEnumMemberFromBaseGenericType()
        {
            var text = @"
class Base<T, U>
{
    public enum Enum1
    {
        A, B, C
    }
}
class Derived<T, U> : Base<U, T>
{
    const Enum1 E = Enum1.C;
}";
            CreateCompilation(text).VerifyDiagnostics(); // No Errors
        }

        [WorkItem(667303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667303")]
        [Fact]
        public void TestFullNameForEnumBaseType()
        {
            var text =
@"public enum Works1 : byte {} 
public enum Works2 : sbyte {} 
public enum Works3 : short {} 
public enum Works4 : ushort {} 
public enum Works5 : int {} 
public enum Works6 : uint {} 
public enum Works7 : long {} 
public enum Works8 : ulong {} 
public enum Breaks1 : System.Byte {} 
public enum Breaks2 : System.SByte {} 
public enum Breaks3 : System.Int16 {} 
public enum Breaks4 : System.UInt16 {} 
public enum Breaks5 : System.Int32 {} 
public enum Breaks6 : System.UInt32 {} 
public enum Breaks7 : System.Int64 {} 
public enum Breaks8 : System.UInt64 {}";
            CreateCompilation(text).VerifyDiagnostics(); // No Errors
        }

        [WorkItem(667303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667303")]
        [Fact]
        public void TestBadEnumBaseType()
        {
            var text =
@"public enum Breaks1 : string {} 
public enum Breaks2 : System.String {}";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,23): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // public enum Breaks1 : string {} 
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "string").WithLocation(1, 23),
                // (2,23): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // public enum Breaks2 : System.String {}
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "System.String").WithLocation(2, 23)
                );
        }

        [WorkItem(750553, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750553")]
        [Fact]
        public void InvalidEnumUnderlyingType()
        {
            var text =
@"enum E1 : int[] { }
enum E2 : int* { }
enum E3 : dynamic { }
class C<T> { enum E4 : T { } }
";
            var compilation = CreateEmptyCompilation(text, new[] { MscorlibRef });
            compilation.VerifyDiagnostics(
                // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // enum E2 : int* { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 11),
                // (2,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E2 : int* { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "int*").WithLocation(2, 11),
                // (3,11): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // enum E3 : dynamic { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(3, 11),
                // (3,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E3 : dynamic { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "dynamic").WithLocation(3, 11),
                // (1,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E1 : int[] { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "int[]").WithLocation(1, 11),
                // (4,24): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // class C<T> { enum E4 : T { } }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "T").WithLocation(4, 24)
                );

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var diagnostics = model.GetDeclarationDiagnostics();
            diagnostics.Verify(
                // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // enum E2 : int* { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 11),
                // (2,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E2 : int* { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "int*").WithLocation(2, 11),
                // (1,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E1 : int[] { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "int[]").WithLocation(1, 11),
                // (3,11): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                // enum E3 : dynamic { }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(3, 11),
                // (3,11): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum E3 : dynamic { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "dynamic").WithLocation(3, 11),
                // (4,24): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // class C<T> { enum E4 : T { } }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "T").WithLocation(4, 24)
                );

            var decls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<EnumDeclarationSyntax>().ToArray();
            Assert.Equal(4, decls.Length);

            foreach (var decl in decls)
            {
                var symbol = model.GetDeclaredSymbol(decl);
                var type = symbol.EnumUnderlyingType;
                Assert.Equal(SpecialType.System_Int32, type.SpecialType);
            }
        }

        private List<Symbol> VerifyEnumsValue(string text, string enumName, params object[] expectedEnumValues)
        {
            var comp = CreateCompilation(text);
            var specialType = SpecialType.System_Int32;
            if (expectedEnumValues.Length > 0)
            {
                var first = expectedEnumValues.First();
                if (first != null && first.GetType() == typeof(long))
                    specialType = SpecialType.System_Int64;
            }
            return VerifyEnumsValue(comp, enumName, specialType, expectedEnumValues);
        }

        private List<Symbol> VerifyEnumsValue(CSharpCompilation comp, string enumName, SpecialType underlyingType, params object[] expectedEnumValues)
        {
            var global = comp.SourceModule.GlobalNamespace;
            var symEnum = GetSymbolByFullName(comp, enumName) as NamedTypeSymbol;
            Assert.NotNull(symEnum);

            var type = symEnum.EnumUnderlyingType;
            Assert.NotNull(type);
            Assert.Equal(underlyingType, type.SpecialType);

            var fields = symEnum.GetMembers().OfType<FieldSymbol>().Cast<Symbol>().ToList();

            Assert.Equal(expectedEnumValues.Length, fields.Count);
            var count = 0;
            foreach (var item in fields)
            {
                var field = item as FieldSymbol;
                Assert.Equal(expectedEnumValues[count++], field.ConstantValue);
            }

            return fields;
        }

        private static Symbol GetSymbolByFullName(CSharpCompilation compilation, string memberName)
        {
            string[] names = memberName.Split('.');
            Symbol currentSymbol = compilation.GlobalNamespace;
            foreach (var name in names)
            {
                Assert.True(currentSymbol is NamespaceOrTypeSymbol, string.Format("{0} does not have members", currentSymbol.ToTestDisplayString()));
                var currentContainer = (NamespaceOrTypeSymbol)currentSymbol;
                var members = currentContainer.GetMembers(name);
                Assert.True(members.Length > 0, string.Format("No members named {0} inside {1}", name, currentSymbol.ToTestDisplayString()));
                Assert.True(members.Length <= 1, string.Format("Multiple members named {0} inside {1}", name, currentSymbol.ToTestDisplayString()));
                currentSymbol = members.First();
            }
            return currentSymbol;
        }
    }
}
