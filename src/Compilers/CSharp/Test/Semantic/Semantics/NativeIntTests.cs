﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        // - @nint
        // - Type.nint, Namespace.nint
        // - All locations from SyntaxFacts.IsInTypeOnlyContext
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

            getArgs(builder, "object", "nint", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}");
            getArgs(builder, "string", "nint", null, null);
            getArgs(builder, "void*", "nint", null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            getArgs(builder, "bool", "nint", null, null);
            getArgs(builder, "char", "nint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "sbyte", "nint", conv("conv.i"), conv("conv.i"));
            getArgs(builder, "byte", "nint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "short", "nint", conv("conv.i"), conv("conv.i"));
            getArgs(builder, "ushort", "nint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "int", "nint", conv("conv.i"), conv("conv.i"));
            getArgs(builder, "uint", "nint", null, conv("conv.u"), conv("conv.ovf.i.un"));
            getArgs(builder, "long", "nint", null, conv("conv.i"), conv("conv.ovf.i"));
            getArgs(builder, "ulong", "nint", null, conv("conv.i"), conv("conv.ovf.i.un"));
            getArgs(builder, "nint", "nint", convNone, convNone);
            getArgs(builder, "nuint", "nint", null, conv("conv.i"), conv("conv.ovf.i.un"));
            getArgs(builder, "float", "nint", null, conv("conv.i"), conv("conv.ovf.i"));
            getArgs(builder, "double", "nint", null, conv("conv.i"), conv("conv.ovf.i"));
            getArgs(builder, "decimal", "nint", null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  ret
}");
            getArgs(builder, "System.IntPtr", "nint", convNone, convNone);
            getArgs(builder, "System.UIntPtr", "nint", null, null);
            getArgs(builder, "bool?", "nint", null, null);
            getArgs(builder, "char?", "nint", null, convFromNullableT("conv.u", "char"));
            getArgs(builder, "sbyte?", "nint", null, convFromNullableT("conv.i", "sbyte"));
            getArgs(builder, "byte?", "nint", null, convFromNullableT("conv.u", "byte"));
            getArgs(builder, "short?", "nint", null, convFromNullableT("conv.i", "short"));
            getArgs(builder, "ushort?", "nint", null, convFromNullableT("conv.u", "ushort"));
            getArgs(builder, "int?", "nint", null, convFromNullableT("conv.i", "int"));
            getArgs(builder, "uint?", "nint", null, convFromNullableT("conv.u", "uint"), convFromNullableT("conv.ovf.i.un", "uint"));
            getArgs(builder, "long?", "nint", null, convFromNullableT("conv.i", "long"), convFromNullableT("conv.ovf.i", "long"));
            getArgs(builder, "ulong?", "nint", null, convFromNullableT("conv.i", "ulong"), convFromNullableT("conv.ovf.i.un", "ulong"));
            getArgs(builder, "nint?", "nint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "nuint?", "nint", null, convFromNullableT("conv.i", "nuint"), convFromNullableT("conv.ovf.i.un", "nuint"));
            getArgs(builder, "float?", "nint", null, convFromNullableT("conv.i", "float"), convFromNullableT("conv.ovf.i", "float"));
            getArgs(builder, "double?", "nint", null, convFromNullableT("conv.i", "double"), convFromNullableT("conv.ovf.i", "double"));
            getArgs(builder, "decimal?", "nint", null,
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
            getArgs(builder, "System.IntPtr?", "nint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "System.UIntPtr?", "nint", null, null);
            getArgs(builder, "object", "nint?", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint?""
  IL_0006:  ret
}");
            getArgs(builder, "string", "nint?", null, null);
            getArgs(builder, "void*", "nint?", null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nint?..ctor(nint)""
  IL_000b:  ret
}");
            getArgs(builder, "bool", "nint?", null, null);
            getArgs(builder, "char", "nint?", convToNullableT("conv.u", "nint"), convToNullableT("conv.u", "nint"));
            getArgs(builder, "sbyte", "nint?", convToNullableT("conv.i", "nint"), convToNullableT("conv.i", "nint"));
            getArgs(builder, "byte", "nint?", convToNullableT("conv.u", "nint"), convToNullableT("conv.u", "nint"));
            getArgs(builder, "short", "nint?", convToNullableT("conv.i", "nint"), convToNullableT("conv.i", "nint"));
            getArgs(builder, "ushort", "nint?", convToNullableT("conv.u", "nint"), convToNullableT("conv.u", "nint"));
            getArgs(builder, "int", "nint?", convToNullableT("conv.i", "nint"), convToNullableT("conv.i", "nint"));
            getArgs(builder, "uint", "nint?", null, convToNullableT("conv.u", "nint"), convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, "long", "nint?", null, convToNullableT("conv.i", "nint"), convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, "ulong", "nint?", null, convToNullableT("conv.i", "nint"), convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, "nint", "nint?",
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
            getArgs(builder, "nuint", "nint?", null, convToNullableT("conv.i", "nint"), convToNullableT("conv.ovf.i.un", "nint"));
            getArgs(builder, "float", "nint?", null, convToNullableT("conv.i", "nint"), convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, "double", "nint?", null, convToNullableT("conv.i", "nint"), convToNullableT("conv.ovf.i", "nint"));
            getArgs(builder, "decimal", "nint?", null, null);
            getArgs(builder, "System.IntPtr", "nint?",
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
            getArgs(builder, "System.UIntPtr", "nint?", null, null);
            getArgs(builder, "bool?", "nint?", null, null);
            getArgs(builder, "char?", "nint?", convFromToNullableT("conv.u", "char", "nint"), convFromToNullableT("conv.u", "char", "nint"));
            getArgs(builder, "sbyte?", "nint?", convFromToNullableT("conv.i", "sbyte", "nint"), convFromToNullableT("conv.i", "sbyte", "nint"));
            getArgs(builder, "byte?", "nint?", convFromToNullableT("conv.u", "byte", "nint"), convFromToNullableT("conv.u", "byte", "nint"));
            getArgs(builder, "short?", "nint?", convFromToNullableT("conv.i", "short", "nint"), convFromToNullableT("conv.i", "short", "nint"));
            getArgs(builder, "ushort?", "nint?", convFromToNullableT("conv.u", "ushort", "nint"), convFromToNullableT("conv.u", "ushort", "nint"));
            getArgs(builder, "int?", "nint?", convFromToNullableT("conv.i", "int", "nint"), convFromToNullableT("conv.i", "int", "nint"));
            getArgs(builder, "uint?", "nint?", null, convFromToNullableT("conv.u", "uint", "nint"), convFromToNullableT("conv.ovf.i.un", "uint", "nint"));
            getArgs(builder, "long?", "nint?", null, convFromToNullableT("conv.i", "long", "nint"), convFromToNullableT("conv.ovf.i", "long", "nint"));
            getArgs(builder, "ulong?", "nint?", null, convFromToNullableT("conv.i", "ulong", "nint"), convFromToNullableT("conv.ovf.i.un", "ulong", "nint"));
            getArgs(builder, "nint?", "nint?", convNone, convNone);
            getArgs(builder, "nuint?", "nint?", null, convFromToNullableT("conv.i", "nuint", "nint"), convFromToNullableT("conv.ovf.i.un", "nuint", "nint"));
            getArgs(builder, "float?", "nint?", null, convFromToNullableT("conv.i", "float", "nint"), convFromToNullableT("conv.ovf.i", "float", "nint"));
            getArgs(builder, "double?", "nint?", null, convFromToNullableT("conv.i", "double", "nint"), convFromToNullableT("conv.ovf.i", "double", "nint"));
            // PROTOTYPE:
            //getArgs(builder, "decimal?", "nint?", null, null);
            getArgs(builder, "System.IntPtr?", "nint?", convNone, convNone);
            getArgs(builder, "System.UIntPtr?", "nint?", null, null);
            getArgs(builder, "nint", "object",
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
            getArgs(builder, "nint", "string", null, null);
            getArgs(builder, "nint", "void*", null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, "nint", "bool", null, null);
            getArgs(builder, "nint", "char", null, conv("conv.u2"), conv("conv.ovf.u2"));
            getArgs(builder, "nint", "sbyte", null, conv("conv.i1"), conv("conv.ovf.i1"));
            getArgs(builder, "nint", "byte", null, conv("conv.u1"), conv("conv.ovf.u1"));
            getArgs(builder, "nint", "short", null, conv("conv.i2"), conv("conv.ovf.i2"));
            getArgs(builder, "nint", "ushort", null, conv("conv.u2"), conv("conv.ovf.u2"));
            getArgs(builder, "nint", "int", null, conv("conv.i4"), conv("conv.ovf.i4"));
            getArgs(builder, "nint", "uint", null, conv("conv.u4"), conv("conv.ovf.u4"));
            getArgs(builder, "nint", "long", conv("conv.i8"), conv("conv.i8"));
            getArgs(builder, "nint", "ulong", null, conv("conv.i8"), conv("conv.ovf.u8")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, "nint", "float", conv("conv.r4"), conv("conv.r4"));
            getArgs(builder, "nint", "double", conv("conv.r8"), conv("conv.r8"));
            getArgs(builder, "nint", "decimal", null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  ret
}");
            getArgs(builder, "nint", "System.IntPtr", convNone, convNone);
            getArgs(builder, "nint", "System.UIntPtr", null, null);
            getArgs(builder, "nint", "bool?", null, null);
            getArgs(builder, "nint", "char?", null, convToNullableT("conv.u2", "char"), convToNullableT("conv.ovf.u2", "char"));
            getArgs(builder, "nint", "sbyte?", null, convToNullableT("conv.i1", "sbyte"), convToNullableT("conv.ovf.i1", "sbyte"));
            getArgs(builder, "nint", "byte?", null, convToNullableT("conv.u1", "byte"), convToNullableT("conv.ovf.u1", "byte"));
            getArgs(builder, "nint", "short?", null, convToNullableT("conv.i2", "short"), convToNullableT("conv.ovf.i2", "short"));
            getArgs(builder, "nint", "ushort?", null, convToNullableT("conv.u2", "ushort"), convToNullableT("conv.ovf.u2", "ushort"));
            getArgs(builder, "nint", "int?", null, convToNullableT("conv.i4", "int"), convToNullableT("conv.ovf.i4", "int"));
            getArgs(builder, "nint", "uint?", null, convToNullableT("conv.u4", "uint"), convToNullableT("conv.ovf.u4", "uint"));
            getArgs(builder, "nint", "long?", convToNullableT("conv.i8", "long"), convToNullableT("conv.i8", "long"));
            getArgs(builder, "nint", "ulong?", null, convToNullableT("conv.i8", "ulong"), convToNullableT("conv.ovf.u8", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, "nint", "float?", convToNullableT("conv.r4", "float"), convToNullableT("conv.r4", "float"), null);
            getArgs(builder, "nint", "double?", convToNullableT("conv.r8", "double"), convToNullableT("conv.r8", "double"), null);
            getArgs(builder, "nint", "decimal?", null,
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
            getArgs(builder, "nint", "System.IntPtr?",
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
            getArgs(builder, "nint", "System.UIntPtr?", null, null);
            getArgs(builder, "nint?", "object",
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
            getArgs(builder, "nint?", "string", null, null);
            getArgs(builder, "nint?", "void*", null,
// PROTOTYPE: Should be conv.i.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_000c:  ret
}");
            getArgs(builder, "nint?", "bool", null, null);
            getArgs(builder, "nint?", "char", null, convFromNullableT("conv.u2", "nint"), convFromNullableT("conv.ovf.u2", "nint"));
            getArgs(builder, "nint?", "sbyte", null, convFromNullableT("conv.i1", "nint"), convFromNullableT("conv.ovf.i1", "nint"));
            getArgs(builder, "nint?", "byte", null, convFromNullableT("conv.u1", "nint"), convFromNullableT("conv.ovf.u1", "nint"));
            getArgs(builder, "nint?", "short", null, convFromNullableT("conv.i2", "nint"), convFromNullableT("conv.ovf.i2", "nint"));
            getArgs(builder, "nint?", "ushort", null, convFromNullableT("conv.u2", "nint"), convFromNullableT("conv.ovf.u2", "nint"));
            getArgs(builder, "nint?", "int", null, convFromNullableT("conv.i4", "nint"), convFromNullableT("conv.ovf.i4", "nint"));
            getArgs(builder, "nint?", "uint", null, convFromNullableT("conv.u4", "nint"), convFromNullableT("conv.ovf.u4", "nint"));
            getArgs(builder, "nint?", "long", null, convFromNullableT("conv.i8", "nint"));
            getArgs(builder, "nint?", "ulong", null, convFromNullableT("conv.i8", "nint"), convFromNullableT("conv.ovf.u8", "nint")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, "nint?", "float", null, convFromNullableT("conv.r4", "nint"));
            getArgs(builder, "nint?", "double", null, convFromNullableT("conv.r8", "nint"));
            getArgs(builder, "nint?", "decimal", null,
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
            getArgs(builder, "nint?", "System.IntPtr", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "nint?", "System.UIntPtr", null, null);
            getArgs(builder, "nint?", "bool?", null, null);
            getArgs(builder, "nint?", "char?", null, convFromToNullableT("conv.u2", "nint", "char"), convFromToNullableT("conv.ovf.u2", "nint", "char"));
            getArgs(builder, "nint?", "sbyte?", null, convFromToNullableT("conv.i1", "nint", "sbyte"), convFromToNullableT("conv.ovf.i1", "nint", "sbyte"));
            getArgs(builder, "nint?", "byte?", null, convFromToNullableT("conv.u1", "nint", "byte"), convFromToNullableT("conv.ovf.u1", "nint", "byte"));
            getArgs(builder, "nint?", "short?", null, convFromToNullableT("conv.i2", "nint", "short"), convFromToNullableT("conv.ovf.i2", "nint", "short"));
            getArgs(builder, "nint?", "ushort?", null, convFromToNullableT("conv.u2", "nint", "ushort"), convFromToNullableT("conv.ovf.u2", "nint", "ushort"));
            getArgs(builder, "nint?", "int?", null, convFromToNullableT("conv.i4", "nint", "int"), convFromToNullableT("conv.ovf.i4", "nint", "int"));
            getArgs(builder, "nint?", "uint?", null, convFromToNullableT("conv.u4", "nint", "uint"), convFromToNullableT("conv.ovf.u4", "nint", "uint"));
            getArgs(builder, "nint?", "long?", convFromToNullableT("conv.i8", "nint", "long"), convFromToNullableT("conv.i8", "nint", "long"));
            getArgs(builder, "nint?", "ulong?", null, convFromToNullableT("conv.i8", "nint", "ulong"), convFromToNullableT("conv.ovf.u8", "nint", "ulong")); // PROTOTYPE: Why conv.i8 but conv.ovf.u8?
            getArgs(builder, "nint?", "float?", convFromToNullableT("conv.r4", "nint", "float"), convFromToNullableT("conv.r4", "nint", "float"), null);
            getArgs(builder, "nint?", "double?", convFromToNullableT("conv.r8", "nint", "double"), convFromToNullableT("conv.r8", "nint", "double"), null);
            // PROTOTYPE:
            //getArgs(builder, "nint?", "decimal?", null, null);
            getArgs(builder, "nint?", "System.IntPtr?", convNone, convNone);
            getArgs(builder, "nint?", "System.UIntPtr?", null, null);
            getArgs(builder, "object", "nuint", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint""
  IL_0006:  ret
}");
            getArgs(builder, "string", "nuint", null, null);
            getArgs(builder, "void*", "nuint", null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  ret
}");
            getArgs(builder, "bool", "nuint", null, null);
            getArgs(builder, "char", "nuint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "sbyte", "nuint", conv("conv.i"), conv("conv.i"), conv("conv.ovf.u"));
            getArgs(builder, "byte", "nuint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "short", "nuint", conv("conv.i"), conv("conv.i"), conv("conv.ovf.u"));
            getArgs(builder, "ushort", "nuint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "int", "nuint", null, conv("conv.i"), conv("conv.ovf.u"));
            getArgs(builder, "uint", "nuint", conv("conv.u"), conv("conv.u"));
            getArgs(builder, "long", "nuint", null, conv("conv.u"), conv("conv.ovf.u"));
            getArgs(builder, "ulong", "nuint", null, conv("conv.u"), conv("conv.ovf.u.un"));
            getArgs(builder, "nint", "nuint", null, conv("conv.u"), conv("conv.ovf.u"));
            getArgs(builder, "nuint", "nuint", convNone, convNone);
            getArgs(builder, "float", "nuint", null, conv("conv.u"), conv("conv.ovf.u"));
            getArgs(builder, "double", "nuint", null, conv("conv.u"), conv("conv.ovf.u"));
            getArgs(builder, "decimal", "nuint", null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_000b:  ret
}");
            getArgs(builder, "System.IntPtr", "nuint", null, null);
            getArgs(builder, "System.UIntPtr", "nuint", convNone, convNone);
            getArgs(builder, "bool?", "nuint", null, null);
            getArgs(builder, "char?", "nuint", null, convFromNullableT("conv.u", "char"));
            getArgs(builder, "sbyte?", "nuint", null, convFromNullableT("conv.i", "sbyte"), convFromNullableT("conv.ovf.u", "sbyte"));
            getArgs(builder, "byte?", "nuint", null, convFromNullableT("conv.u", "byte"));
            getArgs(builder, "short?", "nuint", null, convFromNullableT("conv.i", "short"), convFromNullableT("conv.ovf.u", "short"));
            getArgs(builder, "ushort?", "nuint", null, convFromNullableT("conv.u", "ushort"));
            getArgs(builder, "int?", "nuint", null, convFromNullableT("conv.i", "int"), convFromNullableT("conv.ovf.u", "int"));
            getArgs(builder, "uint?", "nuint", null, convFromNullableT("conv.u", "uint"));
            getArgs(builder, "long?", "nuint", null, convFromNullableT("conv.u", "long"), convFromNullableT("conv.ovf.u", "long"));
            getArgs(builder, "ulong?", "nuint", null, convFromNullableT("conv.u", "ulong"), convFromNullableT("conv.ovf.u.un", "ulong"));
            getArgs(builder, "nint?", "nuint", null, convFromNullableT("conv.u", "nint"), convFromNullableT("conv.ovf.u", "nint"));
            getArgs(builder, "nuint?", "nuint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "float?", "nuint", null, convFromNullableT("conv.u", "float"), convFromNullableT("conv.ovf.u", "float"));
            getArgs(builder, "double?", "nuint", null, convFromNullableT("conv.u", "double"), convFromNullableT("conv.ovf.u", "double"));
            getArgs(builder, "decimal?", "nuint", null,
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
            getArgs(builder, "System.IntPtr?", "nuint", null, null);
            getArgs(builder, "System.UIntPtr?", "nuint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "object", "nuint?", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nuint?""
  IL_0006:  ret
}");
            getArgs(builder, "string", "nuint?", null, null);
            getArgs(builder, "void*", "nuint?", null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(void*)""
  IL_0006:  newobj     ""nuint?..ctor(nuint)""
  IL_000b:  ret
}");
            getArgs(builder, "bool", "nuint?", null, null);
            getArgs(builder, "char", "nuint?", convToNullableT("conv.u", "nuint"), convToNullableT("conv.u", "nuint"));
            getArgs(builder, "sbyte", "nuint?", convToNullableT("conv.i", "nuint"), convToNullableT("conv.i", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "byte", "nuint?", convToNullableT("conv.u", "nuint"), convToNullableT("conv.u", "nuint"));
            getArgs(builder, "short", "nuint?", convToNullableT("conv.i", "nuint"), convToNullableT("conv.i", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "ushort", "nuint?", convToNullableT("conv.u", "nuint"), convToNullableT("conv.u", "nuint"));
            getArgs(builder, "int", "nuint?", null, convToNullableT("conv.i", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "uint", "nuint?", convToNullableT("conv.u", "nuint"), convToNullableT("conv.u", "nuint"));
            getArgs(builder, "long", "nuint?", null, convToNullableT("conv.u", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "ulong", "nuint?", null, convToNullableT("conv.u", "nuint"), convToNullableT("conv.ovf.u.un", "nuint"));
            getArgs(builder, "nint", "nuint?", null, convToNullableT("conv.u", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "nuint", "nuint?",
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
            getArgs(builder, "float", "nuint?", null, convToNullableT("conv.u", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "double", "nuint?", null, convToNullableT("conv.u", "nuint"), convToNullableT("conv.ovf.u", "nuint"));
            getArgs(builder, "decimal", "nuint?", null, null);
            getArgs(builder, "System.IntPtr", "nuint?", null, null);
            getArgs(builder, "System.UIntPtr", "nuint?",
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
            getArgs(builder, "bool?", "nuint?", null, null);
            getArgs(builder, "char?", "nuint?", convFromToNullableT("conv.u", "char", "nuint"), convFromToNullableT("conv.u", "char", "nuint"));
            getArgs(builder, "sbyte?", "nuint?", convFromToNullableT("conv.i", "sbyte", "nuint"), convFromToNullableT("conv.i", "sbyte", "nuint"), convFromToNullableT("conv.ovf.u", "sbyte", "nuint"));
            getArgs(builder, "byte?", "nuint?", convFromToNullableT("conv.u", "byte", "nuint"), convFromToNullableT("conv.u", "byte", "nuint"));
            getArgs(builder, "short?", "nuint?", convFromToNullableT("conv.i", "short", "nuint"), convFromToNullableT("conv.i", "short", "nuint"), convFromToNullableT("conv.ovf.u", "short", "nuint"));
            getArgs(builder, "ushort?", "nuint?", convFromToNullableT("conv.u", "ushort", "nuint"), convFromToNullableT("conv.u", "ushort", "nuint"));
            getArgs(builder, "int?", "nuint?", null, convFromToNullableT("conv.i", "int", "nuint"), convFromToNullableT("conv.ovf.u", "int", "nuint"));
            getArgs(builder, "uint?", "nuint?", convFromToNullableT("conv.u", "uint", "nuint"), convFromToNullableT("conv.u", "uint", "nuint"));
            getArgs(builder, "long?", "nuint?", null, convFromToNullableT("conv.u", "long", "nuint"), convFromToNullableT("conv.ovf.u", "long", "nuint"));
            getArgs(builder, "ulong?", "nuint?", null, convFromToNullableT("conv.u", "ulong", "nuint"), convFromToNullableT("conv.ovf.u.un", "ulong", "nuint"));
            getArgs(builder, "nint?", "nuint?", null, convFromToNullableT("conv.u", "nint", "nuint"), convFromToNullableT("conv.ovf.u", "nint", "nuint"));
            getArgs(builder, "nuint?", "nuint?", convNone, convNone);
            getArgs(builder, "float?", "nuint?", null, convFromToNullableT("conv.u", "float", "nuint"), convFromToNullableT("conv.ovf.u", "float", "nuint"));
            getArgs(builder, "double?", "nuint?", null, convFromToNullableT("conv.u", "double", "nuint"), convFromToNullableT("conv.ovf.u", "double", "nuint"));
            // PROTOTYPE:
            //getArgs(builder, "decimal?", "nuint?", null, null);
            getArgs(builder, "System.IntPtr?", "nuint?", null, null);
            getArgs(builder, "System.UIntPtr?", "nuint?", convNone, convNone);
            getArgs(builder, "nuint", "object",
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
            getArgs(builder, "nuint", "string", null, null);
            getArgs(builder, "nuint", "void*", null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  ret
}");
            getArgs(builder, "nuint", "bool", null, null);
            getArgs(builder, "nuint", "char", null, conv("conv.u2"), conv("conv.ovf.u2.un"));
            getArgs(builder, "nuint", "sbyte", null, conv("conv.i1"), conv("conv.ovf.i1.un"));
            getArgs(builder, "nuint", "byte", null, conv("conv.u1"), conv("conv.ovf.u1.un"));
            getArgs(builder, "nuint", "short", null, conv("conv.i2"), conv("conv.ovf.i2.un"));
            getArgs(builder, "nuint", "ushort", null, conv("conv.u2"), conv("conv.ovf.u2.un"));
            getArgs(builder, "nuint", "int", null, conv("conv.i4"), conv("conv.ovf.i4.un"));
            getArgs(builder, "nuint", "uint", null, conv("conv.u4"), conv("conv.ovf.u4.un"));
            getArgs(builder, "nuint", "long", null, conv("conv.u8"), conv("conv.ovf.i8.un")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, "nuint", "ulong", conv("conv.u8"), conv("conv.u8"));
            getArgs(builder, "nuint", "float", conv("conv.r4"), conv("conv.r4"));
            getArgs(builder, "nuint", "double", conv("conv.r8"), conv("conv.r8"));
            getArgs(builder, "nuint", "decimal", null,
// PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  ret
}");
            getArgs(builder, "nuint", "System.IntPtr", null, null);
            getArgs(builder, "nuint", "System.UIntPtr", convNone, convNone);
            getArgs(builder, "nuint", "bool?", null, null);
            getArgs(builder, "nuint", "char?", null, convToNullableT("conv.u2", "char"), convToNullableT("conv.ovf.u2.un", "char"));
            getArgs(builder, "nuint", "sbyte?", null, convToNullableT("conv.i1", "sbyte"), convToNullableT("conv.ovf.i1.un", "sbyte"));
            getArgs(builder, "nuint", "byte?", null, convToNullableT("conv.u1", "byte"), convToNullableT("conv.ovf.u1.un", "byte"));
            getArgs(builder, "nuint", "short?", null, convToNullableT("conv.i2", "short"), convToNullableT("conv.ovf.i2.un", "short"));
            getArgs(builder, "nuint", "ushort?", null, convToNullableT("conv.u2", "ushort"), convToNullableT("conv.ovf.u2.un", "ushort"));
            getArgs(builder, "nuint", "int?", null, convToNullableT("conv.i4", "int"), convToNullableT("conv.ovf.i4.un", "int"));
            getArgs(builder, "nuint", "uint?", null, convToNullableT("conv.u4", "uint"), convToNullableT("conv.ovf.u4.un", "uint"));
            getArgs(builder, "nuint", "long?", null, convToNullableT("conv.u8", "long"), convToNullableT("conv.ovf.i8.un", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, "nuint", "ulong?", convToNullableT("conv.u8", "ulong"), convToNullableT("conv.u8", "ulong"));
            getArgs(builder, "nuint", "float?", convToNullableT("conv.r4", "float"), convToNullableT("conv.r4", "float"), null);
            getArgs(builder, "nuint", "double?", convToNullableT("conv.r8", "double"), convToNullableT("conv.r8", "double"), null);
            getArgs(builder, "nuint", "decimal?", null,
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
            getArgs(builder, "nuint", "System.IntPtr?", null, null);
            getArgs(builder, "nuint", "System.UIntPtr?",
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
            getArgs(builder, "nuint?", "object",
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
            getArgs(builder, "nuint?", "string", null, null);
            getArgs(builder, "nuint?", "void*", null,
// PROTOTYPE: Should be conv.u.
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_000c:  ret
}");
            getArgs(builder, "nuint?", "bool", null, null);
            getArgs(builder, "nuint?", "char", null, convFromNullableT("conv.u2", "nuint"), convFromNullableT("conv.ovf.u2.un", "nuint"));
            getArgs(builder, "nuint?", "sbyte", null, convFromNullableT("conv.i1", "nuint"), convFromNullableT("conv.ovf.i1.un", "nuint"));
            getArgs(builder, "nuint?", "byte", null, convFromNullableT("conv.u1", "nuint"), convFromNullableT("conv.ovf.u1.un", "nuint"));
            getArgs(builder, "nuint?", "short", null, convFromNullableT("conv.i2", "nuint"), convFromNullableT("conv.ovf.i2.un", "nuint"));
            getArgs(builder, "nuint?", "ushort", null, convFromNullableT("conv.u2", "nuint"), convFromNullableT("conv.ovf.u2.un", "nuint"));
            getArgs(builder, "nuint?", "int", null, convFromNullableT("conv.i4", "nuint"), convFromNullableT("conv.ovf.i4.un", "nuint"));
            getArgs(builder, "nuint?", "uint", null, convFromNullableT("conv.u4", "nuint"), convFromNullableT("conv.ovf.u4.un", "nuint"));
            getArgs(builder, "nuint?", "long", null, convFromNullableT("conv.u8", "nuint"), convFromNullableT("conv.ovf.i8.un", "nuint")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, "nuint?", "ulong", null, convFromNullableT("conv.u8", "nuint"));
            getArgs(builder, "nuint?", "float", null, convFromNullableT("conv.r4", "nuint"));
            getArgs(builder, "nuint?", "double", null, convFromNullableT("conv.r8", "nuint"));
            getArgs(builder, "nuint?", "decimal", null,
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
            getArgs(builder, "nuint?", "System.IntPtr", null, null);
            getArgs(builder, "nuint?", "System.UIntPtr", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}");
            getArgs(builder, "nuint?", "bool?", null, null);
            getArgs(builder, "nuint?", "char?", null, convFromToNullableT("conv.u2", "nuint", "char"), convFromToNullableT("conv.ovf.u2.un", "nuint", "char"));
            getArgs(builder, "nuint?", "sbyte?", null, convFromToNullableT("conv.i1", "nuint", "sbyte"), convFromToNullableT("conv.ovf.i1.un", "nuint", "sbyte"));
            getArgs(builder, "nuint?", "byte?", null, convFromToNullableT("conv.u1", "nuint", "byte"), convFromToNullableT("conv.ovf.u1.un", "nuint", "byte"));
            getArgs(builder, "nuint?", "short?", null, convFromToNullableT("conv.i2", "nuint", "short"), convFromToNullableT("conv.ovf.i2.un", "nuint", "short"));
            getArgs(builder, "nuint?", "ushort?", null, convFromToNullableT("conv.u2", "nuint", "ushort"), convFromToNullableT("conv.ovf.u2.un", "nuint", "ushort"));
            getArgs(builder, "nuint?", "int?", null, convFromToNullableT("conv.i4", "nuint", "int"), convFromToNullableT("conv.ovf.i4.un", "nuint", "int"));
            getArgs(builder, "nuint?", "uint?", null, convFromToNullableT("conv.u4", "nuint", "uint"), convFromToNullableT("conv.ovf.u4.un", "nuint", "uint"));
            getArgs(builder, "nuint?", "long?", null, convFromToNullableT("conv.u8", "nuint", "long"), convFromToNullableT("conv.ovf.i8.un", "nuint", "long")); // PROTOTYPE: Why conv.u8 but conv.ovf.i8.un?
            getArgs(builder, "nuint?", "ulong?", convFromToNullableT("conv.u8", "nuint", "ulong"), convFromToNullableT("conv.u8", "nuint", "ulong"));
            getArgs(builder, "nuint?", "float?", convFromToNullableT("conv.r4", "nuint", "float"), convFromToNullableT("conv.r4", "nuint", "float"), null);
            getArgs(builder, "nuint?", "double?", convFromToNullableT("conv.r8", "nuint", "double"), convFromToNullableT("conv.r8", "nuint", "double"), null);
            // PROTOTYPE:
            //getArgs(builder, "nuint?", "decimal?", null, null);
            getArgs(builder, "nuint?", "System.IntPtr?", null, null);
            getArgs(builder, "nuint?", "System.UIntPtr?", convNone, convNone);

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

        // PROTOTYPE: Test other ops.
        public static IEnumerable<object[]> BinaryOperatorsData()
        {
            static void getArgs(ArrayBuilder<object[]> builder, string op, string leftType, string rightType, string expectedSymbol1 = null, string expectedSymbol2 = null, DiagnosticDescription[] diagnostics1 = null, DiagnosticDescription[] diagnostics2 = null)
            {
                getArgs1(builder, op, leftType, rightType, expectedSymbol1, diagnostics1);
                getArgs1(builder, op, rightType, leftType, expectedSymbol2 ?? expectedSymbol1, diagnostics2 ?? diagnostics1);
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
            getArgs(builder, "+", "nint", "nuint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
            getArgs(builder, "+", "nint", "long", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "ulong", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
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
            getArgs(builder, "+", "nint", "nuint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
            getArgs(builder, "+", "nint", "long?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint", "ulong?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
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
            getArgs(builder, "+", "nint?", "nuint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
            getArgs(builder, "+", "nint?", "long", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "ulong", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
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
            getArgs(builder, "+", "nint?", "nuint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
            getArgs(builder, "+", "nint?", "long?", "long long.op_Addition(long left, long right)");
            getArgs(builder, "+", "nint?", "ulong?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? long+ulong uses +(long, long).
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
            getArgs(builder, "+", "nuint", "int", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint", "uint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "nint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint", "nuint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "long", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
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
            getArgs(builder, "+", "nuint", "int?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint", "uint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "nint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint", "nuint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint", "long?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
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
            getArgs(builder, "+", "nuint?", "int", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint?", "uint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "nint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint?", "nuint", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "long", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
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
            getArgs(builder, "+", "nuint?", "int?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint?", "uint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "nint?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint?", "nuint?", "nuint nuint.op_Addition(nuint left, nuint right)");
            getArgs(builder, "+", "nuint?", "long?", "float float.op_Addition(float left, float right)"); // PROTOTYPE: Is +(float, float) correct? ulong+long uses +(long, long).
            getArgs(builder, "+", "nuint?", "ulong?", "ulong ulong.op_Addition(ulong left, ulong right)");
            getArgs(builder, "+", "nuint?", "float?", "float float.op_Addition(float left, float right)");
            getArgs(builder, "+", "nuint?", "double?", "double double.op_Addition(double left, double right)");
            getArgs(builder, "+", "nuint?", "decimal?");
            getArgs(builder, "+", "nuint?", "System.IntPtr?");
            getArgs(builder, "+", "nuint?", "System.UIntPtr?");

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
