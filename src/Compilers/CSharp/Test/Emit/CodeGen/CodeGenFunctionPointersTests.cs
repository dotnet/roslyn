// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
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

        [Theory]
        [InlineData("", CallingConvention.Default)]
        [InlineData("cdecl", CallingConvention.CDecl)]
        [InlineData("managed", CallingConvention.Default)]
        [InlineData("thiscall", CallingConvention.ThisCall)]
        [InlineData("stdcall", CallingConvention.Standard)]
        internal void CallingConventions(string conventionString, CallingConvention expectedConvention)
        {
            var comp = CompileAndVerifyFunctionPointers(@$"
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
  IL_0001:  calli      0x2
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
  IL_0005:  calli      0x3
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
  IL_000c:  calli      0x3
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
  IL_000d:  calli      0x2
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
  IL_001a:  calli      0x5
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
  IL_001b:  calli      0x8
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
  IL_001a:  calli      0x5
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
  IL_001f:  calli      0x9
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
  IL_001b:  calli      0x5
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
  IL_001b:  calli      0x5
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
  IL_000c:  calli      0x1
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
  IL_000c:  calli      0x1
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
  IL_0005:  calli      0x1
  IL_000a:  stloc.0
  IL_000b:  ldstr      ""Returned""
  IL_0010:  ldloc.0
  IL_0011:  calli      0x2
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
  IL_0011:  calli      0x3
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
  IL_000f:  calli      0x2
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
  IL_000f:  calli      0x2
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
  IL_0005:  calli      0x1
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
  IL_0016:  calli      0x3
  IL_001b:  ldloc.1
  IL_001c:  calli      0x3
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ldloc.0
  IL_0027:  stloc.1
  IL_0028:  ldstr      ""Argument To Function 2""
  IL_002d:  ldloc.1
  IL_002e:  calli      0x3
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
        public void InternalsVisibleToAccessChecks()
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
    }
}
