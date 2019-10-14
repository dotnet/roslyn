// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NativeIntTests : CSharpTestBase
    {
        [Fact]
        public void LanguageVersion()
        {
            var source =
@"interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,5): error CS0246: The type or namespace name 'nint' could not be found (are you missing a using directive or an assembly reference?)
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nint").WithArguments("nint").WithLocation(3, 5),
                // (3,14): error CS0246: The type or namespace name 'nint' could not be found (are you missing a using directive or an assembly reference?)
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nint").WithArguments("nint").WithLocation(3, 14),
                // (3,22): error CS0246: The type or namespace name 'nuint' could not be found (are you missing a using directive or an assembly reference?)
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nuint").WithArguments("nuint").WithLocation(3, 22));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE: Test:
        // - All locations from SyntaxFacts.IsInTypeOnlyContext
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol has the comment "dynamic not allowed as an attribute type". Does that apply to "nint"?
        // - Use-site diagnostics (basically any use-site diagnostics from IntPtr/UIntPtr)

        [Fact]
        public void ClassName()
        {
            var source =
@"class nint
{
}
interface I
{
    nint Add(nint x, nint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AliasName()
        {
            var source =
@"using nint = System.Int16;
interface I
{
    nint Add(nint x, nint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE: Test void* -> nint
        // PROTOTYPE: Test {any} -> nuint
        // PROTOTYPE: Test nint -> {any} and nuint -> {any}
        // PROTOTYPE: Test nint? and nuint?
        // PROTOTYPE: Test explicit conversions.
        public static IEnumerable<object[]> ConversionsData()
        {
            string conv_none =
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";
            string conv_i =
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i
  IL_0002:  ret
}";
            string conv_u =
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u
  IL_0002:  ret
}";
            yield return new object[] { "object", "nint", null };
            yield return new object[] { "string", "nint", null, false };
            yield return new object[] { "bool", "nint", null, false };
            yield return new object[] { "char", "nint", conv_u };
            yield return new object[] { "sbyte", "nint", conv_i };
            yield return new object[] { "byte", "nint", conv_u };
            yield return new object[] { "short", "nint", conv_i };
            yield return new object[] { "ushort", "nint", conv_u };
            yield return new object[] { "int", "nint", conv_i };
            yield return new object[] { "uint", "nint", null };
            yield return new object[] { "long", "nint", null };
            yield return new object[] { "ulong", "nint", null };
            yield return new object[] { "float", "nint", null };
            yield return new object[] { "double", "nint", null };
            yield return new object[] { "decimal", "nint", null };
            yield return new object[] { "System.IntPtr", "nint", conv_none };
            yield return new object[] { "System.UIntPtr", "nint", null, false };
            yield return new object[] { "bool?", "nint", null, false };
            yield return new object[] { "char?", "nint", null };
            yield return new object[] { "sbyte?", "nint", null };
            yield return new object[] { "byte?", "nint", null };
            yield return new object[] { "short?", "nint", null };
            yield return new object[] { "ushort?", "nint", null };
            yield return new object[] { "int?", "nint", null };
            yield return new object[] { "uint?", "nint", null };
            yield return new object[] { "long?", "nint", null };
            yield return new object[] { "ulong?", "nint", null };
            yield return new object[] { "float?", "nint", null };
            yield return new object[] { "double?", "nint", null };
            yield return new object[] { "decimal?", "nint", null };
            yield return new object[] { "System.IntPtr?", "nint", null };
            yield return new object[] { "System.UIntPtr?", "nint", null, false };
        }

        [Theory]
        [MemberData(nameof(ConversionsData))]
        public void Conversions(string sourceType, string destType, string expectedIL, bool hasExplicitCast = true)
        {
            string source =
$@"class Program
{{
    static {destType} Convert({sourceType} value)
    {{
        return value;
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            var expectedDiagnostics = expectedIL == null ?
                new[] { Diagnostic(hasExplicitCast ? ErrorCode.ERR_NoImplicitConvCast : ErrorCode.ERR_NoImplicitConv, "value").WithArguments(sourceType, destType).WithLocation(5, 16) } :
                Array.Empty<DiagnosticDescription>();
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
            var typeInfo = model.GetTypeInfo(expr);

            if (!usesIntPtrOrUIntPtr(sourceType) && !usesIntPtrOrUIntPtr(destType)) // PROTOTYPE: Not distinguishing IntPtr from nint.
            {
                Assert.Equal(sourceType, typeInfo.Type.ToString());
                Assert.Equal(destType, typeInfo.ConvertedType.ToString());
            }

            if (expectedIL != null)
            {
                var verifier = CompileAndVerify(comp);
                verifier.VerifyIL("Program.Convert", expectedIL);
            }

            static bool usesIntPtrOrUIntPtr(string type) => type.Contains("IntPtr");
        }

        // PROTOTYPE: Test conversions from ConversionsBase.HasSpecialIntPtrConversion()
        // (which appear to be allowed for explicit conversions):
        // IntPtr  <---> int
        // IntPtr  <---> long
        // IntPtr  <---> void*
        // UIntPtr <---> uint
        // UIntPtr <---> ulong
        // UIntPtr <---> void*

        // PROTOTYPE: Test unary operators.

        [Fact]
        public void BinaryOperators_NInt()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => x + y;
    static nint Subtract(nint x, nint y) => x - y;
    static nint Multiply(nint x, nint y) => x * y;
    static nint Divide(nint x, nint y) => x / y;
    static nint Mod(nint x, nint y) => x % y;
    static bool Equals(nint x, nint y) => x == y;
    static bool NotEquals(nint x, nint y) => x != y;
    static bool LessThan(nint x, nint y) => x < y;
    static bool LessThanOrEqual(nint x, nint y) => x <= y;
    static bool GreaterThan(nint x, nint y) => x > y;
    static bool GreaterThanOrEqual(nint x, nint y) => x >= y;
    static nint And(nint x, nint y) => x & y;
    static nint Or(nint x, nint y) => x | y;
    static nint Xor(nint x, nint y) => x ^ y;
    static nint ShiftLeft(nint x, int y) => x << y;
    static nint ShiftRight(nint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            // PROTOTYPE: int << int does not require conv.i.
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   31
  IL_0004:  and
  IL_0005:  shl
  IL_0006:  conv.i
  IL_0007:  ret
}");
            // PROTOTYPE: int >> int does not require conv.i.
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   31
  IL_0004:  and
  IL_0005:  shr
  IL_0006:  conv.i
  IL_0007:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => x + y;
    static nuint Subtract(nuint x, nuint y) => x - y;
    static nuint Multiply(nuint x, nuint y) => x * y;
    static nuint Divide(nuint x, nuint y) => x / y;
    static nuint Mod(nuint x, nuint y) => x % y;
    static bool Equals(nuint x, nuint y) => x == y;
    static bool NotEquals(nuint x, nuint y) => x != y;
    static bool LessThan(nuint x, nuint y) => x < y;
    static bool LessThanOrEqual(nuint x, nuint y) => x <= y;
    static bool GreaterThan(nuint x, nuint y) => x > y;
    static bool GreaterThanOrEqual(nuint x, nuint y) => x >= y;
    static nuint And(nuint x, nuint y) => x & y;
    static nuint Or(nuint x, nuint y) => x | y;
    static nuint Xor(nuint x, nuint y) => x ^ y;
    static nuint ShiftLeft(nuint x, int y) => x << y;
    static nuint ShiftRight(nuint x, int y) => x >> y;
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
        Console.WriteLine(Equals(3, 4));
        Console.WriteLine(NotEquals(3, 4));
        Console.WriteLine(LessThan(3, 4));
        Console.WriteLine(LessThanOrEqual(3, 4));
        Console.WriteLine(GreaterThan(3, 4));
        Console.WriteLine(GreaterThanOrEqual(3, 4));
        Console.WriteLine(And(3, 5));
        Console.WriteLine(Or(3, 5));
        Console.WriteLine(Xor(3, 5));
        Console.WriteLine(ShiftLeft(35, 4));
        Console.WriteLine(ShiftRight(35, 4));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1
False
True
True
True
False
False
1
7
6
560
2");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Equals",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.NotEquals",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.LessThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.LessThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.GreaterThan",
@"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  cgt.un
  IL_0004:  ret
}");
            verifier.VerifyIL("Program.GreaterThanOrEqual",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  clt.un
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  ret
}");
            verifier.VerifyIL("Program.And",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  and
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Or",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  or
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Xor",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  xor
  IL_0003:  ret
}");
            // PROTOTYPE: uint << int does not require conv.u.
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   31
  IL_0004:  and
  IL_0005:  shl
  IL_0006:  conv.u
  IL_0007:  ret
}");
            // PROTOTYPE: uint >> int does not require conv.u.
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   31
  IL_0004:  and
  IL_0005:  shr.un
  IL_0006:  conv.u
  IL_0007:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nint Add(nint x, nint y) => checked(x + y);
    static nint Subtract(nint x, nint y) => checked(x - y);
    static nint Multiply(nint x, nint y) => checked(x * y);
    static nint Divide(nint x, nint y) => checked(x / y);
    static nint Mod(nint x, nint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(3, 4));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
-1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  ret
}");
        }

        [Fact]
        public void BinaryOperators_NUInt_Checked()
        {
            var source =
@"using System;
class Program
{
    static nuint Add(nuint x, nuint y) => checked(x + y);
    static nuint Subtract(nuint x, nuint y) => checked(x - y);
    static nuint Multiply(nuint x, nuint y) => checked(x * y);
    static nuint Divide(nuint x, nuint y) => checked(x / y);
    static nuint Mod(nuint x, nuint y) => checked(x % y);
    static void Main()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Subtract(4, 3));
        Console.WriteLine(Multiply(3, 4));
        Console.WriteLine(Divide(5, 2));
        Console.WriteLine(Mod(5, 2));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"7
1
12
2
1");
            verifier.VerifyIL("Program.Add",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Subtract",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Multiply",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Divide",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div.un
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.Mod",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem.un
  IL_0003:  ret
}");
        }

        [Fact]
        public void SizeOf_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.Write(sizeof(IntPtr));
        Console.Write(sizeof(UIntPtr));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,23): error CS0233: 'IntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         Console.Write(sizeof(IntPtr));
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(IntPtr)").WithArguments("System.IntPtr").WithLocation(6, 23),
                // (7,23): error CS0233: 'UIntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         Console.Write(sizeof(UIntPtr));
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(UIntPtr)").WithArguments("System.UIntPtr").WithLocation(7, 23));
        }

        /// <summary>
        /// sizeof(IntPtr) requires compiling with /unsafe.
        /// sizeof(nint) should not.
        /// </summary>
        [Fact]
        public void SizeOf_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Console.Write(sizeof(nint));
        Console.Write(sizeof(nuint));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: $"{IntPtr.Size}{IntPtr.Size}");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  sizeof     ""nint""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  sizeof     ""nuint""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void SizeOf_03()
        {
            var source =
@"class Program
{
    const int C1 = sizeof(nint);
    const int C2 = sizeof(nuint);
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,20): error CS0133: The expression being assigned to 'Program.C1' must be constant
                //     const int C1 = sizeof(nint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nint)").WithArguments("Program.C1").WithLocation(3, 20),
                // (4,20): error CS0133: The expression being assigned to 'Program.C2' must be constant
                //     const int C2 = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nuint)").WithArguments("Program.C2").WithLocation(4, 20));
        }

        [Fact]
        public void SizeOf_04()
        {
            var source =
@"using System.Collections.Generic;
class Program
{
    static IEnumerable<int> F()
    {
        yield return sizeof(nint);
        yield return sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }
    }
}
