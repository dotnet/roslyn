' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class BaseTypeResolution
        Inherits BasicTestBase

        <Fact()>
        Public Sub Test1()

            Dim assembly = MetadataTestHelpers.LoadFromBytes(TestMetadata.ResourcesNet40.mscorlib)

            TestBaseTypeResolutionHelper1(assembly)

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {TestResources.General.MDTestLib1,
                                     TestResources.General.MDTestLib2,
                                     TestMetadata.ResourcesNet40.mscorlib})

            TestBaseTypeResolutionHelper2(assemblies)

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {TestResources.General.MDTestLib1,
                                     TestResources.General.MDTestLib2})

            TestBaseTypeResolutionHelper3(assemblies)

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(
            {
                TestReferences.SymbolsTests.MultiModule.Assembly,
                TestReferences.SymbolsTests.MultiModule.Consumer
            })

            TestBaseTypeResolutionHelper4(assemblies)

        End Sub

        Private Sub TestBaseTypeResolutionHelper1(assembly As AssemblySymbol)
            Dim module0 = assembly.Modules(0)

            Dim sys = module0.GlobalNamespace.GetMembers("SYSTEM")
            Dim collections = DirectCast(sys(0), NamespaceSymbol).GetMembers("CollectionS")
            Dim generic = DirectCast(collections(0), NamespaceSymbol).GetMembers("Generic")
            Dim dictionary = DirectCast(generic(0), NamespaceSymbol).GetMembers("Dictionary")
            Dim base = DirectCast(dictionary(0), NamedTypeSymbol).BaseType

            AssertBaseType(base, "System.Object")
            Assert.Null(base.BaseType)

            Dim concurrent = DirectCast(collections(0), NamespaceSymbol).GetMembers("Concurrent")

            Dim orderablePartitioners = DirectCast(concurrent(0), NamespaceSymbol).GetMembers("OrderablePartitioner")
            Dim orderablePartitioner As NamedTypeSymbol = Nothing

            For Each p In orderablePartitioners
                Dim t = TryCast(p, NamedTypeSymbol)
                If t IsNot Nothing AndAlso t.Arity = 1 Then
                    orderablePartitioner = t
                    Exit For
                End If
            Next

            base = orderablePartitioner.BaseType

            AssertBaseType(base, "System.Collections.Concurrent.Partitioner(Of TSource)")
            Assert.Same(DirectCast(base, NamedTypeSymbol).TypeArguments(0), orderablePartitioner.TypeParameters(0))

            Dim partitioners = DirectCast(concurrent(0), NamespaceSymbol).GetMembers("Partitioner")
            Dim partitioner As NamedTypeSymbol = Nothing

            For Each p In partitioners
                Dim t = TryCast(p, NamedTypeSymbol)
                If t IsNot Nothing AndAlso t.Arity = 0 Then
                    partitioner = t
                    Exit For
                End If
            Next

            Assert.NotNull(partitioner)
        End Sub

        Private Sub TestBaseTypeResolutionHelper2(assemblies() As AssemblySymbol)

            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(1).Modules(0)

            Dim TC2 = module1.GlobalNamespace.GetTypeMembers("TC2").Single()
            Dim TC3 = module1.GlobalNamespace.GetTypeMembers("TC3").Single()
            Dim TC4 = module1.GlobalNamespace.GetTypeMembers("TC4").Single()

            AssertBaseType(TC2.BaseType, "C1(Of TC2_T1).C2(Of TC2_T2)")
            AssertBaseType(TC3.BaseType, "C1(Of TC3_T1).C3")
            AssertBaseType(TC4.BaseType, "C1(Of TC4_T1).C3.C4(Of TC4_T2)")

            Dim C1 = module1.GlobalNamespace.GetTypeMembers("C1").Single()
            AssertBaseType(C1.BaseType, "System.Object")
            Assert.Equal(0, C1.Interfaces.Length())

            Dim TC5 = module2.GlobalNamespace.GetTypeMembers("TC5").Single()
            Dim TC6 = module2.GlobalNamespace.GetTypeMembers("TC6").Single()
            Dim TC7 = module2.GlobalNamespace.GetTypeMembers("TC7").Single()
            Dim TC8 = module2.GlobalNamespace.GetTypeMembers("TC8").Single()
            Dim TC9 = TC6.GetTypeMembers("TC9").Single()

            AssertBaseType(TC5.BaseType, "C1(Of TC5_T1).C2(Of TC5_T2)")
            AssertBaseType(TC6.BaseType, "C1(Of TC6_T1).C3")
            AssertBaseType(TC7.BaseType, "C1(Of TC7_T1).C3.C4(Of TC7_T2)")
            AssertBaseType(TC8.BaseType, "C1(Of System.Type)")
            AssertBaseType(TC9.BaseType, "TC6(Of TC6_T1)")

            Dim CorTypes = module2.GlobalNamespace.GetMembers("CorTypes").OfType(Of NamespaceSymbol)().Single()

            Dim CorTypes_Derived = CorTypes.GetTypeMembers("Derived").Single()
            AssertBaseType(CorTypes_Derived.BaseType,
                           "CorTypes.NS.Base(Of System.Boolean, System.SByte, System.Byte, System.Int16, System.UInt16, System.Int32, System.UInt32, System.Int64, System.UInt64, System.Single, System.Double, System.Char, System.String, System.IntPtr, System.UIntPtr, System.Object)")

            Dim CorTypes_Derived1 = CorTypes.GetTypeMembers("Derived1").Single()
            AssertBaseType(CorTypes_Derived1.BaseType,
                           "CorTypes.Base(Of System.Int32(), System.Double(,))")

            Dim I101 = module1.GlobalNamespace.GetTypeMembers("I101").Single()
            Dim I102 = module1.GlobalNamespace.GetTypeMembers("I102").Single()

            Dim C203 = module1.GlobalNamespace.GetTypeMembers("C203").Single()
            Assert.Equal(1, C203.Interfaces.Length())
            Assert.Same(I101, C203.Interfaces(0))

            Dim C204 = module1.GlobalNamespace.GetTypeMembers("C204").Single()
            Assert.Equal(2, C204.Interfaces.Length())
            Assert.Same(I101, C204.Interfaces(0))
            Assert.Same(I102, C204.Interfaces(1))

            Return

        End Sub

        Private Sub TestBaseTypeResolutionHelper3(assemblies() As AssemblySymbol)

            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(1).Modules(0)

            Dim CorTypes = module2.GlobalNamespace.GetMembers("CorTypes").OfType(Of NamespaceSymbol)().Single()

            Dim CorTypes_Derived = CorTypes.GetTypeMembers("Derived").Single()
            AssertBaseType(CorTypes_Derived.BaseType,
                           "CorTypes.NS.Base(Of System.Boolean[missing], System.SByte[missing], System.Byte[missing], System.Int16[missing], System.UInt16[missing], System.Int32[missing], System.UInt32[missing], System.Int64[missing], System.UInt64[missing], System.Single[missing], System.Double[missing], System.Char[missing], System.String[missing], System.IntPtr[missing], System.UIntPtr[missing], System.Object[missing])")

            For Each arg In CorTypes_Derived.BaseType.TypeArguments()
                Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(arg)
            Next

            Return

        End Sub

        Private Sub TestBaseTypeResolutionHelper4(assemblies() As AssemblySymbol)

            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(0).Modules(1)
            Dim module3 = assemblies(0).Modules(2)
            Dim module0 = assemblies(1).Modules(0)

            Dim Derived1 = module0.GlobalNamespace.GetTypeMembers("Derived1").Single()
            Dim base1 = Derived1.BaseType

            Dim Derived2 = module0.GlobalNamespace.GetTypeMembers("Derived2").Single()
            Dim base2 = Derived2.BaseType

            Dim Derived3 = module0.GlobalNamespace.GetTypeMembers("Derived3").Single()
            Dim base3 = Derived3.BaseType

            AssertBaseType(base1, "Class1")
            AssertBaseType(base2, "Class2")
            AssertBaseType(base3, "Class3")

            Assert.Same(base1, module1.GlobalNamespace.GetTypeMembers("Class1").Single())
            Assert.Same(base2, module2.GlobalNamespace.GetTypeMembers("Class2").Single())
            Assert.Same(base3, module3.GlobalNamespace.GetTypeMembers("Class3").Single())
        End Sub

        Friend Shared Sub AssertBaseType(base As TypeSymbol, name As String)
            Assert.NotEqual(SymbolKind.ErrorType, base.Kind)
            Assert.Equal(name, base.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Test2()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {TestResources.SymbolsTests.DifferByCase.Consumer,
                                     TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase})

            Dim module0 = TryCast(assemblies(0).Modules(0), PEModuleSymbol)
            Dim module1 = TryCast(assemblies(1).Modules(0), PEModuleSymbol)

            Dim bases As New HashSet(Of NamedTypeSymbol)()

            Dim TC1 = module0.GlobalNamespace.GetTypeMembers("TC1").Single()
            Dim base1 = TC1.BaseType
            bases.Add(base1)
            Assert.NotEqual(SymbolKind.ErrorType, base1.Kind)
            Assert.Equal("SomeName.Dummy", base1.ToTestDisplayString())

            Dim TC2 = module0.GlobalNamespace.GetTypeMembers("TC2").Single()
            Dim base2 = TC2.BaseType
            bases.Add(base2)
            Assert.NotEqual(SymbolKind.ErrorType, base2.Kind)
            Assert.Equal("somEnamE", base2.ToTestDisplayString())

            Dim TC3 = module0.GlobalNamespace.GetTypeMembers("TC3").Single()
            Dim base3 = TC3.BaseType
            bases.Add(base3)
            Assert.NotEqual(SymbolKind.ErrorType, base3.Kind)
            Assert.Equal("somEnamE1", base3.ToTestDisplayString())

            Dim TC4 = module0.GlobalNamespace.GetTypeMembers("TC4").Single()
            Dim base4 = TC4.BaseType
            bases.Add(base4)
            Assert.NotEqual(SymbolKind.ErrorType, base4.Kind)
            Assert.Equal("SomeName1", base4.ToTestDisplayString())

            Dim TC5 = module0.GlobalNamespace.GetTypeMembers("TC5").Single()
            Dim base5 = TC5.BaseType
            bases.Add(base5)
            Assert.NotEqual(SymbolKind.ErrorType, base5.Kind)
            Assert.Equal("somEnamE2.OtherName", base5.ToTestDisplayString())

            Dim TC6 = module0.GlobalNamespace.GetTypeMembers("TC6").Single()
            Dim base6 = TC6.BaseType
            bases.Add(base6)
            Assert.NotEqual(SymbolKind.ErrorType, base6.Kind)
            Assert.Equal("SomeName2.OtherName", base6.ToTestDisplayString())
            Assert.Equal("SomeName2.OtherName", base6.ToTestDisplayString())

            Dim TC7 = module0.GlobalNamespace.GetTypeMembers("TC7").Single()
            Dim base7 = TC7.BaseType
            bases.Add(base7)
            Assert.NotEqual(SymbolKind.ErrorType, base7.Kind)
            Assert.Equal("NestingClass.somEnamE3", base7.ToTestDisplayString())

            Dim TC8 = module0.GlobalNamespace.GetTypeMembers("TC8").Single()
            Dim base8 = TC8.BaseType
            bases.Add(base8)
            Assert.NotEqual(SymbolKind.ErrorType, base8.Kind)
            Assert.Equal("NestingClass.SomeName3", base8.ToTestDisplayString())

            Assert.Equal(8, bases.Count)

            Assert.Equal(base1, module1.TypeHandleToTypeMap(DirectCast(base1, PENamedTypeSymbol).Handle))
            Assert.Equal(base2, module1.TypeHandleToTypeMap(DirectCast(base2, PENamedTypeSymbol).Handle))
            Assert.Equal(base3, module1.TypeHandleToTypeMap(DirectCast(base3, PENamedTypeSymbol).Handle))
            Assert.Equal(base4, module1.TypeHandleToTypeMap(DirectCast(base4, PENamedTypeSymbol).Handle))
            Assert.Equal(base5, module1.TypeHandleToTypeMap(DirectCast(base5, PENamedTypeSymbol).Handle))
            Assert.Equal(base6, module1.TypeHandleToTypeMap(DirectCast(base6, PENamedTypeSymbol).Handle))
            Assert.Equal(base7, module1.TypeHandleToTypeMap(DirectCast(base7, PENamedTypeSymbol).Handle))
            Assert.Equal(base8, module1.TypeHandleToTypeMap(DirectCast(base8, PENamedTypeSymbol).Handle))

            Assert.Equal(base1, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC1, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base2, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC2, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base3, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC3, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base4, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC4, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base5, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC5, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base6, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC6, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base7, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC7, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.Equal(base8, module0.TypeRefHandleToTypeMap(CType(module0.Module.GetBaseTypeOfTypeOrThrow(DirectCast(TC8, PENamedTypeSymbol).Handle), TypeReferenceHandle)))

            Dim assembly1 = DirectCast(assemblies(1), MetadataOrSourceAssemblySymbol)

            Assert.Equal(base1, assembly1.CachedTypeByEmittedName(base1.ToTestDisplayString()))
            Assert.Equal(base2, assembly1.CachedTypeByEmittedName(base2.ToTestDisplayString()))
            Assert.Equal(base3, assembly1.CachedTypeByEmittedName(base3.ToTestDisplayString()))
            Assert.Equal(base4, assembly1.CachedTypeByEmittedName(base4.ToTestDisplayString()))
            Assert.Equal(base5, assembly1.CachedTypeByEmittedName(base5.ToTestDisplayString()))
            Assert.Equal(base6, assembly1.CachedTypeByEmittedName(base6.ToTestDisplayString()))

            Assert.Equal(base7.ContainingType, assembly1.CachedTypeByEmittedName(base7.ContainingType.ToTestDisplayString()))

            Assert.Equal(7, assembly1.EmittedNameToTypeMapCount)
        End Sub

        <Fact()>
        Public Sub Test3()

            Dim mscorlibRef = TestMetadata.Net40.mscorlib

            Dim c1 = VisualBasicCompilation.Create("Test", references:={mscorlibRef})

            Assert.Equal("System.Object", DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol).GetCorLibType(SpecialType.System_Object).ToTestDisplayString())

            Dim MTTestLib1Ref = TestReferences.SymbolsTests.V1.MTTestLib1.dll

            Dim c2 = VisualBasicCompilation.Create("Test2", references:={MTTestLib1Ref})
            Assert.Equal("System.Object[missing]", DirectCast(c2.Assembly.Modules(0), SourceModuleSymbol).GetCorLibType(SpecialType.System_Object).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub CrossModuleReferences1()
            Dim compilationDef1 =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test1
    Inherits M3
End Class

Class Test2
    Inherits M4
End Class
    ]]></file>
</compilation>

            Dim crossRefModule1 = TestReferences.SymbolsTests.netModule.CrossRefModule1
            Dim crossRefModule2 = TestReferences.SymbolsTests.netModule.CrossRefModule2
            Dim crossRefLib = TestReferences.SymbolsTests.netModule.CrossRefLib

            Dim compilation1 = CreateCompilationWithMscorlib40AndReferences(compilationDef1, {crossRefLib}, TestOptions.ReleaseDll)

            AssertNoErrors(compilation1)

            Dim test1 = compilation1.GetTypeByMetadataName("Test1")
            Dim test2 = compilation1.GetTypeByMetadataName("Test2")

            Assert.False(test1.BaseType.IsErrorType())
            Assert.False(test1.BaseType.BaseType.IsErrorType())
            Assert.False(test2.BaseType.IsErrorType())
            Assert.False(test2.BaseType.BaseType.IsErrorType())
            Assert.False(test2.BaseType.BaseType.BaseType.IsErrorType())

            Dim compilationDef2 =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Public Class M3
	Inherits M1
End Class

Public Class M4
	Inherits M2
End Class
    ]]></file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(compilationDef2, {crossRefModule1, crossRefModule2}, TestOptions.ReleaseDll)

            AssertNoErrors(compilation2)

            Dim m3 = compilation2.GetTypeByMetadataName("M3")
            Dim m4 = compilation2.GetTypeByMetadataName("M4")

            Assert.False(m3.BaseType.IsErrorType())
            Assert.False(m3.BaseType.BaseType.IsErrorType())
            Assert.False(m4.BaseType.IsErrorType())
            Assert.False(m4.BaseType.BaseType.IsErrorType())

            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(compilationDef2, {crossRefModule2}, TestOptions.ReleaseDll)

            m3 = compilation3.GetTypeByMetadataName("M3")
            m4 = compilation3.GetTypeByMetadataName("M4")

            Assert.True(m3.BaseType.IsErrorType())
            Assert.False(m4.BaseType.IsErrorType())
            Assert.True(m4.BaseType.BaseType.IsErrorType())

            AssertTheseDiagnostics(compilation3,
<expected>
BC37221: Reference to 'CrossRefModule1.netmodule' netmodule missing.
BC30002: Type 'M1' is not defined.
	Inherits M1
          ~~
BC30653: Reference required to module 'CrossRefModule1.netmodule' containing the type 'M1'. Add one to your project.
	Inherits M2
          ~~
</expected>)
        End Sub

    End Class

End Namespace
