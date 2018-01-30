// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadCustomModifiers : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var modifiersModule = assemblies[0].Modules[0];


            var modifiers = modifiersModule.GlobalNamespace.GetTypeMembers("Modifiers").Single();

            FieldSymbol f0 = modifiers.GetMembers("F0").OfType<FieldSymbol>().Single();

            Assert.Equal(1, f0.CustomModifiers.Length);

            var f0Mod = f0.CustomModifiers[0];

            Assert.True(f0Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString());

            MethodSymbol m1 = modifiers.GetMembers("F1").OfType<MethodSymbol>().Single();
            ParameterSymbol p1 = m1.Parameters[0];
            ParameterSymbol p2 = modifiers.GetMembers("F2").OfType<MethodSymbol>().Single().Parameters[0];

            ParameterSymbol p4 = modifiers.GetMembers("F4").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m5 = modifiers.GetMembers("F5").OfType<MethodSymbol>().Single();
            ParameterSymbol p5 = m5.Parameters[0];

            ParameterSymbol p6 = modifiers.GetMembers("F6").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m7 = modifiers.GetMembers("F7").OfType<MethodSymbol>().Single();

            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length);

            Assert.Equal(1, p1.CustomModifiers.Length);

            var p1Mod = p1.CustomModifiers[0];

            Assert.True(p1Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString());

            Assert.Equal(2, p2.CustomModifiers.Length);

            foreach (var p2Mod in p2.CustomModifiers)
            {
                Assert.True(p2Mod.IsOptional);
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString());
            }

            Assert.Equal(SymbolKind.ErrorType, p4.Type.Kind);

            Assert.True(m5.ReturnsVoid);
            Assert.Equal(1, m5.ReturnTypeCustomModifiers.Length);

            var m5Mod = m5.ReturnTypeCustomModifiers[0];
            Assert.True(m5Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p5.CustomModifiers.Length);

            ArrayTypeSymbol p5Type = (ArrayTypeSymbol)p5.Type;

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString());

            Assert.Equal(1, p5Type.CustomModifiers.Length);
            var p5TypeMod = p5Type.CustomModifiers[0];

            Assert.True(p5TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p6.CustomModifiers.Length);

            PointerTypeSymbol p6Type = (PointerTypeSymbol)p6.Type;

            Assert.Equal("System.Int32", p6Type.PointedAtType.ToTestDisplayString());

            Assert.Equal(1, p6Type.CustomModifiers.Length);
            var p6TypeMod = p6Type.CustomModifiers[0];

            Assert.True(p6TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p6TypeMod.Modifier.ToTestDisplayString());

            Assert.False(m7.ReturnsVoid);
            Assert.Equal(1, m7.ReturnTypeCustomModifiers.Length);

            var m7Mod = m7.ReturnTypeCustomModifiers[0];
            Assert.True(m7Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString());
        }

        [Fact]
        public void TestCustomModifierComparisons()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("Comparisons");

            var methods = @class.GetMembers("Method").Select(m => (MethodSymbol)m);
            Assert.Equal(19, methods.Count()); //sanity check that we got as many as we were expecting - change as needed

            //methods should be pairwise NotEqual since they all have different modopts
            foreach (var method1 in methods)
            {
                foreach (var method2 in methods)
                {
                    if (!ReferenceEquals(method1, method2))
                    {
                        //use a comparer that checks both return type and custom modifiers
                        Assert.False(MemberSignatureComparer.RuntimeImplicitImplementationComparer.Equals(method1, method2));
                    }
                }
            }
        }

        [Fact]
        public void TestPropertyTypeCustomModifiers()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");
            var property = @class.GetMember<PropertySymbol>("Property11");
            var propertyTypeCustomModifier = property.TypeCustomModifiers.Single();

            Assert.Equal("System.Runtime.CompilerServices.IsConst", propertyTypeCustomModifier.Modifier.ToTestDisplayString());
            Assert.True(propertyTypeCustomModifier.IsOptional);

            var propertyType = property.Type;
            Assert.Equal(TypeKind.Array, propertyType.TypeKind);

            var arrayPropertyType = (ArrayTypeSymbol)propertyType;
            var arrayPropertyTypeCustomModifiers = arrayPropertyType.CustomModifiers.Single();
            Assert.Equal("System.Runtime.CompilerServices.IsConst", arrayPropertyTypeCustomModifiers.Modifier.ToTestDisplayString());
            Assert.True(arrayPropertyTypeCustomModifiers.IsOptional);
        }

        [Fact]
        public void TestMethodCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("MethodCustomModifierCombinations");

            Assert.Equal(4, @class.GetMember<MethodSymbol>("Method1111").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1110").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1101").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1100").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1011").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1010").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1001").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method1000").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method0111").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0110").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0101").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0100").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0011").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0010").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0001").CustomModifierCount());
            Assert.Equal(0, @class.GetMember<MethodSymbol>("Method0000").CustomModifierCount());
        }

        [Fact]
        public void TestPropertyCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");

            Assert.Equal(2, @class.GetMember<PropertySymbol>("Property11").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<PropertySymbol>("Property10").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<PropertySymbol>("Property01").CustomModifierCount());
            Assert.Equal(0, @class.GetMember<PropertySymbol>("Property00").CustomModifierCount());
        }

        [Fact]
        public void TestEventCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("EventCustomModifierCombinations");

            Assert.True(@class.GetMember<EventSymbol>("Event11").Type.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(1, @class.GetMember<EventSymbol>("Event10").Type.CustomModifierCount());
            Assert.True(@class.GetMember<EventSymbol>("Event01").Type.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(0, @class.GetMember<EventSymbol>("Event00").Type.CustomModifierCount());
        }

        [Fact]
        public void TestFieldCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                    TestReferences.NetFx.v4_0_21006.mscorlib
                });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("FieldCustomModifierCombinations");

            Assert.Equal(2, CustomModifierCount(@class.GetMember<FieldSymbol>("field11")));
            Assert.Equal(1, CustomModifierCount(@class.GetMember<FieldSymbol>("field10")));
            Assert.Equal(1, CustomModifierCount(@class.GetMember<FieldSymbol>("field01")));
            Assert.Equal(0, CustomModifierCount(@class.GetMember<FieldSymbol>("field00")));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Delegates_Parameters()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance void Invoke ([in] int32& x) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke ([in] int32& x, class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance void EndInvoke ([in] int32& x, class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateStandardCompilation(@"
class Test
{
    void M(D d) => d(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,20): error CS0570: 'D.Invoke(in int)' is not supported by the language
                //     void M(D d) => d(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "d(0)").WithArguments("D.Invoke(in int)").WithLocation(4, 20));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Delegates_Parameters_Modopt()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance void Invoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x,
        class [mscorlib]System.AsyncCallback callback,
        object 'object'
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance void EndInvoke (
        [in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x,
        class [mscorlib]System.IAsyncResult result
    ) runtime managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateStandardCompilation(@"
class Test
{
    void M(D d) => d(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,20): error CS0570: 'D.Invoke(in int)' is not supported by the language
                //     void M(D d) => d(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "d(0)").WithArguments("D.Invoke(in int)").WithLocation(4, 20));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Delegates_ReturnTypes()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& Invoke () runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            var c = CreateStandardCompilation(@"
class Test
{
    ref readonly int M(D d) => ref d();
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,36): error CS0570: 'D.Invoke()' is not supported by the language
                //     ref readonly int M(D d) => ref d();
                Diagnostic(ErrorCode.ERR_BindToBogus, "d()").WithArguments("D.Invoke()").WithLocation(4, 36));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Delegates_ReturnTypes_Modopt()
        {
            var reference = CompileIL(@"
.class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) Invoke () runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
    {
    }
    .method public hidebysig newslot virtual instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }
}");

            CreateStandardCompilation(@"
class Test
{
    ref readonly int M(D d) => ref d();
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,36): error CS0570: 'D.Invoke()' is not supported by the language
                //     ref readonly int M(D d) => ref d();
                Diagnostic(ErrorCode.ERR_BindToBogus, "d()").WithArguments("D.Invoke()").WithLocation(4, 36));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Properties()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance int32& get_X () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& X()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& RefTest::get_X()
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public ref readonly int M(RefTest obj) => ref obj.X;
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,55): error CS0570: 'RefTest.X' is not supported by the language
                //     public ref readonly int M(RefTest obj) => ref obj.X;
                Diagnostic(ErrorCode.ERR_BindToBogus, "X").WithArguments("RefTest.X").WithLocation(4, 55));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Properties_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig specialname instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) get_X () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) X()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) RefTest::get_X()
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public ref readonly int M(RefTest obj) => ref obj.X;
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,55): error CS0570: 'RefTest.X' is not supported by the language
                //     public ref readonly int M(RefTest obj) => ref obj.X;
                Diagnostic(ErrorCode.ERR_BindToBogus, "X").WithArguments("RefTest.X").WithLocation(4, 55));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Method_Parameters_Virtual()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Method_Parameters_Virtual_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Method_Parameters_Abstract()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot abstract virtual instance void M ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Method_Parameters_Abstract_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig newslot abstract virtual instance void M ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
class Test
{
    public int M(RefTest obj) => obj.M(0);
}", references: new[] { reference }).VerifyDiagnostics(
                // (4,38): error CS0570: 'RefTest.M(in int)' is not supported by the language
                //     public int M(RefTest obj) => obj.M(0);
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M(in int)").WithLocation(4, 38));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_Parameters_Abstract()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32&  x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_Parameters_Abstract_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi abstract beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot abstract virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method public hidebysig specialname newslot abstract virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    }

    .method family hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_Parameters_Virtual()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& x)
    {
        .get instance int32 RefTest::get_Item(int32&)
        .set instance void RefTest::set_Item(int32&, int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_Parameters_Virtual_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname newslot virtual instance int32 get_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x) cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig specialname newslot virtual instance void set_Item ([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x, int32 'value') cil managed 
    {
        .param [1]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32 Item([in] int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) x)
    {
        .get instance int32 RefTest::get_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute))
        .set instance void RefTest::set_Item(int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute), int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        obj[0] = obj[1];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,9): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[in int]").WithLocation(6, 9),
                // (6,18): error CS0570: 'RefTest.this[in int]' is not supported by the language
                //         obj[0] = obj[1];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[1]").WithArguments("RefTest.this[in int]").WithLocation(6, 18));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_ReturnType()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname instance int32& get_Item (int32 x) cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& Item(int32 x)
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& RefTest::get_Item(int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj[0];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,34): error CS0570: 'RefTest.this[int]' is not supported by the language
                //         ref readonly int x = ref obj[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[int]").WithLocation(6, 34));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Indexers_ReturnType_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (01 00 04 49 74 65 6d 00 00)

    .method public hidebysig specialname instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) get_Item (int32 x) cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .property instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) Item(int32 x)
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .get instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) RefTest::get_Item(int32)
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj[0];
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,34): error CS0570: 'RefTest.this[int]' is not supported by the language
                //         ref readonly int x = ref obj[0];
                Diagnostic(ErrorCode.ERR_BindToBogus, "obj[0]").WithArguments("RefTest.this[int]").WithLocation(6, 34));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Methods_ReturnType()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig instance int32& M () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj.M();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,38): error CS0570: 'RefTest.M()' is not supported by the language
                //         ref readonly int x = ref obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M()").WithLocation(6, 38));
        }

        [Fact]
        public void RejectIsReadOnlySymbolsThatShouldHaveInAttributeModreqButDoNot_Methods_ReturnType_ModOpt()
        {
            var reference = CompileIL(@"
.class public auto ansi beforefieldinit RefTest extends [mscorlib]System.Object
{
    .method public hidebysig instance int32& modopt([mscorlib]System.Runtime.InteropServices.InAttribute) M () cil managed 
    {
        .param [0]
        .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}");

            CreateStandardCompilation(@"
public class Test
{
    public void M(RefTest obj)
    {
        ref readonly int x = ref obj.M();
    }
}", references: new[] { reference }).VerifyDiagnostics(
                // (6,38): error CS0570: 'RefTest.M()' is not supported by the language
                //         ref readonly int x = ref obj.M();
                Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("RefTest.M()").WithLocation(6, 38));
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the type
        /// of the specified field.
        /// </summary>
        internal static int CustomModifierCount(FieldSymbol field)
        {
            int count = 0;

            count += field.CustomModifiers.Length;
            count += field.Type.CustomModifierCount();

            return count;
        }
    }
}
