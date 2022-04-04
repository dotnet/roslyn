// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class UnsignedRightShiftTests : CSharpTestBase
    {
        [Theory]
        [InlineData("char", "char", "ushort.MaxValue", "int", "uint")]
        [InlineData("char", "sbyte", "ushort.MaxValue", "int", "uint")]
        [InlineData("char", "short", "ushort.MaxValue", "int", "uint")]
        [InlineData("char", "int", "ushort.MaxValue", "int", "uint")]
        [InlineData("char", "byte", "ushort.MaxValue", "int", "uint")]
        [InlineData("char", "ushort", "ushort.MaxValue", "int", "uint")]
        [InlineData("sbyte", "char", "sbyte.MinValue", "int", "uint")]
        [InlineData("sbyte", "sbyte", "sbyte.MinValue", "int", "uint")]
        [InlineData("sbyte", "short", "sbyte.MinValue", "int", "uint")]
        [InlineData("sbyte", "int", "sbyte.MinValue", "int", "uint")]
        [InlineData("sbyte", "byte", "sbyte.MinValue", "int", "uint")]
        [InlineData("sbyte", "ushort", "sbyte.MinValue", "int", "uint")]
        [InlineData("short", "char", "short.MinValue", "int", "uint")]
        [InlineData("short", "sbyte", "short.MinValue", "int", "uint")]
        [InlineData("short", "short", "short.MinValue", "int", "uint")]
        [InlineData("short", "int", "short.MinValue", "int", "uint")]
        [InlineData("short", "byte", "short.MinValue", "int", "uint")]
        [InlineData("short", "ushort", "short.MinValue", "int", "uint")]
        [InlineData("int", "char", "int.MinValue", "int", "uint")]
        [InlineData("int", "sbyte", "int.MinValue", "int", "uint")]
        [InlineData("int", "short", "int.MinValue", "int", "uint")]
        [InlineData("int", "int", "int.MinValue", "int", "uint")]
        [InlineData("int", "byte", "int.MinValue", "int", "uint")]
        [InlineData("int", "ushort", "int.MinValue", "int", "uint")]
        [InlineData("long", "char", "long.MinValue", "long", "ulong")]
        [InlineData("long", "sbyte", "long.MinValue", "long", "ulong")]
        [InlineData("long", "short", "long.MinValue", "long", "ulong")]
        [InlineData("long", "int", "long.MinValue", "long", "ulong")]
        [InlineData("long", "byte", "long.MinValue", "long", "ulong")]
        [InlineData("long", "ushort", "long.MinValue", "long", "ulong")]
        [InlineData("byte", "char", "byte.MaxValue", "int", "uint")]
        [InlineData("byte", "sbyte", "byte.MaxValue", "int", "uint")]
        [InlineData("byte", "short", "byte.MaxValue", "int", "uint")]
        [InlineData("byte", "int", "byte.MaxValue", "int", "uint")]
        [InlineData("byte", "byte", "byte.MaxValue", "int", "uint")]
        [InlineData("byte", "ushort", "byte.MaxValue", "int", "uint")]
        [InlineData("ushort", "char", "ushort.MaxValue", "int", "uint")]
        [InlineData("ushort", "sbyte", "ushort.MaxValue", "int", "uint")]
        [InlineData("ushort", "short", "ushort.MaxValue", "int", "uint")]
        [InlineData("ushort", "int", "ushort.MaxValue", "int", "uint")]
        [InlineData("ushort", "byte", "ushort.MaxValue", "int", "uint")]
        [InlineData("ushort", "ushort", "ushort.MaxValue", "int", "uint")]
        [InlineData("uint", "char", "uint.MaxValue", "uint", "uint")]
        [InlineData("uint", "sbyte", "uint.MaxValue", "uint", "uint")]
        [InlineData("uint", "short", "uint.MaxValue", "uint", "uint")]
        [InlineData("uint", "int", "uint.MaxValue", "uint", "uint")]
        [InlineData("uint", "byte", "uint.MaxValue", "uint", "uint")]
        [InlineData("uint", "ushort", "uint.MaxValue", "uint", "uint")]
        [InlineData("ulong", "char", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("ulong", "sbyte", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("ulong", "short", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("ulong", "int", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("ulong", "byte", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("ulong", "ushort", "ulong.MaxValue", "ulong", "ulong")]
        [InlineData("nint", "char", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nint", "sbyte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nint", "short", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nint", "int", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nint", "byte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nint", "ushort", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint", "nuint")]
        [InlineData("nuint", "char", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        [InlineData("nuint", "sbyte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        [InlineData("nuint", "short", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        [InlineData("nuint", "int", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        [InlineData("nuint", "byte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        [InlineData("nuint", "ushort", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint", "nuint")]
        public void BuiltIn_01(string left, string right, string leftValue, string result, string unsignedResult)
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        var x = (" + left + @")" + leftValue + @";
        var y = (" + right + @")1;
        var z1 = x >>> y;
        var z2 = x >> y;

        if (z1 == unchecked((" + result + @")(((" + unsignedResult + @")(" + result + @")x) >> y))) System.Console.WriteLine(""Passed 1"");

        if (x > 0 ? z1 == z2 : z1 != z2) System.Console.WriteLine(""Passed 2"");

        if (z1.GetType() == z2.GetType() && z1.GetType() == typeof(" + result + @")) System.Console.WriteLine(""Passed 3"");
    }

    " + result + @" Test1(" + left + @" x, " + right + @" y) => x >>> y; 
    " + result + @" Test2(" + left + @" x, " + right + @" y) => x >> y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
Passed 1
Passed 2
Passed 3
").VerifyDiagnostics();

            string actualIL = verifier.VisualizeIL("C.Test2");
            verifier.VerifyIL("C.Test1", actualIL.Replace("shr.un", "shr").Replace("shr", "shr.un"));

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var unsignedShift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.UnsignedRightShiftExpression).First();
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.RightShiftExpression).First();

            Assert.Equal("x >>> y", unsignedShift.ToString());
            Assert.Equal("x >> y", shift.ToString());

            var unsignedShiftSymbol = (IMethodSymbol)model.GetSymbolInfo(unsignedShift).Symbol;
            var shiftSymbol = (IMethodSymbol)model.GetSymbolInfo(shift).Symbol;
            Assert.Equal("op_UnsignedRightShift", unsignedShiftSymbol.Name);
            Assert.Equal("op_RightShift", shiftSymbol.Name);

            Assert.Same(shiftSymbol.ReturnType, unsignedShiftSymbol.ReturnType);
            Assert.Same(shiftSymbol.Parameters[0].Type, unsignedShiftSymbol.Parameters[0].Type);
            Assert.Same(shiftSymbol.Parameters[1].Type, unsignedShiftSymbol.Parameters[1].Type);
            Assert.Same(shiftSymbol.ContainingSymbol, unsignedShiftSymbol.ContainingSymbol);
        }

        [Theory]
        [InlineData("object", "object")]
        [InlineData("object", "string")]
        [InlineData("object", "bool")]
        [InlineData("object", "char")]
        [InlineData("object", "sbyte")]
        [InlineData("object", "short")]
        [InlineData("object", "int")]
        [InlineData("object", "long")]
        [InlineData("object", "byte")]
        [InlineData("object", "ushort")]
        [InlineData("object", "uint")]
        [InlineData("object", "ulong")]
        [InlineData("object", "nint")]
        [InlineData("object", "nuint")]
        [InlineData("object", "float")]
        [InlineData("object", "double")]
        [InlineData("object", "decimal")]
        [InlineData("string", "object")]
        [InlineData("string", "string")]
        [InlineData("string", "bool")]
        [InlineData("string", "char")]
        [InlineData("string", "sbyte")]
        [InlineData("string", "short")]
        [InlineData("string", "int")]
        [InlineData("string", "long")]
        [InlineData("string", "byte")]
        [InlineData("string", "ushort")]
        [InlineData("string", "uint")]
        [InlineData("string", "ulong")]
        [InlineData("string", "nint")]
        [InlineData("string", "nuint")]
        [InlineData("string", "float")]
        [InlineData("string", "double")]
        [InlineData("string", "decimal")]
        [InlineData("bool", "object")]
        [InlineData("bool", "string")]
        [InlineData("bool", "bool")]
        [InlineData("bool", "char")]
        [InlineData("bool", "sbyte")]
        [InlineData("bool", "short")]
        [InlineData("bool", "int")]
        [InlineData("bool", "long")]
        [InlineData("bool", "byte")]
        [InlineData("bool", "ushort")]
        [InlineData("bool", "uint")]
        [InlineData("bool", "ulong")]
        [InlineData("bool", "nint")]
        [InlineData("bool", "nuint")]
        [InlineData("bool", "float")]
        [InlineData("bool", "double")]
        [InlineData("bool", "decimal")]
        [InlineData("char", "object")]
        [InlineData("char", "string")]
        [InlineData("char", "bool")]
        [InlineData("char", "long")]
        [InlineData("char", "uint")]
        [InlineData("char", "ulong")]
        [InlineData("char", "nint")]
        [InlineData("char", "nuint")]
        [InlineData("char", "float")]
        [InlineData("char", "double")]
        [InlineData("char", "decimal")]
        [InlineData("sbyte", "object")]
        [InlineData("sbyte", "string")]
        [InlineData("sbyte", "bool")]
        [InlineData("sbyte", "long")]
        [InlineData("sbyte", "uint")]
        [InlineData("sbyte", "ulong")]
        [InlineData("sbyte", "nint")]
        [InlineData("sbyte", "nuint")]
        [InlineData("sbyte", "float")]
        [InlineData("sbyte", "double")]
        [InlineData("sbyte", "decimal")]
        [InlineData("short", "object")]
        [InlineData("short", "string")]
        [InlineData("short", "bool")]
        [InlineData("short", "long")]
        [InlineData("short", "uint")]
        [InlineData("short", "ulong")]
        [InlineData("short", "nint")]
        [InlineData("short", "nuint")]
        [InlineData("short", "float")]
        [InlineData("short", "double")]
        [InlineData("short", "decimal")]
        [InlineData("int", "object")]
        [InlineData("int", "string")]
        [InlineData("int", "bool")]
        [InlineData("int", "long")]
        [InlineData("int", "uint")]
        [InlineData("int", "ulong")]
        [InlineData("int", "nint")]
        [InlineData("int", "nuint")]
        [InlineData("int", "float")]
        [InlineData("int", "double")]
        [InlineData("int", "decimal")]
        [InlineData("long", "object")]
        [InlineData("long", "string")]
        [InlineData("long", "bool")]
        [InlineData("long", "long")]
        [InlineData("long", "uint")]
        [InlineData("long", "ulong")]
        [InlineData("long", "nint")]
        [InlineData("long", "nuint")]
        [InlineData("long", "float")]
        [InlineData("long", "double")]
        [InlineData("long", "decimal")]
        [InlineData("byte", "object")]
        [InlineData("byte", "string")]
        [InlineData("byte", "bool")]
        [InlineData("byte", "long")]
        [InlineData("byte", "uint")]
        [InlineData("byte", "ulong")]
        [InlineData("byte", "nint")]
        [InlineData("byte", "nuint")]
        [InlineData("byte", "float")]
        [InlineData("byte", "double")]
        [InlineData("byte", "decimal")]
        [InlineData("ushort", "object")]
        [InlineData("ushort", "string")]
        [InlineData("ushort", "bool")]
        [InlineData("ushort", "long")]
        [InlineData("ushort", "uint")]
        [InlineData("ushort", "ulong")]
        [InlineData("ushort", "nint")]
        [InlineData("ushort", "nuint")]
        [InlineData("ushort", "float")]
        [InlineData("ushort", "double")]
        [InlineData("ushort", "decimal")]
        [InlineData("uint", "object")]
        [InlineData("uint", "string")]
        [InlineData("uint", "bool")]
        [InlineData("uint", "long")]
        [InlineData("uint", "uint")]
        [InlineData("uint", "ulong")]
        [InlineData("uint", "nint")]
        [InlineData("uint", "nuint")]
        [InlineData("uint", "float")]
        [InlineData("uint", "double")]
        [InlineData("uint", "decimal")]
        [InlineData("ulong", "object")]
        [InlineData("ulong", "string")]
        [InlineData("ulong", "bool")]
        [InlineData("ulong", "long")]
        [InlineData("ulong", "uint")]
        [InlineData("ulong", "ulong")]
        [InlineData("ulong", "nint")]
        [InlineData("ulong", "nuint")]
        [InlineData("ulong", "float")]
        [InlineData("ulong", "double")]
        [InlineData("ulong", "decimal")]
        [InlineData("nint", "object")]
        [InlineData("nint", "string")]
        [InlineData("nint", "bool")]
        [InlineData("nint", "long")]
        [InlineData("nint", "uint")]
        [InlineData("nint", "ulong")]
        [InlineData("nint", "nint")]
        [InlineData("nint", "nuint")]
        [InlineData("nint", "float")]
        [InlineData("nint", "double")]
        [InlineData("nint", "decimal")]
        [InlineData("nuint", "object")]
        [InlineData("nuint", "string")]
        [InlineData("nuint", "bool")]
        [InlineData("nuint", "long")]
        [InlineData("nuint", "uint")]
        [InlineData("nuint", "ulong")]
        [InlineData("nuint", "nint")]
        [InlineData("nuint", "nuint")]
        [InlineData("nuint", "float")]
        [InlineData("nuint", "double")]
        [InlineData("nuint", "decimal")]
        [InlineData("float", "object")]
        [InlineData("float", "string")]
        [InlineData("float", "bool")]
        [InlineData("float", "char")]
        [InlineData("float", "sbyte")]
        [InlineData("float", "short")]
        [InlineData("float", "int")]
        [InlineData("float", "long")]
        [InlineData("float", "byte")]
        [InlineData("float", "ushort")]
        [InlineData("float", "uint")]
        [InlineData("float", "ulong")]
        [InlineData("float", "nint")]
        [InlineData("float", "nuint")]
        [InlineData("float", "float")]
        [InlineData("float", "double")]
        [InlineData("float", "decimal")]
        [InlineData("double", "object")]
        [InlineData("double", "string")]
        [InlineData("double", "bool")]
        [InlineData("double", "char")]
        [InlineData("double", "sbyte")]
        [InlineData("double", "short")]
        [InlineData("double", "int")]
        [InlineData("double", "long")]
        [InlineData("double", "byte")]
        [InlineData("double", "ushort")]
        [InlineData("double", "uint")]
        [InlineData("double", "ulong")]
        [InlineData("double", "nint")]
        [InlineData("double", "nuint")]
        [InlineData("double", "float")]
        [InlineData("double", "double")]
        [InlineData("double", "decimal")]
        [InlineData("decimal", "object")]
        [InlineData("decimal", "string")]
        [InlineData("decimal", "bool")]
        [InlineData("decimal", "char")]
        [InlineData("decimal", "sbyte")]
        [InlineData("decimal", "short")]
        [InlineData("decimal", "int")]
        [InlineData("decimal", "long")]
        [InlineData("decimal", "byte")]
        [InlineData("decimal", "ushort")]
        [InlineData("decimal", "uint")]
        [InlineData("decimal", "ulong")]
        [InlineData("decimal", "nint")]
        [InlineData("decimal", "nuint")]
        [InlineData("decimal", "float")]
        [InlineData("decimal", "double")]
        [InlineData("decimal", "decimal")]
        public void BuiltIn_02(string left, string right)
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        " + left + @" x = default;
        " + right + @" y = default;
        var z1 = x >> y;
        var z2 = x >>> y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (8,18): error CS0019: Operator '>>' cannot be applied to operands of type 'object' and 'object'
                //         var z1 = x >> y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >> y").WithArguments(">>", left, right).WithLocation(8, 18),
                // (9,18): error CS0019: Operator '>>>' cannot be applied to operands of type 'object' and 'object'
                //         var z2 = x >>> y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>> y").WithArguments(">>>", left, right).WithLocation(9, 18)
                );
        }

        [Theory]
        [InlineData("char", "char", "ushort.MaxValue", "int")]
        [InlineData("char", "sbyte", "ushort.MaxValue", "int")]
        [InlineData("char", "short", "ushort.MaxValue", "int")]
        [InlineData("char", "int", "ushort.MaxValue", "int")]
        [InlineData("char", "byte", "ushort.MaxValue", "int")]
        [InlineData("char", "ushort", "ushort.MaxValue", "int")]
        [InlineData("sbyte", "char", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "sbyte", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "short", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "int", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "byte", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "ushort", "sbyte.MinValue", "int")]
        [InlineData("short", "char", "short.MinValue", "int")]
        [InlineData("short", "sbyte", "short.MinValue", "int")]
        [InlineData("short", "short", "short.MinValue", "int")]
        [InlineData("short", "int", "short.MinValue", "int")]
        [InlineData("short", "byte", "short.MinValue", "int")]
        [InlineData("short", "ushort", "short.MinValue", "int")]
        [InlineData("int", "char", "int.MinValue", "int")]
        [InlineData("int", "sbyte", "int.MinValue", "int")]
        [InlineData("int", "short", "int.MinValue", "int")]
        [InlineData("int", "int", "int.MinValue", "int")]
        [InlineData("int", "byte", "int.MinValue", "int")]
        [InlineData("int", "ushort", "int.MinValue", "int")]
        [InlineData("long", "char", "long.MinValue", "long")]
        [InlineData("long", "sbyte", "long.MinValue", "long")]
        [InlineData("long", "short", "long.MinValue", "long")]
        [InlineData("long", "int", "long.MinValue", "long")]
        [InlineData("long", "byte", "long.MinValue", "long")]
        [InlineData("long", "ushort", "long.MinValue", "long")]
        [InlineData("byte", "char", "byte.MaxValue", "int")]
        [InlineData("byte", "sbyte", "byte.MaxValue", "int")]
        [InlineData("byte", "short", "byte.MaxValue", "int")]
        [InlineData("byte", "int", "byte.MaxValue", "int")]
        [InlineData("byte", "byte", "byte.MaxValue", "int")]
        [InlineData("byte", "ushort", "byte.MaxValue", "int")]
        [InlineData("ushort", "char", "ushort.MaxValue", "int")]
        [InlineData("ushort", "sbyte", "ushort.MaxValue", "int")]
        [InlineData("ushort", "short", "ushort.MaxValue", "int")]
        [InlineData("ushort", "int", "ushort.MaxValue", "int")]
        [InlineData("ushort", "byte", "ushort.MaxValue", "int")]
        [InlineData("ushort", "ushort", "ushort.MaxValue", "int")]
        [InlineData("uint", "char", "uint.MaxValue", "uint")]
        [InlineData("uint", "sbyte", "uint.MaxValue", "uint")]
        [InlineData("uint", "short", "uint.MaxValue", "uint")]
        [InlineData("uint", "int", "uint.MaxValue", "uint")]
        [InlineData("uint", "byte", "uint.MaxValue", "uint")]
        [InlineData("uint", "ushort", "uint.MaxValue", "uint")]
        [InlineData("ulong", "char", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "sbyte", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "short", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "int", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "byte", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "ushort", "ulong.MaxValue", "ulong")]
        [InlineData("nint", "char", "int.MaxValue", "nint")]
        [InlineData("nint", "sbyte", "int.MaxValue", "nint")]
        [InlineData("nint", "short", "int.MaxValue", "nint")]
        [InlineData("nint", "int", "int.MaxValue", "nint")]
        [InlineData("nint", "byte", "int.MaxValue", "nint")]
        [InlineData("nint", "ushort", "int.MaxValue", "nint")]
        [InlineData("nuint", "char", "uint.MaxValue", "nuint")]
        [InlineData("nuint", "sbyte", "uint.MaxValue", "nuint")]
        [InlineData("nuint", "short", "uint.MaxValue", "nuint")]
        [InlineData("nuint", "int", "uint.MaxValue", "nuint")]
        [InlineData("nuint", "byte", "uint.MaxValue", "nuint")]
        [InlineData("nuint", "ushort", "uint.MaxValue", "nuint")]
        [InlineData("nint", "char", "0", "nint")]
        [InlineData("nint", "sbyte", "0", "nint")]
        [InlineData("nint", "short", "0", "nint")]
        [InlineData("nint", "int", "0", "nint")]
        [InlineData("nint", "byte", "0", "nint")]
        [InlineData("nint", "ushort", "0", "nint")]
        public void BuiltInConstantFolding_01(string left, string right, string leftValue, string result)
        {

            var source1 =
@"
class C
{
    static void Main()
    {
        const " + left + @" x = (" + left + @")(" + leftValue + @");
        const " + result + @" y = x >>> (" + right + @")1;
        var z1 = x;
        var z2 = z1 >>> (" + right + @")1;

        if (y == z2) System.Console.WriteLine(""Passed 1"");
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(compilation1, expectedOutput: @"
Passed 1
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("int.MinValue")]
        [InlineData("-1")]
        [InlineData("-100")]
        public void BuiltInConstantFolding_02(string leftValue)
        {

            var source1 =
@"
#pragma warning disable CS0219 // The variable 'y' is assigned but its value is never used

class C
{
    static void Main()
    {
        const nint x = (nint)(" + leftValue + @");
        const nint y = x >>> 1;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (9,24): error CS0133: The expression being assigned to 'y' must be constant
                //         const nint y = x >>> 1;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "x >>> 1").WithArguments("y").WithLocation(9, 24)
                );
        }

        [Theory]
        [InlineData("char", "char", "ushort.MaxValue")]
        [InlineData("char", "sbyte", "ushort.MaxValue")]
        [InlineData("char", "short", "ushort.MaxValue")]
        [InlineData("char", "int", "ushort.MaxValue")]
        [InlineData("char", "byte", "ushort.MaxValue")]
        [InlineData("char", "ushort", "ushort.MaxValue")]
        [InlineData("sbyte", "char", "sbyte.MinValue")]
        [InlineData("sbyte", "sbyte", "sbyte.MinValue")]
        [InlineData("sbyte", "short", "sbyte.MinValue")]
        [InlineData("sbyte", "int", "sbyte.MinValue")]
        [InlineData("sbyte", "byte", "sbyte.MinValue")]
        [InlineData("sbyte", "ushort", "sbyte.MinValue")]
        [InlineData("short", "char", "short.MinValue")]
        [InlineData("short", "sbyte", "short.MinValue")]
        [InlineData("short", "short", "short.MinValue")]
        [InlineData("short", "int", "short.MinValue")]
        [InlineData("short", "byte", "short.MinValue")]
        [InlineData("short", "ushort", "short.MinValue")]
        [InlineData("int", "char", "int.MinValue")]
        [InlineData("int", "sbyte", "int.MinValue")]
        [InlineData("int", "short", "int.MinValue")]
        [InlineData("int", "int", "int.MinValue")]
        [InlineData("int", "byte", "int.MinValue")]
        [InlineData("int", "ushort", "int.MinValue")]
        [InlineData("long", "char", "long.MinValue")]
        [InlineData("long", "sbyte", "long.MinValue")]
        [InlineData("long", "short", "long.MinValue")]
        [InlineData("long", "int", "long.MinValue")]
        [InlineData("long", "byte", "long.MinValue")]
        [InlineData("long", "ushort", "long.MinValue")]
        [InlineData("byte", "char", "byte.MaxValue")]
        [InlineData("byte", "sbyte", "byte.MaxValue")]
        [InlineData("byte", "short", "byte.MaxValue")]
        [InlineData("byte", "int", "byte.MaxValue")]
        [InlineData("byte", "byte", "byte.MaxValue")]
        [InlineData("byte", "ushort", "byte.MaxValue")]
        [InlineData("ushort", "char", "ushort.MaxValue")]
        [InlineData("ushort", "sbyte", "ushort.MaxValue")]
        [InlineData("ushort", "short", "ushort.MaxValue")]
        [InlineData("ushort", "int", "ushort.MaxValue")]
        [InlineData("ushort", "byte", "ushort.MaxValue")]
        [InlineData("ushort", "ushort", "ushort.MaxValue")]
        [InlineData("uint", "char", "uint.MaxValue")]
        [InlineData("uint", "sbyte", "uint.MaxValue")]
        [InlineData("uint", "short", "uint.MaxValue")]
        [InlineData("uint", "int", "uint.MaxValue")]
        [InlineData("uint", "byte", "uint.MaxValue")]
        [InlineData("uint", "ushort", "uint.MaxValue")]
        [InlineData("ulong", "char", "ulong.MaxValue")]
        [InlineData("ulong", "sbyte", "ulong.MaxValue")]
        [InlineData("ulong", "short", "ulong.MaxValue")]
        [InlineData("ulong", "int", "ulong.MaxValue")]
        [InlineData("ulong", "byte", "ulong.MaxValue")]
        [InlineData("ulong", "ushort", "ulong.MaxValue")]
        [InlineData("nint", "char", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "sbyte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "short", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "int", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "byte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "ushort", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nuint", "char", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "sbyte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "short", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "int", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "byte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "ushort", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        public void BuiltIn_CompoundAssignment_01(string left, string right, string leftValue)
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        var x = (" + left + @")" + leftValue + @";
        var y = (" + right + @")1;
        var z1 = x;
        z1 >>>= y;

        if (z1 == (" + left + @")(x >>> y)) System.Console.WriteLine(""Passed 1"");

        z1 >>= y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(compilation1, expectedOutput: @"
Passed 1
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var unsignedShift = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.UnsignedRightShiftAssignmentExpression).First();
            var shift = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.RightShiftAssignmentExpression).First();

            Assert.Equal("z1 >>>= y", unsignedShift.ToString());
            Assert.Equal("z1 >>= y", shift.ToString());

            var unsignedShiftSymbol = (IMethodSymbol)model.GetSymbolInfo(unsignedShift).Symbol;
            var shiftSymbol = (IMethodSymbol)model.GetSymbolInfo(shift).Symbol;
            Assert.Equal("op_UnsignedRightShift", unsignedShiftSymbol.Name);
            Assert.Equal("op_RightShift", shiftSymbol.Name);

            Assert.Same(shiftSymbol.ReturnType, unsignedShiftSymbol.ReturnType);
            Assert.Same(shiftSymbol.Parameters[0].Type, unsignedShiftSymbol.Parameters[0].Type);
            Assert.Same(shiftSymbol.Parameters[1].Type, unsignedShiftSymbol.Parameters[1].Type);
            Assert.Same(shiftSymbol.ContainingSymbol, unsignedShiftSymbol.ContainingSymbol);
        }

        [Theory]
        [InlineData("object", "object")]
        [InlineData("object", "string")]
        [InlineData("object", "bool")]
        [InlineData("object", "char")]
        [InlineData("object", "sbyte")]
        [InlineData("object", "short")]
        [InlineData("object", "int")]
        [InlineData("object", "long")]
        [InlineData("object", "byte")]
        [InlineData("object", "ushort")]
        [InlineData("object", "uint")]
        [InlineData("object", "ulong")]
        [InlineData("object", "nint")]
        [InlineData("object", "nuint")]
        [InlineData("object", "float")]
        [InlineData("object", "double")]
        [InlineData("object", "decimal")]
        [InlineData("string", "object")]
        [InlineData("string", "string")]
        [InlineData("string", "bool")]
        [InlineData("string", "char")]
        [InlineData("string", "sbyte")]
        [InlineData("string", "short")]
        [InlineData("string", "int")]
        [InlineData("string", "long")]
        [InlineData("string", "byte")]
        [InlineData("string", "ushort")]
        [InlineData("string", "uint")]
        [InlineData("string", "ulong")]
        [InlineData("string", "nint")]
        [InlineData("string", "nuint")]
        [InlineData("string", "float")]
        [InlineData("string", "double")]
        [InlineData("string", "decimal")]
        [InlineData("bool", "object")]
        [InlineData("bool", "string")]
        [InlineData("bool", "bool")]
        [InlineData("bool", "char")]
        [InlineData("bool", "sbyte")]
        [InlineData("bool", "short")]
        [InlineData("bool", "int")]
        [InlineData("bool", "long")]
        [InlineData("bool", "byte")]
        [InlineData("bool", "ushort")]
        [InlineData("bool", "uint")]
        [InlineData("bool", "ulong")]
        [InlineData("bool", "nint")]
        [InlineData("bool", "nuint")]
        [InlineData("bool", "float")]
        [InlineData("bool", "double")]
        [InlineData("bool", "decimal")]
        [InlineData("char", "object")]
        [InlineData("char", "string")]
        [InlineData("char", "bool")]
        [InlineData("char", "long")]
        [InlineData("char", "uint")]
        [InlineData("char", "ulong")]
        [InlineData("char", "nint")]
        [InlineData("char", "nuint")]
        [InlineData("char", "float")]
        [InlineData("char", "double")]
        [InlineData("char", "decimal")]
        [InlineData("sbyte", "object")]
        [InlineData("sbyte", "string")]
        [InlineData("sbyte", "bool")]
        [InlineData("sbyte", "long")]
        [InlineData("sbyte", "uint")]
        [InlineData("sbyte", "ulong")]
        [InlineData("sbyte", "nint")]
        [InlineData("sbyte", "nuint")]
        [InlineData("sbyte", "float")]
        [InlineData("sbyte", "double")]
        [InlineData("sbyte", "decimal")]
        [InlineData("short", "object")]
        [InlineData("short", "string")]
        [InlineData("short", "bool")]
        [InlineData("short", "long")]
        [InlineData("short", "uint")]
        [InlineData("short", "ulong")]
        [InlineData("short", "nint")]
        [InlineData("short", "nuint")]
        [InlineData("short", "float")]
        [InlineData("short", "double")]
        [InlineData("short", "decimal")]
        [InlineData("int", "object")]
        [InlineData("int", "string")]
        [InlineData("int", "bool")]
        [InlineData("int", "long")]
        [InlineData("int", "uint")]
        [InlineData("int", "ulong")]
        [InlineData("int", "nint")]
        [InlineData("int", "nuint")]
        [InlineData("int", "float")]
        [InlineData("int", "double")]
        [InlineData("int", "decimal")]
        [InlineData("long", "object")]
        [InlineData("long", "string")]
        [InlineData("long", "bool")]
        [InlineData("long", "long")]
        [InlineData("long", "uint")]
        [InlineData("long", "ulong")]
        [InlineData("long", "nint")]
        [InlineData("long", "nuint")]
        [InlineData("long", "float")]
        [InlineData("long", "double")]
        [InlineData("long", "decimal")]
        [InlineData("byte", "object")]
        [InlineData("byte", "string")]
        [InlineData("byte", "bool")]
        [InlineData("byte", "long")]
        [InlineData("byte", "uint")]
        [InlineData("byte", "ulong")]
        [InlineData("byte", "nint")]
        [InlineData("byte", "nuint")]
        [InlineData("byte", "float")]
        [InlineData("byte", "double")]
        [InlineData("byte", "decimal")]
        [InlineData("ushort", "object")]
        [InlineData("ushort", "string")]
        [InlineData("ushort", "bool")]
        [InlineData("ushort", "long")]
        [InlineData("ushort", "uint")]
        [InlineData("ushort", "ulong")]
        [InlineData("ushort", "nint")]
        [InlineData("ushort", "nuint")]
        [InlineData("ushort", "float")]
        [InlineData("ushort", "double")]
        [InlineData("ushort", "decimal")]
        [InlineData("uint", "object")]
        [InlineData("uint", "string")]
        [InlineData("uint", "bool")]
        [InlineData("uint", "long")]
        [InlineData("uint", "uint")]
        [InlineData("uint", "ulong")]
        [InlineData("uint", "nint")]
        [InlineData("uint", "nuint")]
        [InlineData("uint", "float")]
        [InlineData("uint", "double")]
        [InlineData("uint", "decimal")]
        [InlineData("ulong", "object")]
        [InlineData("ulong", "string")]
        [InlineData("ulong", "bool")]
        [InlineData("ulong", "long")]
        [InlineData("ulong", "uint")]
        [InlineData("ulong", "ulong")]
        [InlineData("ulong", "nint")]
        [InlineData("ulong", "nuint")]
        [InlineData("ulong", "float")]
        [InlineData("ulong", "double")]
        [InlineData("ulong", "decimal")]
        [InlineData("nint", "object")]
        [InlineData("nint", "string")]
        [InlineData("nint", "bool")]
        [InlineData("nint", "long")]
        [InlineData("nint", "uint")]
        [InlineData("nint", "ulong")]
        [InlineData("nint", "nint")]
        [InlineData("nint", "nuint")]
        [InlineData("nint", "float")]
        [InlineData("nint", "double")]
        [InlineData("nint", "decimal")]
        [InlineData("nuint", "object")]
        [InlineData("nuint", "string")]
        [InlineData("nuint", "bool")]
        [InlineData("nuint", "long")]
        [InlineData("nuint", "uint")]
        [InlineData("nuint", "ulong")]
        [InlineData("nuint", "nint")]
        [InlineData("nuint", "nuint")]
        [InlineData("nuint", "float")]
        [InlineData("nuint", "double")]
        [InlineData("nuint", "decimal")]
        [InlineData("float", "object")]
        [InlineData("float", "string")]
        [InlineData("float", "bool")]
        [InlineData("float", "char")]
        [InlineData("float", "sbyte")]
        [InlineData("float", "short")]
        [InlineData("float", "int")]
        [InlineData("float", "long")]
        [InlineData("float", "byte")]
        [InlineData("float", "ushort")]
        [InlineData("float", "uint")]
        [InlineData("float", "ulong")]
        [InlineData("float", "nint")]
        [InlineData("float", "nuint")]
        [InlineData("float", "float")]
        [InlineData("float", "double")]
        [InlineData("float", "decimal")]
        [InlineData("double", "object")]
        [InlineData("double", "string")]
        [InlineData("double", "bool")]
        [InlineData("double", "char")]
        [InlineData("double", "sbyte")]
        [InlineData("double", "short")]
        [InlineData("double", "int")]
        [InlineData("double", "long")]
        [InlineData("double", "byte")]
        [InlineData("double", "ushort")]
        [InlineData("double", "uint")]
        [InlineData("double", "ulong")]
        [InlineData("double", "nint")]
        [InlineData("double", "nuint")]
        [InlineData("double", "float")]
        [InlineData("double", "double")]
        [InlineData("double", "decimal")]
        [InlineData("decimal", "object")]
        [InlineData("decimal", "string")]
        [InlineData("decimal", "bool")]
        [InlineData("decimal", "char")]
        [InlineData("decimal", "sbyte")]
        [InlineData("decimal", "short")]
        [InlineData("decimal", "int")]
        [InlineData("decimal", "long")]
        [InlineData("decimal", "byte")]
        [InlineData("decimal", "ushort")]
        [InlineData("decimal", "uint")]
        [InlineData("decimal", "ulong")]
        [InlineData("decimal", "nint")]
        [InlineData("decimal", "nuint")]
        [InlineData("decimal", "float")]
        [InlineData("decimal", "double")]
        [InlineData("decimal", "decimal")]
        public void BuiltIn_CompoundAssignment_02(string left, string right)
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        " + left + @" x = default;
        " + right + @" y = default;
        x >>= y;
        x >>>= y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (8,9): error CS0019: Operator '>>=' cannot be applied to operands of type 'double' and 'char'
                //         x >>= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>= y").WithArguments(">>=", left, right).WithLocation(8, 9),
                // (9,9): error CS0019: Operator '>>>=' cannot be applied to operands of type 'double' and 'char'
                //         x >>>= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>>= y").WithArguments(">>>=", left, right).WithLocation(9, 9)
                );
        }

        [Fact]
        public void CollectionInitializerElement()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        var x = int.MinValue;
        var y = new System.Collections.Generic.List<int>() {
            x >>= 1,
            x >>>= 1 
            };
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyEmitDiagnostics(
                // (8,13): error CS0747: Invalid initializer member declarator
                //             x >>= 1,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "x >>= 1").WithLocation(8, 13),
                // (9,13): error CS0747: Invalid initializer member declarator
                //             x >>>= 1 
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "x >>>= 1").WithLocation(9, 13)
                );
        }

        [Fact]
        public void BuiltIn_ExpressionTree_01()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<int, int, int>> e = (x, y) => x >>> y; 
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,86): error CS7053: An expression tree may not contain '>>>'
                //         System.Linq.Expressions.Expression<System.Func<int, int, int>> e = (x, y) => x >>> y; 
                Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "x >>> y").WithArguments(">>>").WithLocation(6, 86)
                );
        }

        [Fact]
        public void BuiltIn_CompoundAssignment_ExpressionTree_01()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<int, int, int>> e = (x, y) => x >>>= y; 
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,86): error CS0832: An expression tree may not contain an assignment operator
                //         System.Linq.Expressions.Expression<System.Func<int, int, int>> e = (x, y) => x >>>= y;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x >>>= y").WithLocation(6, 86)
                );
        }

        [Fact]
        public void BuiltIn_Dynamic_01()
        {
            var source1 =
@"
class C
{
    static void Main(dynamic x, int y)
    {
        _ = x >>> y;        
        _ = y >>> x;        
        _ = x >>> x;        
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,13): error CS0019: Operator '>>>' cannot be applied to operands of type 'dynamic' and 'int'
                //         _ = x >>> y;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>> y").WithArguments(">>>", "dynamic", "int").WithLocation(6, 13),
                // (7,13): error CS0019: Operator '>>>' cannot be applied to operands of type 'int' and 'dynamic'
                //         _ = y >>> x;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "y >>> x").WithArguments(">>>", "int", "dynamic").WithLocation(7, 13),
                // (8,13): error CS0019: Operator '>>>' cannot be applied to operands of type 'dynamic' and 'dynamic'
                //         _ = x >>> x;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>> x").WithArguments(">>>", "dynamic", "dynamic").WithLocation(8, 13)
                );
        }

        [Fact]
        public void BuiltIn_CompoundAssignment_Dynamic_01()
        {
            var source1 =
@"
class C
{
    static void Main(dynamic x, int y)
    {
        x >>>= y;        
        y >>>= x;        
        x >>>= x;        
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,9): error CS0019: Operator '>>>=' cannot be applied to operands of type 'dynamic' and 'int'
                //         x >>>= y;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>>= y").WithArguments(">>>=", "dynamic", "int").WithLocation(6, 9),
                // (7,9): error CS0019: Operator '>>>=' cannot be applied to operands of type 'int' and 'dynamic'
                //         y >>>= x;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "y >>>= x").WithArguments(">>>=", "int", "dynamic").WithLocation(7, 9),
                // (8,9): error CS0019: Operator '>>>=' cannot be applied to operands of type 'dynamic' and 'dynamic'
                //         x >>>= x;        
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>>= x").WithArguments(">>>=", "dynamic", "dynamic").WithLocation(8, 9)
                );
        }

        [Theory]
        [InlineData("char", "char", "ushort.MaxValue", "int")]
        [InlineData("char", "sbyte", "ushort.MaxValue", "int")]
        [InlineData("char", "short", "ushort.MaxValue", "int")]
        [InlineData("char", "int", "ushort.MaxValue", "int")]
        [InlineData("char", "byte", "ushort.MaxValue", "int")]
        [InlineData("char", "ushort", "ushort.MaxValue", "int")]
        [InlineData("sbyte", "char", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "sbyte", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "short", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "int", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "byte", "sbyte.MinValue", "int")]
        [InlineData("sbyte", "ushort", "sbyte.MinValue", "int")]
        [InlineData("short", "char", "short.MinValue", "int")]
        [InlineData("short", "sbyte", "short.MinValue", "int")]
        [InlineData("short", "short", "short.MinValue", "int")]
        [InlineData("short", "int", "short.MinValue", "int")]
        [InlineData("short", "byte", "short.MinValue", "int")]
        [InlineData("short", "ushort", "short.MinValue", "int")]
        [InlineData("int", "char", "int.MinValue", "int")]
        [InlineData("int", "sbyte", "int.MinValue", "int")]
        [InlineData("int", "short", "int.MinValue", "int")]
        [InlineData("int", "int", "int.MinValue", "int")]
        [InlineData("int", "byte", "int.MinValue", "int")]
        [InlineData("int", "ushort", "int.MinValue", "int")]
        [InlineData("long", "char", "long.MinValue", "long")]
        [InlineData("long", "sbyte", "long.MinValue", "long")]
        [InlineData("long", "short", "long.MinValue", "long")]
        [InlineData("long", "int", "long.MinValue", "long")]
        [InlineData("long", "byte", "long.MinValue", "long")]
        [InlineData("long", "ushort", "long.MinValue", "long")]
        [InlineData("byte", "char", "byte.MaxValue", "int")]
        [InlineData("byte", "sbyte", "byte.MaxValue", "int")]
        [InlineData("byte", "short", "byte.MaxValue", "int")]
        [InlineData("byte", "int", "byte.MaxValue", "int")]
        [InlineData("byte", "byte", "byte.MaxValue", "int")]
        [InlineData("byte", "ushort", "byte.MaxValue", "int")]
        [InlineData("ushort", "char", "ushort.MaxValue", "int")]
        [InlineData("ushort", "sbyte", "ushort.MaxValue", "int")]
        [InlineData("ushort", "short", "ushort.MaxValue", "int")]
        [InlineData("ushort", "int", "ushort.MaxValue", "int")]
        [InlineData("ushort", "byte", "ushort.MaxValue", "int")]
        [InlineData("ushort", "ushort", "ushort.MaxValue", "int")]
        [InlineData("uint", "char", "uint.MaxValue", "uint")]
        [InlineData("uint", "sbyte", "uint.MaxValue", "uint")]
        [InlineData("uint", "short", "uint.MaxValue", "uint")]
        [InlineData("uint", "int", "uint.MaxValue", "uint")]
        [InlineData("uint", "byte", "uint.MaxValue", "uint")]
        [InlineData("uint", "ushort", "uint.MaxValue", "uint")]
        [InlineData("ulong", "char", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "sbyte", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "short", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "int", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "byte", "ulong.MaxValue", "ulong")]
        [InlineData("ulong", "ushort", "ulong.MaxValue", "ulong")]
        [InlineData("nint", "char", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nint", "sbyte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nint", "short", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nint", "int", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nint", "byte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nint", "ushort", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)", "nint")]
        [InlineData("nuint", "char", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        [InlineData("nuint", "sbyte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        [InlineData("nuint", "short", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        [InlineData("nuint", "int", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        [InlineData("nuint", "byte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        [InlineData("nuint", "ushort", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)", "nuint")]
        public void BuiltIn_Lifted_01(string left, string right, string leftValue, string result)
        {
            var nullableLeft = left + "?";
            var nullableRight = right + "?";
            var nullableResult = result + "?";

            var source1 =
@"
class C
{
    static void Main()
    {
        var x = (" + nullableLeft + @")" + leftValue + @";
        var y = (" + nullableRight + @")1;
        var z1 = x >>> y;
        var z2 = x >> y;

        if (z1 == (x.Value >>> y.Value)) System.Console.WriteLine(""Passed 1"");

        if (GetType(z1) == GetType(z2) && GetType(z1) == typeof(" + nullableResult + @")) System.Console.WriteLine(""Passed 2"");

        if (Test1(x, null) == null) System.Console.WriteLine(""Passed 3"");
        if (Test1(null, y) == null) System.Console.WriteLine(""Passed 4"");
        if (Test1(null, null) == null) System.Console.WriteLine(""Passed 5"");
    }

    static " + nullableResult + @" Test1(" + nullableLeft + @" x, " + nullableRight + @" y) => x >>> y; 
    static " + nullableResult + @" Test2(" + nullableLeft + @" x, " + nullableRight + @" y) => x >> y; 

    static System.Type GetType<T>(T x) => typeof(T);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
Passed 1
Passed 2
Passed 3
Passed 4
Passed 5
").VerifyDiagnostics();


            string actualIL = verifier.VisualizeIL("C.Test2");
            verifier.VerifyIL("C.Test1", actualIL.Replace("shr.un", "shr").Replace("shr", "shr.un"));

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var unsignedShift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.UnsignedRightShiftExpression).First();
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.RightShiftExpression).First();

            Assert.Equal("x >>> y", unsignedShift.ToString());
            Assert.Equal("x >> y", shift.ToString());

            var unsignedShiftSymbol = (IMethodSymbol)model.GetSymbolInfo(unsignedShift).Symbol;
            var shiftSymbol = (IMethodSymbol)model.GetSymbolInfo(shift).Symbol;
            Assert.Equal("op_UnsignedRightShift", unsignedShiftSymbol.Name);
            Assert.Equal("op_RightShift", shiftSymbol.Name);

            Assert.Same(shiftSymbol.ReturnType, unsignedShiftSymbol.ReturnType);
            Assert.Same(shiftSymbol.Parameters[0].Type, unsignedShiftSymbol.Parameters[0].Type);
            Assert.Same(shiftSymbol.Parameters[1].Type, unsignedShiftSymbol.Parameters[1].Type);
            Assert.Same(shiftSymbol.ContainingSymbol, unsignedShiftSymbol.ContainingSymbol);
        }

        [Theory]
        [InlineData("object", "object")]
        [InlineData("object", "string")]
        [InlineData("object", "bool")]
        [InlineData("object", "char")]
        [InlineData("object", "sbyte")]
        [InlineData("object", "short")]
        [InlineData("object", "int")]
        [InlineData("object", "long")]
        [InlineData("object", "byte")]
        [InlineData("object", "ushort")]
        [InlineData("object", "uint")]
        [InlineData("object", "ulong")]
        [InlineData("object", "nint")]
        [InlineData("object", "nuint")]
        [InlineData("object", "float")]
        [InlineData("object", "double")]
        [InlineData("object", "decimal")]
        [InlineData("string", "object")]
        [InlineData("string", "string")]
        [InlineData("string", "bool")]
        [InlineData("string", "char")]
        [InlineData("string", "sbyte")]
        [InlineData("string", "short")]
        [InlineData("string", "int")]
        [InlineData("string", "long")]
        [InlineData("string", "byte")]
        [InlineData("string", "ushort")]
        [InlineData("string", "uint")]
        [InlineData("string", "ulong")]
        [InlineData("string", "nint")]
        [InlineData("string", "nuint")]
        [InlineData("string", "float")]
        [InlineData("string", "double")]
        [InlineData("string", "decimal")]
        [InlineData("bool", "object")]
        [InlineData("bool", "string")]
        [InlineData("bool", "bool")]
        [InlineData("bool", "char")]
        [InlineData("bool", "sbyte")]
        [InlineData("bool", "short")]
        [InlineData("bool", "int")]
        [InlineData("bool", "long")]
        [InlineData("bool", "byte")]
        [InlineData("bool", "ushort")]
        [InlineData("bool", "uint")]
        [InlineData("bool", "ulong")]
        [InlineData("bool", "nint")]
        [InlineData("bool", "nuint")]
        [InlineData("bool", "float")]
        [InlineData("bool", "double")]
        [InlineData("bool", "decimal")]
        [InlineData("char", "object")]
        [InlineData("char", "string")]
        [InlineData("char", "bool")]
        [InlineData("char", "long")]
        [InlineData("char", "uint")]
        [InlineData("char", "ulong")]
        [InlineData("char", "nint")]
        [InlineData("char", "nuint")]
        [InlineData("char", "float")]
        [InlineData("char", "double")]
        [InlineData("char", "decimal")]
        [InlineData("sbyte", "object")]
        [InlineData("sbyte", "string")]
        [InlineData("sbyte", "bool")]
        [InlineData("sbyte", "long")]
        [InlineData("sbyte", "uint")]
        [InlineData("sbyte", "ulong")]
        [InlineData("sbyte", "nint")]
        [InlineData("sbyte", "nuint")]
        [InlineData("sbyte", "float")]
        [InlineData("sbyte", "double")]
        [InlineData("sbyte", "decimal")]
        [InlineData("short", "object")]
        [InlineData("short", "string")]
        [InlineData("short", "bool")]
        [InlineData("short", "long")]
        [InlineData("short", "uint")]
        [InlineData("short", "ulong")]
        [InlineData("short", "nint")]
        [InlineData("short", "nuint")]
        [InlineData("short", "float")]
        [InlineData("short", "double")]
        [InlineData("short", "decimal")]
        [InlineData("int", "object")]
        [InlineData("int", "string")]
        [InlineData("int", "bool")]
        [InlineData("int", "long")]
        [InlineData("int", "uint")]
        [InlineData("int", "ulong")]
        [InlineData("int", "nint")]
        [InlineData("int", "nuint")]
        [InlineData("int", "float")]
        [InlineData("int", "double")]
        [InlineData("int", "decimal")]
        [InlineData("long", "object")]
        [InlineData("long", "string")]
        [InlineData("long", "bool")]
        [InlineData("long", "long")]
        [InlineData("long", "uint")]
        [InlineData("long", "ulong")]
        [InlineData("long", "nint")]
        [InlineData("long", "nuint")]
        [InlineData("long", "float")]
        [InlineData("long", "double")]
        [InlineData("long", "decimal")]
        [InlineData("byte", "object")]
        [InlineData("byte", "string")]
        [InlineData("byte", "bool")]
        [InlineData("byte", "long")]
        [InlineData("byte", "uint")]
        [InlineData("byte", "ulong")]
        [InlineData("byte", "nint")]
        [InlineData("byte", "nuint")]
        [InlineData("byte", "float")]
        [InlineData("byte", "double")]
        [InlineData("byte", "decimal")]
        [InlineData("ushort", "object")]
        [InlineData("ushort", "string")]
        [InlineData("ushort", "bool")]
        [InlineData("ushort", "long")]
        [InlineData("ushort", "uint")]
        [InlineData("ushort", "ulong")]
        [InlineData("ushort", "nint")]
        [InlineData("ushort", "nuint")]
        [InlineData("ushort", "float")]
        [InlineData("ushort", "double")]
        [InlineData("ushort", "decimal")]
        [InlineData("uint", "object")]
        [InlineData("uint", "string")]
        [InlineData("uint", "bool")]
        [InlineData("uint", "long")]
        [InlineData("uint", "uint")]
        [InlineData("uint", "ulong")]
        [InlineData("uint", "nint")]
        [InlineData("uint", "nuint")]
        [InlineData("uint", "float")]
        [InlineData("uint", "double")]
        [InlineData("uint", "decimal")]
        [InlineData("ulong", "object")]
        [InlineData("ulong", "string")]
        [InlineData("ulong", "bool")]
        [InlineData("ulong", "long")]
        [InlineData("ulong", "uint")]
        [InlineData("ulong", "ulong")]
        [InlineData("ulong", "nint")]
        [InlineData("ulong", "nuint")]
        [InlineData("ulong", "float")]
        [InlineData("ulong", "double")]
        [InlineData("ulong", "decimal")]
        [InlineData("nint", "object")]
        [InlineData("nint", "string")]
        [InlineData("nint", "bool")]
        [InlineData("nint", "long")]
        [InlineData("nint", "uint")]
        [InlineData("nint", "ulong")]
        [InlineData("nint", "nint")]
        [InlineData("nint", "nuint")]
        [InlineData("nint", "float")]
        [InlineData("nint", "double")]
        [InlineData("nint", "decimal")]
        [InlineData("nuint", "object")]
        [InlineData("nuint", "string")]
        [InlineData("nuint", "bool")]
        [InlineData("nuint", "long")]
        [InlineData("nuint", "uint")]
        [InlineData("nuint", "ulong")]
        [InlineData("nuint", "nint")]
        [InlineData("nuint", "nuint")]
        [InlineData("nuint", "float")]
        [InlineData("nuint", "double")]
        [InlineData("nuint", "decimal")]
        [InlineData("float", "object")]
        [InlineData("float", "string")]
        [InlineData("float", "bool")]
        [InlineData("float", "char")]
        [InlineData("float", "sbyte")]
        [InlineData("float", "short")]
        [InlineData("float", "int")]
        [InlineData("float", "long")]
        [InlineData("float", "byte")]
        [InlineData("float", "ushort")]
        [InlineData("float", "uint")]
        [InlineData("float", "ulong")]
        [InlineData("float", "nint")]
        [InlineData("float", "nuint")]
        [InlineData("float", "float")]
        [InlineData("float", "double")]
        [InlineData("float", "decimal")]
        [InlineData("double", "object")]
        [InlineData("double", "string")]
        [InlineData("double", "bool")]
        [InlineData("double", "char")]
        [InlineData("double", "sbyte")]
        [InlineData("double", "short")]
        [InlineData("double", "int")]
        [InlineData("double", "long")]
        [InlineData("double", "byte")]
        [InlineData("double", "ushort")]
        [InlineData("double", "uint")]
        [InlineData("double", "ulong")]
        [InlineData("double", "nint")]
        [InlineData("double", "nuint")]
        [InlineData("double", "float")]
        [InlineData("double", "double")]
        [InlineData("double", "decimal")]
        [InlineData("decimal", "object")]
        [InlineData("decimal", "string")]
        [InlineData("decimal", "bool")]
        [InlineData("decimal", "char")]
        [InlineData("decimal", "sbyte")]
        [InlineData("decimal", "short")]
        [InlineData("decimal", "int")]
        [InlineData("decimal", "long")]
        [InlineData("decimal", "byte")]
        [InlineData("decimal", "ushort")]
        [InlineData("decimal", "uint")]
        [InlineData("decimal", "ulong")]
        [InlineData("decimal", "nint")]
        [InlineData("decimal", "nuint")]
        [InlineData("decimal", "float")]
        [InlineData("decimal", "double")]
        [InlineData("decimal", "decimal")]
        public void BuiltIn_Lifted_02(string left, string right)
        {
            var nullableLeft = NullableIfPossible(left);
            var nullableRight = NullableIfPossible(right);

            var source1 =
@"
class C
{
    static void Main()
    {
        " + nullableLeft + @" x = default;
        " + nullableRight + @" y = default;
        var z1 = x >> y;
        var z2 = x >>> y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (8,18): error CS0019: Operator '>>' cannot be applied to operands of type 'object' and 'object'
                //         var z1 = x >> y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >> y").WithArguments(">>", nullableLeft, nullableRight).WithLocation(8, 18),
                // (9,18): error CS0019: Operator '>>>' cannot be applied to operands of type 'object' and 'object'
                //         var z2 = x >>> y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>> y").WithArguments(">>>", nullableLeft, nullableRight).WithLocation(9, 18)
                );
        }

        private static string NullableIfPossible(string type)
        {
            switch (type)
            {
                case "object":
                case "string":
                    return type;

                default:
                    return type + "?";
            }
        }

        [Theory]
        [InlineData("char", "char", "ushort.MaxValue")]
        [InlineData("char", "sbyte", "ushort.MaxValue")]
        [InlineData("char", "short", "ushort.MaxValue")]
        [InlineData("char", "int", "ushort.MaxValue")]
        [InlineData("char", "byte", "ushort.MaxValue")]
        [InlineData("char", "ushort", "ushort.MaxValue")]
        [InlineData("sbyte", "char", "sbyte.MinValue")]
        [InlineData("sbyte", "sbyte", "sbyte.MinValue")]
        [InlineData("sbyte", "short", "sbyte.MinValue")]
        [InlineData("sbyte", "int", "sbyte.MinValue")]
        [InlineData("sbyte", "byte", "sbyte.MinValue")]
        [InlineData("sbyte", "ushort", "sbyte.MinValue")]
        [InlineData("short", "char", "short.MinValue")]
        [InlineData("short", "sbyte", "short.MinValue")]
        [InlineData("short", "short", "short.MinValue")]
        [InlineData("short", "int", "short.MinValue")]
        [InlineData("short", "byte", "short.MinValue")]
        [InlineData("short", "ushort", "short.MinValue")]
        [InlineData("int", "char", "int.MinValue")]
        [InlineData("int", "sbyte", "int.MinValue")]
        [InlineData("int", "short", "int.MinValue")]
        [InlineData("int", "int", "int.MinValue")]
        [InlineData("int", "byte", "int.MinValue")]
        [InlineData("int", "ushort", "int.MinValue")]
        [InlineData("long", "char", "long.MinValue")]
        [InlineData("long", "sbyte", "long.MinValue")]
        [InlineData("long", "short", "long.MinValue")]
        [InlineData("long", "int", "long.MinValue")]
        [InlineData("long", "byte", "long.MinValue")]
        [InlineData("long", "ushort", "long.MinValue")]
        [InlineData("byte", "char", "byte.MaxValue")]
        [InlineData("byte", "sbyte", "byte.MaxValue")]
        [InlineData("byte", "short", "byte.MaxValue")]
        [InlineData("byte", "int", "byte.MaxValue")]
        [InlineData("byte", "byte", "byte.MaxValue")]
        [InlineData("byte", "ushort", "byte.MaxValue")]
        [InlineData("ushort", "char", "ushort.MaxValue")]
        [InlineData("ushort", "sbyte", "ushort.MaxValue")]
        [InlineData("ushort", "short", "ushort.MaxValue")]
        [InlineData("ushort", "int", "ushort.MaxValue")]
        [InlineData("ushort", "byte", "ushort.MaxValue")]
        [InlineData("ushort", "ushort", "ushort.MaxValue")]
        [InlineData("uint", "char", "uint.MaxValue")]
        [InlineData("uint", "sbyte", "uint.MaxValue")]
        [InlineData("uint", "short", "uint.MaxValue")]
        [InlineData("uint", "int", "uint.MaxValue")]
        [InlineData("uint", "byte", "uint.MaxValue")]
        [InlineData("uint", "ushort", "uint.MaxValue")]
        [InlineData("ulong", "char", "ulong.MaxValue")]
        [InlineData("ulong", "sbyte", "ulong.MaxValue")]
        [InlineData("ulong", "short", "ulong.MaxValue")]
        [InlineData("ulong", "int", "ulong.MaxValue")]
        [InlineData("ulong", "byte", "ulong.MaxValue")]
        [InlineData("ulong", "ushort", "ulong.MaxValue")]
        [InlineData("nint", "char", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "sbyte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "short", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "int", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "byte", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nint", "ushort", "(System.IntPtr.Size == 4 ? int.MinValue : long.MinValue)")]
        [InlineData("nuint", "char", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "sbyte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "short", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "int", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "byte", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        [InlineData("nuint", "ushort", "(System.IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue)")]
        public void BuiltIn_Lifted_CompoundAssignment_01(string left, string right, string leftValue)
        {
            var nullableLeft = left + "?";
            var nullableRight = right + "?";

            var source1 =
@"
class C
{
    static void Main()
    {
        var x = (" + nullableLeft + @")" + leftValue + @";
        var y = (" + nullableRight + @")1;
        var z1 = x;
        z1 >>>= y;

        if (z1 == (" + left + @")(x.Value >>> y.Value)) System.Console.WriteLine(""Passed 1"");

        z1 >>= y;

        z1 = null;
        z1 >>>= y;
        if (z1 == null) System.Console.WriteLine(""Passed 2"");

        y = null;
        z1 >>>= y;
        if (z1 == null) System.Console.WriteLine(""Passed 3"");

        z1 = x;
        z1 >>>= y;
        if (z1 == null) System.Console.WriteLine(""Passed 4"");
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(compilation1, expectedOutput: @"
Passed 1
Passed 2
Passed 3
Passed 4
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var unsignedShift = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.UnsignedRightShiftAssignmentExpression).First();
            var shift = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(e => e.Kind() == SyntaxKind.RightShiftAssignmentExpression).First();

            Assert.Equal("z1 >>>= y", unsignedShift.ToString());
            Assert.Equal("z1 >>= y", shift.ToString());

            var unsignedShiftSymbol = (IMethodSymbol)model.GetSymbolInfo(unsignedShift).Symbol;
            var shiftSymbol = (IMethodSymbol)model.GetSymbolInfo(shift).Symbol;
            Assert.Equal("op_UnsignedRightShift", unsignedShiftSymbol.Name);
            Assert.Equal("op_RightShift", shiftSymbol.Name);

            Assert.Same(shiftSymbol.ReturnType, unsignedShiftSymbol.ReturnType);
            Assert.Same(shiftSymbol.Parameters[0].Type, unsignedShiftSymbol.Parameters[0].Type);
            Assert.Same(shiftSymbol.Parameters[1].Type, unsignedShiftSymbol.Parameters[1].Type);
            Assert.Same(shiftSymbol.ContainingSymbol, unsignedShiftSymbol.ContainingSymbol);
        }

        [Theory]
        [InlineData("object", "object")]
        [InlineData("object", "string")]
        [InlineData("object", "bool")]
        [InlineData("object", "char")]
        [InlineData("object", "sbyte")]
        [InlineData("object", "short")]
        [InlineData("object", "int")]
        [InlineData("object", "long")]
        [InlineData("object", "byte")]
        [InlineData("object", "ushort")]
        [InlineData("object", "uint")]
        [InlineData("object", "ulong")]
        [InlineData("object", "nint")]
        [InlineData("object", "nuint")]
        [InlineData("object", "float")]
        [InlineData("object", "double")]
        [InlineData("object", "decimal")]
        [InlineData("string", "object")]
        [InlineData("string", "string")]
        [InlineData("string", "bool")]
        [InlineData("string", "char")]
        [InlineData("string", "sbyte")]
        [InlineData("string", "short")]
        [InlineData("string", "int")]
        [InlineData("string", "long")]
        [InlineData("string", "byte")]
        [InlineData("string", "ushort")]
        [InlineData("string", "uint")]
        [InlineData("string", "ulong")]
        [InlineData("string", "nint")]
        [InlineData("string", "nuint")]
        [InlineData("string", "float")]
        [InlineData("string", "double")]
        [InlineData("string", "decimal")]
        [InlineData("bool", "object")]
        [InlineData("bool", "string")]
        [InlineData("bool", "bool")]
        [InlineData("bool", "char")]
        [InlineData("bool", "sbyte")]
        [InlineData("bool", "short")]
        [InlineData("bool", "int")]
        [InlineData("bool", "long")]
        [InlineData("bool", "byte")]
        [InlineData("bool", "ushort")]
        [InlineData("bool", "uint")]
        [InlineData("bool", "ulong")]
        [InlineData("bool", "nint")]
        [InlineData("bool", "nuint")]
        [InlineData("bool", "float")]
        [InlineData("bool", "double")]
        [InlineData("bool", "decimal")]
        [InlineData("char", "object")]
        [InlineData("char", "string")]
        [InlineData("char", "bool")]
        [InlineData("char", "long")]
        [InlineData("char", "uint")]
        [InlineData("char", "ulong")]
        [InlineData("char", "nint")]
        [InlineData("char", "nuint")]
        [InlineData("char", "float")]
        [InlineData("char", "double")]
        [InlineData("char", "decimal")]
        [InlineData("sbyte", "object")]
        [InlineData("sbyte", "string")]
        [InlineData("sbyte", "bool")]
        [InlineData("sbyte", "long")]
        [InlineData("sbyte", "uint")]
        [InlineData("sbyte", "ulong")]
        [InlineData("sbyte", "nint")]
        [InlineData("sbyte", "nuint")]
        [InlineData("sbyte", "float")]
        [InlineData("sbyte", "double")]
        [InlineData("sbyte", "decimal")]
        [InlineData("short", "object")]
        [InlineData("short", "string")]
        [InlineData("short", "bool")]
        [InlineData("short", "long")]
        [InlineData("short", "uint")]
        [InlineData("short", "ulong")]
        [InlineData("short", "nint")]
        [InlineData("short", "nuint")]
        [InlineData("short", "float")]
        [InlineData("short", "double")]
        [InlineData("short", "decimal")]
        [InlineData("int", "object")]
        [InlineData("int", "string")]
        [InlineData("int", "bool")]
        [InlineData("int", "long")]
        [InlineData("int", "uint")]
        [InlineData("int", "ulong")]
        [InlineData("int", "nint")]
        [InlineData("int", "nuint")]
        [InlineData("int", "float")]
        [InlineData("int", "double")]
        [InlineData("int", "decimal")]
        [InlineData("long", "object")]
        [InlineData("long", "string")]
        [InlineData("long", "bool")]
        [InlineData("long", "long")]
        [InlineData("long", "uint")]
        [InlineData("long", "ulong")]
        [InlineData("long", "nint")]
        [InlineData("long", "nuint")]
        [InlineData("long", "float")]
        [InlineData("long", "double")]
        [InlineData("long", "decimal")]
        [InlineData("byte", "object")]
        [InlineData("byte", "string")]
        [InlineData("byte", "bool")]
        [InlineData("byte", "long")]
        [InlineData("byte", "uint")]
        [InlineData("byte", "ulong")]
        [InlineData("byte", "nint")]
        [InlineData("byte", "nuint")]
        [InlineData("byte", "float")]
        [InlineData("byte", "double")]
        [InlineData("byte", "decimal")]
        [InlineData("ushort", "object")]
        [InlineData("ushort", "string")]
        [InlineData("ushort", "bool")]
        [InlineData("ushort", "long")]
        [InlineData("ushort", "uint")]
        [InlineData("ushort", "ulong")]
        [InlineData("ushort", "nint")]
        [InlineData("ushort", "nuint")]
        [InlineData("ushort", "float")]
        [InlineData("ushort", "double")]
        [InlineData("ushort", "decimal")]
        [InlineData("uint", "object")]
        [InlineData("uint", "string")]
        [InlineData("uint", "bool")]
        [InlineData("uint", "long")]
        [InlineData("uint", "uint")]
        [InlineData("uint", "ulong")]
        [InlineData("uint", "nint")]
        [InlineData("uint", "nuint")]
        [InlineData("uint", "float")]
        [InlineData("uint", "double")]
        [InlineData("uint", "decimal")]
        [InlineData("ulong", "object")]
        [InlineData("ulong", "string")]
        [InlineData("ulong", "bool")]
        [InlineData("ulong", "long")]
        [InlineData("ulong", "uint")]
        [InlineData("ulong", "ulong")]
        [InlineData("ulong", "nint")]
        [InlineData("ulong", "nuint")]
        [InlineData("ulong", "float")]
        [InlineData("ulong", "double")]
        [InlineData("ulong", "decimal")]
        [InlineData("nint", "object")]
        [InlineData("nint", "string")]
        [InlineData("nint", "bool")]
        [InlineData("nint", "long")]
        [InlineData("nint", "uint")]
        [InlineData("nint", "ulong")]
        [InlineData("nint", "nint")]
        [InlineData("nint", "nuint")]
        [InlineData("nint", "float")]
        [InlineData("nint", "double")]
        [InlineData("nint", "decimal")]
        [InlineData("nuint", "object")]
        [InlineData("nuint", "string")]
        [InlineData("nuint", "bool")]
        [InlineData("nuint", "long")]
        [InlineData("nuint", "uint")]
        [InlineData("nuint", "ulong")]
        [InlineData("nuint", "nint")]
        [InlineData("nuint", "nuint")]
        [InlineData("nuint", "float")]
        [InlineData("nuint", "double")]
        [InlineData("nuint", "decimal")]
        [InlineData("float", "object")]
        [InlineData("float", "string")]
        [InlineData("float", "bool")]
        [InlineData("float", "char")]
        [InlineData("float", "sbyte")]
        [InlineData("float", "short")]
        [InlineData("float", "int")]
        [InlineData("float", "long")]
        [InlineData("float", "byte")]
        [InlineData("float", "ushort")]
        [InlineData("float", "uint")]
        [InlineData("float", "ulong")]
        [InlineData("float", "nint")]
        [InlineData("float", "nuint")]
        [InlineData("float", "float")]
        [InlineData("float", "double")]
        [InlineData("float", "decimal")]
        [InlineData("double", "object")]
        [InlineData("double", "string")]
        [InlineData("double", "bool")]
        [InlineData("double", "char")]
        [InlineData("double", "sbyte")]
        [InlineData("double", "short")]
        [InlineData("double", "int")]
        [InlineData("double", "long")]
        [InlineData("double", "byte")]
        [InlineData("double", "ushort")]
        [InlineData("double", "uint")]
        [InlineData("double", "ulong")]
        [InlineData("double", "nint")]
        [InlineData("double", "nuint")]
        [InlineData("double", "float")]
        [InlineData("double", "double")]
        [InlineData("double", "decimal")]
        [InlineData("decimal", "object")]
        [InlineData("decimal", "string")]
        [InlineData("decimal", "bool")]
        [InlineData("decimal", "char")]
        [InlineData("decimal", "sbyte")]
        [InlineData("decimal", "short")]
        [InlineData("decimal", "int")]
        [InlineData("decimal", "long")]
        [InlineData("decimal", "byte")]
        [InlineData("decimal", "ushort")]
        [InlineData("decimal", "uint")]
        [InlineData("decimal", "ulong")]
        [InlineData("decimal", "nint")]
        [InlineData("decimal", "nuint")]
        [InlineData("decimal", "float")]
        [InlineData("decimal", "double")]
        [InlineData("decimal", "decimal")]
        public void BuiltIn_Lifted_CompoundAssignment_02(string left, string right)
        {
            var nullableLeft = NullableIfPossible(left);
            var nullableRight = NullableIfPossible(right);

            var source1 =
@"
class C
{
    static void Main()
    {
        " + nullableLeft + @" x = default;
        " + nullableRight + @" y = default;
        x >>= y;
        x >>>= y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (8,9): error CS0019: Operator '>>=' cannot be applied to operands of type 'double' and 'char'
                //         x >>= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>= y").WithArguments(">>=", nullableLeft, nullableRight).WithLocation(8, 9),
                // (9,9): error CS0019: Operator '>>>=' cannot be applied to operands of type 'double' and 'char'
                //         x >>>= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x >>>= y").WithArguments(">>>=", nullableLeft, nullableRight).WithLocation(9, 9)
                );
        }

        [Fact]
        public void CollectionInitializerElement_Lifted()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        int? x = int.MinValue;
        var y = new System.Collections.Generic.List<int?>() {
            x >>= 1,
            x >>>= 1 
            };
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyEmitDiagnostics(
                // (8,13): error CS0747: Invalid initializer member declarator
                //             x >>= 1,
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "x >>= 1").WithLocation(8, 13),
                // (9,13): error CS0747: Invalid initializer member declarator
                //             x >>>= 1 
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "x >>>= 1").WithLocation(9, 13)
                );
        }

        [Fact]
        public void BuiltIn_Lifted_ExpressionTree_01()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<int?, int?, int?>> e = (x, y) => x >>> y; 
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,89): error CS7053: An expression tree may not contain '>>>'
                //         System.Linq.Expressions.Expression<System.Func<int?, int?, int?>> e = (x, y) => x >>> y; 
                Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "x >>> y").WithArguments(">>>").WithLocation(6, 89)
                );
        }

        [Fact]
        public void BuiltIn_Lifted_CompoundAssignment_ExpressionTree_01()
        {
            var source1 =
@"
class C
{
    static void Main()
    {
        System.Linq.Expressions.Expression<System.Func<int?, int?, int?>> e = (x, y) => x >>>= y; 
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyEmitDiagnostics(
                // (6,89): error CS0832: An expression tree may not contain an assignment operator
                //         System.Linq.Expressions.Expression<System.Func<int?, int?, int?>> e = (x, y) => x >>>= y; 
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x >>>= y").WithLocation(6, 89)
                );
        }
    }
}
