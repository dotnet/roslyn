' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingMethods : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            ' Metadata is in Compilers\Test\Resources
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestResources.General.MDTestLib1,
                    TestResources.General.MDTestLib2,
                    TestResources.SymbolsTests.Methods.CSMethods,
                    TestResources.SymbolsTests.Methods.VBMethods,
                    TestResources.NetFX.v4_0_21006.mscorlib,
                    TestResources.SymbolsTests.Methods.ByRefReturn
                }, importInternals:=True)


            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(1).Modules(0)
            Dim module3 = assemblies(2).Modules(0)
            Dim module4 = assemblies(3).Modules(0)
            Dim module5 = assemblies(4).Modules(0)
            Dim byrefReturn = assemblies(5).Modules(0)

            Dim TC10 = module2.GlobalNamespace.GetTypeMembers("TC10").Single()

            Assert.Equal(6, TC10.GetMembers().Length())

            Dim M1 = DirectCast(TC10.GetMembers("M1").Single(), MethodSymbol)
            Dim M2 = DirectCast(TC10.GetMembers("M2").Single(), MethodSymbol)
            Dim M3 = DirectCast(TC10.GetMembers("M3").Single(), MethodSymbol)
            Dim M4 = DirectCast(TC10.GetMembers("M4").Single(), MethodSymbol)
            Dim M5 = DirectCast(TC10.GetMembers("M5").Single(), MethodSymbol)

            Assert.Equal("Sub TC10.M1()", M1.ToTestDisplayString())
            Assert.True(M1.IsSub)
            Assert.Equal(Accessibility.Public, (M1.DeclaredAccessibility))
            Assert.Same(module2, M1.Locations.Single().MetadataModule)
            Assert.False(M1.IsRuntimeImplemented()) ' test false case for PEMethodSymbols, true is covered in delegate tests

            Assert.Equal("Sub TC10.M2(m1_1 As System.Int32)", M2.ToTestDisplayString())
            Assert.True(M2.IsSub)
            Assert.Equal(Accessibility.Protected, (M2.DeclaredAccessibility))
            Assert.False(M2.IsRuntimeImplemented())

            Dim m1_1 = M2.Parameters(0)

            Assert.IsType(Of PEParameterSymbol)(m1_1)
            Assert.Same(m1_1.ContainingSymbol, M2)
            Assert.Equal(SymbolKind.Parameter, m1_1.Kind)
            Assert.Equal(Accessibility.NotApplicable, m1_1.DeclaredAccessibility)
            Assert.False(m1_1.IsMustOverride)
            Assert.False(m1_1.IsNotOverridable)
            Assert.False(m1_1.IsOverridable)
            Assert.False(m1_1.IsOverrides)
            Assert.False(m1_1.IsShared)

            Assert.Equal("Function TC10.M3() As TC8", M3.ToTestDisplayString())
            Assert.False(M3.IsSub)
            Assert.Equal(Accessibility.Protected, (M3.DeclaredAccessibility))

            Assert.Equal("Function TC10.M4(ByRef x As C1(Of System.Type), ByRef y As TC8) As C1(Of System.Type)", M4.ToTestDisplayString())
            Assert.False(M4.IsSub)
            Assert.Equal(Accessibility.Friend, (M4.DeclaredAccessibility))

            Assert.Equal("Sub TC10.M5(ByRef x As C1(Of System.Type)(,,), ByRef y As TC8())", M5.ToTestDisplayString())
            Assert.True(M5.IsSub)
            Assert.Equal(Accessibility.ProtectedOrFriend, (M5.DeclaredAccessibility))

            Dim M6 = TC10.GetMembers("M6")
            Assert.Equal(0, M6.Length())

            Dim C107 = module1.GlobalNamespace.GetTypeMembers("C107").Single()
            Dim C108 = C107.GetMembers("C108").Single()
            Assert.Equal(SymbolKind.NamedType, C108.Kind)

            Dim CS_C1 = module3.GlobalNamespace.GetTypeMembers("C1").Single()
            Dim sameName = CS_C1.GetMembers("SameName")
            Assert.Equal(2, sameName.Length)
            Assert.Equal(SymbolKind.Method, sameName(0).Kind)
            Assert.Equal("sameName", sameName(0).Name)
            Assert.Equal(SymbolKind.NamedType, sameName(1).Kind)
            Assert.Equal("SameName", sameName(1).Name)

            Assert.Equal(3, CS_C1.GetMembers("SameName2").Length())
            Assert.Equal(3, CS_C1.GetMembers("sameName2").Length())

            Assert.Equal(0, CS_C1.GetMembers("DoesntExist").Length())

            Dim VB_C1 = module4.GlobalNamespace.GetTypeMembers("C1").Single()

            Dim VB_C1_M1 = DirectCast(VB_C1.GetMembers("M1").Single(), MethodSymbol)
            Dim VB_C1_M2 = DirectCast(VB_C1.GetMembers("M2").Single(), MethodSymbol)
            Dim VB_C1_M3 = DirectCast(VB_C1.GetMembers("M3").Single(), MethodSymbol)
            Dim VB_C1_M4 = DirectCast(VB_C1.GetMembers("M4").Single(), MethodSymbol)

            Assert.False(VB_C1_M1.Parameters(0).IsOptional)
            Assert.False(VB_C1_M1.Parameters(0).HasExplicitDefaultValue)
            Assert.Same(module4, VB_C1_M1.Parameters(0).Locations.Single().MetadataModule)

            Assert.True(VB_C1_M2.Parameters(0).IsOptional)
            Assert.False(VB_C1_M2.Parameters(0).HasExplicitDefaultValue)

            Assert.True(VB_C1_M3.Parameters(0).IsOptional)
            Assert.True(VB_C1_M3.Parameters(0).HasExplicitDefaultValue)

            Assert.True(VB_C1_M4.Parameters(0).IsOptional)
            Assert.False(VB_C1_M4.Parameters(0).HasExplicitDefaultValue)

            Dim EmptyStructure = module4.GlobalNamespace.GetTypeMembers("EmptyStructure").Single()
            Assert.Equal(1, EmptyStructure.GetMembers().Length()) ' Implicit parameterless constructor
            Assert.Equal(0, EmptyStructure.GetMembers("NoMembersOrTypes").Length())

            Dim VB_C1_M5 = DirectCast(VB_C1.GetMembers("M5").Single(), MethodSymbol)
            Dim VB_C1_M6 = DirectCast(VB_C1.GetMembers("M6").Single(), MethodSymbol)
            Dim VB_C1_M7 = DirectCast(VB_C1.GetMembers("M7").Single(), MethodSymbol)
            Dim VB_C1_M8 = DirectCast(VB_C1.GetMembers("M8").Single(), MethodSymbol)
            Dim VB_C1_M9 = VB_C1.GetMembers("M9").OfType(Of MethodSymbol)()

            Assert.False(VB_C1_M5.IsGenericMethod) ' Check genericity before cracking signature
            Assert.True(VB_C1_M6.IsSub)
            Assert.False(VB_C1_M6.IsGenericMethod) ' Check genericity after cracking signature

            Assert.True(VB_C1_M7.IsGenericMethod) ' Check genericity before cracking signature
            Assert.Equal("Sub C1.M7(Of T)(x As System.Int32)", VB_C1_M7.ToTestDisplayString())
            Assert.True(VB_C1_M6.IsSub)
            Assert.True(VB_C1_M8.IsGenericMethod) ' Check genericity after cracking signature
            Assert.Equal("Sub C1.M8(Of T)(x As System.Int32)", VB_C1_M8.ToTestDisplayString())

            Assert.Equal(2, VB_C1_M9.Count())
            Assert.Equal(1, VB_C1_M9.Where(Function(m) m.IsGenericMethod).Count())
            Assert.Equal(1, VB_C1_M9.Where(Function(m) Not m.IsGenericMethod).Count())

            Dim VB_C1_M10 = DirectCast(VB_C1.GetMembers("M10").Single(), MethodSymbol)
            Assert.Equal("Sub C1.M10(Of T1)(x As T1)", VB_C1_M10.ToTestDisplayString())

            Dim VB_C1_M11 = DirectCast(VB_C1.GetMembers("M11").Single(), MethodSymbol)
            Assert.Equal("Function C1.M11(Of T2, T3)(x As T2) As T3", VB_C1_M11.ToTestDisplayString())
            Assert.Equal(0, VB_C1_M11.TypeParameters(0).ConstraintTypes.Length)
            Assert.Same(VB_C1, VB_C1_M11.TypeParameters(1).ConstraintTypes.Single())

            Dim VB_C1_M12 = DirectCast(VB_C1.GetMembers("M12").Single(), MethodSymbol)
            Assert.Equal(0, VB_C1_M12.TypeArguments.Length)
            Assert.False(VB_C1_M12.IsVararg)
            Assert.False(VB_C1_M12.IsExternalMethod)
            Assert.False(VB_C1_M12.IsShared)

            Dim LoadLibrary = DirectCast(VB_C1.GetMembers("LoadLibrary").Single(), MethodSymbol)
            Assert.True(LoadLibrary.IsExternalMethod)

            Dim VB_C2 = module4.GlobalNamespace.GetTypeMembers("C2").Single()

            Dim VB_C2_M1 = DirectCast(VB_C2.GetMembers("M1").Single(), MethodSymbol)
            Assert.Equal("Sub C2(Of T4).M1(Of T5)(x As T5, y As T4)", VB_C2_M1.ToTestDisplayString())

            Dim console = module5.GlobalNamespace.GetMembers("System").OfType(Of NamespaceSymbol)().Single().
                GetTypeMembers("Console").Single()

            Assert.Equal(1, console.GetMembers("WriteLine").OfType(Of MethodSymbol)().Where(Function(m) m.IsVararg).Count())
            Assert.True(console.GetMembers("WriteLine").OfType(Of MethodSymbol)().Where(Function(m) m.IsVararg).Single().IsShared)


            Dim VB_Modifiers1 = module4.GlobalNamespace.GetTypeMembers("Modifiers1").Single()

            Dim VB_Modifiers1_M1 = DirectCast(VB_Modifiers1.GetMembers("M1").Single(), MethodSymbol)
            Dim VB_Modifiers1_M2 = DirectCast(VB_Modifiers1.GetMembers("M2").Single(), MethodSymbol)
            Dim VB_Modifiers1_M3 = DirectCast(VB_Modifiers1.GetMembers("M3").Single(), MethodSymbol)
            Dim VB_Modifiers1_M4 = DirectCast(VB_Modifiers1.GetMembers("M4").Single(), MethodSymbol)
            Dim VB_Modifiers1_M5 = DirectCast(VB_Modifiers1.GetMembers("M5").Single(), MethodSymbol)
            Dim VB_Modifiers1_M6 = DirectCast(VB_Modifiers1.GetMembers("M6").Single(), MethodSymbol)
            Dim VB_Modifiers1_M7 = DirectCast(VB_Modifiers1.GetMembers("M7").Single(), MethodSymbol)
            Dim VB_Modifiers1_M8 = DirectCast(VB_Modifiers1.GetMembers("M8").Single(), MethodSymbol)
            Dim VB_Modifiers1_M9 = DirectCast(VB_Modifiers1.GetMembers("M9").Single(), MethodSymbol)

            Assert.True(VB_Modifiers1_M1.IsMustOverride)
            Assert.False(VB_Modifiers1_M1.IsOverridable)
            Assert.False(VB_Modifiers1_M1.IsNotOverridable)
            Assert.False(VB_Modifiers1_M1.IsOverloads)
            Assert.False(VB_Modifiers1_M1.IsOverrides)

            Assert.False(VB_Modifiers1_M2.IsMustOverride)
            Assert.True(VB_Modifiers1_M2.IsOverridable)
            Assert.False(VB_Modifiers1_M2.IsNotOverridable)
            Assert.False(VB_Modifiers1_M2.IsOverloads)
            Assert.False(VB_Modifiers1_M2.IsOverrides)

            Assert.False(VB_Modifiers1_M3.IsMustOverride)
            Assert.False(VB_Modifiers1_M3.IsOverridable)
            Assert.False(VB_Modifiers1_M3.IsNotOverridable)
            Assert.True(VB_Modifiers1_M3.IsOverloads)
            Assert.False(VB_Modifiers1_M3.IsOverrides)

            Assert.False(VB_Modifiers1_M4.IsMustOverride)
            Assert.False(VB_Modifiers1_M4.IsOverridable)
            Assert.False(VB_Modifiers1_M4.IsNotOverridable)
            Assert.False(VB_Modifiers1_M4.IsOverloads)
            Assert.False(VB_Modifiers1_M4.IsOverrides)

            Assert.False(VB_Modifiers1_M5.IsMustOverride)
            Assert.False(VB_Modifiers1_M5.IsOverridable)
            Assert.False(VB_Modifiers1_M5.IsNotOverridable)
            Assert.False(VB_Modifiers1_M5.IsOverloads)
            Assert.False(VB_Modifiers1_M5.IsOverrides)

            Assert.True(VB_Modifiers1_M6.IsMustOverride)
            Assert.False(VB_Modifiers1_M6.IsOverridable)
            Assert.False(VB_Modifiers1_M6.IsNotOverridable)
            Assert.True(VB_Modifiers1_M6.IsOverloads)
            Assert.False(VB_Modifiers1_M6.IsOverrides)

            Assert.False(VB_Modifiers1_M7.IsMustOverride)
            Assert.True(VB_Modifiers1_M7.IsOverridable)
            Assert.False(VB_Modifiers1_M7.IsNotOverridable)
            Assert.True(VB_Modifiers1_M7.IsOverloads)
            Assert.False(VB_Modifiers1_M7.IsOverrides)

            Assert.True(VB_Modifiers1_M8.IsMustOverride)
            Assert.False(VB_Modifiers1_M8.IsOverridable)
            Assert.False(VB_Modifiers1_M8.IsNotOverridable)
            Assert.False(VB_Modifiers1_M8.IsOverloads)
            Assert.False(VB_Modifiers1_M8.IsOverrides)

            Assert.False(VB_Modifiers1_M9.IsMustOverride)
            Assert.True(VB_Modifiers1_M9.IsOverridable)
            Assert.False(VB_Modifiers1_M9.IsNotOverridable)
            Assert.False(VB_Modifiers1_M9.IsOverloads)
            Assert.False(VB_Modifiers1_M9.IsOverrides)

            Dim VB_Modifiers2 = module4.GlobalNamespace.GetTypeMembers("Modifiers2").Single()

            Dim VB_Modifiers2_M1 = DirectCast(VB_Modifiers2.GetMembers("M1").Single(), MethodSymbol)
            Dim VB_Modifiers2_M2 = DirectCast(VB_Modifiers2.GetMembers("M2").Single(), MethodSymbol)
            Dim VB_Modifiers2_M6 = DirectCast(VB_Modifiers2.GetMembers("M6").Single(), MethodSymbol)
            Dim VB_Modifiers2_M7 = DirectCast(VB_Modifiers2.GetMembers("M7").Single(), MethodSymbol)

            Assert.True(VB_Modifiers2_M1.IsMustOverride)
            Assert.False(VB_Modifiers2_M1.IsOverridable)
            Assert.False(VB_Modifiers2_M1.IsNotOverridable)
            Assert.True(VB_Modifiers2_M1.IsOverloads)
            Assert.True(VB_Modifiers2_M1.IsOverrides)

            Assert.False(VB_Modifiers2_M2.IsMustOverride)
            Assert.False(VB_Modifiers2_M2.IsOverridable)
            Assert.True(VB_Modifiers2_M2.IsNotOverridable)
            Assert.True(VB_Modifiers2_M2.IsOverloads)
            Assert.True(VB_Modifiers2_M2.IsOverrides)

            Assert.True(VB_Modifiers2_M6.IsMustOverride)
            Assert.False(VB_Modifiers2_M6.IsOverridable)
            Assert.False(VB_Modifiers2_M6.IsNotOverridable)
            Assert.True(VB_Modifiers2_M6.IsOverloads)
            Assert.True(VB_Modifiers2_M6.IsOverrides)

            Assert.False(VB_Modifiers2_M7.IsMustOverride)
            Assert.False(VB_Modifiers2_M7.IsOverridable)
            Assert.True(VB_Modifiers2_M7.IsNotOverridable)
            Assert.True(VB_Modifiers2_M7.IsOverloads)
            Assert.True(VB_Modifiers2_M7.IsOverrides)

            Dim VB_Modifiers3 = module4.GlobalNamespace.GetTypeMembers("Modifiers3").Single()

            Dim VB_Modifiers3_M1 = DirectCast(VB_Modifiers3.GetMembers("M1").Single(), MethodSymbol)
            Dim VB_Modifiers3_M6 = DirectCast(VB_Modifiers3.GetMembers("M6").Single(), MethodSymbol)

            Assert.False(VB_Modifiers3_M1.IsMustOverride)
            Assert.False(VB_Modifiers3_M1.IsOverridable)
            Assert.False(VB_Modifiers3_M1.IsNotOverridable)
            Assert.True(VB_Modifiers3_M1.IsOverloads)
            Assert.True(VB_Modifiers3_M1.IsOverrides)

            Assert.False(VB_Modifiers3_M6.IsMustOverride)
            Assert.False(VB_Modifiers3_M6.IsOverridable)
            Assert.False(VB_Modifiers3_M6.IsNotOverridable)
            Assert.True(VB_Modifiers3_M6.IsOverloads)
            Assert.True(VB_Modifiers3_M6.IsOverrides)

            Dim CS_Modifiers1 = module3.GlobalNamespace.GetTypeMembers("Modifiers1").Single()

            Dim CS_Modifiers1_M1 = DirectCast(CS_Modifiers1.GetMembers("M1").Single(), MethodSymbol)
            Dim CS_Modifiers1_M2 = DirectCast(CS_Modifiers1.GetMembers("M2").Single(), MethodSymbol)
            Dim CS_Modifiers1_M3 = DirectCast(CS_Modifiers1.GetMembers("M3").Single(), MethodSymbol)
            Dim CS_Modifiers1_M4 = DirectCast(CS_Modifiers1.GetMembers("M4").Single(), MethodSymbol)

            Assert.True(CS_Modifiers1_M1.IsMustOverride)
            Assert.False(CS_Modifiers1_M1.IsOverridable)
            Assert.False(CS_Modifiers1_M1.IsNotOverridable)
            Assert.True(CS_Modifiers1_M1.IsOverloads)
            Assert.False(CS_Modifiers1_M1.IsOverrides)

            Assert.False(CS_Modifiers1_M2.IsMustOverride)
            Assert.True(CS_Modifiers1_M2.IsOverridable)
            Assert.False(CS_Modifiers1_M2.IsNotOverridable)
            Assert.True(CS_Modifiers1_M2.IsOverloads)
            Assert.False(CS_Modifiers1_M2.IsOverrides)

            Assert.False(CS_Modifiers1_M3.IsMustOverride)
            Assert.False(CS_Modifiers1_M3.IsOverridable)
            Assert.False(CS_Modifiers1_M3.IsNotOverridable)
            Assert.True(CS_Modifiers1_M3.IsOverloads)
            Assert.False(CS_Modifiers1_M3.IsOverrides)

            Assert.False(CS_Modifiers1_M4.IsMustOverride)
            Assert.True(CS_Modifiers1_M4.IsOverridable)
            Assert.False(CS_Modifiers1_M4.IsNotOverridable)
            Assert.True(CS_Modifiers1_M4.IsOverloads)
            Assert.False(CS_Modifiers1_M4.IsOverrides)


            Dim CS_Modifiers2 = module3.GlobalNamespace.GetTypeMembers("Modifiers2").Single()

            Dim CS_Modifiers2_M1 = DirectCast(CS_Modifiers2.GetMembers("M1").Single(), MethodSymbol)
            Dim CS_Modifiers2_M2 = DirectCast(CS_Modifiers2.GetMembers("M2").Single(), MethodSymbol)
            Dim CS_Modifiers2_M3 = DirectCast(CS_Modifiers2.GetMembers("M3").Single(), MethodSymbol)

            Assert.False(CS_Modifiers2_M1.IsMustOverride)
            Assert.False(CS_Modifiers2_M1.IsOverridable)
            Assert.True(CS_Modifiers2_M1.IsNotOverridable)
            Assert.True(CS_Modifiers2_M1.IsOverloads)
            Assert.True(CS_Modifiers2_M1.IsOverrides)

            Assert.True(CS_Modifiers2_M2.IsMustOverride)
            Assert.False(CS_Modifiers2_M2.IsOverridable)
            Assert.False(CS_Modifiers2_M2.IsNotOverridable)
            Assert.True(CS_Modifiers2_M2.IsOverloads)
            Assert.True(CS_Modifiers2_M2.IsOverrides)

            Assert.False(CS_Modifiers2_M3.IsMustOverride)
            Assert.True(CS_Modifiers2_M3.IsOverridable)
            Assert.False(CS_Modifiers2_M3.IsNotOverridable)
            Assert.True(CS_Modifiers2_M3.IsOverloads)
            Assert.False(CS_Modifiers2_M3.IsOverrides)

            Dim CS_Modifiers3 = module3.GlobalNamespace.GetTypeMembers("Modifiers3").Single()

            Dim CS_Modifiers3_M1 = DirectCast(CS_Modifiers3.GetMembers("M1").Single(), MethodSymbol)
            Dim CS_Modifiers3_M3 = DirectCast(CS_Modifiers3.GetMembers("M3").Single(), MethodSymbol)
            Dim CS_Modifiers3_M4 = DirectCast(CS_Modifiers3.GetMembers("M4").Single(), MethodSymbol)

            Assert.False(CS_Modifiers3_M1.IsMustOverride)
            Assert.False(CS_Modifiers3_M1.IsOverridable)
            Assert.False(CS_Modifiers3_M1.IsNotOverridable)
            Assert.True(CS_Modifiers3_M1.IsOverloads)
            Assert.True(CS_Modifiers3_M1.IsOverrides)

            Assert.False(CS_Modifiers3_M3.IsMustOverride)
            Assert.False(CS_Modifiers3_M3.IsOverridable)
            Assert.False(CS_Modifiers3_M3.IsNotOverridable)
            Assert.True(CS_Modifiers3_M3.IsOverloads)
            Assert.False(CS_Modifiers3_M3.IsOverrides)

            Assert.True(CS_Modifiers3_M4.IsMustOverride)
            Assert.False(CS_Modifiers3_M4.IsOverridable)
            Assert.False(CS_Modifiers3_M4.IsNotOverridable)
            Assert.True(CS_Modifiers3_M4.IsOverloads)
            Assert.False(CS_Modifiers3_M4.IsOverrides)

            Dim byrefReturnMethod = byrefReturn.GlobalNamespace.GetTypeMembers("ByRefReturn").Single().GetMembers("M").OfType(Of MethodSymbol)().Single()
            Assert.True(byrefReturnMethod.ReturnsByRef)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationSimple()
            Dim assembly = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp}).Single()

            Dim globalNamespace = assembly.GlobalNamespace

            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("Interface").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, [interface].TypeKind)

            Dim interfaceMethod = DirectCast([interface].GetMembers("Method").Single(), MethodSymbol)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Class").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)
            Assert.True([class].Interfaces.Contains([interface]))

            Dim classMethod = DirectCast([class].GetMembers("Interface.Method").Single(), MethodSymbol)
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)

            Dim explicitImpl = classMethod.ExplicitInterfaceImplementations.Single()
            Assert.Equal(interfaceMethod, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationMultiple()
            Dim assembly = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL}).[Single]()

            Dim globalNamespace = assembly.GlobalNamespace

            Dim interface1 = DirectCast(globalNamespace.GetTypeMembers("I1").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, interface1.TypeKind)

            Dim interface1Method = DirectCast(interface1.GetMembers("Method1").Single(), MethodSymbol)

            Dim interface2 = DirectCast(globalNamespace.GetTypeMembers("I2").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, interface2.TypeKind)

            Dim interface2Method = DirectCast(interface2.GetMembers("Method2").Single(), MethodSymbol)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("C").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)
            Assert.True([class].Interfaces.Contains(interface1))
            Assert.True([class].Interfaces.Contains(interface2))

            Dim classMethod = DirectCast([class].GetMembers("Method").Single(), MethodSymbol)
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)

            Dim explicitImpls = classMethod.ExplicitInterfaceImplementations
            Assert.Equal(2, explicitImpls.Length)
            Assert.Equal(interface1Method, explicitImpls(0))
            Assert.Equal(interface2Method, explicitImpls(1))
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationGeneric()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.NetFx.v4_0_30319.mscorlib,
                 TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace

            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("IGeneric").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, [interface].TypeKind)

            Dim interfaceMethod = DirectCast([interface].GetMembers("Method").Last(), MethodSymbol)
            Assert.Equal("Sub IGeneric(Of T).Method(Of U)(t As T, u As U)", interfaceMethod.ToTestDisplayString())

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Generic").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)

            Dim substitutedInterface = [class].Interfaces.Single()
            Assert.Equal([interface], substitutedInterface.ConstructedFrom)

            Dim substitutedInterfaceMethod = DirectCast(substitutedInterface.GetMembers("Method").Last(), MethodSymbol)
            Assert.Equal("Sub IGeneric(Of S).Method(Of U)(t As S, u As U)", substitutedInterfaceMethod.ToTestDisplayString())
            Assert.Equal(interfaceMethod, substitutedInterfaceMethod.OriginalDefinition)

            Dim classMethod = DirectCast([class].GetMembers("IGeneric<S>.Method").Last(), MethodSymbol)
            Assert.Equal("Sub Generic(Of S).IGeneric<S>.Method(Of V)(s As S, v As V)", classMethod.ToTestDisplayString())
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)

            Dim explicitImpl = classMethod.ExplicitInterfaceImplementations.Single()
            Assert.Equal(substitutedInterfaceMethod, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationConstructed()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.NetFx.v4_0_30319.mscorlib,
                 TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace

            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("IGeneric").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, [interface].TypeKind)

            Dim interfaceMethod = DirectCast([interface].GetMembers("Method").Last(), MethodSymbol)
            Assert.Equal("Sub IGeneric(Of T).Method(Of U)(t As T, u As U)", interfaceMethod.ToTestDisplayString())

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Constructed").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)

            Dim substitutedInterface = [class].Interfaces.Single()
            Assert.Equal([interface], substitutedInterface.ConstructedFrom)

            Dim substitutedInterfaceMethod = DirectCast(substitutedInterface.GetMembers("Method").Last(), MethodSymbol)
            Assert.Equal("Sub IGeneric(Of System.Int32).Method(Of U)(t As System.Int32, u As U)", substitutedInterfaceMethod.ToTestDisplayString())
            Assert.Equal(interfaceMethod, substitutedInterfaceMethod.OriginalDefinition)

            Dim classMethod = DirectCast([class].GetMembers("IGeneric<System.Int32>.Method").Last(), MethodSymbol)
            Assert.Equal("Sub Constructed.IGeneric<System.Int32>.Method(Of W)(i As System.Int32, w As W)", classMethod.ToTestDisplayString())
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)

            Dim explicitImpl = classMethod.ExplicitInterfaceImplementations.Single()
            Assert.Equal(substitutedInterfaceMethod, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationInterfaceCycleSuccess()
            Dim assembly = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL}).Single()

            Dim globalNamespace = assembly.GlobalNamespace

            Dim cyclicInterface = DirectCast(globalNamespace.GetTypeMembers("ImplementsSelf").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, cyclicInterface.TypeKind)

            Dim implementedInterface = DirectCast(globalNamespace.GetTypeMembers("I1").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Interface, implementedInterface.TypeKind)

            Dim interface2Method = DirectCast(implementedInterface.GetMembers("Method1").Single(), MethodSymbol)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("InterfaceCycleSuccess").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)
            Assert.True([class].Interfaces.Contains(cyclicInterface))
            Assert.True([class].Interfaces.Contains(implementedInterface))

            Dim classMethod = DirectCast([class].GetMembers("Method").Single(), MethodSymbol)
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)

            Dim explicitImpl = classMethod.ExplicitInterfaceImplementations.Single()
            Assert.Equal(interface2Method, explicitImpl)
        End Sub

        ''' <summary>
        ''' IL type explicitly overrides an interface method on an unrelated generic interface.
        ''' ExplicitInterfaceImplementations should be empty.
        ''' </summary>
        <Fact>
        Public Sub TestExplicitImplementationOfUnrelatedGenericInterfaceMethod()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.NetFx.v4_0_30319.mscorlib,
                 TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace

            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("IUnrelated").Last(), NamedTypeSymbol)
            Assert.Equal(1, [interface].Arity)
            Assert.Equal(TypeKind.Interface, [interface].TypeKind)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("ExplicitlyImplementsUnrelatedInterfaceMethods").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.Class, [class].TypeKind)
            Assert.Equal(0, [class].AllInterfaces.Length)

            Dim classMethod = DirectCast([class].GetMembers("Method2").Single(), MethodSymbol)
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind)
            Assert.Equal(0, classMethod.ExplicitInterfaceImplementations.Length)

            Dim classGenericMethod = DirectCast([class].GetMembers("Method2").Single(), MethodSymbol)
            Assert.Equal(MethodKind.Ordinary, classGenericMethod.MethodKind)
            Assert.Equal(0, classGenericMethod.ExplicitInterfaceImplementations.Length)
        End Sub

        ''' <summary>
        ''' In metadata, nested types implicitly share all type parameters of their containing types.
        ''' This results in some extra computations when mapping a type parameter position to a type
        ''' parameter symbol.
        ''' </summary>
        <Fact>
        Public Sub TestTypeParameterPositions()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                {TestReferences.NetFx.v4_0_30319.mscorlib,
                 TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace

            Dim outerInterface = DirectCast(globalNamespace.GetTypeMembers("IGeneric2").Single(), NamedTypeSymbol)
            Assert.Equal(1, outerInterface.Arity)
            Assert.Equal(TypeKind.Interface, outerInterface.TypeKind)

            Dim outerInterfaceMethod = outerInterface.GetMembers().Single()

            Dim outerClass = DirectCast(globalNamespace.GetTypeMembers("Outer").Single(), NamedTypeSymbol)
            Assert.Equal(1, outerClass.Arity)
            Assert.Equal(TypeKind.Class, outerClass.TypeKind)

            Dim innerInterface = DirectCast(outerClass.GetTypeMembers("IInner").Single(), NamedTypeSymbol)
            Assert.Equal(1, innerInterface.Arity)
            Assert.Equal(TypeKind.Interface, innerInterface.TypeKind)

            Dim innerInterfaceMethod = innerInterface.GetMembers().Single()

            Dim innerClass1 = DirectCast(outerClass.GetTypeMembers("Inner1").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass1, "IGeneric2<A>.Method", outerInterfaceMethod)

            Dim innerClass2 = DirectCast(outerClass.GetTypeMembers("Inner2").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass2, "IGeneric2<T>.Method", outerInterfaceMethod)

            Dim innerClass3 = DirectCast(outerClass.GetTypeMembers("Inner3").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass3, "Outer<T>.IInner<C>.Method", innerInterfaceMethod)

            Dim innerClass4 = DirectCast(outerClass.GetTypeMembers("Inner4").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass4, "Outer<T>.IInner<T>.Method", innerInterfaceMethod)
        End Sub

        Private Shared Sub CheckInnerClassHelper(innerClass As NamedTypeSymbol, methodName As String, interfaceMethod As Symbol)
            Dim [interface] = interfaceMethod.ContainingType

            Assert.Equal(1, innerClass.Arity)
            Assert.Equal(TypeKind.Class, innerClass.TypeKind)
            Assert.Equal([interface], innerClass.Interfaces.Single().ConstructedFrom)

            Dim innerClassMethod = DirectCast(innerClass.GetMembers(methodName).Single(), MethodSymbol)
            Dim innerClassImplementingMethod = innerClassMethod.ExplicitInterfaceImplementations.Single()
            Assert.Equal(interfaceMethod, innerClassImplementingMethod.OriginalDefinition)
            Assert.Equal([interface], innerClassImplementingMethod.ContainingType.ConstructedFrom)
        End Sub

        <Fact()>
        Public Sub Constructors1()
            Dim ilSource =
            <![CDATA[
.class private auto ansi cls1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method public specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Instance_vs_Static
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          static void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method public specialname rtspecialname instance 
          void  .cctor() cil managed 
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi ReturnAValue1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance int32  .ctor(int32 x) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 

  .method private specialname rtspecialname static 
          int32  .cctor() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 
} 

.class private auto ansi ReturnAValue2
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          int32  .cctor() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 
} 

.class private auto ansi Generic1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor<T>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method private specialname rtspecialname static 
          void  .cctor<T>() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Generic2
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          void  .cctor<T>() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi HasParameter
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          void  .cctor(int32 x) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Virtual
       extends [mscorlib]System.Object
{
  .method public newslot strict virtual specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
} 
]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            For Each m In compilation.GetTypeByMetadataName("cls1").GetMembers()
                Assert.Equal(If(m.Name = ".cctor", MethodKind.SharedConstructor, MethodKind.Constructor), DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("Instance_vs_Static").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("ReturnAValue1").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("ReturnAValue2").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("Generic1").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("Generic2").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("HasParameter").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next

            For Each m In compilation.GetTypeByMetadataName("Virtual").GetMembers()
                Assert.Equal(MethodKind.Ordinary, DirectCast(m, MethodSymbol).MethodKind)
            Next
        End Sub

        <Fact()>
        Public Sub LoadDateTimeDefaultValue()
            Dim ilSource =
            <![CDATA[
.class public auto ansi C1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C1::.ctor

  .method public instance void  Foo([opt] valuetype [mscorlib]System.DateTime pDateTime,
                                    [opt] valuetype [mscorlib]System.Decimal pDecimal1,
                                    [opt] valuetype [mscorlib]System.Decimal pDecimal2) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 00 C0 28 6A 27 0C CB 08 00 00 )             // ....(j'.....
    .param [2]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    uint32,
                                                                                                    uint32,
                                                                                                    uint32) = ( 01 00 03 00 00 00 00 00 00 00 00 00 D2 04 00 00 
                                                                                                                00 00 ) 
    .param [3]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                    uint8,
                                                                                                    int32,
                                                                                                    int32,
                                                                                                    int32) = ( 01 00 02 00 00 00 00 00 00 00 00 00 26 09 00 00   // ............&...
                                                                                                              00 00 ) 

    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method C1::Foo

} // end of class C1

]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)
            Dim fooMethod = compilation.GetTypeByMetadataName("C1").GetMember("Foo")

            Assert.Equal(#11/4/2008#, CType(fooMethod, PEMethodSymbol).Parameters(0).ExplicitDefaultValue)
            Assert.Equal(1.234D, CType(fooMethod, PEMethodSymbol).Parameters(1).ExplicitDefaultValue)
            Assert.Equal(23.42D, CType(fooMethod, PEMethodSymbol).Parameters(2).ExplicitDefaultValue)
        End Sub

        <Fact()>
        Public Sub OverridesAndLackOfNewSlot()
            Dim ilSource =
            <![CDATA[
.class interface public abstract auto ansi serializable Microsoft.FSharp.Control.IDelegateEvent`1<([mscorlib]System.Delegate) TDelegate>
{
  .method public hidebysig abstract virtual 
          instance void  AddHandler(!TDelegate 'handler') cil managed
  {
  } // end of method IDelegateEvent`1::AddHandler

  .method public hidebysig abstract virtual 
          instance void  RemoveHandler(!TDelegate 'handler') cil managed
  {
  } // end of method IDelegateEvent`1::RemoveHandler

} // end of class Microsoft.FSharp.Control.IDelegateEvent`1
]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            For Each m In compilation.GetTypeByMetadataName("Microsoft.FSharp.Control.IDelegateEvent`1").GetMembers()
                Assert.False(DirectCast(m, MethodSymbol).IsOverridable)
                Assert.True(DirectCast(m, MethodSymbol).IsMustOverride)
                Assert.False(DirectCast(m, MethodSymbol).IsOverrides)
            Next

        End Sub

        <Fact>
        Public Sub MemberSignature_LongFormType()
            Dim source =
<compilation>
    <file>
Public Class D

    Public Shared Sub Main()
        Dim s As String = C.RT()
        Dim d As Double = C.VT()
    End Sub
End Class
    </file>
</compilation>

            Dim longFormRef = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.LongTypeFormInSignature.AsImmutableOrNull())
            Dim c = CreateCompilationWithMscorlibAndReferences(source, {longFormRef})
            c.AssertTheseDiagnostics(<![CDATA[
BC30657: 'RT' has a return type that is not supported or parameter types that are not supported.
        Dim s As String = C.RT()
                            ~~
BC30657: 'VT' has a return type that is not supported or parameter types that are not supported.
        Dim d As Double = C.VT()
                            ~~
]]>)
        End Sub
    End Class
End Namespace
