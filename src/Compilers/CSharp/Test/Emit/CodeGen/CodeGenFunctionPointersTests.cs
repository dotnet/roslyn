// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenFunctionPointersTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyFunctionPointers(string source, Action<ModuleSymbol>? symbolValidator = null, string? expectedOutput = null, Verification verify = Verification.Passes)
        {
            return CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe, symbolValidator: symbolValidator, expectedOutput: expectedOutput, verify: verify);
        }

        private CompilationVerifier CompileAndVerifyFunctionPointersWithIl(string source, string ilStub, Action<ModuleSymbol>? symbolValidator = null, string? expectedOutput = null)
        {
            var comp = CreateCompilationWithIL(source, ilStub, parseOptions: TestOptions.RegularPreview, options: expectedOutput is null ? TestOptions.UnsafeReleaseDll : TestOptions.UnsafeReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: symbolValidator);
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
}", symbolValidator: symbolValidator, verify: Verification.Skipped);

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
}", verify: Verification.Skipped);
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
  IL_000c:  calli      0x4
  IL_0011:  ret
}");
        }

        [Theory(Skip = "PROTOTYPE(func-ptr)")]
        [InlineData("cdecl")]
        [InlineData("thiscall")]
        [InlineData("stdcall")]
        public void UnmanagedCallingConventions(string convention)
        {
            var ilStub = $@"
.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{{
    // Methods
    .method public hidebysig static 
        method unmanaged {convention} void *() LoadPtr () cil managed 
    {{
        nop
        ldftn unmanaged cdecl void Program::Called()
        ret
    }} // end of method Program::Main

    .method private hidebysig static 
        unmanaged {convention} void Called () cil managed 
    {{
        nop
        ldstr ""Called""
        call void [mscorlib]System.Console::WriteLine(string)
        nop
        ret
    }} // end of Program::Called

    .method public hidebysig specialname rtspecialname
        instance void .ctor() cil managed
    {{
            ldarg.0
            call instance void[mscorlib] System.Object::.ctor()
            nop
            ret
    }} // end of Program::.ctor
}}
";

            var source = $@"
class Caller
{{
    public unsafe static void Main()
    {{
        Call(Program.LoadPtr());
    }}

    public unsafe static void Call(delegate* {convention}<void> ptr)
    {{
        ptr();
    }}
}}";

            var verifier = CompileAndVerifyFunctionPointersWithIl(source, ilStub, expectedOutput: "Called");
            verifier.VerifyIL($"Caller.Call(delegate* {convention}<void>)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  calli      0x2
  IL_0006:  ret
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
  IL_001b:  calli      0x6
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
  IL_001b:  calli      0x6
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
  IL_000c:  calli      0x2
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
        var retValue = Program.LoadPtr()(""Unused"");
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
  IL_000c:  calli      0x2
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
  .locals init (delegate*<string,string> V_0) //inner
  IL_0000:  call       ""delegate*<delegate*<string,string>> Program.LoadPtr()""
  IL_0005:  calli      0x2
  IL_000a:  stloc.0
  IL_000b:  ldstr      ""Returned""
  IL_0010:  ldloc.0
  IL_0011:  calli      0x3
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
  IL_0011:  calli      0x4
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
  .locals init (delegate*<ref string,void> V_0, //pointer
                string V_1) //str
  IL_0000:  call       ""delegate*<ref string,void> Program.LoadPtr()""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""Unset""
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  ldloc.0
  IL_000f:  calli      0x3
  IL_0014:  ldloc.1
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void RefReturnUnused()
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

        // typeof() on function pointer

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
