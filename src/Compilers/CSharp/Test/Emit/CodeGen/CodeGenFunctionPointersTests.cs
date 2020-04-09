// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenFunctionPointersTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyFunctionPointers(
            string source,
            MetadataReference[]? references = null,
            Action<ModuleSymbol>? symbolValidator = null,
            string? expectedOutput = null)
        {
            return CompileAndVerify(source, references, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe, symbolValidator: symbolValidator, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointersWithIl(string source, string ilStub, Action<ModuleSymbol>? symbolValidator = null, string? expectedOutput = null)
        {
            var comp = CreateCompilationWithIL(source, ilStub, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: symbolValidator, verify: Verification.Skipped);
        }

        private CSharpCompilation CreateCompilationWithFunctionPointers(string source, IEnumerable<MetadataReference>? references = null)
        {
            return CreateCompilation(source, references: references, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
        }

        [Theory]
        [InlineData("", CallingConvention.Default)]
        [InlineData("cdecl", CallingConvention.CDecl)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("thiscall", CallingConvention.ThisCall)]
        [InlineData("stdcall", CallingConvention.Standard)]
        internal void CallingConventions(string conventionString, CallingConvention expectedConvention)
        {
            var comp = CompileAndVerifyFunctionPointers($@"
class C
{{
    public unsafe delegate* {conventionString}<string, int> M() => throw null;
}}", symbolValidator: symbolValidator);

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
            var comp = CompileAndVerifyFunctionPointers(@"
class C
{
    public unsafe void M(delegate*<ref C, ref string, ref int[]> param1) => throw null;
}", symbolValidator: symbolValidator);

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
        public void NestedFunctionPointers()
        {
            var comp = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe delegate* cdecl<delegate* stdcall<int, void>, void> M(delegate*<C, delegate*<S>> param1) => throw null;
}
public struct S
{
}", symbolValidator: symbolValidator);
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
            var comp = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe void M(delegate*<in string, in int, ref readonly bool> param) {}
}", symbolValidator: symbolValidator);

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
        public void NestedPointerTypes()
        {
            var comp = CompileAndVerifyFunctionPointers(@"
public class C
{
    public unsafe delegate* cdecl<ref delegate*<ref readonly string>, void> M(delegate*<in delegate* stdcall<delegate*<void>>, delegate*<int>> param) => throw null;
}", symbolValidator: symbolValidator);

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
            CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
	public void M(delegate*<ref int, ref bool> param1,
                  delegate*<ref int, ref bool> param2,
                  delegate*<ref int, ref bool> param3,
                  delegate*<ref int, ref bool> param4,
                  delegate*<ref int, ref bool> param5) {}
                     
}", symbolValidator: symbolValidator);

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
            var compVerifier = CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
    public delegate*<string, void> Prop1 { get; set; }
    public delegate* stdcall<int> Prop2 { get => throw null; set => throw null; }
}", symbolValidator: symbolValidator);

            compVerifier.VerifyIL("C.Prop1.get", expectedIL: @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""delegate*<string,void> C.<Prop1>k__BackingField""
  IL_0006:  ret
}
");

            compVerifier.VerifyIL("C.Prop1.set", expectedIL: @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""delegate*<string,void> C.<Prop1>k__BackingField""
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
            CompileAndVerifyFunctionPointers(@"
public unsafe class C
{
    public readonly delegate*<C, C> _field;
}", symbolValidator: symbolValidator);

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

            var comp = CompileAndVerifyFunctionPointersWithIl(source, ilStub: ilSource, symbolValidator: symbolValidator);

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

            var comp = CreateCompilationWithIL("", ilSource, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp);

            var c = comp.GetTypeByMetadataName("C");
            var prop = c.GetProperty("Prop");

            VerifyFunctionPointerSymbol(prop.Type, CallingConvention.FastCall,
                (RefKind.None, IsVoidType()));
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
  .locals init (delegate*<string,void> V_0)
  IL_0000:  ldsfld     ""delegate*<string,void> Caller._field""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Called""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string,void>""
  IL_0011:  ret
}");
        }

        [Theory]
        [InlineData("cdecl")]
        [InlineData("stdcall")]
        public void UnmanagedCallingConventions(string convention)
        {
            // Use IntPtr Marshal.GetFunctionPointerForDelegate<TDelgate>(TDelegate delegate) to
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
            // PROTOTYPE(func-ptr): Add calling convention when the formatter supports it
            verifier.VerifyIL($"Caller.Call(delegate*<string,string,string>)", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (delegate*<string,string,string> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""Hello""
  IL_0007:  ldstr      "" World""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate*<string,string,string>""
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
                delegate*<S*,int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*,int> UnmanagedFunctionPointer.GetFuncPtrSingleParam()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldloc.1
  IL_001a:  calli      ""delegate*<S*,int>""
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret
}
");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (S V_0, //s
                delegate*<S*,int,int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*,int,int> UnmanagedFunctionPointer.GetFuncPtrMultipleParams()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldc.i4.3
  IL_001a:  ldloc.1
  IL_001b:  calli      ""delegate*<S*,int,int>""
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
                delegate*<S*,IntWrapper> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*,IntWrapper> UnmanagedFunctionPointer.GetFuncPtrSingleParam()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldloc.1
  IL_001a:  calli      ""delegate*<S*,IntWrapper>""
  IL_001f:  ldfld      ""int IntWrapper.i""
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
}");

            verifier.VerifyIL("C.TestMultiple()", @"
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (S V_0, //s
                delegate*<S*,float,ReturnWrapper> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.2
  IL_000b:  stfld      ""int S.i""
  IL_0010:  call       ""delegate*<S*,float,ReturnWrapper> UnmanagedFunctionPointer.GetFuncPtrMultipleParams()""
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  conv.u
  IL_0019:  ldc.r4     3.5
  IL_001e:  ldloc.1
  IL_001f:  calli      ""delegate*<S*,float,ReturnWrapper>""
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
  .locals init (delegate*<string,string,void> V_0)
  IL_0000:  call       ""delegate*<string,string,void> C.Prop.get""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""1""
  IL_000b:  call       ""string C.GetArg(string)""
  IL_0010:  ldstr      ""2""
  IL_0015:  call       ""string C.GetArg(string)""
  IL_001a:  ldloc.0
  IL_001b:  calli      ""delegate*<string,string,void>""
  IL_0020:  ret
}");

            verifier.VerifyIL("C.MethodOrder()", expectedIL: @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (delegate*<string,string,void> V_0)
  IL_0000:  call       ""delegate*<string,string,void> C.Method()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""3""
  IL_000b:  call       ""string C.GetArg(string)""
  IL_0010:  ldstr      ""4""
  IL_0015:  call       ""string C.GetArg(string)""
  IL_001a:  ldloc.0
  IL_001b:  calli      ""delegate*<string,string,void>""
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
  .locals init (delegate*<string,string> V_0)
  IL_0000:  call       ""delegate*<string,string> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Returned""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string,string>""
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
  .locals init (delegate*<string,string> V_0)
  IL_0000:  call       ""delegate*<string,string> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Unused""
  IL_000b:  ldloc.0
  IL_000c:  calli      ""delegate*<string,string>""
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
  .locals init (delegate*<string,string> V_0)
  IL_0000:  call       ""delegate*<delegate*<string,string>> Program.LoadPtr()""
  IL_0005:  calli      ""delegate*<delegate*<string,string>>""
  IL_000a:  stloc.0
  IL_000b:  ldstr      ""Returned""
  IL_0010:  ldloc.0
  IL_0011:  calli      ""delegate*<string,string>""
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
  .locals init (delegate*<Program,void> V_0)
  IL_0000:  call       ""delegate*<Program,void> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""C..ctor()""
  IL_000b:  call       ""Program C.op_Implicit(C)""
  IL_0010:  ldloc.0
  IL_0011:  calli      ""delegate*<Program,void>""
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
                delegate*<ref string,void> V_1)
  IL_0000:  call       ""delegate*<ref string,void> Program.LoadPtr()""
  IL_0005:  ldstr      ""Unset""
  IL_000a:  stloc.0
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldloc.1
  IL_000f:  calli      ""delegate*<ref string,void>""
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
  IL_000a:  call       ""delegate*<string> Program.LoadPtr()""
  IL_000f:  calli      ""delegate*<string>""
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
  IL_0000:  call       ""delegate*<string> Program.LoadPtr()""
  IL_0005:  calli      ""delegate*<string>""
  IL_000a:  ldstr      ""Field""
  IL_000f:  stind.ref
  IL_0010:  ldsfld     ""string Program.field""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}");
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
  .locals init (delegate*<string,string> V_0, //ptr
                delegate*<string,string> V_1,
                delegate*<string,string> V_2)
  IL_0000:  call       ""delegate*<string,string> Program.LoadPtr1()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  call       ""delegate*<string,string> Program.LoadPtr2()""
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  stloc.2
  IL_0010:  ldstr      ""Argument To Function 2""
  IL_0015:  ldloc.2
  IL_0016:  calli      ""delegate*<string,string>""
  IL_001b:  ldloc.1
  IL_001c:  calli      ""delegate*<string,string>""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ldloc.0
  IL_0027:  stloc.1
  IL_0028:  ldstr      ""Argument To Function 2""
  IL_002d:  ldloc.1
  IL_002e:  calli      ""delegate*<string,string>""
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
  .locals init (delegate*<string,int,void> V_0)
  IL_0000:  ldftn      ""void C.M(string, int)""
  IL_0006:  stloc.0
  IL_0007:  ldstr      ""1""
  IL_000c:  ldc.i4.2
  IL_000d:  ldloc.0
  IL_000e:  calli      ""delegate*<string,int,void>""
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
                delegate*<ref string,in int,out object,void> V_3)
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
  IL_0016:  calli      ""delegate*<ref string,in int,out object,void>""
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
  .locals init (delegate*<int,S> V_0,
                S V_1)
  IL_0000:  ldftn      ""S S.MakeS(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int,S>""
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
  .locals init (delegate*<int,C> V_0)
  IL_0000:  ldftn      ""C C.MakeC(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int,C>""
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
                delegate*<string,int*,void> V_1)
  IL_0000:  ldftn      ""void C.M(object, void*)""
  IL_0006:  ldc.i4.2
  IL_0007:  stloc.0
  IL_0008:  stloc.1
  IL_0009:  ldstr      ""1""
  IL_000e:  ldloca.s   V_0
  IL_0010:  conv.u
  IL_0011:  ldloc.1
  IL_0012:  calli      ""delegate*<string,int*,void>""
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
  .locals init (delegate*<string,object> V_0)
  IL_0000:  ldftn      ""delegate*<object,string> C.Returner()""
  IL_0006:  calli      ""delegate*<delegate*<string,object>>""
  IL_000b:  stloc.0
  IL_000c:  ldstr      ""1""
  IL_0011:  ldloc.0
  IL_0012:  calli      ""delegate*<string,object>""
  IL_0017:  call       ""void System.Console.Write(object)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void AddressOf_Initializer_Overloads()
        {
            var verifier = CompileAndVerifyFunctionPointers(@"
using System;
unsafe class C
{
    static void M(object o) => Console.Write(""object"" + o.ToString());
    static void M(string s) => Console.Write(""string"" + s);
    static void M(int i) => Console.Write(""int"" + i.ToString());
    static void Main()
    {
        delegate*<string, void> ptr = &M;
        ptr(""1"");
    }
}", expectedOutput: "string1");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (delegate*<string,void> V_0)
  IL_0000:  ldftn      ""void C.M(string)""
  IL_0006:  stloc.0
  IL_0007:  ldstr      ""1""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate*<string,void>""
  IL_0012:  ret
}");
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
                // (9,44): error CS8757: No overload for 'M1' matches function pointer 'delegate*<ref string,void>'
                //         delegate*<ref string, void> ptr1 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<ref string,void>").WithLocation(9, 44),
                // (10,40): error CS8757: No overload for 'M1' matches function pointer 'delegate*<string,void>'
                //         delegate*<string, void> ptr2 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<string,void>").WithLocation(10, 40),
                // (11,43): error CS8757: No overload for 'M2' matches function pointer 'delegate*<in string,void>'
                //         delegate*<in string, void> ptr3 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<in string,void>").WithLocation(11, 43),
                // (12,40): error CS8757: No overload for 'M2' matches function pointer 'delegate*<string,void>'
                //         delegate*<string, void> ptr4 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<string,void>").WithLocation(12, 40),
                // (13,44): error CS8757: No overload for 'M3' matches function pointer 'delegate*<out object,void>'
                //         delegate*<out object, void> ptr5 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<out object,void>").WithLocation(13, 44),
                // (14,40): error CS8757: No overload for 'M3' matches function pointer 'delegate*<string,void>'
                //         delegate*<string, void> ptr6 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<string,void>").WithLocation(14, 40)
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
        delegate*<ref object, void> ptr4 = &M4;
        delegate*<in object, void> ptr5 = &M4;
        delegate*<out object, void> ptr6 = &M4;
        delegate*<object> ptr7 = &M5;
        delegate*<object> ptr8 = &M6;
        delegate*<ref object> ptr9 = &M7;
        delegate*<ref readonly object> ptr10 = &M7;
    }
}");

            comp.VerifyDiagnostics(
                // (13,40): error CS8757: No overload for 'M1' matches function pointer 'delegate*<object,void>'
                //         delegate*<object, void> ptr1 = &M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M1").WithArguments("M1", "delegate*<object,void>").WithLocation(13, 40),
                // (14,40): error CS8757: No overload for 'M2' matches function pointer 'delegate*<object,void>'
                //         delegate*<object, void> ptr2 = &M2;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M2").WithArguments("M2", "delegate*<object,void>").WithLocation(14, 40),
                // (15,40): error CS8757: No overload for 'M3' matches function pointer 'delegate*<object,void>'
                //         delegate*<object, void> ptr3 = &M3;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M3").WithArguments("M3", "delegate*<object,void>").WithLocation(15, 40),
                // (16,44): error CS8757: No overload for 'M4' matches function pointer 'delegate*<ref object,void>'
                //         delegate*<ref object, void> ptr4 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<ref object,void>").WithLocation(16, 44),
                // (17,43): error CS8757: No overload for 'M4' matches function pointer 'delegate*<in object,void>'
                //         delegate*<in object, void> ptr5 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<in object,void>").WithLocation(17, 43),
                // (18,44): error CS8757: No overload for 'M4' matches function pointer 'delegate*<out object,void>'
                //         delegate*<out object, void> ptr6 = &M4;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M4").WithArguments("M4", "delegate*<out object,void>").WithLocation(18, 44),
                // (19,35): error CS8758: Ref mismatch between 'C.M5()' and function pointer 'delegate*<object>'
                //         delegate*<object> ptr7 = &M5;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M5").WithArguments("C.M5()", "delegate*<object>").WithLocation(19, 35),
                // (20,35): error CS8758: Ref mismatch between 'C.M6()' and function pointer 'delegate*<object>'
                //         delegate*<object> ptr8 = &M6;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M6").WithArguments("C.M6()", "delegate*<object>").WithLocation(20, 35),
                // (21,39): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<object>'
                //         delegate*<ref object> ptr9 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<object>").WithLocation(21, 39),
                // (22,49): error CS8758: Ref mismatch between 'C.M7()' and function pointer 'delegate*<object>'
                //         delegate*<ref readonly object> ptr10 = &M7;
                Diagnostic(ErrorCode.ERR_FuncPtrRefMismatch, "M7").WithArguments("C.M7()", "delegate*<object>").WithLocation(22, 49)
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
  .locals init (delegate*<int,string> V_0)
  IL_0000:  ldftn      ""string C.Convert(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int,string>""
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
                // (9,35): error CS8757: No overload for 'M' matches function pointer 'delegate*<int,int>'
                //         delegate*<int, int> ptr = &M;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&M").WithArguments("M", "delegate*<int,int>").WithLocation(9, 35)
            );
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
  IL_0006:  call       ""void C.Caller(delegate*<string,void>)""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void AddressOf_CannotAssignToVoidStar()
        {
            var comp = CreateCompilationWithFunctionPointers(@"
unsafe class C
{
    static void M()
    {
        void* ptr = &M;
    }
}");

            comp.VerifyDiagnostics(
                // (6,21): error CS0428: Cannot convert method group 'M' to non-delegate type 'void*'. Did you intend to invoke the method?
                //         void* ptr = &M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "&M").WithArguments("M", "void*").WithLocation(6, 21)
            );
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
                // (11,48): error CS8785: '&' on method groups cannot be used in expression trees
                //         Expression<Func<string>> a = () => M1(&M2);
                Diagnostic(ErrorCode.ERR_AddressOfMethodGroupInExpressionTree, "M2").WithLocation(11, 48),
                // (12,44): error CS0149: Method name expected
                //         Expression<Func<string>> b = () => (&M2)();
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "(&M2)").WithLocation(12, 44)
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
  .locals init (delegate*<C,void> V_0)
  IL_0000:  ldftn      ""void C.M(I2)""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""C..ctor()""
  IL_000c:  ldloc.0
  IL_000d:  calli      ""delegate*<C,void>""
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
  .locals init (delegate*<int,void> V_0)
  IL_0000:  ldftn      ""void C.M1<int>(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldloc.0
  IL_0009:  calli      ""delegate*<int,void>""
  IL_000e:  ldftn      ""void C.M1<int>(int)""
  IL_0014:  stloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  ldloc.0
  IL_0017:  calli      ""delegate*<int,void>""
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
                // (10,35): error CS8757: No overload for 'M1' matches function pointer 'delegate*<C,void>'
                //         delegate*<C, void> ptr1 = &c.M1;
                Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&c.M1").WithArguments("M1", "delegate*<C,void>").WithLocation(10, 35),
                // (11,32): error CS8788: Cannot use a an extension method with a receiver as the target of a '&amp;' operator.
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
                delegate*<C,void> V_1)
  IL_0000:  ldftn      ""void CHelper.M1(C)""
  IL_0006:  newobj     ""C..ctor()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      ""int C.i""
  IL_0013:  stloc.1
  IL_0014:  ldloc.0
  IL_0015:  ldloc.1
  IL_0016:  calli      ""delegate*<C,void>""
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
            // This is analgous to MethodBodyModelTests.MethodGroupToDelegate04, where both methods
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
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program1.D(delegate*<int,void>)' and 'Program1.D(delegate*<long,void>)'
                //         D(&Y);
                Diagnostic(ErrorCode.ERR_AmbigCall, "D").WithArguments("Program1.D(delegate*<int,void>)", "Program1.D(delegate*<long,void>)").WithLocation(11, 9)
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
  .locals init (delegate*<object,void> V_0, //printer
                delegate*<delegate*<object,void>,string,void> V_1)
  IL_0000:  ldftn      ""void C.PrintWrapper(delegate*<string,void>, string)""
  IL_0006:  ldftn      ""void C.Printer(object)""
  IL_000c:  stloc.0
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldstr      ""1""
  IL_0014:  ldloc.1
  IL_0015:  calli      ""delegate*<delegate*<object,void>,string,void>""
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
  .locals init (delegate*<string,void>[] V_0, //ptrs
                int V_1, //i
                delegate*<string,void> V_2)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""delegate*<string,void>""
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
  IL_0029:  calli      ""delegate*<string,void>""
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
                delegate*<string,void>[] V_1,
                int V_2,
                delegate*<string,void> V_3,
                int V_4)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""delegate*<string,void>""
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
  IL_0032:  calli      ""delegate*<string,void>""
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
                delegate*<string,void> V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""delegate*<string,void>""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldftn      ""void C.Print(string)""
  IL_0015:  stelem.i
  IL_0016:  stfld      ""delegate*<string,void>[] C.arr1""
  IL_001b:  ldloc.0
  IL_001c:  ldfld      ""delegate*<string,void>[] C.arr2""
  IL_0021:  ldc.i4.0
  IL_0022:  ldftn      ""void C.Print(string)""
  IL_0028:  stelem.i
  IL_0029:  ldloc.0
  IL_002a:  dup
  IL_002b:  ldfld      ""delegate*<string,void>[] C.arr1""
  IL_0030:  ldc.i4.0
  IL_0031:  ldelem.i
  IL_0032:  stloc.1
  IL_0033:  ldstr      ""1""
  IL_0038:  ldloc.1
  IL_0039:  calli      ""delegate*<string,void>""
  IL_003e:  ldfld      ""delegate*<string,void>[] C.arr2""
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.i
  IL_0045:  stloc.1
  IL_0046:  ldstr      ""2""
  IL_004b:  ldloc.1
  IL_004c:  calli      ""delegate*<string,void>""
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
  IL_0001:  newarr     ""delegate*<string,void>""
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

        private static void VerifyFunctionPointerSymbol(TypeSymbol type, CallingConvention expectedConvention, (RefKind RefKind, Action<TypeSymbol> TypeVerifier) returnVerifier, params (RefKind RefKind, Action<TypeSymbol> TypeVerifier)[] argumentVerifiers)
        {
            FunctionPointerTypeSymbol funcPtr = (FunctionPointerTypeSymbol)type;

            FunctionPointerUtilities.CommonVerifyFunctionPointer(funcPtr);

            var signature = funcPtr.Signature;
            Assert.Equal(expectedConvention, signature.CallingConvention);

            Assert.Equal(returnVerifier.RefKind, signature.RefKind);
            returnVerifier.TypeVerifier(signature.ReturnType);

            Assert.Equal(argumentVerifiers.Length, signature.ParameterCount);
            for (int i = 0; i < argumentVerifiers.Length; i++)
            {
                Assert.Equal(argumentVerifiers[i].RefKind, signature.Parameters[i].RefKind);
                argumentVerifiers[i].TypeVerifier(signature.Parameters[i].Type);
            }
        }

        private static Action<TypeSymbol> IsVoidType() => typeSymbol => Assert.True(typeSymbol.IsVoidType());

        private static Action<TypeSymbol> IsSpecialType(SpecialType specialType)
            => typeSymbol => Assert.Equal(specialType, typeSymbol.SpecialType);

        private static Action<TypeSymbol> IsTypeName(string typeName)
            => typeSymbol => Assert.Equal(typeName, typeSymbol.Name);

        private static Action<TypeSymbol> IsArrayType(Action<TypeSymbol> arrayTypeVerifier)
            => typeSymbol =>
            {
                Assert.True(typeSymbol.IsArray());
                arrayTypeVerifier(((ArrayTypeSymbol)typeSymbol).ElementType);
            };

        private static Action<TypeSymbol> IsFunctionPointerTypeSymbol(CallingConvention callingConvention, (RefKind, Action<TypeSymbol>) returnVerifier, params (RefKind, Action<TypeSymbol>)[] argumentVerifiers)
            => typeSymbol => VerifyFunctionPointerSymbol((FunctionPointerTypeSymbol)typeSymbol, callingConvention, returnVerifier, argumentVerifiers);

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
    }
}
