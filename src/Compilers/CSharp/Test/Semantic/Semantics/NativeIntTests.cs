// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        // PROTOTYPE: Test nint? and nuint?
        // PROTOTYPE: Test checked(...)
        public static IEnumerable<object[]> ConversionsData()
        {
            string conv_none =
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
            yield return new object[] { "object", "nint", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}" };
            yield return new object[] { "string", "nint", null, null };
            yield return new object[] { "void*", "nint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}" };
            yield return new object[] { "bool", "nint", null, null };
            yield return new object[] { "char", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "sbyte", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "byte", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "short", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "ushort", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "int", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "uint", "nint", null, conv("conv.u") };
            yield return new object[] { "long", "nint", null, conv("conv.i") };
            yield return new object[] { "ulong", "nint", null, conv("conv.i") };
            yield return new object[] { "nint", "nint", conv_none, conv_none };
            yield return new object[] { "nuint", "nint", null, conv("conv.i") };
            yield return new object[] { "float", "nint", null, conv("conv.i") };
            yield return new object[] { "double", "nint", null, conv("conv.i") };
            yield return new object[] { "decimal", "nint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  ret
}" };
            yield return new object[] { "System.IntPtr", "nint", conv_none, conv_none };
            yield return new object[] { "System.UIntPtr", "nint", null, null };
            yield return new object[] { "bool?", "nint", null, null };
            yield return new object[] { "char?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""char char?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "sbyte?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""sbyte sbyte?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "byte?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""byte byte?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "short?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""short short?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "ushort?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""ushort ushort?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "int?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int int?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "uint?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""uint uint?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "long?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""long long?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "ulong?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""ulong ulong?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "nint?", "nint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "float?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""float float?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "double?", "nint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""double double?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "decimal?", "nint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""long decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_0011:  ret
}" };
            yield return new object[] { "System.IntPtr?", "nint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.IntPtr System.IntPtr?.Value.get""
  IL_0007:  ret
}" };
            yield return new object[] { "System.UIntPtr?", "nint", null, null };
            yield return new object[] { "nint", "object",
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
}" };
            yield return new object[] { "nint", "string", null, null };
            yield return new object[] { "nint", "void*", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  ret
}" };
            yield return new object[] { "nint", "bool", null, null };
            yield return new object[] { "nint", "char", null, conv("conv.u2") };
            yield return new object[] { "nint", "sbyte", null, conv("conv.i1") };
            yield return new object[] { "nint", "byte", null, conv("conv.u1") };
            yield return new object[] { "nint", "short", null, conv("conv.i2") };
            yield return new object[] { "nint", "ushort", null, conv("conv.u2") };
            yield return new object[] { "nint", "int", null, conv("conv.i4") };
            yield return new object[] { "nint", "uint", null, conv("conv.u4") };
            yield return new object[] { "nint", "long", conv("conv.i8"), conv("conv.i8") };
            yield return new object[] { "nint", "ulong", null, conv("conv.i8") };
            yield return new object[] { "nint", "float", conv("conv.r4"), conv("conv.r4") };
            yield return new object[] { "nint", "double", conv("conv.r8"), conv("conv.r8") };
            yield return new object[] { "nint", "decimal", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  ret
}" };
            yield return new object[] { "nint", "System.IntPtr", conv_none, conv_none };
            yield return new object[] { "nint", "System.UIntPtr", null, null };
            yield return new object[] { "nint", "bool?", null, null };
            yield return new object[] { "nint", "char?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  newobj     ""char?..ctor(char)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "sbyte?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  newobj     ""sbyte?..ctor(sbyte)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "byte?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  newobj     ""byte?..ctor(byte)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "short?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  newobj     ""short?..ctor(short)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "ushort?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  newobj     ""ushort?..ctor(ushort)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "int?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  newobj     ""int?..ctor(int)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "uint?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  newobj     ""uint?..ctor(uint)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "long?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""long?..ctor(long)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""long?..ctor(long)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "ulong?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""ulong?..ctor(ulong)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "float?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r4
  IL_0002:  newobj     ""float?..ctor(float)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r4
  IL_0002:  newobj     ""float?..ctor(float)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "double?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r8
  IL_0002:  newobj     ""double?..ctor(double)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r8
  IL_0002:  newobj     ""double?..ctor(double)""
  IL_0007:  ret
}" };
            yield return new object[] { "nint", "decimal?", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long System.IntPtr.op_Explicit(System.IntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(long)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}" };
            yield return new object[] { "nint", "System.IntPtr?",
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
}" };
            yield return new object[] { "nint", "System.UIntPtr?", null, null };
            yield return new object[] { "object", "nint", null,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""nint""
  IL_0006:  ret
}" };
            yield return new object[] { "string", "nint", null, null };
            yield return new object[] { "void*", "nint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(void*)""
  IL_0006:  ret
}" };
            yield return new object[] { "bool", "nint", null, null };
            yield return new object[] { "char", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "sbyte", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "byte", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "short", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "ushort", "nint", conv("conv.u"), conv("conv.u") };
            yield return new object[] { "int", "nint", conv("conv.i"), conv("conv.i") };
            yield return new object[] { "uint", "nint", null, conv("conv.u") };
            yield return new object[] { "long", "nint", null, conv("conv.i") };
            yield return new object[] { "ulong", "nint", null, conv("conv.i") };
            yield return new object[] { "nint", "nint", conv_none, conv_none };
            yield return new object[] { "nuint", "nint", null, conv("conv.i") };
            yield return new object[] { "float", "nint", null, conv("conv.i") };
            yield return new object[] { "double", "nint", null, conv("conv.i") };
            yield return new object[] { "decimal", "nint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""long decimal.op_Explicit(decimal)""
  IL_0006:  call       ""System.IntPtr System.IntPtr.op_Explicit(long)""
  IL_000b:  ret
}" };
            yield return new object[] { "System.IntPtr", "nint", conv_none, conv_none };
            yield return new object[] { "System.UIntPtr", "nint", null, null };
            yield return new object[] { "bool?", "nuint", null, null };
            yield return new object[] { "char?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""char char?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "sbyte?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""sbyte sbyte?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "byte?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""byte byte?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "short?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""short short?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "ushort?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""ushort ushort?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "int?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int int?.Value.get""
  IL_0007:  conv.i
  IL_0008:  ret
}" };
            yield return new object[] { "uint?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""uint uint?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "long?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""long long?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "ulong?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""ulong ulong?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "nint?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nint nint?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "nuint?", "nuint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""nuint nuint?.Value.get""
  IL_0007:  ret
}" };
            yield return new object[] { "float?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""float float?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "double?", "nuint", null,
@"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""double double?.Value.get""
  IL_0007:  conv.u
  IL_0008:  ret
}" };
            yield return new object[] { "decimal?", "nuint", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""decimal decimal?.Value.get""
  IL_0007:  call       ""ulong decimal.op_Explicit(decimal)""
  IL_000c:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0011:  ret
}" };
            yield return new object[] { "System.IntPtr?", "nuint", null, null };
            yield return new object[] { "System.UIntPtr?", "nuint", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""System.UIntPtr System.UIntPtr?.Value.get""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "object",
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
}" };
            yield return new object[] { "nuint", "string", null, null };
            yield return new object[] { "nuint", "void*", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void* System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  ret
}" };
            yield return new object[] { "nuint", "bool", null, null };
            yield return new object[] { "nuint", "char", null, conv("conv.u2") };
            yield return new object[] { "nuint", "sbyte", null, conv("conv.i1") };
            yield return new object[] { "nuint", "byte", null, conv("conv.u1") };
            yield return new object[] { "nuint", "short", null, conv("conv.i2") };
            yield return new object[] { "nuint", "ushort", null, conv("conv.u2") };
            yield return new object[] { "nuint", "int", null, conv("conv.i4") };
            yield return new object[] { "nuint", "uint", null, conv("conv.u4") };
            yield return new object[] { "nuint", "long", null, conv("conv.u8") };
            yield return new object[] { "nuint", "ulong", conv("conv.u8"), conv("conv.u8") };
            yield return new object[] { "nuint", "float", conv("conv.r4"), conv("conv.r4") };
            yield return new object[] { "nuint", "double", conv("conv.r8"), conv("conv.r8") };
            yield return new object[] { "nuint", "decimal", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  ret
}" };
            yield return new object[] { "nuint", "System.IntPtr", null, null };
            yield return new object[] { "nuint", "System.UIntPtr", conv_none, conv_none };
            yield return new object[] { "nuint", "bool?", null, null };
            yield return new object[] { "nuint", "char?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  newobj     ""char?..ctor(char)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "sbyte?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  newobj     ""sbyte?..ctor(sbyte)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "byte?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  newobj     ""byte?..ctor(byte)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "short?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  newobj     ""short?..ctor(short)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "ushort?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  newobj     ""ushort?..ctor(ushort)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "int?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  newobj     ""int?..ctor(int)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "uint?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  newobj     ""uint?..ctor(uint)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "long?", null,
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  newobj     ""long?..ctor(long)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "ulong?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  newobj     ""ulong?..ctor(ulong)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  newobj     ""ulong?..ctor(ulong)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "float?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r4
  IL_0002:  newobj     ""float?..ctor(float)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r4
  IL_0002:  newobj     ""float?..ctor(float)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "double?",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r8
  IL_0002:  newobj     ""double?..ctor(double)""
  IL_0007:  ret
}",
@"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.r8
  IL_0002:  newobj     ""double?..ctor(double)""
  IL_0007:  ret
}" };
            yield return new object[] { "nuint", "decimal?", null,
                // PROTOTYPE: Is this explicit conversion expected?
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ulong System.UIntPtr.op_Explicit(System.UIntPtr)""
  IL_0006:  call       ""decimal decimal.op_Implicit(ulong)""
  IL_000b:  newobj     ""decimal?..ctor(decimal)""
  IL_0010:  ret
}" };
            yield return new object[] { "nuint", "System.IntPtr?", null, null };
            yield return new object[] { "nuint", "System.UIntPtr?",
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
}" };
        }

        [Theory]
        [MemberData(nameof(ConversionsData))]
        public void Conversions(string sourceType, string destType, string expectedImplicitIL, string expectedExplicitIL)
        {
            bool useUnsafeContext = useUnsafe(sourceType) || useUnsafe(destType);

            Conversion(
                sourceType,
                destType,
                expectedImplicitIL,
                skipTypeChecks: usesIntPtrOrUIntPtr(sourceType) || usesIntPtrOrUIntPtr(destType), // PROTOTYPE: Not distinguishing IntPtr from nint.
                useExplicitCast: false,
                useUnsafeContext: useUnsafeContext,
                expectedImplicitIL is null ?
                    expectedExplicitIL is null ? ErrorCode.ERR_NoImplicitConv : ErrorCode.ERR_NoImplicitConvCast :
                    0);
            Conversion(
                sourceType,
                destType,
                expectedExplicitIL,
                skipTypeChecks: true,
                useExplicitCast: true,
                useUnsafeContext: useUnsafeContext,
                expectedExplicitIL is null ? ErrorCode.ERR_NoExplicitConv : 0);

            static bool useUnsafe(string type) => type == "void*";
            static bool usesIntPtrOrUIntPtr(string type) => type.Contains("IntPtr");
        }

        private void Conversion(
            string sourceType,
            string destType,
            string expectedIL,
            bool skipTypeChecks,
            bool useExplicitCast,
            bool useUnsafeContext,
            ErrorCode expectedErrorCode)
        {
            string value = $"{(useExplicitCast ? $"({destType})" : "")}value";
            string source =
$@"class Program
{{
    static {(useUnsafeContext ? "unsafe " : "")}{destType} Convert({sourceType} value)
    {{
        return {value};
    }}
}}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithAllowUnsafe(useUnsafeContext), parseOptions: TestOptions.RegularPreview);
            var expectedDiagnostics = expectedErrorCode == 0 ?
                Array.Empty<DiagnosticDescription>() :
                new[] { Diagnostic(expectedErrorCode, value).WithArguments(sourceType, destType).WithLocation(5, 16) };
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
        // PROTOTYPE: Test with `static IntPtr operator-(IntPtr)` defined on System.IntPtr. (Should be ignored for `nint`.)

        public static IEnumerable<object[]> BinaryOperatorsData()
        {
            static object[] getArgs(string op, string leftType, string rightType, string expectedSymbol = null, DiagnosticDescription diagnostic = null)
            {
                if (expectedSymbol == null && diagnostic == null)
                {
                    diagnostic = Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, leftType, rightType);
                }
                return new object[] { op, leftType, rightType, expectedSymbol, diagnostic != null ? new[] { diagnostic } : Array.Empty<DiagnosticDescription>() };
            }

            yield return getArgs("+", "nint", "object");
            yield return getArgs("+", "nint", "string", "string string.op_Addition(object left, string right)");
            yield return getArgs("+", "nint", "void*", "void* void*.op_Addition(long left, void* right)", Diagnostic(ErrorCode.ERR_VoidError, "x + y"));
            yield return getArgs("+", "nint", "bool");
            yield return getArgs("+", "nint", "char", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "sbyte", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "byte", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "short", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "ushort", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "int", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "uint", "long long.op_Addition(long left, long right)");
            yield return getArgs("+", "nint", "nint", "nint nint.op_Addition(nint left, nint right)");
            yield return getArgs("+", "nint", "nuint", "float float.op_Addition(float left, float right)"); // PROTOTYPE: +(float, float) seems unfortunate.
            yield return getArgs("+", "nint", "long", "long long.op_Addition(long left, long right)");
            yield return getArgs("+", "nint", "ulong", "float float.op_Addition(float left, float right)"); // PROTOTYPE: +(float, float) seems unfortunate.
            yield return getArgs("+", "nint", "float", "float float.op_Addition(float left, float right)");
            yield return getArgs("+", "nint", "double", "double double.op_Addition(double left, double right)");
            yield return getArgs("+", "nint", "decimal");
            yield return getArgs("+", "nint", "System.IntPtr");
            yield return getArgs("+", "nint", "System.UIntPtr");
            // PROTOTYPE: Test nuint + {any}
            // PROTOTYPE: Test {any} + nint and {any} + nuint
            // PROTOTYPE: Test nint? and nuint?
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
