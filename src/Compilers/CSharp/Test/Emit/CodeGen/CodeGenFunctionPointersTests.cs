﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.UnitTests.FunctionPointerUtilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenFunctionPointersTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyFunctionPointers(
            string source,
            MetadataReference[]? references = null,
            Action<ModuleSymbol>? symbolValidator = null,
            string? expectedOutput = null,
            TargetFramework targetFramework = TargetFramework.Standard)
        {
            return CompileAndVerify(source, references, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe, symbolValidator: symbolValidator, expectedOutput: expectedOutput, verify: Verification.Skipped, targetFramework: targetFramework);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointersWithIl(string source, string ilStub, Action<ModuleSymbol>? symbolValidator = null, string? expectedOutput = null)
        {
            var comp = CreateCompilationWithIL(source, ilStub, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: symbolValidator, verify: Verification.Skipped);
        }

        private CSharpCompilation CreateCompilationWithFunctionPointers(string source, IEnumerable<MetadataReference>? references = null, CSharpCompilationOptions? options = null)
        {
            return CreateCompilation(source, references: references, options: options ?? TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
        }

        private CSharpCompilation CreateCompilationWithFunctionPointersAndIl(string source, string ilStub, IEnumerable<MetadataReference>? references = null)
        {
            return CreateCompilationWithIL(source, ilStub, references: references, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
        }

        [Theory]
        [InlineData("", CallingConvention.Default)]
        [InlineData("cdecl", CallingConvention.CDecl)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("thiscall", CallingConvention.ThisCall)]
        [InlineData("stdcall", CallingConvention.Standard)]
        internal void CallingConventions(string conventionString, CallingConvention expectedConvention)
        {
            var verifier = CompileAndVerifyFunctionPointers($@"
class C
{{
    public unsafe delegate* {conventionString}<string, int> M() => throw null;
}}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var funcPtr = m.ReturnType;

                VerifyFunctionPointerSymbol(funcPtr, expectedConvention,
                    (RefKind.None, IsSpecialType(SpecialType.System_Int32)),
                    (RefKind.None, IsSpecialType(SpecialType.System_String)));
            }
        }

        [Fact]
        public void RefParameters()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
class C
{
    public unsafe void M(delegate*<ref C, ref string, ref int[]> param1) => throw null;
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var funcPtr = m.ParameterTypesWithAnnotations[0].Type;

                VerifyFunctionPointerSymbol(funcPtr, CallingConvention.Default,
                    (RefKind.Ref, IsArrayType(IsSpecialType(SpecialType.System_Int32))),
                    (RefKind.Ref, IsTypeName("C")),
                    (RefKind.Ref, IsSpecialType(SpecialType.System_String)));
            }
        }

        [Fact]
        public void OutParameters()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
class C
{
    public unsafe void M(delegate*<out C, out string, ref int[]> param1) => throw null;
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var funcPtr = m.ParameterTypesWithAnnotations[0].Type;

                VerifyFunctionPointerSymbol(funcPtr, CallingConvention.Default,
                    (RefKind.Ref, IsArrayType(IsSpecialType(SpecialType.System_Int32))),
                    (RefKind.Out, IsTypeName("C")),
                    (RefKind.Out, IsSpecialType(SpecialType.System_String)));
            }
        }

        [Fact]
        public void NestedFunctionPointers()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe delegate* cdecl<delegate* stdcall<int, void>, void> M(delegate*<C, delegate*<S>> param1) => throw null;
}
public struct S
{
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var returnType = m.ReturnType;

                VerifyFunctionPointerSymbol(returnType, CallingConvention.CDecl,
                    (RefKind.None, IsVoidType()),
                    (RefKind.None, IsFunctionPointerTypeSymbol(CallingConvention.Standard,
                        (RefKind.None, IsVoidType()),
                        (RefKind.None, IsSpecialType(SpecialType.System_Int32)))
                        ));

                var paramType = m.Parameters[0].Type;
                VerifyFunctionPointerSymbol(paramType, CallingConvention.Default,
                    (RefKind.None, IsFunctionPointerTypeSymbol(CallingConvention.Default,
                        (RefKind.None, IsTypeName("S")))),
                    (RefKind.None, IsTypeName("C")));
            }
        }

        [Fact]
        public void InModifier()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe void M(delegate*<in string, in int, ref readonly bool> param) {}
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var paramType = m.Parameters[0].Type;

                VerifyFunctionPointerSymbol(paramType, CallingConvention.Default,
                    (RefKind.RefReadOnly, IsSpecialType(SpecialType.System_Boolean)),
                    (RefKind.In, IsSpecialType(SpecialType.System_String)),
                    (RefKind.In, IsSpecialType(SpecialType.System_Int32)));
            }
        }

        [Fact]
        public void BadReturnModReqs()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
    .field public method int32 modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)& *() 'Field1'
    .field public method int32& modreq([mscorlib]System.Runtime.InteropServices.OutAttribute) *() 'Field2'
    .field public method int32& modreq([mscorlib]System.Runtime.InteropServices.OutAttribute) modreq([mscorlib]System.Runtime.InteropServices.InAttribute) *() 'Field3'
    .field public method int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modreq([mscorlib]System.Runtime.InteropServices.OutAttribute) *() 'Field4'
    .field public method int32 modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) *() 'Field5'
    .field public method int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute)& *() 'Field6'
    .field public method int32 modreq([mscorlib]System.Object)& *() 'Field7'
    .field public method int32& modreq([mscorlib]System.Object) *() 'Field8'
    .field static public method method int32 modreq([mscorlib]System.Object) *() *() 'Field9'
}
";

            var source = @"
class D
{
    void M(C c)
    {
        ref int i1 = ref c.Field1();
        ref int i2 = ref c.Field2();
        c.Field1 = c.Field1;
        c.Field2 = c.Field2;
    }
}";

            var comp = CreateCompilationWithFunctionPointersAndIl(source, il);

            comp.VerifyDiagnostics(
                // (6,26): error CS0570: 'delegate*<int>' is not supported by the language
                //         ref int i1 = ref c.Field1();
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field1()").WithArguments("delegate*<int>").WithLocation(6, 26),
                // (6,28): error CS0570: 'C.Field1' is not supported by the language
                //         ref int i1 = ref c.Field1();
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(6, 28),
                // (7,26): error CS0570: 'delegate*<int>' is not supported by the language
                //         ref int i2 = ref c.Field2();
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field2()").WithArguments("delegate*<int>").WithLocation(7, 26),
                // (7,28): error CS0570: 'C.Field2' is not supported by the language
                //         ref int i2 = ref c.Field2();
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field2").WithArguments("C.Field2").WithLocation(7, 28),
                // (8,11): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1 = c.Field1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(8, 11),
                // (8,22): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1 = c.Field1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(8, 22),
                // (9,11): error CS0570: 'C.Field2' is not supported by the language
                //         c.Field2 = c.Field2;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field2").WithArguments("C.Field2").WithLocation(9, 11),
                // (9,22): error CS0570: 'C.Field2' is not supported by the language
                //         c.Field2 = c.Field2;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field2").WithArguments("C.Field2").WithLocation(9, 22)
            );

            var c = comp.GetTypeByMetadataName("C");

            for (int i = 1; i <= 9; i++)
            {
                var field = c.GetField($"Field{i}");
                Assert.True(field.HasUseSiteError);
                Assert.True(field.HasUnsupportedMetadata);
                Assert.Equal(TypeKind.FunctionPointer, field.Type.TypeKind);
                var signature = ((FunctionPointerTypeSymbol)field.Type).Signature;
                Assert.True(signature.HasUseSiteError);
                Assert.True(signature.HasUnsupportedMetadata);
                Assert.True(field.Type.HasUseSiteError);
                Assert.True(field.Type.HasUnsupportedMetadata);
            }
        }

        [Fact]
        public void BadParamModReqs()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
    .field public method void *(int32& modreq([mscorlib]System.Runtime.InteropServices.OutAttribute) modreq([mscorlib]System.Runtime.InteropServices.InAttribute)) 'Field1'
    .field public method void *(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)) 'Field2'
    .field public method void *(int32 modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)) 'Field3'
    .field public method void *(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute)& modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)) 'Field4'
    .field public method void *(int32 modreq([mscorlib]System.Runtime.InteropServices.InAttribute)&) 'Field5'
    .field public method void *(int32 modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)&) 'Field6'
    .field public method void *(int32& modreq([mscorlib]System.Object)) 'Field7'
    .field public method void *(int32 modreq([mscorlib]System.Object)&) 'Field8'
    .field public method void *(method void *(int32 modreq([mscorlib]System.Object))) 'Field9'
}
";

            var source = @"
class D
{
    void M(C c)
    {
        int i = 1;
        c.Field1(ref i);
        c.Field1(in i);
        c.Field1(out i);
        c.Field1 = c.Field1;
    }
}";

            var comp = CreateCompilationWithFunctionPointersAndIl(source, il);
            comp.VerifyDiagnostics(
                // (7,9): error CS0570: 'delegate*<in int, void>' is not supported by the language
                //         c.Field1(ref i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field1(ref i)").WithArguments("delegate*<in int, void>").WithLocation(7, 9),
                // (7,11): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1(ref i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(7, 11),
                // (8,9): error CS0570: 'delegate*<in int, void>' is not supported by the language
                //         c.Field1(in i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field1(in i)").WithArguments("delegate*<in int, void>").WithLocation(8, 9),
                // (8,11): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1(in i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(8, 11),
                // (9,9): error CS0570: 'delegate*<in int, void>' is not supported by the language
                //         c.Field1(out i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field1(out i)").WithArguments("delegate*<in int, void>").WithLocation(9, 9),
                // (9,11): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1(out i);
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(9, 11),
                // (10,11): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1 = c.Field1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(10, 11),
                // (10,22): error CS0570: 'C.Field1' is not supported by the language
                //         c.Field1 = c.Field1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(10, 22)
            );

            var c = comp.GetTypeByMetadataName("C");

            for (int i = 1; i <= 9; i++)
            {
                var field = c.GetField($"Field{i}");
                Assert.True(field.HasUseSiteError);
                Assert.True(field.HasUnsupportedMetadata);
                Assert.Equal(TypeKind.FunctionPointer, field.Type.TypeKind);
                var signature = ((FunctionPointerTypeSymbol)field.Type).Signature;
                Assert.True(signature.HasUseSiteError);
                Assert.True(signature.HasUnsupportedMetadata);
                Assert.True(field.Type.HasUseSiteError);
                Assert.True(field.Type.HasUnsupportedMetadata);
            }
        }

        [Fact]
        public void ValidModReqsAndOpts()
        {
            var il = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
    .field static public method int32& modopt([mscorlib]System.Runtime.InteropServices.OutAttribute) modreq([mscorlib]System.Runtime.InteropServices.InAttribute) *() 'Field1'
    .field static public method int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modopt([mscorlib]System.Runtime.InteropServices.OutAttribute) *() 'Field2'
    .field static public method void *(int32& modreq([mscorlib]System.Runtime.InteropServices.OutAttribute) modopt([mscorlib]System.Runtime.InteropServices.InAttribute)) 'Field3'
    .field static public method void *(int32& modopt([mscorlib]System.Runtime.InteropServices.OutAttribute) modreq([mscorlib]System.Runtime.InteropServices.InAttribute)) 'Field4'
    .field static public method void *(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) modreq([mscorlib]System.Runtime.InteropServices.OutAttribute)) 'Field5'
    .field static public method void *(int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modopt([mscorlib]System.Runtime.InteropServices.OutAttribute)) 'Field6'
}
";

            var source = @"
using System;
unsafe class D
{
    static int i = 1;
    static ref readonly int M()
    {
        return ref i;
    }
    static void MIn(in int param)
    {
        Console.Write(param);
    }
    static void MOut(out int param)
    {
        param = i;
    }

    static void Main()
    {
        TestRefReadonly();
        TestOut();
        TestIn();
    }

    static void TestRefReadonly()
    {
        C.Field1 = &M;
        ref readonly int local1 = ref C.Field1();
        Console.Write(local1);
        i = 2;
        Console.Write(local1);

        C.Field2 = &M;
        i = 3;
        ref readonly int local2 = ref C.Field2();
        Console.Write(local2);
        i = 4;
        Console.Write(local2);
    }

    static void TestOut()
    {
        C.Field3 = &MOut;
        i = 5;
        C.Field3(out int local);
        Console.Write(local);

        C.Field5 = &MOut;
        i = 6;
        C.Field5(out local);
        Console.Write(local);
    }

    static void TestIn()
    {
        i = 7;
        C.Field4 = &MIn;
        C.Field4(in i);

        i = 8;
        C.Field6 = &MIn;
        C.Field6(in i);
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, il, expectedOutput: "12345678");

            verifier.VerifyIL("D.TestRefReadonly", @"
{
  // Code size       87 (0x57)
  .maxstack  2
  IL_0000:  ldftn      ""ref readonly int D.M()""
  IL_0006:  stsfld     ""delegate*<ref readonly int> C.Field1""
  IL_000b:  ldsfld     ""delegate*<ref readonly int> C.Field1""
  IL_0010:  calli      ""delegate*<ref readonly int>""
  IL_0015:  dup
  IL_0016:  ldind.i4
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ldc.i4.2
  IL_001d:  stsfld     ""int D.i""
  IL_0022:  ldind.i4
  IL_0023:  call       ""void System.Console.Write(int)""
  IL_0028:  ldftn      ""ref readonly int D.M()""
  IL_002e:  stsfld     ""delegate*<ref readonly int> C.Field2""
  IL_0033:  ldc.i4.3
  IL_0034:  stsfld     ""int D.i""
  IL_0039:  ldsfld     ""delegate*<ref readonly int> C.Field2""
  IL_003e:  calli      ""delegate*<ref readonly int>""
  IL_0043:  dup
  IL_0044:  ldind.i4
  IL_0045:  call       ""void System.Console.Write(int)""
  IL_004a:  ldc.i4.4
  IL_004b:  stsfld     ""int D.i""
  IL_0050:  ldind.i4
  IL_0051:  call       ""void System.Console.Write(int)""
  IL_0056:  ret
}
");
            verifier.VerifyIL("D.TestOut", @"
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0, //local
                delegate*<out int, void> V_1,
                delegate*<out int, void> V_2)
  IL_0000:  ldftn      ""void D.MOut(out int)""
  IL_0006:  stsfld     ""delegate*<out int, void> C.Field3""
  IL_000b:  ldc.i4.5
  IL_000c:  stsfld     ""int D.i""
  IL_0011:  ldsfld     ""delegate*<out int, void> C.Field3""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.1
  IL_001a:  calli      ""delegate*<out int, void>""
  IL_001f:  ldloc.0
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ldftn      ""void D.MOut(out int)""
  IL_002b:  stsfld     ""delegate*<out int, void> C.Field5""
  IL_0030:  ldc.i4.6
  IL_0031:  stsfld     ""int D.i""
  IL_0036:  ldsfld     ""delegate*<out int, void> C.Field5""
  IL_003b:  stloc.2
  IL_003c:  ldloca.s   V_0
  IL_003e:  ldloc.2
  IL_003f:  calli      ""delegate*<out int, void>""
  IL_0044:  ldloc.0
  IL_0045:  call       ""void System.Console.Write(int)""
  IL_004a:  ret
}
");
            verifier.VerifyIL("D.TestIn", @"
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (delegate*<in int, void> V_0,
                delegate*<in int, void> V_1)
  IL_0000:  ldc.i4.7
  IL_0001:  stsfld     ""int D.i""
  IL_0006:  ldftn      ""void D.MIn(in int)""
  IL_000c:  stsfld     ""delegate*<in int, void> C.Field4""
  IL_0011:  ldsfld     ""delegate*<in int, void> C.Field4""
  IL_0016:  stloc.0
  IL_0017:  ldsflda    ""int D.i""
  IL_001c:  ldloc.0
  IL_001d:  calli      ""delegate*<in int, void>""
  IL_0022:  ldc.i4.8
  IL_0023:  stsfld     ""int D.i""
  IL_0028:  ldftn      ""void D.MIn(in int)""
  IL_002e:  stsfld     ""delegate*<in int, void> C.Field6""
  IL_0033:  ldsfld     ""delegate*<in int, void> C.Field6""
  IL_0038:  stloc.1
  IL_0039:  ldsflda    ""int D.i""
  IL_003e:  ldloc.1
  IL_003f:  calli      ""delegate*<in int, void>""
  IL_0044:  ret
}
");

            var c = ((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("C");
            var field = c.GetField("Field1");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.RefReadOnly, IsSpecialType(SpecialType.System_Int32)));

            field = c.GetField("Field2");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.RefReadOnly, IsSpecialType(SpecialType.System_Int32)));

            field = c.GetField("Field3");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.None, IsVoidType()),
                (RefKind.Out, IsSpecialType(SpecialType.System_Int32)));

            field = c.GetField("Field4");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.None, IsVoidType()),
                (RefKind.In, IsSpecialType(SpecialType.System_Int32)));

            field = c.GetField("Field5");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.None, IsVoidType()),
                (RefKind.Out, IsSpecialType(SpecialType.System_Int32)));

            field = c.GetField("Field6");
            VerifyFunctionPointerSymbol(field.Type, CallingConvention.Default,
                (RefKind.None, IsVoidType()),
                (RefKind.In, IsSpecialType(SpecialType.System_Int32)));
        }

        [Fact]
        public void RefReadonlyIsDoneByRef()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    private static int i = 0;
    static ref readonly int GetI() => ref i;
    static void Main()
    {
        delegate*<ref readonly int> d = &GetI;
        ref readonly int local = ref d();
        Console.Write(local);
        i = 1;
        Console.Write(local);
    }
}
", expectedOutput: "01");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldftn      ""ref readonly int C.GetI()""
  IL_0006:  calli      ""delegate*<ref readonly int>""
  IL_000b:  dup
  IL_000c:  ldind.i4
  IL_000d:  call       ""void System.Console.Write(int)""
  IL_0012:  ldc.i4.1
  IL_0013:  stsfld     ""int C.i""
  IL_0018:  ldind.i4
  IL_0019:  call       ""void System.Console.Write(int)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void NestedPointerTypes()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe delegate* cdecl<ref delegate*<ref readonly string>, void> M(delegate*<in delegate* stdcall<delegate*<void>>, delegate*<int>> param) => throw null;
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var returnType = m.ReturnType;
                var paramType = m.Parameters[0].Type;

                VerifyFunctionPointerSymbol(returnType, CallingConvention.CDecl,
                    (RefKind.None, IsVoidType()),
                    (RefKind.Ref,
                     IsFunctionPointerTypeSymbol(CallingConvention.Default,
                        (RefKind.RefReadOnly, IsSpecialType(SpecialType.System_String)))));

                VerifyFunctionPointerSymbol(paramType, CallingConvention.Default,
                    (RefKind.None,
                     IsFunctionPointerTypeSymbol(CallingConvention.Default,
                        (RefKind.None, IsSpecialType(SpecialType.System_Int32)))),
                    (RefKind.In,
                     IsFunctionPointerTypeSymbol(CallingConvention.Standard,
                        (RefKind.None,
                         IsFunctionPointerTypeSymbol(CallingConvention.Default,
                            (RefKind.None, IsVoidType()))))));
            }
        }

        [Fact]
        public void RandomModOptsFromIl()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test1
       extends[mscorlib] System.Object
{
    .method public hidebysig instance void  M(method bool modopt([mscorlib]System.Runtime.InteropServices.OutAttribute)& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modopt([mscorlib]System.Runtime.InteropServices.ComImport) *(int32 modopt([mscorlib]System.Runtime.InteropServices.AllowReversePInvokeCallsAttribute)& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modopt([mscorlib]System.Runtime.InteropServices.PreserveSigAttribute)) param) cil managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method Test::M
}
";

            var compilation = CreateCompilationWithIL(source: "", ilSource, parseOptions: TestOptions.RegularPreview);
            var testClass = compilation.GetTypeByMetadataName("Test1")!;

            var m = testClass.GetMethod("M");
            Assert.NotNull(m);
            var param = (FunctionPointerTypeSymbol)m.Parameters[0].Type;
            VerifyFunctionPointerSymbol(param, CallingConvention.Default,
                (RefKind.RefReadOnly, IsSpecialType(SpecialType.System_Boolean)),
                (RefKind.In, IsSpecialType(SpecialType.System_Int32)));

            var returnModifiers = param.Signature.ReturnTypeWithAnnotations.CustomModifiers;
            verifyMod(1, "OutAttribute", returnModifiers);

            var returnRefModifiers = param.Signature.RefCustomModifiers;
            verifyMod(2, "ComImport", returnRefModifiers);

            var paramModifiers = param.Signature.ParameterTypesWithAnnotations[0].CustomModifiers;
            verifyMod(1, "AllowReversePInvokeCallsAttribute", paramModifiers);

            var paramRefModifiers = param.Signature.Parameters[0].RefCustomModifiers;
            verifyMod(2, "PreserveSigAttribute", paramRefModifiers);

            static void verifyMod(int length, string expectedTypeName, ImmutableArray<CustomModifier> customMods)
            {
                Assert.Equal(length, customMods.Length);
                var firstMod = customMods[0];
                Assert.True(firstMod.IsOptional);
                Assert.Equal(expectedTypeName, ((CSharpCustomModifier)firstMod).ModifierSymbol.Name);

                if (length > 1)
                {
                    Assert.Equal(2, customMods.Length);
                    var inMod = customMods[1];
                    Assert.False(inMod.IsOptional);
                    Assert.True(((CSharpCustomModifier)inMod).ModifierSymbol.IsWellKnownTypeInAttribute());
                }
            }
        }

        [Fact]
        public void MultipleFunctionPointerArguments()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
	public void M(delegate*<ref int, ref bool> param1,
                  delegate*<ref int, ref bool> param2,
                  delegate*<ref int, ref bool> param3,
                  delegate*<ref int, ref bool> param4,
                  delegate*<ref int, ref bool> param5) {}
                     
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");

                foreach (var param in m.Parameters)
                {
                    VerifyFunctionPointerSymbol(param.Type, CallingConvention.Default,
                        (RefKind.Ref, IsSpecialType(SpecialType.System_Boolean)),
                        (RefKind.Ref, IsSpecialType(SpecialType.System_Int32)));
                }
            }
        }

        [Fact]
        public void FunctionPointersInProperties()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
    public delegate*<string, void> Prop1 { get; set; }
    public delegate* stdcall<int> Prop2 { get => throw null; set => throw null; }
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            verifier.VerifyIL("C.Prop1.get", expectedIL: @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""delegate*<string, void> C.<Prop1>k__BackingField""
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Prop1.set", expectedIL: @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""delegate*<string, void> C.<Prop1>k__BackingField""
  IL_0007:  ret
}
");

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                validateProperty((PropertySymbol)c.GetProperty((string)"Prop1"), IsFunctionPointerTypeSymbol(CallingConvention.Default,
                    (RefKind.None, IsVoidType()),
                    (RefKind.None, IsSpecialType(SpecialType.System_String))));

                validateProperty(c.GetProperty("Prop2"), IsFunctionPointerTypeSymbol(CallingConvention.Standard,
                    (RefKind.None, IsSpecialType(SpecialType.System_Int32))));

                static void validateProperty(PropertySymbol property, Action<TypeSymbol> verifier)
                {
                    verifier(property.Type);
                    verifier(property.GetMethod.ReturnType);
                    verifier(property.SetMethod.GetParameterType(0));
                }
            }
        }

        [Fact]
        public void FunctionPointersInFields()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
    public readonly delegate*<C, C> _field;
}", symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                VerifyFunctionPointerSymbol(c.GetField("_field").Type, CallingConvention.Default,
                    (RefKind.None, IsTypeName("C")),
                    (RefKind.None, IsTypeName("C")));
            }
        }

        [Fact]
        public void CustomModifierOnReturnType()
        {

            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends[mscorlib] System.Object
{
    .method public hidebysig newslot virtual instance method bool modopt([mscorlib]System.Object)& *(int32&)  M() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method C::M

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method C::.ctor
}
";

            var source = @"
class D : C
{
    public unsafe override delegate*<ref int, ref bool> M() => throw null;
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub: ilSource, symbolValidator: symbolValidator);

            symbolValidator(GetSourceModule(verifier));

            static void symbolValidator(ModuleSymbol module)
            {
                var d = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
                var m = d.GetMethod("M");

                var returnTypeWithAnnotations = ((FunctionPointerTypeSymbol)m.ReturnType).Signature.ReturnTypeWithAnnotations;
                Assert.Equal(1, returnTypeWithAnnotations.CustomModifiers.Length);
                Assert.Equal(SpecialType.System_Object, returnTypeWithAnnotations.CustomModifiers[0].Modifier.SpecialType);
            }
        }

        [Fact]
        public void UnsupportedCallingConventionInMetadata()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
    .field public method vararg void*() 'Field'
    .field private method unmanaged fastcall void *() '<Prop>k__BackingField'
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
    
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method C::.ctor
    
    .method public hidebysig specialname instance method unmanaged fastcall void *() 
            get_Prop() cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      method unmanaged fastcall void *() C::'<Prop>k__BackingField'
      IL_0006:  ret
    } // end of method C::get_Prop
    
    .method public hidebysig specialname instance void 
            set_Prop(method unmanaged fastcall void *() 'value') cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      method unmanaged fastcall void *() C::'<Prop>k__BackingField'
      IL_0007:  ret
    } // end of method C::set_Prop
    
    .property instance method unmanaged fastcall void *()
            Prop()
    {
      .get instance method unmanaged fastcall void *() C::get_Prop()
      .set instance void C::set_Prop(method unmanaged fastcall void *())
    } // end of property C::Prop
} // end of class C
";

            var source = @"
unsafe class D
{
    void M(C c)
    {
        c.Field(__arglist(1, 2));
        c.Field(1, 2, 3);
        c.Field();
        c.Field = c.Field;
        c.Prop();
        c.Prop = c.Prop;
    }
}
";

            var comp = CreateCompilationWithFunctionPointersAndIl(source, ilSource);
            comp.VerifyDiagnostics(
                // (6,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Field(__arglist(1, 2));
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate*<void>").WithLocation(6, 11),
                // (7,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Field(1, 2, 3);
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate*<void>").WithLocation(7, 11),
                // (8,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Field();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate*<void>").WithLocation(8, 11),
                // (9,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Field = c.Field;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate*<void>").WithLocation(9, 11),
                // (9,21): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Field = c.Field;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate*<void>").WithLocation(9, 21),
                // (10,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Prop();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate*<void>").WithLocation(10, 11),
                // (11,11): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Prop = c.Prop;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate*<void>").WithLocation(11, 11),
                // (11,20): error CS8806: The calling convention of 'delegate*<void>' is not supported by the language.
                //         c.Prop = c.Prop;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate*<void>").WithLocation(11, 20)
            );

            var c = comp.GetTypeByMetadataName("C");
            var prop = c.GetProperty("Prop");

            VerifyFunctionPointerSymbol(prop.Type, CallingConvention.FastCall,
                (RefKind.None, IsVoidType()));

            Assert.True(prop.Type.HasUseSiteError);

            var field = c.GetField("Field");

            var type = (FunctionPointerTypeSymbol)field.Type;
            VerifyFunctionPointerSymbol(type, CallingConvention.ExtraArguments,
                (RefKind.None, IsVoidType()));

            Assert.True(type.HasUseSiteError);
            Assert.True(type.Signature.IsVararg);
        }

        [Fact]
        public void StructWithFunctionPointerThatReferencesStruct()
        {
            CompileAndVerifyFunctionPointers(@"
unsafe struct S
{
    public delegate*<S, S> Field;
    public delegate*<S, S> Property { get; set; }
}");
        }

        [Fact]
        public void CalliOnParameter()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method void *() LoadPtr () cil managed 
    {
        nop
        ldftn void Program::Called()
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        void Called () cil managed 
    {
        nop
        ldstr ""Called""
        call void [mscorlib]System.Console::WriteLine(string)
        nop
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
class Caller
{
    public unsafe static void Main()
    {
        Call(Program.LoadPtr());
    }

    public unsafe static void Call(delegate*<void> ptr)
    {
        ptr();
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: "Called");
            verifier.VerifyIL("Caller.Call(delegate*<void>)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  calli      ""delegate*<void>""
  IL_0006:  ret
}");
        }

        [Fact]
        public void CalliOnFieldNoArgs()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method void *() LoadPtr () cil managed 
    {
        nop
        ldftn void Program::Called()
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        void Called () cil managed 
    {
        nop
        ldstr ""Called""
        call void [mscorlib]System.Console::WriteLine(string)
        nop
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
unsafe class Caller
{
    static delegate*<void> _field;

    public unsafe static void Main()
    {
        _field = Program.LoadPtr();
        Call();
    }

    public unsafe static void Call()
    {
        _field();
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: "Called");
            verifier.VerifyIL("Caller.Call()", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsfld     ""delegate*<void> Caller._field""
  IL_0005:  calli      ""delegate*<void>""
  IL_000a:  ret
}");
        }

        [Fact]
        public void CalliOnFieldArgs()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method void *(string) LoadPtr () cil managed 
    {
        nop
        ldftn void Program::Called(string)
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        void Called (string arg) cil managed 
    {
        nop
        ldarg.0
        call void [mscorlib]System.Console::WriteLine(string)
        nop
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
unsafe class Caller
{
    static delegate*<string, void> _field;

    public unsafe static void Main()
    {
        _field = Program.LoadPtr();
        Call();
    }

    public unsafe static void Call()
    {
        _field(""Called"");
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: "Called");
            verifier.VerifyIL("Caller.Call()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (delegate*<string, void> V_0)
  IL_0000:  ldsfld     ""delegate*<string, void> Caller._field""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Called""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string, void>""
  IL_0011:  ret
}");
        }

        [Theory]
        [InlineData("cdecl")]
        [InlineData("stdcall")]
        public void UnmanagedCallingConventions(string convention)
        {
            // Use IntPtr Marshal.GetFunctionPointerForDelegate<TDelegate>(TDelegate delegate) to
            // get a function pointer around a native calling convention
            var ilStub = $@"
.class public auto ansi beforefieldinit UnmanagedFunctionPointer
    extends [mscorlib]System.Object
{{
    // Nested Types
    .class nested private auto ansi sealed CombineStrings
        extends [mscorlib]System.MulticastDelegate
    {{
        .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = (
            01 00 {(convention == "cdecl" ? "02" : "03")} 00 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                object 'object',
                native int 'method'
            ) runtime managed 
        {{
        }} // end of method CombineStrings::.ctor

        .method public hidebysig newslot virtual 
            instance string Invoke (
                string s1,
                string s2
            ) runtime managed 
        {{
        }} // end of method CombineStrings::Invoke

        .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult BeginInvoke (
                string s1,
                string s2,
                class [mscorlib]System.AsyncCallback callback,
                object 'object'
            ) runtime managed 
        {{
        }} // end of method CombineStrings::BeginInvoke

        .method public hidebysig newslot virtual 
            instance string EndInvoke (
                class [mscorlib]System.IAsyncResult result
            ) runtime managed 
        {{
        }} // end of method CombineStrings::EndInvoke
    }} // end of class CombineStrings

    // Methods
    .method private hidebysig static 
        string CombineStringsImpl (
            string s1,
            string s2
        ) cil managed 
    {{
        // Method begins at RVA 0x2050
        // Code size 13 (0xd)
        .maxstack 2
        .locals init (
            [0] string
        )

        ldarg.0
        ldarg.1
        call string [mscorlib]System.String::Concat(string, string)
        ret
    }} // end of method UnmanagedFunctionPointer::CombineStringsImpl

    .method public hidebysig static 
        method unmanaged {convention} string *(string, string) GetFuncPtr () cil managed 
    {{
        // Method begins at RVA 0x206c
        // Code size 23 (0x17)
        .maxstack 2
        .locals init (
            [0] native int
        )

        nop
        ldnull
        ldftn string UnmanagedFunctionPointer::CombineStringsImpl(string, string)
        newobj instance void UnmanagedFunctionPointer/CombineStrings::.ctor(object, native int)
        call native int [mscorlib]System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate<class UnmanagedFunctionPointer/CombineStrings>(!!0)
        stloc.0
        ldloc.0
        box [mscorlib]System.IntPtr
		call void [mscorlib]System.GC::KeepAlive(object)
        ldloc.0
        ret
    }} // end of method UnmanagedFunctionPointer::GetFuncPtr
}} // end of class UnmanagedFunctionPointer";

            var source = $@"
using System;
class Caller
{{
    public unsafe static void Main()
    {{
        Call(UnmanagedFunctionPointer.GetFuncPtr());
    }}

    public unsafe static void Call(delegate* {convention}<string, string, string> ptr)
    {{
        Console.WriteLine(ptr(""Hello"", "" World""));
    }}
}}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: "Hello World");
            // https://github.com/dotnet/roslyn/issues/39865: Add calling convention when the formatter supports it
            verifier.VerifyIL($"Caller.Call(delegate*<string, string, string>)", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (delegate*<string, string, string> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""Hello""
  IL_0007:  ldstr      "" World""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate*<string, string, string>""
  IL_0012:  call       ""void System.Console.WriteLine(string)""
  IL_0017:  ret
}");
        }

        [Fact]
        public void ThiscallSimpleReturn()
        {
            var ilSource = @"
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    // Fields
    .field public int32 i

    // Methods
    .method public hidebysig static 
        int32 GetInt (
            valuetype S* s
        ) cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 12 (0xc)
        .maxstack 1
        .locals init (
            [0] int32
        )

        nop
        ldarg.0
        ldfld int32 S::i
        ret
    } // end of method S::GetInt

    .method public hidebysig static 
        int32 GetReturn (
            valuetype S* s,
            int32 i
        ) cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 14 (0xe)
        .maxstack 2
        .locals init (
            [0] int32
        )

        nop
        ldarg.0
        ldfld int32 S::i
        ldarg.1
        add
        ret
    } // end of method S::GetReturn

} // end of class S

.class public auto ansi beforefieldinit UnmanagedFunctionPointer
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed SingleParam
        extends [mscorlib]System.MulticastDelegate
    {
        .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = (
            01 00 04 00 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                object 'object',
                native int 'method'
            ) runtime managed 
        {
        } // end of method SingleParam::.ctor

        .method public hidebysig newslot virtual 
            instance int32 Invoke (
                valuetype S* s
            ) runtime managed 
        {
        } // end of method SingleParam::Invoke

        .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult BeginInvoke (
                valuetype S* s,
                class [mscorlib]System.AsyncCallback callback,
                object 'object'
            ) runtime managed 
        {
        } // end of method SingleParam::BeginInvoke

        .method public hidebysig newslot virtual 
            instance int32 EndInvoke (
                class [mscorlib]System.IAsyncResult result
            ) runtime managed 
        {
        } // end of method SingleParam::EndInvoke

    } // end of class SingleParam

    .class nested private auto ansi sealed MultipleParams
        extends [mscorlib]System.MulticastDelegate
    {
        .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = (
            01 00 04 00 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                object 'object',
                native int 'method'
            ) runtime managed 
        {
        } // end of method MultipleParams::.ctor

        .method public hidebysig newslot virtual 
            instance int32 Invoke (
                valuetype S* s,
                int32 i
            ) runtime managed 
        {
        } // end of method MultipleParams::Invoke

        .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult BeginInvoke (
                valuetype S* s,
                int32 i,
                class [mscorlib]System.AsyncCallback callback,
                object 'object'
            ) runtime managed 
        {
        } // end of method MultipleParams::BeginInvoke

        .method public hidebysig newslot virtual 
            instance int32 EndInvoke (
                class [mscorlib]System.IAsyncResult result
            ) runtime managed 
        {
        } // end of method MultipleParams::EndInvoke

    } // end of class MultipleParams


    // Methods
    .method public hidebysig static 
        method unmanaged thiscall int32 *(valuetype S*) GetFuncPtrSingleParam () cil managed 
    {
        // Method begins at RVA 0x2084
        // Code size 37 (0x25)
        .maxstack 2
        .locals init (
            [0] native int,
            [1] native int
        )

        nop
        ldnull
        ldftn int32 S::GetInt(valuetype S*)
        newobj instance void UnmanagedFunctionPointer/SingleParam::.ctor(object, native int)
        call native int [mscorlib]System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate<class UnmanagedFunctionPointer/SingleParam>(!!0)
        stloc.0
        ldloc.0
        box [mscorlib]System.IntPtr
        call void [mscorlib]System.GC::KeepAlive(object)
        ldloc.0
        ret
    } // end of method UnmanagedFunctionPointer::GetFuncPtrSingleParam

    .method public hidebysig static 
        method unmanaged thiscall int32 *(valuetype S*, int32) GetFuncPtrMultipleParams () cil managed 
    {
        // Method begins at RVA 0x20b8
        // Code size 37 (0x25)
        .maxstack 2
        .locals init (
            [0] native int,
            [1] native int
        )

        nop
        ldnull
        ldftn int32 S::GetReturn(valuetype S*, int32)
        newobj instance void UnmanagedFunctionPointer/MultipleParams::.ctor(object, native int)
        call native int [mscorlib]System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate<class UnmanagedFunctionPointer/MultipleParams>(!!0)
        stloc.0
        ldloc.0
        box [mscorlib]System.IntPtr
        call void [mscorlib]System.GC::KeepAlive(object)
        ldloc.0
        ret
    } // end of method UnmanagedFunctionPointer::GetFuncPtrMultipleParams
} // end of class UnmanagedFunctionPointer
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(@"
using System;
unsafe class C
{
    public static void Main()
    {
        TestSingle();
        TestMultiple();
    }

    public static void TestSingle()
    {
        S s = new S();
        s.i = 1;
        var i = UnmanagedFunctionPointer.GetFuncPtrSingleParam()(&s);
        Console.Write(i);
    }

    public static void TestMultiple()
    {
        S s = new S();
        s.i = 2;
        var i = UnmanagedFunctionPointer.GetFuncPtrMultipleParams()(&s, 3);
        Console.Write(i);
    }
}", ilSource, expectedOutput: @"15");

            verifier.VerifyIL("C.TestSingle()", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (S V_0, //s
                delegate*<S*, int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*, int> UnmanagedFunctionPointer.GetFuncPtrSingleParam()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldloc.1
  IL_001a:  calli      ""delegate*<S*, int>""
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret
}
");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (S V_0, //s
                delegate*<S*, int, int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*, int, int> UnmanagedFunctionPointer.GetFuncPtrMultipleParams()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldc.i4.3
  IL_001a:  ldloc.1
  IL_001b:  calli      ""delegate*<S*, int, int>""
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ret
}
");
        }

        // Fails on .net core due to https://github.com/dotnet/runtime/issues/33129
        [ConditionalFact(typeof(DesktopOnly))]
        public void ThiscallBlittable()
        {
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit IntWrapper
    extends [mscorlib]System.ValueType
{
    // Fields
    .field public int32 i

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 i
        ) cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 9 (0x9)
        .maxstack 8

        nop
        ldarg.0
        ldarg.1
        stfld int32 IntWrapper::i
        ret
    } // end of method IntWrapper::.ctor

} // end of class IntWrapper

.class public sequential ansi sealed beforefieldinit ReturnWrapper
    extends [mscorlib]System.ValueType
{
    // Fields
    .field public int32 i1
    .field public float32 f2

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 i1,
            float32 f2
        ) cil managed 
    {
        // Method begins at RVA 0x205a
        // Code size 16 (0x10)
        .maxstack 8

        nop
        ldarg.0
        ldarg.1
        stfld int32 ReturnWrapper::i1
        ldarg.0
        ldarg.2
        stfld float32 ReturnWrapper::f2
        ret
    } // end of method ReturnWrapper::.ctor

} // end of class ReturnWrapper

.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    // Fields
    .field public int32 i

    // Methods
    .method public hidebysig static 
        valuetype IntWrapper GetInt (
            valuetype S* s
        ) cil managed 
    {
        // Method begins at RVA 0x206c
        // Code size 17 (0x11)
        .maxstack 1
        .locals init (
            [0] valuetype IntWrapper
        )

        nop
        ldarg.0
        ldfld int32 S::i
        newobj instance void IntWrapper::.ctor(int32)
        ret
    } // end of method S::GetInt

    .method public hidebysig static 
        valuetype ReturnWrapper GetReturn (
            valuetype S* s,
            float32 f
        ) cil managed 
    {
        // Method begins at RVA 0x208c
        // Code size 18 (0x12)
        .maxstack 2
        .locals init (
            [0] valuetype ReturnWrapper
        )

        nop
        ldarg.0
        ldfld int32 S::i
        ldarg.1
        newobj instance void ReturnWrapper::.ctor(int32, float32)
        ret
    } // end of method S::GetReturn

} // end of class S

.class public auto ansi beforefieldinit UnmanagedFunctionPointer
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed SingleParam
        extends [mscorlib]System.MulticastDelegate
    {
        .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = (
            01 00 04 00 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                object 'object',
                native int 'method'
            ) runtime managed 
        {
        } // end of method SingleParam::.ctor

        .method public hidebysig newslot virtual 
            instance valuetype IntWrapper Invoke (
                valuetype S* s
            ) runtime managed 
        {
        } // end of method SingleParam::Invoke

        .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult BeginInvoke (
                valuetype S* s,
                class [mscorlib]System.AsyncCallback callback,
                object 'object'
            ) runtime managed 
        {
        } // end of method SingleParam::BeginInvoke

        .method public hidebysig newslot virtual 
            instance valuetype IntWrapper EndInvoke (
                class [mscorlib]System.IAsyncResult result
            ) runtime managed 
        {
        } // end of method SingleParam::EndInvoke

    } // end of class SingleParam

    .class nested private auto ansi sealed MultipleParams
        extends [mscorlib]System.MulticastDelegate
    {
        .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = (
            01 00 04 00 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor (
                object 'object',
                native int 'method'
            ) runtime managed 
        {
        } // end of method MultipleParams::.ctor

        .method public hidebysig newslot virtual 
            instance valuetype ReturnWrapper Invoke (
                valuetype S* s,
                float32 f
            ) runtime managed 
        {
        } // end of method MultipleParams::Invoke

        .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult BeginInvoke (
                valuetype S* s,
                float32 f,
                class [mscorlib]System.AsyncCallback callback,
                object 'object'
            ) runtime managed 
        {
        } // end of method MultipleParams::BeginInvoke

        .method public hidebysig newslot virtual 
            instance valuetype ReturnWrapper EndInvoke (
                class [mscorlib]System.IAsyncResult result
            ) runtime managed 
        {
        } // end of method MultipleParams::EndInvoke

    } // end of class MultipleParams


    // Methods
    .method public hidebysig static 
        method unmanaged thiscall valuetype IntWrapper *(valuetype S*) GetFuncPtrSingleParam () cil managed 
    {
        // Method begins at RVA 0x20ac
        // Code size 37 (0x25)
        .maxstack 2
        .locals init (
            [0] native int,
            [1] native int
        )

        nop
        ldnull
        ldftn valuetype IntWrapper S::GetInt(valuetype S*)
        newobj instance void UnmanagedFunctionPointer/SingleParam::.ctor(object, native int)
        call native int [mscorlib]System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate<class UnmanagedFunctionPointer/SingleParam>(!!0)
        stloc.0
        ldloc.0
        box [mscorlib]System.IntPtr
        call void [mscorlib]System.GC::KeepAlive(object)
        ldloc.0
        ret
    } // end of method UnmanagedFunctionPointer::GetFuncPtrSingleParam

    .method public hidebysig static 
        method unmanaged thiscall valuetype ReturnWrapper *(valuetype S*, float32) GetFuncPtrMultipleParams () cil managed 
    {
        // Method begins at RVA 0x20e0
        // Code size 37 (0x25)
        .maxstack 2
        .locals init (
            [0] native int,
            [1] native int
        )

        nop
        ldnull
        ldftn valuetype ReturnWrapper S::GetReturn(valuetype S*, float32)
        newobj instance void UnmanagedFunctionPointer/MultipleParams::.ctor(object, native int)
        call native int [mscorlib]System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate<class UnmanagedFunctionPointer/MultipleParams>(!!0)
        stloc.0
        ldloc.0
        box [mscorlib]System.IntPtr
        call void [mscorlib]System.GC::KeepAlive(object)
        ldloc.0
        ret
    } // end of method UnmanagedFunctionPointer::GetFuncPtrMultipleParams
} // end of class UnmanagedFunctionPointer
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(@"
using System;
unsafe class C
{
    public static void Main()
    {
        TestSingle();
        TestMultiple();
    }

    public static void TestSingle()
    {
        S s = new S();
        s.i = 1;
        var intWrapper = UnmanagedFunctionPointer.GetFuncPtrSingleParam()(&s);
        Console.WriteLine(intWrapper.i);
    }

    public static void TestMultiple()
    {
        S s = new S();
        s.i = 2;
        var returnWrapper = UnmanagedFunctionPointer.GetFuncPtrMultipleParams()(&s, 3.5f);
        Console.Write(returnWrapper.i1);
        Console.Write(returnWrapper.f2);
    }
}", ilSource, expectedOutput: @"
1
23.5
");

            verifier.VerifyIL("C.TestSingle()", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (S V_0, //s
                delegate*<S*, IntWrapper> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*, IntWrapper> UnmanagedFunctionPointer.GetFuncPtrSingleParam()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldloc.1
  IL_001a:  calli      ""delegate*<S*, IntWrapper>""
  IL_001f:  ldfld      ""int IntWrapper.i""
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
}");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (S V_0, //s
                delegate*<S*, float, ReturnWrapper> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*, float, ReturnWrapper> UnmanagedFunctionPointer.GetFuncPtrMultipleParams()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldc.r4     3.5
  IL_001e:  ldloc.1
  IL_001f:  calli      ""delegate*<S*, float, ReturnWrapper>""
  IL_0024:  dup
  IL_0025:  ldfld      ""int ReturnWrapper.i1""
  IL_002a:  call       ""void System.Console.Write(int)""
  IL_002f:  ldfld      ""float ReturnWrapper.f2""
  IL_0034:  call       ""void System.Console.Write(float)""
  IL_0039:  ret
}");
        }

        [Fact]
        public void InvocationOrder()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method void *(string, string) LoadPtr () cil managed 
    {
        nop
        ldftn void Program::Called(string, string)
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        void Called (
            string arg1,
            string arg2) cil managed 
    {
        nop
        ldarg.0
        ldarg.1
        call string [mscorlib]System.String::Concat(string, string)
        call void [mscorlib]System.Console::WriteLine(string)
        nop
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
        ldarg.0
        call instance void[mscorlib]
        System.Object::.ctor()
        nop
        ret
    } // end of Program::.ctor
}";

            var source = @"
using System;
unsafe class C
{
    static delegate*<string, string, void> Prop
    {
        get
        {
            Console.WriteLine(""Getter"");
            return Program.LoadPtr();
        }
    }

    static delegate*<string, string, void> Method()
    {
        Console.WriteLine(""Method"");
        return Program.LoadPtr();
    }

    static string GetArg(string val)
    {
        Console.WriteLine($""Getting {val}"");
        return val;
    }

    static void PropertyOrder()
    {
        Prop(GetArg(""1""), GetArg(""2""));
    }

    static void MethodOrder()
    {
        Method()(GetArg(""3""), GetArg(""4""));
    }

    static void Main()
    {
        Console.WriteLine(""Property Access"");
        PropertyOrder();
        Console.WriteLine(""Method Access"");
        MethodOrder();
    }
}
";
            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
Property Access
Getter
Getting 1
Getting 2
12
Method Access
Method
Getting 3
Getting 4
34");

            verifier.VerifyIL("C.PropertyOrder", expectedIL: @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (delegate*<string, string, void> V_0)
  IL_0000:  call       ""delegate*<string, string, void> C.Prop.get""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""1""
  IL_000b:  call       ""string C.GetArg(string)""
  IL_0010:  ldstr      ""2""
  IL_0015:  call       ""string C.GetArg(string)""
  IL_001a:  ldloc.0
  IL_001b:  calli      ""delegate*<string, string, void>""
  IL_0020:  ret
}");

            verifier.VerifyIL("C.MethodOrder()", expectedIL: @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (delegate*<string, string, void> V_0)
  IL_0000:  call       ""delegate*<string, string, void> C.Method()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""3""
  IL_000b:  call       ""string C.GetArg(string)""
  IL_0010:  ldstr      ""4""
  IL_0015:  call       ""string C.GetArg(string)""
  IL_001a:  ldloc.0
  IL_001b:  calli      ""delegate*<string, string, void>""
  IL_0020:  ret
}");
        }

        [Fact]
        public void ReturnValueUsed()
        {

            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method string *(string) LoadPtr () cil managed 
    {
        nop
        ldftn string Program::Called(string)
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        string Called (string arg) cil managed 
    {
        nop
        ldstr ""Called""
        call void [mscorlib]System.Console::WriteLine(string)
        ldarg.0
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        var retValue = Program.LoadPtr()(""Returned"");
        Console.WriteLine(retValue);
    }
}
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
Called
Returned");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (delegate*<string, string> V_0)
  IL_0000:  call       ""delegate*<string, string> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Returned""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string, string>""
  IL_0011:  call       ""void System.Console.WriteLine(string)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void ReturnValueUnused()
        {

            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method string *(string) LoadPtr () cil managed 
    {
        nop
        ldftn string Program::Called(string)
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        string Called (string arg) cil managed 
    {
        nop
        ldstr ""Called""
        call void [mscorlib]System.Console::WriteLine(string)
        ldarg.0
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        Program.LoadPtr()(""Unused"");
        Console.WriteLine(""Constant"");
    }
}
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
Called
Constant");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (delegate*<string, string> V_0)
  IL_0000:  call       ""delegate*<string, string> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Unused""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string, string>""
  IL_0011:  pop
  IL_0012:  ldstr      ""Constant""
  IL_0017:  call       ""void System.Console.WriteLine(string)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void FunctionPointerReturningFunctionPointer()
        {

            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method method string *(string) *() LoadPtr () cil managed 
    {
        nop
        ldftn method string *(string) Program::Called1()
        ret
    } // end of method Program::LoadPtr

    .method private hidebysig static 
        method string *(string) Called1 () cil managed 
    {
        nop
        ldstr ""Outer pointer""
        call void [mscorlib]System.Console::WriteLine(string)
        ldftn string Program::Called2(string)
        ret
    } // end of Program::Called1

    .method private hidebysig static 
        string Called2 (string arg) cil managed 
    {
        nop
        ldstr ""Inner pointer""
        call void [mscorlib]System.Console::WriteLine(string)
        ldarg.0
        ret
    } // end of Program::Called2

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    public static void Main()
    {
        var outer = Program.LoadPtr();
        var inner = outer();
        Console.WriteLine(inner(""Returned""));
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
Outer pointer
Inner pointer
Returned");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (delegate*<string, string> V_0)
  IL_0000:  call       ""delegate*<delegate*<string, string>> Program.LoadPtr()""
  IL_0005:  calli      ""delegate*<delegate*<string, string>>""
  IL_000a:  stloc.0
  IL_000b:  ldstr      ""Returned""
  IL_0010:  ldloc.0
  IL_0011:  calli      ""delegate*<string, string>""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}");
        }

        [Fact]
        public void UserDefinedConversionParameter()
        {

            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    .field public string '_field'

    // Methods
    .method public hidebysig static 
        method void *(class Program) LoadPtr () cil managed 
    {
        nop
        ldstr ""LoadPtr""
        call void [mscorlib]System.Console::WriteLine(string)
        ldftn void Program::Called(class Program)
        ret
    } // end of method Program::LoadPtr

    .method private hidebysig static 
        void Called (class Program arg1) cil managed 
    {
        nop
        ldarg.0
        ldfld string Program::'_field'
        call void [mscorlib]System.Console::WriteLine(string)
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    public static void Main()
    {
        Program.LoadPtr()(new C());
    }

    public static implicit operator Program(C c)
    {
        var p = new Program();
        p._field = ""Implicit conversion"";
        return p;
    }
}";
            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
LoadPtr
Implicit conversion
");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (delegate*<Program, void> V_0)
  IL_0000:  call       ""delegate*<Program, void> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""C..ctor()""
  IL_000b:  call       ""Program C.op_Implicit(C)""
  IL_0010:  ldloc.0
  IL_0011:  calli      ""delegate*<Program, void>""
  IL_0016:  ret
}");
        }

        [Fact]
        public void RefParameter()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method void *(string&) LoadPtr () cil managed 
    {
        nop
        ldftn void Program::Called(string&)
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        void Called (string& arg) cil managed 
    {
        nop
        ldarg.0
        ldstr ""Ref set""
        stind.ref
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        delegate*<ref string, void> pointer = Program.LoadPtr();
        string str = ""Unset"";
        pointer(ref str);
        Console.WriteLine(str);
    }
}
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"Ref set");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (string V_0, //str
                delegate*<ref string, void> V_1)
  IL_0000:  call       ""delegate*<ref string, void> Program.LoadPtr()""
  IL_0005:  ldstr      ""Unset""
  IL_000a:  stloc.0
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldloc.1
  IL_000f:  calli      ""delegate*<ref string, void>""
  IL_0014:  ldloc.0
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void RefReturnUsedByValue()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    .field public static string 'field'

    // Methods
    .method public hidebysig static 
        method string& *() LoadPtr () cil managed 
    {
        nop
        ldftn string& Program::Called()
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        string& Called () cil managed 
    {
        nop
        ldsflda string Program::'field'
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        Program.field = ""Field"";
        delegate*<ref string> pointer = Program.LoadPtr();
        Console.WriteLine(pointer());
    }
}
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"Field");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldstr      ""Field""
  IL_0005:  stsfld     ""string Program.field""
  IL_000a:  call       ""delegate*<ref string> Program.LoadPtr()""
  IL_000f:  calli      ""delegate*<ref string>""
  IL_0014:  ldind.ref
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void RefReturnUsed()
        {
            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    .field public static string 'field'

    // Methods
    .method public hidebysig static 
        method string& *() LoadPtr () cil managed 
    {
        nop
        ldftn string& Program::Called()
        ret
    } // end of method Program::Main

    .method private hidebysig static 
        string& Called () cil managed 
    {
        nop
        ldsflda string Program::'field'
        ret
    } // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        Program.LoadPtr()() = ""Field"";
        Console.WriteLine(Program.field);
    }
}
";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"Field");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  call       ""delegate*<ref string> Program.LoadPtr()""
  IL_0005:  calli      ""delegate*<ref string>""
  IL_000a:  ldstr      ""Field""
  IL_000f:  stind.ref
  IL_0010:  ldsfld     ""string Program.field""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void ModifiedReceiverInParameter()
        {

            var ilStub = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        method string *(string) LoadPtr1 () cil managed 
    {
        nop
        ldftn string Program::Called1(string)
        ret
    } // end of method Program::LoadPtr1

    .method public hidebysig static 
        method string *(string) LoadPtr2 () cil managed 
    {
        nop
        ldftn string Program::Called2(string)
        ret
    } // end of method Program::LoadPtr2

    .method private hidebysig static 
        string Called1 (string) cil managed 
    {
        nop
        ldstr ""Called Function 1""
        call void [mscorlib]System.Console::WriteLine(string)
        ldarg.0
        call void [mscorlib]System.Console::WriteLine(string)
        ldstr ""Returned From Function 1""
        ret
    } // end of Program::Called1

    .method private hidebysig static 
        string Called2 (string) cil managed 
    {
        nop
        ldstr ""Called Function 2""
        call void [mscorlib]System.Console::WriteLine(string)
        ldarg.0
        call void [mscorlib]System.Console::WriteLine(string)
        ldstr ""Returned From Function 2""
        ret
    } // end of Program::Called2

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    } // end of Program::.ctor
}
";

            var source = @"
using System;
unsafe class C
{
    public static void Main()
    {
        var ptr = Program.LoadPtr1();
        Console.WriteLine(ptr((ptr = Program.LoadPtr2())(""Argument To Function 2"")));
        Console.WriteLine(ptr(""Argument To Function 2""));
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: @"
Called Function 2
Argument To Function 2
Called Function 1
Returned From Function 2
Returned From Function 1
Called Function 2
Argument To Function 2
Returned From Function 2");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (delegate*<string, string> V_0, //ptr
                delegate*<string, string> V_1,
                delegate*<string, string> V_2)
  IL_0000:  call       ""delegate*<string, string> Program.LoadPtr1()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  call       ""delegate*<string, string> Program.LoadPtr2()""
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  stloc.2
  IL_0010:  ldstr      ""Argument To Function 2""
  IL_0015:  ldloc.2
  IL_0016:  calli      ""delegate*<string, string>""
  IL_001b:  ldloc.1
  IL_001c:  calli      ""delegate*<string, string>""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ldloc.0
  IL_0027:  stloc.1
  IL_0028:  ldstr      ""Argument To Function 2""
  IL_002d:  ldloc.1
  IL_002e:  calli      ""delegate*<string, string>""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}");
        }

        [Fact]
        public void Typeof()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
class C
{
    static void Main()
    {
        var t = typeof(delegate*<void>);
        Console.WriteLine(t.ToString());
    }
}
", expectedOutput: "System.IntPtr");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldtoken    ""delegate*<void>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  callvirt   ""string object.ToString()""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ret
}");
        }

        private const string NoPiaInterfaces = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface I1
{
    string GetStr();
}

[ComImport]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58270"")]
public interface I2{}";

        [Fact]
        public void NoPiaInSignature()
        {
            var nopiaReference = CreateCompilation(NoPiaInterfaces).EmitToImageReference(embedInteropTypes: true);

            CompileAndVerifyFunctionPointers(@"
unsafe class C
{
    public delegate*<I2, I1> M() => throw null;
}", references: new[] { nopiaReference }, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                Assert.Equal(1, module.ReferencedAssemblies.Length);
                Assert.NotEqual(nopiaReference.Display, module.ReferencedAssemblies[0].Name);

                var i1 = module.GlobalNamespace.GetTypeMembers("I1").Single();
                Assert.NotNull(i1);
                Assert.Equal(module, i1.ContainingModule);

                var i2 = module.GlobalNamespace.GetTypeMembers("I2").Single();
                Assert.NotNull(i2);
                Assert.Equal(module, i2.ContainingModule);

                var c = module.GlobalNamespace.GetTypeMembers("C").Single();
                var m = c.GetMethod("M");

                var returnType = (FunctionPointerTypeSymbol)m.ReturnType;
                Assert.Equal(i1, returnType.Signature.ReturnType);
                Assert.Equal(i2, returnType.Signature.ParameterTypesWithAnnotations[0].Type);
            }
        }

        [Fact]
        public void NoPiaInTypeOf()
        {
            var nopiaReference = CreateCompilation(NoPiaInterfaces).EmitToImageReference(embedInteropTypes: true);

            CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    public Type M() => typeof(delegate*<I1, I2>);
}", references: new[] { nopiaReference }, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                Assert.Equal(1, module.ReferencedAssemblies.Length);
                Assert.NotEqual(nopiaReference.Display, module.ReferencedAssemblies[0].Name);

                var i1 = module.GlobalNamespace.GetTypeMembers("I1").Single();
                Assert.NotNull(i1);
                Assert.Equal(module, i1.ContainingModule);

                var i2 = module.GlobalNamespace.GetTypeMembers("I2").Single();
                Assert.NotNull(i2);
                Assert.Equal(module, i2.ContainingModule);
            }
        }

        [Fact]
        public void NoPiaInCall()
        {
            var nopiaReference = CreateCompilation(NoPiaInterfaces).EmitToImageReference(embedInteropTypes: true);

            var intermediate = CreateCompilation(@"
using System;
public unsafe class C
{
    public delegate*<I1> M() => throw null;
}", references: new[] { nopiaReference }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

            CompileAndVerifyFunctionPointers(@"
unsafe class C2
{
    public void M(C c)
    {
        _ = c.M()();
    }
}", references: new[] { nopiaReference, intermediate }, symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                Assert.Equal(2, module.ReferencedAssemblies.Length);
                Assert.DoesNotContain(nopiaReference.Display, module.ReferencedAssemblies.Select(a => a.Name));
                Assert.Equal(intermediate.Display, module.ReferencedAssemblies[1].Name);

                var i1 = module.GlobalNamespace.GetTypeMembers("I1").Single();
                Assert.NotNull(i1);
                Assert.Equal(module, i1.ContainingModule);
            }
        }

        [Fact]
        public void InternalsVisibleToAccessChecks_01()
        {
            var aRef = CreateCompilation(@"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""B"")]
internal class A {}", assemblyName: "A").EmitToImageReference();

            var bRef = CreateCompilation(@"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""C"")]
internal class B
{
    internal unsafe delegate*<A> M() => throw null;
}", references: new[] { aRef }, assemblyName: "B", parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

            var cComp = CreateCompilation(@"
internal class C
{
    internal unsafe void CM(B b)
    {
        b.M()();
    }
}", references: new[] { aRef, bRef }, assemblyName: "C", parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll);

            cComp.VerifyDiagnostics(
                    // (6,9): error CS0122: 'B.M()' is inaccessible due to its protection level
                    //         b.M()();
                    Diagnostic(ErrorCode.ERR_BadAccess, "b.M").WithArguments("B.M()").WithLocation(6, 9));
        }

        [Fact]
        public void InternalsVisibleToAccessChecks_02()
        {
            var aRef = CreateCompilation(@"
using System.Runtime.CompilerServices;
public class A {}", assemblyName: "A").EmitToImageReference();

            var bRef = CreateCompilation(@"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""C"")]
internal class B
{
    internal unsafe delegate*<A> M() => throw null;
}", references: new[] { aRef }, assemblyName: "B", parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

            var cComp = CreateCompilation(@"
internal class C
{
    internal unsafe void CM(B b)
    {
        b.M()();
    }
}", references: new[] { aRef, bRef }, assemblyName: "C", parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseDll);

            cComp.VerifyDiagnostics();
        }

        [Fact]
        public void AddressOf_Initializer_VoidReturnNoParams()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M() => Console.Write(""1"");
    static void Main()
    {
        delegate*<void> ptr = &M;
        ptr();
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void C.M()""
  IL_0006:  calli      ""delegate*<void>""
  IL_000b:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_VoidReturnValueParams()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(string s, int i) => Console.Write(s + i.ToString());
    static void Main()
    {
        delegate*<string, int, void> ptr = &M;
        ptr(""1"", 2);
    }
}", expectedOutput: "12");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (delegate*<string, int, void> V_0)
  IL_0000:  ldftn      ""void C.M(string, int)""
  IL_0006:  stloc.0
  IL_0007:  ldstr      ""1""
  IL_000c:  ldc.i4.2
  IL_000d:  ldloc.0
  IL_000e:  calli      ""delegate*<string, int, void>""
  IL_0013:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_VoidReturnRefParameters()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(ref string s, in int i, out object o)
    {
        Console.Write(s + i.ToString());
        s = ""3"";
        o = ""4"";
    }
    static void Main()
    {
        delegate*<ref string, in int, out object, void> ptr = &M;
        string s = ""1"";
        int i = 2;
        ptr(ref s, in i, out var o);
        Console.Write(s);
        Console.Write(o);
    }
}", expectedOutput: "1234");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (string V_0, //s
                int V_1, //i
                object V_2, //o
                delegate*<ref string, in int, out object, void> V_3)
  IL_0000:  ldftn      ""void C.M(ref string, in int, out object)""
  IL_0006:  ldstr      ""1""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.2
  IL_000d:  stloc.1
  IL_000e:  stloc.3
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldloca.s   V_2
  IL_0015:  ldloc.3
  IL_0016:  calli      ""delegate*<ref string, in int, out object, void>""
  IL_001b:  ldloc.0
  IL_001c:  call       ""void System.Console.Write(string)""
  IL_0021:  ldloc.2
  IL_0022:  call       ""void System.Console.Write(object)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_ReturnStruct()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe struct S
{
    int i;
    public S(int i)
    {
        this.i = i;
    }
    void M() => Console.Write(i);

    static S MakeS(int i) => new S(i); 
    public static void Main()
    {
        delegate*<int, S> ptr = &MakeS;
        ptr(1).M();
    }
}", expectedOutput: "1");

            verifier.VerifyIL("S.Main()", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (delegate*<int, S> V_0,
                S V_1)
  IL_0000:  ldftn      ""S S.MakeS(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int, S>""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""void S.M()""
  IL_0016:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_ReturnClass()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    int i;
    public C(int i)
    {
        this.i = i;
    }
    void M() => Console.Write(i);

    static C MakeC(int i) => new C(i); 
    public static void Main()
    {
        delegate*<int, C> ptr = &MakeC;
        ptr(1).M();
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (delegate*<int, C> V_0)
  IL_0000:  ldftn      ""C C.MakeC(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int, C>""
  IL_000e:  callvirt   ""void C.M()""
  IL_0013:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_ContravariantParameters()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(object o, void* i) => Console.Write(o.ToString() + (*((int*)i)).ToString());
    static void Main()
    {
        delegate*<string, int*, void> ptr = &M;
        int i = 2;
        ptr(""1"", &i);
    }
}", expectedOutput: "12");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (int V_0, //i
                delegate*<string, int*, void> V_1)
  IL_0000:  ldftn      ""void C.M(object, void*)""
  IL_0006:  ldc.i4.2
  IL_0007:  stloc.0
  IL_0008:  stloc.1
  IL_0009:  ldstr      ""1""
  IL_000e:  ldloca.s   V_0
  IL_0010:  conv.u
  IL_0011:  ldloc.1
  IL_0012:  calli      ""delegate*<string, int*, void>""
  IL_0017:  ret
}");
        }

        [Fact]
        public void AddressOf_Initializer_CovariantReturns()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
public unsafe class C
{
    static string M1() => ""1"";
    static int i = 2;
    static int* M2()
    {
        fixed (int* i1 = &i)
        {
            return i1;
        }
    }

    static void Main()
    {
        delegate*<object> ptr1 = &M1;
        Console.Write(ptr1());
        delegate*<void*> ptr2 = &M2;
        Console.Write(*(int*)ptr2());
    }
}
", expectedOutput: "12");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       34 (0x22)
  .maxstack  1
  IL_0000:  ldftn      ""string C.M1()""
  IL_0006:  calli      ""delegate*<object>""
  IL_000b:  call       ""void System.Console.Write(object)""
  IL_0010:  ldftn      ""int* C.M2()""
  IL_0016:  calli      ""delegate*<void*>""
  IL_001b:  ldind.i4
  IL_001c:  call       ""void System.Console.Write(int)""
  IL_0021:  ret
}");
        }

        [Fact]
        public void AddressOf_FunctionPointerConversionReturn()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static string ToStringer(object o) => o.ToString();
    static delegate*<object, string> Returner() => &ToStringer;
    public static void Main()
    {
        delegate*<delegate*<string, object>> ptr = &Returner;
        Console.Write(ptr()(""1""));
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (delegate*<string, object> V_0)
  IL_0000:  ldftn      ""delegate*<object, string> C.Returner()""
  IL_0006:  calli      ""delegate*<delegate*<string, object>>""
  IL_000b:  stloc.0
  IL_000c:  ldstr      ""1""
  IL_0011:  ldloc.0
  IL_0012:  calli      ""delegate*<string, object>""
  IL_0017:  call       ""void System.Console.Write(object)""
  IL_001c:  ret
}
");
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref")]
        public void AddressOf_Initializer_Overloads(string refType)
        {
            var verifier = CompileAndVerifyFunctionPointers($@"
using System;
unsafe class C
{{
    static void M(object o) => Console.Write(""object"" + o.ToString());
    static void M(string s) => Console.Write(""string"" + s);
    static void M({refType} string s) {{ Console.Write(""{refType}"" + s); }}
    static void M(int i) => Console.Write(""int"" + i.ToString());
    static void Main()
    {{
        delegate*<string, void> ptr = &M;
        ptr(""1"");
        string s = ""2"";
        delegate*<{refType} string, void> ptr2 = &M;
        ptr2({refType} s);
    }}
}}", expectedOutput: $"string1{refType}2");

            verifier.VerifyIL("C.Main()", expectedIL: $@"
{{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (string V_0, //s
                delegate*<string, void> V_1,
                delegate*<{refType} string, void> V_2)
  IL_0000:  ldftn      ""void C.M(string)""
  IL_0006:  stloc.1
  IL_0007:  ldstr      ""1""
  IL_000c:  ldloc.1
  IL_000d:  calli      ""delegate*<string, void>""
  IL_0012:  ldstr      ""2""
  IL_0017:  stloc.0
  IL_0018:  ldftn      ""void C.M({refType} string)""
  IL_001e:  stloc.2
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.2
  IL_0022:  calli      ""delegate*<{refType} string, void>""
  IL_0027:  ret
}}
");
        }

        [Fact]
        public void AddressOf_Initializer_Overloads_Out()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(object o) => Console.Write(""object"" + o.ToString());
    static void M(string s) => Console.Write(""string"" + s);
    static void M(out string s) { s = ""2""; }
    static void M(int i) => Console.Write(""int"" + i.ToString());
    static void Main()
    {
        delegate*<string, void> ptr = &M;
        ptr(""1"");
        delegate*<out string, void> ptr2 = &M;
        ptr2(out string s);
        Console.Write(s);
    }
}", expectedOutput: $"string12");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (string V_0, //s
                delegate*<string, void> V_1,
                delegate*<out string, void> V_2)
  IL_0000:  ldftn      ""void C.M(string)""
  IL_0006:  stloc.1
  IL_0007:  ldstr      ""1""
  IL_000c:  ldloc.1
  IL_000d:  calli      ""delegate*<string, void>""
  IL_0012:  ldftn      ""void C.M(out string)""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_0
  IL_001b:  ldloc.2
  IL_001c:  calli      ""delegate*<out string, void>""
  IL_0021:  ldloc.0
  IL_0022:  call       ""void System.Console.Write(string)""
  IL_0027:  ret
}
");

            var comp = (CSharpCompilation)verifier.Compilation;
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var addressOfs = syntaxTree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().ToArray();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfs[0],
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "delegate*<System.String, System.Void>",
                expectedSymbol: "void C.M(System.String s)");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfs[1],
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) System.String, System.Void>",
                expectedSymbol: "void C.M(out System.String s)");

            string[] expectedMembers = new[] {
                "void C.M(System.Object o)",
                "void C.M(System.String s)",
                "void C.M(out System.String s)",
                "void C.M(System.Int32 i)"
            };

            AssertEx.Equal(expectedMembers, model.GetMemberGroup(addressOfs[0].Operand).Select(m => m.ToTestDisplayString(includeNonNullable: false)));
            AssertEx.Equal(expectedMembers, model.GetMemberGroup(addressOfs[1].Operand).Select(m => m.ToTestDisplayString(includeNonNullable: false)));
        }

        [Fact]
        public void AddressOf_Initializer_Overloads_NoMostSpecific()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
interface I1 {}
interface I2 {}
static class IHelpers
{
    public static void M(I1 i1) {}
    public static void M(I2 i2) {}
}
class C : I1, I2
{
    unsafe static void Main()
    {
        delegate*<C, void> ptr = &IHelpers.M;
    }
}");
            comp.VerifyDiagnostics(
                // (13,35): error CS0121: The call is ambiguous between the following methods or properties: 'IHelpers.M(I1)' and 'IHelpers.M(I2)'
                //         delegate*<C, void> ptr = &IHelpers.M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "IHelpers.M").WithArguments("IHelpers.M(I1)", "IHelpers.M(I2)").WithLocation(13, 35)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var addressOf = syntaxTree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOf,
                expectedSyntax: "&IHelpers.M",
                expectedType: null,
                expectedConvertedType: "delegate*<C, System.Void>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void IHelpers.M(I1 i1)", "void IHelpers.M(I2 i2)" });
        }

        [Fact]
        public void AddressOf_Initializer_Overloads_RefNotCovariant()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M1(ref object o) {}
    void M2(in object o) {}
    void M3(out string s) => throw null;
    void M()
    {
        delegate*<ref string, void> ptr1 = &M1;
        delegate*<string, void> ptr2 = &M1;
        delegate*<in string, void> ptr3 = &M2;
        delegate*<string, void> ptr4 = &M2;
        delegate*<out object, void> ptr5 = &M3;
        delegate*<string, void> ptr6 = &M3;
    }
}");

            comp.VerifyDiagnostics(
                // (9,44): error CS8757: No overload for 'M1' matches function pointer 'delegate*<ref string, void>'
                //         delegate*<ref string, void> ptr1 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<ref string, void>").WithLocation(9, 44),
                // (10,40): error CS8757: No overload for 'M1' matches function pointer 'delegate*<string, void>'
                //         delegate*<string, void> ptr2 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<string, void>").WithLocation(10, 40),
                // (11,43): error CS8757: No overload for 'M2' matches function pointer 'delegate*<in string, void>'
                //         delegate*<in string, void> ptr3 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<in string, void>").WithLocation(11, 43),
                // (12,40): error CS8757: No overload for 'M2' matches function pointer 'delegate*<string, void>'
                //         delegate*<string, void> ptr4 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<string, void>").WithLocation(12, 40),
                // (13,44): error CS8757: No overload for 'M3' matches function pointer 'delegate*<out object, void>'
                //         delegate*<out object, void> ptr5 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<out object, void>").WithLocation(13, 44),
                // (14,40): error CS8757: No overload for 'M3' matches function pointer 'delegate*<string, void>'
                //         delegate*<string, void> ptr6 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<string, void>").WithLocation(14, 40)
            );
        }

        [Fact]
        public void AddressOf_RefsMustMatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    void M1(ref object o) {}
    void M2(in object o) {}
    void M3(out object s) => throw null;
    void M4(object s) => throw null;
    ref object M5() => throw null;
    ref readonly object M6() => throw null;
    object M7() => throw null!;
    void M()
    {
        delegate*<object, void> ptr1 = &M1;
        delegate*<object, void> ptr2 = &M2;
        delegate*<object, void> ptr3 = &M3;
        delegate*<ref object, void> ptr4 = &M2;
        delegate*<ref object, void> ptr5 = &M3;
        delegate*<ref object, void> ptr6 = &M4;
        delegate*<in object, void> ptr7 = &M1;
        delegate*<in object, void> ptr8 = &M3;
        delegate*<in object, void> ptr9 = &M4;
        delegate*<out object, void> ptr10 = &M1;
        delegate*<out object, void> ptr11 = &M2;
        delegate*<out object, void> ptr12 = &M4;
        delegate*<object> ptr13 = &M5;
        delegate*<object> ptr14 = &M6;
        delegate*<ref object> ptr15 = &M6;
        delegate*<ref object> ptr16 = &M7;
        delegate*<ref readonly object> ptr17 = &M5;
        delegate*<ref readonly object> ptr18 = &M7;
    }
}");

            comp.VerifyDiagnostics(
                // (13,40): error CS8757: No overload for 'M1' matches function pointer 'delegate*<object, void>'
                //         delegate*<object, void> ptr1 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<object, void>").WithLocation(13, 40),
                // (14,40): error CS8757: No overload for 'M2' matches function pointer 'delegate*<object, void>'
                //         delegate*<object, void> ptr2 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<object, void>").WithLocation(14, 40),
                // (15,40): error CS8757: No overload for 'M3' matches function pointer 'delegate*<object, void>'
                //         delegate*<object, void> ptr3 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<object, void>").WithLocation(15, 40),
                // (16,44): error CS8757: No overload for 'M2' matches function pointer 'delegate*<ref object, void>'
                //         delegate*<ref object, void> ptr4 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<ref object, void>").WithLocation(16, 44),
                // (17,44): error CS8757: No overload for 'M3' matches function pointer 'delegate*<ref object, void>'
                //         delegate*<ref object, void> ptr5 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<ref object, void>").WithLocation(17, 44),
                // (18,44): error CS8757: No overload for 'M4' matches function pointer 'delegate*<ref object, void>'
                //         delegate*<ref object, void> ptr6 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<ref object, void>").WithLocation(18, 44),
                // (19,43): error CS8757: No overload for 'M1' matches function pointer 'delegate*<in object, void>'
                //         delegate*<in object, void> ptr7 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<in object, void>").WithLocation(19, 43),
                // (20,43): error CS8757: No overload for 'M3' matches function pointer 'delegate*<in object, void>'
                //         delegate*<in object, void> ptr8 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<in object, void>").WithLocation(20, 43),
                // (21,43): error CS8757: No overload for 'M4' matches function pointer 'delegate*<in object, void>'
                //         delegate*<in object, void> ptr9 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<in object, void>").WithLocation(21, 43),
                // (22,45): error CS8757: No overload for 'M1' matches function pointer 'delegate*<out object, void>'
                //         delegate*<out object, void> ptr10 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<out object, void>").WithLocation(22, 45),
                // (23,45): error CS8757: No overload for 'M2' matches function pointer 'delegate*<out object, void>'
                //         delegate*<out object, void> ptr11 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<out object, void>").WithLocation(23, 45),
                // (24,45): error CS8757: No overload for 'M4' matches function pointer 'delegate*<out object, void>'
                //         delegate*<out object, void> ptr12 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<out object, void>").WithLocation(24, 45),
                // (25,36): error CS8758: Ref mismatch between 'C.M5()' and function pointer 'delegate*<object>'
                //         delegate*<object> ptr13 = &M5;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M5").WithArguments("C.M5()", "delegate*<object>").WithLocation(25, 36),
                // (26,36): error CS8758: Ref mismatch between 'C.M6()' and function pointer 'delegate*<object>'
                //         delegate*<object> ptr14 = &M6;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M6").WithArguments("C.M6()", "delegate*<object>").WithLocation(26, 36),
                // (27,40): error CS8758: Ref mismatch between 'C.M6()' and function pointer 'delegate*<object>'
                //         delegate*<ref object> ptr15 = &M6;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M6").WithArguments("C.M6()", "delegate*<object>").WithLocation(27, 40),
                // (28,40): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<object>'
                //         delegate*<ref object> ptr16 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<object>").WithLocation(28, 40),
                // (29,49): error CS8758: Ref mismatch between 'C.M5()' and function pointer 'delegate*<object>'
                //         delegate*<ref readonly object> ptr17 = &M5;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M5").WithArguments("C.M5()", "delegate*<object>").WithLocation(29, 49),
                // (30,49): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<object>'
                //         delegate*<ref readonly object> ptr18 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<object>").WithLocation(30, 49)
            );
        }

        [Theory]
        [InlineData("cdecl", "CDecl")]
        [InlineData("stdcall", "Standard")]
        [InlineData("thiscall", "ThisCall")]
        public void AddressOf_CallingConventionMustMatch(string callingConventionKeyword, string callingConvention)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    static void M1() {{}}
    static void M()
    {{
        delegate* {callingConventionKeyword}<void> ptr = &M1;
    }}
}}");

            comp.VerifyDiagnostics(
                // (7,41): error CS8786: Calling convention of 'C.M1()' is not compatible with '{callingConvention}'.
                //         delegate* {callingConventionKeyword}<void> ptr = &M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M1").WithArguments("C.M1()", callingConvention).WithLocation(7, 33 + callingConventionKeyword.Length));
        }

        [Fact]
        public void AddressOf_Assignment()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static string Convert(int i) => i.ToString();
    static void Main()
    {
        delegate*<int, string> ptr;
        ptr = &Convert;
        Console.Write(ptr(1));
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (delegate*<int, string> V_0)
  IL_0000:  ldftn      ""string C.Convert(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int, string>""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void AddressOf_NonStaticMethods()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
public class C
{
    public unsafe void M()
    {
        delegate*<void> ptr1 = &M;
        int? i = null;
        delegate*<int> ptr2 = &i.GetValueOrDefault;
    }
}");

            comp.VerifyDiagnostics(
                // (6,33): error CS8759: Cannot bind function pointer to 'C.M()' because it is not a static method
                //         delegate*<void> ptr1 = &M;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "M").WithArguments("C.M()").WithLocation(6, 33),
                // (8,32): error CS8759: Cannot bind function pointer to 'int?.GetValueOrDefault()' because it is not a static method
                //         delegate*<int> ptr2 = &i.GetValueOrDefault;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "i.GetValueOrDefault").WithArguments("int?.GetValueOrDefault()").WithLocation(8, 32)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var declarators = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(d => d.Initializer!.Value.IsKind(SyntaxKind.AddressOfExpression)).ToArray();
            var addressOfs = declarators.Select(d => d.Initializer!.Value).ToArray();
            Assert.Equal(2, addressOfs.Length);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfs[0],
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Void>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });

            VerifyOperationTreeForNode(comp, model, declarators[0], expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Void> ptr1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr1 = &M')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= &M')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Void>, IsInvalid, IsImplicit) (Syntax: '&M')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&M')
            Reference: 
              IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
                Children(1):
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
            ");

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOfs[1],
                expectedSyntax: "&i.GetValueOrDefault",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Int32>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "System.Int32 System.Int32?.GetValueOrDefault()", "System.Int32 System.Int32?.GetValueOrDefault(System.Int32 defaultValue)" });

            VerifyOperationTreeForNode(comp, model, declarators[1], expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Int32> ptr2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr2 = &i.G ... ueOrDefault')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= &i.GetValueOrDefault')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32>, IsInvalid, IsImplicit) (Syntax: '&i.GetValueOrDefault')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&i.GetValueOrDefault')
            Reference: 
              IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'i.GetValueOrDefault')
                Children(1):
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32?, IsInvalid) (Syntax: 'i')
            ");
        }

        [Fact]
        public void AddressOf_MultipleInvalidOverloads()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static int M(string s) => throw null;
    static int M(ref int i) => throw null;

    static void M1()
    {
        delegate*<int, int> ptr = &M;
    }
}");

            comp.VerifyDiagnostics(
                // (9,35): error CS8757: No overload for 'M' matches function pointer 'delegate*<int, int>'
                //         delegate*<int, int> ptr = &M;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M").WithArguments("M", "delegate*<int, int>").WithLocation(9, 35)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var declarator = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var addressOf = declarator.Initializer!.Value;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOf,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Int32, System.Int32>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "System.Int32 C.M(System.String s)", "System.Int32 C.M(ref System.Int32 i)" });

            VerifyOperationTreeForNode(comp, model, declarator, expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Int32, System.Int32> ptr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr = &M')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= &M')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Int32, System.Int32>, IsInvalid, IsImplicit) (Syntax: '&M')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&M')
            Reference: 
              IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
                Children(1):
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
");
        }

        [Fact]
        public void AddressOf_AmbiguousBestMethod()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M(string s, object o) {}
    static void M(object o, string s) {}
    static void M1()
    {
        delegate*<string, string, void> ptr = &M;
    }
}");
            comp.VerifyDiagnostics(
                // (8,48): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(string, object)' and 'C.M(object, string)'
                //         delegate*<string, string, void> ptr = &M;
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(string, object)", "C.M(object, string)").WithLocation(8, 48)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var declarator = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var addressOf = declarator.Initializer!.Value;

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, addressOf,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "delegate*<System.String, System.String, System.Void>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M(System.String s, System.Object o)", "void C.M(System.Object o, System.String s)" });

            VerifyOperationTreeForNode(comp, model, declarator, expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.String, System.String, System.Void> ptr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'ptr = &M')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= &M')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.String, System.String, System.Void>, IsInvalid, IsImplicit) (Syntax: '&M')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&M')
            Reference: 
              IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
                Children(1):
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
");
        }

        [Fact]
        public void AddressOf_AsLvalue()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M() {}
    static void M1()
    {
        delegate*<void> ptr = &M;
        &M = ptr;
        M2(&M);
        M2(ref &M);
        ref delegate*<void> ptr2 = ref &M;
    }
    static void M2(ref delegate*<void> ptr) {}
}");

            comp.VerifyDiagnostics(
                // (8,9): error CS1656: Cannot assign to 'M' because it is a '&method group'
                //         &M = ptr;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "&M").WithArguments("M", "&method group").WithLocation(8, 9),
                // (9,12): error CS1503: Argument 1: cannot convert from '&method group' to 'ref delegate*<void>'
                //         M2(&M);
                Diagnostic(ErrorCode.ERR_BadArgType, "&M").WithArguments("1", "&method group", "ref delegate*<void>").WithLocation(9, 12),
                // (10,16): error CS1657: Cannot use 'M' as a ref or out value because it is a '&method group'
                //         M2(ref &M);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "&M").WithArguments("M", "&method group").WithLocation(10, 16),
                // (11,40): error CS1657: Cannot use 'M' as a ref or out value because it is a '&method group'
                //         ref delegate*<void> ptr2 = ref &M;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "&M").WithArguments("M", "&method group").WithLocation(11, 40)
            );
        }

        [Fact]
        public void AddressOf_MethodParameter()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(string s) => Console.Write(s);
    static void Caller(delegate*<string, void> ptr) => ptr(""1"");
    static void Main()
    {
        Caller(&M);
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void C.M(string)""
  IL_0006:  call       ""void C.Caller(delegate*<string, void>)""
  IL_000b:  ret
}
");
        }

        [Fact]
        [WorkItem(44489, "https://github.com/dotnet/roslyn/issues/44489")]
        public void AddressOf_CannotAssignToVoidStar()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M()
    {
        void* ptr1 = &M;
        void* ptr2 = (void*)&M;
    }
}");

            comp.VerifyDiagnostics(
                // (6,22): error CS8812: Cannot convert &method group 'M' to non-function pointer type 'void*'.
                //         void* ptr1 = &M;
                Diagnostic(ErrorCode.ERR_AddressOfToNonFunctionPointer, "&M").WithArguments("M", "void*").WithLocation(6, 22),
                // (7,22): error CS8812: Cannot convert &method group 'M' to non-function pointer type 'void*'.
                //         void* ptr2 = (void*)&M;
                Diagnostic(ErrorCode.ERR_AddressOfToNonFunctionPointer, "(void*)&M").WithArguments("M", "void*").WithLocation(7, 22)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, decls[0].Initializer!.Value,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "System.Void*",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ((CastExpressionSyntax)decls[1].Initializer!.Value).Expression,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: null,
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });
        }

        [Fact]
        [WorkItem(44489, "https://github.com/dotnet/roslyn/issues/44489")]
        public void AddressOf_ToDelegateType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
class C
{
    unsafe void M()
    {
        // This actually gets bound as a binary expression: (Action) & M
        Action ptr1 = (Action)&M;
        Action ptr2 = (Action)(&M);
        Action ptr3 = &M;
    }
}");

            comp.VerifyDiagnostics(
                // (8,24): error CS0119: 'Action' is a type, which is not valid in the given context
                //         Action ptr1 = (Action)&M;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Action").WithArguments("System.Action", "type").WithLocation(8, 24),
                // (8,24): error CS0119: 'Action' is a type, which is not valid in the given context
                //         Action ptr1 = (Action)&M;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Action").WithArguments("System.Action", "type").WithLocation(8, 24),
                // (9,23): error CS8811: Cannot convert &method group 'M' to delegate type 'M'.
                //         Action ptr2 = (Action)(&M);
                Diagnostic(ErrorCode.ERR_CannotConvertAddressOfToDelegate, "(Action)(&M)").WithArguments("M", "System.Action").WithLocation(9, 23),
                // (10,23): error CS8811: Cannot convert &method group 'M' to delegate type 'M'.
                //         Action ptr3 = &M;
                Diagnostic(ErrorCode.ERR_CannotConvertAddressOfToDelegate, "&M").WithArguments("M", "System.Action").WithLocation(10, 23)
            );


            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ((CastExpressionSyntax)decls[1].Initializer!.Value).Expression,
                expectedSyntax: "(&M)",
                expectedType: null,
                expectedConvertedType: null,
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, decls[2].Initializer!.Value,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "System.Action",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });
        }

        [Fact]
        [WorkItem(44489, "https://github.com/dotnet/roslyn/issues/44489")]
        public void AddressOf_ToNonDelegateOrPointerType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
class C
{
    unsafe void M()
    {
        // This actually gets bound as a binary expression: (C) & M
        C ptr1 = (C)&M;
        C ptr2 = (C)(&M);
        C ptr3 = &M;
    }
}");

            comp.VerifyDiagnostics(
                // (7,19): error CS0119: 'C' is a type, which is not valid in the given context
                //         C ptr1 = (C)&M;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(7, 19),
                // (7,19): error CS0119: 'C' is a type, which is not valid in the given context
                //         C ptr1 = (C)&M;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(7, 19),
                // (8,18): error CS8812: Cannot convert &method group 'M' to non-function pointer type 'C'.
                //         C ptr2 = (C)(&M);
                Diagnostic(ErrorCode.ERR_AddressOfToNonFunctionPointer, "(C)(&M)").WithArguments("M", "C").WithLocation(8, 18),
                // (9,18): error CS8812: Cannot convert &method group 'M' to non-function pointer type 'C'.
                //         C ptr3 = &M;
                Diagnostic(ErrorCode.ERR_AddressOfToNonFunctionPointer, "&M").WithArguments("M", "C").WithLocation(9, 18)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ((CastExpressionSyntax)decls[1].Initializer!.Value).Expression,
                expectedSyntax: "(&M)",
                expectedType: null,
                expectedConvertedType: null,
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, decls[2].Initializer!.Value,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: "C",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });
        }

        [Fact]
        public void AddressOf_ExplicitCastToNonCompatibleFunctionPointerType()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
class C
{
    unsafe void M()
    {
        var ptr = (delegate*<string>)&M;
    }
}
");

            comp.VerifyDiagnostics(
                // (6,19): error CS8757: No overload for 'M' matches function pointer 'delegate*<string>'
                //         var ptr = (delegate*<string>)&M;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "(delegate*<string>)&M").WithArguments("M", "delegate*<string>").WithLocation(6, 19)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, ((CastExpressionSyntax)decls[0].Initializer!.Value).Expression,
                expectedSyntax: "&M",
                expectedType: null,
                expectedConvertedType: null,
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M()" });
        }

        [Fact]
        public void AddressOf_DisallowedInExpressionTree()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Linq.Expressions;
unsafe class C
{
    static string M1(delegate*<string> ptr) => ptr();
    static string M2() => string.Empty;

    static void M()
    {
        Expression<Func<string>> a = () => M1(&M2);
        Expression<Func<string>> b = () => (&M2)();
    }
}");

            comp.VerifyDiagnostics(
                // (11,47): error CS1944: An expression tree may not contain an unsafe pointer operation
                //         Expression<Func<string>> a = () => M1(&M2);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPointerOp, "&M2").WithLocation(11, 47),
                // (11,48): error CS8810: '&' on method groups cannot be used in expression trees
                //         Expression<Func<string>> a = () => M1(&M2);
                Diagnostic(ErrorCode.ERR_AddressOfMethodGroupInExpressionTree, "M2").WithLocation(11, 48),
                // (12,44): error CS0149: Method name expected
                //         Expression<Func<string>> b = () => (&M2)();
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "(&M2)").WithLocation(12, 44)
            );
        }

        [Fact]
        public void FunctionPointerTypeUsageInExpressionTree()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Linq.Expressions;
unsafe class C
{
    void M1(delegate*<void> ptr)
    {
        Expression<Action> a = () => M2(ptr);
    }
    void M2(void* ptr) {}
}
");

            comp.VerifyDiagnostics(
                // (8,41): error CS1944: An expression tree may not contain an unsafe pointer operation
                //         Expression<Action> a = () => M2(ptr);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPointerOp, "ptr").WithLocation(8, 41)
            );
        }

        [Fact]
        public void AmbiguousApplicableMethodsAreFilteredForStatic()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
interface I1{}
interface I2
{
    string Prop { get; }
}

public unsafe class C : I1, I2 {
    void M(I1 i) {}
    static void M(I2 i) => Console.Write(i.Prop);
    public static void Main() {
        delegate*<C, void> a = &M;
        a(new C());
    }
    public string Prop { get => ""I2""; }
}", expectedOutput: "I2");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (delegate*<C, void> V_0)
  IL_0000:  ldftn      ""void C.M(I2)""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""C..ctor()""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate*<C, void>""
  IL_0012:  ret
}
");
        }

        [Fact]
        public void TypeArgumentNotSpecifiedNotInferred()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M1<T>(int i) {}
    static T M2<T>() => throw null;

    static void M()
    {
        delegate*<int, void> ptr1 = &C.M1;
        delegate*<string> ptr2 = &C.M2;
    }
}");

            comp.VerifyDiagnostics(
                // (9,38): error CS0411: The type arguments for method 'C.M1<T>(int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         delegate*<int, void> ptr1 = &C.M1;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "C.M1").WithArguments("C.M1<T>(int)").WithLocation(9, 38),
                // (10,35): error CS0411: The type arguments for method 'C.M2<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         delegate*<string> ptr2 = &C.M2;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "C.M2").WithArguments("C.M2<T>()").WithLocation(10, 35)
            );
        }

        [Fact]
        public void TypeArgumentSpecifiedOrInferred()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M1<T>(T t) => Console.Write(t);
    static void Main()
    {
        delegate*<int, void> ptr = &C.M1<int>;
        ptr(1);
        ptr = &C.M1;
        ptr(2);
    }
}", expectedOutput: "12");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (delegate*<int, void> V_0)
  IL_0000:  ldftn      ""void C.M1<int>(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int, void>""
  IL_000e:  ldftn      ""void C.M1<int>(int)""
  IL_0014:  stloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  ldloc.0
  IL_0017:  calli      ""delegate*<int, void>""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void ReducedExtensionMethod()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe static class CHelper
{
    public static void M1(this C c) {}
}
unsafe class C
{
    static void M(C c)
    {
        delegate*<C, void> ptr1 = &c.M1;
        delegate*<void> ptr2 = &c.M1;
    }
}");

            comp.VerifyDiagnostics(
                // (10,35): error CS8757: No overload for 'M1' matches function pointer 'delegate*<C, void>'
                //         delegate*<C, void> ptr1 = &c.M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&c.M1").WithArguments("M1", "delegate*<C, void>").WithLocation(10, 35),
                // (11,32): error CS8788: Cannot use an extension method with a receiver as the target of a '&amp;' operator.
                //         delegate*<void> ptr2 = &c.M1;
                Diagnostic(ErrorCode.ERR_CannotUseReducedExtensionMethodInAddressOf, "&c.M1").WithLocation(11, 32)
            );
        }

        [Fact]
        public void UnreducedExtensionMethod()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
#pragma warning suppress CS0414 // Field never used
using System;
unsafe static class CHelper
{
    public static void M1(this C c) => Console.Write(c.i);
}
unsafe class C
{
    public int i;
    static void Main()
    {
        delegate*<C, void> ptr = &CHelper.M1;
        var c = new C();
        c.i = 1;
        ptr(c);
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (C V_0, //c
                delegate*<C, void> V_1)
  IL_0000:  ldftn      ""void CHelper.M1(C)""
  IL_0006:  newobj     ""C..ctor()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      ""int C.i""
  IL_0013:  stloc.1
  IL_0014:  ldloc.0
  IL_0015:  ldloc.1
  IL_0016:  calli      ""delegate*<C, void>""
  IL_001b:  ret
}
");
        }

        [Fact]
        public void BadScenariosDontCrash()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M1() {}
    static void M2()
    {
        &delegate*<void> ptr = &M1;
    }
}
");

            comp.VerifyDiagnostics(
                // (7,18): error CS1514: { expected
                //         &delegate*<void> ptr = &M1;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "*").WithLocation(7, 18),
                // (7,19): error CS1525: Invalid expression term '<'
                //         &delegate*<void> ptr = &M1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(7, 19),
                // (7,20): error CS1525: Invalid expression term 'void'
                //         &delegate*<void> ptr = &M1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void").WithLocation(7, 20),
                // (7,26): error CS0103: The name 'ptr' does not exist in the current context
                //         &delegate*<void> ptr = &M1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ptr").WithArguments("ptr").WithLocation(7, 26)
            );
        }

        [Fact]
        public void EmptyMethodGroups()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M1()
    {
        delegate*<C, void> ptr1 = &C.NonExistent;
        delegate*<C, void> ptr2 = &NonExistent;
    }
}
");

            comp.VerifyDiagnostics(
                // (6,38): error CS0117: 'C' does not contain a definition for 'NonExistent'
                //         delegate*<C, void> ptr1 = &C.NonExistent;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "NonExistent").WithArguments("C", "NonExistent").WithLocation(6, 38),
                // (7,36): error CS0103: The name 'NonExistent' does not exist in the current context
                //         delegate*<C, void> ptr2 = &NonExistent;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "NonExistent").WithArguments("NonExistent").WithLocation(7, 36)
            );
        }

        [Fact]
        public void MultipleApplicableMethods()
        {
            // This is analogous to MethodBodyModelTests.MethodGroupToDelegate04, where both methods
            // are applicable even though D(delegate*<int, void>) is not compatible.
            var comp = CreateCompilationWithFunctionPointers(@"
public unsafe class Program1
{
    static void Y(long x) { }

    static void D(delegate*<int, void> o) { }
    static void D(delegate*<long, void> o) { }

    void T()
    {
        D(&Y);
    }
}
");

            comp.VerifyDiagnostics(
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program1.D(delegate*<int, void>)' and 'Program1.D(delegate*<long, void>)'
                //         D(&Y);
                Diagnostic(ErrorCode.ERR_AmbigCall, "D").WithArguments("Program1.D(delegate*<int, void>)", "Program1.D(delegate*<long, void>)").WithLocation(11, 9)
            );
        }

        [Fact]
        public void InvalidTopAttributeErrors()
        {

            using var peStream = new MemoryStream();
            var ilBuilder = new BlobBuilder();
            var metadataBuilder = new MetadataBuilder();
            // SignatureAttributes has the following values:
            // 0x00 - default
            // 0x10 - Generic
            // 0x20 - Instance
            // 0x40 - ExplicitThis
            // There is no defined meaning for 0x80, the 8th bit here, so this signature is invalid.
            // ldftn throws an invalid signature exception at runtime, so we error here for function
            // pointers.
            DefineInvalidSignatureAttributeIL(metadataBuilder, ilBuilder, headerToUseForM: new SignatureHeader(SignatureKind.Method, SignatureCallingConvention.Default, ((SignatureAttributes)0x80)));
            WritePEImage(peStream, metadataBuilder, ilBuilder);
            peStream.Position = 0;

            var invalidAttributeReference = MetadataReference.CreateFromStream(peStream);
            var comp = CreateCompilationWithFunctionPointers(@"
using ConsoleApplication;
unsafe class C
{
    static void Main()
    {
        delegate*<void> ptr = &Program.M;
    }
}", references: new[] { invalidAttributeReference });

            comp.VerifyEmitDiagnostics(
                // (7,32): error CS8776: Calling convention of 'Program.M()' is not compatible with 'Default'.
                //         delegate*<void> ptr = &Program.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "Program.M").WithArguments("ConsoleApplication.Program.M()", "Default").WithLocation(7, 32)
            );
        }

        [Fact]
        public void MissingAddressOf()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
class C
{
    static void M1() {}
    static unsafe void M2(delegate*<void> b)
    {
        delegate*<void> a = M1;
        M2(M1);
    }
}");

            comp.VerifyDiagnostics(
                // (7,29): error CS8787: Cannot convert method group to function pointer (Are you missing a '&'?)
                //         delegate*<void> a = M1;
                Diagnostic(ErrorCode.ERR_MissingAddressOf, "M1").WithLocation(7, 29),
                // (8,12): error CS8787: Cannot convert method group to function pointer (Are you missing a '&'?)
                //         M2(M1);
                Diagnostic(ErrorCode.ERR_MissingAddressOf, "M1").WithLocation(8, 12)
            );

            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var variableDeclaratorSyntax = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            var methodGroup1 = variableDeclaratorSyntax.Initializer!.Value;
            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model, methodGroup1,
                expectedSyntax: "M1",
                expectedType: null,
                expectedConvertedType: "delegate*<System.Void>",
                expectedCandidateReason: CandidateReason.OverloadResolutionFailure,
                expectedSymbolCandidates: new[] { "void C.M1()" });

            AssertEx.Equal(new[] { "void C.M1()" }, model.GetMemberGroup(methodGroup1).Select(m => m.ToTestDisplayString(includeNonNullable: false)));

            VerifyOperationTreeForNode(comp, model, variableDeclaratorSyntax, expectedOperationTree: @"
IVariableDeclaratorOperation (Symbol: delegate*<System.Void> a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = M1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= M1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: delegate*<System.Void>, IsInvalid, IsImplicit) (Syntax: 'M1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M1')
");
        }

        [Fact]
        public void NestedFunctionPointerVariantConversion()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    public static void Printer(object o) => Console.Write(o);
    public static void PrintWrapper(delegate*<string, void> printer, string o) => printer(o);
    static void Main()
    {
        delegate*<delegate*<object, void>, string, void> wrapper = &PrintWrapper;
        delegate*<object, void> printer = &Printer;
        wrapper(printer, ""1""); 
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (delegate*<object, void> V_0, //printer
                delegate*<delegate*<object, void>, string, void> V_1)
  IL_0000:  ldftn      ""void C.PrintWrapper(delegate*<string, void>, string)""
  IL_0006:  ldftn      ""void C.Printer(object)""
  IL_000c:  stloc.0
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldstr      ""1""
  IL_0014:  ldloc.1
  IL_0015:  calli      ""delegate*<delegate*<object, void>, string, void>""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void ArraysSupport()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    public static void M(string s) => Console.Write(s);
    public static void Main()
    {
        delegate*<string, void>[] ptrs = new delegate*<string, void>[] { &M, &M };
        for (int i = 0; i < ptrs.Length; i++)
        {
            ptrs[i](i.ToString());
        }
    }
}", expectedOutput: "01");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (delegate*<string, void>[] V_0, //ptrs
                int V_1, //i
                delegate*<string, void> V_2)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""delegate*<string, void>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldftn      ""void C.M(string)""
  IL_000e:  stelem.i
  IL_000f:  dup
  IL_0010:  ldc.i4.1
  IL_0011:  ldftn      ""void C.M(string)""
  IL_0017:  stelem.i
  IL_0018:  stloc.0
  IL_0019:  ldc.i4.0
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_0032
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  ldelem.i
  IL_0020:  stloc.2
  IL_0021:  ldloca.s   V_1
  IL_0023:  call       ""string int.ToString()""
  IL_0028:  ldloc.2
  IL_0029:  calli      ""delegate*<string, void>""
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  stloc.1
  IL_0032:  ldloc.1
  IL_0033:  ldloc.0
  IL_0034:  ldlen
  IL_0035:  conv.i4
  IL_0036:  blt.s      IL_001d
  IL_0038:  ret
}
");
        }

        [Fact]
        public void ArrayElementRef()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    public static void Print() => Console.Write(1);

    public static void M(delegate*<void>[] a)
    {
        ref delegate*<void> ptr = ref a[0];
        ptr = &Print;
    }
    
    public static void Main()
    {
        var a = new delegate*<void>[1];
        M(a);
        a[0]();
    }
}");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""delegate*<void>""
  IL_0007:  ldftn      ""void C.Print()""
  IL_000d:  stind.i
  IL_000e:  ret
}
");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""delegate*<void>""
  IL_0006:  dup
  IL_0007:  call       ""void C.M(delegate*<void>[])""
  IL_000c:  ldc.i4.0
  IL_000d:  ldelem.i
  IL_000e:  calli      ""delegate*<void>""
  IL_0013:  ret
}
");
        }

        [Fact]
        public void FixedSizeBufferOfFunctionPointers()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe struct S
{
    fixed delegate*<void> ptrs[1];
}");

            comp.VerifyDiagnostics(
                // (4,11): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //     fixed delegate*<void> ptrs[1];
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "delegate*<void>").WithLocation(4, 11)
            );
        }

        [Fact]
        public void IndirectLoadsAndStores()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static delegate*<void> field;
    static void Printer() => Console.Write(1);
    static ref delegate*<void> Getter() => ref field;

    static void Main()
    {
        ref var printer = ref Getter();
        printer = &Printer;
        printer();
        field();
    }
}", expectedOutput: "11");

            verifier.VerifyIL(@"C.Main", expectedIL: @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  call       ""ref delegate*<void> C.Getter()""
  IL_0005:  dup
  IL_0006:  ldftn      ""void C.Printer()""
  IL_000c:  stind.i
  IL_000d:  ldind.i
  IL_000e:  calli      ""delegate*<void>""
  IL_0013:  ldsfld     ""delegate*<void> C.field""
  IL_0018:  calli      ""delegate*<void>""
  IL_001d:  ret
}
");
        }

        [Fact]
        public void Foreach()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    public static void M(string s) => Console.Write(s);
    public static void Main()
    {
        delegate*<string, void>[] ptrs = new delegate*<string, void>[] { &M, &M };
        int i = 0;
        foreach (delegate*<string, void> ptr in ptrs)
        {
            ptr(i++.ToString());
        }
    }
}", expectedOutput: "01");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (int V_0, //i
                delegate*<string, void>[] V_1,
                int V_2,
                delegate*<string, void> V_3,
                int V_4)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""delegate*<string, void>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldftn      ""void C.M(string)""
  IL_000e:  stelem.i
  IL_000f:  dup
  IL_0010:  ldc.i4.1
  IL_0011:  ldftn      ""void C.M(string)""
  IL_0017:  stelem.i
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  IL_001a:  stloc.1
  IL_001b:  ldc.i4.0
  IL_001c:  stloc.2
  IL_001d:  br.s       IL_003b
  IL_001f:  ldloc.1
  IL_0020:  ldloc.2
  IL_0021:  ldelem.i
  IL_0022:  stloc.3
  IL_0023:  ldloc.0
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stloc.0
  IL_0028:  stloc.s    V_4
  IL_002a:  ldloca.s   V_4
  IL_002c:  call       ""string int.ToString()""
  IL_0031:  ldloc.3
  IL_0032:  calli      ""delegate*<string, void>""
  IL_0037:  ldloc.2
  IL_0038:  ldc.i4.1
  IL_0039:  add
  IL_003a:  stloc.2
  IL_003b:  ldloc.2
  IL_003c:  ldloc.1
  IL_003d:  ldlen
  IL_003e:  conv.i4
  IL_003f:  blt.s      IL_001f
  IL_0041:  ret
}
");
        }

        [Fact]
        public void FieldInitializers()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    delegate*<string, void>[] arr1;
    delegate*<string, void>[] arr2 = new delegate*<string, void>[1];
    static void Print(string s) => Console.Write(s);
    static void Main()
    {
        var c = new C()
        {
            arr1 = new delegate*<string, void>[] { &Print },
            arr2 = { [0] = &Print }
        };

        c.arr1[0](""1"");
        c.arr2[0](""2"");
    }
}", expectedOutput: "12");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       82 (0x52)
  .maxstack  5
  .locals init (C V_0,
                delegate*<string, void> V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""delegate*<string, void>""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldftn      ""void C.Print(string)""
  IL_0015:  stelem.i
  IL_0016:  stfld      ""delegate*<string, void>[] C.arr1""
  IL_001b:  ldloc.0
  IL_001c:  ldfld      ""delegate*<string, void>[] C.arr2""
  IL_0021:  ldc.i4.0
  IL_0022:  ldftn      ""void C.Print(string)""
  IL_0028:  stelem.i
  IL_0029:  ldloc.0
  IL_002a:  dup
  IL_002b:  ldfld      ""delegate*<string, void>[] C.arr1""
  IL_0030:  ldc.i4.0
  IL_0031:  ldelem.i
  IL_0032:  stloc.1
  IL_0033:  ldstr      ""1""
  IL_0038:  ldloc.1
  IL_0039:  calli      ""delegate*<string, void>""
  IL_003e:  ldfld      ""delegate*<string, void>[] C.arr2""
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.i
  IL_0045:  stloc.1
  IL_0046:  ldstr      ""2""
  IL_004b:  ldloc.1
  IL_004c:  calli      ""delegate*<string, void>""
  IL_0051:  ret
}
");
        }

        [Fact]
        public void InitializeFunctionPointerWithNull()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Main()
    {
         delegate*<string, void>[] ptrs = new delegate*<string, void>[] { null, null, null }; 
         Console.Write(ptrs[0] is null);
    }
}", expectedOutput: "True");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       32 (0x20)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""delegate*<string, void>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.0
  IL_0009:  conv.u
  IL_000a:  stelem.i
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.0
  IL_000e:  conv.u
  IL_000f:  stelem.i
  IL_0010:  dup
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.0
  IL_0013:  conv.u
  IL_0014:  stelem.i
  IL_0015:  ldc.i4.0
  IL_0016:  ldelem.i
  IL_0017:  ldnull
  IL_0018:  ceq
  IL_001a:  call       ""void System.Console.Write(bool)""
  IL_001f:  ret
}
");
        }

        [Fact]
        public void InferredArrayInitializer_ParameterVariance()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Print(object o) => Console.Write(o);
    static void Print(string s) => Console.Write(s);
    static void Main()
    {
        delegate*<string, void> ptr1 = &Print;
        delegate*<object, void> ptr2 = &Print;
        var ptrs = new[] { ptr1, ptr2 }; 
        ptrs[0](""1"");
        ptrs[1](""2"");
    }
}", expectedOutput: "12");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       58 (0x3a)
  .maxstack  4
  .locals init (delegate*<string, void> V_0, //ptr1
                delegate*<object, void> V_1, //ptr2
                delegate*<string, void> V_2)
  IL_0000:  ldftn      ""void C.Print(string)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void C.Print(object)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.2
  IL_000f:  newarr     ""delegate*<string, void>""
  IL_0014:  dup
  IL_0015:  ldc.i4.0
  IL_0016:  ldloc.0
  IL_0017:  stelem.i
  IL_0018:  dup
  IL_0019:  ldc.i4.1
  IL_001a:  ldloc.1
  IL_001b:  stelem.i
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldelem.i
  IL_001f:  stloc.2
  IL_0020:  ldstr      ""1""
  IL_0025:  ldloc.2
  IL_0026:  calli      ""delegate*<string, void>""
  IL_002b:  ldc.i4.1
  IL_002c:  ldelem.i
  IL_002d:  stloc.2
  IL_002e:  ldstr      ""2""
  IL_0033:  ldloc.2
  IL_0034:  calli      ""delegate*<string, void>""
  IL_0039:  ret
}
");
        }

        [Fact]
        public void InferredArrayInitializer_ReturnVariance()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static object GetObject() => 1.ToString();
    static string GetString() => 2.ToString();
    static void Print(delegate*<object>[] ptrs)
    {
        Console.Write(""Object"");
        foreach (var ptr in ptrs)
        {
            Console.Write(ptr());
        }
    }
    static void Print(delegate*<string>[] ptrs)
    {
        Console.Write(""String"");
        foreach (var ptr in ptrs)
        {
            Console.Write(ptr());
        }
    }
    static void Main()
    {
        delegate*<object> ptr1 = &GetObject;
        delegate*<string> ptr2 = &GetString;
        Print(new[] { ptr1, ptr2 });
    }
}", expectedOutput: "Object12");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       34 (0x22)
  .maxstack  4
  .locals init (delegate*<object> V_0, //ptr1
                delegate*<string> V_1) //ptr2
  IL_0000:  ldftn      ""object C.GetObject()""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""string C.GetString()""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.2
  IL_000f:  newarr     ""delegate*<object>""
  IL_0014:  dup
  IL_0015:  ldc.i4.0
  IL_0016:  ldloc.0
  IL_0017:  stelem.i
  IL_0018:  dup
  IL_0019:  ldc.i4.1
  IL_001a:  ldloc.1
  IL_001b:  stelem.i
  IL_001c:  call       ""void C.Print(delegate*<object>[])""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void BestTypeForConditional_ParameterVariance()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Print(object o) => Console.Write(o + ""Object"");
    static void Print(string s) => Console.Write(s + ""String"");
    static void M(bool b)
    {
        delegate*<object, void> ptr1 = &Print;
        delegate*<string, void> ptr2 = &Print;
        var ptr3 = b ? ptr1 : ptr2;
        ptr3(""1"");
        ptr3 = b ? ptr2 : ptr1;
        ptr3(""2"");
    }
    static void Main() => M(true);
}", expectedOutput: "1Object2String");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (delegate*<object, void> V_0, //ptr1
                delegate*<string, void> V_1, //ptr2
                delegate*<string, void> V_2)
  IL_0000:  ldftn      ""void C.Print(object)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void C.Print(string)""
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldloc.1
  IL_0012:  br.s       IL_0015
  IL_0014:  ldloc.0
  IL_0015:  stloc.2
  IL_0016:  ldstr      ""1""
  IL_001b:  ldloc.2
  IL_001c:  calli      ""delegate*<string, void>""
  IL_0021:  ldarg.0
  IL_0022:  brtrue.s   IL_0027
  IL_0024:  ldloc.0
  IL_0025:  br.s       IL_0028
  IL_0027:  ldloc.1
  IL_0028:  stloc.2
  IL_0029:  ldstr      ""2""
  IL_002e:  ldloc.2
  IL_002f:  calli      ""delegate*<string, void>""
  IL_0034:  ret
}
");
        }

        [Fact]
        public void BestTypeForConditional_ReturnVariance()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static object GetObject() => 1.ToString();
    static string GetString() => 2.ToString();
    static void Print(delegate*<object> ptr)
    {
        Console.Write(ptr());
        Console.Write(""Object"");
    }
    static void Print(delegate*<string> ptr)
    {
        Console.Write(ptr());
        Console.Write(""String"");
    }
    static void M(bool b)
    {
        delegate*<object> ptr1 = &GetObject;
        delegate*<string> ptr2 = &GetString;
        Print(b ? ptr1 : ptr2);
        Print(b ? ptr2 : ptr1);
    }
    static void Main() => M(true);
}", expectedOutput: "1Object2Object");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (delegate*<object> V_0, //ptr1
                delegate*<string> V_1) //ptr2
  IL_0000:  ldftn      ""object C.GetObject()""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""string C.GetString()""
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldloc.1
  IL_0012:  br.s       IL_0015
  IL_0014:  ldloc.0
  IL_0015:  call       ""void C.Print(delegate*<object>)""
  IL_001a:  ldarg.0
  IL_001b:  brtrue.s   IL_0020
  IL_001d:  ldloc.0
  IL_001e:  br.s       IL_0021
  IL_0020:  ldloc.1
  IL_0021:  call       ""void C.Print(delegate*<object>)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void BestTypeForConditional_NestedParameterVariance()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Print(object o)
    {
        Console.Write(o);
    }
    static void PrintObject(delegate*<object, void> ptr, string o)
    {
        ptr(o);
    }
    static void PrintString(delegate*<string, void> ptr, string s)
    {
        ptr(s);
    }
    static void Invoke(delegate*<delegate*<object, void>, string, void> ptr, string s)
    {
        Console.Write(""Object"");
        delegate*<object, void> print = &Print;
        ptr(print, s);
    }
    static void Invoke(delegate*<delegate*<string, void>, string, void> ptr, string s)
    {
        Console.Write(""String"");
        delegate*<string, void> print = &Print;
        ptr(print, s);
    }
    static void M(bool b)
    {
        delegate*<delegate*<object, void>, string, void> printObject = &PrintObject;
        delegate*<delegate*<string, void>, string, void> printString = &PrintString;
        Invoke(b ? printObject : printString, ""1"");
    }
    static void Main() => M(true);
}", expectedOutput: "Object1");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (delegate*<delegate*<object, void>, string, void> V_0, //printObject
                delegate*<delegate*<string, void>, string, void> V_1) //printString
  IL_0000:  ldftn      ""void C.PrintObject(delegate*<object, void>, string)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void C.PrintString(delegate*<string, void>, string)""
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldloc.1
  IL_0012:  br.s       IL_0015
  IL_0014:  ldloc.0
  IL_0015:  ldstr      ""1""
  IL_001a:  call       ""void C.Invoke(delegate*<delegate*<object, void>, string, void>, string)""
  IL_001f:  ret
}
");
        }

        [Fact]
        public void BestTypeForConditional_NestedParameterRef()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Print(ref object o1, object o2)
    {
        o1 = o2;
    }
    static void PrintObject(delegate*<ref object, object, void> ptr, ref object o, object arg)
    {
        ptr(ref o, arg);
    }
    static void PrintString(delegate*<ref object, string, void> ptr, ref object o, string arg)
    {
        ptr(ref o, arg);
    }
    static void Invoke(delegate*<delegate*<ref object, object, void>, ref object, string, void> ptr, ref object s, object arg)
    {
        Console.Write(""Object"");
        delegate*<ref object, object, void> print = &Print;
        ptr(print, ref s, arg.ToString());
    }
    static void Invoke(delegate*<delegate*<ref object, string, void>, ref object, string, void> ptr, ref object s, string arg)
    {
        Console.Write(""String"");
        delegate*<ref object, string, void> print = &Print;
        ptr(print, ref s, arg);
    }
    static void M(bool b)
    {
        delegate*<delegate*<ref object, object, void>, ref object, string, void> printObject1 = &PrintObject;
        delegate*<delegate*<ref object, string, void>, ref object, string, void> printObject2 = &PrintString;
        object o = null;
        Invoke(b ? printObject1 : printObject2, ref o, ""1"");
        Console.Write(o);
    }
    static void Main() => M(true);
}", expectedOutput: "Object1");

            verifier.VerifyIL("C.M", expectedIL: @"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (delegate*<delegate*<ref object, object, void>, ref object, string, void> V_0, //printObject1
                delegate*<delegate*<ref object, string, void>, ref object, string, void> V_1, //printObject2
                object V_2) //o
  IL_0000:  ldftn      ""void C.PrintObject(delegate*<ref object, object, void>, ref object, object)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void C.PrintString(delegate*<ref object, string, void>, ref object, string)""
  IL_000d:  stloc.1
  IL_000e:  ldnull
  IL_000f:  stloc.2
  IL_0010:  ldarg.0
  IL_0011:  brtrue.s   IL_0016
  IL_0013:  ldloc.1
  IL_0014:  br.s       IL_0017
  IL_0016:  ldloc.0
  IL_0017:  ldloca.s   V_2
  IL_0019:  ldstr      ""1""
  IL_001e:  call       ""void C.Invoke(delegate*<delegate*<ref object, object, void>, ref object, string, void>, ref object, object)""
  IL_0023:  ldloc.2
  IL_0024:  call       ""void System.Console.Write(object)""
  IL_0029:  ret
}
");
        }

        [Fact]
        public void DefaultOfFunctionPointerType()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void Main()
    {
        delegate*<void> ptr = default;
        Console.Write(ptr is null);
    }
}", expectedOutput: "True");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  ldnull
  IL_0003:  ceq
  IL_0005:  call       ""void System.Console.Write(bool)""
  IL_000a:  ret
}
");

            var comp = (CSharpCompilation)verifier.Compilation;
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            FunctionPointerUtilities.VerifyFunctionPointerSemanticInfo(model,
                syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<LiteralExpressionSyntax>()
                    .Where(l => l.IsKind(SyntaxKind.DefaultLiteralExpression))
                    .Single(),
                expectedSyntax: "default",
                expectedType: "delegate*<System.Void>",
                expectedSymbol: null,
                expectedSymbolCandidates: null);
        }

        [Fact]
        public void ParamsArrayOfFunctionPointers()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe class C
{
    static void Params(params delegate*<void>[] funcs)
    {
        foreach (var f in funcs)
        {
            f();
        }
    }

    static void Main()
    {
        Params();
    }
}", expectedOutput: "");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""delegate*<void>""
  IL_0006:  call       ""void C.Params(params delegate*<void>[])""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void StackallocOfFunctionPointers()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
static unsafe class C
{
    static int Getter(int i) => i;
    static void Print(delegate*<int, int>* p)
    {
        for (int i = 0; i < 3; i++)
            Console.Write(p[i](i));
    }

    static void Main()
    {
        delegate*<int, int>* p = stackalloc delegate*<int, int>[] { &Getter, &Getter, &Getter };
        Print(p);
    }
}
", expectedOutput: "012");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       58 (0x3a)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  conv.u
  IL_0002:  sizeof     ""delegate*<int, int>""
  IL_0008:  mul.ovf.un
  IL_0009:  localloc
  IL_000b:  dup
  IL_000c:  ldftn      ""int C.Getter(int)""
  IL_0012:  stind.i
  IL_0013:  dup
  IL_0014:  sizeof     ""delegate*<int, int>""
  IL_001a:  add
  IL_001b:  ldftn      ""int C.Getter(int)""
  IL_0021:  stind.i
  IL_0022:  dup
  IL_0023:  ldc.i4.2
  IL_0024:  conv.i
  IL_0025:  sizeof     ""delegate*<int, int>""
  IL_002b:  mul
  IL_002c:  add
  IL_002d:  ldftn      ""int C.Getter(int)""
  IL_0033:  stind.i
  IL_0034:  call       ""void C.Print(delegate*<int, int>*)""
  IL_0039:  ret
}
");
        }

        [Fact]
        public void FunctionPointerCannotBeUsedAsSpanArgument()
        {
            var comp = CreateCompilationWithSpan(@"
using System;
static unsafe class C
{
    static void Main()
    {
        Span<delegate*<int, int>> p = stackalloc delegate*<int, int>[1];
    }
}
", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (7,14): error CS0306: The type 'delegate*<int, int>' may not be used as a type argument
                //         Span<delegate*<int, int>> p = stackalloc delegate*<int, int>[1];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<int, int>").WithArguments("delegate*<int, int>").WithLocation(7, 14)
            );
        }

        [Fact]
        public void RecursivelyUsedTypeInFunctionPointer()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
namespace Interop
{
    public unsafe struct PROPVARIANT
    {
        public CAPROPVARIANT ca;
    }
    public unsafe struct CAPROPVARIANT
    {
        public uint cElems;
        public delegate*<PROPVARIANT> pElems;
        public delegate*<PROPVARIANT> pElemsProp { get; }
    }
}");
        }

        [Fact]
        public void VolatileFunctionPointerField()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static volatile delegate*<void> ptr;
    static void Print() => Console.Write(1);
    static void Main()
    {
        ptr = &Print;
        ptr();
    }
}", expectedOutput: "1");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldftn      ""void C.Print()""
  IL_0006:  volatile.
  IL_0008:  stsfld     ""delegate*<void> C.ptr""
  IL_000d:  volatile.
  IL_000f:  ldsfld     ""delegate*<void> C.ptr""
  IL_0014:  calli      ""delegate*<void>""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void SupportedBinaryOperators()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static (bool, bool, bool, bool, bool, bool) DoCompare(delegate*<void> func_1a, delegate*<string, void> func_1b)
    {
        return (func_1a == func_1b,
                func_1a != func_1b,
                func_1a > func_1b,
                func_1a >= func_1b,
                func_1a < func_1b,
                func_1a <= func_1b);
    }

    static void M(delegate*<void> func_1a, delegate*<string, void> func_1b, delegate*<int> func_2, int* int_1, int* int_2)
    {
        var compareResults = DoCompare(func_1a, func_1b);
        Console.WriteLine(""func_1a == func_1b: "" + compareResults.Item1);
        Console.WriteLine(""func_1a != func_1b: "" + compareResults.Item2);
        Console.WriteLine(""func_1a > func_1b: "" + compareResults.Item3);
        Console.WriteLine(""func_1a >= func_1b: "" + compareResults.Item4);
        Console.WriteLine(""func_1a < func_1b: "" + compareResults.Item5);
        Console.WriteLine(""func_1a <= func_1b: "" + compareResults.Item6);
        Console.WriteLine(""func_1a == func_2: "" + (func_1a == func_2));
        Console.WriteLine(""func_1a != func_2: "" + (func_1a != func_2));
        Console.WriteLine(""func_1a > func_2: "" + (func_1a > func_2));
        Console.WriteLine(""func_1a >= func_2: "" + (func_1a >= func_2));
        Console.WriteLine(""func_1a < func_2: "" + (func_1a < func_2));
        Console.WriteLine(""func_1a <= func_2: "" + (func_1a <= func_2));
        Console.WriteLine(""func_1a == int_1: "" + (func_1a == int_1));
        Console.WriteLine(""func_1a != int_1: "" + (func_1a != int_1));
        Console.WriteLine(""func_1a > int_1: "" + (func_1a > int_1));
        Console.WriteLine(""func_1a >= int_1: "" + (func_1a >= int_1));
        Console.WriteLine(""func_1a < int_1: "" + (func_1a < int_1));
        Console.WriteLine(""func_1a <= int_1: "" + (func_1a <= int_1));
        Console.WriteLine(""func_1a == int_2: "" + (func_1a == int_2));
        Console.WriteLine(""func_1a != int_2: "" + (func_1a != int_2));
        Console.WriteLine(""func_1a > int_2: "" + (func_1a > int_2));
        Console.WriteLine(""func_1a >= int_2: "" + (func_1a >= int_2));
        Console.WriteLine(""func_1a < int_2: "" + (func_1a < int_2));
        Console.WriteLine(""func_1a <= int_2: "" + (func_1a <= int_2));
    }

    static void Main()
    {
        delegate*<void> func_1a = (delegate*<void>)1;
        delegate*<string, void> func_1b = (delegate*<string, void>)1;
        delegate*<int> func_2 = (delegate*<int>)2;
        int* int_1 = (int*)1;
        int* int_2 = (int*)2;
        M(func_1a, func_1b, func_2, int_1, int_2);
    }
}", expectedOutput: @"
func_1a == func_1b: True
func_1a != func_1b: False
func_1a > func_1b: False
func_1a >= func_1b: True
func_1a < func_1b: False
func_1a <= func_1b: True
func_1a == func_2: False
func_1a != func_2: True
func_1a > func_2: False
func_1a >= func_2: False
func_1a < func_2: True
func_1a <= func_2: True
func_1a == int_1: True
func_1a != int_1: False
func_1a > int_1: False
func_1a >= int_1: True
func_1a < int_1: False
func_1a <= int_1: True
func_1a == int_2: False
func_1a != int_2: True
func_1a > int_2: False
func_1a >= int_2: False
func_1a < int_2: True
func_1a <= int_2: True");

            verifier.VerifyIL("C.DoCompare", expectedIL: @"
{
  // Code size       39 (0x27)
  .maxstack  7
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ceq
  IL_0004:  ldarg.0
  IL_0005:  ldarg.1
  IL_0006:  ceq
  IL_0008:  ldc.i4.0
  IL_0009:  ceq
  IL_000b:  ldarg.0
  IL_000c:  ldarg.1
  IL_000d:  cgt.un
  IL_000f:  ldarg.0
  IL_0010:  ldarg.1
  IL_0011:  clt.un
  IL_0013:  ldc.i4.0
  IL_0014:  ceq
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  clt.un
  IL_001a:  ldarg.0
  IL_001b:  ldarg.1
  IL_001c:  cgt.un
  IL_001e:  ldc.i4.0
  IL_001f:  ceq
  IL_0021:  newobj     ""System.ValueTuple<bool, bool, bool, bool, bool, bool>..ctor(bool, bool, bool, bool, bool, bool)""
  IL_0026:  ret
}
");
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        public void UnsupportedBinaryOps(string op)
        {
            bool isLogical = op == "&&" || op == "||";
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    static void M(delegate*<void> ptr1, delegate*<void> ptr2, int* ptr3)
    {{
        _ = ptr1 {op} ptr2;
        _ = ptr1 {op} ptr3;
        _ = ptr1 {op} 1;
        {(isLogical ? "" : $@"ptr1 {op}= ptr2;
        ptr1 {op}= ptr3;
        ptr1 {op}= 1;")}
    }}
}}");

            var expectedDiagnostics = ArrayBuilder<DiagnosticDescription>.GetInstance();
            expectedDiagnostics.AddRange(
                // (6,13): error CS0019: Operator 'op' cannot be applied to operands of type 'delegate*<void>' and 'delegate*<void>'
                //         _ = ptr1 op ptr2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op} ptr2").WithArguments(op, "delegate*<void>", "delegate*<void>").WithLocation(6, 13),
                // (7,13): error CS0019: Operator 'op' cannot be applied to operands of type 'delegate*<void>' and 'int*'
                //         _ = ptr1 op ptr3;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op} ptr3").WithArguments(op, "delegate*<void>", "int*").WithLocation(7, 13),
                // (8,13): error CS0019: Operator 'op' cannot be applied to operands of type 'delegate*<void>' and 'int'
                //         _ = ptr1 op 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op} 1").WithArguments(op, "delegate*<void>", "int").WithLocation(8, 13));

            if (!isLogical)
            {
                expectedDiagnostics.AddRange(
                    // (9,9): error CS0019: Operator 'op=' cannot be applied to operands of type 'delegate*<void>' and 'delegate*<void>'
                    //         ptr1 op= ptr2;
                    Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op}= ptr2").WithArguments($"{op}=", "delegate*<void>", "delegate*<void>").WithLocation(9, 9),
                    // (10,9): error CS0019: Operator 'op=' cannot be applied to operands of type 'delegate*<void>' and 'int*'
                    //         ptr1 op= ptr3;
                    Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op}= ptr3").WithArguments($"{op}=", "delegate*<void>", "int*").WithLocation(10, 9),
                    // (11,9): error CS0019: Operator 'op=' cannot be applied to operands of type 'delegate*<void>' and 'int'
                    //         ptr1 op= 1;
                    Diagnostic(ErrorCode.ERR_BadBinaryOps, $"ptr1 {op}= 1").WithArguments($"{op}=", "delegate*<void>", "int").WithLocation(11, 9));
            }

            comp.VerifyDiagnostics(expectedDiagnostics.ToArrayAndFree());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("++")]
        [InlineData("-")]
        [InlineData("--")]
        [InlineData("!")]
        [InlineData("~")]
        public void UnsupportedPrefixUnaryOps(string op)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    public static void M(delegate*<void> ptr)
    {{
        _ = {op}ptr;
    }}
}}");

            comp.VerifyDiagnostics(
                // (6,13): error CS0023: Operator 'op' cannot be applied to operand of type 'delegate*<void>'
                //         _ = {op}ptr;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, $"{op}ptr").WithArguments(op, "delegate*<void>").WithLocation(6, 13)
            );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnsupportedPostfixUnaryOps(string op)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C
{{
    public static void M(delegate*<void> ptr)
    {{
        _ = ptr{op};
    }}
}}");

            comp.VerifyDiagnostics(
                // (6,13): error CS0023: Operator 'op' cannot be applied to operand of type 'delegate*<void>'
                //         _ = ptr{op};
                Diagnostic(ErrorCode.ERR_BadUnaryOp, $"ptr{op}").WithArguments(op, "delegate*<void>").WithLocation(6, 13)
            );
        }

        [Fact]
        public void FunctionPointerReturnTypeConstrainedCallVirtIfRef()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe class C
{
    void M1() {}
    void M<T>(delegate*<ref T> ptr1, delegate*<T> ptr2) where T : C
    {
        ptr1().M1();
        ptr2().M1();
    }
}");

            verifier.VerifyIL(@"C.M<T>(delegate*<T>, delegate*<T>)", @"
{
  // Code size       34 (0x22)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  calli      ""delegate*<ref T>""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""void C.M1()""
  IL_0011:  ldarg.2
  IL_0012:  calli      ""delegate*<T>""
  IL_0017:  box        ""T""
  IL_001c:  callvirt   ""void C.M1()""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void NullableAnnotationsInMetadata()
        {
            var source = @"
public unsafe class C
{
    public delegate*<string, object, C> F1;
    public delegate*<string?, object, C> F2;
    public delegate*<string, object?, C> F3;
    public delegate*<string, object, C?> F4;
    public delegate*<string?, object?, C?> F5;
    public delegate*<delegate*<string, int*>, delegate*<string?>, delegate*<void*, string>> F6;
}";

            var comp = CreateCompilationWithFunctionPointers(source, options: WithNonNullTypesTrue(TestOptions.UnsafeReleaseDll));
            comp.VerifyDiagnostics();

            verifySymbolNullabilities(comp.GetTypeByMetadataName("C")!);

            CompileAndVerify(comp, symbolValidator: symbolValidator);

            static void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 1, 1, 1})", getAttribute("F1"));
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 1, 2, 1})", getAttribute("F2"));
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 1, 1, 2})", getAttribute("F3"));
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 2, 1, 1})", getAttribute("F4"));
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 2, 2, 2})", getAttribute("F5"));
                Assert.Equal("System.Runtime.CompilerServices.NullableAttribute({0, 0, 1, 0, 0, 0, 1, 0, 2})", getAttribute("F6"));

                verifySymbolNullabilities(c);

                string getAttribute(string fieldName) => c.GetField(fieldName).GetAttributes().Single().ToString()!;
            }

            static void verifySymbolNullabilities(NamedTypeSymbol c)
            {
                assertExpected("delegate*<System.String!, System.Object!, C!>", "F1");
                assertExpected("delegate*<System.String?, System.Object!, C!>", "F2");
                assertExpected("delegate*<System.String!, System.Object?, C!>", "F3");
                assertExpected("delegate*<System.String!, System.Object!, C?>", "F4");
                assertExpected("delegate*<System.String?, System.Object?, C?>", "F5");
                assertExpected("delegate*<delegate*<System.String!, System.Int32*>, delegate*<System.String?>, delegate*<System.Void*, System.String!>>", "F6");

                void assertExpected(string expectedType, string fieldName)
                {
                    var type = (FunctionPointerTypeSymbol)c.GetField(fieldName).Type;
                    CommonVerifyFunctionPointer(type);
                    Assert.Equal(expectedType, type.ToTestDisplayString(includeNonNullable: true));
                }
            }
        }

        [Theory]
        [InlineData("delegate*<Z, Z, (Z a, Z b)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", ""b""})")]
        [InlineData("delegate*<(Z a, Z b), Z, Z>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", ""b""})")]
        [InlineData("delegate*<Z, (Z a, Z b)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", ""b""})")]
        [InlineData("delegate*<(Z c, Z d), (Z e, Z f), (Z a, Z b)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", ""b"", ""c"", ""d"", ""e"", ""f""})")]
        [InlineData("delegate*<(Z, Z), (Z, Z), (Z a, Z b)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", ""b"", null, null, null, null})")]
        [InlineData("delegate*<(Z, Z), (Z, Z), (Z a, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""a"", null, null, null, null, null})")]
        [InlineData("delegate*<(Z, Z), (Z, Z), (Z, Z b)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, ""b"", null, null, null, null})")]
        [InlineData("delegate*<(Z c, Z d), (Z, Z), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, ""c"", ""d"", null, null})")]
        [InlineData("delegate*<(Z c, Z), (Z, Z), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, ""c"", null, null, null})")]
        [InlineData("delegate*<(Z, Z d), (Z, Z), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, null, ""d"", null, null})")]
        [InlineData("delegate*<(Z, Z), (Z e, Z f), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, null, null, ""e"", ""f""})")]
        [InlineData("delegate*<(Z, Z), (Z e, Z), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, null, null, ""e"", null})")]
        [InlineData("delegate*<(Z, Z), (Z, Z f), (Z, Z)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({null, null, null, null, null, ""f""})")]
        [InlineData("delegate*<(Z a, (Z b, Z c) d), (Z e, Z f)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""e"", ""f"", ""a"", ""d"", ""b"", ""c""})")]
        [InlineData("delegate*<(Z a, Z b), ((Z c, Z d) e, Z f)>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""e"", ""f"", ""c"", ""d"", ""a"", ""b""})")]
        [InlineData("delegate*<delegate*<(Z a, Z b), Z>, delegate*<Z, (Z d, Z e)>>", @"System.Runtime.CompilerServices.TupleElementNamesAttribute({""d"", ""e"", ""a"", ""b""})")]
        [InlineData("delegate*<(Z, Z), (Z, Z), (Z, Z)>", null)]
        public void TupleNamesInMetadata(string type, string? expectedAttribute)
        {
            var comp = CompileAndVerifyFunctionPointers($@"
#pragma warning disable CS0649 // Unassigned field
unsafe class Z
{{
    public {type} F;
}}
", symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetTypeMember("Z");

                var field = c.GetField("F");
                if (expectedAttribute == null)
                {
                    Assert.Empty(field.GetAttributes());
                }
                else
                {
                    Assert.Equal(expectedAttribute, field.GetAttributes().Single().ToString());
                }

                CommonVerifyFunctionPointer((FunctionPointerTypeSymbol)field.Type);
                Assert.Equal(type, field.Type.ToTestDisplayString());
            }
        }

        [Fact]
        public void DynamicTypeAttributeInMetadata()
        {
            var comp = CompileAndVerifyFunctionPointers(@"
#pragma warning disable CS0649 // Unassigned field
unsafe class C
{
    public delegate*<dynamic, dynamic, dynamic> F1;

    public delegate*<object, object, dynamic> F2;
    public delegate*<dynamic, object, object> F3;
    public delegate*<object, dynamic, object> F4;

    public delegate*<object, object, object> F5;

    public delegate*<object, object, ref dynamic> F6;
    public delegate*<ref dynamic, object, object> F7;
    public delegate*<object, ref dynamic, object> F8;

    public delegate*<ref object, ref object, dynamic> F9;
    public delegate*<dynamic, ref object, ref object> F10;
    public delegate*<ref object, dynamic, ref object> F11;

    public delegate*<object, ref readonly dynamic> F12;
    public delegate*<in dynamic, object> F13;

    public delegate*<out dynamic, object> F14;

    public D<delegate*<dynamic>[], dynamic> F15;

    public delegate*<A<object>.B<dynamic>> F16;
}
class D<T, U> { }
class A<T>
{
    public class B<U> {}
}
", symbolValidator: symbolValidator);

            void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetTypeMember("C");

                assertField("F1", "System.Runtime.CompilerServices.DynamicAttribute({false, true, true, true})", "delegate*<dynamic, dynamic, dynamic>");

                assertField("F2", "System.Runtime.CompilerServices.DynamicAttribute({false, true, false, false})", "delegate*<System.Object, System.Object, dynamic>");
                assertField("F3", "System.Runtime.CompilerServices.DynamicAttribute({false, false, true, false})", "delegate*<dynamic, System.Object, System.Object>");
                assertField("F4", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true})", "delegate*<System.Object, dynamic, System.Object>");

                assertField("F5", null, "delegate*<System.Object, System.Object, System.Object>");

                assertField("F6", "System.Runtime.CompilerServices.DynamicAttribute({false, false, true, false, false})", "delegate*<System.Object, System.Object, ref dynamic>");
                assertField("F7", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true, false})", "delegate*<ref dynamic, System.Object, System.Object>");
                assertField("F8", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, false, true})", "delegate*<System.Object, ref dynamic, System.Object>");

                assertField("F9", "System.Runtime.CompilerServices.DynamicAttribute({false, true, false, false, false, false})", "delegate*<ref System.Object, ref System.Object, dynamic>");
                assertField("F10", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true, false, false})", "delegate*<dynamic, ref System.Object, ref System.Object>");
                assertField("F11", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, false, false, true})", "delegate*<ref System.Object, dynamic, ref System.Object>");

                assertField("F12", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true, false})", "delegate*<System.Object, ref readonly modreq(System.Runtime.InteropServices.InAttribute) dynamic>");
                assertField("F13", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, false, true})", "delegate*<in modreq(System.Runtime.InteropServices.InAttribute) dynamic, System.Object>");

                assertField("F14", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, false, true})", "delegate*<out modreq(System.Runtime.InteropServices.OutAttribute) dynamic, System.Object>");

                // https://github.com/dotnet/roslyn/issues/44160 tracks fixing this. We're not encoding dynamic correctly for function pointers as type parameters
                assertField("F15", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true})", "D<delegate*<System.Object>[], System.Object>");

                assertField("F16", "System.Runtime.CompilerServices.DynamicAttribute({false, false, false, true})", "delegate*<A<System.Object>.B<dynamic>>");

                void assertField(string field, string? expectedAttribute, string expectedType)
                {
                    var f = c.GetField(field);
                    if (expectedAttribute is null)
                    {
                        Assert.Empty(f.GetAttributes());
                    }
                    else
                    {
                        Assert.Equal(expectedAttribute, f.GetAttributes().Single().ToString());
                    }

                    if (f.Type is FunctionPointerTypeSymbol ptrType)
                    {
                        CommonVerifyFunctionPointer(ptrType);
                    }
                    Assert.Equal(expectedType, f.Type.ToTestDisplayString());
                }
            }
        }

        [Fact]
        public void DynamicOverriddenWithCustomModifiers()
        {
            var il = @"
.class public A
{
  .method public hidebysig newslot virtual
    instance void M(method class [mscorlib]System.Object modopt([mscorlib]System.Object) & modopt([mscorlib]System.Object) modreq([mscorlib]System.Runtime.InteropServices.InAttribute) *(class [mscorlib]System.Object modopt([mscorlib]System.Object)) a) managed
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 08 00 00 00 00 00 00 00 00 01 00 01 00 00 ) 
    ret
  }
  .method public hidebysig specialname rtspecialname 
    instance void .ctor () cil managed 
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var source = @"
unsafe class B : A
{
    public override void M(delegate*<dynamic, ref readonly dynamic> a) {}
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, il, symbolValidator: symbolValidator);

            static void symbolValidator(ModuleSymbol module)
            {
                var b = module.GlobalNamespace.GetTypeMember("B");

                var m = b.GetMethod("M");
                var param = m.Parameters.Single();
                Assert.Equal("System.Runtime.CompilerServices.DynamicAttribute({false, false, false, false, false, true, false, true})", param.GetAttributes().Single().ToString());

                CommonVerifyFunctionPointer((FunctionPointerTypeSymbol)param.Type);
                Assert.Equal("delegate*<dynamic modopt(System.Object), ref readonly modreq(System.Runtime.InteropServices.InAttribute) modopt(System.Object) dynamic modopt(System.Object)>", param.Type.ToTestDisplayString());
            }
        }

        [Fact]
        public void BadDynamicAttributes()
        {
            var il = @"
.class public A
{
  .method public hidebysig static void TooManyFlags(method class [mscorlib]System.Object *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 04 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void TooFewFlags_MissingParam(method class [mscorlib]System.Object *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    ret
  }
  .method public hidebysig static void TooFewFlags_MissingReturn(method class [mscorlib]System.Object *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) 
    ret
  }
  .method public hidebysig static void PtrTypeIsTrue(method class [mscorlib]System.Object *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void NonObjectIsTrue(method class [mscorlib]System.String *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void RefIsTrue_Return(method class [mscorlib]System.Object& *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 04 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void RefIsTrue_Param(method class [mscorlib]System.Object *(class [mscorlib]System.Object&) a)
  {
    .param [1]
    //{false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 04 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void ModIsTrue_Return(method class [mscorlib]System.Object modopt([mscorlib]System.Object) *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 04 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void ModIsTrue_Param(method class [mscorlib]System.Object *(class [mscorlib]System.Object modopt([mscorlib]System.Object)) a)
  {
    .param [1]
    //{false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 04 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void ModIsTrue_RefReturn(method class [mscorlib]System.Object & modopt([mscorlib]System.Object) *(class [mscorlib]System.Object) a)
  {
    .param [1]
    //{false, false, true, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 00 00 01 01 01 00 00 ) 
    ret
  }
  .method public hidebysig static void ModIsTrue_RefParam(method class [mscorlib]System.Object *(class [mscorlib]System.Object & modopt([mscorlib]System.Object)) a)
  {
    .param [1]
    //{false, true, false, true, true}
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 05 00 00 00 00 01 00 01 01 00 00 ) 
    ret
  }
}
";

            var comp = CreateCompilationWithFunctionPointersAndIl("", il);

            var a = comp.GetTypeByMetadataName("A");

            assert("TooManyFlags", "delegate*<System.Object, System.Object>");
            assert("TooFewFlags_MissingParam", "delegate*<System.Object, System.Object>");
            assert("TooFewFlags_MissingReturn", "delegate*<System.Object, System.Object>");
            assert("PtrTypeIsTrue", "delegate*<System.Object, System.Object>");
            assert("NonObjectIsTrue", "delegate*<System.Object, System.String>");
            assert("RefIsTrue_Return", "delegate*<System.Object, ref System.Object>");
            assert("RefIsTrue_Param", "delegate*<ref System.Object, System.Object>");
            assert("ModIsTrue_Return", "delegate*<System.Object, System.Object modopt(System.Object)>");
            assert("ModIsTrue_Param", "delegate*<System.Object modopt(System.Object), System.Object>");
            assert("ModIsTrue_RefReturn", "delegate*<System.Object, ref modopt(System.Object) System.Object>");
            assert("ModIsTrue_RefParam", "delegate*<ref modopt(System.Object) System.Object, System.Object>");

            void assert(string methodName, string expectedType)
            {
                var method = a.GetMethod(methodName);
                var param = method.Parameters.Single();
                CommonVerifyFunctionPointer((FunctionPointerTypeSymbol)param.Type);
                Assert.Equal(expectedType, param.Type.ToTestDisplayString());
            }
        }

        [Fact]
        public void BetterFunctionMember_BreakTiesByCustomModifierCount_TypeMods()
        {
            var il = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void RetModifiers (method void modopt([mscorlib]System.Object) modopt([mscorlib]System.Object) *()) cil managed 
    {
        ldstr ""M""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void RetModifiers (method void modopt([mscorlib]System.Object) *()) cil managed 
    {
        ldstr ""L""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void ParamModifiers (method void *(int32 modopt([mscorlib]System.Object) modopt([mscorlib]System.Object))) cil managed 
    {
        ldstr ""M""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void ParamModifiers (method void *(int32) modopt([mscorlib]System.Object)) cil managed 
    {
        ldstr ""L""
        call void [mscorlib]System.Console::Write(string)
        ret
    }
}";

            var source = @"
unsafe class C
{
    static void Main()
    {
        delegate*<void> ptr1 = null;
        Program.RetModifiers(ptr1);
        delegate*<int, void> ptr2 = null;
        Program.ParamModifiers(ptr2);
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, il, expectedOutput: "LL");
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (delegate*<void> V_0, //ptr1
                delegate*<int, void> V_1) //ptr2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void Program.RetModifiers(delegate*<void>)""
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  call       ""void Program.ParamModifiers(delegate*<int, void>)""
  IL_0012:  ret
}
            ");
        }

        [Fact]
        public void BetterFunctionMember_BreakTiesByCustomModifierCount_Ref()
        {
            var il = @"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void RetModifiers (method int32 & modopt([mscorlib]System.Object) modopt([mscorlib]System.Object) *()) cil managed 
    {
        ldstr ""M""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void RetModifiers (method int32 & modopt([mscorlib]System.Object) *()) cil managed 
    {
        ldstr ""L""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void ParamModifiers (method void *(int32 & modopt([mscorlib]System.Object) modopt([mscorlib]System.Object))) cil managed 
    {
        ldstr ""M""
        call void [mscorlib]System.Console::Write(string)
        ret
    }

    .method public hidebysig static 
        void ParamModifiers (method void *(int32 & modopt([mscorlib]System.Object))) cil managed 
    {
        ldstr ""L""
        call void [mscorlib]System.Console::Write(string)
        ret
    }
}
";

            var source = @"
unsafe class C
{
    static void Main()
    {
        delegate*<ref int> ptr1 = null;
        Program.RetModifiers(ptr1);
        delegate*<ref int, void> ptr2 = null;
        Program.ParamModifiers(ptr2);
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, il, expectedOutput: "LL");
            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (delegate*<ref int> V_0, //ptr1
                delegate*<ref int, void> V_1) //ptr2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void Program.RetModifiers(delegate*<ref int>)""
  IL_0009:  ldc.i4.0
  IL_000a:  conv.u
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  call       ""void Program.ParamModifiers(delegate*<ref int, void>)""
  IL_0012:  ret
}
");
        }

        [Fact]
        public void Overloading_ReturnTypes()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(delegate*<void> ptr) => ptr();
    static void M(delegate*<object> ptr) => Console.WriteLine(ptr());
    static void M(delegate*<string> ptr) => Console.WriteLine(ptr());
    static void Ptr_Void() => Console.WriteLine(""Void"");
    static object Ptr_Obj() => ""Object"";
    static string Ptr_Str() => ""String"";

    static void Main()
    {
        M(&Ptr_Void);
        M(&Ptr_Obj);
        M(&Ptr_Str);
    }
}
", expectedOutput: @"
Void
Object
String");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  1
  IL_0000:  ldftn      ""void C.Ptr_Void()""
  IL_0006:  call       ""void C.M(delegate*<void>)""
  IL_000b:  ldftn      ""object C.Ptr_Obj()""
  IL_0011:  call       ""void C.M(delegate*<object>)""
  IL_0016:  ldftn      ""string C.Ptr_Str()""
  IL_001c:  call       ""void C.M(delegate*<string>)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void Overloading_ValidReturnRefness()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static object field = ""R"";
    static void M(delegate*<object> ptr) => Console.WriteLine(ptr());
    static void M(delegate*<ref object> ptr)
    {
        Console.Write(field);
        ref var local = ref ptr();
        local = ""ef"";
        Console.WriteLine(field);
    }
    static object Ptr_NonRef() => ""NonRef"";
    static ref object Ptr_Ref() => ref field;

    static void Main()
    {
        M(&Ptr_NonRef);
        M(&Ptr_Ref);
    }
}
", expectedOutput: @"
NonRef
Ref");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldftn      ""object C.Ptr_NonRef()""
  IL_0006:  call       ""void C.M(delegate*<object>)""
  IL_000b:  ldftn      ""ref object C.Ptr_Ref()""
  IL_0011:  call       ""void C.M(delegate*<ref object>)""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void Overloading_InvalidReturnRefness()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C<T>
{
    static void M1(delegate*<ref readonly object> ptr) => throw null;
    static void M1(delegate*<ref object> ptr) => throw null;

    static void M2(C<delegate*<ref readonly object>[]> c) => throw null;
    static void M2(C<delegate*<ref object>[]> c) => throw null;
}
");

            comp.VerifyDiagnostics(
                // (5,17): error CS0111: Type 'C<T>' already defines a member called 'M1' with the same parameter types
                //     static void M1(delegate*<ref object> ptr) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C<T>").WithLocation(5, 17),
                // (8,17): error CS0111: Type 'C<T>' already defines a member called 'M2' with the same parameter types
                //     static void M2(C<delegate*<ref object>[]> c) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C<T>").WithLocation(8, 17)
            );
        }

        [Fact]
        public void Overloading_ReturnNoBetterFunction()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
interface I1 {}
interface I2 {}
unsafe class C : I1, I2
{
    static void M1(delegate*<I1> ptr) => throw null;
    static void M1(delegate*<I2> ptr) => throw null;

    static void M2(delegate*<C> ptr)
    {
        M1(ptr);
    }
}
");

            comp.VerifyDiagnostics(
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(delegate*<I1>)' and 'C.M1(delegate*<I2>)'
                //         M1(ptr);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(delegate*<I1>)", "C.M1(delegate*<I2>)").WithLocation(11, 9)
            );
        }

        [Fact]
        public void Overloading_ParameterTypes()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(delegate*<object, void> ptr) => ptr(""Object"");
    static void M(delegate*<string, void> ptr) => ptr(""String"");

    static void Main()
    {
        delegate*<object, void> ptr1 = &Console.WriteLine;
        delegate*<string, void> ptr2 = &Console.WriteLine;
        M(ptr1);
        M(ptr2);
    }
}
", expectedOutput: @"
Object
String");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (delegate*<string, void> V_0) //ptr2
  IL_0000:  ldftn      ""void System.Console.WriteLine(object)""
  IL_0006:  ldftn      ""void System.Console.WriteLine(string)""
  IL_000c:  stloc.0
  IL_000d:  call       ""void C.M(delegate*<object, void>)""
  IL_0012:  ldloc.0
  IL_0013:  call       ""void C.M(delegate*<string, void>)""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void Overloading_ValidParameterRefness()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static object field = ""R"";
    static void M(delegate*<ref object, void> ptr)
    {
        Console.Write(field);
        ref var local = ref field;
        ptr(ref local);
        Console.WriteLine(field);
    }
    static void M(delegate*<object, void> ptr) => ptr(""NonRef"");

    static void Ptr(ref object param) => param = ""ef"";

    static void Main()
    {
        M(&Console.WriteLine);
        M(&Ptr);
    }
}
", expectedOutput: @"
NonRef
Ref");

            verifier.VerifyIL("C.Main", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldftn      ""void System.Console.WriteLine(object)""
  IL_0006:  call       ""void C.M(delegate*<object, void>)""
  IL_000b:  ldftn      ""void C.Ptr(ref object)""
  IL_0011:  call       ""void C.M(delegate*<ref object, void>)""
  IL_0016:  ret
}
");
        }

        [Theory]
        [InlineData("ref", "out")]
        [InlineData("ref", "in")]
        [InlineData("out", "in")]
        public void Overloading_InvalidParameterRefness(string refKind1, string refKind2)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe class C<T>
{{
    static void M1(delegate*<{refKind1} object, void> ptr) => throw null;
    static void M1(delegate*<{refKind2} object, void> ptr) => throw null;

    static void M2(C<delegate*<{refKind1} object, void>[]> c) => throw null;
    static void M2(C<delegate*<{refKind2} object, void>[]> c) => throw null;
}}
");

            comp.VerifyDiagnostics(
                // (5,17): error CS0111: Type 'C<T>' already defines a member called 'M1' with the same parameter types
                //     static void M1(delegate*<{refKind2} object, void> ptr) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C<T>").WithLocation(5, 17),
                // (8,17): error CS0111: Type 'C<T>' already defines a member called 'M2' with the same parameter types
                //     static void M2(C<delegate*<{refKind2} object, void>[]> c) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C<T>").WithLocation(8, 17)
            );
        }

        [Fact]
        public void Overloading_ParameterTypesNoBetterFunctionMember()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
interface I1 {}
interface I2 {}
unsafe class C : I1, I2
{
    static void M1(delegate*<delegate*<I1, void>, void> ptr) => throw null;
    static void M1(delegate*<delegate*<I2, void>, void> ptr) => throw null;

    static void M2(delegate*<delegate*<C, void>, void> ptr)
    {
        M1(ptr);
    }
}
");

            comp.VerifyDiagnostics(
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M1(delegate*<delegate*<I1, void>, void>)' and 'C.M1(delegate*<delegate*<I2, void>, void>)'
                //         M1(ptr);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("C.M1(delegate*<delegate*<I1, void>, void>)", "C.M1(delegate*<delegate*<I2, void>, void>)").WithLocation(11, 9)
            );
        }

        [Fact]
        public void Override_CallingConventionMustMatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<void> ptr) {}
    protected virtual delegate*<void> M2() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate* cdecl<void> ptr) {}
    protected override delegate* cdecl<void> M2() => throw null;
}");

            comp.VerifyDiagnostics(
                // (9,29): error CS0115: 'Derived.M1(delegate*<void>)': no suitable method found to override
                //     protected override void M1(delegate* cdecl<void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived.M1(delegate*<void>)").WithLocation(9, 29),
                // (10,46): error CS0508: 'Derived.M2()': return type must be 'delegate*<void>' to match overridden member 'Base.M2()'
                //     protected override delegate* cdecl<void> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", "delegate*<void>").WithLocation(10, 46)
            );
        }

        [Theory]
        [InlineData("", "ref ")]
        [InlineData("", "out ")]
        [InlineData("", "in ")]
        [InlineData("ref ", "")]
        [InlineData("ref ", "out ")]
        [InlineData("ref ", "in ")]
        [InlineData("out ", "")]
        [InlineData("out ", "ref ")]
        [InlineData("out ", "in ")]
        [InlineData("in ", "")]
        [InlineData("in ", "ref ")]
        [InlineData("in ", "out ")]
        public void Override_RefnessMustMatch_Parameters(string refKind1, string refKind2)
        {
            var comp = CreateCompilationWithFunctionPointers(@$"
unsafe class Base
{{
    protected virtual void M1(delegate*<{refKind1}string, void> ptr) {{}}
    protected virtual delegate*<{refKind1}string, void> M2() => throw null;
}}
unsafe class Derived : Base
{{
    protected override void M1(delegate*<{refKind2}string, void> ptr) {{}}
    protected override delegate*<{refKind2}string, void> M2() => throw null;
}}");

            comp.VerifyDiagnostics(
                // (9,29): error CS0115: 'Derived.M1(delegate*<{refKind2} string, void>)': no suitable method found to override
                //     protected override void M1(delegate*<{refKind2} string, void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments($"Derived.M1(delegate*<{refKind2}string, void>)").WithLocation(9, 29),
                // (10,49): error CS0508: 'Derived.M2()': return type must be 'delegate*<{refKind1} string, void>' to match overridden member 'Base.M2()'
                //     protected override delegate*<{refKind2} string, void> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", $"delegate*<{refKind1}string, void>").WithLocation(10, 48 + refKind2.Length)
            );
        }

        [Theory]
        [InlineData(" ", "ref ")]
        [InlineData(" ", "ref readonly ")]
        [InlineData("ref ", " ")]
        [InlineData("ref ", "ref readonly ")]
        [InlineData("ref readonly ", " ")]
        [InlineData("ref readonly ", "ref ")]
        public void Override_RefnessMustMatch_Returns(string refKind1, string refKind2)
        {
            var comp = CreateCompilationWithFunctionPointers(@$"
unsafe class Base
{{
    protected virtual void M1(delegate*<{refKind1}string> ptr) {{}}
    protected virtual delegate*<{refKind1}string> M2() => throw null;
}}
unsafe class Derived : Base
{{
    protected override void M1(delegate*<{refKind2}string> ptr) {{}}
    protected override delegate*<{refKind2}string> M2() => throw null;
}}");

            comp.VerifyDiagnostics(
                // (9,29): error CS0115: 'Derived.M1(delegate*<{refKind2} string>)': no suitable method found to override
                //     protected override void M1(delegate*<{refKind2} string> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments($"Derived.M1(delegate*<string>)").WithLocation(9, 29),
                // (10,49): error CS0508: 'Derived.M2()': return type must be 'delegate*<{refKind1} string>' to match overridden member 'Base.M2()'
                //     protected override delegate*<{refKind2} string> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", $"delegate*<string>").WithLocation(10, 42 + refKind2.Length)
            );
        }

        [Fact]
        public void Override_ParameterTypesMustMatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<object, void> ptr) {{}}
    protected virtual delegate*<object, void> M2() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<string, void> ptr) {{}}
    protected override delegate*<string, void> M2() => throw null;
}");

            comp.VerifyDiagnostics(
                // (9,29): error CS0115: 'Derived.M1(delegate*<string, void>)': no suitable method found to override
                //     protected override void M1(delegate*<string, void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived.M1(delegate*<string, void>)").WithLocation(9, 29),
                // (10,48): error CS0508: 'Derived.M2()': return type must be 'delegate*<object, void>' to match overridden member 'Base.M2()'
                //     protected override delegate*<string, void> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", "delegate*<object, void>").WithLocation(10, 48)
            );
        }

        [Fact]
        public void Override_ReturnTypesMustMatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<object> ptr) {{}}
    protected virtual delegate*<object> M2() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<string> ptr) {{}}
    protected override delegate*<string> M2() => throw null;
}");

            comp.VerifyDiagnostics(
                // (9,29): error CS0115: 'Derived.M1(delegate*<string>)': no suitable method found to override
                //     protected override void M1(delegate*<string> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived.M1(delegate*<string>)").WithLocation(9, 29),
                // (10,42): error CS0508: 'Derived.M2()': return type must be 'delegate*<object>' to match overridden member 'Base.M2()'
                //     protected override delegate*<string> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", "delegate*<object>").WithLocation(10, 42)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44358")]
        public void Override_NintIntPtrDifferences()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<nint> ptr) {}
    protected virtual delegate*<nint> M2() => throw null;
    protected virtual void M3(delegate*<nint, void> ptr) {}
    protected virtual delegate*<nint, void> M4() => throw null;
    protected virtual void M5(delegate*<System.IntPtr> ptr) {}
    protected virtual delegate*<System.IntPtr> M6() => throw null;
    protected virtual void M7(delegate*<System.IntPtr, void> ptr) {}
    protected virtual delegate*<System.IntPtr, void> M8() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<System.IntPtr> ptr) {}
    protected override delegate*<System.IntPtr> M2() => throw null;
    protected override void M3(delegate*<System.IntPtr, void> ptr) {}
    protected override delegate*<System.IntPtr, void> M4() => throw null;
    protected override void M5(delegate*<nint> ptr) {}
    protected override delegate*<nint> M6() => throw null;
    protected override void M7(delegate*<nint, void> ptr) {}
    protected override delegate*<nint, void> M8() => throw null;
}");

            comp.VerifyDiagnostics(
            );

            assertMethods(comp.SourceModule);
            CompileAndVerify(comp, symbolValidator: assertMethods);

            static void assertMethods(ModuleSymbol module)
            {
                var derived = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                assertMethod(derived, "M1", "void Derived.M1(delegate*<System.IntPtr> ptr)");
                assertMethod(derived, "M2", "delegate*<System.IntPtr> Derived.M2()");
                assertMethod(derived, "M3", "void Derived.M3(delegate*<System.IntPtr, System.Void> ptr)");
                assertMethod(derived, "M4", "delegate*<System.IntPtr, System.Void> Derived.M4()");
                assertMethod(derived, "M5", "void Derived.M5(delegate*<nint> ptr)");
                assertMethod(derived, "M6", "delegate*<nint> Derived.M6()");
                assertMethod(derived, "M7", "void Derived.M7(delegate*<nint, System.Void> ptr)");
                assertMethod(derived, "M8", "delegate*<nint, System.Void> Derived.M8()");
            }

            static void assertMethod(NamedTypeSymbol derived, string methodName, string expectedSignature)
            {
                var m = derived.GetMember<MethodSymbol>(methodName);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString(includeNonNullable: true));
            }
        }

        [Fact]
        public void Override_ObjectDynamicDifferences()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<dynamic> ptr) {}
    protected virtual delegate*<dynamic> M2() => throw null;
    protected virtual void M3(delegate*<dynamic, void> ptr) {}
    protected virtual delegate*<dynamic, void> M4() => throw null;
    protected virtual void M5(delegate*<object> ptr) {}
    protected virtual delegate*<object> M6() => throw null;
    protected virtual void M7(delegate*<object, void> ptr) {}
    protected virtual delegate*<object, void> M8() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<object> ptr) {}
    protected override delegate*<object> M2() => throw null;
    protected override void M3(delegate*<object, void> ptr) {}
    protected override delegate*<object, void> M4() => throw null;
    protected override void M5(delegate*<dynamic> ptr) {}
    protected override delegate*<dynamic> M6() => throw null;
    protected override void M7(delegate*<dynamic, void> ptr) {}
    protected override delegate*<dynamic, void> M8() => throw null;
}");

            comp.VerifyDiagnostics(
            );

            assertMethods(comp.SourceModule);
            CompileAndVerify(comp, symbolValidator: assertMethods);

            static void assertMethods(ModuleSymbol module)
            {
                var derived = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                assertMethod(derived, "M1", "void Derived.M1(delegate*<System.Object> ptr)");
                assertMethod(derived, "M2", "delegate*<System.Object> Derived.M2()");
                assertMethod(derived, "M3", "void Derived.M3(delegate*<System.Object, System.Void> ptr)");
                assertMethod(derived, "M4", "delegate*<System.Object, System.Void> Derived.M4()");
                assertMethod(derived, "M5", "void Derived.M5(delegate*<dynamic> ptr)");
                assertMethod(derived, "M6", "delegate*<dynamic> Derived.M6()");
                assertMethod(derived, "M7", "void Derived.M7(delegate*<dynamic, System.Void> ptr)");
                assertMethod(derived, "M8", "delegate*<dynamic, System.Void> Derived.M8()");
            }

            static void assertMethod(NamedTypeSymbol derived, string methodName, string expectedSignature)
            {
                var m = derived.GetMember<MethodSymbol>(methodName);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString(includeNonNullable: true));
            }
        }

        [Fact]
        public void Override_TupleNameChanges()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Base
{
    protected virtual void M1(delegate*<(int, string)> ptr) {}
    protected virtual delegate*<(int, string)> M2() => throw null;
    protected virtual void M3(delegate*<(int, string), void> ptr) {}
    protected virtual delegate*<(int, string), void> M4() => throw null;
    protected virtual void M5(delegate*<(int i, string s)> ptr) {}
    protected virtual delegate*<(int i, string s)> M6() => throw null;
    protected virtual void M7(delegate*<(int i, string s), void> ptr) {}
    protected virtual delegate*<(int i, string s), void> M8() => throw null;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<(int i, string s)> ptr) {}
    protected override delegate*<(int i, string s)> M2() => throw null;
    protected override void M3(delegate*<(int i, string s), void> ptr) {}
    protected override delegate*<(int i, string s), void> M4() => throw null;
    protected override void M5(delegate*<(int, string)> ptr) {}
    protected override delegate*<(int, string)> M6() => throw null;
    protected override void M7(delegate*<(int, string), void> ptr) {}
    protected override delegate*<(int, string), void> M8() => throw null;
}");

            comp.VerifyDiagnostics(
                // (15,29): error CS8139: 'Derived.M1(delegate*<(int i, string s)>)': cannot change tuple element names when overriding inherited member 'Base.M1(delegate*<(int, string)>)'
                //     protected override void M1(delegate*<(int i, string s)> ptr) {}
                Diagnostic(ErrorCode.ERR_CantChangeTupleNamesOnOverride, "M1").WithArguments("Derived.M1(delegate*<(int i, string s)>)", "Base.M1(delegate*<(int, string)>)").WithLocation(15, 29),
                // (16,53): error CS8139: 'Derived.M2()': cannot change tuple element names when overriding inherited member 'Base.M2()'
                //     protected override delegate*<(int i, string s)> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeTupleNamesOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()").WithLocation(16, 53),
                // (17,29): error CS8139: 'Derived.M3(delegate*<(int i, string s), void>)': cannot change tuple element names when overriding inherited member 'Base.M3(delegate*<(int, string), void>)'
                //     protected override void M3(delegate*<(int i, string s), void> ptr) {}
                Diagnostic(ErrorCode.ERR_CantChangeTupleNamesOnOverride, "M3").WithArguments("Derived.M3(delegate*<(int i, string s), void>)", "Base.M3(delegate*<(int, string), void>)").WithLocation(17, 29),
                // (18,59): error CS8139: 'Derived.M4()': cannot change tuple element names when overriding inherited member 'Base.M4()'
                //     protected override delegate*<(int i, string s), void> M4() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeTupleNamesOnOverride, "M4").WithArguments("Derived.M4()", "Base.M4()").WithLocation(18, 59)
            );

            assertMethod("M1", "void Derived.M1(delegate*<(System.Int32 i, System.String s)> ptr)");
            assertMethod("M2", "delegate*<(System.Int32 i, System.String s)> Derived.M2()");
            assertMethod("M3", "void Derived.M3(delegate*<(System.Int32 i, System.String s), System.Void> ptr)");
            assertMethod("M4", "delegate*<(System.Int32 i, System.String s), System.Void> Derived.M4()");
            assertMethod("M5", "void Derived.M5(delegate*<(System.Int32, System.String)> ptr)");
            assertMethod("M6", "delegate*<(System.Int32, System.String)> Derived.M6()");
            assertMethod("M7", "void Derived.M7(delegate*<(System.Int32, System.String), System.Void> ptr)");
            assertMethod("M8", "delegate*<(System.Int32, System.String), System.Void> Derived.M8()");

            void assertMethod(string methodName, string expectedSignature)
            {
                var m = comp.GetMember<MethodSymbol>($"Derived.{methodName}");
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString());
            }
        }

        [Fact]
        public void Override_NullabilityChanges_NoRefs()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
unsafe class Base
{
    protected virtual void M1(delegate*<string?> ptr) {}
    protected virtual delegate*<string?> M2() => throw null!;
    protected virtual void M3(delegate*<string?, void> ptr) {}
    protected virtual delegate*<string?, void> M4() => throw null!;
    protected virtual void M5(delegate*<string> ptr) {}
    protected virtual delegate*<string> M6() => throw null!;
    protected virtual void M7(delegate*<string, void> ptr) {}
    protected virtual delegate*<string, void> M8() => throw null!;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<string> ptr) {}
    protected override delegate*<string> M2() => throw null!;
    protected override void M3(delegate*<string, void> ptr) {}
    protected override delegate*<string, void> M4() => throw null!;
    protected override void M5(delegate*<string?> ptr) {}
    protected override delegate*<string?> M6() => throw null!;
    protected override void M7(delegate*<string?, void> ptr) {}
    protected override delegate*<string?, void> M8() => throw null!;
}");

            comp.VerifyDiagnostics(
                // (16,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M1(delegate*<string> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("ptr").WithLocation(16, 29),
                // (19,48): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<string, void> M4() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M4").WithLocation(19, 48),
                // (21,43): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<string?> M6() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M6").WithLocation(21, 43),
                // (22,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M7(delegate*<string?, void> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M7").WithArguments("ptr").WithLocation(22, 29)
            );

            assertMethods(comp.SourceModule);
            CompileAndVerify(comp, symbolValidator: assertMethods);

            static void assertMethods(ModuleSymbol module)
            {
                var derived = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                assertMethod(derived, "M1", "void Derived.M1(delegate*<System.String!> ptr)");
                assertMethod(derived, "M2", "delegate*<System.String!> Derived.M2()");
                assertMethod(derived, "M3", "void Derived.M3(delegate*<System.String!, System.Void> ptr)");
                assertMethod(derived, "M4", "delegate*<System.String!, System.Void> Derived.M4()");
                assertMethod(derived, "M5", "void Derived.M5(delegate*<System.String?> ptr)");
                assertMethod(derived, "M6", "delegate*<System.String?> Derived.M6()");
                assertMethod(derived, "M7", "void Derived.M7(delegate*<System.String?, System.Void> ptr)");
                assertMethod(derived, "M8", "delegate*<System.String?, System.Void> Derived.M8()");
            }

            static void assertMethod(NamedTypeSymbol derived, string methodName, string expectedSignature)
            {
                var m = derived.GetMember<MethodSymbol>(methodName);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString(includeNonNullable: true));
            }
        }

        [Fact]
        public void Override_NullabilityChanges_RefsInParameterReturnTypes()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
unsafe class Base
{
    protected virtual void M1(delegate*<ref string?> ptr) {}
    protected virtual delegate*<ref string?> M2() => throw null!;
    protected virtual void M3(delegate*<ref string?, void> ptr) {}
    protected virtual delegate*<ref string?, void> M4() => throw null!;
    protected virtual void M5(delegate*<ref string> ptr) {}
    protected virtual delegate*<ref string> M6() => throw null!;
    protected virtual void M7(delegate*<ref string, void> ptr) {}
    protected virtual delegate*<ref string, void> M8() => throw null!;
}
unsafe class Derived : Base
{
    protected override void M1(delegate*<ref string> ptr) {}
    protected override delegate*<ref string> M2() => throw null!;
    protected override void M3(delegate*<ref string, void> ptr) {}
    protected override delegate*<ref string, void> M4() => throw null!;
    protected override void M5(delegate*<ref string?> ptr) {}
    protected override delegate*<ref string?> M6() => throw null!;
    protected override void M7(delegate*<ref string?, void> ptr) {}
    protected override delegate*<ref string?, void> M8() => throw null!;
}");

            comp.VerifyDiagnostics(
                // (16,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M1(delegate*<ref string> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("ptr").WithLocation(16, 29),
                // (17,46): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<ref string> M2() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M2").WithLocation(17, 46),
                // (18,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M3(delegate*<ref string, void> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M3").WithArguments("ptr").WithLocation(18, 29),
                // (19,52): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<ref string, void> M4() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M4").WithLocation(19, 52),
                // (20,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M5(delegate*<ref string?> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M5").WithArguments("ptr").WithLocation(20, 29),
                // (21,47): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<ref string?> M6() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M6").WithLocation(21, 47),
                // (22,29): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     protected override void M7(delegate*<ref string?, void> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M7").WithArguments("ptr").WithLocation(22, 29),
                // (23,53): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     protected override delegate*<ref string?, void> M8() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M8").WithLocation(23, 53)
            );

            assertMethods(comp.SourceModule);
            CompileAndVerify(comp, symbolValidator: assertMethods);

            static void assertMethods(ModuleSymbol module)
            {
                var derived = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                assertMethod(derived, "M1", "void Derived.M1(delegate*<ref System.String!> ptr)");
                assertMethod(derived, "M2", "delegate*<ref System.String!> Derived.M2()");
                assertMethod(derived, "M3", "void Derived.M3(delegate*<ref System.String!, System.Void> ptr)");
                assertMethod(derived, "M4", "delegate*<ref System.String!, System.Void> Derived.M4()");
                assertMethod(derived, "M5", "void Derived.M5(delegate*<ref System.String?> ptr)");
                assertMethod(derived, "M6", "delegate*<ref System.String?> Derived.M6()");
                assertMethod(derived, "M7", "void Derived.M7(delegate*<ref System.String?, System.Void> ptr)");
                assertMethod(derived, "M8", "delegate*<ref System.String?, System.Void> Derived.M8()");
            }

            static void assertMethod(NamedTypeSymbol derived, string methodName, string expectedSignature)
            {
                var m = derived.GetMember<MethodSymbol>(methodName);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString(includeNonNullable: true));
            }
        }

        [Fact]
        public void Override_NullabilityChanges_PointerByRef()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
public unsafe class Base
{
    public virtual void M1(ref delegate*<string?> ptr) {}
    public virtual ref delegate*<string?> M2() => throw null!;
    public virtual void M3(ref delegate*<string?, void> ptr) {}
    public virtual ref delegate*<string?, void> M4() => throw null!;
    public virtual void M5(ref delegate*<string> ptr) {}
    public virtual ref delegate*<string> M6() => throw null!;
    public virtual void M7(ref delegate*<string, void> ptr) {}
    public virtual ref delegate*<string, void> M8() => throw null!;
}
public unsafe class Derived : Base
{
    public override void M1(ref delegate*<string> ptr) {}
    public override ref delegate*<string> M2() => throw null!;
    public override void M3(ref delegate*<string, void> ptr) {}
    public override ref delegate*<string, void> M4() => throw null!;
    public override void M5(ref delegate*<string?> ptr) {}
    public override ref delegate*<string?> M6() => throw null!;
    public override void M7(ref delegate*<string?, void> ptr) {}
    public override ref delegate*<string?, void> M8() => throw null!;
}");

            comp.VerifyDiagnostics(
                // (16,26): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     public override void M1(ref delegate*<string> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("ptr").WithLocation(16, 26),
                // (17,43): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override ref delegate*<string> M2() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M2").WithLocation(17, 43),
                // (18,26): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     public override void M3(ref delegate*<string, void> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M3").WithArguments("ptr").WithLocation(18, 26),
                // (19,49): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override ref delegate*<string, void> M4() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M4").WithLocation(19, 49),
                // (20,26): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     public override void M5(ref delegate*<string?> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M5").WithArguments("ptr").WithLocation(20, 26),
                // (21,44): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override ref delegate*<string?> M6() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M6").WithLocation(21, 44),
                // (22,26): warning CS8610: Nullability of reference types in type of parameter 'ptr' doesn't match overridden member.
                //     public override void M7(ref delegate*<string?, void> ptr) {}
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride, "M7").WithArguments("ptr").WithLocation(22, 26),
                // (23,50): warning CS8609: Nullability of reference types in return type doesn't match overridden member.
                //     public override ref delegate*<string?, void> M8() => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride, "M8").WithLocation(23, 50)
            );

            assertMethods(comp.SourceModule);
            CompileAndVerify(comp, symbolValidator: assertMethods);

            static void assertMethods(ModuleSymbol module)
            {
                var derived = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                assertMethod(derived, "M1", "void Derived.M1(ref delegate*<System.String!> ptr)");
                assertMethod(derived, "M2", "ref delegate*<System.String!> Derived.M2()");
                assertMethod(derived, "M3", "void Derived.M3(ref delegate*<System.String!, System.Void> ptr)");
                assertMethod(derived, "M4", "ref delegate*<System.String!, System.Void> Derived.M4()");
                assertMethod(derived, "M5", "void Derived.M5(ref delegate*<System.String?> ptr)");
                assertMethod(derived, "M6", "ref delegate*<System.String?> Derived.M6()");
                assertMethod(derived, "M7", "void Derived.M7(ref delegate*<System.String?, System.Void> ptr)");
                assertMethod(derived, "M8", "ref delegate*<System.String?, System.Void> Derived.M8()");

            }

            static void assertMethod(NamedTypeSymbol derived, string methodName, string expectedSignature)
            {
                var m = derived.GetMember<MethodSymbol>(methodName);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, m.ToTestDisplayString(includeNonNullable: true));
            }
        }

        [Fact]
        public void Override_SingleDimensionArraySizesInMetadata()
        {
            var il = @"
.class public auto ansi abstract beforefieldinit Base
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig newslot abstract virtual
        void M1 (method void *(int32[0...]) param) cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        method void *(int32[0...]) M2 () cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        void M3 (method int32[0...] *() param) cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        method int32[0...] *() M4 () cil managed 
    {
    }

    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}";

            var source = @"
unsafe class Derived : Base
{
    public override void M1(delegate*<int[], void> param) => throw null;
    public override delegate*<int[], void> M2() => throw null;
    public override void M3(delegate*<int[]> param) => throw null;
    public override delegate*<int[]> M4() => throw null;
}";

            var comp = CreateCompilationWithFunctionPointersAndIl(source, il);
            comp.VerifyDiagnostics(
                // (2,14): error CS0534: 'Derived' does not implement inherited abstract member 'Base.M1(delegate*<int[*], void>)'
                // unsafe class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.M1(delegate*<int[*], void>)").WithLocation(2, 14),
                // (2,14): error CS0534: 'Derived' does not implement inherited abstract member 'Base.M3(delegate*<int[*]>)'
                // unsafe class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.M3(delegate*<int[*]>)").WithLocation(2, 14),
                // (4,26): error CS0115: 'Derived.M1(delegate*<int[], void>)': no suitable method found to override
                //     public override void M1(delegate*<int[], void> param) => throw null;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived.M1(delegate*<int[], void>)").WithLocation(4, 26),
                // (5,44): error CS0508: 'Derived.M2()': return type must be 'delegate*<int[*], void>' to match overridden member 'Base.M2()'
                //     public override delegate*<int[], void> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", "delegate*<int[*], void>").WithLocation(5, 44),
                // (6,26): error CS0115: 'Derived.M3(delegate*<int[]>)': no suitable method found to override
                //     public override void M3(delegate*<int[]> param) => throw null;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M3").WithArguments("Derived.M3(delegate*<int[]>)").WithLocation(6, 26),
                // (7,38): error CS0508: 'Derived.M4()': return type must be 'delegate*<int[*]>' to match overridden member 'Base.M4()'
                //     public override delegate*<int[]> M4() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M4").WithArguments("Derived.M4()", "Base.M4()", "delegate*<int[*]>").WithLocation(7, 38)
            );
        }

        [Fact]
        public void Override_ArraySizesInMetadata()
        {
            var il = @"
.class public auto ansi abstract beforefieldinit Base
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig newslot abstract virtual
        void M1 (method void *(int32[5...5,2...4]) param) cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        method void *(int32[5...5,2...4]) M2 () cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        void M3 (method int32[5...5,2...4] *() param) cil managed 
    {
    }

    .method public hidebysig newslot abstract virtual
        method int32[5...5,2...4] *() M4 () cil managed 
    {
    }

    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}";

            var source = @"
using System;
unsafe class Derived : Base
{
    private static void MultiDimensionParamFunc(int[,] param) { }
    private static int[,] MultiDimensionReturnFunc() => null;

    public override void M1(delegate*<int[,], void> param)
    {
        Console.WriteLine(""Multi-dimension array param as param"");
        param(null);
    }

    public override delegate*<int[,], void> M2()
    {
        Console.WriteLine(""Multi-dimension array param as return"");
        return &MultiDimensionParamFunc;
    }

    public override void M3(delegate*<int[,]> param)
    {
        Console.WriteLine(""Multi-dimension array return as param"");
        _ = param();
    }

    public override delegate*<int[,]> M4()
    {
        Console.WriteLine(""Multi-dimension array return as return"");
        return &MultiDimensionReturnFunc;
    }

    public static void Main()
    {
        var d = new Derived();
        d.M1(&MultiDimensionParamFunc);
        var ptr1 = d.M2();
        ptr1(null);
        d.M3(&MultiDimensionReturnFunc);
        var ptr2 = d.M4();
        _ = ptr2();
    }
}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, il, expectedOutput: @"
Multi-dimension array param as param
Multi-dimension array param as return
Multi-dimension array return as param
Multi-dimension array return as return
");

            verifier.VerifyIL("Derived.M1", expectedIL: @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (delegate*<int[,], void> V_0)
  IL_0000:  ldstr      ""Multi-dimension array param as param""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  stloc.0
  IL_000c:  ldnull
  IL_000d:  ldloc.0
  IL_000e:  calli      ""delegate*<int[,], void>""
  IL_0013:  ret
}
");

            verifier.VerifyIL("Derived.M2", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldstr      ""Multi-dimension array param as return""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldftn      ""void Derived.MultiDimensionParamFunc(int[,])""
  IL_0010:  ret
}
");

            verifier.VerifyIL("Derived.M3", expectedIL: @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""Multi-dimension array return as param""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  calli      ""delegate*<int[,]>""
  IL_0010:  pop
  IL_0011:  ret
}
");

            verifier.VerifyIL("Derived.M4", expectedIL: @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldstr      ""Multi-dimension array return as return""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldftn      ""int[,] Derived.MultiDimensionReturnFunc()""
  IL_0010:  ret
}
");

            verifier.VerifyIL("Derived.Main", expectedIL: @"
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (delegate*<int[,], void> V_0)
  IL_0000:  newobj     ""Derived..ctor()""
  IL_0005:  dup
  IL_0006:  ldftn      ""void Derived.MultiDimensionParamFunc(int[,])""
  IL_000c:  callvirt   ""void Base.M1(delegate*<int[,], void>)""
  IL_0011:  dup
  IL_0012:  callvirt   ""delegate*<int[,], void> Base.M2()""
  IL_0017:  stloc.0
  IL_0018:  ldnull
  IL_0019:  ldloc.0
  IL_001a:  calli      ""delegate*<int[,], void>""
  IL_001f:  dup
  IL_0020:  ldftn      ""int[,] Derived.MultiDimensionReturnFunc()""
  IL_0026:  callvirt   ""void Base.M3(delegate*<int[,]>)""
  IL_002b:  callvirt   ""delegate*<int[,]> Base.M4()""
  IL_0030:  calli      ""delegate*<int[,]>""
  IL_0035:  pop
  IL_0036:  ret
}
");

            var comp = (CSharpCompilation)verifier.Compilation;

            var m1 = comp.GetMember<MethodSymbol>("Derived.M1");
            var m2 = comp.GetMember<MethodSymbol>("Derived.M2");
            var m3 = comp.GetMember<MethodSymbol>("Derived.M3");
            var m4 = comp.GetMember<MethodSymbol>("Derived.M4");

            var funcPtr = (FunctionPointerTypeSymbol)m1.Parameters.Single().Type;
            CommonVerifyFunctionPointer(funcPtr);
            verifyArray(funcPtr.Signature.Parameters.Single().Type);

            funcPtr = (FunctionPointerTypeSymbol)m2.ReturnType;
            CommonVerifyFunctionPointer(funcPtr);
            verifyArray(funcPtr.Signature.Parameters.Single().Type);

            funcPtr = (FunctionPointerTypeSymbol)m3.Parameters.Single().Type;
            CommonVerifyFunctionPointer(funcPtr);
            verifyArray(funcPtr.Signature.ReturnType);

            funcPtr = (FunctionPointerTypeSymbol)m4.ReturnType;
            CommonVerifyFunctionPointer(funcPtr);
            verifyArray(funcPtr.Signature.ReturnType);

            static void verifyArray(TypeSymbol type)
            {
                var array = (ArrayTypeSymbol)type;
                Assert.False(array.IsSZArray);
                Assert.Equal(2, array.Rank);
                Assert.Equal(5, array.LowerBounds[0]);
                Assert.Equal(1, array.Sizes[0]);
                Assert.Equal(2, array.LowerBounds[1]);
                Assert.Equal(3, array.Sizes[1]);
            }
        }

        [Fact]
        public void NullableUsageWarnings()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#nullable enable
unsafe public class C
{
    static void M1(delegate*<string, string?, string?> ptr1)
    {
        _ = ptr1(null, null);
        _ = ptr1("""", null).ToString();
        delegate*<string?, string?, string?> ptr2 = ptr1;
        delegate*<string, string?, string> ptr3 = ptr1;
    }

    static void M2(delegate*<ref string, ref string> ptr1)
    {
        string? str1 = null;
        ptr1(ref str1);
        string str2 = """";
        ref string? str3 = ref ptr1(ref str2);
        delegate*<ref string?, ref string> ptr2 = ptr1;
        delegate*<ref string, ref string?> ptr3 = ptr1;
    }
}
");

            comp.VerifyDiagnostics(
                // (7,18): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         _ = ptr1(null, null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 18),
                // (8,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = ptr1("", null).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, @"ptr1("""", null)").WithLocation(8, 13),
                // (9,53): warning CS8619: Nullability of reference types in value of type 'delegate*<string, string?, string?>' doesn't match target type 'delegate*<string?, string?, string?>'.
                //         delegate*<string?, string?, string?> ptr2 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<string, string?, string?>", "delegate*<string?, string?, string?>").WithLocation(9, 53),
                // (10,51): warning CS8619: Nullability of reference types in value of type 'delegate*<string, string?, string?>' doesn't match target type 'delegate*<string, string?, string>'.
                //         delegate*<string, string?, string> ptr3 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<string, string?, string?>", "delegate*<string, string?, string>").WithLocation(10, 51),
                // (16,18): warning CS8601: Possible null reference assignment.
                //         ptr1(ref str1);
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "str1").WithLocation(16, 18),
                // (18,32): warning CS8619: Nullability of reference types in value of type 'string' doesn't match target type 'string?'.
                //         ref string? str3 = ref ptr1(ref str2);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1(ref str2)").WithArguments("string", "string?").WithLocation(18, 32),
                // (19,51): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string, string>' doesn't match target type 'delegate*<ref string?, string>'.
                //         delegate*<ref string?, ref string> ptr2 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<ref string, string>", "delegate*<ref string?, string>").WithLocation(19, 51),
                // (20,51): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string, string>' doesn't match target type 'delegate*<ref string, string?>'.
                //         delegate*<ref string, ref string?> ptr3 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<ref string, string>", "delegate*<ref string, string?>").WithLocation(20, 51)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SpanInArgumentAndReturn()
        {
            var comp = CompileAndVerifyFunctionPointers(@"
using System;
public class C
{
    static char[] chars = new[] { '1', '2', '3', '4' };

    static Span<char> ChopSpan(Span<char> span) => span[..^1];

    public static unsafe void Main()
    {
        delegate*<Span<char>, Span<char>> ptr = &ChopSpan;
        Console.Write(new string(ptr(chars)));
    }
}
", targetFramework: TargetFramework.NetCoreApp30, expectedOutput: "123");

            comp.VerifyIL("C.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (delegate*<System.Span<char>, System.Span<char>> V_0)
  IL_0000:  ldftn      ""System.Span<char> C.ChopSpan(System.Span<char>)""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""char[] C.chars""
  IL_000c:  call       ""System.Span<char> System.Span<char>.op_Implicit(char[])""
  IL_0011:  ldloc.0
  IL_0012:  calli      ""delegate*<System.Span<char>, System.Span<char>>""
  IL_0017:  call       ""System.ReadOnlySpan<char> System.Span<char>.op_Implicit(System.Span<char>)""
  IL_001c:  newobj     ""string..ctor(System.ReadOnlySpan<char>)""
  IL_0021:  call       ""void System.Console.Write(string)""
  IL_0026:  ret
}
");
        }

        private static readonly Guid s_guid = new Guid("97F4DBD4-F6D1-4FAD-91B3-1001F92068E5");
        private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

        private static void DefineInvalidSignatureAttributeIL(MetadataBuilder metadata, BlobBuilder ilBuilder, SignatureHeader headerToUseForM)
        {
            metadata.AddModule(
                0,
                metadata.GetOrAddString("ConsoleApplication.exe"),
                metadata.GetOrAddGuid(s_guid),
                default(GuidHandle),
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString("ConsoleApplication"),
                version: new Version(1, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: metadata.GetOrAddBlob(new byte[0]),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            var systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            var systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().String());

            var consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => { });

            var parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            var objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            // Signature for M() with an _invalid_ SignatureAttribute
            var mSignature = new BlobBuilder();
            var mBlobBuilder = new BlobEncoder(mSignature);
            mBlobBuilder.Builder.WriteByte(headerToUseForM.RawValue);
            var mParameterEncoder = new MethodSignatureEncoder(mBlobBuilder.Builder, hasVarArgs: false);
            mParameterEncoder.Parameters(parameterCount: 0, returnType => returnType.Void(), parameters => { });

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            InstructionEncoder il;

            //
            // Program::.ctor
            //
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            //
            // Program::M
            //
            il = new InstructionEncoder(codeBuilder);

            // ldstr "M"
            il.LoadString(metadata.GetOrAddUserString("M"));

            // call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int mBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            var mMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString("M"),
                metadata.GetOrAddBlob(mSignature),
                mBodyOffset,
                parameterList: default(ParameterHandle));

            var ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                parameterList: default(ParameterHandle));

            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mMethodDef);

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString("ConsoleApplication"),
                metadata.GetOrAddString("Program"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mMethodDef);
        }

        private static void WritePEImage(
            Stream peStream,
            MetadataBuilder metadataBuilder,
            BlobBuilder ilBuilder)
        {
            var peHeaderBuilder = new PEHeaderBuilder(imageCharacteristics: Characteristics.Dll);

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => s_contentId);

            var peBlob = new BlobBuilder();

            var contentId = peBuilder.Serialize(peBlob);

            peBlob.WriteContentTo(peStream);
        }

        private static ModuleSymbol GetSourceModule(CompilationVerifier verifier)
        {
            return ((CSharpCompilation)verifier.Compilation).SourceModule;
        }
    }
}
