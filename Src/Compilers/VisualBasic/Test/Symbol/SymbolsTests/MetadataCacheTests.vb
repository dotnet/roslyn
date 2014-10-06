' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#Disable Warning BC40000  ' MetadataCache to be removed

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Roslyn.Test.Utilities
Imports VBReferenceManager = Microsoft.CodeAnalysis.VisualBasic.VBCompilation.ReferenceManager

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class MetadataCacheTests
        Inherits BasicTestBase

#Region "Helpers"
        Public Overrides Sub Dispose()
            ' invoke finalizers of all remaining streams and memory mapps before we attempt to delete temp files
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced)
            GC.WaitForPendingFinalizers()
            MyBase.Dispose()
        End Sub

        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Function CreateCompilation(name As String, ParamArray dependencies As Object()) As VBCompilation
            Dim result = VBCompilation.Create(name, references:=MetadataCacheTestHelpers.CreateMetadataReferences(dependencies))
            Dim asm = result.Assembly
            GC.KeepAlive(asm)
            Return result
        End Function

        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Function CreateWeakCompilation(name As String, ParamArray dependencies As Object()) As ObjectReference
            ' this compilation should only be reachable via the WeakReference we return
            Dim result = VBCompilation.Create(name, references:=MetadataCacheTestHelpers.CreateMetadataReferences(dependencies))
            Dim asm = result.Assembly
            GC.KeepAlive(asm)
            Return New ObjectReference(result)
        End Function

#End Region

        <Fact()>
        Public Sub CompilationWithEmptyInput()
            Using MetadataCache.LockAndClean()

                Dim c1 = VBCompilation.Create("Test", Nothing)

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.NotNull(c1)
                Assert.NotNull(c1.Assembly)
                Assert.Equal(0, c1.RetargetingAssemblySymbols.WeakCount)

                Assert.Equal(0, c1.References.Count)
                Assert.IsType(GetType(SourceAssemblySymbol), c1.Assembly)

                Dim a1 = DirectCast(c1.Assembly, SourceAssemblySymbol)
                Assert.Equal("Test", a1.Name)
                Assert.Equal(1, a1.Modules.Length)
                Assert.IsType(GetType(SourceModuleSymbol), a1.Modules(0))

                Dim m1 = DirectCast(a1.Modules(0), SourceModuleSymbol)
                Assert.Same(c1.SourceModule, m1)
                Assert.Same(m1.ContainingAssembly, c1.Assembly)
                Assert.Same(m1.ContainingSymbol, c1.Assembly)
                Assert.Same(m1.ContainingType, Nothing)

                Assert.Equal(0, m1.GetReferencedAssemblies().Length)
                Assert.Equal(0, m1.GetReferencedAssemblySymbols().Length)
                Assert.Same(m1.CorLibrary, a1)

            End Using
        End Sub

        <WorkItem(537422, "DevDiv")>
        <Fact()>
        Public Sub CompilationWithMscorlibReference()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path

            Dim mscorlibRef As New MetadataFileReference(mscorlibPath)

            Assert.True(mscorlibRef.Properties.Aliases.IsDefault)
            Assert.Equal(False, mscorlibRef.Properties.EmbedInteropTypes)
            Assert.Equal(mscorlibPath, mscorlibRef.FilePath, StringComparer.OrdinalIgnoreCase)
            Assert.Equal(MetadataImageKind.Assembly, mscorlibRef.Properties.Kind)

            Using MetadataCache.LockAndClean()

                Dim c1 = VBCompilation.Create("Test", references:={mscorlibRef})

                Assert.NotNull(c1.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim gns = c1.Assembly.GlobalNamespace
                Assert.NotNull(gns)
                Assert.Equal(0, gns.GetMembers().Length)
                Assert.Equal(0, gns.GetModuleMembers().Length)

                Dim cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single()

                Assert.True(String.Equals(MetadataCache.AssembliesFromFiles.Keys.Single().FullPath, mscorlibPath, StringComparison.OrdinalIgnoreCase))

                Dim assembly = cachedAssembly.Metadata.GetTarget().GetAssembly
                Assert.NotNull(assembly)

                Assert.True(assembly.Identity.Name.Equals("mscorlib"))
                Assert.Equal(0, assembly.AssemblyReferences.Length)
                Assert.Equal(1, assembly.ModuleReferenceCounts.Length)
                Assert.Equal(0, assembly.ModuleReferenceCounts(0))
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count)

                Dim mscorlibAsm = DirectCast(cachedAssembly.CachedSymbols.First(), PEAssemblySymbol)

                Assert.NotNull(mscorlibAsm)
                Assert.Same(mscorlibAsm.Assembly, cachedAssembly.Metadata.GetTarget().GetAssembly)
                Assert.True(mscorlibAsm.Identity.Name.Equals("mscorlib"))
                Assert.True(mscorlibAsm.Name.Equals("mscorlib"))
                Assert.Equal(1, mscorlibAsm.Modules.Length)
                Assert.Same(mscorlibAsm.Modules(0), mscorlibAsm.Locations.Single().MetadataModule)
                Assert.Same(mscorlibAsm.Modules(0), mscorlibAsm.Modules(0).Locations.Single().MetadataModule)

                Dim mscorlibMod = DirectCast(mscorlibAsm.Modules(0), PEModuleSymbol)

                Assert.Same(mscorlibMod.Module, mscorlibAsm.Assembly.Modules(0))
                Assert.Same(mscorlibMod.ContainingAssembly, mscorlibAsm)
                Assert.Same(mscorlibMod.ContainingSymbol, mscorlibAsm)
                Assert.Same(mscorlibMod.ContainingType, Nothing)

                Assert.Equal(0, mscorlibMod.GetReferencedAssemblies().Length)
                Assert.Equal(0, mscorlibMod.GetReferencedAssemblySymbols().Length)
                Assert.Same(mscorlibMod.CorLibrary, mscorlibAsm)
                Assert.True(mscorlibMod.Name.Equals("CommonLanguageRuntimeLibrary"))
                Assert.True(mscorlibMod.ToTestDisplayString().Equals("CommonLanguageRuntimeLibrary"))

                Assert.NotNull(c1)
                Assert.NotNull(c1.Assembly)
                Assert.Equal(0, c1.RetargetingAssemblySymbols.WeakCount)

                Assert.Equal(1, c1.References.Count)
                Assert.Same(c1.References(0), mscorlibRef)
                Assert.Same(c1.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm)

                Assert.IsType(GetType(SourceAssemblySymbol), c1.Assembly)

                Dim a1 = DirectCast(c1.Assembly, SourceAssemblySymbol)
                Assert.Equal("Test", a1.Name, StringComparer.OrdinalIgnoreCase)
                Assert.Equal(1, a1.Modules.Length)
                Assert.IsType(GetType(SourceModuleSymbol), a1.Modules(0))

                Dim m1 = DirectCast(a1.Modules(0), SourceModuleSymbol)
                Assert.Same(c1.SourceModule, m1)
                Assert.Same(m1.ContainingAssembly, c1.Assembly)
                Assert.Same(m1.ContainingSymbol, c1.Assembly)
                Assert.Same(m1.ContainingType, Nothing)

                Assert.Equal(1, m1.GetReferencedAssemblies().Length)
                Assert.Equal(1, m1.GetReferencedAssemblySymbols().Length)
                Assert.Same(m1.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m1.CorLibrary, mscorlibAsm)

                Dim c2 = VBCompilation.Create("Test2", references:={New MetadataFileReference(mscorlibPath)})

                Assert.NotNull(c2.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single()
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count)
                Assert.Same(c2.GetReferencedAssemblySymbol(c2.References(0)), mscorlibAsm)

                GC.KeepAlive(c1)
                GC.KeepAlive(c2)

            End Using
        End Sub

        <Fact>
        Public Sub NoExplicitCorLibraryReference()
            Dim noMsCorLibRefPath = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).Path
            Dim corlib2Path = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim corlib3Path = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path

            Using lock = MetadataCache.LockAndClean()

                Dim assemblies1 = MetadataTestHelpers.GetSymbolsForReferences({New MetadataFileReference(noMsCorLibRefPath)})

                Dim noMsCorLibRef_1 = assemblies1(0)

                Assert.Equal(0, noMsCorLibRef_1.Modules(0).GetReferencedAssemblies().Length)
                Assert.True(noMsCorLibRef_1.CorLibrary.IsMissing)
                Assert.Equal(TypeKind.Error,
                    noMsCorLibRef_1.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType(Of MethodSymbol)().Single().
                    Parameters(0).Type.TypeKind)

                Dim assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(
                                    {New MetadataFileReference(noMsCorLibRefPath),
                                     New MetadataFileReference(corlib2Path)})

                Dim noMsCorLibRef_2 = assemblies2(0)
                Dim msCorLib_2 = assemblies2(1)

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_2)
                Assert.Same(msCorLib_2, msCorLib_2.CorLibrary)
                Assert.Same(msCorLib_2, noMsCorLibRef_2.CorLibrary)
                Assert.Same(msCorLib_2.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_2.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType(Of MethodSymbol)().Single().
                    Parameters(0).Type)

                Dim assemblies3 = MetadataTestHelpers.GetSymbolsForReferences({New MetadataFileReference(noMsCorLibRefPath), New MetadataFileReference(corlib3Path)})

                Dim noMsCorLibRef_3 = assemblies3(0)
                Dim msCorLib_3 = assemblies3(1)

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_3)
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_3)
                Assert.NotSame(msCorLib_2, msCorLib_3)
                Assert.Same(msCorLib_3, msCorLib_3.CorLibrary)
                Assert.Same(msCorLib_3, noMsCorLibRef_3.CorLibrary)
                Assert.Same(msCorLib_3.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_3.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType(Of MethodSymbol)().Single().
                    Parameters(0).Type)

                Dim assemblies4 = MetadataTestHelpers.GetSymbolsForReferences({New MetadataFileReference(noMsCorLibRefPath)})

                For i As Integer = 0 To assemblies1.Length - 1
                    Assert.Same(assemblies1(i), assemblies4(i))
                Next

                Dim assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(
                                    {New MetadataFileReference(noMsCorLibRefPath),
                                     New MetadataFileReference(corlib2Path)})

                For i As Integer = 0 To assemblies2.Length - 1
                    Assert.Same(assemblies2(i), assemblies5(i))
                Next

                Dim assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(
                                    {New MetadataFileReference(noMsCorLibRefPath),
                                     New MetadataFileReference(corlib3Path)})

                For i As Integer = 0 To assemblies3.Length - 1
                    Assert.Same(assemblies3(i), assemblies6(i))
                Next

                GC.KeepAlive(assemblies1)
                GC.KeepAlive(assemblies2)
                GC.KeepAlive(assemblies3)

                lock.CleanCaches()

                Dim assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(
                                  {
                                     New MetadataFileReference(noMsCorLibRefPath),
                                     New MetadataFileReference(corlib2Path)
                                  })

                Dim noMsCorLibRef_7 = assemblies7(0)
                Dim msCorLib_7 = assemblies7(1)

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_7)
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_7)
                Assert.NotSame(noMsCorLibRef_3, noMsCorLibRef_7)
                Assert.NotSame(msCorLib_2, msCorLib_7)
                Assert.NotSame(msCorLib_3, msCorLib_7)
                Assert.Same(msCorLib_7, msCorLib_7.CorLibrary)
                Assert.Same(msCorLib_7, noMsCorLibRef_7.CorLibrary)
                Assert.Same(msCorLib_7.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_7.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType(Of MethodSymbol)().Single().
                    Parameters(0).Type)

                Dim assemblies8 = MetadataTestHelpers.GetSymbolsForReferences({New MetadataFileReference(noMsCorLibRefPath)})

                Dim noMsCorLibRef_8 = assemblies8(0)

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_8)
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_8)
                Assert.NotSame(noMsCorLibRef_3, noMsCorLibRef_8)
                Assert.NotSame(noMsCorLibRef_7, noMsCorLibRef_8)
                Assert.True(noMsCorLibRef_8.CorLibrary.IsMissing)
                Assert.Equal(TypeKind.Error,
                    noMsCorLibRef_8.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType(Of MethodSymbol)().Single().
                    Parameters(0).Type.TypeKind)

                GC.KeepAlive(assemblies7)
            End Using
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub ReferenceAnotherCompilation()
#Else
        <Fact()>
        Public Sub ReferenceAnotherCompilation()
#End If
            Using MetadataCache.LockAndClean()

                Dim tc1 = VBCompilation.Create("Test1", Nothing)
                Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol

                Dim c1Ref As New VisualBasicCompilationReference(tc1)

                Assert.True(c1Ref.Properties.Aliases.IsDefault)
                Assert.Equal(False, c1Ref.Properties.EmbedInteropTypes)
                Assert.Same(c1Ref.Compilation, tc1)
                Assert.Same(tc1.Assembly, tc1.Assembly.CorLibrary)

                Dim tc2 = VBCompilation.Create("Test2", references:={c1Ref})
                Assert.NotNull(tc2.Assembly) ' force creation of SourceAssemblySymbol

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.NotNull(tc2.Assembly)
                Assert.Equal(1, tc1.RetargetingAssemblySymbols.WeakCount)
                Assert.Equal(0, tc2.RetargetingAssemblySymbols.WeakCount)

                Assert.Equal(1, tc2.References.Count)
                Assert.Same(tc2.References(0), c1Ref)
                Assert.NotSame(tc2.GetReferencedAssemblySymbol(c1Ref), tc1.Assembly)
                Assert.IsType(GetType(SourceAssemblySymbol), tc2.Assembly)
                Assert.Same(DirectCast(tc2.GetReferencedAssemblySymbol(c1Ref), RetargetingAssemblySymbol).UnderlyingAssembly, tc1.Assembly)

                Dim a1 = DirectCast(tc2.Assembly, SourceAssemblySymbol)
                Assert.True(a1.Name.Equals("Test2"))
                Assert.Equal(1, a1.Modules.Length)
                Assert.IsType(GetType(SourceModuleSymbol), a1.Modules(0))

                Dim m1 = DirectCast(a1.Modules(0), SourceModuleSymbol)
                Assert.Same(tc2.SourceModule, m1)
                Assert.Same(m1.ContainingAssembly, tc2.Assembly)
                Assert.Same(m1.ContainingSymbol, tc2.Assembly)
                Assert.Same(m1.ContainingType, Nothing)

                Assert.Equal(1, m1.GetReferencedAssemblies().Length)
                Assert.True(m1.GetReferencedAssemblies()(0).Name.Equals("Test1"))
                Assert.Equal(1, m1.GetReferencedAssemblySymbols().Length)
                Assert.NotSame(m1.GetReferencedAssemblySymbols()(0), tc1.Assembly)
                Assert.Same(DirectCast(m1.GetReferencedAssemblySymbols()(0), RetargetingAssemblySymbol).UnderlyingAssembly, tc1.Assembly)
                Assert.NotSame(m1.CorLibrary, tc1.Assembly)

                Dim mscorlibRef As New MetadataFileReference(Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path)

                Dim tc3 = VBCompilation.Create("Test3", references:={mscorlibRef})

                Assert.NotNull(tc3.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim mscorlibAsm = DirectCast(tc3.Assembly.Modules(0), SourceModuleSymbol).CorLibrary

                Dim c3Ref As New VisualBasicCompilationReference(tc3)

                Dim tc4 = VBCompilation.Create("Test4", references:={mscorlibRef, c3Ref})
                Assert.NotNull(tc4.Assembly) ' force creation of SourceAssemblySymbol

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count)

                Assert.Equal(2, tc4.References.Count)
                Assert.Same(tc4.References(0), mscorlibRef)
                Assert.Same(tc4.References(1), c3Ref)
                Assert.Same(tc4.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm)
                Assert.Same(tc4.GetReferencedAssemblySymbol(c3Ref), tc3.Assembly)

                Dim a4 = DirectCast(tc4.Assembly, SourceAssemblySymbol)
                Assert.Equal(1, a4.Modules.Length)

                Dim m4 = DirectCast(a4.Modules(0), SourceModuleSymbol)

                Assert.Equal(2, m4.GetReferencedAssemblies().Length)
                Assert.True(m4.GetReferencedAssemblies()(0).Name.Equals("mscorlib"))
                Assert.True(m4.GetReferencedAssemblies()(1).Name.Equals("Test3"))
                Assert.Equal(2, m4.GetReferencedAssemblySymbols().Length)
                Assert.Same(m4.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m4.GetReferencedAssemblySymbols()(1), tc3.Assembly)
                Assert.Same(m4.CorLibrary, mscorlibAsm)


                Dim tc5 = VBCompilation.Create("Test5", references:={c3Ref, mscorlibRef})
                Assert.NotNull(tc5.Assembly) ' force creation of SourceAssemblySymbol

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count)

                Assert.Equal(2, tc5.References.Count)
                Assert.Same(tc5.References(1), mscorlibRef)
                Assert.Same(tc5.References(0), c3Ref)
                Assert.Same(tc5.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm)
                Assert.Same(tc5.GetReferencedAssemblySymbol(c3Ref), tc3.Assembly)

                Dim a5 = DirectCast(tc5.Assembly, SourceAssemblySymbol)
                Assert.Equal(1, a5.Modules.Length)

                Dim m5 = DirectCast(a5.Modules(0), SourceModuleSymbol)

                Assert.Equal(2, m5.GetReferencedAssemblies().Length)
                Assert.True(m5.GetReferencedAssemblies()(1).Name.Equals("mscorlib"))
                Assert.True(m5.GetReferencedAssemblies()(0).Name.Equals("Test3"))
                Assert.Equal(2, m5.GetReferencedAssemblySymbols().Length)
                Assert.Same(m5.GetReferencedAssemblySymbols()(1), mscorlibAsm)
                Assert.Same(m5.GetReferencedAssemblySymbols()(0), tc3.Assembly)
                Assert.Same(m5.CorLibrary, mscorlibAsm)

                GC.KeepAlive(tc1)
                GC.KeepAlive(tc2)
                GC.KeepAlive(tc3)
                GC.KeepAlive(tc4)
                GC.KeepAlive(tc5)

            End Using

        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub ReferenceModule()
#Else
        <Fact()>
        Public Sub ReferenceModule()
#End If
            Using MetadataCache.LockAndClean()

                Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
                Dim mscorlibRef As New MetadataFileReference(mscorlibPath)

                Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path
                Dim module1Ref As New MetadataFileReference(module1Path, MetadataImageKind.Module)
                Assert.True(module1Ref.Properties.Aliases.IsDefault)
                Assert.Equal(False, module1Ref.Properties.EmbedInteropTypes)
                Assert.Equal(MetadataImageKind.Module, module1Ref.Properties.Kind)

                Dim tc1 = VBCompilation.Create("Test1", references:={module1Ref, mscorlibRef})

                Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)

                Dim mscorlibAsm = DirectCast(MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Single(), PEAssemblySymbol)

                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)
                Assert.True(String.Equals(MetadataCache.ModulesFromFiles.Keys.Single().FullPath, module1Path))
                Dim cachedModule1 = MetadataCache.ModulesFromFiles.Values.Single()

                Dim module1 = cachedModule1.Metadata.GetTarget().Module
                Assert.NotNull(module1)

                Assert.Equal(1, module1.ReferencedAssemblies.Length)
                Assert.True(module1.ReferencedAssemblies(0).Name.Equals("mscorlib"))

                Assert.Equal(2, tc1.References.Count)
                Assert.Same(tc1.References(1), mscorlibRef)
                Assert.Same(tc1.References(0), module1Ref)
                Assert.Same(tc1.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm)
                Assert.Same(tc1.GetReferencedModuleSymbol(module1Ref), tc1.Assembly.Modules(1))

                Assert.Equal(2, tc1.Assembly.Modules.Length)

                Dim m11 = DirectCast(tc1.Assembly.Modules(0), SourceModuleSymbol)

                Assert.Equal(1, m11.GetReferencedAssemblies().Length)
                Assert.True(m11.GetReferencedAssemblies()(0).Name.Equals("mscorlib"))
                Assert.Equal(1, m11.GetReferencedAssemblySymbols().Length)
                Assert.Same(m11.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m11.CorLibrary, mscorlibAsm)

                Dim m12 = DirectCast(tc1.Assembly.Modules(1), PEModuleSymbol)

                Assert.Same(m12.Module, cachedModule1.Metadata.GetTarget().Module)
                Assert.Same(m12.ContainingAssembly, tc1.Assembly)
                Assert.Same(m12.ContainingSymbol, tc1.Assembly)
                Assert.Same(m12.ContainingType, Nothing)

                Assert.Equal(1, m12.GetReferencedAssemblies().Length)
                Assert.True(m12.GetReferencedAssemblies()(0).Name.Equals("mscorlib"))
                Assert.Equal(1, m12.GetReferencedAssemblySymbols().Length)
                Assert.Same(m12.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m12.CorLibrary, mscorlibAsm)
                Assert.True(m12.Name.Equals("netModule1.netmodule"))
                Assert.True(m12.ToTestDisplayString().Equals("netModule1.netmodule"))

                Assert.Same(m12, m12.Locations.Single().MetadataModule)

                Dim module2Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule2).Path
                Dim module2Ref As New MetadataFileReference(module2Path, MetadataImageKind.Module)

                Dim MTTestLib1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path
                Dim MTTestLib1Ref As New MetadataFileReference(MTTestLib1Path)

                Dim tc1Ref As New VisualBasicCompilationReference(tc1)

                Dim tc2 = VBCompilation.Create("Test2", references:={tc1Ref, module2Ref, MTTestLib1Ref, mscorlibRef, module1Ref})

                Assert.NotNull(tc2.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(2, MetadataCache.AssembliesFromFiles.Count)

                Dim mscorlibInfo = MetadataCache.AssembliesFromFiles(FileKey.Create(mscorlibPath))
                Assert.Equal(1, mscorlibInfo.CachedSymbols.Count)
                Assert.Same(mscorlibInfo.CachedSymbols.First(), mscorlibAsm)

                Dim MTTestLib1Info = MetadataCache.AssembliesFromFiles(FileKey.Create(MTTestLib1Path))
                Dim assembly = MTTestLib1Info.Metadata.GetTarget().GetAssembly
                Assert.NotNull(assembly)
                Assert.Equal(1, MTTestLib1Info.CachedSymbols.Count)

                Dim MTTestLib1Asm = DirectCast(MTTestLib1Info.CachedSymbols.First(), PEAssemblySymbol)
                Assert.True(assembly.Identity.Name.Equals("MTTestLib1"))
                Assert.Equal(3, assembly.AssemblyReferences.Length)

                Dim mscorlibRefIndex As Integer = -1
                Dim msvbRefIndex As Integer = -1
                Dim systemRefIndex As Integer = -1

                For i As Integer = 0 To 2 Step 1
                    If assembly.AssemblyReferences(i).Name.Equals("mscorlib") Then
                        Assert.Equal(-1, mscorlibRefIndex)
                        mscorlibRefIndex = i
                    ElseIf assembly.AssemblyReferences(i).Name.Equals("Microsoft.VisualBasic") Then
                        Assert.Equal(-1, msvbRefIndex)
                        msvbRefIndex = i
                    ElseIf assembly.AssemblyReferences(i).Name.Equals("System") Then
                        Assert.Equal(-1, systemRefIndex)
                        systemRefIndex = i
                    Else
                        Assert.True(False)
                    End If
                Next

                Assert.Equal(1, assembly.ModuleReferenceCounts.Length)
                Assert.Equal(3, assembly.ModuleReferenceCounts(0))

                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count)
                Assert.Same(MetadataCache.ModulesFromFiles(FileKey.Create(module1Path)).Metadata, cachedModule1.Metadata)

                Dim cachedModule2 = MetadataCache.ModulesFromFiles(FileKey.Create(module2Path))
                Dim module2 = cachedModule2.Metadata.GetTarget().Module
                Assert.NotNull(module2)

                Assert.Equal(1, module2.ReferencedAssemblies.Length)
                Assert.True(module2.ReferencedAssemblies(0).Name.Equals("mscorlib"))

                Assert.Equal(0, tc1.RetargetingAssemblySymbols.WeakCount)

                Assert.Equal(5, tc2.References.Count)
                Assert.Same(tc2.References(0), tc1Ref)
                Assert.Same(tc2.References(1), module2Ref)
                Assert.Same(tc2.References(2), MTTestLib1Ref)
                Assert.Same(tc2.References(3), mscorlibRef)
                Assert.Same(tc2.References(4), module1Ref)
                Assert.Same(tc2.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm)
                Assert.Same(tc2.GetReferencedAssemblySymbol(tc1Ref), tc1.Assembly)
                Assert.Same(tc2.GetReferencedAssemblySymbol(MTTestLib1Ref), MTTestLib1Info.CachedSymbols.First())
                Assert.Same(tc2.GetReferencedModuleSymbol(module1Ref), tc2.Assembly.Modules(2))
                Assert.Same(tc2.GetReferencedModuleSymbol(module2Ref), tc2.Assembly.Modules(1))

                Assert.Equal(3, tc2.Assembly.Modules.Length)

                Dim m21 = DirectCast(tc2.Assembly.Modules(0), SourceModuleSymbol)

                Assert.Equal(3, m21.GetReferencedAssemblies().Length)
                Assert.True(m21.GetReferencedAssemblies()(0).Name.Equals("Test1"))
                Assert.True(m21.GetReferencedAssemblies()(1).Name.Equals("MTTestLib1"))
                Assert.True(m21.GetReferencedAssemblies()(2).Name.Equals("mscorlib"))
                Assert.Equal(3, m21.GetReferencedAssemblySymbols().Length)
                Assert.Same(m21.GetReferencedAssemblySymbols()(0), tc1.Assembly)
                Assert.Same(m21.GetReferencedAssemblySymbols()(1), MTTestLib1Asm)
                Assert.Same(m21.GetReferencedAssemblySymbols()(2), mscorlibAsm)
                Assert.Same(m21.CorLibrary, mscorlibAsm)

                Dim m22 = DirectCast(tc2.Assembly.Modules(1), PEModuleSymbol)

                Assert.Same(m22.Module, cachedModule2.Metadata.GetTarget().Module)
                Assert.Same(m22.ContainingAssembly, tc2.Assembly)
                Assert.Same(m22.ContainingSymbol, tc2.Assembly)
                Assert.Same(m22.ContainingType, Nothing)

                Assert.Equal(1, m22.GetReferencedAssemblies().Length)
                Assert.True(m22.GetReferencedAssemblies()(0).Name.Equals("mscorlib"))
                Assert.Equal(1, m22.GetReferencedAssemblySymbols().Length)
                Assert.Same(m22.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m22.CorLibrary, mscorlibAsm)
                Assert.True(m22.Name.Equals("netModule2.netmodule"))
                Assert.True(m22.ToTestDisplayString().Equals("netModule2.netmodule"))

                Dim m23 = DirectCast(tc2.Assembly.Modules(2), PEModuleSymbol)

                Assert.Same(m23.Module, cachedModule1.Metadata.GetTarget().Module)
                Assert.Same(m23.ContainingAssembly, tc2.Assembly)
                Assert.Same(m23.ContainingSymbol, tc2.Assembly)
                Assert.Same(m23.ContainingType, Nothing)

                Assert.Equal(1, m23.GetReferencedAssemblies().Length)
                Assert.True(m23.GetReferencedAssemblies()(0).Name.Equals("mscorlib"))
                Assert.Equal(1, m23.GetReferencedAssemblySymbols().Length)
                Assert.Same(m23.GetReferencedAssemblySymbols()(0), mscorlibAsm)
                Assert.Same(m23.CorLibrary, mscorlibAsm)
                Assert.True(m23.Name.Equals("netModule1.netmodule"))
                Assert.True(m23.ToTestDisplayString().Equals("netModule1.netmodule"))

                Assert.Equal(1, MTTestLib1Asm.Modules.Length)
                Dim MTTestLib1Module = DirectCast(MTTestLib1Asm.Modules(0), PEModuleSymbol)

                Assert.Equal(3, MTTestLib1Module.GetReferencedAssemblies().Length)
                Assert.True(MTTestLib1Module.GetReferencedAssemblies()(mscorlibRefIndex).Name.Equals("mscorlib"))
                Assert.True(MTTestLib1Module.GetReferencedAssemblies()(msvbRefIndex).Name.Equals("Microsoft.VisualBasic"))
                Assert.True(MTTestLib1Module.GetReferencedAssemblies()(systemRefIndex).Name.Equals("System"))

                Assert.Equal(3, MTTestLib1Module.GetReferencedAssemblySymbols().Length)
                Assert.Same(MTTestLib1Module.GetReferencedAssemblySymbols()(mscorlibRefIndex), mscorlibAsm)
                Assert.True(MTTestLib1Module.GetReferencedAssemblySymbols()(msvbRefIndex).IsMissing)
                Assert.True(MTTestLib1Module.GetReferencedAssemblySymbols()(systemRefIndex).IsMissing)

                Assert.Same(MTTestLib1Module.CorLibrary, mscorlibAsm)


                Dim MTTestLib1V2Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path
                Dim MTTestLib1V2Ref As New MetadataFileReference(MTTestLib1V2Path)

                Dim tc2Ref As New VisualBasicCompilationReference(tc2)

                Dim tc3 = VBCompilation.Create("Test3", references:={MTTestLib1V2Ref, tc2Ref})
                Assert.NotNull(tc3.Assembly) ' force creation of SourceAssemblySymbol

                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count)

                Dim a3 = DirectCast(tc3.Assembly, SourceAssemblySymbol)

                Dim a3tc2 = DirectCast(tc3.GetReferencedAssemblySymbol(tc2Ref), RetargetingAssemblySymbol)
                Dim MTTestLib1V2Asm = DirectCast(tc3.GetReferencedAssemblySymbol(MTTestLib1V2Ref), PEAssemblySymbol)

                Assert.NotSame(MTTestLib1Asm, MTTestLib1V2Asm)
                Assert.Equal(1, tc2.RetargetingAssemblySymbols.WeakCount)
                Assert.Same(a3tc2, tc2.RetargetingAssemblySymbols.GetWeakReference(0).GetTarget())

                Assert.True(a3tc2.Name.Equals("Test2"))
                Assert.Equal(3, a3tc2.Modules.Length)

                Dim a3m1tc2 = DirectCast(a3tc2.Modules(0), RetargetingModuleSymbol)

                Assert.Same(a3m1tc2.ContainingAssembly, a3tc2)
                Assert.Same(a3m1tc2.ContainingSymbol, a3tc2)
                Assert.Same(a3m1tc2.ContainingType, Nothing)
                Assert.Same(a3m1tc2.UnderlyingModule, m21)

                Assert.True(a3m1tc2.GetReferencedAssemblies().Equals(m21.GetReferencedAssemblies()))
                Assert.Equal(3, a3m1tc2.GetReferencedAssemblySymbols().Length)
                Assert.True(a3m1tc2.GetReferencedAssemblySymbols()(0).IsMissing)
                Assert.Same(a3m1tc2.GetReferencedAssemblySymbols()(1), MTTestLib1V2Asm)
                Assert.True(a3m1tc2.GetReferencedAssemblySymbols()(2).IsMissing)

                Dim a3m2tc2 = DirectCast(a3tc2.Modules(1), PEModuleSymbol)

                Assert.Same(a3m2tc2.ContainingAssembly, a3tc2)
                Assert.Same(a3m2tc2.ContainingSymbol, a3tc2)
                Assert.Same(a3m2tc2.ContainingType, Nothing)
                Assert.NotEqual(a3m2tc2, m22)
                Assert.Same(a3m2tc2.Module, m22.Module)

                Assert.True(a3m2tc2.GetReferencedAssemblies().Equals(m22.GetReferencedAssemblies()))
                Assert.Equal(1, a3m2tc2.GetReferencedAssemblySymbols().Length)
                Assert.True(a3m2tc2.GetReferencedAssemblySymbols()(0).IsMissing)

                Dim a3m3tc2 = DirectCast(a3tc2.Modules(2), PEModuleSymbol)

                Assert.Same(a3m3tc2.ContainingAssembly, a3tc2)
                Assert.Same(a3m3tc2.ContainingSymbol, a3tc2)
                Assert.Same(a3m3tc2.ContainingType, Nothing)
                Assert.NotEqual(a3m3tc2, m23)
                Assert.Same(a3m3tc2.Module, m23.Module)

                Assert.True(a3m3tc2.GetReferencedAssemblies().Equals(m23.GetReferencedAssemblies()))
                Assert.Equal(1, a3m3tc2.GetReferencedAssemblySymbols().Length)
                Assert.True(a3m3tc2.GetReferencedAssemblySymbols()(0).IsMissing)

                Dim tc4 = VBCompilation.Create("Test4", references:={MTTestLib1V2Ref, tc2Ref, mscorlibRef})
                Assert.NotNull(tc4.Assembly) ' force creation of SourceAssemblySymbol

                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count)

                Dim a4 = DirectCast(tc4.Assembly, SourceAssemblySymbol)

                Dim a4tc2 = DirectCast(tc4.GetReferencedAssemblySymbol(tc2Ref), RetargetingAssemblySymbol)
                Dim _MTTestLib1V2Asm = DirectCast(tc4.GetReferencedAssemblySymbol(MTTestLib1V2Ref), PEAssemblySymbol)

                Assert.NotSame(MTTestLib1V2Asm, _MTTestLib1V2Asm)
                Assert.NotSame(MTTestLib1Asm, _MTTestLib1V2Asm)
                Assert.Equal(2, tc2.RetargetingAssemblySymbols.WeakCount)
                Assert.Same(a4tc2, tc2.RetargetingAssemblySymbols.GetWeakReference(1).GetTarget())

                Assert.True(a4tc2.Name.Equals("Test2"))
                Assert.Equal(3, a4tc2.Modules.Length)

                Dim a4m1tc2 = DirectCast(a4tc2.Modules(0), RetargetingModuleSymbol)

                Assert.Same(a4m1tc2.ContainingAssembly, a4tc2)
                Assert.Same(a4m1tc2.ContainingSymbol, a4tc2)
                Assert.Same(a4m1tc2.ContainingType, Nothing)
                Assert.Same(a4m1tc2.UnderlyingModule, m21)

                Assert.True(a4m1tc2.GetReferencedAssemblies().Equals(m21.GetReferencedAssemblies()))
                Assert.Equal(3, a4m1tc2.GetReferencedAssemblySymbols().Length)
                Assert.True(a4m1tc2.GetReferencedAssemblySymbols()(0).IsMissing)
                Assert.Same(a4m1tc2.GetReferencedAssemblySymbols()(1), _MTTestLib1V2Asm)
                Assert.Same(a4m1tc2.GetReferencedAssemblySymbols()(2), mscorlibAsm)

                Dim a4m2tc2 = DirectCast(a4tc2.Modules(1), PEModuleSymbol)

                Assert.Same(a4m2tc2.ContainingAssembly, a4tc2)
                Assert.Same(a4m2tc2.ContainingSymbol, a4tc2)
                Assert.Same(a4m2tc2.ContainingType, Nothing)
                Assert.NotEqual(a4m2tc2, m22)
                Assert.Same(a4m2tc2.Module, m22.Module)

                Assert.True(a4m2tc2.GetReferencedAssemblies().Equals(m22.GetReferencedAssemblies()))
                Assert.Equal(1, a4m2tc2.GetReferencedAssemblySymbols().Length)
                Assert.Same(a4m2tc2.GetReferencedAssemblySymbols()(0), mscorlibAsm)

                Dim a4m3tc2 = DirectCast(a4tc2.Modules(2), PEModuleSymbol)

                Assert.Same(a4m3tc2.ContainingAssembly, a4tc2)
                Assert.Same(a4m3tc2.ContainingSymbol, a4tc2)
                Assert.Same(a4m3tc2.ContainingType, Nothing)
                Assert.NotEqual(a4m3tc2, m23)
                Assert.Same(a4m3tc2.Module, m23.Module)

                Assert.True(a4m3tc2.GetReferencedAssemblies().Equals(m23.GetReferencedAssemblies()))
                Assert.Equal(1, a4m3tc2.GetReferencedAssemblySymbols().Length)
                Assert.Same(a4m3tc2.GetReferencedAssemblySymbols()(0), mscorlibAsm)

                GC.KeepAlive(tc1)
                GC.KeepAlive(tc2)
                GC.KeepAlive(tc3)
                GC.KeepAlive(tc4)

            End Using

        End Sub

        <Fact()>
        Public Sub ReferenceMultiModuleAssembly()

            Using MetadataCache.LockAndClean()

                Dim dir = Temp.CreateDirectory()
                Dim MscorlibRef = New MetadataFileReference(dir.CreateFile("mscorlib.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path)
                Dim SystemCoreRef = New MetadataFileReference(dir.CreateFile("System.Core.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).Path)

                Dim mm = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModule).Path
                dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2)
                dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3)
                Dim multimoduleRef = New MetadataFileReference(mm)

                Dim tc1 = VBCompilation.Create("Test1", references:={MscorlibRef, SystemCoreRef, multimoduleRef})

                Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim cachedAssembly = MetadataCache.AssembliesFromFiles(FileKey.Create(multimoduleRef.FilePath))
                Dim assembly = cachedAssembly.Metadata.GetTarget().GetAssembly
                Assert.NotNull(assembly)

                Assert.True(assembly.Identity.Name.Equals("MultiModule"))
                Assert.Equal(5, assembly.AssemblyReferences.Length)
                Assert.True(assembly.AssemblyReferences(0).Name.Equals("mscorlib"))
                Assert.True(assembly.AssemblyReferences(1).Name.Equals("System.Core"))
                Assert.True(assembly.AssemblyReferences(2).Name.Equals("mscorlib"))
                Assert.True(assembly.AssemblyReferences(3).Name.Equals("mscorlib"))
                Assert.True(assembly.AssemblyReferences(4).Name.Equals("System.Core"))
                Assert.Equal(3, assembly.ModuleReferenceCounts.Length)
                Assert.Equal(2, assembly.ModuleReferenceCounts(0))
                Assert.Equal(1, assembly.ModuleReferenceCounts(1))
                Assert.Equal(2, assembly.ModuleReferenceCounts(2))

                Dim multiModuleAsm = DirectCast(tc1.GetReferencedAssemblySymbol(multimoduleRef), PEAssemblySymbol)
                Dim mscorlibAsm = DirectCast(tc1.GetReferencedAssemblySymbol(MscorlibRef), PEAssemblySymbol)
                Dim systemCoreAsm = DirectCast(tc1.GetReferencedAssemblySymbol(SystemCoreRef), PEAssemblySymbol)

                Assert.Equal(3, multiModuleAsm.Modules.Length)
                Assert.Same(multiModuleAsm.Modules(0), multiModuleAsm.Locations.Single().MetadataModule)

                For Each m In multiModuleAsm.Modules
                    Assert.Same(m, m.Locations.Single().MetadataModule)
                Next

                Dim m1 = DirectCast(multiModuleAsm.Modules(0), PEModuleSymbol)

                Assert.Equal(2, m1.GetReferencedAssemblies.Length)
                Assert.True(m1.GetReferencedAssemblies(0).Name.Equals("mscorlib"))
                Assert.True(m1.GetReferencedAssemblies(1).Name.Equals("System.Core"))
                Assert.Equal(2, m1.GetReferencedAssemblySymbols.Length)
                Assert.Same(m1.GetReferencedAssemblySymbols(0), mscorlibAsm)
                Assert.Same(m1.GetReferencedAssemblySymbols(1), systemCoreAsm)

                Dim m2 = DirectCast(multiModuleAsm.Modules(1), PEModuleSymbol)

                Assert.Equal(1, m2.GetReferencedAssemblies.Length)
                Assert.True(m2.GetReferencedAssemblies(0).Name.Equals("mscorlib"))
                Assert.Equal(1, m2.GetReferencedAssemblySymbols.Length)
                Assert.Same(m2.GetReferencedAssemblySymbols(0), mscorlibAsm)

                Dim m3 = DirectCast(multiModuleAsm.Modules(2), PEModuleSymbol)

                Assert.Equal(2, m3.GetReferencedAssemblies.Length)
                Assert.True(m3.GetReferencedAssemblies(0).Name.Equals("mscorlib"))
                Assert.True(m3.GetReferencedAssemblies(1).Name.Equals("System.Core"))
                Assert.Equal(2, m3.GetReferencedAssemblySymbols.Length)
                Assert.Same(m3.GetReferencedAssemblySymbols(0), mscorlibAsm)
                Assert.Same(m3.GetReferencedAssemblySymbols(1), systemCoreAsm)

            End Using

        End Sub

        <Fact()>
        Public Sub LazySourceAssembly_Assembly()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path

            Dim mscorlibRef = New MetadataFileReference(mscorlibPath)
            Dim module1Ref = New MetadataFileReference(module1Path, MetadataImageKind.Module)

            Using lock = MetadataCache.LockAndClean()

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                ' Test Assembly property
                Dim c1 = VBCompilation.Create("Test", references:={mscorlibRef, module1Ref})

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Dim a = c1.Assembly

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                GC.KeepAlive(c1)
            End Using
        End Sub

        <Fact()>
        Public Sub LazySourceAssembly_GetReferencedAssemblySymbol()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path

            Dim mscorlibRef = New MetadataFileReference(mscorlibPath)
            Dim module1Ref = New MetadataFileReference(module1Path, MetadataImageKind.Module)

            Using lock = MetadataCache.LockAndClean()
                Dim c1 = VBCompilation.Create("Test", references:={mscorlibRef, module1Ref})

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Dim a = c1.GetReferencedAssemblySymbol(mscorlibRef)

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                GC.KeepAlive(c1)
                lock.CleanCaches()
            End Using
        End Sub

        <Fact()>
        Public Sub LazySourceAssembly_GetReferencedModuleSymbol()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path

            Dim mscorlibRef = New MetadataFileReference(mscorlibPath)
            Dim module1Ref = New MetadataFileReference(module1Path, MetadataImageKind.Module)

            Using lock = MetadataCache.LockAndClean()
                Dim c1 = VBCompilation.Create("Test", references:={mscorlibRef, module1Ref})

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Dim a = c1.GetReferencedModuleSymbol(module1Ref).ContainingAssembly

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                GC.KeepAlive(c1)
            End Using
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub LazySourceAssembly_CompilationReference1()
#Else
        <Fact()>
        Public Sub LazySourceAssembly_CompilationReference1()
#End If
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path

            Dim mscorlibRef = New MetadataFileReference(mscorlibPath)
            Dim module1Ref = New MetadataFileReference(module1Path, MetadataImageKind.Module)

            Using lock = MetadataCache.LockAndClean()
                Dim c1 = VBCompilation.Create("Test1", references:={mscorlibRef, module1Ref})
                Dim c2 = VBCompilation.Create("Test2", references:={mscorlibRef, module1Ref, New VisualBasicCompilationReference(c1)})

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c2))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c2))

                Dim a = c1.Assembly

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c2))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c2))

                a = c2.Assembly

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c2))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c2))

                GC.KeepAlive(c1)
                GC.KeepAlive(c2)
            End Using
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub LazySourceAssembly_CompilationReference2()
#Else
        <Fact()>
        Public Sub LazySourceAssembly_CompilationReference2()
#End If
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path

            Dim mscorlibRef = New MetadataFileReference(mscorlibPath)
            Dim module1Ref = New MetadataFileReference(module1Path, MetadataImageKind.Module)

            Using lock = MetadataCache.LockAndClean()
                Dim c1 = VBCompilation.Create("Test1", references:={mscorlibRef, module1Ref})
                Dim c2 = VBCompilation.Create("Test2", references:={mscorlibRef, module1Ref, c1.ToMetadataReference()})

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Assert.False(VBReferenceManager.IsSourceAssemblySymbolCreated(c2))
                Assert.False(VBReferenceManager.IsReferenceManagerInitialized(c2))

                Dim a = c2.Assembly

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count)
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count)

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c1))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c1))

                Assert.True(VBReferenceManager.IsSourceAssemblySymbolCreated(c2))
                Assert.True(VBReferenceManager.IsReferenceManagerInitialized(c2))

                GC.KeepAlive(c1)
                GC.KeepAlive(c2)
            End Using
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub LazySourceAssembly1()
#Else
        <Fact()>
        Public Sub LazySourceAssembly1()

#End If
            Dim mscorlibRef = TestReferences.NetFx.v4_0_21006.mscorlib
            Dim MTTestLib1V1Ref = TestReferences.SymbolsTests.V1.MTTestLib1.dll
            Dim MTTestLib1V2Ref = TestReferences.SymbolsTests.V2.MTTestLib1.dll

            Dim c1 As VBCompilation
            Dim c2 As VBCompilation

            c1 = VBCompilation.Create("Test1", references:={mscorlibRef, MTTestLib1V1Ref})

            Dim c1Ref = c1.ToMetadataReference()

            c2 = VBCompilation.Create("Test2", references:={mscorlibRef, MTTestLib1V2Ref, c1Ref})


            Dim asm2 = c2.GetReferencedAssemblySymbol(mscorlibRef)
            Dim asm1 = c1.GetReferencedAssemblySymbol(mscorlibRef)

            Assert.Same(asm2, asm1)

            GC.KeepAlive(c1)
            GC.KeepAlive(c2)
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>       
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache1()
#Else
        <Fact()>
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache1()
#End If
            Using MetadataCache.LockAndClean()
                Dim mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
                Dim mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
                Dim V1MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path
                Dim V2MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path

                ' Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
                Dim c1 = VBCompilation.Create("Test1", references:=MetadataCacheTestHelpers.CreateMetadataReferences(
                    mscorlib2,
                    V1MTTestLib1))

                Dim c2 = CreateWeakCompilation("Test2",
                    mscorlib3,
                    c1,
                    V1MTTestLib1)

                Dim c3 = CreateCompilation("Test3",
                    mscorlib3,
                    c1,
                    V2MTTestLib1)

                Dim ras1 = c1.RetargetingAssemblySymbols

                Assert.Equal(2, ras1.WeakCount)
                Dim weakRetargetingAsm1 As WeakReference(Of IAssemblySymbol) = ras1.GetWeakReference(0)
                Dim weakRetargetingAsm2 As WeakReference(Of IAssemblySymbol) = ras1.GetWeakReference(1)

                c2.Strong = Nothing

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced)
                Assert.True(weakRetargetingAsm1.IsNull())

                Dim symbols As List(Of AssemblySymbol) = New List(Of AssemblySymbol)()
                SyncLock CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard
                    c1.AddRetargetingAssemblySymbolsNoLock(symbols)
                End SyncLock

                Assert.Equal(1, symbols.Count)
                Assert.Equal(2, ras1.WeakCount) ' weak list hasn't been compacted

                Dim asm2 = weakRetargetingAsm2.GetTarget()
                Assert.NotNull(asm2)
                Assert.Null(ras1.GetWeakReference(0).GetTarget())
                Assert.Same(ras1.GetWeakReference(1).GetTarget(), asm2)

                GC.KeepAlive(c3)
                GC.KeepAlive(c1)
            End Using
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache2()
#Else
        <Fact>
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache2()
#End If
            Using MetadataCache.LockAndClean()

                Dim mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
                Dim mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
                Dim libV1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path
                Dim libV2 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path

                ' Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
                Dim c1 = VBCompilation.Create("Test1", references:=MetadataCacheTestHelpers.CreateMetadataReferences(
                    mscorlib2,
                    libV1))

                Dim c2 = CreateWeakCompilation("Test2",
                    mscorlib3,
                    c1,
                    libV1)

                Dim c3 = CreateWeakCompilation("Test3",
                    mscorlib3,
                    c1,
                    libV2)

                Dim ras1 = c1.RetargetingAssemblySymbols

                Assert.Equal(2, ras1.WeakCount)
                Dim weakRetargetingAsm1 As WeakReference(Of IAssemblySymbol) = ras1.GetWeakReference(0)
                Dim weakRetargetingAsm2 As WeakReference(Of IAssemblySymbol) = ras1.GetWeakReference(1)

                ' remove strong references, the only references left are weak
                c2.Strong = Nothing
                c3.Strong = Nothing

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, True)
                Assert.True(weakRetargetingAsm1.IsNull())
                Assert.True(weakRetargetingAsm2.IsNull())

                Dim symbols As List(Of AssemblySymbol) = New List(Of AssemblySymbol)()
                SyncLock CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard
                    c1.AddRetargetingAssemblySymbolsNoLock(symbols)
                End SyncLock
                Assert.Equal(0, symbols.Count)
                Assert.Equal(0, ras1.WeakCount)

                GC.KeepAlive(c1)
            End Using
        End Sub
    End Class
End Namespace

