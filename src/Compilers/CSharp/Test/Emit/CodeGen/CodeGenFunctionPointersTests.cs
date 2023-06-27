// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                CSharpTestSource sources,
                MetadataReference[]? references = null,
                Action<ModuleSymbol>? symbolValidator = null,
                string? expectedOutput = null,
                TargetFramework targetFramework = TargetFramework.Standard,
                CSharpCompilationOptions? options = null)
        {
            var comp = CreateCompilation(
                sources,
                references,
                parseOptions: TestOptions.Regular9,
                options: options ?? (expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe),
                targetFramework: targetFramework);

            return CompileAndVerify(comp, symbolValidator: symbolValidator, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        private static CSharpCompilation CreateCompilationWithFunctionPointers(CSharpTestSource source, IEnumerable<MetadataReference>? references = null, CSharpCompilationOptions? options = null, TargetFramework? targetFramework = null)
        {
            return CreateCompilation(source, references: references, options: options ?? TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, targetFramework: targetFramework ?? TargetFramework.Net50);
        }

        private static CSharpCompilation CreateCompilationWithFunctionPointers(CSharpTestSource source, bool includeUnmanagedCallersOnly, CSharpCompilationOptions? options = null)
        {
            var references = includeUnmanagedCallersOnly
                ? TargetFrameworkUtil.GetReferences(TargetFramework.Net50)
                : TargetFrameworkUtil.GetReferencesWithout(TargetFramework.Net50, "System.Runtime.InteropServices.dll");
            return CreateCompilation(source, references: references, options: options ?? TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Empty);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointersWithIl(string source, string ilStub, Action<ModuleSymbol>? symbolValidator = null, string? expectedOutput = null)
        {
            var comp = CreateCompilationWithIL(source, ilStub, parseOptions: TestOptions.Regular9, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: symbolValidator, verify: Verification.Skipped);
        }

        private static CSharpCompilation CreateCompilationWithFunctionPointersAndIl(string source, string ilStub, CSharpCompilationOptions? options = null, bool includeUnmanagedCallersOnly = true)
        {
            var references = includeUnmanagedCallersOnly
                ? TargetFrameworkUtil.GetReferences(TargetFramework.Net50)
                : TargetFrameworkUtil.GetReferencesWithout(TargetFramework.Net50, "System.Runtime.InteropServices.dll");
            return CreateCompilationWithIL(source, ilStub, references: references, options: options ?? TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Empty);
        }

        [Theory]
        [InlineData("", CallingConvention.Default)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("unmanaged[Cdecl]", CallingConvention.CDecl)]
        [InlineData("unmanaged[Thiscall]", CallingConvention.ThisCall)]
        [InlineData("unmanaged[Stdcall]", CallingConvention.Standard)]
        [InlineData("unmanaged[Fastcall]", CallingConvention.FastCall)]
        [InlineData("unmanaged[@Cdecl]", CallingConvention.CDecl)]
        [InlineData("unmanaged[@Thiscall]", CallingConvention.ThisCall)]
        [InlineData("unmanaged[@Stdcall]", CallingConvention.Standard)]
        [InlineData("unmanaged[@Fastcall]", CallingConvention.FastCall)]
        [InlineData("unmanaged", CallingConvention.Unmanaged)]
        internal void CallingConventions(string conventionString, CallingConvention expectedConvention)
        {
            var verifier = CompileAndVerifyFunctionPointers($@"
class C
{{
    public unsafe delegate* {conventionString}<string, int> M() => throw null;
}}", symbolValidator: symbolValidator, targetFramework: TargetFramework.NetCoreApp);

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
        public void MultipleCallingConventions()
        {
            var comp = CompileAndVerifyFunctionPointers(@"
#pragma warning disable CS0168
unsafe class C
{
    public delegate* unmanaged[Thiscall, Stdcall]<void> M() => throw null;
}", symbolValidator: symbolValidator, targetFramework: TargetFramework.NetCoreApp);

            void symbolValidator(ModuleSymbol module)
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMethod("M");
                var funcPtr = m.ReturnType;

                AssertEx.Equal("delegate* unmanaged[Thiscall, Stdcall]<System.Void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall)>", funcPtr.ToTestDisplayString());
                Assert.Equal(CallingConvention.Unmanaged, ((FunctionPointerTypeSymbol)funcPtr).Signature.CallingConvention);
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
    public unsafe delegate* unmanaged[Cdecl]<delegate* unmanaged[Stdcall]<int, void>, void> M(delegate*<C, delegate*<S>> param1) => throw null;
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
                // (6,26): error CS0570: 'delegate*<ref int>' is not supported by the language
                //         ref int i1 = ref c.Field1();
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field1()").WithArguments("delegate*<ref int>").WithLocation(6, 26),
                // (6,28): error CS0570: 'C.Field1' is not supported by the language
                //         ref int i1 = ref c.Field1();
                Diagnostic(ErrorCode.ERR_BindToBogus, "Field1").WithArguments("C.Field1").WithLocation(6, 28),
                // (7,26): error CS0570: 'delegate*<ref int>' is not supported by the language
                //         ref int i2 = ref c.Field2();
                Diagnostic(ErrorCode.ERR_BindToBogus, "c.Field2()").WithArguments("delegate*<ref int>").WithLocation(7, 26),
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
    public unsafe delegate* unmanaged[Cdecl]<ref delegate*<ref readonly string>, void> M(delegate*<in delegate* unmanaged[Stdcall]<delegate*<void>>, delegate*<int>> param) => throw null;
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

            var compilation = CreateCompilationWithIL(source: "", ilSource, parseOptions: TestOptions.Regular9);
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
    public delegate* unmanaged[Stdcall]<int> Prop2 { get => throw null; set => throw null; }
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
    .field private method vararg void *() '<Prop>k__BackingField'
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
    
    .method public hidebysig specialname instance method vararg void *() 
            get_Prop() cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      method vararg void *() C::'<Prop>k__BackingField'
      IL_0006:  ret
    } // end of method C::get_Prop
    
    .method public hidebysig specialname instance void 
            set_Prop(method  vararg void *() 'value') cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      method vararg void *() C::'<Prop>k__BackingField'
      IL_0007:  ret
    } // end of method C::set_Prop
    
    .property instance method vararg void *()
            Prop()
    {
      .get instance method vararg void *() C::get_Prop()
      .set instance void C::set_Prop(method vararg void *())
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
                // (6,9): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field(__arglist(1, 2));
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "c.Field(__arglist(1, 2))").WithArguments("delegate* unmanaged[]<void>").WithLocation(6, 9),
                // (6,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field(__arglist(1, 2));
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate* unmanaged[]<void>").WithLocation(6, 11),
                // (7,9): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field(1, 2, 3);
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "c.Field(1, 2, 3)").WithArguments("delegate* unmanaged[]<void>").WithLocation(7, 9),
                // (7,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field(1, 2, 3);
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate* unmanaged[]<void>").WithLocation(7, 11),
                // (8,9): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "c.Field()").WithArguments("delegate* unmanaged[]<void>").WithLocation(8, 9),
                // (8,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate* unmanaged[]<void>").WithLocation(8, 11),
                // (9,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field = c.Field;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate* unmanaged[]<void>").WithLocation(9, 11),
                // (9,21): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Field = c.Field;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Field").WithArguments("delegate* unmanaged[]<void>").WithLocation(9, 21),
                // (10,9): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Prop();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "c.Prop()").WithArguments("delegate* unmanaged[]<void>").WithLocation(10, 9),
                // (10,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Prop();
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate* unmanaged[]<void>").WithLocation(10, 11),
                // (11,11): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Prop = c.Prop;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate* unmanaged[]<void>").WithLocation(11, 11),
                // (11,20): error CS8806: The calling convention of 'delegate* unmanaged[]<void>' is not supported by the language.
                //         c.Prop = c.Prop;
                Diagnostic(ErrorCode.ERR_UnsupportedCallingConvention, "Prop").WithArguments("delegate* unmanaged[]<void>").WithLocation(11, 20)
            );

            var c = comp.GetTypeByMetadataName("C");
            var prop = c.GetProperty("Prop");

            VerifyFunctionPointerSymbol(prop.Type, CallingConvention.ExtraArguments,
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
        [InlineData("Cdecl", "Cdecl")]
        [InlineData("Stdcall", "StdCall")]
        public void UnmanagedCallingConventions(string unmanagedConvention, string enumConvention)
        {
            // Use IntPtr Marshal.GetFunctionPointerForDelegate<TDelegate>(TDelegate delegate) to
            // get a function pointer around a native calling convention
            var source = $@" 
using System;
using System.Runtime.InteropServices;
public unsafe class UnmanagedFunctionPointer 
{{
    [UnmanagedFunctionPointer(CallingConvention.{enumConvention})]
    public delegate string CombineStrings(string s1, string s2);
    
    private static string CombineStringsImpl(string s1, string s2)
    {{
        return s1 + s2;
    }}

    public static delegate* unmanaged[{unmanagedConvention}]<string, string, string> GetFuncPtr(out CombineStrings del)
    {{
        del = CombineStringsImpl;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[{unmanagedConvention}]<string, string, string>)ptr;
    }}
}}
class Caller
{{
    public unsafe static void Main()
    {{
        Call(UnmanagedFunctionPointer.GetFuncPtr(out var del));
        GC.KeepAlive(del);
    }}

    public unsafe static void Call(delegate* unmanaged[{unmanagedConvention}]<string, string, string> ptr)
    {{
        Console.WriteLine(ptr(""Hello"", "" World""));
    }}
}}";

            var verifier = CompileAndVerifyFunctionPointers(source, expectedOutput: "Hello World");
            verifier.VerifyIL($"Caller.Call", $@"
{{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (delegate* unmanaged[{unmanagedConvention}]<string, string, string> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""Hello""
  IL_0007:  ldstr      "" World""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate* unmanaged[{unmanagedConvention}]<string, string, string>""
  IL_0012:  call       ""void System.Console.WriteLine(string)""
  IL_0017:  ret
}}");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("", "")]
        [InlineData("[Cdecl]", "typeof(System.Runtime.CompilerServices.CallConvCdecl)")]
        [InlineData("[Stdcall]", "typeof(System.Runtime.CompilerServices.CallConvStdcall)")]
        public void UnmanagedCallingConventions_UnmanagedCallersOnlyAttribute(string delegateConventionString, string attributeArgumentString)
        {
            var verifier = CompileAndVerifyFunctionPointers(new[] { $@"
using System;
using System.Runtime.InteropServices;
unsafe
{{
    delegate* unmanaged{delegateConventionString}<void> ptr = &M;
    ptr();

    [UnmanagedCallersOnly(CallConvs = new Type[] {{ {attributeArgumentString} }})]
    static void M() => Console.Write(1);
}}
", UnmanagedCallersOnlyAttribute }, expectedOutput: "1", targetFramework: TargetFramework.NetCoreApp);

            verifier.VerifyIL("<top-level-statements-entry-point>", $@"
{{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M|0_0()""
  IL_0006:  calli      ""delegate* unmanaged{delegateConventionString}<void>""
  IL_000b:  ret
}}
");
        }

        [Fact]
        public void FastCall()
        {
            // Use IntPtr Marshal.GetFunctionPointerForDelegate<TDelegate>(TDelegate delegate) to
            // get a function pointer around a native calling convention
            var source = @" 
using System;
using System.Runtime.InteropServices;
public unsafe class UnmanagedFunctionPointer 
{
    [UnmanagedFunctionPointer(CallingConvention.FastCall)]
    public delegate string CombineStrings(string s1, string s2);
    
    private static string CombineStringsImpl(string s1, string s2)
    {
        return s1 + s2;
    }

    public static delegate* unmanaged[Fastcall]<string, string, string> GetFuncPtr(out CombineStrings del)
    {
        del = CombineStringsImpl;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[Fastcall]<string, string, string>)ptr;
    }
}
class Caller
{
    public unsafe static void Main()
    {
        Call(UnmanagedFunctionPointer.GetFuncPtr(out var del));
        GC.KeepAlive(del);
    }

    public unsafe static void Call(delegate* unmanaged[Fastcall]<string, string, string> ptr)
    {
        Console.WriteLine(ptr(""Hello"", "" World""));
    }
}";

            // Fastcall is only supported by Mono on Windows x86, which we do not have a test leg for.
            // Therefore, we just verify that the emitted IL is what we expect.
            var verifier = CompileAndVerifyFunctionPointers(source);
            verifier.VerifyIL($"Caller.Call(delegate* unmanaged[Fastcall]<string, string, string>)", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (delegate* unmanaged[Fastcall]<string, string, string> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""Hello""
  IL_0007:  ldstr      "" World""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate* unmanaged[Fastcall]<string, string, string>""
  IL_0012:  call       ""void System.Console.WriteLine(string)""
  IL_0017:  ret
}");
        }

        [Fact]
        public void ThiscallSimpleReturn()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
using System.Runtime.InteropServices;

unsafe struct S
{
    public int i;
    public static int GetInt(S* s)
    {
        return s->i;
    }
    
    public static int GetReturn(S* s, int i)
    {
        return s->i + i;
    }
}

unsafe class UnmanagedFunctionPointer
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate int SingleParam(S* s);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate int MultipleParams(S* s, int i);
    
    public static delegate* unmanaged[Thiscall]<S*, int> GetFuncPtrSingleParam(out SingleParam del)
    {
        del = S.GetInt;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[Thiscall]<S*, int>)ptr;
    }
    public static delegate* unmanaged[Thiscall]<S*, int, int> GetFuncPtrMultipleParams(out MultipleParams del)
    {
        del = S.GetReturn;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[Thiscall]<S*, int, int>)ptr;
    }
}

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
        var i = UnmanagedFunctionPointer.GetFuncPtrSingleParam(out var del)(&s);
        Console.Write(i);
        GC.KeepAlive(del);
    }

    public static void TestMultiple()
    {
        S s = new S();
        s.i = 2;
        var i = UnmanagedFunctionPointer.GetFuncPtrMultipleParams(out var del)(&s, 3);
        Console.Write(i);
        GC.KeepAlive(del);
    }
}", expectedOutput: @"15");

            verifier.VerifyIL("C.TestSingle()", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (S V_0, //s
                UnmanagedFunctionPointer.SingleParam V_1, //del
                delegate* unmanaged[Thiscall]<S*, int> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""delegate* unmanaged[Thiscall]<S*, int> UnmanagedFunctionPointer.GetFuncPtrSingleParam(out UnmanagedFunctionPointer.SingleParam)""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_0
  IL_001a:  conv.u
  IL_001b:  ldloc.2
  IL_001c:  calli      ""delegate* unmanaged[Thiscall]<S*, int>""
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ldloc.1
  IL_0027:  call       ""void System.GC.KeepAlive(object)""
  IL_002c:  ret
}
");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (S V_0, //s
                UnmanagedFunctionPointer.MultipleParams V_1, //del
                delegate* unmanaged[Thiscall]<S*, int, int> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""delegate* unmanaged[Thiscall]<S*, int, int> UnmanagedFunctionPointer.GetFuncPtrMultipleParams(out UnmanagedFunctionPointer.MultipleParams)""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_0
  IL_001a:  conv.u
  IL_001b:  ldc.i4.3
  IL_001c:  ldloc.2
  IL_001d:  calli      ""delegate* unmanaged[Thiscall]<S*, int, int>""
  IL_0022:  call       ""void System.Console.Write(int)""
  IL_0027:  ldloc.1
  IL_0028:  call       ""void System.GC.KeepAlive(object)""
  IL_002d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Thiscall_UnmanagedCallersOnly()
        {
            var verifier = CompileAndVerifyFunctionPointers(new[] { @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe
{
    TestSingle();
    TestMultiple();

    static void TestSingle()
    {
        S s = new S();
        s.i = 1;
        delegate* unmanaged[Thiscall]<S*, int> ptr = &S.GetInt;
        Console.Write(ptr(&s));
    }

    static void TestMultiple()
    {
        S s = new S();
        s.i = 2;
        delegate* unmanaged[Thiscall]<S*, int, int> ptr = &S.GetReturn;
        Console.Write(ptr(&s, 3));
    }
}

unsafe struct S
{
    public int i;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvThiscall) })]
    public static int GetInt(S* s)
    {
        return s->i;
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvThiscall) })]
    public static int GetReturn(S* s, int i)
    {
        return s->i + i;
    }
}
", UnmanagedCallersOnlyAttribute }, expectedOutput: "15", targetFramework: TargetFramework.NetCoreApp);

            verifier.VerifyIL(@"Program.<<Main>$>g__TestSingle|0_0()", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (S V_0, //s
                delegate* unmanaged[Thiscall]<S*, int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldftn      ""int S.GetInt(S*)""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_0
  IL_0019:  conv.u
  IL_001a:  ldloc.1
  IL_001b:  calli      ""delegate* unmanaged[Thiscall]<S*, int>""
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ret
}
");

            verifier.VerifyIL(@"Program.<<Main>$>g__TestMultiple|0_1()", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (S V_0, //s
                delegate* unmanaged[Thiscall]<S*, int, int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldftn      ""int S.GetReturn(S*, int)""
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_0
  IL_0019:  conv.u
  IL_001a:  ldc.i4.3
  IL_001b:  ldloc.1
  IL_001c:  calli      ""delegate* unmanaged[Thiscall]<S*, int, int>""
  IL_0021:  call       ""void System.Console.Write(int)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void ThiscallBlittable()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
using System.Runtime.InteropServices;

struct IntWrapper
{
    public int i;
    public IntWrapper(int i)
    {
        this.i = i;
    }
}

struct ReturnWrapper
{
    public int i1;
    public float f2;
    
    public ReturnWrapper(int i1, float f2)
    {
        this.i1 = i1;
        this.f2 = f2;
    }
}

unsafe struct S
{
    public int i;
    public static IntWrapper GetInt(S* s)
    {
        return new IntWrapper(s->i);
    }
    
    public static ReturnWrapper GetReturn(S* s, float f)
    {
        return new ReturnWrapper(s->i, f);
    }
}

unsafe class UnmanagedFunctionPointer
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntWrapper SingleParam(S* s);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate ReturnWrapper MultipleParams(S* s, float f);
    
    public static delegate* unmanaged[Thiscall]<S*, IntWrapper> GetFuncPtrSingleParam(out SingleParam del)
    {
        del = S.GetInt;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[Thiscall]<S*, IntWrapper>)ptr;
    }
    public static delegate* unmanaged[Thiscall]<S*, float, ReturnWrapper> GetFuncPtrMultipleParams(out MultipleParams del)
    {
        del = S.GetReturn;
        var ptr = Marshal.GetFunctionPointerForDelegate(del);
        return (delegate* unmanaged[Thiscall]<S*, float, ReturnWrapper>)ptr;
    }
}

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
        var intWrapper = UnmanagedFunctionPointer.GetFuncPtrSingleParam(out var del)(&s);
        Console.WriteLine(intWrapper.i);
        GC.KeepAlive(del);
    }

    public static void TestMultiple()
    {
        S s = new S();
        s.i = 2;
        var returnWrapper = UnmanagedFunctionPointer.GetFuncPtrMultipleParams(out var del)(&s, 3.5f);
        Console.Write(returnWrapper.i1);
        Console.Write(returnWrapper.f2);
        GC.KeepAlive(del);
    }
}", expectedOutput: @"
1
23.5
");

            verifier.VerifyIL("C.TestSingle()", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (S V_0, //s
                UnmanagedFunctionPointer.SingleParam V_1, //del
                delegate* unmanaged[Thiscall]<S*, IntWrapper> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""delegate* unmanaged[Thiscall]<S*, IntWrapper> UnmanagedFunctionPointer.GetFuncPtrSingleParam(out UnmanagedFunctionPointer.SingleParam)""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_0
  IL_001a:  conv.u
  IL_001b:  ldloc.2
  IL_001c:  calli      ""delegate* unmanaged[Thiscall]<S*, IntWrapper>""
  IL_0021:  ldfld      ""int IntWrapper.i""
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  ldloc.1
  IL_002c:  call       ""void System.GC.KeepAlive(object)""
  IL_0031:  ret
}
");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  .locals init (S V_0, //s
                UnmanagedFunctionPointer.MultipleParams V_1, //del
                delegate* unmanaged[Thiscall]<S*, float, ReturnWrapper> V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       ""delegate* unmanaged[Thiscall]<S*, float, ReturnWrapper> UnmanagedFunctionPointer.GetFuncPtrMultipleParams(out UnmanagedFunctionPointer.MultipleParams)""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_0
  IL_001a:  conv.u
  IL_001b:  ldc.r4     3.5
  IL_0020:  ldloc.2
  IL_0021:  calli      ""delegate* unmanaged[Thiscall]<S*, float, ReturnWrapper>""
  IL_0026:  dup
  IL_0027:  ldfld      ""int ReturnWrapper.i1""
  IL_002c:  call       ""void System.Console.Write(int)""
  IL_0031:  ldfld      ""float ReturnWrapper.f2""
  IL_0036:  call       ""void System.Console.Write(float)""
  IL_003b:  ldloc.1
  IL_003c:  call       ""void System.GC.KeepAlive(object)""
  IL_0041:  ret
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/68208")]
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
", expectedOutput: ExecutionConditionUtil.IsCoreClr ? "System.Void()" : "System.IntPtr");

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
}", references: new[] { nopiaReference }, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

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
}", references: new[] { aRef }, assemblyName: "B", parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

            var cComp = CreateCompilation(@"
internal class C
{
    internal unsafe void CM(B b)
    {
        b.M()();
    }
}", references: new[] { aRef, bRef }, assemblyName: "C", parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);

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
}", references: new[] { aRef }, assemblyName: "B", parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();

            var cComp = CreateCompilation(@"
internal class C
{
    internal unsafe void CM(B b)
    {
        b.M()();
    }
}", references: new[] { aRef, bRef }, assemblyName: "C", parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);

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
                // (27,40): error CS8758: Ref mismatch between 'C.M6()' and function pointer 'delegate*<ref object>'
                //         delegate*<ref object> ptr15 = &M6;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M6").WithArguments("C.M6()", "delegate*<ref object>").WithLocation(27, 40),
                // (28,40): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<ref object>'
                //         delegate*<ref object> ptr16 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<ref object>").WithLocation(28, 40),
                // (29,49): error CS8758: Ref mismatch between 'C.M5()' and function pointer 'delegate*<ref readonly object>'
                //         delegate*<ref readonly object> ptr17 = &M5;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M5").WithArguments("C.M5()", "delegate*<ref readonly object>").WithLocation(29, 49),
                // (30,49): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<ref readonly object>'
                //         delegate*<ref readonly object> ptr18 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<ref readonly object>").WithLocation(30, 49)
            );
        }

        [Theory]
        [InlineData("unmanaged[Cdecl]", "CDecl")]
        [InlineData("unmanaged[Stdcall]", "Standard")]
        [InlineData("unmanaged[Thiscall]", "ThisCall")]
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
}", targetFramework: TargetFramework.Standard);

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
                // (9,23): error CS8811: Cannot convert &method group 'M' to delegate type 'Action'.
                //         Action ptr2 = (Action)(&M);
                Diagnostic(ErrorCode.ERR_CannotConvertAddressOfToDelegate, "(Action)(&M)").WithArguments("M", "System.Action").WithLocation(9, 23),
                // (10,23): error CS8811: Cannot convert &method group 'M' to delegate type 'Action'.
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

        [Fact, WorkItem(59454, "https://github.com/dotnet/roslyn/issues/59454")]
        public void FunctionPointerInvocationInExpressionTree()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Linq.Expressions;
namespace test
{
    unsafe class Program
    {
        static double f() => 0;
        static delegate*<double> fp() => &f;
        static void Main()
        {
            Expression<Func<double>> h = static () => fp()();
            Console.WriteLine(h);
        }
    }
}
");

            comp.VerifyDiagnostics(
                // (12,55): error CS1944: An expression tree may not contain an unsafe pointer operation
                //             Expression<Func<double>> h = static () => fp()();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPointerOp, "fp()()").WithLocation(12, 55)
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
  // Code size       33 (0x21)
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
  IL_0017:  ldc.i4.0
  IL_0018:  conv.u
  IL_0019:  ceq
  IL_001b:  call       ""void System.Console.Write(bool)""
  IL_0020:  ret
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
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  ldc.i4.0
  IL_0003:  conv.u
  IL_0004:  ceq
  IL_0006:  call       ""void System.Console.Write(bool)""
  IL_000b:  ret
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
", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9);

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
#pragma warning disable CS8909 // Function pointers should not be compared
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

        [Theory, WorkItem(48919, "https://github.com/dotnet/roslyn/issues/48919")]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData(">=")]
        [InlineData(">")]
        [InlineData("<=")]
        [InlineData("<")]
        public void BinaryComparisonWarnings(string @operator)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe
{{
    delegate*<void> a = null, b = null;
    _ = a {@operator} b;
}}", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (5,9): error CS8909: Comparison of function pointers might yield an unexpected result, since pointers to the same function may be distinct.
                //     _ = a {@operator} b;
                Diagnostic(ErrorCode.WRN_DoNotCompareFunctionPointers, @operator).WithLocation(5, 11)
            );
        }

        [Theory, WorkItem(48919, "https://github.com/dotnet/roslyn/issues/48919")]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData(">=")]
        [InlineData(">")]
        [InlineData("<=")]
        [InlineData("<")]
        public void BinaryComparisonCastToVoidStar_NoWarning(string @operator)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
unsafe
{{
    delegate*<void> a = null, b = null;
    _ = (void*)a {@operator} b;
    _ = a {@operator} (void*)b;
}}", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
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

            verifier.VerifyIL(@"C.M<T>(delegate*<ref T>, delegate*<T>)", @"
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

            var comp = CreateCompilationWithFunctionPointers(source, options: WithNullableEnable(TestOptions.UnsafeReleaseDll));
            comp.VerifyDiagnostics();

            verifySymbolNullabilities(comp.GetTypeByMetadataName("C")!);

            CompileAndVerify(comp, symbolValidator: symbolValidator, verify: Verification.Skipped);

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
    protected virtual void M3(delegate* unmanaged[Stdcall, Thiscall]<void> ptr) {}
    protected virtual delegate* unmanaged[Stdcall, Thiscall]<void> M4() => throw null;
}
unsafe class Derived1 : Base
{
    protected override void M1(delegate* unmanaged[Cdecl]<void> ptr) {}
    protected override delegate* unmanaged[Cdecl]<void> M2() => throw null;
    protected override void M3(delegate* unmanaged[Fastcall, Thiscall]<void> ptr) {}
    protected override delegate* unmanaged[Fastcall, Thiscall]<void> M4() => throw null;
}
unsafe class Derived2 : Base
{
    protected override void M1(delegate*<void> ptr) {} // Implemented correctly
    protected override delegate*<void> M2() => throw null; // Implemented correctly
    protected override void M3(delegate* unmanaged[Stdcall, Fastcall]<void> ptr) {}
    protected override delegate* unmanaged[Stdcall, Fastcall]<void> M4() => throw null;
}
");

            comp.VerifyDiagnostics(
                // (11,29): error CS0115: 'Derived1.M1(delegate* unmanaged[Cdecl]<void>)': no suitable method found to override
                //     protected override void M1(delegate* unmanaged[Cdecl]<void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived1.M1(delegate* unmanaged[Cdecl]<void>)").WithLocation(11, 29),
                // (12,57): error CS0508: 'Derived1.M2()': return type must be 'delegate*<void>' to match overridden member 'Base.M2()'
                //     protected override delegate* unmanaged[Cdecl]<void> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived1.M2()", "Base.M2()", "delegate*<void>").WithLocation(12, 57),
                // (13,29): error CS0115: 'Derived1.M3(delegate* unmanaged[Fastcall, Thiscall]<void>)': no suitable method found to override
                //     protected override void M3(delegate* unmanaged[Fastcall, Thiscall]<void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M3").WithArguments("Derived1.M3(delegate* unmanaged[Fastcall, Thiscall]<void>)").WithLocation(13, 29),
                // (14,70): error CS0508: 'Derived1.M4()': return type must be 'delegate* unmanaged[Stdcall, Thiscall]<void>' to match overridden member 'Base.M4()'
                //     protected override delegate* unmanaged[Fastcall, Thiscall]<void> M4() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M4").WithArguments("Derived1.M4()", "Base.M4()", "delegate* unmanaged[Stdcall, Thiscall]<void>").WithLocation(14, 70),
                // (20,29): error CS0115: 'Derived2.M3(delegate* unmanaged[Stdcall, Fastcall]<void>)': no suitable method found to override
                //     protected override void M3(delegate* unmanaged[Stdcall, Fastcall]<void> ptr) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M3").WithArguments("Derived2.M3(delegate* unmanaged[Stdcall, Fastcall]<void>)").WithLocation(20, 29),
                // (21,69): error CS0508: 'Derived2.M4()': return type must be 'delegate* unmanaged[Stdcall, Thiscall]<void>' to match overridden member 'Base.M4()'
                //     protected override delegate* unmanaged[Stdcall, Fastcall]<void> M4() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M4").WithArguments("Derived2.M4()", "Base.M4()", "delegate* unmanaged[Stdcall, Thiscall]<void>").WithLocation(21, 69)
            );
        }

        [Fact]
        public void Override_ConventionOrderingDoesNotMatter()
        {
            var source1 = @"
using System;
public unsafe class Base
{
    public virtual void M1(delegate* unmanaged[Thiscall, Stdcall]<void> param) => Console.WriteLine(""Base Thiscall, Stdcall param"");
    public virtual delegate* unmanaged[Thiscall, Stdcall]<void> M2() { Console.WriteLine(""Base Thiscall, Stdcall return""); return null; }
    public virtual void M3(delegate* unmanaged[Thiscall, Stdcall]<ref string> param) => Console.WriteLine(""Base Thiscall, Stdcall ref param"");
    public virtual delegate* unmanaged[Thiscall, Stdcall]<ref string> M4() { Console.WriteLine(""Base Thiscall, Stdcall ref return""); return null; }
}
";

            var source2 = @"
using System;
unsafe class Derived1 : Base
{
    public override void M1(delegate* unmanaged[Stdcall, Thiscall]<void> param) => Console.WriteLine(""Derived1 Stdcall, Thiscall param"");
    public override delegate* unmanaged[Stdcall, Thiscall]<void> M2() { Console.WriteLine(""Derived1 Stdcall, Thiscall return""); return null; }
    public override void M3(delegate* unmanaged[Stdcall, Thiscall]<ref string> param) => Console.WriteLine(""Derived1 Stdcall, Thiscall ref param"");
    public override delegate* unmanaged[Stdcall, Thiscall]<ref string> M4() { Console.WriteLine(""Derived1 Stdcall, Thiscall ref return""); return null; }
}
unsafe class Derived2 : Base
{
    public override void M1(delegate* unmanaged[Stdcall, Stdcall, Thiscall]<void> param) => Console.WriteLine(""Derived2 Stdcall, Stdcall, Thiscall param"");
    public override delegate* unmanaged[Stdcall, Stdcall, Thiscall]<void> M2() { Console.WriteLine(""Derived2 Stdcall, Stdcall, Thiscall return""); return null; }
    public override void M3(delegate* unmanaged[Stdcall, Stdcall, Thiscall]<ref string> param) => Console.WriteLine(""Derived2 Stdcall, Stdcall, Thiscall ref param"");
    public override delegate* unmanaged[Stdcall, Stdcall, Thiscall]<ref string> M4() { Console.WriteLine(""Derived2 Stdcall, Stdcall, Thiscall ref return""); return null; }
}
";

            var executableCode = @"
using System;
unsafe
{
    delegate* unmanaged[Stdcall, Thiscall]<void> ptr1 = null;
    delegate* unmanaged[Stdcall, Thiscall]<ref string> ptr3 = null;

    Base b1 = new Base();
    b1.M1(ptr1);
    b1.M2();
    b1.M3(ptr3);
    b1.M4();

    Base d1 = new Derived1();
    d1.M1(ptr1);
    d1.M2();
    d1.M3(ptr3);
    d1.M4();

    Base d2 = new Derived2();
    d2.M1(ptr1);
    d2.M2();
    d2.M3(ptr3);
    d2.M4();
}
";

            var expectedOutput = @"
Base Thiscall, Stdcall param
Base Thiscall, Stdcall return
Base Thiscall, Stdcall ref param
Base Thiscall, Stdcall ref return
Derived1 Stdcall, Thiscall param
Derived1 Stdcall, Thiscall return
Derived1 Stdcall, Thiscall ref param
Derived1 Stdcall, Thiscall ref return
Derived2 Stdcall, Stdcall, Thiscall param
Derived2 Stdcall, Stdcall, Thiscall return
Derived2 Stdcall, Stdcall, Thiscall ref param
Derived2 Stdcall, Stdcall, Thiscall ref return
";

            var allSourceComp = CreateCompilationWithFunctionPointers(new[] { executableCode, source1, source2 }, options: TestOptions.UnsafeReleaseExe);

            CompileAndVerify(
                allSourceComp,
                expectedOutput: RuntimeUtilities.IsCoreClrRuntime ? expectedOutput : null,
                symbolValidator: getSymbolValidator(separateAssembly: false),
                verify: Verification.Skipped);

            var baseComp = CreateCompilationWithFunctionPointers(source1);
            var metadataRef = baseComp.EmitToImageReference();

            var derivedComp = CreateCompilationWithFunctionPointers(new[] { executableCode, source2 }, references: new[] { metadataRef }, options: TestOptions.UnsafeReleaseExe);
            CompileAndVerify(
                derivedComp,
                expectedOutput: RuntimeUtilities.IsCoreClrRuntime ? expectedOutput : null,
                symbolValidator: getSymbolValidator(separateAssembly: true),
                verify: Verification.Skipped);

            static Action<ModuleSymbol> getSymbolValidator(bool separateAssembly)
            {
                return module =>
                {
                    var @base = (separateAssembly ? module.ReferencedAssemblySymbols[1].GlobalNamespace : module.GlobalNamespace).GetTypeMember("Base");
                    var baseM1 = @base.GetMethod("M1");
                    var baseM2 = @base.GetMethod("M2");
                    var baseM3 = @base.GetMethod("M3");
                    var baseM4 = @base.GetMethod("M4");

                    for (int derivedI = 1; derivedI <= 2; derivedI++)
                    {
                        var derived = module.GlobalNamespace.GetTypeMember($"Derived{derivedI}");
                        var derivedM1 = derived.GetMethod("M1");
                        var derivedM2 = derived.GetMethod("M2");
                        var derivedM3 = derived.GetMethod("M3");
                        var derivedM4 = derived.GetMethod("M4");

                        Assert.True(baseM1.Parameters.Single().Type.Equals(derivedM1.Parameters.Single().Type, TypeCompareKind.ConsiderEverything));
                        Assert.True(baseM2.ReturnType.Equals(derivedM2.ReturnType, TypeCompareKind.ConsiderEverything));
                        Assert.True(baseM3.Parameters.Single().Type.Equals(derivedM3.Parameters.Single().Type, TypeCompareKind.ConsiderEverything));
                        Assert.True(baseM4.ReturnType.Equals(derivedM4.ReturnType, TypeCompareKind.ConsiderEverything));
                    }
                };
            }
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
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments($"Derived.M1(delegate*<{(string.IsNullOrWhiteSpace(refKind2) ? "" : refKind2)}string>)").WithLocation(9, 29),
                // (10,49): error CS0508: 'Derived.M2()': return type must be 'delegate*<{refKind1} string>' to match overridden member 'Base.M2()'
                //     protected override delegate*<{refKind2} string> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", $"delegate*<{(string.IsNullOrWhiteSpace(refKind1) ? "" : refKind1)}string>").WithLocation(10, 42 + refKind2.Length)
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
            CompileAndVerify(comp, symbolValidator: assertMethods, verify: Verification.Skipped);

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
            CompileAndVerify(comp, symbolValidator: assertMethods, verify: Verification.Skipped);

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
            CompileAndVerify(comp, symbolValidator: assertMethods, verify: Verification.Skipped);

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
            CompileAndVerify(comp, symbolValidator: assertMethods, verify: Verification.Skipped);

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
                // (19,51): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string, ref string>' doesn't match target type 'delegate*<ref string?, ref string>'.
                //         delegate*<ref string?, ref string> ptr2 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<ref string, ref string>", "delegate*<ref string?, ref string>").WithLocation(19, 51),
                // (20,51): warning CS8619: Nullability of reference types in value of type 'delegate*<ref string, ref string>' doesn't match target type 'delegate*<ref string, ref string?>'.
                //         delegate*<ref string, ref string?> ptr3 = ptr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "ptr1").WithArguments("delegate*<ref string, ref string>", "delegate*<ref string, ref string?>").WithLocation(20, 51)
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
", targetFramework: TargetFramework.NetCoreApp, expectedOutput: "123");

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

        [Fact, WorkItem(45447, "https://github.com/dotnet/roslyn/issues/45447")]
        public void LocalFunction_ValidStatic()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe class FunctionPointer
{
    public static void Main()
    {
        delegate*<void> a = &local;
        a();

        static void local() => System.Console.Write(""local"");
    }
}
", expectedOutput: "local");

            verifier.VerifyIL("FunctionPointer.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void FunctionPointer.<Main>g__local|0_0()""
  IL_0006:  calli      ""delegate*<void>""
  IL_000b:  ret
}
");
        }

        [Fact, WorkItem(45447, "https://github.com/dotnet/roslyn/issues/45447")]
        public void LocalFunction_ValidStatic_NestedInLocalFunction()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe class FunctionPointer
{
    public static void Main()
    {
        local(true);

        static void local(bool invoke)
        {
            if (invoke)
            {
                delegate*<bool, void> ptr = &local;
                ptr(false);
            }
            else
            {
                System.Console.Write(""local"");
            }
        }
    }
}
", expectedOutput: "local");

            verifier.VerifyIL("FunctionPointer.<Main>g__local|0_0(bool)", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (delegate*<bool, void> V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldftn      ""void FunctionPointer.<Main>g__local|0_0(bool)""
  IL_0009:  stloc.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<bool, void>""
  IL_0011:  ret
  IL_0012:  ldstr      ""local""
  IL_0017:  call       ""void System.Console.Write(string)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void LocalFunction_ValidStatic_NestedInLambda()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe class C
{
    public static void Main()
    {
        int capture = 1;
        System.Action _ = () =>
        {
            System.Console.Write(capture); // Just to ensure that this is emitted as a capture
            delegate*<void> ptr = &local;
            ptr();

            static void local() => System.Console.Write(""local"");
        };

        _();
    }
}
", expectedOutput: "1local");

            verifier.VerifyIL("C.<>c__DisplayClass0_0.<Main>b__0()", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.capture""
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  ldftn      ""void C.<Main>g__local|0_1()""
  IL_0011:  calli      ""delegate*<void>""
  IL_0016:  ret
}
");
        }

        [Fact, WorkItem(45447, "https://github.com/dotnet/roslyn/issues/45447")]
        public void LocalFunction_InvalidNonStatic()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class FunctionPointer
{
    public static void M()
    {
        int local = 1;

        delegate*<void> first = &noCaptures;
        delegate*<void> second = &capturesLocal;

        void noCaptures() { }
        void capturesLocal() { local++; }
    }
}");

            comp.VerifyDiagnostics(
                // (8,34): error CS8759: Cannot create a function pointer for 'noCaptures()' because it is not a static method
                //         delegate*<void> first = &noCaptures;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "noCaptures").WithArguments("noCaptures()").WithLocation(8, 34),
                // (9,35): error CS8759: Cannot create a function pointer for 'capturesLocal()' because it is not a static method
                //         delegate*<void> second = &capturesLocal;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "capturesLocal").WithArguments("capturesLocal()").WithLocation(9, 35)
            );
        }

        [Fact, WorkItem(45418, "https://github.com/dotnet/roslyn/issues/45418")]
        public void RefMismatchInCall()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Test
{
    void M1(delegate*<ref string, void> param1)
    {
        param1(out var l);
        string s = null;
        param1(s);
        param1(in s);
    }

    void M2(delegate*<in string, void> param2)
    {
        param2(out var l);
        string s = null;
        param2(s);
        param2(ref s);
    }

    void M3(delegate*<out string, void> param3)
    {
        string s = null;
        param3(s);
        param3(ref s);
        param3(in s);
    }

    void M4(delegate*<string, void> param4)
    {
        param4(out var l);
        string s = null;
        param4(ref s);
        param4(in s);
    }
}");

            comp.VerifyDiagnostics(
                // (6,20): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         param1(out var l);
                Diagnostic(ErrorCode.ERR_BadArgRef, "var l").WithArguments("1", "ref").WithLocation(6, 20),
                // (8,16): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         param1(s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "ref").WithLocation(8, 16),
                // (9,19): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         param1(in s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "ref").WithLocation(9, 19),
                // (14,20): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         param2(out var l);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var l").WithArguments("1", "out").WithLocation(14, 20),
                // (17,20): error CS9505: Argument 1 may not be passed with the 'ref' keyword in language version 9.0. To pass 'ref' arguments to 'in' parameters, upgrade to language version preview or greater.
                //         param2(ref s);
                Diagnostic(ErrorCode.ERR_BadArgExtraRefLangVersion, "s").WithArguments("1", "9.0", "preview").WithLocation(17, 20),
                // (23,16): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         param3(s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "out").WithLocation(23, 16),
                // (24,20): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         param3(ref s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "out").WithLocation(24, 20),
                // (25,19): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         param3(in s);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s").WithArguments("1", "out").WithLocation(25, 19),
                // (30,20): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         param4(out var l);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var l").WithArguments("1", "out").WithLocation(30, 20),
                // (32,20): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         param4(ref s);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "s").WithArguments("1", "ref").WithLocation(32, 20),
                // (33,19): error CS1615: Argument 1 may not be passed with the 'in' keyword
                //         param4(in s);
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "s").WithArguments("1", "in").WithLocation(33, 19)
            );
        }

        [Fact]
        public void MismatchedInferredLambdaReturn()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    public void M(delegate*<System.Func<string>, void> param)
    {
        param(a => a);
    }
}");

            comp.VerifyDiagnostics(
                // (6,15): error CS1593: Delegate 'Func<string>' does not take 1 arguments
                //         param(a => a);
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "a => a").WithArguments("System.Func<string>", "1").WithLocation(6, 15)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var lambda = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

            Assert.Equal("a => a", lambda.ToString());

            var info = model.GetSymbolInfo(lambda);
            var lambdaSymbol = (IMethodSymbol)info.Symbol!;
            Assert.NotNull(lambdaSymbol);
            Assert.Equal("System.String", lambdaSymbol.ReturnType.ToTestDisplayString(includeNonNullable: false));
            Assert.True(lambdaSymbol.Parameters.Single().Type.IsErrorType());
        }

        [Fact, WorkItem(45418, "https://github.com/dotnet/roslyn/issues/45418")]
        public void OutDeconstructionMismatch()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class Test
{
    void M1(delegate*<string, void> param1, object o)
    {
        param1(o is var (a, b));
        param1(o is (var c, var d));
    }
}", targetFramework: TargetFramework.Standard);

            comp.VerifyDiagnostics(
                // (6,25): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         param1(o is var (a, b));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(a, b)").WithArguments("object", "Deconstruct").WithLocation(6, 25),
                // (6,25): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //         param1(o is var (a, b));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(a, b)").WithArguments("object", "2").WithLocation(6, 25),
                // (7,21): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         param1(o is (var c, var d));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(var c, var d)").WithArguments("object", "Deconstruct").WithLocation(7, 21),
                // (7,21): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //         param1(o is (var c, var d));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(var c, var d)").WithArguments("object", "2").WithLocation(7, 21)
            );
        }

        [Fact]
        public void UnusedLoadNotLeftOnStack()
        {
            string source = @"
unsafe class FunctionPointer
{
    public static void Main()
    {
        delegate*<void> ptr = &Main;
    }
}
";
            var verifier = CompileAndVerifyFunctionPointers(source, expectedOutput: "", options: TestOptions.UnsafeReleaseExe);

            verifier.VerifyIL(@"FunctionPointer.Main", expectedIL: @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
");

            verifier = CompileAndVerifyFunctionPointers(source, expectedOutput: "", options: TestOptions.UnsafeDebugExe);
            verifier.VerifyIL("FunctionPointer.Main", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (delegate*<void> V_0) //ptr
  IL_0000:  nop
  IL_0001:  ldftn      ""void FunctionPointer.Main()""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");
        }

        [Fact]
        public void UnmanagedOnUnsupportedRuntime()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
#pragma warning disable CS0168 // Unused variable
class C
{
    unsafe void M()
    {
        delegate* unmanaged<void> ptr1;
        delegate* unmanaged[Stdcall, Thiscall]<void> ptr2;
    }
}", targetFramework: TargetFramework.NetStandard20);

            comp.VerifyDiagnostics(
                // (7,19): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                //         delegate* unmanaged<void> ptr1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "unmanaged").WithLocation(7, 19),
                // (8,19): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                //         delegate* unmanaged[Stdcall, Thiscall]<void> ptr2;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "unmanaged").WithLocation(8, 19)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var functionPointerSyntaxes = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().ToArray();

            Assert.Equal(2, functionPointerSyntaxes.Length);

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntaxes[0],
                expectedSyntax: "delegate* unmanaged<void>",
                expectedType: "delegate* unmanaged<System.Void>",
                expectedSymbol: "delegate* unmanaged<System.Void>");

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntaxes[1],
                expectedSyntax: "delegate* unmanaged[Stdcall, Thiscall]<void>",
                expectedType: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>",
                expectedSymbol: "delegate* unmanaged[Stdcall, Thiscall]<System.Void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall)>");
        }

        [Fact]
        public void NonPublicCallingConventionType()
        {
            string source1 = @"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public class String { }
    namespace Runtime.CompilerServices
    {
        internal class CallConvTest {}
        public static class RuntimeFeature
        {
            public const string UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);
        }
    }
}
";

            string source2 = @"
#pragma warning disable CS0168 // Unused local
unsafe class C
{
    void M()
    {
        delegate* unmanaged[Test]<void> ptr = null;
    }
}
";
            var allInCoreLib = CreateEmptyCompilation(source1 + source2, parseOptions: TestOptions.Regular9.WithNoRefSafetyRulesAttribute(), options: TestOptions.UnsafeReleaseDll);
            allInCoreLib.VerifyDiagnostics(
                // (23,29): error CS8891: Type 'CallConvTest' must be public to be used as a calling convention.
                //         delegate* unmanaged[Test]<void> ptr = null;
                Diagnostic(ErrorCode.ERR_TypeMustBePublic, "Test").WithArguments("System.Runtime.CompilerServices.CallConvTest").WithLocation(23, 29)
            );

            var tree = allInCoreLib.SyntaxTrees[0];
            var model = allInCoreLib.GetSemanticModel(tree);

            var functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest)>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest)>");

            var coreLib = CreateEmptyCompilation(source1, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            coreLib.VerifyDiagnostics();

            var comp1 = CreateEmptyCompilation(source2, references: new[] { coreLib.EmitToImageReference() }, parseOptions: TestOptions.Regular9.WithNoRefSafetyRulesAttribute(), options: TestOptions.UnsafeReleaseDll);
            comp1.VerifyDiagnostics(
                // (7,29): error CS8891: Type 'CallConvTest' must be public to be used as a calling convention.
                //         delegate* unmanaged[Test]<void> ptr = null;
                Diagnostic(ErrorCode.ERR_TypeMustBePublic, "Test").WithArguments("System.Runtime.CompilerServices.CallConvTest").WithLocation(7, 29)
            );

            tree = comp1.SyntaxTrees[0];
            model = comp1.GetSemanticModel(tree);

            functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest)>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest)>");
        }

        [Fact]
        public void GenericCallingConventionType()
        {
            string source1 = @"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public class String { }
    namespace Runtime.CompilerServices
    {
        public class CallConvTest<T> {}
        public static class RuntimeFeature
        {
            public const string UnmanagedSignatureCallingConvention = nameof(UnmanagedSignatureCallingConvention);
        }
    }
}
";

            string source2 = @"
#pragma warning disable CS0168 // Unused local
unsafe class C
{
    void M()
    {
        delegate* unmanaged[Test]<void> ptr = null;
    }
}
";
            var allInCoreLib = CreateEmptyCompilation(source1 + source2, parseOptions: TestOptions.Regular9.WithNoRefSafetyRulesAttribute(), options: TestOptions.UnsafeReleaseDll);
            allInCoreLib.VerifyDiagnostics(
                // (23,29): error CS8890: Type 'CallConvTest' is not defined.
                //         delegate* unmanaged[Test]<void> ptr = null;
                Diagnostic(ErrorCode.ERR_TypeNotFound, "Test").WithArguments("CallConvTest").WithLocation(23, 29)
            );

            var tree = allInCoreLib.SyntaxTrees[0];
            var model = allInCoreLib.GetSemanticModel(tree);

            var functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>");

            var coreLib = CreateEmptyCompilation(source1, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            coreLib.VerifyDiagnostics();

            var comp1 = CreateEmptyCompilation(source2, references: new[] { coreLib.EmitToImageReference() }, parseOptions: TestOptions.Regular9.WithNoRefSafetyRulesAttribute(), options: TestOptions.UnsafeReleaseDll);
            comp1.VerifyDiagnostics(
                // (7,29): error CS8890: Type 'CallConvTest' is not defined.
                //         delegate* unmanaged[Test]<void> ptr = null;
                Diagnostic(ErrorCode.ERR_TypeNotFound, "Test").WithArguments("CallConvTest").WithLocation(7, 29)
            );

            tree = comp1.SyntaxTrees[0];
            model = comp1.GetSemanticModel(tree);

            functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>");

            var @string = comp1.GetSpecialType(SpecialType.System_String);
            var testMod = CSharpCustomModifier.CreateOptional(comp1.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvTest`1"));

            var funcPtr = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: default,
                returnRefKind: RefKind.None, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                parameterRefCustomModifiers: default, compilation: comp1);
            var funcPtrRef = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: default,
                parameterRefCustomModifiers: default, returnRefKind: RefKind.Ref, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                compilation: comp1);

            var funcPtrWithTestOnReturn = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string, customModifiers: ImmutableArray.Create(testMod)), refCustomModifiers: default,
                parameterRefCustomModifiers: default, returnRefKind: RefKind.None, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                compilation: comp1);
            var funcPtrWithTestOnRef = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: ImmutableArray.Create(testMod),
                parameterRefCustomModifiers: default, returnRefKind: RefKind.Ref, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                compilation: comp1);

            Assert.Empty(funcPtrWithTestOnReturn.Signature.GetCallingConventionModifiers());
            Assert.Empty(funcPtrWithTestOnRef.Signature.GetCallingConventionModifiers());
            Assert.True(funcPtr.Equals(funcPtrWithTestOnReturn, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.False(funcPtr.Equals(funcPtrWithTestOnReturn, TypeCompareKind.ConsiderEverything));
            Assert.True(funcPtrRef.Equals(funcPtrWithTestOnRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.False(funcPtrRef.Equals(funcPtrWithTestOnRef, TypeCompareKind.ConsiderEverything));
        }

        [Fact]
        public void ConventionDefinedInWrongAssembly()
        {
            var source1 = @"
namespace System.Runtime.CompilerServices
{
    public class CallConvTest { }
}
";

            var source2 = @"
#pragma warning disable CS0168 // Unused local
unsafe class C
{
    static void M()
    {
        delegate* unmanaged[Test]<void> ptr;
    }
}
";

            var comp1 = CreateCompilationWithFunctionPointers(source1 + source2);
            comp1.VerifyDiagnostics(
                // (12,29): error CS8890: Type 'CallConvTest' is not defined.
                //         delegate* unmanaged[Test]<void> ptr;
                Diagnostic(ErrorCode.ERR_TypeNotFound, "Test").WithArguments("CallConvTest").WithLocation(12, 29)
            );

            var tree = comp1.SyntaxTrees[0];
            var model = comp1.GetSemanticModel(tree);

            var functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>");

            var reference = CreateCompilation(source1);
            var comp2 = CreateCompilationWithFunctionPointers(source2, new[] { reference.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (7,29): error CS8890: Type 'CallConvTest' is not defined.
                //         delegate* unmanaged[Test]<void> ptr;
                Diagnostic(ErrorCode.ERR_TypeNotFound, "Test").WithArguments("CallConvTest").WithLocation(7, 29)
            );

            tree = comp2.SyntaxTrees[0];
            model = comp2.GetSemanticModel(tree);

            functionPointerSyntax = tree.GetRoot().DescendantNodes().OfType<FunctionPointerTypeSyntax>().Single();

            VerifyFunctionPointerSemanticInfo(model, functionPointerSyntax,
                expectedSyntax: "delegate* unmanaged[Test]<void>",
                expectedType: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>",
                expectedSymbol: "delegate* unmanaged[Test]<System.Void modopt(System.Runtime.CompilerServices.CallConvTest[missing])>");

            var @string = comp2.GetSpecialType(SpecialType.System_String);
            var testMod = CSharpCustomModifier.CreateOptional(comp2.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvTest"));

            var funcPtr = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: default,
                returnRefKind: RefKind.None, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                parameterRefCustomModifiers: default, compilation: comp2);
            var funcPtrRef = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: default,
                returnRefKind: RefKind.Ref, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                parameterRefCustomModifiers: default, compilation: comp2);

            var funcPtrWithTestOnReturn = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string, customModifiers: ImmutableArray.Create(testMod)), refCustomModifiers: default,
                returnRefKind: RefKind.None, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                parameterRefCustomModifiers: default, compilation: comp2);
            var funcPtrWithTestOnRef = FunctionPointerTypeSymbol.CreateFromPartsForTests(
                CallingConvention.Unmanaged, TypeWithAnnotations.Create(@string), refCustomModifiers: ImmutableArray.Create(testMod),
                returnRefKind: RefKind.Ref, parameterTypes: ImmutableArray<TypeWithAnnotations>.Empty, parameterRefKinds: ImmutableArray<RefKind>.Empty,
                parameterRefCustomModifiers: default, compilation: comp2);

            Assert.Empty(funcPtrWithTestOnReturn.Signature.GetCallingConventionModifiers());
            Assert.Empty(funcPtrWithTestOnRef.Signature.GetCallingConventionModifiers());
            Assert.True(funcPtr.Equals(funcPtrWithTestOnReturn, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.False(funcPtr.Equals(funcPtrWithTestOnReturn, TypeCompareKind.ConsiderEverything));
            Assert.True(funcPtrRef.Equals(funcPtrWithTestOnRef, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));
            Assert.False(funcPtrRef.Equals(funcPtrWithTestOnRef, TypeCompareKind.ConsiderEverything));
        }

        private const string UnmanagedCallersOnlyAttribute = @"
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public Type[] CallConvs;
        public string EntryPoint;
    }
}
";

        private const string UnmanagedCallersOnlyAttributeIl = @"
.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 40 00 00 00 01 00 54 02 09 49 6e 68 65 72
        69 74 65 64 00
    )
    .field public class [mscorlib]System.Type[] CallConvs
    .field public string EntryPoint

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    }
}
";

        [Fact]
        public void UnmanagedCallersOnlyRequiresStatic()
        {
            var comp = CreateCompilation(new[] { @"
#pragma warning disable 8321 // Unreferenced local function
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    void M1() {}

    public void M2()
    {
        [UnmanagedCallersOnly]
        void local() {}
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (6,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(6, 6),
                // (11,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(11, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyAllowedOnStatics()
        {
            var comp = CreateCompilation(new[] { @"
#pragma warning disable 8321 // Unreferenced local function
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    static void M1() {}

    public void M2()
    {
        [UnmanagedCallersOnly]
        static void local() {}
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsMustComeFromCorrectNamespace()
        {
            var comp = CreateEmptyCompilation(new[] { @"
using System.Runtime.InteropServices;
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public abstract partial class Enum : ValueType {}
    public class String { }
    public struct Boolean { }
    public struct Int32 { }
    public class Type { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) {}
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { Method = 0x0040, }
}
class CallConvTest
{
}
class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })]
    static void M() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (26,6): error CS8893: 'CallConvTest' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })").WithArguments("CallConvTest").WithLocation(26, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsMustNotBeNestedType()
        {
            var comp = CreateEmptyCompilation(new[] { @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public abstract partial class Enum : ValueType {}
    public class String { }
    public struct Boolean { }
    public struct Int32 { }
    public class Type { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) {}
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { Method = 0x0040, }
    namespace Runtime.CompilerServices
    {
        public class CallConvTestA
        {
            public class CallConvTestB { }
        }
    }
}
class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTestA.CallConvTestB) })]
    static void M() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (31,6): error CS8893: 'CallConvTestA.CallConvTestB' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTestA.CallConvTestB) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTestA.CallConvTestB) })").WithArguments("System.Runtime.CompilerServices.CallConvTestA.CallConvTestB").WithLocation(31, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsMustComeFromCorelib()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace System.Runtime.CompilerServices
{
    class CallConvTest
    {
    }
}
class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })]
    static void M() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (12,6): error CS8893: 'CallConvTest' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvTest) })").WithArguments("System.Runtime.CompilerServices.CallConvTest").WithLocation(12, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsMustStartWithCallConv()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(ExtensionAttribute) })]
    static void M() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (6,6): error CS8893: 'ExtensionAttribute' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.ExtensionAttribute) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(ExtensionAttribute) })").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(6, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvNull_InSource()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly(CallConvs = new System.Type[] { null })]
    static void M() {}

    unsafe static void M1()
    {
        delegate* unmanaged<void> ptr = &M;
    }
}
");

            comp.VerifyDiagnostics(
                // (5,6): error CS8893: 'null' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new System.Type[] { null })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new System.Type[] { null })").WithArguments("null").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvNull_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static void M () cil managed 
    {
        // [UnmanagedCallersOnly(CallConvs = new Type[] { null })]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
            73 01 00 00 00 ff
        )

        ret
    }
} 
";

            var comp = CreateCompilationWithFunctionPointersAndIl(@"
class D
{
    unsafe static void M1()
    {
        C.M();
        delegate* unmanaged<void> ptr = &C.M;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,9): error CS8901: 'C.M()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         C.M();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "C.M()").WithArguments("C.M()").WithLocation(6, 9)
            );

            var c = comp.GetTypeByMetadataName("C");
            var m1 = c.GetMethod("M");
            var unmanagedData = m1.GetUnmanagedCallersOnlyAttributeData(forceComplete: true);
            Assert.NotSame(unmanagedData, UnmanagedCallersOnlyAttributeData.Uninitialized);
            Assert.NotSame(unmanagedData, UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound);
            Assert.Empty(unmanagedData!.CallingConventionTypes);
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvDefault()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly(CallConvs = new System.Type[] { default })]
    static void M() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8893: 'null' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new System.Type[] { default })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new System.Type[] { default })").WithArguments("null").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_Errors()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    static string M1() => throw null;

    [UnmanagedCallersOnly]
    static void M2(object o) {}

    [UnmanagedCallersOnly]
    static T M3<T>() => throw null;

    [UnmanagedCallersOnly]
    static void M4<T>(T t) {}

    [UnmanagedCallersOnly]
    static T M5<T>() where T : struct => throw null;

    [UnmanagedCallersOnly]
    static void M6<T>(T t) where T : struct {}

    [UnmanagedCallersOnly]
    static T M7<T>() where T : unmanaged => throw null;

    [UnmanagedCallersOnly]
    static void M8<T>(T t) where T : unmanaged {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (6,12): error CS8894: Cannot use 'string' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     static string M1() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "string").WithArguments("string", "return").WithLocation(6, 12),
                // (9,20): error CS8894: Cannot use 'object' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     static void M2(object o) {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "object o").WithArguments("object", "parameter").WithLocation(9, 20),
                // (11,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(11, 6),
                // (12,12): error CS8894: Cannot use 'T' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     static T M3<T>() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "T").WithArguments("T", "return").WithLocation(12, 12),
                // (14,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(14, 6),
                // (15,23): error CS8894: Cannot use 'T' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     static void M4<T>(T t) {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "T t").WithArguments("T", "parameter").WithLocation(15, 23),
                // (17,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(17, 6),
                // (18,12): error CS8894: Cannot use 'T' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     static T M5<T>() where T : struct => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "T").WithArguments("T", "return").WithLocation(18, 12),
                // (20,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(20, 6),
                // (21,23): error CS8894: Cannot use 'T' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     static void M6<T>(T t) where T : struct {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "T t").WithArguments("T", "parameter").WithLocation(21, 23),
                // (23,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(23, 6),
                // (26,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(26, 6)
            );
        }

        [Fact, WorkItem(57025, "https://github.com/dotnet/roslyn/issues/57025")]
        public void UnmanagedCallersOnlyRequiresNonRef_Errors()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    static ref int M1() => throw null;

    [UnmanagedCallersOnly]
    static ref readonly int M2() => throw null;

    [UnmanagedCallersOnly]
    static void M3(ref int o) => throw null;

    [UnmanagedCallersOnly]
    static void M4(in int o) => throw null;

    [UnmanagedCallersOnly]
    static void M5(out int o) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (6,12): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     static ref int M1() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "ref int").WithLocation(6, 12),
                // (9,12): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     static ref readonly int M2() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "ref readonly int").WithLocation(9, 12),
                // (12,20): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     static void M3(ref int o) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "ref int o").WithLocation(12, 20),
                // (15,20): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     static void M4(in int o) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "in int o").WithLocation(15, 20),
                // (18,20): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     static void M5(out int o) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "out int o").WithLocation(18, 20)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_Valid()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
#pragma warning disable CS0169 // unused private field
struct S
{
    private int _field;
}
class C
{
    [UnmanagedCallersOnly]
    static int M1() => throw null;

    [UnmanagedCallersOnly]
    static void M2(int o) {}

    [UnmanagedCallersOnly]
    static S M3() => throw null;

    [UnmanagedCallersOnly]
    public static void M4(S s) {}

    [UnmanagedCallersOnly]
    static int? M5() => throw null;

    [UnmanagedCallersOnly]
    static void M6(int? o) {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_MethodWithGenericParameter()
        {
            var comp = CreateCompilation(new[] { @"
#pragma warning disable CS8321 // Unused local function
using System.Runtime.InteropServices;
public struct S<T> where T : unmanaged
{
    public T t;
}
class C
{
    [UnmanagedCallersOnly] // 1
    static S<T> M1<T>() where T : unmanaged => throw null;

    [UnmanagedCallersOnly] // 2
    static void M2<T>(S<T> o) where T : unmanaged {}

    static void M3<T>()
    {
        [UnmanagedCallersOnly] // 3
        static void local1() {}

        static void local2()
        {
            [UnmanagedCallersOnly] // 4
            static void local3() { }
        }

        System.Action a = () =>
        {
            [UnmanagedCallersOnly] // 5
            static void local4() { }
        };
    }

    static void M4()
    {
        [UnmanagedCallersOnly] // 6
        static void local2<T>() {}
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (10,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 1
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(10, 6),
                // (13,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 2
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(13, 6),
                // (18,10): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //         [UnmanagedCallersOnly] // 3
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(18, 10),
                // (23,14): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //             [UnmanagedCallersOnly] // 4
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(23, 14),
                // (29,14): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //             [UnmanagedCallersOnly] // 5
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(29, 14),
                // (36,10): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //         [UnmanagedCallersOnly] // 6
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(36, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiredUnmanagedTypes_MethodWithGenericParameter_InIl()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig static void M<T> () cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ret
    }
}
";

            var comp = CreateCompilationWithFunctionPointersAndIl(@"
unsafe
{
    delegate* unmanaged<void> ptr = C.M<int>;
}", il, options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (4,37): error CS0570: 'C.M<T>()' is not supported by the language
                //     delegate* unmanaged<void> ptr = C.M<int>;
                Diagnostic(ErrorCode.ERR_BindToBogus, "C.M<int>").WithArguments("C.M<T>()").WithLocation(4, 37)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_TypeWithGenericParameter()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S<T> where T : unmanaged
{
    public T t;
}
class C<T> where T : unmanaged
{
    [UnmanagedCallersOnly] // 1
    static S<T> M1() => throw null;

    [UnmanagedCallersOnly] // 2
    static void M2(S<T> o) {}

    [UnmanagedCallersOnly] // 3
    static S<int> M3() => throw null;

    [UnmanagedCallersOnly] // 4
    static void M4(S<int> o) {}

    class C2
    {
        [UnmanagedCallersOnly] // 5
        static void M5() {}
    }

    struct S2
    {
        [UnmanagedCallersOnly] // 6
        static void M6() {}
    }

#pragma warning disable CS8321 // Unused local function
    static void M7()
    {
        [UnmanagedCallersOnly] // 7
        static void local1() { }
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (9,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 1
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(9, 6),
                // (12,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 2
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(12, 6),
                // (15,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 3
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(15, 6),
                // (18,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly] // 4
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(18, 6),
                // (23,10): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //         [UnmanagedCallersOnly] // 5
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(23, 10),
                // (29,10): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //         [UnmanagedCallersOnly] // 6
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(29, 10),
                // (36,10): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //         [UnmanagedCallersOnly] // 7
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(36, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_TypeWithGenericParameter_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C`1<T> extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi beforefieldinit NestedClass<T> extends [mscorlib]System.Object
    {
        .method public hidebysig static void M2 () cil managed 
        {
            .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
                01 00 00 00
            )
            ret
        }
    }

    .class nested public sequential ansi sealed beforefieldinit NestedStruct<T> extends [mscorlib]System.ValueType
    {
        .pack 0
        .size 1

        // Methods
        .method public hidebysig static void M3 () cil managed 
        {
            .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
                01 00 00 00
            )
            ret
        }
    }

    .method public hidebysig static void M1 () cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ret
    }
}
";

            var comp = CreateCompilationWithFunctionPointersAndIl(@"
unsafe
{
    delegate* unmanaged<void> ptr1 = &C<int>.M1;
    delegate* unmanaged<void> ptr2 = &C<int>.NestedClass.M2;
    delegate* unmanaged<void> ptr3 = &C<int>.NestedStruct.M3;
}
", il, options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (4,39): error CS0570: 'C<T>.M1()' is not supported by the language
                //     delegate* unmanaged<void> ptr1 = &C<int>.M1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "C<int>.M1").WithArguments("C<T>.M1()").WithLocation(4, 39),
                // (5,39): error CS0570: 'C<T>.NestedClass.M2()' is not supported by the language
                //     delegate* unmanaged<void> ptr2 = &C<int>.NestedClass.M2;
                Diagnostic(ErrorCode.ERR_BindToBogus, "C<int>.NestedClass.M2").WithArguments("C<T>.NestedClass.M2()").WithLocation(5, 39),
                // (6,39): error CS0570: 'C<T>.NestedStruct.M3()' is not supported by the language
                //     delegate* unmanaged<void> ptr3 = &C<int>.NestedStruct.M3;
                Diagnostic(ErrorCode.ERR_BindToBogus, "C<int>.NestedStruct.M3").WithArguments("C<T>.NestedStruct.M3()").WithLocation(6, 39)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_TypeAndMethodWithGenericParameter()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C<T1>
{
    [UnmanagedCallersOnly]
    static void M<T2>() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8895: Methods attributed with 'UnmanagedCallersOnly' cannot have generic type parameters and cannot be declared in a generic type.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, "UnmanagedCallersOnly").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_StructWithGenericParameters_1()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S<T>
{
    public T t;
}
class C
{
    [UnmanagedCallersOnly]
    static S<int> M1() => throw null;

    [UnmanagedCallersOnly]
    static void M2(S<int> o) {}

    [UnmanagedCallersOnly]
    static S<S<int>> M2() => throw null;

    [UnmanagedCallersOnly]
    static void M3(S<S<int>> o) {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyRequiresUnmanagedTypes_StructWithGenericParameters_2()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S<T>
{
    public T t;
}
class C
{
    [UnmanagedCallersOnly]
    static S<object> M1() => throw null;

    [UnmanagedCallersOnly]
    static void M2(S<object> o) {}

    [UnmanagedCallersOnly]
    static S<S<object>> M2() => throw null;

    [UnmanagedCallersOnly]
    static void M3(S<S<object>> o) {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (10,12): error CS8894: Cannot use 'S<object>' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     static S<object> M1() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "S<object>").WithArguments("S<object>", "return").WithLocation(10, 12),
                // (13,20): error CS8894: Cannot use 'S<object>' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     static void M2(S<object> o) {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "S<object> o").WithArguments("S<object>", "parameter").WithLocation(13, 20),
                // (16,12): error CS8894: Cannot use 'S<S<object>>' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     static S<S<object>> M2() => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "S<S<object>>").WithArguments("S<S<object>>", "return").WithLocation(16, 12),
                // (19,20): error CS8894: Cannot use 'S<S<object>>' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     static void M3(S<S<object>> o) {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "S<S<object>> o").WithArguments("S<S<object>>", "parameter").WithLocation(19, 20)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCannotCallMethodDirectly()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly]
    public static void M1() { }

    public static unsafe void M2()
    {
        M1();
        delegate*<void> p1 = &M1;
        delegate* unmanaged<void> p2 = &M1;
    }
}
");

            comp.VerifyDiagnostics(
                // (10,9): error CS8901: 'C.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         M1();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "M1()", isSuppressed: false).WithArguments("C.M1()").WithLocation(10, 9),
                // (11,31): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Default'.
                //         delegate*<void> p1 = &M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M1", isSuppressed: false).WithArguments("C.M1()", "Default").WithLocation(11, 31)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCannotCallMethodDirectlyWithAlias()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.InteropServices;
using E = D;
public class C
{
    public static unsafe void M2()
    {
        E.M1();
        delegate*<void> p1 = &E.M1;
        delegate* unmanaged<void> p2 = &E.M1;
    }
}
public class D
{
    [UnmanagedCallersOnly]
    public static void M1() { }

}
");

            comp.VerifyDiagnostics(
                // (8,9): error CS8901: 'D.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         E.M1();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "E.M1()", isSuppressed: false).WithArguments("D.M1()").WithLocation(8, 9),
                // (9,31): error CS8786: Calling convention of 'D.M1()' is not compatible with 'Default'.
                //         delegate*<void> p1 = &E.M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "E.M1", isSuppressed: false).WithArguments("D.M1()", "Default").WithLocation(9, 31)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCannotCallMethodDirectlyWithUsingStatic()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.InteropServices;
using static D;
public class C
{
    public static unsafe void M2()
    {
        M1();
        delegate*<void> p1 = &M1;
        delegate* unmanaged<void> p2 = &M1;
    }
}
public class D
{
    [UnmanagedCallersOnly]
    public static void M1() { }

}
");

            comp.VerifyDiagnostics(
                // (8,9): error CS8901: 'D.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         M1();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "M1()", isSuppressed: false).WithArguments("D.M1()").WithLocation(8, 9),
                // (9,31): error CS8786: Calling convention of 'D.M1()' is not compatible with 'Default'.
                //         delegate*<void> p1 = &M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M1", isSuppressed: false).WithArguments("D.M1()", "Default").WithLocation(9, 31)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyReferencedFromMetadata()
        {
            var comp0 = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly]
    public static void M1() { }
}
", UnmanagedCallersOnlyAttribute });

            validate(comp0.ToMetadataReference());
            validate(comp0.EmitToImageReference());

            static void validate(MetadataReference reference)
            {
                var comp1 = CreateCompilationWithFunctionPointers(@"
class D
{
    public static unsafe void M2()
    {
        C.M1();
        delegate*<void> p1 = &C.M1;
        delegate* unmanaged<void> p2 = &C.M1;
    }
}
", references: new[] { reference }, targetFramework: TargetFramework.Standard);

                comp1.VerifyDiagnostics(
                    // (6,9): error CS8901: 'C.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                    //         C.M1();
                    Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "C.M1()").WithArguments("C.M1()").WithLocation(6, 9),
                    // (7,31): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Default'.
                    //         delegate*<void> p1 = &C.M1;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M1", isSuppressed: false).WithArguments("C.M1()", "Default").WithLocation(7, 31),
                    // (8,19): error CS8889: The target runtime doesn't support extensible or runtime-environment default calling conventions.
                    //         delegate* unmanaged<void> p2 = &C.M1;
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, "unmanaged").WithLocation(8, 19)
                );
            }
        }

        [Fact]
        public void UnmanagedCallersOnlyReferencedFromMetadata_BadTypeInList()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static 
        void M1 () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(object) })]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 01 00 53 1d 50 09 43 61 6c 6c 43 6f 6e 76
            73 01 00 00 00 68 53 79 73 74 65 6d 2e 4f 62 6a
            65 63 74 2c 20 53 79 73 74 65 6d 2e 50 72 69 76
            61 74 65 2e 43 6f 72 65 4c 69 62 2c 20 56 65 72
            73 69 6f 6e 3d 34 2e 30 2e 30 2e 30 2c 20 43 75
            6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20 50
            75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 37 63
            65 63 38 35 64 37 62 65 61 37 37 39 38 65
        )
        ret
    }
}";

            var comp = CreateCompilationWithFunctionPointersAndIl(@"
class D
{
    public unsafe static void M2()
    {
        C.M1();
        delegate* unmanaged<void> ptr = &C.M1;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,9): error CS8901: 'C.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         C.M1();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "C.M1()").WithArguments("C.M1()").WithLocation(6, 9)
            );

            var c = comp.GetTypeByMetadataName("C");
            var m1 = c.GetMethod("M1");
            var unmanagedData = m1.GetUnmanagedCallersOnlyAttributeData(forceComplete: true);
            Assert.NotSame(unmanagedData, UnmanagedCallersOnlyAttributeData.Uninitialized);
            Assert.NotSame(unmanagedData, UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound);
            Assert.Empty(unmanagedData!.CallingConventionTypes);
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnInstanceMethod()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig 
        instance void M1 () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ret
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c)
    {
        c.M1();
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,11): error CS0570: 'C.M1()' is not supported by the language
                //         c.M1();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M1").WithArguments("C.M1()").WithLocation(6, 11)
            );
        }

        [Fact]
        [WorkItem(54113, "https://github.com/dotnet/roslyn/issues/54113")]
        public void UnmanagedCallersOnlyDefinedOnConversion()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        int32 op_Implicit (
            class C i
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";
            var comp = CreateCompilationWithIL(@"
class Test
{
    void M(C x)
    {
        _ = (int)x;
    }
}
", il);

            comp.VerifyDiagnostics(
                    // (6,13): error CS0570: 'C.implicit operator int(C)' is not supported by the language
                    //         _ = (int)x;
                    Diagnostic(ErrorCode.ERR_BindToBogus, "(int)x").WithArguments("C.implicit operator int(C)").WithLocation(6, 13)
            );
        }

        [Fact]
        [WorkItem(54113, "https://github.com/dotnet/roslyn/issues/54113")]
        public void UnmanagedCallersOnlyDefinedOnConversion_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C1
{
    [UnmanagedCallersOnly]
    public static implicit operator int(C1 c) => throw null;
}
class C2
{
    [UnmanagedCallersOnly]
    public static explicit operator int(C2 c) => throw null;
}
class Test
{
    void M(C1 x, C2 y)
    {
        _ = (int)x;
        _ = (int)y;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(5, 6),
                // (10,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(10, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnProperty_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    static int Prop
    {
        [UnmanagedCallersOnly] get => throw null;
        [UnmanagedCallersOnly] set => throw null;
    }
    static void M()
    {
        Prop = 1;
        _ = Prop;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (7,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly] get => throw null;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(7, 10),
                // (8,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly] set => throw null;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(8, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnProperty_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig specialname static 
        int32 get_Prop () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .method public hidebysig specialname static 
        void set_Prop (
            int32 'value'
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .property int32 Prop()
    {
        .get int32 C::get_Prop()
        .set void C::set_Prop(int32)
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2()
    {
        C.Prop = 1;
        _ = C.Prop;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,11): error CS0570: 'C.Prop.set' is not supported by the language
                //         C.Prop = 1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Prop").WithArguments("C.Prop.set").WithLocation(6, 11),
                // (7,15): error CS0570: 'C.Prop.get' is not supported by the language
                //         _ = C.Prop;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Prop").WithArguments("C.Prop.get").WithLocation(7, 15)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnPropertyRefReadonlyGetterAsLvalue_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    static ref int Prop { [UnmanagedCallersOnly] get => throw null; }
    static void M()
    {
        Prop = 1;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,28): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     static int Prop { [UnmanagedCallersOnly] get {} }
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(5, 28)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnPropertyRefReadonlyGetterAsLvalue_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig specialname static 
        int32& get_Prop () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )

        ldnull
        throw
    } // end of method C::get_Prop

    // Properties
    .property int32& Prop()
    {
        .get int32& C::get_Prop()
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2()
    {
        C.Prop = 1;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,11): error CS0570: 'C.Prop.get' is not supported by the language
                //         C.Prop = 1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "Prop").WithArguments("C.Prop.get").WithLocation(6, 11)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnIndexer_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance void set_Item (
            int32 i,
            int32 'value'
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        nop
        ret
    } // end of method C::set_Item

    .method public hidebysig specialname 
        instance int32 get_Item (
            int32 i
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    } // end of method C::get_Item

    // Properties
    .property instance int32 Item(
        int32 i
    )
    {
        .get instance int32 C::get_Item(int32)
        .set instance void C::set_Item(int32, int32)
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c)
    {
        c[1] = 1;
        _ = c[0];
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,10): error CS0570: 'C.this[int].set' is not supported by the language
                //         c[1] = 1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "[1]").WithArguments("C.this[int].set").WithLocation(6, 10),
                // (7,14): error CS0570: 'C.this[int].get' is not supported by the language
                //         _ = c[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "[0]").WithArguments("C.this[int].get").WithLocation(7, 14)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnIndexer_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    public int this[int i]
    { 
        [UnmanagedCallersOnly] set => throw null;
        [UnmanagedCallersOnly] get => throw null;
    }
    static void M(C c)
    {
        c[1] = 1;
        _ = c[0];
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (7,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly] set => throw null;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(7, 10),
                // (8,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly] get => throw null;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(8, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnIndexerRefReturnAsLvalue_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance int32& get_Item (
            int32 i
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    } // end of method C::get_Item

    // Properties
    .property instance int32& Item(
        int32 i
    )
    {
        .get instance int32& C::get_Item(int32)
    }

} 
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c)
    {
        c[1] = 1;
        _ = c[0];
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,10): error CS0570: 'C.this[int].get' is not supported by the language
                //         c[1] = 1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "[1]").WithArguments("C.this[int].get").WithLocation(6, 10),
                // (7,14): error CS0570: 'C.this[int].get' is not supported by the language
                //         _ = c[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "[0]").WithArguments("C.this[int].get").WithLocation(7, 14)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnBinaryOperator_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    public static C operator +(C c1, C c2) => null;
    static void M(C c1, C c2)
    {
        _ = c1 + c2;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnBinaryOperator_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        class C op_Addition (
            class C c1,
            class C c2
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        ret
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c1, C c2)
    {
        _ = c1 + c2;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,13): error CS0570: 'C.operator +(C, C)' is not supported by the language
                //         _ = c1 + c2;
                Diagnostic(ErrorCode.ERR_BindToBogus, "c1 + c2").WithArguments("C.operator +(C, C)").WithLocation(6, 13)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnUnaryOperator_InSource()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    public static C operator +(C c) => null;
    static void M(C c)
    {
        _ = +c;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDefinedOnUnaryOperator_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        class C op_UnaryPlus (
            class C c1
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        ret
    }
}
";
            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c1, C c2)
    {
        _ = +c1;
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,13): error CS0570: 'C.operator +(C)' is not supported by the language
                //         _ = +c1;
                Diagnostic(ErrorCode.ERR_BindToBogus, "+c1").WithArguments("C.operator +(C)").WithLocation(6, 13)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDeclaredOnGetEnumerator_InMetadata()
        {
            var il = UnmanagedCallersOnlyAttributeIl + @"
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig 
        instance class [mscorlib]System.Collections.Generic.IEnumerator`1<int32> GetEnumerator () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }
}
";

            var comp = CreateCompilationWithIL(@"
class D
{
    public static void M2(C c)
    {
        foreach (var i in c) {}
    }
}
", il);

            comp.VerifyDiagnostics(
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in c) {}
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDeclaredOnGetEnumeratorExtension()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S
{
    public static void M2(S s)
    {
        foreach (var i in s) {}
    }
}
public struct SEnumerator
{
    public bool MoveNext() => throw null;
    public int Current => throw null;
}
public static class CExt
{
    [UnmanagedCallersOnly]
    public static SEnumerator GetEnumerator(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (7,9): error CS8901: 'CExt.GetEnumerator(S)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         foreach (var i in s) {}
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "foreach").WithArguments("CExt.GetEnumerator(S)").WithLocation(7, 9)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDeclaredOnMoveNext()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S
{
    public static void M2(S s)
    {
        foreach (var i in s) {}
    }
}
public struct SEnumerator
{
    [UnmanagedCallersOnly]
    public bool MoveNext() => throw null;
    public int Current => throw null;
}
public static class CExt
{
    public static SEnumerator GetEnumerator(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (12,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(12, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyDeclaredOnPatternDispose()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public struct S
{
    public static void M2(S s)
    {
        foreach (var i in s) {}
    }
}
public ref struct SEnumerator
{
    public bool MoveNext() => throw null;
    public int Current => throw null;
    [UnmanagedCallersOnly]
    public void Dispose() => throw null;
}
public static class CExt
{
    public static SEnumerator GetEnumerator(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (14,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(14, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCannotCaptureToDelegate()
        {
            var comp = CreateCompilation(new[] { @"
using System;
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly]
    public static void M1() { }

    public static void M2()
    {
        Action a = M1;
        a = local;
        a = new Action(M1);
        a = new Action(local);

        [UnmanagedCallersOnly]
        static void local() {}
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (11,20): error CS8902: 'C.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         Action a = M1;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "M1").WithArguments("C.M1()").WithLocation(11, 20),
                // (12,13): error CS8902: 'local()' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         a = local;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "local").WithArguments("local()").WithLocation(12, 13),
                // (13,24): error CS8902: 'C.M1()' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         a = new Action(M1);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "M1").WithArguments("C.M1()").WithLocation(13, 24),
                // (14,24): error CS8902: 'local()' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         a = new Action(local);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "local").WithArguments("local()").WithLocation(14, 24)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCannotCaptureToDelegate_OverloadStillPicked()
        {
            var comp = CreateCompilation(new[] { @"
using System;
using System.Runtime.InteropServices;
public class C 
{
    [UnmanagedCallersOnly]
    public static void M(int s) { }

    public static void M(object o) { }

    void N()
    {
        Action<int> a = M;
    }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (13,25): error CS8902: 'C.M(int)' is attributed with 'UnmanagedCallersOnly' and cannot be converted to a delegate type. Obtain a function pointer to this method.
                //         Action<int> a = M;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate, "M").WithArguments("C.M(int)").WithLocation(13, 25)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnExtensionsCannotBeUsedDirectly()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;

struct S
{
    static void M(S s)
    {
        s.Extension();
        CExt.Extension(s);
    }
}
static class CExt
{
    [UnmanagedCallersOnly]
    public static void Extension(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (8,9): error CS8901: 'CExt.Extension(S)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         s.Extension();
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "s.Extension()").WithArguments("CExt.Extension(S)").WithLocation(8, 9),
                // (9,9): error CS8901: 'CExt.Extension(S)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         CExt.Extension(s);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "CExt.Extension(s)").WithArguments("CExt.Extension(S)").WithLocation(9, 9)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyExtensionDeconstructCannotBeUsedDirectly()
        {
            var comp = CreateCompilation(new[] { @"
using System.Collections.Generic;
using System.Runtime.InteropServices;

struct S
{
    static void M(S s, List<S> ls)
    {
        var (i1, i2) = s;
        (i1, i2) = s;
        foreach (var (_, _) in ls) { }
        _ = s is (int _, int _);
    }
}
static class CExt
{
    [UnmanagedCallersOnly]
    public static void Deconstruct(this S s, out int i1, out int i2) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (9,24): error CS8901: 'CExt.Deconstruct(S, out int, out int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         var (i1, i2) = s;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "s").WithArguments("CExt.Deconstruct(S, out int, out int)").WithLocation(9, 24),
                // (10,20): error CS8901: 'CExt.Deconstruct(S, out int, out int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         (i1, i2) = s;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "s").WithArguments("CExt.Deconstruct(S, out int, out int)").WithLocation(10, 20),
                // (11,32): error CS8901: 'CExt.Deconstruct(S, out int, out int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         foreach (var (_, _) in ls) { }
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "ls").WithArguments("CExt.Deconstruct(S, out int, out int)").WithLocation(11, 32),
                // (12,18): error CS8901: 'CExt.Deconstruct(S, out int, out int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         _ = s is (int _, int _);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "(int _, int _)").WithArguments("CExt.Deconstruct(S, out int, out int)").WithLocation(12, 18),
                // (18,46): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     public static void Deconstruct(this S s, out int i1, out int i2) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "out int i1").WithLocation(18, 46),
                // (18,58): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     public static void Deconstruct(this S s, out int i1, out int i2) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "out int i2").WithLocation(18, 58)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyExtensionAddCannotBeUsedDirectly()
        {
            var comp = CreateCompilation(new[] { @"
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

struct S : IEnumerable
{
    static void M(S s, List<S> ls)
    {
        _ = new S() { 1, 2, 3 };
    }

    public IEnumerator GetEnumerator() => throw null;
}
static class CExt
{
    [UnmanagedCallersOnly]
    public static void Add(this S s, int i) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (10,23): error CS8901: 'CExt.Add(S, int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         _ = new S() { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "1").WithArguments("CExt.Add(S, int)").WithLocation(10, 23),
                // (10,26): error CS8901: 'CExt.Add(S, int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         _ = new S() { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "2").WithArguments("CExt.Add(S, int)").WithLocation(10, 26),
                // (10,29): error CS8901: 'CExt.Add(S, int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         _ = new S() { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "3").WithArguments("CExt.Add(S, int)").WithLocation(10, 29)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyExtensionGetAwaiterCannotBeUsedDirectly()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;

struct S
{
    static async void M(S s)
    {
        await s;
    }
}
public struct Result : System.Runtime.CompilerServices.INotifyCompletion
{
    public int GetResult() => throw null;
    public void OnCompleted(System.Action continuation) => throw null;
    public bool IsCompleted => throw null;
}
static class CExt
{
    [UnmanagedCallersOnly]
    public static Result GetAwaiter(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (8,9): error CS8901: 'CExt.GetAwaiter(S)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         await s;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "await s").WithArguments("CExt.GetAwaiter(S)").WithLocation(8, 9)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyExtensionGetPinnableReferenceCannotBeUsedDirectly()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;

struct S
{
    static void M(S s)
    {
        unsafe
        {
            fixed (int* i = s)
            {

            }
        }
    }
}
static class CExt
{
    [UnmanagedCallersOnly]
    public static ref int GetPinnableReference(this S s) => throw null;
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (10,29): error CS8901: 'CExt.GetPinnableReference(S)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //             fixed (int* i = s)
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "s").WithArguments("CExt.GetPinnableReference(S)").WithLocation(10, 29),
                // (20,19): error CS8977: Cannot use 'ref', 'in', or 'out' in the signature of a method attributed with 'UnmanagedCallersOnly'.
                //     public static ref int GetPinnableReference(this S s) => throw null;
                Diagnostic(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, "ref int").WithLocation(20, 19)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnMain_1()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    public static void Main() {}
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (6,24): error CS8899: Application entry points cannot be attributed with 'UnmanagedCallersOnly'.
                //     public static void Main() {}
                Diagnostic(ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly, "Main").WithLocation(6, 24)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnMain_2()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    public static void Main() {}
}
class D
{
    [UnmanagedCallersOnly]
    public static void Main() {}
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (5,24): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                //     public static void Main() {}
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main").WithLocation(5, 24),
                // (10,24): error CS8899: Application entry points cannot be attributed with 'UnmanagedCallersOnly'.
                //     public static void Main() {}
                Diagnostic(ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly, "Main").WithLocation(10, 24)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnMain_3()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    public static void Main() {}
}
class D
{
    [UnmanagedCallersOnly]
    public static void Main() {}
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyOnMain_4()
        {
            var comp = CreateCompilation(new[] { @"
using System.Threading.Tasks;
using System.Runtime.InteropServices;
class C
{
    public static async Task Main() {}
}
class D
{
    [UnmanagedCallersOnly]
    public static async Task Main() {}
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (6,30): error CS0017: Program has more than one entry point defined. Compile with /main to specify the type that contains the entry point.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.ERR_MultipleEntryPoints, "Main").WithLocation(6, 30),
                // (6,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(6, 30),
                // (11,25): error CS8894: Cannot use 'Task' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "Task").WithArguments("System.Threading.Tasks.Task", "return").WithLocation(11, 25),
                // (11,30): error CS8899: Application entry points cannot be attributed with 'UnmanagedCallersOnly'.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly, "Main").WithLocation(11, 30),
                // (11,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(11, 30)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnMain_5()
        {
            var comp = CreateCompilation(new[] { @"
using System.Threading.Tasks;
using System.Runtime.InteropServices;
class C
{
    public static void Main() {}
}
class D
{
    [UnmanagedCallersOnly]
    public static async Task Main() {}
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (11,25): error CS8894: Cannot use 'Task' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "Task").WithArguments("System.Threading.Tasks.Task", "return").WithLocation(11, 25),
                // (11,30): warning CS8892: Method 'D.Main()' will not be used as an entry point because a synchronous entry point 'C.Main()' was found.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("D.Main()", "C.Main()").WithLocation(11, 30),
                // (11,30): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async Task Main() {}
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(11, 30)
            );
        }

        [Fact, WorkItem(47858, "https://github.com/dotnet/roslyn/issues/47858")]
        public void UnmanagedCallersOnlyOnMain_GetEntryPoint()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly]
    public static void Main()
    {
    }
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.ReleaseExe);

            var method = comp.GetEntryPoint(System.Threading.CancellationToken.None);
            Assert.Equal("void C.Main()", method.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (6,24): error CS8899: Application entry points cannot be attributed with 'UnmanagedCallersOnly'.
                //     public static void Main()
                Diagnostic(ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly, "Main", isSuppressed: false).WithLocation(6, 24)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyOnModuleInitializer()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }

public class C
{
    [UnmanagedCallersOnly, ModuleInitializer]
    public static void M1() {}

    [ModuleInitializer, UnmanagedCallersOnly]
    public static void M2() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (9,28): error CS8900: Module initializer cannot be attributed with 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly, ModuleInitializer]
                Diagnostic(ErrorCode.ERR_ModuleInitializerCannotBeUnmanagedCallersOnly, "ModuleInitializer").WithLocation(9, 28),
                // (12,6): error CS8900: Module initializer cannot be attributed with 'UnmanagedCallersOnly'.
                //     [ModuleInitializer, UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_ModuleInitializerCannotBeUnmanagedCallersOnly, "ModuleInitializer").WithLocation(12, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyMultipleApplications()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(string) })]
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(object) })]
    public static void M1() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8893: 'string' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(string) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(string) })").WithArguments("string").WithLocation(5, 6),
                // (6,6): error CS0579: Duplicate 'UnmanagedCallersOnly' attribute
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(object) })]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "UnmanagedCallersOnly").WithArguments("UnmanagedCallersOnly").WithLocation(6, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInDefinition_1()
        {
            var comp = CreateCompilation(@"
#nullable enable
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public Type[]? CallConvs;
        public string? EntryPoint;

        [UnmanagedCallersOnly]
        static void M() {}
    }
}
");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInDefinition_2()
        {
            var comp = CreateCompilation(@"
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(UnmanagedCallersOnlyAttribute) })]
        public UnmanagedCallersOnlyAttribute() { }
        public Type[] CallConvs;
    }
}
");

            comp.VerifyDiagnostics(
                // (7,10): error CS8893: 'UnmanagedCallersOnlyAttribute' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //         [UnmanagedCallersOnly(CallConvs = new[] { typeof(UnmanagedCallersOnlyAttribute) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(UnmanagedCallersOnlyAttribute) })").WithArguments("System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute").WithLocation(7, 10),
                // (7,10): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         [UnmanagedCallersOnly(CallConvs = new[] { typeof(UnmanagedCallersOnlyAttribute) })]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(7, 10)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInUsage_1()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(C) })]
    public static void Func() {}
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (5,6): error CS8893: 'C' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(C) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(C) })").WithArguments("C").WithLocation(5, 6)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInUsage_2()
        {
            var comp = CreateCompilation(new[] { @"
using System.Runtime.InteropServices;
class A
{
    struct B { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(B) })]
    static void F() { }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (6,6): error CS8893: 'A.B' is not a valid calling convention type for 'UnmanagedCallersOnly'.
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(B) })]
                Diagnostic(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, "UnmanagedCallersOnly(CallConvs = new[] { typeof(B) })").WithArguments("A.B").WithLocation(6, 6)
            );
        }

        [Fact]
        [WorkItem(47125, "https://github.com/dotnet/roslyn/issues/47125")]
        public void UnmanagedCallersOnlyWithLoopInUsage_3()
        {
            var comp = CreateCompilation(new[] { @"
#nullable enable
using System;
using System.Runtime.InteropServices;
class C
{
    [UnmanagedCallersOnly(CallConvs = F())]
    static Type[] F() { throw null!; }
}
", UnmanagedCallersOnlyAttribute });

            comp.VerifyDiagnostics(
                // (7,39): error CS8901: 'C.F()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //     [UnmanagedCallersOnly(CallConvs = F())]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "F()").WithArguments("C.F()").WithLocation(7, 39),
                // (7,39): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [UnmanagedCallersOnly(CallConvs = F())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "F()").WithLocation(7, 39)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInUsage_4()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Runtime.InteropServices;
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
unsafe class Attr : Attribute
{
    public Attr(delegate*<void> d) {}
}
unsafe class C
{
    [UnmanagedCallersOnly]
    [Attr(&M1)]
    static void M1()
    {
    }
}
");

            comp.VerifyDiagnostics(
                // (12,6): error CS0181: Attribute constructor parameter 'd' has type 'delegate*<void>', which is not a valid attribute parameter type
                //     [Attr(&M1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Attr", isSuppressed: false).WithArguments("d", "delegate*<void>").WithLocation(12, 6)
            );
        }

        [Fact]
        [WorkItem(47125, "https://github.com/dotnet/roslyn/issues/47125")]
        public void UnmanagedCallersOnlyWithLoopInUsage_5()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Runtime.InteropServices;
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
class Attr : Attribute
{
    public Attr(int i) {}
}
unsafe class C
{
    [UnmanagedCallersOnly]
    [Attr(F())]
    static int F()
    {
        return 0;
    }
}
");

            comp.VerifyDiagnostics(
                // (12,11): error CS8901: 'C.F()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //     [Attr(F())]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "F()", isSuppressed: false).WithArguments("C.F()").WithLocation(12, 11),
                // (12,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [Attr(F())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "F()", isSuppressed: false).WithLocation(12, 11)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInUsage_6()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public unsafe class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    static void F(int i = G(&F)) { }
    static int G(delegate*unmanaged[Fastcall]<int, void> d) => 0;
}
");

            comp.VerifyDiagnostics(
                // (7,27): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //     static void F(int i = G(&F)) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "G(&F)", isSuppressed: false).WithArguments("i").WithLocation(7, 27)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyWithLoopInUsage_7()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public unsafe class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    static int F(int i = F()) => 0;
}
");

            comp.VerifyDiagnostics(
                // (7,26): error CS8901: 'C.F(int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //     static int F(int i = F()) => 0;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "F()", isSuppressed: false).WithArguments("C.F(int)").WithLocation(7, 26),
                // (7,26): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //     static int F(int i = F()) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "F()", isSuppressed: false).WithArguments("i").WithLocation(7, 26)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyUnrecognizedConstructor()
        {
            var comp = CreateCompilation(@"
using System.Runtime.InteropServices;
public class C
{
    // Invalid typeof for the regular constructor, non-static method
    [UnmanagedCallersOnly(CallConvs: new[] { typeof(string) })]
    public void M() {}
}

#nullable enable
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute(Type[]? CallConvs)
        {
        }

        public string? EntryPoint;
    }
}
");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsWithADifferentType()
        {
            var definition = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static void M1() {}
}
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public string EntryPoint;
        public object CallConvs;
    }
}
";

            var usage = @"
class D
{
    unsafe void M2()
    {
        delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
    }
}
";

            var allInOne = CreateCompilationWithFunctionPointers(definition + usage, includeUnmanagedCallersOnly: false);

            allInOne.VerifyDiagnostics(
                // (27,51): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Standard'.
                //         delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M1").WithArguments("C.M1()", "Standard").WithLocation(27, 51)
            );

            var definitionComp = CreateCompilation(definition);

            var usageComp = CreateCompilationWithFunctionPointers(usage, new[] { definitionComp.EmitToImageReference() });
            usageComp.VerifyDiagnostics(
                // (6,51): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Standard'.
                //         delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M1").WithArguments("C.M1()", "Standard").WithLocation(6, 51)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCallConvsWithADifferentType_2()
        {
            var definition = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static void M1() {}
}
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public string EntryPoint;
        public object[] CallConvs;
    }
}
";

            var usage = @"
class D
{
    unsafe void M2()
    {
        delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
    }
}
";

            var allInOne = CreateCompilationWithFunctionPointers(definition + usage, includeUnmanagedCallersOnly: false);

            allInOne.VerifyDiagnostics(
                // (27,51): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Standard'.
                //         delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M1").WithArguments("C.M1()", "Standard").WithLocation(27, 51)
            );

            var definitionComp = CreateCompilation(definition);

            var usageComp = CreateCompilationWithFunctionPointers(usage, new[] { definitionComp.EmitToImageReference() });
            usageComp.VerifyDiagnostics(
                // (6,51): error CS8786: Calling convention of 'C.M1()' is not compatible with 'Standard'.
                //         delegate* unmanaged[Stdcall]<void> ptr = &C.M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M1").WithArguments("C.M1()", "Standard").WithLocation(6, 51)
            );
        }

        [Fact]
        public void UnmanagedCallersOnly_CallConvsAsProperty()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public Type[] CallConvs { get; set; }
        public string EntryPoint;
    }
}

public class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void M() {}
}";
            string source2 = @"
class D
{
    unsafe void M2()
    {
        delegate* unmanaged<void> ptr1 = &C.M;
        delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
    }
}";
            var sameComp = CreateCompilationWithFunctionPointers(source1 + source2, includeUnmanagedCallersOnly: false);
            sameComp.VerifyDiagnostics(
                // (28,50): error CS8786: Calling convention of 'C.M()' is not compatible with 'CDecl'.
                //         delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "CDecl").WithLocation(28, 50)
            );

            verifyUnmanagedData(sameComp);

            var refComp = CreateCompilation(source1);

            var differentComp = CreateCompilationWithFunctionPointers(source2, new[] { refComp.EmitToImageReference() });
            differentComp.VerifyDiagnostics(
                // (7,50): error CS8786: Calling convention of 'C.M()' is not compatible with 'CDecl'.
                //         delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "CDecl").WithLocation(7, 50)
            );

            verifyUnmanagedData(differentComp);

            static void verifyUnmanagedData(CSharpCompilation compilation)
            {
                var c = compilation.GetTypeByMetadataName("C");
                var m = c.GetMethod("M");
                Assert.Empty(m.GetUnmanagedCallersOnlyAttributeData(forceComplete: true)!.CallingConventionTypes);
            }
        }

        [Fact]
        public void UnmanagedCallersOnly_UnrecognizedSignature()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute(Type[] CallConvs)
        {
        }

        public string EntryPoint;
    }
}

public class C
{
    [UnmanagedCallersOnly(CallConvs: new[] { typeof(CallConvCdecl) })]
    public static void M() {}
}";
            string source2 = @"
class D
{
    unsafe void M2()
    {
        delegate* unmanaged<void> ptr1 = &C.M;
        delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
        delegate*<void> ptr3 = &C.M;
    }
}";
            var sameComp = CreateCompilationWithFunctionPointers(source1 + source2, includeUnmanagedCallersOnly: false);
            sameComp.VerifyDiagnostics(
                // (26,43): error CS8786: Calling convention of 'C.M()' is not compatible with 'Unmanaged'.
                //         delegate* unmanaged<void> ptr1 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "Unmanaged").WithLocation(26, 43),
                // (27,50): error CS8786: Calling convention of 'C.M()' is not compatible with 'CDecl'.
                //         delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "CDecl").WithLocation(27, 50)
            );

            verifyUnmanagedData(sameComp);

            var refComp = CreateCompilation(source1);

            var differentComp = CreateCompilationWithFunctionPointers(source2, new[] { refComp.EmitToImageReference() });
            differentComp.VerifyDiagnostics(
                // (6,43): error CS8786: Calling convention of 'C.M()' is not compatible with 'Unmanaged'.
                //         delegate* unmanaged<void> ptr1 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "Unmanaged").WithLocation(6, 43),
                // (7,50): error CS8786: Calling convention of 'C.M()' is not compatible with 'CDecl'.
                //         delegate* unmanaged[Cdecl]<void> ptr2 = &C.M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "CDecl").WithLocation(7, 50)
            );

            verifyUnmanagedData(differentComp);

            static void verifyUnmanagedData(CSharpCompilation compilation)
            {
                var c = compilation.GetTypeByMetadataName("C");
                var m = c.GetMethod("M");
                Assert.Null(m.GetUnmanagedCallersOnlyAttributeData(forceComplete: true));
            }
        }

        [Fact]
        public void UnmanagedCallersOnly_PropertyAndFieldNamedCallConvs()
        {
            var il = @"
.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 40 00 00 00 01 00 54 02 09 49 6e 68 65 72
        69 74 65 64 00
    )
    // Fields
    .field public class [mscorlib]System.Type[] CallConvs
    .field public string EntryPoint

    // Methods
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    } // end of method UnmanagedCallersOnlyAttribute::.ctor

    .method public hidebysig specialname instance class [mscorlib]System.Type[] get_CallConvs () cil managed
    {
        ldnull
        ret
    } // end of method UnmanagedCallersOnlyAttribute::get_CallConvs

    .method public hidebysig specialname instance void set_CallConvs (
            class [mscorlib]System.Type[] 'value'
        ) cil managed 
    {
        ret
    } // end of method UnmanagedCallersOnlyAttribute::set_CallConvs

    // Properties
    .property instance class [mscorlib]System.Type[] CallConvs()
    {
        .get instance class [mscorlib]System.Type[] System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::get_CallConvs()
        .set instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::set_CallConvs(class [mscorlib]System.Type[])
}

}
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static void M () cil managed
    {
        // As separate field/property assignments. Property is first.
        // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, CallConvs = new[] { typeof(CallConvCdecl) })]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 02 00 54 1d 50 09 43 61 6c 6c 43 6f 6e 76
            73 01 00 00 00 7c 53 79 73 74 65 6d 2e 52 75 6e
            74 69 6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65 72
            76 69 63 65 73 2e 43 61 6c 6c 43 6f 6e 76 53 74
            64 63 61 6c 6c 2c 20 6d 73 63 6f 72 6c 69 62 2c
            20 56 65 72 73 69 6f 6e 3d 34 2e 30 2e 30 2e 30
            2c 20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61
            6c 2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65
            6e 3d 62 37 37 61 35 63 35 36 31 39 33 34 65 30
            38 39 53 1d 50 09 43 61 6c 6c 43 6f 6e 76 73 01
            00 00 00 7a 53 79 73 74 65 6d 2e 52 75 6e 74 69
            6d 65 2e 43 6f 6d 70 69 6c 65 72 53 65 72 76 69
            63 65 73 2e 43 61 6c 6c 43 6f 6e 76 43 64 65 63
            6c 2c 20 6d 73 63 6f 72 6c 69 62 2c 20 56 65 72
            73 69 6f 6e 3d 34 2e 30 2e 30 2e 30 2c 20 43 75
            6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20 50
            75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 62 37
            37 61 35 63 35 36 31 39 33 34 65 30 38 39
        )

        ret
    } // end of method C::M
}
";

            var comp = CreateCompilationWithFunctionPointersAndIl(@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
unsafe class D
{
    static void M1()
    {
        delegate* unmanaged[Cdecl]<void> ptr1 = &C.M;
        delegate* unmanaged[Stdcall]<void> ptr2 = &C.M; // Error
        M2();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static void M2() {}
}
", il, includeUnmanagedCallersOnly: false);

            comp.VerifyDiagnostics(
                // (9,52): error CS8786: Calling convention of 'C.M()' is not compatible with 'Standard'.
                //         delegate* unmanaged[Stdcall]<void> ptr2 = &C.M; // Error
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "C.M").WithArguments("C.M()", "Standard").WithLocation(9, 52),
                // (13,27): error CS0229: Ambiguity between 'UnmanagedCallersOnlyAttribute.CallConvs' and 'UnmanagedCallersOnlyAttribute.CallConvs'
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                Diagnostic(ErrorCode.ERR_AmbigMember, "CallConvs").WithArguments("System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute.CallConvs", "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute.CallConvs").WithLocation(13, 27)
            );

            var c = comp.GetTypeByMetadataName("C");
            var m = c.GetMethod("M");
            var callConvCdecl = comp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvCdecl");

            Assert.True(callConvCdecl!.Equals((NamedTypeSymbol)m.GetUnmanagedCallersOnlyAttributeData(forceComplete: true)!.CallingConventionTypes.Single(), TypeCompareKind.ConsiderEverything));
        }

        [Fact]
        public void UnmanagedCallersOnly_BadExpressionInArguments()
        {

            var comp = CreateCompilationWithFunctionPointers(@"
using System.Runtime.InteropServices;
class A
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(Bad, Expression) })]
    static unsafe void F()
    {
        delegate*<void> ptr1 = &F;
        delegate* unmanaged<void> ptr2 = &F;
    }
}
");

            comp.VerifyDiagnostics(
                // (5,54): error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(Bad, Expression) })]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad", isSuppressed: false).WithArguments("Bad").WithLocation(5, 54),
                // (5,57): error CS1026: ) expected
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(Bad, Expression) })]
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ",", isSuppressed: false).WithLocation(5, 57),
                // (5,59): error CS0103: The name 'Expression' does not exist in the current context
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(Bad, Expression) })]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Expression", isSuppressed: false).WithArguments("Expression").WithLocation(5, 59),
                // (5,69): error CS1003: Syntax error, ',' expected
                //     [UnmanagedCallersOnly(CallConvs = new[] { typeof(Bad, Expression) })]
                Diagnostic(ErrorCode.ERR_SyntaxError, ")", isSuppressed: false).WithArguments(",").WithLocation(5, 69),
                // (9,43): error CS8786: Calling convention of 'A.F()' is not compatible with 'Unmanaged'.
                //         delegate* unmanaged<void> ptr2 = &F;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "F", isSuppressed: false).WithArguments("A.F()", "Unmanaged").WithLocation(9, 43)
            );
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData("CallConvs = null", 1)]
        [InlineData("CallConvs = new System.Type[0]", 1)]
        [InlineData("CallConvs = new[] { typeof(CallConvCdecl) }", 2)]
        [InlineData("CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvCdecl) }", 2)]
        [InlineData("CallConvs = new[] { typeof(CallConvThiscall) }", 3)]
        [InlineData("CallConvs = new[] { typeof(CallConvStdcall) }", 4)]
        [InlineData("CallConvs = new[] { typeof(CallConvFastcall) }", 5)]
        [InlineData("CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvThiscall) }", 6)]
        [InlineData("CallConvs = new[] { typeof(CallConvThiscall), typeof(CallConvCdecl) }", 6)]
        [InlineData("CallConvs = new[] { typeof(CallConvThiscall), typeof(CallConvCdecl), typeof(CallConvCdecl) }", 6)]
        [InlineData("CallConvs = new[] { typeof(CallConvFastcall), typeof(CallConvCdecl) }", -1)]
        [InlineData("CallConvs = new[] { typeof(CallConvThiscall), typeof(CallConvCdecl), typeof(CallConvStdcall) }", -1)]
        public void UnmanagedCallersOnlyAttribute_ConversionsToPointerType(string unmanagedCallersOnlyConventions, int diagnosticToSkip)
        {
            var comp = CreateCompilationWithFunctionPointers($@"
#pragma warning disable CS8019 // Unused using
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public unsafe class C
{{
    [UnmanagedCallersOnly({unmanagedCallersOnlyConventions})]
    public static void M()
    {{
        delegate*<void> ptrManaged = &M;
        delegate* unmanaged<void> ptrUnmanaged = &M;
        delegate* unmanaged[Cdecl]<void> ptrCdecl = &M;
        delegate* unmanaged[Thiscall]<void> ptrThiscall = &M;
        delegate* unmanaged[Stdcall]<void> ptrStdcall = &M;
        delegate* unmanaged[Fastcall]<void> ptrFastcall = &M;
        delegate* unmanaged[Cdecl, Thiscall]<void> ptrCdeclThiscall = &M;
    }}
}}
");

            List<DiagnosticDescription> diagnostics = new()
            {
                // (10,39): error CS8786: Calling convention of 'C.M()' is not compatible with 'Default'.
                //         delegate*<void> ptrManaged = &M;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "Default").WithLocation(10, 39)
            };

            if (diagnosticToSkip != 1)
            {
                diagnostics.Add(
                    // (11,25): error CS8786: Calling convention of 'C.M()' is not compatible with 'Unmanaged'.
                    //         ptrUnmanaged = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "Unmanaged").WithLocation(11, 51)
                    );
            }

            if (diagnosticToSkip != 2)
            {
                diagnostics.Add(
                    // (12,54): error CS8786: Calling convention of 'C.M()' is not compatible with 'CDecl'.
                    //         delegate* unmanaged[Cdecl]<void> ptrCdecl = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "CDecl").WithLocation(12, 54)
                    );
            }

            if (diagnosticToSkip != 3)
            {
                diagnostics.Add(
                    // (13,60): error CS8786: Calling convention of 'C.M()' is not compatible with 'ThisCall'.
                    //         delegate* unmanaged[Thiscall]<void> ptrThiscall = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "ThisCall").WithLocation(13, 60)
                    );
            }

            if (diagnosticToSkip != 4)
            {
                diagnostics.Add(
                    // (14,58): error CS8786: Calling convention of 'C.M()' is not compatible with 'Standard'.
                    //         delegate* unmanaged[Stdcall]<void> ptrStdcall = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "Standard").WithLocation(14, 58)
                    );
            }

            if (diagnosticToSkip != 5)
            {
                diagnostics.Add(
                    // (15,60): error CS8786: Calling convention of 'C.M()' is not compatible with 'FastCall'.
                    //         delegate* unmanaged[Fastcall]<void> ptrFastcall = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "FastCall").WithLocation(15, 60)
                    );
            }

            if (diagnosticToSkip != 6)
            {
                diagnostics.Add(
                    // (16,72): error CS8786: Calling convention of 'C.M()' is not compatible with 'Unmanaged'.
                    //         delegate* unmanaged[Cdecl, Thiscall]<void> ptrCdeclThiscall = &M;
                    Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M", isSuppressed: false).WithArguments("C.M()", "Unmanaged").WithLocation(16, 72)
                    );
            }

            comp.VerifyDiagnostics(diagnostics.ToArray());
        }

        [Fact]
        public void UnmanagedCallersOnlyAttribute_AddressOfUsedInAttributeArgument()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Runtime.InteropServices;
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
unsafe class Attr : Attribute
{
    public Attr() {}

    public delegate* unmanaged<void> PropUnmanaged { get; set; }
    public delegate*<void> PropManaged { get; set; }
    public delegate* unmanaged[Cdecl]<void> PropCdecl { get; set; }
}
unsafe class C
{
    [UnmanagedCallersOnly]
    static void M1()
    {
    }

    [Attr(PropUnmanaged = &M1)]
    [Attr(PropManaged = &M1)]
    [Attr(PropCdecl = &M1)]
    static unsafe void M2()
    {

    }
}
");

            comp.VerifyDiagnostics(
                // (20,11): error CS0655: 'PropUnmanaged' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [Attr(PropUnmanaged = &M1)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "PropUnmanaged", isSuppressed: false).WithArguments("PropUnmanaged").WithLocation(20, 11),
                // (21,11): error CS0655: 'PropManaged' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [Attr(PropManaged = &M1)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "PropManaged", isSuppressed: false).WithArguments("PropManaged").WithLocation(21, 11),
                // (22,11): error CS0655: 'PropCdecl' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [Attr(PropCdecl = &M1)]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "PropCdecl", isSuppressed: false).WithArguments("PropCdecl").WithLocation(22, 11)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UnmanagedCallersOnly_Il()
        {
            var verifier = CompileAndVerifyFunctionPointers(new[] { @"
using System;
using System.Runtime.InteropServices;

unsafe
{
    delegate* unmanaged<void> ptr = &M;
    ptr();
}

[UnmanagedCallersOnly]
static void M()
{
    Console.WriteLine(1);
}
", UnmanagedCallersOnlyAttribute }, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.NetCoreApp);

            // TODO: Remove the manual unmanagedcallersonlyattribute definition and override and verify the
            // output of running this code when we move to p8

            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL: @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M|0_0()""
  IL_0006:  calli      ""delegate* unmanaged<void>""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void UnmanagedCallersOnly_AddressOfAsInvocationArgument()
        {
            var verifier = CompileAndVerifyFunctionPointers(new[] { @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public unsafe class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void M1(int i) { }

    public static void M2(delegate* unmanaged[Cdecl]<int, void> param)
    {
        M2(&M1);
    }
}
", UnmanagedCallersOnlyAttribute });

            verifier.VerifyIL(@"C.M2", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""void C.M1(int)""
  IL_0006:  call       ""void C.M2(delegate* unmanaged[Cdecl]<int, void>)""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void UnmanagedCallersOnly_LambdaInference()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
using System.Runtime.InteropServices;
public unsafe class C
{
    [UnmanagedCallersOnly]
    public static void M1(int i) { }

    public static void M2()
    {
        Func<delegate*<int, void>> a1 = () => &M1;
        Func<delegate* unmanaged<int, void>> a2 = () => &M1;
    }
}
");

            comp.VerifyDiagnostics(
                // (11,14): error CS0306: The type 'delegate*<int, void>' may not be used as a type argument
                //         Func<delegate*<int, void>> a1 = () => &M1;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<int, void>", isSuppressed: false).WithArguments("delegate*<int, void>").WithLocation(11, 14),
                // (11,47): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<delegate*<int, void>> a1 = () => &M1;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "&M1", isSuppressed: false).WithArguments("lambda expression").WithLocation(11, 47),
                // (11,48): error CS8786: Calling convention of 'C.M1(int)' is not compatible with 'Default'.
                //         Func<delegate*<int, void>> a1 = () => &M1;
                Diagnostic(ErrorCode.ERR_WrongFuncPtrCallingConvention, "M1", isSuppressed: false).WithArguments("C.M1(int)", "Default").WithLocation(11, 48),
                // (12,14): error CS0306: The type 'delegate* unmanaged<int, void>' may not be used as a type argument
                //         Func<delegate* unmanaged<int, void>> a2 = () => &M1;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate* unmanaged<int, void>", isSuppressed: false).WithArguments("delegate* unmanaged<int, void>").WithLocation(12, 14)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var lambdas = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray();

            Assert.Equal(2, lambdas.Length);

            var typeInfo = model.GetTypeInfo(lambdas[0]);
            var conversion = model.GetConversion(lambdas[0]);
            AssertEx.Equal("System.Func<delegate*<System.Int32, System.Void>>",
                           typeInfo.Type.ToTestDisplayString(includeNonNullable: false));
            AssertEx.Equal("System.Func<delegate*<System.Int32, System.Void>>",
                           typeInfo.ConvertedType.ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(Conversion.NoConversion, conversion);

            typeInfo = model.GetTypeInfo(lambdas[1]);
            conversion = model.GetConversion(lambdas[1]);
            Assert.Null(typeInfo.Type);
            AssertEx.Equal("System.Func<delegate* unmanaged<System.Int32, System.Void>>",
                           typeInfo.ConvertedType.ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(ConversionKind.AnonymousFunction, conversion.Kind);
        }

        [Fact, WorkItem(47487, "https://github.com/dotnet/roslyn/issues/47487")]
        public void InAndRefParameter()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe
{
    delegate*<in int, ref char, void> F = &Test;
    char c = 'a';
    F(int.MaxValue, ref c);
}

static void Test(in int b, ref char c)
{
    Console.WriteLine($""b = {b}, c = {c}"");
}
", expectedOutput: "b = 2147483647, c = a");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (char V_0, //c
                delegate*<in int, ref char, void> V_1,
                int V_2)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__Test|0_0(in int, ref char)""
  IL_0006:  ldc.i4.s   97
  IL_0008:  stloc.0
  IL_0009:  stloc.1
  IL_000a:  ldc.i4     0x7fffffff
  IL_000f:  stloc.2
  IL_0010:  ldloca.s   V_2
  IL_0012:  ldloca.s   V_0
  IL_0014:  ldloc.1
  IL_0015:  calli      ""delegate*<in int, ref char, void>""
  IL_001a:  ret
}
");
        }

        [Fact, WorkItem(47487, "https://github.com/dotnet/roslyn/issues/47487")]
        public void OutDiscard()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe
{
    delegate*<out int, out int, void> F = &Test;
    F(out var i1, out _);
    F(out _, out var i2);
    Console.Write(i1);
    Console.Write(i2);
}

static void Test(out int i1, out int i2)
{
    i1 = 1;
    i2 = 2;
}
", expectedOutput: "12");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       42 (0x2a)
  .maxstack  4
  .locals init (int V_0, //i1
                int V_1, //i2
                int V_2,
                delegate*<out int, out int, void> V_3)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__Test|0_0(out int, out int)""
  IL_0006:  dup
  IL_0007:  stloc.3
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldloca.s   V_2
  IL_000c:  ldloc.3
  IL_000d:  calli      ""delegate*<out int, out int, void>""
  IL_0012:  stloc.3
  IL_0013:  ldloca.s   V_2
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldloc.3
  IL_0018:  calli      ""delegate*<out int, out int, void>""
  IL_001d:  ldloc.0
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ldloc.1
  IL_0024:  call       ""void System.Console.Write(int)""
  IL_0029:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void ReturnByRefFromRefReturningMethod()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    ref int iRef = ref ReturnPtrByRef(&ReturnByRef, ref i);
    iRef = 2;
    System.Console.WriteLine(i);
    
    static ref int ReturnPtrByRef(delegate*<ref int, ref int> ptr, ref int i)
        => ref ptr(ref i);

    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("Program.<<Main>$>g__ReturnPtrByRef|0_0(delegate*<ref int, ref int>, ref int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (delegate*<ref int, ref int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  ldloc.0
  IL_0004:  calli      ""delegate*<ref int, ref int>""
  IL_0009:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void ReturnByRefFromRefReturningMethod_FunctionPointerDoesNotReturnByRefError()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    int i = 1;
    ref int iRef = ref ReturnPtrByRef(&ReturnByRef, ref i);
    
    static ref int ReturnPtrByRef(delegate*<ref int, int> ptr, ref int i)
        => ref ptr(ref i);

    static int ReturnByRef(ref int i) => i;
}", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (8,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         => ref ptr(ref i);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "ptr(ref i)").WithLocation(8, 16)
            );
        }

        [WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        [Fact]
        public void ReturnByRefFromRefReturningMethod_NotSafeToEscape()
        {
            string source = @"
using System;
unsafe
{
    ref Span<int> spanRef = ref ReturnPtrByRef(&ReturnByRef);
    
    static ref Span<int> ReturnPtrByRef(delegate*<ref Span<int>, ref Span<int>> ptr)
    {
        Span<int> span = stackalloc int[1];
        return ref ptr(ref span);
    }

    static ref Span<int> ReturnByRef(ref Span<int> i) => throw null;
}";

            var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular10, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (10,28): warning CS9091: This returns local 'span' by reference but it is not a ref local
                //         return ref ptr(ref span);
                Diagnostic(ErrorCode.WRN_RefReturnLocal, "span").WithArguments("span").WithLocation(10, 28)
            );

            comp = CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (10,28): warning CS9091: This returns local 'span' by reference but it is not a ref local
                //         return ref ptr(ref span);
                Diagnostic(ErrorCode.WRN_RefReturnLocal, "span").WithArguments("span").WithLocation(10, 28)
            );
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void ReturnByRefFromRefReturningMethod_SafeToEscape()
        {
            string source = @"
using System;
unsafe
{
    Span<int> s = stackalloc int[1];
    s[0] = 1;
    ref Span<int> sRef = ref ReturnPtrByRef(&ReturnByRef, ref s);
    sRef[0] = 2;
    Console.WriteLine(s[0]);
    
    static ref Span<int> ReturnPtrByRef(delegate*<ref Span<int>, ref Span<int>> ptr, ref Span<int> s)
        => ref ptr(ref s);

    static ref Span<int> ReturnByRef(ref Span<int> i) => ref i;
}";

            var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular10, options: TestOptions.UnsafeReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Skipped);
            verifier.VerifyIL("Program.<<Main>$>g__ReturnPtrByRef|0_0(delegate*<ref System.Span<int>, ref System.Span<int>>, ref System.Span<int>)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (delegate*<ref System.Span<int>, ref System.Span<int>> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  ldloc.0
  IL_0004:  calli      ""delegate*<ref System.Span<int>, ref System.Span<int>>""
  IL_0009:  ret
}
");

            comp = CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void ReturnByRefFromRefReturningMethod_RefReadonlyToRefError()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    int i = 1;
    ref int iRef = ref ReturnPtrByRef(&ReturnByRef, ref i);
    
    static ref int ReturnPtrByRef(delegate*<ref int, ref readonly int> ptr, ref int i)
        => ref ptr(ref i);

    static ref readonly int ReturnByRef(ref int i) => ref i;
}", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (8,16): error CS8333: Cannot return method 'delegate*<ref int, ref readonly int>' by writable reference because it is a readonly variable
                //         => ref ptr(ref i);
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "ptr(ref i)").WithArguments("method", "delegate*<ref int, ref readonly int>").WithLocation(8, 16)
            );
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void ReturnByRefFromRefReturningMethod_RefToRefReadonly()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    ref readonly int iRef = ref ReturnPtrByRef(&ReturnByRef, ref i);
    i = 2;
    System.Console.WriteLine(iRef);
    
    static ref readonly int ReturnPtrByRef(delegate*<ref int, ref int> ptr, ref int i)
        => ref ptr(ref i);

    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("Program.<<Main>$>g__ReturnPtrByRef|0_0(delegate*<ref int, ref int>, ref int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (delegate*<ref int, ref int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  ldloc.0
  IL_0004:  calli      ""delegate*<ref int, ref int>""
  IL_0009:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void RefAssignment()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    delegate*<ref int, ref int> ptr = &ReturnByRef;
    ref readonly int iRef = ref ptr(ref i);
    i = 2;
    System.Console.WriteLine(iRef);
    
    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int V_0, //i
                delegate*<ref int, ref int> V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldftn      ""ref int Program.<<Main>$>g__ReturnByRef|0_0(ref int)""
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.1
  IL_000c:  calli      ""delegate*<ref int, ref int>""
  IL_0011:  ldc.i4.2
  IL_0012:  stloc.0
  IL_0013:  ldind.i4
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void RefAssignmentThroughTernary()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    int i2 = 3;
    delegate*<ref int, ref int> ptr = &ReturnByRef;
    ref readonly int iRef = ref false ? ref i2 : ref ptr(ref i);
    i = 2;
    System.Console.WriteLine(iRef);
    
    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int V_0, //i
                delegate*<ref int, ref int> V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldftn      ""ref int Program.<<Main>$>g__ReturnByRef|0_0(ref int)""
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.1
  IL_000c:  calli      ""delegate*<ref int, ref int>""
  IL_0011:  ldc.i4.2
  IL_0012:  stloc.0
  IL_0013:  ldind.i4
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void RefReturnThroughTernary()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    int i2 = 3;
    ref int iRef = ref ReturnPtrByRef(&ReturnByRef, ref i, ref i2);
    iRef = 2;
    System.Console.WriteLine(i);
    
    static ref int ReturnPtrByRef(delegate*<ref int, ref int> ptr, ref int i, ref int i2)
        => ref false ? ref i2 : ref ptr(ref i);

    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("Program.<<Main>$>g__ReturnPtrByRef|0_0(delegate*<ref int, ref int>, ref int, ref int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (delegate*<ref int, ref int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  ldloc.0
  IL_0004:  calli      ""delegate*<ref int, ref int>""
  IL_0009:  ret
}
");
        }

        [Fact, WorkItem(49315, "https://github.com/dotnet/roslyn/issues/49315")]
        public void PassedAsByRefParameter()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    int i = 1;
    delegate*<ref int, ref int> ptr = &ReturnByRef;
    ref readonly int iRef = ref ptr(ref ptr(ref i));
    i = 2;
    System.Console.WriteLine(iRef);
    
    static ref int ReturnByRef(ref int i) => ref i;
}", expectedOutput: "2");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (int V_0, //i
                delegate*<ref int, ref int> V_1,
                delegate*<ref int, ref int> V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldftn      ""ref int Program.<<Main>$>g__ReturnByRef|0_0(ref int)""
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  stloc.2
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldloc.2
  IL_000e:  calli      ""delegate*<ref int, ref int>""
  IL_0013:  ldloc.1
  IL_0014:  calli      ""delegate*<ref int, ref int>""
  IL_0019:  ldc.i4.2
  IL_001a:  stloc.0
  IL_001b:  ldind.i4
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  ret
}
");
        }

        [Fact, WorkItem(49760, "https://github.com/dotnet/roslyn/issues/49760")]
        public void ReturnRefStructByValue_CanEscape()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;

unsafe
{
    Console.WriteLine(ptrTest().field);
    
    static BorrowedReference ptrTest()
    {
        delegate*<BorrowedReference> ptr = &test;
        return ptr();
    }

    static BorrowedReference test() => new BorrowedReference() { field = 1 };
}

ref struct BorrowedReference {
    public int field;
}
", expectedOutput: "1");

            verifier.VerifyIL("Program.<<Main>$>g__ptrTest|0_0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldftn      ""BorrowedReference Program.<<Main>$>g__test|0_1()""
  IL_0006:  calli      ""delegate*<BorrowedReference>""
  IL_000b:  ret
}
");
        }

        [Fact, WorkItem(49760, "https://github.com/dotnet/roslyn/issues/49760")]
        public void ReturnRefStructByValue_CannotEscape()
        {
            var comp = CreateCompilationWithSpan(@"
#pragma warning disable CS8321 // Unused local function ptrTest
using System;
unsafe
{
    static Span<int> ptrTest()
    {
        Span<int> s = stackalloc int[1];
        delegate*<Span<int>, Span<int>> ptr = &test;
        return ptr(s);
    }

    static Span<int> ptrTest2(Span<int> s)
    {
        delegate*<Span<int>, Span<int>> ptr = &test;
        return ptr(s);
    }
    static Span<int> test(Span<int> s) => s;
}
", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (10,20): warning CS9077: Use of variable 's' in this context may expose referenced variables outside of their declaration scope
                //         return ptr(s);
                Diagnostic(ErrorCode.WRN_EscapeVariable, "s").WithArguments("s").WithLocation(10, 20)
            );
        }

        [Fact]
        public void RefEscapeNestedArrayAccess()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
System.Console.WriteLine(M());

static ref int M()
{
    var arr = new int[1]{40};

    unsafe ref int N()
    {
        static ref int NN(ref int arg) => ref arg;

        delegate*<ref int, ref int> ptr = &NN;
        ref var r = ref ptr(ref arr[0]); 
        r += 2;

        return ref r;
    }

    return ref N();
}
", expectedOutput: "42");

            verifier.VerifyIL("Program.<<Main>$>g__N|0_1(ref Program.<>c__DisplayClass0_0)", @"
{
  // Code size       32 (0x20)
  .maxstack  4
  .locals init (delegate*<ref int, ref int> V_0)
  IL_0000:  ldftn      ""ref int Program.<<Main>$>g__NN|0_2(ref int)""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int[] Program.<>c__DisplayClass0_0.arr""
  IL_000d:  ldc.i4.0
  IL_000e:  ldelema    ""int""
  IL_0013:  ldloc.0
  IL_0014:  calli      ""delegate*<ref int, ref int>""
  IL_0019:  dup
  IL_001a:  dup
  IL_001b:  ldind.i4
  IL_001c:  ldc.i4.2
  IL_001d:  add
  IL_001e:  stind.i4
  IL_001f:  ret
}
");
        }

        [Fact]
        public void RefReturnInCompoundAssignment()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
unsafe
{
    delegate*<ref int, ref int> ptr = &RefReturn;
    int i = 0;
    ptr(ref i) += 1;
    System.Console.WriteLine(i);

    static ref int RefReturn(ref int i) => ref i;
}", expectedOutput: "1");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int V_0, //i
                delegate*<ref int, ref int> V_1)
  IL_0000:  ldftn      ""ref int Program.<<Main>$>g__RefReturn|0_0(ref int)""
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.1
  IL_000c:  calli      ""delegate*<ref int, ref int>""
  IL_0011:  dup
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  stind.i4
  IL_0016:  ldloc.0
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void InvalidReturnInCompoundAssignment()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe
{
    delegate*<int, int> ptr = &RefReturn;
    int i = 0;
    ptr(i) += 1;

    static int RefReturn(int i) => i;
}", options: TestOptions.UnsafeReleaseExe);

            comp.VerifyDiagnostics(
                // (6,5): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //     ptr(i) += 1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "ptr(i)").WithLocation(6, 5)
            );
        }

        [Fact, WorkItem(49639, "https://github.com/dotnet/roslyn/issues/49639")]
        public void CompareToNullWithNestedUnconstrainedTypeParameter()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe
{
    test<int>(null);
    test<int>(&intTest);

    static void test<T>(delegate*<T, void> f)
    {
        Console.WriteLine(f == null);
        Console.WriteLine(f is null);
    }

    static void intTest(int i) {}
}
", expectedOutput: @"
True
True
False
False");

            verifier.VerifyIL("Program.<<Main>$>g__test|0_0<T>(delegate*<T, void>)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.u
  IL_0003:  ceq
  IL_0005:  call       ""void System.Console.WriteLine(bool)""
  IL_000a:  ldarg.0
  IL_000b:  ldc.i4.0
  IL_000c:  conv.u
  IL_000d:  ceq
  IL_000f:  call       ""void System.Console.WriteLine(bool)""
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(48765, "https://github.com/dotnet/roslyn/issues/48765")]
        public void TypeOfFunctionPointerInAttribute()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
using System;
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
[Attr(typeof(delegate*<void>))]
[Attr(typeof(delegate*<void>[]))]
[Attr(typeof(C<delegate*<void>[]>))]
unsafe class Attr : System.Attribute
{
    public Attr(System.Type type) {}
}

class C<T> {}
");

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario. Currently, we don't know how to
            // encode these in metadata, and may need to work with the runtime team to define a new format.
            comp.VerifyEmitDiagnostics(
                // (4,2): error CS8911: Using a function pointer type in this context is not supported.
                // [Attr(typeof(delegate*<void>))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "Attr(typeof(delegate*<void>))").WithLocation(4, 2),
                // (5,2): error CS8911: Using a function pointer type in this context is not supported.
                // [Attr(typeof(delegate*<void>[]))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "Attr(typeof(delegate*<void>[]))").WithLocation(5, 2),
                // (6,2): error CS8911: Using a function pointer type in this context is not supported.
                // [Attr(typeof(C<delegate*<void>[]>))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "Attr(typeof(C<delegate*<void>[]>))").WithLocation(6, 2)
            );
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Enum_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default(B<delegate*<void>[]>.E))]
                class C { }
                """;

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (11,4): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A(default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "default(B<delegate*<void>[]>.E)").WithLocation(11, 4),
                // (11,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A(default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(11, 14));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Enum_ConstructorArgument_WithUnsafeContext([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default(B<delegate*<void>[]>.E))]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(default(B<delegate*<void>[]>.E))").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_GenericObjectDefault_Enum_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A<T> : System.Attribute
                {
                    public A(T t) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A<object>(default(B<delegate*<void>[]>.E))]
                class C { }
                """;

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (11,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A<object>(default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "default(B<delegate*<void>[]>.E)").WithLocation(11, 12),
                // (11,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A<object>(default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(11, 22));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Enum_ConstructorArgument_ParamsArray([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(params object[] o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(null, "abc", default(B<delegate*<void>[]>.E))]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(null, "abc", default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, @"A(null, ""abc"", default(B<delegate*<void>[]>.E))").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Enum_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public object P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(P = default(B<delegate*<void>[]>.E))]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = default(B<delegate*<void>[]>.E))").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Implicit_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(B<delegate*<void>[]>.E e) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (3,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public A(B<delegate*<void>[]>.E e) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(3, 16)
                );
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Implicit_ConstructorArgument_WithUnsafeContext([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(B<delegate*<void>[]>.E e) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.Empty(attr.ConstructorArguments);
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Implicit_ConstructorArgument_WithoutUnsafeContext([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(B<delegate*<void>[]>.E e) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default)]
                class C { }
                """;

            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (11,4): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A(default)]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "default").WithLocation(11, 4)
                );
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_GenericTypedDefault_Enum_Implicit_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A<T> : System.Attribute
                {
                    public A(T t) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A<B<delegate*<void>[]>.E>(default)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.Empty(attr.ConstructorArguments);
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Implicit_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe B<delegate*<void>[]>.E P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(P = default)]
                class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = default)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = default)").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Explicit_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(B<delegate*<void>[]>.E e) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(default(B<delegate*<void>[]>.E))]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.Empty(attr.ConstructorArguments);
                Assert.Empty(attr.NamedArguments);
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedDefault_Enum_Explicit_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe B<delegate*<void>[]>.E P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(P = default(B<delegate*<void>[]>.E))]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = default(B<delegate*<void>[]>.E))]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = default(B<delegate*<void>[]>.E))").WithLocation(11, 2));
        }

        [Fact, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_GenericArgument()
        {
            var source = """
                using System;
                using System.Linq;

                var attr = typeof(C).CustomAttributes.Single(d => d.AttributeType == typeof(A));
                var arg = attr.ConstructorArguments.Single();
                Console.WriteLine(arg.Value is null);

                class A : Attribute
                {
                    public A(object o) { }
                }

                class B<T> { }

                [A(default(B<delegate*<void>[]>))]
                unsafe class C { }
                """;

            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugExe, expectedOutput: "True", symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                var arg = attr.ConstructorArguments.Single();
                Assert.True(arg.IsNull);
            });
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Array()
        {
            var source = """
                using System;
                using System.Linq;

                var attr = typeof(C).CustomAttributes.Single(d => d.AttributeType == typeof(A));
                var arg = attr.ConstructorArguments.Single();
                Console.WriteLine(arg.Value is null);

                class A : Attribute
                {
                    public A(object o) { }
                }

                [A(default(delegate*<void>[]))]
                unsafe class C { }
                """;

            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe,
                expectedOutput: "True",
                symbolValidator: static module =>
                {
                    var c = module.GlobalNamespace.GetTypeMember("C");
                    var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                    var arg = attr.ConstructorArguments.Single();
                    Assert.True(arg.IsNull);
                });
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectDefault_Standalone()
        {
            var source = """
                class A : System.Attribute
                {
                    public A(object value) { }
                }

                [A(default(delegate*<void>))]
                unsafe class C { }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,4): error CS1503: Argument 1: cannot convert from 'delegate*<void>' to 'object'
                // [A(default(delegate*<void>))]
                Diagnostic(ErrorCode.ERR_BadArgType, "default(delegate*<void>)").WithArguments("1", "delegate*<void>", "object").WithLocation(6, 4));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectConstant_Enum_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A(B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(B<delegate*<void>[]>.C)").WithLocation(12, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectConstant_EnumArray_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new B<delegate*<void>[]>.E[]{})]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(new B<delegate*<void>[]>.E[]{})]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(new B<delegate*<void>[]>.E[]{})").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectArrayConstant_EnumArray_ConstructorArgument_01(
            [CombinatorialValues("class", "struct")] string kind,
            [CombinatorialValues("object", "object[]")] string parameterType)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A({{parameterType}} a) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new object[] { new B<delegate*<void>[]>.E() })]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(new object[] { new B<delegate*<void>[]>.E() })]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(new object[] { new B<delegate*<void>[]>.E() })").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectArrayConstant_EnumArray_ConstructorArgument_02([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object[] a) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new B<delegate*<void>[]>.E[]{})]
                unsafe class C { }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (11,4): error CS1503: Argument 1: cannot convert from 'B<delegate*<void>[]>.E[]' to 'object[]'
                // [A(new B<delegate*<void>[]>.E[]{})]
                Diagnostic(ErrorCode.ERR_BadArgType, "new B<delegate*<void>[]>.E[]{}").WithArguments("1", "B<delegate*<void>[]>.E[]", "object[]").WithLocation(11, 4));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectParamsConstant_EnumArray_ConstructorArgument(
            [CombinatorialValues("class", "struct")] string kind,
            [CombinatorialValues("[]{}", "()")] string initializer)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(params object[] a) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new B<delegate*<void>[]>.E{{initializer}})]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(new B<delegate*<void>[]>.E())]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, $"A(new B<delegate*<void>[]>.E{initializer})").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_GenericObjectConstant_Enum_ConstructorArgument_NoUnsafeModifier([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A<T> : System.Attribute
                {
                    public A(T t) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A<object>(B<delegate*<void>[]>.C)]
                class C { }
                """;

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (12,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A<object>(B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "B<delegate*<void>[]>").WithLocation(12, 12),
                // (12,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A<object>(B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "B<delegate*<void>[]>.C").WithLocation(12, 12),
                // (12,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // [A<object>(B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(12, 14));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_GenericObjectConstant_Enum_ConstructorArgument_UnsafeModifier([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A<T> : System.Attribute
                {
                    public A(T t) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A<object>(B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A<object>(B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A<object>(B<delegate*<void>[]>.C)").WithLocation(12, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_GenericTypedConstant_Enum_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A<T> : System.Attribute
                {
                    public A(T t) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A<B<delegate*<void>[]>.E>(B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.True(attr.HasErrors); // https://github.com/dotnet/roslyn/issues/66370
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectConstant_Enum_ConstructorNamedArguments([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public A(object x, int y) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A(y: 1, x: B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(y: 1, x: B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(y: 1, x: B<delegate*<void>[]>.C)").WithLocation(12, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectConstant_Enum_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public object P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A(P = B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = B<delegate*<void>[]>.C)").WithLocation(12, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectConstant_EnumArray_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public object P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(P = new B<delegate*<void>[]>.E[]{})]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = new B<delegate*<void>[]>.E[]{})]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = new B<delegate*<void>[]>.E[]{})").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_ObjectArrayConstant_EnumArray_NamedArgument(
            [CombinatorialValues("class", "struct")] string kind,
            [CombinatorialValues("object", "object[]")] string parameterType)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public {{parameterType}} P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(P = new object[] { new B<delegate*<void>[]>.E() })]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (11,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = new object[] { new B<delegate*<void>[]>.E() })]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = new object[] { new B<delegate*<void>[]>.E() })").WithLocation(11, 2));
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedConstant_Enum_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(B<delegate*<void>[]>.E o) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A(B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.True(attr.HasErrors); // https://github.com/dotnet/roslyn/issues/66370
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedConstant_EnumArray_ConstructorArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(B<delegate*<void>[]>.E[] a) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new B<delegate*<void>[]>.E[]{})]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.True(attr.HasErrors); // https://github.com/dotnet/roslyn/issues/66370
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedParamsConstant_EnumArray_ConstructorArgument(
            [CombinatorialValues("class", "struct")] string kind,
            [CombinatorialValues("[]{}", "()")] string initializer)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe A(params B<delegate*<void>[]>.E[] a) { }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                }

                [A(new B<delegate*<void>[]>.E{{initializer}})]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/66187 tracks enabling runtime reflection support for this scenario.
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, symbolValidator: static module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var attr = c.GetAttributes().Single(d => d.AttributeClass?.Name == "A");
                Assert.True(attr.HasErrors); // https://github.com/dotnet/roslyn/issues/66370
            });
            verifier.VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem(65594, "https://github.com/dotnet/roslyn/issues/65594")]
        public void Attribute_TypedConstant_Enum_NamedArgument([CombinatorialValues("class", "struct")] string kind)
        {
            var source = $$"""
                class A : System.Attribute
                {
                    public unsafe B<delegate*<void>[]>.E P { get; set; }
                }

                {{kind}} B<T>
                {
                    public enum E { }
                    public const E C = (E)33;
                }

                [A(P = B<delegate*<void>[]>.C)]
                unsafe class C { }
                """;

            // https://github.com/dotnet/roslyn/issues/48765 tracks enabling support for this scenario.
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,2): error CS8911: Using a function pointer type in this context is not supported.
                // [A(P = B<delegate*<void>[]>.C)]
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported, "A(P = B<delegate*<void>[]>.C)").WithLocation(12, 2));
        }

        [Fact, WorkItem(55394, "https://github.com/dotnet/roslyn/issues/55394")]
        public void SwitchExpression_01()
        {
            var code = @"
unsafe
{
    delegate*<string, void> ptr = &M;
    bool b = true;
    ptr(b switch { true => ""true"", false => ""false"" });
}

static void M(string s) => System.Console.WriteLine(s);
";

            var verifier = CompileAndVerifyFunctionPointers(code, expectedOutput: "true");
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (delegate*<string, void> V_0, //ptr
                delegate*<string, void> V_1,
                string V_2,
                delegate*<string, void> V_3)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M|0_0(string)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  brfalse.s  IL_0014
  IL_000c:  ldstr      ""true""
  IL_0011:  stloc.2
  IL_0012:  br.s       IL_001a
  IL_0014:  ldstr      ""false""
  IL_0019:  stloc.2
  IL_001a:  ldloc.1
  IL_001b:  stloc.3
  IL_001c:  ldloc.2
  IL_001d:  ldloc.3
  IL_001e:  calli      ""delegate*<string, void>""
  IL_0023:  ret
}
");
        }

        [Fact, WorkItem(55394, "https://github.com/dotnet/roslyn/issues/55394")]
        public void SwitchExpression_02()
        {
            var code = @"
unsafe
{
    delegate*<string, void> ptr1 = &M1;
    delegate*<string, void> ptr2 = &M2;
    bool b = true;
    (b switch { true => ptr1, false => ptr2 })(""true"");
}

static void M1(string s) => System.Console.WriteLine(s);
static void M2(string s) => throw null;
";

            var verifier = CompileAndVerifyFunctionPointers(code, expectedOutput: "true");
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (delegate*<string, void> V_0, //ptr1
                delegate*<string, void> V_1, //ptr2
                delegate*<string, void> V_2,
                delegate*<string, void> V_3)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M1|0_0(string)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void Program.<<Main>$>g__M2|0_1(string)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.1
  IL_000f:  brfalse.s  IL_0015
  IL_0011:  ldloc.0
  IL_0012:  stloc.2
  IL_0013:  br.s       IL_0017
  IL_0015:  ldloc.1
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  stloc.3
  IL_0019:  ldstr      ""true""
  IL_001e:  ldloc.3
  IL_001f:  calli      ""delegate*<string, void>""
  IL_0024:  ret
}
");
        }

        [Fact, WorkItem(55394, "https://github.com/dotnet/roslyn/issues/55394")]
        public void SwitchExpression_03()
        {
            var code = @"
unsafe
{
    delegate*<string, void> ptr1 = &M1;
    delegate*<object, void> ptr2 = &M2;
    bool b = true;
    (b switch { true => ptr1, false => ptr2 })(""true"");
}

static void M1(string s) => System.Console.WriteLine(s);
static void M2(object s) => throw null;
";

            var verifier = CompileAndVerifyFunctionPointers(code, expectedOutput: "true");
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (delegate*<string, void> V_0, //ptr1
                delegate*<object, void> V_1, //ptr2
                delegate*<string, void> V_2,
                delegate*<string, void> V_3)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M1|0_0(string)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void Program.<<Main>$>g__M2|0_1(object)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.1
  IL_000f:  brfalse.s  IL_0015
  IL_0011:  ldloc.0
  IL_0012:  stloc.2
  IL_0013:  br.s       IL_0017
  IL_0015:  ldloc.1
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  stloc.3
  IL_0019:  ldstr      ""true""
  IL_001e:  ldloc.3
  IL_001f:  calli      ""delegate*<string, void>""
  IL_0024:  ret
}
");
        }

        [Fact, WorkItem(55394, "https://github.com/dotnet/roslyn/issues/55394")]
        public void SwitchExpression_04()
        {
            var code = @"
unsafe
{
    delegate*<string, void> ptr1 = &M1;
    delegate*<string, void> ptr2 = &M2;
    bool b = true;
    (b switch { true => ptr1, false => ptr2 })(b switch { true => ""true"", false => ""false"" });
}

static void M1(string s) => System.Console.WriteLine(s);
static void M2(string s) => throw null;
";

            var verifier = CompileAndVerifyFunctionPointers(code, expectedOutput: "true");
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (delegate*<string, void> V_0, //ptr1
                delegate*<string, void> V_1, //ptr2
                bool V_2, //b
                delegate*<string, void> V_3,
                string V_4,
                delegate*<string, void> V_5)
  IL_0000:  ldftn      ""void Program.<<Main>$>g__M1|0_0(string)""
  IL_0006:  stloc.0
  IL_0007:  ldftn      ""void Program.<<Main>$>g__M2|0_1(string)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.1
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  brfalse.s  IL_0017
  IL_0013:  ldloc.0
  IL_0014:  stloc.3
  IL_0015:  br.s       IL_0019
  IL_0017:  ldloc.1
  IL_0018:  stloc.3
  IL_0019:  ldloc.3
  IL_001a:  ldloc.2
  IL_001b:  brfalse.s  IL_0026
  IL_001d:  ldstr      ""true""
  IL_0022:  stloc.s    V_4
  IL_0024:  br.s       IL_002d
  IL_0026:  ldstr      ""false""
  IL_002b:  stloc.s    V_4
  IL_002d:  stloc.s    V_5
  IL_002f:  ldloc.s    V_4
  IL_0031:  ldloc.s    V_5
  IL_0033:  calli      ""delegate*<string, void>""
  IL_0038:  ret
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
