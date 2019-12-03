// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
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
                // (3,5): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 5),
                // (3,14): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nint").WithArguments("native-sized integers").WithLocation(3, 14),
                // (3,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(3, 22));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE: Test:
        // - @nint
        // - Type.nint, Namespace.nint
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol has the comment "dynamic not allowed as an attribute type". Does that apply to "nint"?
        // - BindNonGenericSimpleNamespaceOrTypeOrAliasSymbol checks IsViableType(result)
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
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(6, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                var model = comp.GetSemanticModel(tree);
                var type = model.GetDeclaredSymbol(nodes.OfType<ClassDeclarationSyntax>().Single());
                Assert.Equal("nint", type.ToTestDisplayString());
                Assert.Equal(SpecialType.None, type.SpecialType);
                var method = model.GetDeclaredSymbol(nodes.OfType<MethodDeclarationSyntax>().Single());
                Assert.Equal("nint I.Add(nint x, System.UIntPtr y)", method.ToTestDisplayString());
                var type0 = method.Parameters[0].Type;
                var type1 = method.Parameters[1].Type;
                Assert.Equal(SpecialType.None, type0.SpecialType);
                Assert.False(((NamedTypeSymbol)type0).IsNativeInt);
                Assert.Equal(SpecialType.System_UIntPtr, type1.SpecialType);
                Assert.True(((NamedTypeSymbol)type1).IsNativeInt);
            }
        }

        [Fact]
        public void AliasName()
        {
            var source =
@"using nint = System.Int16;
interface I
{
    nint Add(nint x, nuint y);
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,22): error CS8652: The feature 'native-sized integers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     nint Add(nint x, nuint y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nuint").WithArguments("native-sized integers").WithLocation(4, 22));
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var nodes = tree.GetRoot().DescendantNodes().ToArray();
                var model = comp.GetSemanticModel(tree);
                var method = model.GetDeclaredSymbol(nodes.OfType<MethodDeclarationSyntax>().Single());
                Assert.Equal("System.Int16 I.Add(System.Int16 x, System.UIntPtr y)", method.ToTestDisplayString());
                var type0 = method.Parameters[0].Type;
                var type1 = method.Parameters[1].Type;
                Assert.Equal(SpecialType.System_Int16, type0.SpecialType);
                Assert.False(((NamedTypeSymbol)type0).IsNativeInt);
                Assert.Equal(SpecialType.System_UIntPtr, type1.SpecialType);
                Assert.True(((NamedTypeSymbol)type1).IsNativeInt);
            }
        }

        // PROTOTYPE: nint and nuint should be allowed.
        [Fact]
        public void MemberName()
        {
            var source =
@"class Program
{
    static void Main()
    {
        nint.ToString();
        nuint.ToString();
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,9): error CS0103: The name 'nint' does not exist in the current context
                //         nint.ToString();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 9),
                // (6,9): error CS0103: The name 'nuint' does not exist in the current context
                //         nuint.ToString();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 9));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,9): error CS0103: The name 'nint' does not exist in the current context
                //         nint.ToString();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 9),
                // (6,9): error CS0103: The name 'nuint' does not exist in the current context
                //         nuint.ToString();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 9));
        }

        // PROTOTYPE: nint and nuint should be allowed.
        [Fact]
        public void NameOf()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = nameof(nint);
        _ = nameof(nuint);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 20));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS0103: The name 'nint' does not exist in the current context
                //         _ = nameof(nint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nint").WithArguments("nint").WithLocation(5, 20),
                // (6,20): error CS0103: The name 'nuint' does not exist in the current context
                //         _ = nameof(nuint);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nuint").WithArguments("nuint").WithLocation(6, 20));
        }

        /// <summary>
        /// sizeof(IntPtr) and sizeof(nint) require compiling with /unsafe.
        /// </summary>
        [Fact]
        public void SizeOf_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = sizeof(System.IntPtr);
        _ = sizeof(System.UIntPtr);
        _ = sizeof(nint);
        _ = sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS0233: 'IntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.IntPtr)").WithArguments("System.IntPtr").WithLocation(5, 13),
                // (6,13): error CS0233: 'UIntPtr' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(System.UIntPtr)").WithArguments("System.UIntPtr").WithLocation(6, 13),
                // (7,13): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(7, 13),
                // (8,13): error CS0233: 'nuint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
                //         _ = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nuint)").WithArguments("nuint").WithLocation(8, 13));
        }

        // PROTOTYPE: PEVerify error: TypeRef has a duplicate.
        [Fact(Skip = "PEVerify")]
        public void SizeOf_02()
        {
            var source =
@"using System;
class Program
{
    unsafe static void Main()
    {
        Console.Write(sizeof(System.IntPtr));
        Console.Write(sizeof(System.UIntPtr));
        Console.Write(sizeof(nint));
        Console.Write(sizeof(nuint));
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.RegularPreview);
            int size = IntPtr.Size;
            var verifier = CompileAndVerify(comp, expectedOutput: $"{size}{size}{size}{size}");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  sizeof     ""System.IntPtr""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  sizeof     ""System.UIntPtr""
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  sizeof     ""System.IntPtr""
  IL_001c:  call       ""void System.Console.Write(int)""
  IL_0021:  sizeof     ""System.UIntPtr""
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void SizeOf_03()
        {
            var source =
@"using System.Collections.Generic;
unsafe class Program
{
    static IEnumerable<int> F()
    {
        yield return sizeof(nint);
        yield return sizeof(nuint);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nint)").WithLocation(6, 22),
                // (7,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(nuint);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(nuint)").WithLocation(7, 22));
        }

        [Fact]
        public void SizeOf_04()
        {
            var source =
@"unsafe class Program
{
    const int A = sizeof(System.IntPtr);
    const int B = sizeof(System.UIntPtr);
    const int C = sizeof(nint);
    const int D = sizeof(nuint);
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,19): error CS0133: The expression being assigned to 'Program.A' must be constant
                //     const int A = sizeof(System.IntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.IntPtr)").WithArguments("Program.A").WithLocation(3, 19),
                // (4,19): error CS0133: The expression being assigned to 'Program.B' must be constant
                //     const int B = sizeof(System.UIntPtr);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(System.UIntPtr)").WithArguments("Program.B").WithLocation(4, 19),
                // (5,19): error CS0133: The expression being assigned to 'Program.C' must be constant
                //     const int C = sizeof(nint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nint)").WithArguments("Program.C").WithLocation(5, 19),
                // (6,19): error CS0133: The expression being assigned to 'Program.D' must be constant
                //     const int D = sizeof(nuint);
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "sizeof(nuint)").WithArguments("Program.D").WithLocation(6, 19));
        }

        public static IEnumerable<object[]> ConversionsData()
        {
            string convNone =
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";
            static string conv(string conversion) =>
$@"{{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  ret
}}";
            static string convFromNullableT(string conversion, string sourceType) =>
$@"{{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""{sourceType} {sourceType}?.Value.get""
  IL_0007:  {conversion}
  IL_0008:  ret
}}";
            static string convToNullableT(string conversion, string destType) =>
$@"{{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  {conversion}
  IL_0002:  newobj     ""{destType}?..ctor({destType})""
  IL_0007:  ret
}}";
            static string convFromToNullableT(string conversion, string sourceType, string destType) =>
$@"{{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init ({sourceType}? V_0,
                {destType}? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool {sourceType}?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""{destType}?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""{sourceType} {sourceType}?.GetValueOrDefault()""
  IL_001c:  {conversion}
  IL_001d:  newobj     ""{destType}?..ctor({destType})""
  IL_0022:  ret
}}";
            static void getArgs(ArrayBuilder<object[]> builder, string sourceType, string destType, string expectedImplicitIL, string expectedExplicitIL, string expectedCheckedIL = null)
            {
                getArgs1(
                    builder,
                    sourceType,
                    destType,
                    expectedImplicitIL,
                    skipTypeChecks: usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType), // PROTOTYPE: Not distinguishing IntPtr from nint.
                    useExplicitCast: false,
                    useChecked: false,
                    expectedImplicitIL is null ?
                        expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                        0);
                getArgs1(
                    builder,
                    sourceType,
                    destType,
                    expectedExplicitIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: false,
                    expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);
                expectedCheckedIL ??= expectedExplicitIL;
                getArgs1(
                    builder,
                    sourceType,
                    destType,
                    expectedCheckedIL,
                    skipTypeChecks: true,
                    useExplicitCast: true,
                    useChecked: true,
                    expectedCheckedIL is null ? ErrorCode.ERR_NoExplicitConv : 0);

                static bool usesIntPtrOrUIntPtr(string type) => type.Contains("IntPtr");
            }

            static void getArgs1(ArrayBuilder<object[]> builder, string sourceType, string destType, string expectedIL, bool skipTypeChecks, bool useExplicitCast, bool useChecked, ErrorCode expectedErrorCode)
            {
                builder.Add(new object[] { sourceType, destType, expectedIL, skipTypeChecks, useExplicitCast, useChecked, expectedErrorCode });
            }

            var builder = new ArrayBuilder<object[]>();

            getArgs(builder, sourceType: "object", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "string", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "void*", destType: "nint", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "bool", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "sbyte", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            getArgs(builder, sourceType: "byte", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "short", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            getArgs(builder, sourceType: "ushort", destType: "nint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "int", destType: "nint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"));
            getArgs(builder, sourceType: "uint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.i.un"));
            getArgs(builder, sourceType: "long", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            getArgs(builder, sourceType: "ulong", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            getArgs(builder, sourceType: "nint", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nuint", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i.un"));
            getArgs(builder, sourceType: "float", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            getArgs(builder, sourceType: "double", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.i"));
            getArgs(builder, sourceType: "decimal", destType: "nint", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "System.IntPtr", destType: "nint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "System.UIntPtr", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "bool?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            getArgs(builder, sourceType: "sbyte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"));
            getArgs(builder, sourceType: "byte?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            getArgs(builder, sourceType: "short?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"));
            getArgs(builder, sourceType: "ushort?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            getArgs(builder, sourceType: "int?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"));
            getArgs(builder, sourceType: "uint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "uint"));
            getArgs(builder, sourceType: "long?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "long"));
            getArgs(builder, sourceType: "ulong?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "ulong"));
            getArgs(builder, sourceType: "nint?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i.un", "nuint"));
            getArgs(builder, sourceType: "float?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "float"));
            getArgs(builder, sourceType: "double?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.i", "double"));
            getArgs(builder, sourceType: "decimal?", destType: "nint", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_0011:  ret
}");
            getArgs(builder, sourceType: "System.IntPtr?", destType: "nint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "System.UIntPtr?", destType: "nint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "object", destType: "nint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "string", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "void*", destType: "nint?", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nint?..ctor(nint)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "bool", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            getArgs(builder, sourceType: "sbyte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            getArgs(builder, sourceType: "byte", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            getArgs(builder, sourceType: "short", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            getArgs(builder, sourceType: "ushort", destType: "nint?", expectedImplicitIL: convToNullableT("conv.u", "nint"), expectedExplicitIL: convToNullableT("conv.u", "nint"));
            getArgs(builder, sourceType: "int", destType: "nint?", expectedImplicitIL: convToNullableT("conv.i", "nint"), expectedExplicitIL: convToNullableT("conv.i", "nint"));
            getArgs(builder, sourceType: "uint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, sourceType: "long", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, sourceType: "ulong", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, sourceType: "nint", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nuint", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, sourceType: "float", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, sourceType: "double", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nint"), expectedCheckedIL: convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, sourceType: "decimal", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.IntPtr", destType: "nint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nint?..ctor(nint)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "System.UIntPtr", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "bool?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nint"));
            getArgs(builder, sourceType: "sbyte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nint"));
            getArgs(builder, sourceType: "byte?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nint"));
            getArgs(builder, sourceType: "short?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nint"));
            getArgs(builder, sourceType: "ushort?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nint"));
            getArgs(builder, sourceType: "int?", destType: "nint?", expectedImplicitIL: convFromToNullableT("conv.i", "int", "nint"), expectedExplicitIL: convFromToNullableT("conv.i", "int", "nint"));
            getArgs(builder, sourceType: "uint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            getArgs(builder, sourceType: "long?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "long", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "long", "nint"));
            getArgs(builder, sourceType: "ulong?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "ulong", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nuint?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "nuint", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            getArgs(builder, sourceType: "float?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "float", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "float", "nint"));
            getArgs(builder, sourceType: "double?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "double", "nint"), expectedCheckedIL: convFromToNullableT("conv.ovf.i", "double", "nint"));
            // PROTOTYPE:
            //getArgs(builder, "decimal?", "nint?", null, null);
            getArgs(builder, sourceType: "System.IntPtr?", destType: "nint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "System.UIntPtr?", destType: "nint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            getArgs(builder, sourceType: "nint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1"));
            getArgs(builder, sourceType: "nint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1"));
            getArgs(builder, sourceType: "nint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2"));
            getArgs(builder, sourceType: "nint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2"));
            getArgs(builder, sourceType: "nint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4"));
            getArgs(builder, sourceType: "nint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4"));
            getArgs(builder, sourceType: "nint", destType: "long", expectedImplicitIL: conv("conv.i8"), expectedExplicitIL: conv("conv.i8"));
            getArgs(builder, sourceType: "nint", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i8"), expectedCheckedIL: conv("conv.ovf.u8")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, sourceType: "nint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            getArgs(builder, sourceType: "nint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            getArgs(builder, sourceType: "nint", destType: "decimal", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "nint", destType: "System.IntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nint", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "char"));
            getArgs(builder, sourceType: "nint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1", "sbyte"));
            getArgs(builder, sourceType: "nint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1", "byte"));
            getArgs(builder, sourceType: "nint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2", "short"));
            getArgs(builder, sourceType: "nint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2", "ushort"));
            getArgs(builder, sourceType: "nint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4", "int"));
            getArgs(builder, sourceType: "nint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4", "uint"));
            getArgs(builder, sourceType: "nint", destType: "long?", expectedImplicitIL: convToNullableT("conv.i8", "long"), expectedExplicitIL: convToNullableT("conv.i8", "long"));
            getArgs(builder, sourceType: "nint", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i8", "ulong"), expectedCheckedIL: convToNullableT("conv.ovf.u8", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, sourceType: "nint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            getArgs(builder, sourceType: "nint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            getArgs(builder, sourceType: "nint", destType: "decimal?", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}");
            getArgs(builder, sourceType: "nint", destType: "System.IntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nint", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nint?""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint?", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  ret
}");
            getArgs(builder, sourceType: "nint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i8", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u8", "nint")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, sourceType: "nint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nint"));
            getArgs(builder, sourceType: "nint?", destType: "decimal", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  call       ""decimal decimal.op_Implicit(long)""
  IL_0011:  ret
}");
            getArgs(builder, sourceType: "nint?", destType: "System.IntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "nint?", destType: "System.UIntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "char"));
            getArgs(builder, sourceType: "nint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            getArgs(builder, sourceType: "nint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            getArgs(builder, sourceType: "nint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2", "nint", "short"));
            getArgs(builder, sourceType: "nint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            getArgs(builder, sourceType: "nint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4", "nint", "int"));
            getArgs(builder, sourceType: "nint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            getArgs(builder, sourceType: "nint?", destType: "long?", expectedImplicitIL: convFromToNullableT("conv.i8", "nint", "long"), expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "long"));
            getArgs(builder, sourceType: "nint?", destType: "ulong?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i8", "nint", "ulong"), expectedCheckedIL: convFromToNullableT("conv.ovf.u8", "nint", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, sourceType: "nint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nint", "float"), null);
            getArgs(builder, sourceType: "nint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nint", "double"), null);
            // PROTOTYPE:
            //getArgs(builder, "nint?", "decimal?", null, null);
            getArgs(builder, sourceType: "nint?", destType: "System.IntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nint?", destType: "System.UIntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "object", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "string", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "void*", destType: "nuint", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "bool", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "sbyte", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "byte", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "short", destType: "nuint", expectedImplicitIL: conv("conv.i"), expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "ushort", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "int", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "uint", destType: "nuint", expectedImplicitIL: conv("conv.u"), expectedExplicitIL: conv("conv.u"));
            getArgs(builder, sourceType: "long", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "ulong", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u.un"));
            getArgs(builder, sourceType: "nint", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "nuint", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "float", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "double", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u"), expectedCheckedIL: conv("conv.ovf.u"));
            getArgs(builder, sourceType: "decimal", destType: "nuint", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "System.IntPtr", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.UIntPtr", destType: "nuint", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "bool?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "char"));
            getArgs(builder, sourceType: "sbyte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "sbyte"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "sbyte"));
            getArgs(builder, sourceType: "byte?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "byte"));
            getArgs(builder, sourceType: "short?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "short"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "short"));
            getArgs(builder, sourceType: "ushort?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ushort"));
            getArgs(builder, sourceType: "int?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i", "int"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "int"));
            getArgs(builder, sourceType: "uint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "uint"));
            getArgs(builder, sourceType: "long?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "long"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "long"));
            getArgs(builder, sourceType: "ulong?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "ulong"), expectedCheckedIL: convFromNullableT("conv.ovf.u.un", "ulong"));
            getArgs(builder, sourceType: "nint?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "nint"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "nint"));
            getArgs(builder, sourceType: "nuint?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "float?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "float"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "float"));
            getArgs(builder, sourceType: "double?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u", "double"), expectedCheckedIL: convFromNullableT("conv.ovf.u", "double"));
            getArgs(builder, sourceType: "decimal?", destType: "nuint", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0011:  ret
}");
            getArgs(builder, sourceType: "System.IntPtr?", destType: "nuint", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.UIntPtr?", destType: "nuint", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "object", destType: "nuint?", expectedImplicitIL: null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "string", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "void*", destType: "nuint?", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nuint?..ctor(nuint)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "bool", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            getArgs(builder, sourceType: "sbyte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "byte", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            getArgs(builder, sourceType: "short", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.i", "nuint"), expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "ushort", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            getArgs(builder, sourceType: "int", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "uint", destType: "nuint?", expectedImplicitIL: convToNullableT("conv.u", "nuint"), expectedExplicitIL: convToNullableT("conv.u", "nuint"));
            getArgs(builder, sourceType: "long", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "ulong", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u.un", "nuint"));
            getArgs(builder, sourceType: "nint", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "nuint", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "float", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "double", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u", "nuint"), expectedCheckedIL: convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, sourceType: "decimal", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.IntPtr", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.UIntPtr", destType: "nuint?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""nuint?..ctor(nuint)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "bool?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "char?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "char", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "char", "nuint"));
            getArgs(builder, sourceType: "sbyte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "sbyte", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            getArgs(builder, sourceType: "byte?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "byte", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "byte", "nuint"));
            getArgs(builder, sourceType: "short?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedExplicitIL: convFromToNullableT("conv.i", "short", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "short", "nuint"));
            getArgs(builder, sourceType: "ushort?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "ushort", "nuint"));
            getArgs(builder, sourceType: "int?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i", "int", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "int", "nuint"));
            getArgs(builder, sourceType: "uint?", destType: "nuint?", expectedImplicitIL: convFromToNullableT("conv.u", "uint", "nuint"), expectedExplicitIL: convFromToNullableT("conv.u", "uint", "nuint"));
            getArgs(builder, sourceType: "long?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "long", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "long", "nuint"));
            getArgs(builder, sourceType: "ulong?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "ulong", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            getArgs(builder, sourceType: "nint?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "nint", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "float?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "float", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "float", "nuint"));
            getArgs(builder, sourceType: "double?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u", "double", "nuint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u", "double", "nuint"));
            // PROTOTYPE:
            //getArgs(builder, "decimal?", "nuint?", null, null);
            getArgs(builder, sourceType: "System.IntPtr?", destType: "nuint?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "System.UIntPtr?", destType: "nuint?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nuint", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nuint", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nuint", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint", destType: "char", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            getArgs(builder, sourceType: "nuint", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i1"), expectedCheckedIL: conv("conv.ovf.i1.un"));
            getArgs(builder, sourceType: "nuint", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u1"), expectedCheckedIL: conv("conv.ovf.u1.un"));
            getArgs(builder, sourceType: "nuint", destType: "short", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i2"), expectedCheckedIL: conv("conv.ovf.i2.un"));
            getArgs(builder, sourceType: "nuint", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u2"), expectedCheckedIL: conv("conv.ovf.u2.un"));
            getArgs(builder, sourceType: "nuint", destType: "int", expectedImplicitIL: null, expectedExplicitIL: conv("conv.i4"), expectedCheckedIL: conv("conv.ovf.i4.un"));
            getArgs(builder, sourceType: "nuint", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u4"), expectedCheckedIL: conv("conv.ovf.u4.un"));
            getArgs(builder, sourceType: "nuint", destType: "long", expectedImplicitIL: null, expectedExplicitIL: conv("conv.u8"), expectedCheckedIL: conv("conv.ovf.i8.un")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, sourceType: "nuint", destType: "ulong", expectedImplicitIL: conv("conv.u8"), expectedExplicitIL: conv("conv.u8"));
            getArgs(builder, sourceType: "nuint", destType: "float", expectedImplicitIL: conv("conv.r4"), expectedExplicitIL: conv("conv.r4"));
            getArgs(builder, sourceType: "nuint", destType: "double", expectedImplicitIL: conv("conv.r8"), expectedExplicitIL: conv("conv.r8"));
            getArgs(builder, sourceType: "nuint", destType: "decimal", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  ret
}");
            getArgs(builder, sourceType: "nuint", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint", destType: "System.UIntPtr", expectedImplicitIL: convNone, expectedExplicitIL: convNone);
            getArgs(builder, sourceType: "nuint", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "char"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "char"));
            getArgs(builder, sourceType: "nuint", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i1", "sbyte"), expectedCheckedIL: convToNullableT("conv.ovf.i1.un", "sbyte"));
            getArgs(builder, sourceType: "nuint", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u1", "byte"), expectedCheckedIL: convToNullableT("conv.ovf.u1.un", "byte"));
            getArgs(builder, sourceType: "nuint", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i2", "short"), expectedCheckedIL: convToNullableT("conv.ovf.i2.un", "short"));
            getArgs(builder, sourceType: "nuint", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u2", "ushort"), expectedCheckedIL: convToNullableT("conv.ovf.u2.un", "ushort"));
            getArgs(builder, sourceType: "nuint", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.i4", "int"), expectedCheckedIL: convToNullableT("conv.ovf.i4.un", "int"));
            getArgs(builder, sourceType: "nuint", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u4", "uint"), expectedCheckedIL: convToNullableT("conv.ovf.u4.un", "uint"));
            getArgs(builder, sourceType: "nuint", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convToNullableT("conv.u8", "long"), expectedCheckedIL: convToNullableT("conv.ovf.i8.un", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, sourceType: "nuint", destType: "ulong?", expectedImplicitIL: convToNullableT("conv.u8", "ulong"), expectedExplicitIL: convToNullableT("conv.u8", "ulong"));
            getArgs(builder, sourceType: "nuint", destType: "float?", expectedImplicitIL: convToNullableT("conv.r4", "float"), expectedExplicitIL: convToNullableT("conv.r4", "float"), null);
            getArgs(builder, sourceType: "nuint", destType: "double?", expectedImplicitIL: convToNullableT("conv.r8", "double"), expectedExplicitIL: convToNullableT("conv.r8", "double"), null);
            getArgs(builder, sourceType: "nuint", destType: "decimal?", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}");
            getArgs(builder, sourceType: "nuint", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint", destType: "System.UIntPtr?",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.UIntPtr?..ctor(System.UIntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "object",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""nuint?""
  IL_0006:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "string", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint?", destType: "void*", expectedImplicitIL: null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "bool", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint?", destType: "char", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "sbyte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i1.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "byte", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u1", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u1.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "short", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i2.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "ushort", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u2", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u2.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "int", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.i4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i4.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "uint", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u4", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.u4.un", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "long", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"), expectedCheckedIL: convFromNullableT("conv.ovf.i8.un", "nuint")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, sourceType: "nuint?", destType: "ulong", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.u8", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "float", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r4", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "double", expectedImplicitIL: null, expectedExplicitIL: convFromNullableT("conv.r8", "nuint"));
            getArgs(builder, sourceType: "nuint?", destType: "decimal", expectedImplicitIL: null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_0011:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "System.IntPtr", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint?", destType: "System.UIntPtr", expectedImplicitIL: null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, sourceType: "nuint?", destType: "bool?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint?", destType: "char?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "char"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            getArgs(builder, sourceType: "nuint?", destType: "sbyte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i1", "nuint", "sbyte"), expectedCheckedIL: convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            getArgs(builder, sourceType: "nuint?", destType: "byte?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u1", "nuint", "byte"), expectedCheckedIL: convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            getArgs(builder, sourceType: "nuint?", destType: "short?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i2", "nuint", "short"), expectedCheckedIL: convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            getArgs(builder, sourceType: "nuint?", destType: "ushort?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u2", "nuint", "ushort"), expectedCheckedIL: convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            getArgs(builder, sourceType: "nuint?", destType: "int?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.i4", "nuint", "int"), expectedCheckedIL: convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            getArgs(builder, sourceType: "nuint?", destType: "uint?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u4", "nuint", "uint"), expectedCheckedIL: convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            getArgs(builder, sourceType: "nuint?", destType: "long?", expectedImplicitIL: null, expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "long"), expectedCheckedIL: convFromToNullableT("conv.ovf.i8.un", "nuint", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, sourceType: "nuint?", destType: "ulong?", expectedImplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"), expectedExplicitIL: convFromToNullableT("conv.u8", "nuint", "ulong"));
            getArgs(builder, sourceType: "nuint?", destType: "float?", expectedImplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), expectedExplicitIL: convFromToNullableT("conv.r4", "nuint", "float"), null);
            getArgs(builder, sourceType: "nuint?", destType: "double?", expectedImplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), expectedExplicitIL: convFromToNullableT("conv.r8", "nuint", "double"), null);
            // PROTOTYPE:
            //getArgs(builder, "nuint?", "decimal?", null, null);
            getArgs(builder, sourceType: "nuint?", destType: "System.IntPtr?", expectedImplicitIL: null, expectedExplicitIL: null);
            getArgs(builder, sourceType: "nuint?", destType: "System.UIntPtr?", expectedImplicitIL: convNone, expectedExplicitIL: convNone);

            return builder.ToImmutableAndFree();
        }

        [Theory]
        [MemberData(nameof(ConversionsData))]
        public void Conversions(string sourceType,
            string destType,
            string expectedIL,
            bool skipTypeChecks,
            bool useExplicitCast,
            bool useChecked,
            int expectedErrorCode)
        {
            bool useUnsafeContext = useUnsafe(sourceType) || useUnsafe(destType);
            string value = "value";
            if (useExplicitCast)
            {
                value = $"({destType})value";
            }
            var expectedDiagnostics = expectedErrorCode == 0 ?
                Array.Empty<DiagnosticDescription>() :
                new[] { Diagnostic((ErrorCode)expectedErrorCode, value).WithArguments(sourceType, destType) };
            if (useChecked)
            {
                value = $"checked({value})";
            }
            string source =
$@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}{destType} Convert({sourceType} value)
    {{
        return {value};
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
            var typeInfo = model.GetTypeInfo(expr);

            if (!skipTypeChecks)
            {
                Assert.Equal(sourceType, typeInfo.Type.ToString());
                Assert.Equal(destType, typeInfo.ConvertedType.ToString());
            }

            if (expectedIL != null)
            {
                var verifier = CompileAndVerify(comp, verify: useUnsafeContext ? Verification.Skipped : Verification.Passes);
                verifier.VerifyIL("Program.Convert", expectedIL);
            }

            static bool useUnsafe(string type) => type == "void*";
        }

        // PROTOTYPE: Test unary operator- with `static IntPtr operator-(IntPtr)` defined on System.IntPtr. (Should be ignored for `nint`.)
        public static IEnumerable<object[]> UnaryOperatorsData()
        {
            static void getArgs(ArrayBuilder<object[]> builder, string op, string opType, string expectedSymbol = null, DiagnosticDescription diagnostic = null)
            {
                if (expectedSymbol == null && diagnostic == null)
                {
                    diagnostic = Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, opType);
                }
                builder.Add(new object[] { op, opType, expectedSymbol, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>() });
            }

            var builder = new ArrayBuilder<object[]>();

            getArgs(builder, "+", "nint", "nint nint.op_UnaryPlus(nint value)");
            getArgs(builder, "+", "nuint", "nuint nuint.op_UnaryPlus(nuint value)");
            getArgs(builder, "-", "nint", "nint nint.op_UnaryMinus(nint value)");
            getArgs(builder, "-", "nuint", null);
            getArgs(builder, "~", "nint", "nint nint.op_UnaryNot(nint value)");
            getArgs(builder, "~", "nuint", "nuint nuint.op_UnaryNot(nuint value)");

            // PROTOTYPE: Test nint? and nuint?
            return builder;
        }

        [Theory(Skip = "PROTOTYPE")]
        [MemberData(nameof(UnaryOperatorsData))]
        public void UnaryOperators(string op, string opType, string expectedSymbol, DiagnosticDescription[] expectedDiagnostics)
        {
            string source =
$@"class Program
{{
    static object Evaluate({opType} operand)
    {{
        return {op}operand;
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

            if (expectedDiagnostics.Length == 0)
            {
                CompileAndVerify(comp);
            }
        }

        public static IEnumerable<object[]> BinaryOperatorsData()
        {
            static void getArgs(ArrayBuilder<object[]> builder, string op, string leftType, string rightType, string expectedSymbol1 = null, string expectedSymbol2 = "", DiagnosticDescription[] diagnostics1 = null, DiagnosticDescription[] diagnostics2 = null)
            {
                getArgs1(builder, op, leftType, rightType, expectedSymbol1, diagnostics1);
                getArgs1(builder, op, rightType, leftType, expectedSymbol2 == "" ? expectedSymbol1 : expectedSymbol2, diagnostics2 ?? diagnostics1);
            }

            static void getArgs1(ArrayBuilder<object[]> builder, string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] diagnostics)
            {
                if (expectedSymbol == null && diagnostics == null)
                {
                    diagnostics = new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, leftType, rightType) };
                }
                builder.Add(new object[] { op, leftType, rightType, expectedSymbol, diagnostics ?? Array.Empty<DiagnosticDescription>() });
            }

            var builder = new ArrayBuilder<object[]>();

            // Arithmetic
            getArgs(builder, "*", "nint", "object");
            getArgs(builder, "*", "nint", "string");
            getArgs(builder, "*", "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") });
            getArgs(builder, "*", "nint", "bool");
            getArgs(builder, "*", "nint", "char", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "sbyte", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "byte", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "short", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "ushort", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "int", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "uint", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint", "nint", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "nuint", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint", "long", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint", "ulong", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint", "float", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nint", "double", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nint", "decimal");
            getArgs(builder, "*", "nint", "System.IntPtr");
            getArgs(builder, "*", "nint", "System.UIntPtr");
            getArgs(builder, "*", "nint", "bool?");
            getArgs(builder, "*", "nint", "char?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "sbyte?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "byte?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "short?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "ushort?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "int?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "uint?", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint", "nint?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint", "nuint?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint", "long?", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint", "ulong?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint", "float?", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nint", "double?", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nint", "decimal?");
            getArgs(builder, "*", "nint", "System.IntPtr?");
            getArgs(builder, "*", "nint", "System.UIntPtr?");
            getArgs(builder, "*", "nint", "object");
            getArgs(builder, "*", "nint?", "string");
            getArgs(builder, "*", "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") });
            getArgs(builder, "*", "nint?", "bool");
            getArgs(builder, "*", "nint?", "char", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "sbyte", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "byte", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "short", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "ushort", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "int", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "uint", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint?", "nint", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "nuint", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint?", "long", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint?", "ulong", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint?", "float", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nint?", "double", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nint?", "decimal");
            getArgs(builder, "*", "nint?", "System.IntPtr");
            getArgs(builder, "*", "nint?", "System.UIntPtr");
            getArgs(builder, "*", "nint?", "bool?");
            getArgs(builder, "*", "nint?", "char?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "sbyte?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "byte?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "short?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "ushort?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "int?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "uint?", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint?", "nint?", "nint nint.op_Multiply(nint left, nint right)");
            getArgs(builder, "*", "nint?", "nuint?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint?", "long?", "long long.op_Multiply(long left, long right)");
            getArgs(builder, "*", "nint?", "ulong?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nint?", "float?", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nint?", "double?", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nint?", "decimal?");
            getArgs(builder, "*", "nint?", "System.IntPtr?");
            getArgs(builder, "*", "nint?", "System.UIntPtr?");
            getArgs(builder, "*", "nuint", "object");
            getArgs(builder, "*", "nuint", "string");
            getArgs(builder, "*", "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") });
            getArgs(builder, "*", "nuint", "bool");
            getArgs(builder, "*", "nuint", "char", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "sbyte", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "byte", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "short", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "ushort", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "int", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "uint", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "nint", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "nuint", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "long", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "ulong", "ulong ulong.op_Multiply(ulong left, ulong right)");
            getArgs(builder, "*", "nuint", "float", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nuint", "double", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nuint", "decimal");
            getArgs(builder, "*", "nuint", "System.IntPtr");
            getArgs(builder, "*", "nuint", "System.UIntPtr");
            getArgs(builder, "*", "nuint", "bool?");
            getArgs(builder, "*", "nuint", "char?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "sbyte?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "byte?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "short?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "ushort?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "int?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "uint?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "nint?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "nuint?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint", "long?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint", "ulong?", "ulong ulong.op_Multiply(ulong left, ulong right)");
            getArgs(builder, "*", "nuint", "float?", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nuint", "double?", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nuint", "decimal?");
            getArgs(builder, "*", "nuint", "System.IntPtr?");
            getArgs(builder, "*", "nuint", "System.UIntPtr?");
            getArgs(builder, "*", "nuint?", "object");
            getArgs(builder, "*", "nuint?", "string");
            getArgs(builder, "*", "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x * y").WithArguments("*", "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, "x * y") });
            getArgs(builder, "*", "nuint?", "bool");
            getArgs(builder, "*", "nuint?", "char", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "sbyte", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "byte", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "short", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "ushort", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "int", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "uint", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "nint", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "nuint", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "long", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "ulong", "ulong ulong.op_Multiply(ulong left, ulong right)");
            getArgs(builder, "*", "nuint?", "float", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nuint?", "double", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nuint?", "decimal");
            getArgs(builder, "*", "nuint?", "System.IntPtr");
            getArgs(builder, "*", "nuint?", "System.UIntPtr");
            getArgs(builder, "*", "nuint?", "bool?");
            getArgs(builder, "*", "nuint?", "char?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "sbyte?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "byte?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "short?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "ushort?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "int?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "uint?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "nint?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "nuint?", "nuint nuint.op_Multiply(nuint left, nuint right)");
            getArgs(builder, "*", "nuint?", "long?", "float float.op_Multiply(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "*", "nuint?", "ulong?", "ulong ulong.op_Multiply(ulong left, ulong right)");
            getArgs(builder, "*", "nuint?", "float?", "float float.op_Multiply(float left, float right)");
            getArgs(builder, "*", "nuint?", "double?", "double double.op_Multiply(double left, double right)");
            getArgs(builder, "*", "nuint?", "decimal?");
            getArgs(builder, "*", "nuint?", "System.IntPtr?");
            getArgs(builder, "*", "nuint?", "System.UIntPtr?");

            // Addition
            getArgs(builder, "+", "nint", "object");
            getArgs(builder, "+", "nint", "string", "string string.op_Addition(object left, string right)", "string string.op_Addition(string left, object right)");
            getArgs(builder, "+", "nint", "void*", "void* void*.op_Addition(long left, void* right)", "void* void*.op_Addition(void* left, long right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
            getArgs(builder, "+", "nint", "bool");
            getArgs(builder, "+", "nint", "char", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "sbyte", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "byte", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "short", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "ushort", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "int", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "uint", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "nint", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "nuint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint", "long", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "ulong", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint", "float", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nint", "double", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nint", "decimal");
            getArgs(builder, "+", "nint", "System.IntPtr");
            getArgs(builder, "+", "nint", "System.UIntPtr");
            getArgs(builder, "+", "nint", "bool?");
            getArgs(builder, "+", "nint", "char?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "sbyte?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "byte?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "short?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "ushort?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "int?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "uint?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "nint?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint", "nuint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint", "long?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "ulong?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint", "float?", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nint", "double?", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nint", "decimal?");
            getArgs(builder, "+", "nint", "System.IntPtr?");
            getArgs(builder, "+", "nint", "System.UIntPtr?");
            getArgs(builder, "+", "nint", "object");
            getArgs(builder, "+", "nint?", "string", "string string.op_Addition(object left, string right)", "string string.op_Addition(string left, object right)");
            getArgs(builder, "+", "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments("+", "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments("+", "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
            getArgs(builder, "+", "nint?", "bool");
            getArgs(builder, "+", "nint?", "char", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "sbyte", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "byte", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "short", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "ushort", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "int", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "uint", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "nint", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "nuint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint?", "long", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "ulong", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint?", "float", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nint?", "double", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nint?", "decimal");
            getArgs(builder, "+", "nint?", "System.IntPtr");
            getArgs(builder, "+", "nint?", "System.UIntPtr");
            getArgs(builder, "+", "nint?", "bool?");
            getArgs(builder, "+", "nint?", "char?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "sbyte?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "byte?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "short?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "ushort?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "int?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "uint?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "nint?", "nint nint.op_Addition(nint left, nint right)");
            getArgs(builder, "+", "nint?", "nuint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint?", "long?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "ulong?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nint?", "float?", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nint?", "double?", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nint?", "decimal?");
            getArgs(builder, "+", "nint?", "System.IntPtr?");
            getArgs(builder, "+", "nint?", "System.UIntPtr?");
            getArgs(builder, "+", "nuint", "object");
            getArgs(builder, "+", "nuint", "string", "string string.op_Addition(object left, string right)", "string string.op_Addition(string left, object right)");
            getArgs(builder, "+", "nuint", "void*", "void* void*.op_Addition(ulong left, void* right)", "void* void*.op_Addition(void* left, ulong right)", new[] { Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
            getArgs(builder, "+", "nuint", "bool");
            getArgs(builder, "+", "nuint", "char", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "sbyte", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "byte", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "short", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "ushort", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "int", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "uint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "nint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "nuint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "long", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "ulong", "ulong ulong.op_Addition(ulong left, ulong right)");
            getArgs(builder, "+", "nuint", "float", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nuint", "double", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nuint", "decimal");
            getArgs(builder, "+", "nuint", "System.IntPtr");
            getArgs(builder, "+", "nuint", "System.UIntPtr");
            getArgs(builder, "+", "nuint", "bool?");
            getArgs(builder, "+", "nuint", "char?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "sbyte?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "byte?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "short?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "ushort?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "int?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "uint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "nint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "nuint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "long?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint", "ulong?", "ulong ulong.op_Addition(ulong left, ulong right)");
            getArgs(builder, "+", "nuint", "float?", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nuint", "double?", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nuint", "decimal?");
            getArgs(builder, "+", "nuint", "System.IntPtr?");
            getArgs(builder, "+", "nuint", "System.UIntPtr?");
            getArgs(builder, "+", "nuint?", "object");
            getArgs(builder, "+", "nuint?", "string", "string string.op_Addition(object left, string right)", "string string.op_Addition(string left, object right)");
            getArgs(builder, "+", "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments("+", "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x + y").WithArguments("+", "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, "x + y") });
            getArgs(builder, "+", "nuint?", "bool");
            getArgs(builder, "+", "nuint?", "char", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "sbyte", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "byte", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "short", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "ushort", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "int", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "uint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "nint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "nuint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "long", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "ulong", "ulong ulong.op_Addition(ulong left, ulong right)");
            getArgs(builder, "+", "nuint?", "float", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nuint?", "double", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nuint?", "decimal");
            getArgs(builder, "+", "nuint?", "System.IntPtr");
            getArgs(builder, "+", "nuint?", "System.UIntPtr");
            getArgs(builder, "+", "nuint?", "bool?");
            getArgs(builder, "+", "nuint?", "char?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "sbyte?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "byte?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "short?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "ushort?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "int?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "uint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "nint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "nuint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "long?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "+", "nuint?", "ulong?", "ulong ulong.op_Addition(ulong left, ulong right)");
            getArgs(builder, "+", "nuint?", "float?", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nuint?", "double?", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nuint?", "decimal?");
            getArgs(builder, "+", "nuint?", "System.IntPtr?");
            getArgs(builder, "+", "nuint?", "System.UIntPtr?");

            // Shift
            getArgs(builder, "<<", "nint", "object");
            getArgs(builder, "<<", "nint", "string");
            getArgs(builder, "<<", "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") });
            getArgs(builder, "<<", "nint", "bool");
            getArgs(builder, "<<", "nint", "char", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "sbyte", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "byte", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "short", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "ushort", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "int", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "uint");
            getArgs(builder, "<<", "nint", "nint");
            getArgs(builder, "<<", "nint", "nuint");
            getArgs(builder, "<<", "nint", "long");
            getArgs(builder, "<<", "nint", "ulong");
            getArgs(builder, "<<", "nint", "float");
            getArgs(builder, "<<", "nint", "double");
            getArgs(builder, "<<", "nint", "decimal");
            getArgs(builder, "<<", "nint", "System.IntPtr");
            getArgs(builder, "<<", "nint", "System.UIntPtr");
            getArgs(builder, "<<", "nint", "bool?");
            getArgs(builder, "<<", "nint", "char?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "sbyte?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "byte?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "short?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "ushort?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "int?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint", "uint?");
            getArgs(builder, "<<", "nint", "nint?");
            getArgs(builder, "<<", "nint", "nuint?");
            getArgs(builder, "<<", "nint", "long?");
            getArgs(builder, "<<", "nint", "ulong?");
            getArgs(builder, "<<", "nint", "float?");
            getArgs(builder, "<<", "nint", "double?");
            getArgs(builder, "<<", "nint", "decimal?");
            getArgs(builder, "<<", "nint", "System.IntPtr?");
            getArgs(builder, "<<", "nint", "System.UIntPtr?");
            getArgs(builder, "<<", "nint", "object");
            getArgs(builder, "<<", "nint?", "string");
            getArgs(builder, "<<", "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") });
            getArgs(builder, "<<", "nint?", "bool");
            getArgs(builder, "<<", "nint?", "char", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "sbyte", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "byte", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "short", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "ushort", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "int", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "uint");
            getArgs(builder, "<<", "nint?", "nint");
            getArgs(builder, "<<", "nint?", "nuint");
            getArgs(builder, "<<", "nint?", "long");
            getArgs(builder, "<<", "nint?", "ulong");
            getArgs(builder, "<<", "nint?", "float");
            getArgs(builder, "<<", "nint?", "double");
            getArgs(builder, "<<", "nint?", "decimal");
            getArgs(builder, "<<", "nint?", "System.IntPtr");
            getArgs(builder, "<<", "nint?", "System.UIntPtr");
            getArgs(builder, "<<", "nint?", "bool?");
            getArgs(builder, "<<", "nint?", "char?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "sbyte?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "byte?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "short?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "ushort?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "int?", "nint nint.op_LeftShift(nint left, int right)", null);
            getArgs(builder, "<<", "nint?", "uint?");
            getArgs(builder, "<<", "nint?", "nint?");
            getArgs(builder, "<<", "nint?", "nuint?");
            getArgs(builder, "<<", "nint?", "long?");
            getArgs(builder, "<<", "nint?", "ulong?");
            getArgs(builder, "<<", "nint?", "float?");
            getArgs(builder, "<<", "nint?", "double?");
            getArgs(builder, "<<", "nint?", "decimal?");
            getArgs(builder, "<<", "nint?", "System.IntPtr?");
            getArgs(builder, "<<", "nint?", "System.UIntPtr?");
            getArgs(builder, "<<", "nuint", "object");
            getArgs(builder, "<<", "nuint", "string");
            getArgs(builder, "<<", "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") });
            getArgs(builder, "<<", "nuint", "bool");
            getArgs(builder, "<<", "nuint", "char", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "sbyte", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "byte", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "short", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "ushort", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "int", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "uint");
            getArgs(builder, "<<", "nuint", "nint");
            getArgs(builder, "<<", "nuint", "nuint");
            getArgs(builder, "<<", "nuint", "long");
            getArgs(builder, "<<", "nuint", "ulong");
            getArgs(builder, "<<", "nuint", "float");
            getArgs(builder, "<<", "nuint", "double");
            getArgs(builder, "<<", "nuint", "decimal");
            getArgs(builder, "<<", "nuint", "System.IntPtr");
            getArgs(builder, "<<", "nuint", "System.UIntPtr");
            getArgs(builder, "<<", "nuint", "bool?");
            getArgs(builder, "<<", "nuint", "char?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "sbyte?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "byte?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "short?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "ushort?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "int?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint", "uint?");
            getArgs(builder, "<<", "nuint", "nint?");
            getArgs(builder, "<<", "nuint", "nuint?");
            getArgs(builder, "<<", "nuint", "long?");
            getArgs(builder, "<<", "nuint", "ulong?");
            getArgs(builder, "<<", "nuint", "float?");
            getArgs(builder, "<<", "nuint", "double?");
            getArgs(builder, "<<", "nuint", "decimal?");
            getArgs(builder, "<<", "nuint", "System.IntPtr?");
            getArgs(builder, "<<", "nuint", "System.UIntPtr?");
            getArgs(builder, "<<", "nuint?", "object");
            getArgs(builder, "<<", "nuint?", "string");
            getArgs(builder, "<<", "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x << y").WithArguments("<<", "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, "x << y") });
            getArgs(builder, "<<", "nuint?", "bool");
            getArgs(builder, "<<", "nuint?", "char", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "sbyte", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "byte", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "short", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "ushort", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "int", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "uint");
            getArgs(builder, "<<", "nuint?", "nint");
            getArgs(builder, "<<", "nuint?", "nuint");
            getArgs(builder, "<<", "nuint?", "long");
            getArgs(builder, "<<", "nuint?", "ulong");
            getArgs(builder, "<<", "nuint?", "float");
            getArgs(builder, "<<", "nuint?", "double");
            getArgs(builder, "<<", "nuint?", "decimal");
            getArgs(builder, "<<", "nuint?", "System.IntPtr");
            getArgs(builder, "<<", "nuint?", "System.UIntPtr");
            getArgs(builder, "<<", "nuint?", "bool?");
            getArgs(builder, "<<", "nuint?", "char?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "sbyte?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "byte?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "short?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "ushort?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "int?", "nuint nuint.op_LeftShift(nuint left, int right)", null);
            getArgs(builder, "<<", "nuint?", "uint?");
            getArgs(builder, "<<", "nuint?", "nint?");
            getArgs(builder, "<<", "nuint?", "nuint?");
            getArgs(builder, "<<", "nuint?", "long?");
            getArgs(builder, "<<", "nuint?", "ulong?");
            getArgs(builder, "<<", "nuint?", "float?");
            getArgs(builder, "<<", "nuint?", "double?");
            getArgs(builder, "<<", "nuint?", "decimal?");
            getArgs(builder, "<<", "nuint?", "System.IntPtr?");
            getArgs(builder, "<<", "nuint?", "System.UIntPtr?");

            // Equality
            getArgs(builder, "==", "nint", "object");
            getArgs(builder, "==", "nint", "string");
            getArgs(builder, "==", "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "nint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "void*", "nint") });
            getArgs(builder, "==", "nint", "bool");
            getArgs(builder, "==", "nint", "char", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "sbyte", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "byte", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "short", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "ushort", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "int", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "uint", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint", "nint", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "nuint", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint", "long", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint", "ulong", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint", "float", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nint", "double", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nint", "decimal"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            //getArgs(builder, "==", "nint", "System.IntPtr"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nint", "System.UIntPtr");
            getArgs(builder, "==", "nint", "bool?");
            getArgs(builder, "==", "nint", "char?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "sbyte?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "byte?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "short?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "ushort?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "int?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "uint?", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint", "nint?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint", "nuint?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint", "long?", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint", "ulong?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint", "float?", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nint", "double?", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nint", "decimal?"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            //getArgs(builder, "==", "nint", "System.IntPtr?"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nint", "System.UIntPtr?");
            getArgs(builder, "==", "nint", "object");
            getArgs(builder, "==", "nint?", "string");
            getArgs(builder, "==", "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "nint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "void*", "nint?") });
            getArgs(builder, "==", "nint?", "bool");
            getArgs(builder, "==", "nint?", "char", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "sbyte", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "byte", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "short", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "ushort", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "int", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "uint", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint?", "nint", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "nuint", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint?", "long", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint?", "ulong", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint?", "float", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nint?", "double", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nint?", "decimal"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            //getArgs(builder, "==", "nint?", "System.IntPtr"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nint?", "System.UIntPtr");
            getArgs(builder, "==", "nint?", "bool?");
            getArgs(builder, "==", "nint?", "char?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "sbyte?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "byte?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "short?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "ushort?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "int?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "uint?", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint?", "nint?", "bool nint.op_Equality(nint left, nint right)");
            getArgs(builder, "==", "nint?", "nuint?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint?", "long?", "bool long.op_Equality(long left, long right)");
            getArgs(builder, "==", "nint?", "ulong?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nint?", "float?", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nint?", "double?", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nint?", "decimal?"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            //getArgs(builder, "==", "nint?", "System.IntPtr?"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nint?", "System.UIntPtr?");
            getArgs(builder, "==", "nuint", "object");
            getArgs(builder, "==", "nuint", "string");
            getArgs(builder, "==", "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "nuint", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "void*", "nuint") });
            getArgs(builder, "==", "nuint", "bool");
            getArgs(builder, "==", "nuint", "char", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "sbyte", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "byte", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "short", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "ushort", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "int", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "uint", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "nint", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "nuint", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "long", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "ulong", "bool ulong.op_Equality(ulong left, ulong right)");
            getArgs(builder, "==", "nuint", "float", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nuint", "double", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nuint", "decimal"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            getArgs(builder, "==", "nuint", "System.IntPtr");
            //getArgs(builder, "==", "nuint", "System.UIntPtr"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nuint", "bool?");
            getArgs(builder, "==", "nuint", "char?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "sbyte?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "byte?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "short?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "ushort?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "int?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "uint?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "nint?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "nuint?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint", "long?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint", "ulong?", "bool ulong.op_Equality(ulong left, ulong right)");
            getArgs(builder, "==", "nuint", "float?", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nuint", "double?", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nuint", "decimal?"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            getArgs(builder, "==", "nuint", "System.IntPtr?");
            //getArgs(builder, "==", "nuint", "System.UIntPtr?"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nuint?", "object");
            getArgs(builder, "==", "nuint?", "string");
            getArgs(builder, "==", "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "nuint?", "void*") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x == y").WithArguments("==", "void*", "nuint?") });
            getArgs(builder, "==", "nuint?", "bool");
            getArgs(builder, "==", "nuint?", "char", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "sbyte", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "byte", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "short", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "ushort", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "int", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "uint", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "nint", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "nuint", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "long", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "ulong", "bool ulong.op_Equality(ulong left, ulong right)");
            getArgs(builder, "==", "nuint?", "float", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nuint?", "double", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nuint?", "decimal"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            getArgs(builder, "==", "nuint?", "System.IntPtr");
            //getArgs(builder, "==", "nuint?", "System.UIntPtr"); // PROTOTYPE: Not handled.
            getArgs(builder, "==", "nuint?", "bool?");
            getArgs(builder, "==", "nuint?", "char?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "sbyte?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "byte?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "short?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "ushort?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "int?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "uint?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "nint?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "nuint?", "bool nuint.op_Equality(nuint left, nuint right)");
            getArgs(builder, "==", "nuint?", "long?", "bool float.op_Equality(float left, float right)"); // PROTOTYPE: Is it correct to promote args to float?
            getArgs(builder, "==", "nuint?", "ulong?", "bool ulong.op_Equality(ulong left, ulong right)");
            getArgs(builder, "==", "nuint?", "float?", "bool float.op_Equality(float left, float right)");
            getArgs(builder, "==", "nuint?", "double?", "bool double.op_Equality(double left, double right)");
            getArgs(builder, "==", "nuint?", "decimal?"); // PROTOTYPE: bool decimal.op_Equality(decimal left, decimal right)
            getArgs(builder, "==", "nuint?", "System.IntPtr?");
            //getArgs(builder, "==", "nuint?", "System.UIntPtr?"); // PROTOTYPE: Not handled.

            // Logical
            getArgs(builder, "|", "nint", "object");
            getArgs(builder, "|", "nint", "string");
            getArgs(builder, "|", "nint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "nint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "void*", "nint"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") });
            getArgs(builder, "|", "nint", "bool");
            getArgs(builder, "|", "nint", "char", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "sbyte", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "byte", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "short", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "ushort", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "int", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "uint", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint", "nint", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "nuint");
            getArgs(builder, "|", "nint", "long", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint", "ulong");
            getArgs(builder, "|", "nint", "float");
            getArgs(builder, "|", "nint", "double");
            getArgs(builder, "|", "nint", "decimal");
            getArgs(builder, "|", "nint", "System.IntPtr");
            getArgs(builder, "|", "nint", "System.UIntPtr");
            getArgs(builder, "|", "nint", "bool?");
            getArgs(builder, "|", "nint", "char?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "sbyte?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "byte?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "short?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "ushort?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "int?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "uint?", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint", "nint?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint", "nuint?");
            getArgs(builder, "|", "nint", "long?", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint", "ulong?");
            getArgs(builder, "|", "nint", "float?");
            getArgs(builder, "|", "nint", "double?");
            getArgs(builder, "|", "nint", "decimal?");
            getArgs(builder, "|", "nint", "System.IntPtr?");
            getArgs(builder, "|", "nint", "System.UIntPtr?");
            getArgs(builder, "|", "nint", "object");
            getArgs(builder, "|", "nint?", "string");
            getArgs(builder, "|", "nint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "nint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "void*", "nint?"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") });
            getArgs(builder, "|", "nint?", "bool");
            getArgs(builder, "|", "nint?", "char", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "sbyte", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "byte", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "short", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "ushort", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "int", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "uint", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint?", "nint", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "nuint");
            getArgs(builder, "|", "nint?", "long", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint?", "ulong");
            getArgs(builder, "|", "nint?", "float");
            getArgs(builder, "|", "nint?", "double");
            getArgs(builder, "|", "nint?", "decimal");
            getArgs(builder, "|", "nint?", "System.IntPtr");
            getArgs(builder, "|", "nint?", "System.UIntPtr");
            getArgs(builder, "|", "nint?", "bool?");
            getArgs(builder, "|", "nint?", "char?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "sbyte?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "byte?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "short?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "ushort?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "int?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "uint?", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint?", "nint?", "nint nint.op_BitwiseOr(nint left, nint right)");
            getArgs(builder, "|", "nint?", "nuint?");
            getArgs(builder, "|", "nint?", "long?", "long long.op_BitwiseOr(long left, long right)");
            getArgs(builder, "|", "nint?", "ulong?");
            getArgs(builder, "|", "nint?", "float?");
            getArgs(builder, "|", "nint?", "double?");
            getArgs(builder, "|", "nint?", "decimal?");
            getArgs(builder, "|", "nint?", "System.IntPtr?");
            getArgs(builder, "|", "nint?", "System.UIntPtr?");
            getArgs(builder, "|", "nuint", "object");
            getArgs(builder, "|", "nuint", "string");
            getArgs(builder, "|", "nuint", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "nuint", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "void*", "nuint"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") });
            getArgs(builder, "|", "nuint", "bool");
            getArgs(builder, "|", "nuint", "char", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "sbyte", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "byte", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "short", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "ushort", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "int");
            getArgs(builder, "|", "nuint", "uint", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint", "nint");
            getArgs(builder, "|", "nuint", "nuint", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "long");
            getArgs(builder, "|", "nuint", "ulong", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint", "float");
            getArgs(builder, "|", "nuint", "double");
            getArgs(builder, "|", "nuint", "decimal");
            getArgs(builder, "|", "nuint", "System.IntPtr");
            getArgs(builder, "|", "nuint", "System.UIntPtr");
            getArgs(builder, "|", "nuint", "bool?");
            getArgs(builder, "|", "nuint", "char?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "sbyte?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "byte?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "short?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "ushort?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "int?");
            getArgs(builder, "|", "nuint", "uint?", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint", "nint?");
            getArgs(builder, "|", "nuint", "nuint?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint", "long?");
            getArgs(builder, "|", "nuint", "ulong?", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint", "float?");
            getArgs(builder, "|", "nuint", "double?");
            getArgs(builder, "|", "nuint", "decimal?");
            getArgs(builder, "|", "nuint", "System.IntPtr?");
            getArgs(builder, "|", "nuint", "System.UIntPtr?");
            getArgs(builder, "|", "nuint?", "object");
            getArgs(builder, "|", "nuint?", "string");
            getArgs(builder, "|", "nuint?", "void*", null, null, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "nuint?", "void*"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") }, new[] { Diagnostic(ErrorCode.ERR_BadBinaryOps, "x | y").WithArguments("|", "void*", "nuint?"), Diagnostic(ErrorCode.ERR_VoidError, "x | y") });
            getArgs(builder, "|", "nuint?", "bool");
            getArgs(builder, "|", "nuint?", "char", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "sbyte", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "byte", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "short", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "ushort", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "int");
            getArgs(builder, "|", "nuint?", "uint", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint?", "nint");
            getArgs(builder, "|", "nuint?", "nuint", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "long");
            getArgs(builder, "|", "nuint?", "ulong", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint?", "float");
            getArgs(builder, "|", "nuint?", "double");
            getArgs(builder, "|", "nuint?", "decimal");
            getArgs(builder, "|", "nuint?", "System.IntPtr");
            getArgs(builder, "|", "nuint?", "System.UIntPtr");
            getArgs(builder, "|", "nuint?", "bool?");
            getArgs(builder, "|", "nuint?", "char?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "sbyte?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "byte?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "short?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "ushort?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "int?");
            getArgs(builder, "|", "nuint?", "uint?", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint?", "nint?");
            getArgs(builder, "|", "nuint?", "nuint?", "nuint nuint.op_BitwiseOr(nuint left, nuint right)");
            getArgs(builder, "|", "nuint?", "long?");
            getArgs(builder, "|", "nuint?", "ulong?", "ulong ulong.op_BitwiseOr(ulong left, ulong right)");
            getArgs(builder, "|", "nuint?", "float?");
            getArgs(builder, "|", "nuint?", "double?");
            getArgs(builder, "|", "nuint?", "decimal?");
            getArgs(builder, "|", "nuint?", "System.IntPtr?");
            getArgs(builder, "|", "nuint?", "System.UIntPtr?");

            return builder;
        }

        [Theory]
        [MemberData(nameof(BinaryOperatorsData))]
        public void BinaryOperators(string op, string leftType, string rightType, string expectedSymbol, DiagnosticDescription[] expectedDiagnostics)
        {
            bool useUnsafeContext = useUnsafe(leftType) || useUnsafe(rightType);
            string source =
$@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}object Evaluate({leftType} x, {rightType} y)
    {{
        return x {op} y;
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(expectedSymbol, symbolInfo.Symbol?.ToDisplayString(SymbolDisplayFormat.TestFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));

            if (expectedDiagnostics.Length == 0)
            {
                CompileAndVerify(comp);
            }

            static bool useUnsafe(string type) => type == "void*";
        }

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
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr
  IL_0003:  ret
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
            verifier.VerifyIL("Program.ShiftLeft",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shl
  IL_0003:  ret
}");
            verifier.VerifyIL("Program.ShiftRight",
@"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  shr.un
  IL_0003:  ret
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
    }
}
