// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class EnumTests : CSharpTestBase
    {
        // Common example. (Associated Dev10 errors are indicated for cases
        // where the underlying type of the enum cannot be determined.)
        private const string ExampleSource =
@"class C
{
    static void F(E e) { }
    static void Main()
    {
        E e = E.A; // Dev10: 'E.A' is not supported by the language
        F(e); // Dev10: no error
        F(E.B); // Dev10: 'E.B' is not supported by the language
        int b = (int)e; // Dev10: cannot convert 'E' to 'int'
        e = e + 1; // Dev10: operator '+' cannot be applied to operands of type 'E' and 'int'
        e = ~e; // Dev10: operator '~' cannot be applied to operand of type 'E'
    }
}";

        [ClrOnlyFact]
        public void EnumWithPrivateInstanceField()
        {
            // No errors.
            CompileWithCustomILSource(
                ExampleSource,
@".class public E extends [mscorlib]System.Enum
{
    .field private specialname rtspecialname int16 _val
	.field public static literal valuetype E A = int16(31)
	.field public static literal valuetype E B = int16(32)
}");
        }

        [Fact]
        public void EnumWithNoInstanceFields()
        {
            EnumWithBogusUnderlyingType(
@".class public E extends [mscorlib]System.Enum
{
	.field public static literal valuetype E A = int32(0)
	.field public static literal valuetype E B = int32(1)
}");
        }

        [Fact]
        public void EnumWithMultipleInstanceFields()
        {
            // Note that Dev10 reports a single error for this case:
            // "'E' is a type not supported by the language" for "static void F(E e) { }"
            EnumWithBogusUnderlyingType(
@".class public E extends [mscorlib]System.Enum
{
    .field public specialname rtspecialname int32 _val1
    .field public specialname rtspecialname int32 _val2
	.field public static literal valuetype E A = int32(0)
	.field public static literal valuetype E B = int32(1)
}");
        }

        [Fact]
        public void EnumWithPrivateLiterals()
        {
            CreateCompilationWithILAndMscorlib40(
@"class C
{
    static void F(E e) { }
    static void Main()
    {
        F(E.A);
        F(E.B);
        F(E.C);
    }
}",
@".class public E extends [mscorlib]System.Enum
{
    .field public specialname rtspecialname int16 _val
	.field public static literal valuetype E A = int16(0)
	.field private static literal valuetype E B = int16(1)
	.field assembly static literal valuetype E C = int16(2)
}").VerifyDiagnostics(
            // (7,13): error CS0117: 'E' does not contain a definition for 'B'
            //         F(E.B);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "B").WithArguments("E", "B"),
            // (8,13): error CS0117: 'E' does not contain a definition for 'C'
            //         F(E.C);
            Diagnostic(ErrorCode.ERR_NoSuchMember, "C").WithArguments("E", "C"));
        }

        [Fact]
        public void EnumUnsupportedUnderlyingType()
        {
            // bool
            EnumWithBogusUnderlyingType(
@".class public E extends [mscorlib]System.Enum
{
    .field public specialname rtspecialname bool value__
	.field public static literal valuetype E A = bool(false)
	.field public static literal valuetype E B = bool(true)
}");
            // char
            EnumWithBogusUnderlyingType(
@".class public E extends [mscorlib]System.Enum
{
    .field public specialname rtspecialname char value__
	.field public static literal valuetype E A = char(0)
	.field public static literal valuetype E B = char(1)
}");
            // string
            EnumWithBogusUnderlyingType(
@".class public E extends [mscorlib]System.Enum
{
    .field public specialname rtspecialname string _val
	.field public static literal valuetype E A = int16(0)
	.field public static literal valuetype E B = int16(1)
}");
        }

        private void EnumWithBogusUnderlyingType(string ilSource)
        {
            CreateCompilationWithILAndMscorlib40(ExampleSource, ilSource).VerifyDiagnostics(
                // (6,15): error CS0570: 'E.A' is not supported by the language
                //         E e = E.A; // Dev10: 'E.A' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "E.A").WithArguments("E.A"),
                // (8,11): error CS0570: 'E.B' is not supported by the language
                //         F(E.B); // Dev10: 'E.B' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "E.B").WithArguments("E.B"),
                // (10,13): error CS0019: Operator '+' cannot be applied to operands of type 'E' and 'int'
                //         e = e + 1; // Dev10: operator '+' cannot be applied to operands of type 'E' and 'int'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "e + 1").WithArguments("+", "E", "int"),
                // (11,13): error CS0023: Operator '~' cannot be applied to operand of type 'E'
                //         e = ~e; // Dev10: operator '~' cannot be applied to operand of type 'E'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "~e").WithArguments("~", "E"));
        }

        [Fact]
        public void CycleOneMember()
        {
            var source =
@"enum E
{
    A = A,
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                //     A = A,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(3, 5));
        }

        [Fact]
        public void CycleTwoMembers()
        {
            var source =
@"enum E
{
    A = B + 1,
    B = A + 1,
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                //     A = B + 1,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(3, 5));
        }

        [Fact]
        public void TwoConnectedCycles()
        {
            var source =
@"enum E
{
    A = B | C,
    B = A + 1,
    C = A + 2,
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                //     A = B | C,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(3, 5));
        }

        [Fact]
        public void CyclesAndConnectedFields()
        {
            var source =
@"enum E
{
    A = A | B,
    B = C,
    C = D,
    D = D
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                //     A = A | B,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(3, 5),
                // (6,5): error CS0110: The evaluation of the constant value for 'E.D' involves a circular definition
                //     D = D
                Diagnostic(ErrorCode.ERR_CircConstValue, "D").WithArguments("E.D").WithLocation(6, 5));
        }

        [Fact]
        public void DependenciesTwoEnums()
        {
            var source =
@"enum E
{
    A = F.A,
    B = F.B + A
}
enum F
{
    A = 1,
    B = E.A
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void MultipleCircularDependencies()
        {
            var source =
@"enum E
{
    A = B + F.B,
    B = A + F.A,
}
enum F
{
    A = E.B + 1,
    B = A + 1,
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.A' involves a circular definition
                //     A = B + F.B,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E.A").WithLocation(3, 5),
                // (4,5): error CS0110: The evaluation of the constant value for 'E.B' involves a circular definition
                //     B = A + F.A,
                Diagnostic(ErrorCode.ERR_CircConstValue, "B").WithArguments("E.B").WithLocation(4, 5));
        }

        [Fact]
        public void CircularDefinitionManyMembers_Implicit()
        {
            // enum E { M0 = Mn + 1, M1, ..., Mn, }
            // Dev12 reports "CS1647: An expression is too long or complex to compile" at ~5600 members.
            var source = GenerateEnum(10000, (i, n) => (i == 0) ? string.Format("M{0} + 1", n - 1) : "");
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.M0' involves a circular definition
                //     M0 = M5999 + 1,
                Diagnostic(ErrorCode.ERR_CircConstValue, "M0").WithArguments("E.M0").WithLocation(3, 5));
        }

        [WorkItem(843037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843037")]
        [ConditionalFact(typeof(NoIOperationValidation))]
        public void CircularDefinitionManyMembers_Explicit()
        {
            // enum E { M0 = Mn + 1, M1 = M0 + 1, ..., Mn = Mn-1 + 1, }
            // Dev12 reports "CS1647: An expression is too long or complex to compile" at ~1600 members.
            var source = GenerateEnum(10000, (i, n) => string.Format("M{0} + 1", (i == 0) ? (n - 1) : (i - 1)));
            CreateCompilation(source).VerifyDiagnostics(
                // (3,5): error CS0110: The evaluation of the constant value for 'E.M0' involves a circular definition
                //     M0 = M1999 + 1,
                Diagnostic(ErrorCode.ERR_CircConstValue, "M0").WithArguments("E.M0").WithLocation(3, 5));
        }

        [WorkItem(843037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843037")]
        [ConditionalFact(typeof(NoIOperationValidation))]
        public void InvertedDefinitionManyMembers_Explicit()
        {
            // enum E { M0 = M1 - 1, M1 = M2 - 1, ..., Mn = n, }
            // Dev12 reports "CS1647: An expression is too long or complex to compile" at ~1500 members.
            var source = GenerateEnum(10000, (i, n) => (i < n - 1) ? string.Format("M{0} - 1", i + 1) : i.ToString());
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// Generate "enum E { M0 = ..., M1 = ..., ..., Mn = ... }".
        /// </summary>
        private static string GenerateEnum(int n, Func<int, int, string> getMemberValue)
        {
            var builder = new StringBuilder();
            builder.AppendLine("enum E");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.Append(string.Format("    M{0}", i));
                var value = getMemberValue(i, n);
                if (!string.IsNullOrEmpty(value))
                {
                    builder.Append(" = ");
                    builder.Append(value);
                }
                builder.AppendLine(",");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        [WorkItem(45625, "https://github.com/dotnet/roslyn/issues/45625")]
        [Fact]
        public void UseSiteError_01()
        {
            var sourceA =
@"public class A { }";
            var comp = CreateCompilation(sourceA, assemblyName: "UseSiteError_sourceA");
            var refA = comp.EmitToImageReference();

            var sourceB =
@"public class B<T>
{
    public enum E { F }
}
public class C
{
    public const B<A>.E F = default;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            var refB = comp.EmitToImageReference();

            var sourceC =
@"class Program
{
    static void Main()
    {
        const int x = (int)~C.F;
        System.Console.WriteLine(x);
    }
}";
            comp = CreateCompilation(sourceC, references: new[] { refB });
            comp.VerifyDiagnostics(
                // (5,31): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         const int x = (int)~C.F;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F").WithArguments("A", "UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 31)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.BitwiseNotExpression);
            var value = model.GetConstantValue(expr);
            Assert.False(value.HasValue);
        }

        [WorkItem(45625, "https://github.com/dotnet/roslyn/issues/45625")]
        [Fact]
        public void UseSiteError_02()
        {
            var sourceA =
@"public class A { }";
            var comp = CreateCompilation(sourceA, assemblyName: "UseSiteError_sourceA");
            var refA = comp.EmitToImageReference();

            var sourceB =
@"public class B<T>
{
    public enum E { F }
}
public class C
{
    public const B<A>.E F = default;
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            var refB = comp.EmitToImageReference();

            var sourceC =
@"class Program
{
    static void Main()
    {
        var x = ~C.F;
        System.Console.WriteLine(x);
    }
}";
            comp = CreateCompilation(sourceC, references: new[] { refB }, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (5,20): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var x = ~C.F;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F").WithArguments("A", "UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 20)
                );

            comp = CreateCompilation(sourceC, references: new[] { refB }, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,20): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var x = ~C.F;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F").WithArguments("A", "UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 20)
                );
        }

        [Fact]
        public void PartialPublicEnum()
        {
            CreateCompilation("partial public enum E { }").VerifyDiagnostics(
                // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public enum E { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1),
                // (1,21): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public enum E { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(1, 21));
        }
    }
}
