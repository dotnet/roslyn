' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
Imports VBReferenceManager = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ReferenceManager

Namespace CompilationCreationTestHelpers
    Module Helpers
        <Extension()>
        Friend Function BoundReferences(this As AssemblySymbol) As AssemblySymbol()
            Return (From m In this.Modules, ref In m.GetReferencedAssemblySymbols() Select ref).ToArray()
        End Function

        <Extension()>
        Friend Function Length(Of T)(this As ImmutableArray(Of T)) As Integer
            Return this.Length
        End Function

        <Extension()>
        Friend Function SourceAssembly(this As VisualBasicCompilation) As SourceAssemblySymbol
            Return DirectCast(this.Assembly, SourceAssemblySymbol)
        End Function

        <Extension()>
        Private Function HasUnresolvedReferencesByComparisonTo(this As AssemblySymbol, that As AssemblySymbol) As Boolean
            Dim thisRefs = this.BoundReferences()
            Dim thatRefs = that.BoundReferences()

            For i As Integer = 0 To Math.Max(thisRefs.Length, thatRefs.Length) - 1
                If thisRefs(i).IsMissing AndAlso Not thatRefs(i).IsMissing Then
                    Return True
                End If
            Next

            Return False
        End Function

        <Extension()>
        Friend Function RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(this As AssemblySymbol, that As AssemblySymbol) As Boolean
            Dim thisPEAssembly = TryCast(this, PEAssemblySymbol)

            If thisPEAssembly IsNot Nothing Then
                Dim thatPEAssembly = TryCast(that, PEAssemblySymbol)

                Return thatPEAssembly IsNot Nothing AndAlso
                    thisPEAssembly.Assembly Is thatPEAssembly.Assembly AndAlso this.HasUnresolvedReferencesByComparisonTo(that)
            End If

            Dim thisRetargetingAssembly = TryCast(this, RetargetingAssemblySymbol)

            If thisRetargetingAssembly IsNot Nothing Then
                Dim thatRetargetingAssembly = TryCast(that, RetargetingAssemblySymbol)

                If thatRetargetingAssembly IsNot Nothing Then
                    Return thisRetargetingAssembly.UnderlyingAssembly Is thatRetargetingAssembly.UnderlyingAssembly AndAlso
                        this.HasUnresolvedReferencesByComparisonTo(that)
                End If

                Dim thatSourceAssembly = TryCast(that, SourceAssemblySymbol)

                Return thatSourceAssembly IsNot Nothing AndAlso thisRetargetingAssembly.UnderlyingAssembly Is thatSourceAssembly AndAlso
                    this.HasUnresolvedReferencesByComparisonTo(that)
            End If

            Return False
        End Function

    End Module
End Namespace

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CompilationCreationTests : Inherits BasicTestBase

        <Fact()>
        Public Sub CompilationWithEmptyInput()
            Using MetadataCache.LockAndClean()

                Dim c1 = VisualBasicCompilation.Create("Test", Nothing)

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

        <WorkItem(537422)>
        <Fact()>
        Public Sub CompilationWithMscorlibReference()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path

            Dim mscorlibRef As New MetadataFileReference(mscorlibPath)

            Assert.Same(mscorlibRef.Properties.Alias, Nothing)
            Assert.Equal(False, mscorlibRef.Properties.EmbedInteropTypes)
            Assert.Equal(mscorlibPath, mscorlibRef.FullPath, StringComparer.OrdinalIgnoreCase)
            Assert.Equal(MetadataImageKind.Assembly, mscorlibRef.Properties.Kind)

            Using MetadataCache.LockAndClean()

                Dim c1 = VisualBasicCompilation.Create("Test", references:={mscorlibRef})

                Assert.NotNull(c1.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim gns = c1.Assembly.GlobalNamespace
                Assert.NotNull(gns)
                Assert.Equal(0, gns.GetMembers().Length)
                Assert.Equal(0, gns.GetModuleMembers().Length)

                Dim cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single()

                Assert.True(String.Equals(MetadataCache.AssembliesFromFiles.Keys.Single().FullPath, mscorlibPath, StringComparison.OrdinalIgnoreCase))

                Dim assembly = cachedAssembly.Metadata.GetTarget().Assembly
                Assert.NotNull(assembly)

                Assert.True(assembly.Identity.Name.Equals("mscorlib"))
                Assert.Equal(0, assembly.AssemblyReferences.Length)
                Assert.Equal(1, assembly.ModuleReferenceCounts.Length)
                Assert.Equal(0, assembly.ModuleReferenceCounts(0))
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count)

                Dim mscorlibAsm = DirectCast(cachedAssembly.CachedSymbols.First(), PEAssemblySymbol)

                Assert.NotNull(mscorlibAsm)
                Assert.Same(mscorlibAsm.Assembly, cachedAssembly.Metadata.GetTarget().Assembly)
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

                Dim c2 = VisualBasicCompilation.Create("Test2", references:={New MetadataFileReference(mscorlibPath)})

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


        <Fact()>
        Public Sub CorLibTypes()
            Dim mdTestLib1 = TestReferences.SymbolsTests.MDTestLib1

            Using (MetadataCache.LockAndClean())

                Dim c1 = VisualBasicCompilation.Create("Test", references:={MscorlibRef_v4_0_30316_17626, mdTestLib1})

                Dim c107 As TypeSymbol = c1.GlobalNamespace.GetTypeMembers("C107").Single()

                Assert.Equal(SpecialType.None, c107.SpecialType)

                For i As Integer = 1 To SpecialType.Count Step 1
                    Dim type As NamedTypeSymbol = c1.Assembly.GetSpecialType(CType(i, SpecialType))
                    Assert.NotEqual(type.Kind, SymbolKind.ErrorType)
                    Assert.Equal(CType(i, SpecialType), type.SpecialType)
                Next

                Assert.Equal(SpecialType.None, c107.SpecialType)

                Dim arrayOfc107 = New ArrayTypeSymbol(c107, Nothing, 1, c1)

                Assert.Equal(SpecialType.None, arrayOfc107.SpecialType)

                Dim c2 = VisualBasicCompilation.Create("Test", references:={mdTestLib1})

                Assert.Equal(SpecialType.None, c2.GlobalNamespace.GetTypeMembers("C107").Single().SpecialType)
            End Using
        End Sub

        <Fact()>
        Public Sub RootNamespace_WithFiles_Simple()
            Dim sourceTree = ParserTestUtilities.Parse(
<text>
Namespace XYZ
    Public Class Clazz
    End Class
End Namespace
</text>.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, DefaultReferences, Options.OptionsDll.WithRootNamespace("A.B.C"))

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("C", root.Name)
            Assert.Equal("B", root.ContainingNamespace.Name)
            Assert.Equal("A", root.ContainingNamespace.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)
        End Sub

        <Fact()>
        Public Sub RootNamespace_WithFiles_UpdateCompilation()
            Dim sourceTree = ParserTestUtilities.Parse(
<text>
Namespace XYZ
    Public Class Clazz
    End Class
End Namespace
</text>.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, DefaultReferences, Options.OptionsDll)

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)

            c1 = c1.WithOptions(Options.OptionsDll.WithRootNamespace("A.B"))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("B", root.Name)
            Assert.Equal("A", root.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)

            c1 = c1.WithOptions(Options.OptionsDll.WithRootNamespace(""))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)
        End Sub

        <Fact()>
        Public Sub RootNamespace_NoFiles_UpdateCompilation()
            Dim c1 = VisualBasicCompilation.Create("Test", references:=DefaultReferences, options:=Options.OptionsDll)

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)

            c1 = c1.WithOptions(Options.OptionsDll.WithRootNamespace("A.B"))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("B", root.Name)
            Assert.Equal("A", root.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)

            c1 = c1.WithOptions(Options.OptionsDll.WithRootNamespace(""))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)
        End Sub

        Private Sub VerifyNamespaceShape(c1 As VisualBasicCompilation)

            Dim globalNs = c1.GlobalNamespace

            Assert.Equal(1, globalNs.GetNamespaceMembers().Count())
            Dim globalsChild = globalNs.GetNamespaceMembers().Single()
            Assert.Equal("FromOptions", globalsChild.Name)

            Assert.Equal(1, globalsChild.GetNamespaceMembers().Count())
            Assert.Equal("InSource", globalsChild.GetNamespaceMembers().Single.Name)

        End Sub

        <WorkItem(753078)>
        <Fact()>
        Public Sub RootNamespaceUpdateViaChangeInCompilationOptions()
            Dim sourceTree = ParserTestUtilities.Parse(
<text>
Namespace InSource
End Namespace
</text>.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, options:=Options.OptionsDll.WithRootNamespace("FromOptions"))
            VerifyNamespaceShape(c1)
            c1 = VisualBasicCompilation.Create("Test", {sourceTree}, options:=Options.OptionsDll)
            c1 = c1.WithOptions(c1.Options.WithRootNamespace("FromOptions"))
            VerifyNamespaceShape(c1)
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub ReferenceAnotherCompilation()
#Else
        <Fact()>
        Public Sub ReferenceAnotherCompilation()
#End If
            Using MetadataCache.LockAndClean()

                Dim tc1 = VisualBasicCompilation.Create("Test1", Nothing)
                Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol

                Dim c1Ref As New VisualBasicCompilationReference(tc1)

                Assert.Same(c1Ref.Properties.Alias, Nothing)
                Assert.Equal(False, c1Ref.Properties.EmbedInteropTypes)
                Assert.Same(c1Ref.Compilation, tc1)
                Assert.Same(tc1.Assembly, tc1.Assembly.CorLibrary)

                Dim tc2 = VisualBasicCompilation.Create("Test2", references:={c1Ref})
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

                Dim tc3 = VisualBasicCompilation.Create("Test3", references:={mscorlibRef})

                Assert.NotNull(tc3.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim mscorlibAsm = DirectCast(tc3.Assembly.Modules(0), SourceModuleSymbol).CorLibrary

                Dim c3Ref As New VisualBasicCompilationReference(tc3)

                Dim tc4 = VisualBasicCompilation.Create("Test4", references:={mscorlibRef, c3Ref})
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


                Dim tc5 = VisualBasicCompilation.Create("Test5", references:={c3Ref, mscorlibRef})
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
                Assert.Same(module1Ref.Properties.Alias, Nothing)
                Assert.Equal(False, module1Ref.Properties.EmbedInteropTypes)
                Assert.Equal(MetadataImageKind.Module, module1Ref.Properties.Kind)

                Dim tc1 = VisualBasicCompilation.Create("Test1", references:={module1Ref, mscorlibRef})

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

                Dim tc2 = VisualBasicCompilation.Create("Test2", references:={tc1Ref, module2Ref, MTTestLib1Ref, mscorlibRef, module1Ref})

                Assert.NotNull(tc2.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(2, MetadataCache.AssembliesFromFiles.Count)

                Dim mscorlibInfo = MetadataCache.AssembliesFromFiles(FileKey.Create(mscorlibPath))
                Assert.Equal(1, mscorlibInfo.CachedSymbols.Count)
                Assert.Same(mscorlibInfo.CachedSymbols.First(), mscorlibAsm)

                Dim MTTestLib1Info = MetadataCache.AssembliesFromFiles(FileKey.Create(MTTestLib1Path))
                Dim assembly = MTTestLib1Info.Metadata.GetTarget().Assembly
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

                Dim tc3 = VisualBasicCompilation.Create("Test3", references:={MTTestLib1V2Ref, tc2Ref})
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

                Dim tc4 = VisualBasicCompilation.Create("Test4", references:={MTTestLib1V2Ref, tc2Ref, mscorlibRef})
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

                Dim tc1 = VisualBasicCompilation.Create("Test1", references:={MscorlibRef, SystemCoreRef, multimoduleRef})

                Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol
                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count)
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count)

                Dim cachedAssembly = MetadataCache.AssembliesFromFiles(FileKey.Create(multimoduleRef.FullPath))
                Dim assembly = cachedAssembly.Metadata.GetTarget().Assembly
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
        Public Sub CyclicReference()
            Dim mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib
            Dim cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll

            Dim tc1 = VisualBasicCompilation.Create("Cyclic1", references:={mscorlibRef, cyclic2Ref})
            Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol

            Dim cyclic1Asm = DirectCast(tc1.Assembly, SourceAssemblySymbol)
            Dim cyclic1Mod = DirectCast(cyclic1Asm.Modules(0), SourceModuleSymbol)

            Dim cyclic2Asm = DirectCast(tc1.GetReferencedAssemblySymbol(cyclic2Ref), PEAssemblySymbol)
            Dim cyclic2Mod = DirectCast(cyclic2Asm.Modules(0), PEModuleSymbol)

            Assert.Same(cyclic2Mod.GetReferencedAssemblySymbols(1), cyclic1Asm)
            Assert.Same(cyclic1Mod.GetReferencedAssemblySymbols(1), cyclic2Asm)
        End Sub

        <Fact()>
        Public Sub MultiTargeting1()

            Dim varV1MTTestLib2Path = TestReferences.SymbolsTests.V1.MTTestLib2.dll
            Dim asm1 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
                varV1MTTestLib2Path
            })

            Assert.Equal("mscorlib", asm1(0).Identity.Name)
            Assert.Equal(0, asm1(0).BoundReferences().Length)
            Assert.Equal("MTTestLib2", asm1(1).Identity.Name)
            Assert.Equal(1, (From a In asm1(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm1(1).BoundReferences() Where a Is asm1(0) Select a).Count())
            Assert.Equal(SymbolKind.ErrorType, asm1(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType.Kind)

            Dim asm2 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
                varV1MTTestLib2Path,
            TestReferences.SymbolsTests.V1.MTTestLib1.dll
            })

            Assert.Same(asm2(0), asm1(0))
            Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
            Assert.NotSame(asm2(1), asm1(1))
            Assert.Same((DirectCast(asm2(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
            Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())

            Dim retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
            Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
            Assert.Equal(1, asm2(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())

            Dim varV2MTTestLib3Path = TestReferences.SymbolsTests.V2.MTTestLib3.dll
            Dim asm3 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
                varV1MTTestLib2Path,
            TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                varV2MTTestLib3Path
            })

            Assert.Same(asm3(0), asm1(0))
            Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
            Assert.NotSame(asm3(1), asm1(1))
            Assert.NotSame(asm3(1), asm2(1))
            Assert.Same((DirectCast(asm3(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())

            Dim retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
            Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
            Assert.NotSame(asm3(2), asm2(2))
            Assert.NotSame((DirectCast(asm3(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, asm3(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
            Assert.Equal(3, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
            Dim type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
            Dim retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
            Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
            Dim retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
            Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
            Dim retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
            Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())

            Dim varV3MTTestLib4Path = TestReferences.SymbolsTests.V3.MTTestLib4.dll
            Dim asm4 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
                varV1MTTestLib2Path,
            TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                varV2MTTestLib3Path,
                varV3MTTestLib4Path
            })

            Assert.Same(asm3(0), asm1(0))
            Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
            Assert.NotSame(asm4(1), asm1(1))
            Assert.NotSame(asm4(1), asm2(1))
            Assert.NotSame(asm4(1), asm3(1))
            Assert.Same((DirectCast(asm4(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
            Dim retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
            Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
            Assert.NotSame(asm4(2), asm2(2))
            Assert.NotSame(asm4(2), asm3(2))
            Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
            Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(2), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, asm4(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
            Assert.NotSame(asm4(3), asm3(3))
            Assert.Same((DirectCast(asm4(3), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(3), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
            Dim type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
            Dim retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
            Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            Dim retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
            Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
            Dim retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
            Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
            Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
            Assert.Equal(4, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
            Dim type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
            Dim retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
            Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            Dim retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
            Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
            Dim retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
            Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
            Dim retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
            Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
            Dim retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
            Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())
            Dim asm5 = GetSymbolsForReferences(
            {
                TestReferences.NetFx.v4_0_21006.mscorlib,
                TestReferences.SymbolsTests.V2.MTTestLib3.dll
            })

            Assert.Same(asm5(0), asm1(0))
            Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
            Dim asm6 = GetSymbolsForReferences(
            {
                TestReferences.NetFx.v4_0_21006.mscorlib,
                TestReferences.SymbolsTests.V1.MTTestLib2.dll
            })

            Assert.Same(asm6(0), asm1(0))
            Assert.Same(asm6(1), asm1(1))
            Dim asm7 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
            TestReferences.SymbolsTests.V1.MTTestLib2.dll,
            TestReferences.SymbolsTests.V2.MTTestLib3.dll,
            TestReferences.SymbolsTests.V3.MTTestLib4.dll
            })

            Assert.Same(asm7(0), asm1(0))
            Assert.Same(asm7(1), asm1(1))
            Assert.NotSame(asm7(2), asm3(3))
            Assert.NotSame(asm7(2), asm4(3))
            Assert.NotSame(asm7(3), asm4(4))
            Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
            Assert.Same((DirectCast(asm7(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(3), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
            Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
            Dim type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
            Dim retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval15.Kind)
            Dim retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval16.Kind)
            Dim retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
            Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
            Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
            Assert.Same((DirectCast(asm7(3), PEAssemblySymbol)).[Assembly], (DirectCast(asm4(4), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
            Dim type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
            Dim retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval18.Kind)
            Dim retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval19.Kind)
            Dim retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval20.Kind)
            Dim retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
            Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
            Dim retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
            Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())

            ' This test shows that simple reordering of references doesn't pick different set of assemblies
            Dim asm8 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
            TestReferences.SymbolsTests.V3.MTTestLib4.dll,
            TestReferences.SymbolsTests.V1.MTTestLib2.dll,
            TestReferences.SymbolsTests.V2.MTTestLib3.dll
            })

            Assert.Same(asm8(0), asm1(0))
            Assert.Same(asm8(0), asm1(0))
            Assert.Same(asm8(2), asm7(1))
            Assert.True(asm8(3).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(3)))
            Assert.Same(asm8(3), asm7(2))
            Assert.True(asm8(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
            Assert.Same(asm8(1), asm7(3))
            Dim asm9 = GetSymbolsForReferences(
            {
                TestReferences.NetFx.v4_0_21006.mscorlib,
                TestReferences.SymbolsTests.V3.MTTestLib4.dll
            })

            Assert.Same(asm9(0), asm1(0))
            Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
            Dim asm10 = GetSymbolsForReferences(
            {
            TestReferences.NetFx.v4_0_21006.mscorlib,
            TestReferences.SymbolsTests.V1.MTTestLib2.dll,
            TestReferences.SymbolsTests.V3.MTTestLib1.dll,
            TestReferences.SymbolsTests.V2.MTTestLib3.dll,
            TestReferences.SymbolsTests.V3.MTTestLib4.dll
            })

            Assert.Same(asm10(0), asm1(0))
            Assert.Same(asm10(1), asm4(1))
            Assert.Same(asm10(2), asm4(2))
            Assert.Same(asm10(3), asm4(3))
            Assert.Same(asm10(4), asm4(4))
            Assert.Equal("MTTestLib2", asm1(1).Identity.Name)
            Assert.Equal(1, (From a In asm1(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm1(1).BoundReferences() Where a Is asm1(0) Select a).Count())
            Assert.Equal(SymbolKind.ErrorType, asm1(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType.Kind)
            Assert.Same(asm2(0), asm1(0))
            Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
            Assert.NotSame(asm2(1), asm1(1))
            Assert.Same((DirectCast(asm2(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
            Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())
            retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
            Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
            Assert.Equal(1, asm2(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())
            Assert.Same(asm3(0), asm1(0))
            Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
            Assert.NotSame(asm3(1), asm1(1))
            Assert.NotSame(asm3(1), asm2(1))
            Assert.Same((DirectCast(asm3(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())
            retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
            Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
            Assert.NotSame(asm3(2), asm2(2))
            Assert.NotSame((DirectCast(asm3(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, asm3(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
            Assert.Equal(3, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
            Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
            type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
            retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
            Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
            retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
            Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
            retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
            Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())
            Assert.Same(asm3(0), asm1(0))
            Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
            Assert.NotSame(asm4(1), asm1(1))
            Assert.NotSame(asm4(1), asm2(1))
            Assert.NotSame(asm4(1), asm3(1))
            Assert.Same((DirectCast(asm4(1), PEAssemblySymbol)).[Assembly], (DirectCast(asm1(1), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
            retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
            Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
            Assert.NotSame(asm4(2), asm2(2))
            Assert.NotSame(asm4(2), asm3(2))
            Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
            Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(2), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, asm4(2).Identity.Version.Major)
            Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
            Assert.NotSame(asm4(3), asm3(3))
            Assert.Same((DirectCast(asm4(3), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(3), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
            Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
            type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
            retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
            Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
            Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
            retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
            Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
            Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
            Assert.Equal(4, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
            Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
            type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
            retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
            Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
            retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
            Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
            retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
            Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
            retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
            Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
            retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
            Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())
            Assert.Same(asm7(0), asm1(0))
            Assert.Same(asm7(1), asm1(1))
            Assert.NotSame(asm7(2), asm3(3))
            Assert.NotSame(asm7(2), asm4(3))
            Assert.NotSame(asm7(3), asm4(4))
            Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
            Assert.Same((DirectCast(asm7(2), PEAssemblySymbol)).Assembly, (DirectCast(asm3(3), PEAssemblySymbol)).[Assembly])
            Assert.Equal(2, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
            Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
            type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
            retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval15.Kind)
            retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval16.Kind)
            retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
            Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
            Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
            Assert.Same((DirectCast(asm7(3), PEAssemblySymbol)).Assembly, (DirectCast(asm4(4), PEAssemblySymbol)).[Assembly])
            Assert.Equal(3, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
            Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
            type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
            retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval18.Kind)
            retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval19.Kind)
            retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.Equal(SymbolKind.ErrorType, retval20.Kind)
            retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
            Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
            retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
            Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())
        End Sub

        Private Function CreateCompilation(
            identity As AssemblyIdentity,
            sources() As String,
            files() As String,
            compilations() As VisualBasicCompilation,
            Optional modules() As String = Nothing
        ) As VisualBasicCompilation
            Dim refs As New List(Of MetadataReference)

            If files IsNot Nothing Then
                For Each f In files
                    refs.Add(New MetadataFileReference(f))
                Next
            End If

            If compilations IsNot Nothing Then
                For Each c In compilations
                    refs.Add(New VisualBasicCompilationReference(c))
                Next
            End If

            If modules IsNot Nothing Then
                For Each m In modules
                    refs.Add(New MetadataFileReference(m, MetadataImageKind.Module))
                Next
            End If

            Return CreateCompilation(identity, sources, refs.ToArray())
        End Function

        Private Function CreateCompilation(
            identity As AssemblyIdentity,
            sources() As String,
            refs() As MetadataReference
        ) As VisualBasicCompilation
            If identity.Name Is Nothing OrElse identity.Name.Length = 0 Then
                identity = New AssemblyIdentity("Dummy")
            End If

            Dim trees As SyntaxTree() = Nothing

            If sources IsNot Nothing Then
                trees = New VisualBasicSyntaxTree(sources.Length - 1) {}

                For i As Integer = 0 To sources.Length - 1 Step 1
                    trees(i) = VisualBasicSyntaxTree.ParseText(sources(i))
                Next
            End If

            Dim tc1 = VisualBasicCompilation.Create(identity.Name, trees, refs)
            Assert.NotNull(tc1.Assembly) ' force creation of SourceAssemblySymbol

            DirectCast(tc1.Assembly, SourceAssemblySymbol).m_lazyIdentity = identity

            Return tc1
        End Function


#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting2()
#Else
        <Fact()>
        Public Sub MultiTargeting2()
#End If
            Using MetadataCache.LockAndClean()

                Dim varMTTestLib1_V1_Name = New AssemblyIdentity("MTTestLib1", New Version("1.0.0.0"))
                Dim varC_MTTestLib1_V1 = CreateCompilation(varMTTestLib1_V1_Name, New String() {<text>
Public Class Class1

End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, Nothing)
                Dim asm_MTTestLib1_V1 = varC_MTTestLib1_V1.SourceAssembly().BoundReferences()
                Dim varMTTestLib2_Name = New AssemblyIdentity("MTTestLib2")
                Dim varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name, New String() {<text>
Public Class Class4
    Function Foo() As Class1
        Return Nothing
    End Function

    Public Bar As Class1

End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib1_V1})
                Dim asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences()
                Assert.Same(asm_MTTestLib2(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm_MTTestLib2(1), varC_MTTestLib1_V1.SourceAssembly())
                Dim c2 = CreateCompilation(New AssemblyIdentity("c2"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V1})
                Dim asm2 = c2.SourceAssembly().BoundReferences()
                Assert.Same(asm2(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm2(1), varC_MTTestLib2.SourceAssembly())
                Assert.Same(asm2(2), varC_MTTestLib1_V1.SourceAssembly())
                Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())
                Dim retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
                Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
                Assert.Equal(1, asm2(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())
                Dim varMTTestLib1_V2_Name = New AssemblyIdentity("MTTestLib1", New Version("2.0.0.0"))
                Dim varC_MTTestLib1_V2 = CreateCompilation(varMTTestLib1_V2_Name, New String() {<text>
Public Class Class1

End Class

Public Class Class2

End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, Nothing)
                Dim asm_MTTestLib1_V2 = varC_MTTestLib1_V2.SourceAssembly().BoundReferences()
                Dim varMTTestLib3_Name = New AssemblyIdentity("MTTestLib3")
                Dim varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name, New String() {<text>
Public Class Class5
    Function Foo1() As Class1
        Return Nothing
    End Function

    Function Foo2() As Class2
        Return Nothing
    End Function

    Function Foo3() As Class4
        Return Nothing
    End Function

    Public Bar1 As Class1
    Public Bar2 As Class2
    Public Bar3 As Class4
End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V2})
                Dim asm_MTTestLib3 = varC_MTTestLib3.SourceAssembly().BoundReferences()
                Assert.Same(asm_MTTestLib3(0), asm_MTTestLib1_V1(0))
                Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm_MTTestLib3(2), varC_MTTestLib1_V1.SourceAssembly())
                Dim c3 = CreateCompilation(New AssemblyIdentity("c3"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V2, varC_MTTestLib3})
                Dim asm3 = c3.SourceAssembly().BoundReferences()
                Assert.Same(asm3(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm3(1), asm_MTTestLib3(1))
                Assert.Same(asm3(2), asm_MTTestLib3(2))
                Assert.Same(asm3(3), varC_MTTestLib3.SourceAssembly())
                Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
                Assert.Same((DirectCast(asm3(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())
                Dim retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
                Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
                Assert.NotSame(asm3(2), asm2(2))
                Assert.NotSame(asm3(2).DeclaringCompilation, asm2(2).DeclaringCompilation)
                Assert.Equal(2, asm3(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
                Assert.Equal(3, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
                Dim type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
                Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
                Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
                Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim varMTTestLib1_V3_Name = New AssemblyIdentity("MTTestLib1", New Version("3.0.0.0"))
                Dim varC_MTTestLib1_V3 = CreateCompilation(varMTTestLib1_V3_Name, New String() {<text>
Public Class Class1

End Class

Public Class Class2

End Class

Public Class Class3

End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, Nothing)
                Dim asm_MTTestLib1_V3 = varC_MTTestLib1_V3.SourceAssembly().BoundReferences()
                Dim varMTTestLib4_Name = New AssemblyIdentity("MTTestLib4")
                Dim varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name, New String() {<text>
Public Class Class6
    Function Foo1() As Class1
        Return Nothing
    End Function

    Function Foo2() As Class2
        Return Nothing
    End Function

    Function Foo3() As Class3
        Return Nothing
    End Function

    Function Foo4() As Class4
        Return Nothing
    End Function

    Function Foo5() As Class5
        Return Nothing
    End Function

    Public Bar1 As Class1
    Public Bar2 As Class2
    Public Bar3 As Class3
    Public Bar4 As Class4
    Public Bar5 As Class5

End Class
                </text>.Value}, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3})
                Dim asm_MTTestLib4 = varC_MTTestLib4.SourceAssembly().BoundReferences()
                Assert.Same(asm_MTTestLib4(0), asm_MTTestLib1_V1(0))
                Assert.NotSame(asm_MTTestLib4(1), varC_MTTestLib2.SourceAssembly())
                Assert.Same(asm_MTTestLib4(2), varC_MTTestLib1_V3.SourceAssembly())
                Assert.NotSame(asm_MTTestLib4(3), varC_MTTestLib3.SourceAssembly())
                Dim c4 = CreateCompilation(New AssemblyIdentity("c4"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3, varC_MTTestLib4})
                Dim asm4 = c4.SourceAssembly().BoundReferences()
                Assert.Same(asm4(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm4(1), asm_MTTestLib4(1))
                Assert.Same(asm4(2), asm_MTTestLib4(2))
                Assert.Same(asm4(3), asm_MTTestLib4(3))
                Assert.Same(asm4(4), varC_MTTestLib4.SourceAssembly())
                Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
                Assert.NotSame(asm4(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm4(1), asm2(1))
                Assert.NotSame(asm4(1), asm3(1))
                Assert.Same((DirectCast(asm4(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
                Dim retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
                Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
                Assert.NotSame(asm4(2), asm2(2))
                Assert.NotSame(asm4(2), asm3(2))
                Assert.NotSame(asm4(2).DeclaringCompilation, asm2(2).DeclaringCompilation)
                Assert.NotSame(asm4(2).DeclaringCompilation, asm3(2).DeclaringCompilation)
                Assert.Equal(3, asm4(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
                Assert.NotSame(asm4(3), asm3(3))
                Assert.Same((DirectCast(asm4(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(3, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
                Dim type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
                Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
                Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
                Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
                Assert.Equal(4, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
                Dim type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
                Dim retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
                Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
                Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
                Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
                Dim retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
                Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
                Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())
                Dim c5 = CreateCompilation(New AssemblyIdentity("c5"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib3})
                Dim asm5 = c5.SourceAssembly().BoundReferences()
                Assert.Same(asm5(0), asm2(0))
                Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
                Dim c6 = CreateCompilation(New AssemblyIdentity("c6"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2})
                Dim asm6 = c6.SourceAssembly().BoundReferences()
                Assert.Same(asm6(0), asm2(0))
                Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Dim c7 = CreateCompilation(New AssemblyIdentity("c6"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib3, varC_MTTestLib4})
                Dim asm7 = c7.SourceAssembly().BoundReferences()
                Assert.Same(asm7(0), asm2(0))
                Assert.True(asm7(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Assert.NotSame(asm7(2), asm3(3))
                Assert.NotSame(asm7(2), asm4(3))
                Assert.NotSame(asm7(3), asm4(4))
                Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
                Assert.Same((DirectCast(asm7(2), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
                Dim type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name)
                Assert.Equal(0, (From a In asm7 Where a IsNot Nothing AndAlso a.Name = "MTTestLib1" Select a).Count())
                Dim retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name)
                Dim retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
                Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
                Assert.Same((DirectCast(asm7(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm4(4))
                Assert.Equal(3, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
                Dim type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
                Dim retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name)
                Dim retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name)
                Dim retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name)
                Dim retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
                Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
                Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())
                ' This test shows that simple reordering of references doesn't pick different set of assemblies
                Dim c8 = CreateCompilation(New AssemblyIdentity("c8"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib4, varC_MTTestLib2, varC_MTTestLib3})
                Dim asm8 = c8.SourceAssembly().BoundReferences()
                Assert.Same(asm8(0), asm2(0))
                Assert.True(asm8(2).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(1)))
                Assert.Same(asm8(2), asm7(1))
                Assert.True(asm8(3).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(3)))
                Assert.Same(asm8(3), asm7(2))
                Assert.True(asm8(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
                Assert.Same(asm8(1), asm7(3))
                Dim c9 = CreateCompilation(New AssemblyIdentity("c9"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib4})
                Dim asm9 = c9.SourceAssembly().BoundReferences()
                Assert.Same(asm9(0), asm2(0))
                Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
                Dim c10 = CreateCompilation(New AssemblyIdentity("c10"), Nothing, New String() {GetType(Integer).[Assembly].Location}, New VisualBasicCompilation() {varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3, varC_MTTestLib4})
                Dim asm10 = c10.SourceAssembly().BoundReferences()
                Assert.Same(asm10(0), asm2(0))
                Assert.Same(asm10(1), asm4(1))
                Assert.Same(asm10(2), asm4(2))
                Assert.Same(asm10(3), asm4(3))
                Assert.Same(asm10(4), asm4(4))
                Assert.Same(asm2(0), asm_MTTestLib1_V1(0))
                Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Equal(1, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())
                retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
                Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
                Assert.Equal(1, asm2(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Same(asm_MTTestLib3(0), asm_MTTestLib1_V1(0))
                Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm_MTTestLib3(2), varC_MTTestLib1_V1.SourceAssembly())
                Assert.Same(asm3(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm3(1), asm_MTTestLib3(1))
                Assert.Same(asm3(2), asm_MTTestLib3(2))
                Assert.Same(asm3(3), varC_MTTestLib3.SourceAssembly())
                Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
                Assert.Same((DirectCast(asm3(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(1, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())
                retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
                Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
                Assert.NotSame(asm3(2), asm2(2))
                Assert.NotSame(asm3(2).DeclaringCompilation, asm2(2).DeclaringCompilation)
                Assert.Equal(2, asm3(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
                Assert.Equal(3, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
                Assert.Equal(1, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
                type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
                Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
                Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
                retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
                Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Same(asm4(0), asm_MTTestLib1_V1(0))
                Assert.Same(asm4(1), asm_MTTestLib4(1))
                Assert.Same(asm4(2), asm_MTTestLib4(2))
                Assert.Same(asm4(3), asm_MTTestLib4(3))
                Assert.Same(asm4(4), varC_MTTestLib4.SourceAssembly())
                Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
                Assert.NotSame(asm4(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm4(1), asm2(1))
                Assert.NotSame(asm4(1), asm3(1))
                Assert.Same((DirectCast(asm4(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
                retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
                Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
                Assert.NotSame(asm4(2), asm2(2))
                Assert.NotSame(asm4(2), asm3(2))
                Assert.NotSame(asm4(2).DeclaringCompilation, asm2(2).DeclaringCompilation)
                Assert.NotSame(asm4(2).DeclaringCompilation, asm3(2).DeclaringCompilation)
                Assert.Equal(3, asm4(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
                Assert.NotSame(asm4(3), asm3(3))
                Assert.Same((DirectCast(asm4(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(3, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(1, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
                type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
                Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
                Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
                Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
                Assert.Equal(4, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
                Assert.Equal(1, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
                type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
                retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
                Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
                Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
                Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
                retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
                Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
                Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())
                Assert.Same(asm5(0), asm2(0))
                Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
                Assert.Same(asm6(0), asm2(0))
                Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Assert.Same(asm7(0), asm2(0))
                Assert.True(asm7(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Assert.NotSame(asm7(2), asm3(3))
                Assert.NotSame(asm7(2), asm4(3))
                Assert.NotSame(asm7(3), asm4(4))
                Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
                Assert.Same((DirectCast(asm7(2), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(1, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
                type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name)
                Assert.Equal(0, (From a In asm7 Where a IsNot Nothing AndAlso a.Name = "MTTestLib1" Select a).Count())
                retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name)
                retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
                Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
                Assert.Same((DirectCast(asm7(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm4(4))
                Assert.Equal(3, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
                Assert.Equal(1, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
                type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
                retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name)
                retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name)
                retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name)
                retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
                Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
                Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())

            End Using

        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting3()
#Else
        <Fact()>
        Public Sub MultiTargeting3()
#End If
            Using MetadataCache.LockAndClean()

                Dim varMTTestLib2_Name = New AssemblyIdentity("MTTestLib2")
                Dim varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name,
                                                        Nothing,
                                                        {TestReferences.NetFx.v4_0_30319.mscorlib,
                                                        TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                                        TestReferences.SymbolsTests.V1.MTTestModule2.netmodule})

                Dim asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences()
                Dim c2 = CreateCompilation(New AssemblyIdentity("c2"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                           TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                           New VisualBasicCompilationReference(varC_MTTestLib2)})

                Dim asm2Prime = c2.SourceAssembly().BoundReferences()
                Dim asm2 = New AssemblySymbol() {asm2Prime(0), asm2Prime(2), asm2Prime(1)}
                Assert.Same(asm2(0), asm_MTTestLib2(0))
                Assert.Same(asm2(1), varC_MTTestLib2.SourceAssembly())
                Assert.Same(asm2(2), asm_MTTestLib2(1))
                Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
                Assert.Equal(4, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())
                Dim retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval1, asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Bar").OfType(Of FieldSymbol)().Single().[Type])
                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
                Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
                Assert.Equal(1, asm2(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())
                Dim varMTTestLib3_Name = New AssemblyIdentity("MTTestLib3")

                Dim varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name,
                                                        Nothing,
                                                        {TestReferences.NetFx.v4_0_30319.mscorlib,
                                                        TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                                        New VisualBasicCompilationReference(varC_MTTestLib2),
                                                        TestReferences.SymbolsTests.V2.MTTestModule3.netmodule})

                Dim asm_MTTestLib3Prime = varC_MTTestLib3.SourceAssembly().BoundReferences()
                Dim asm_MTTestLib3 = New AssemblySymbol() {asm_MTTestLib3Prime(0), asm_MTTestLib3Prime(2), asm_MTTestLib3Prime(1)}
                Assert.Same(asm_MTTestLib3(0), asm_MTTestLib2(0))
                Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm_MTTestLib3(2), asm_MTTestLib2(1))

                Dim c3 = CreateCompilation(New AssemblyIdentity("c3"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                           TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                           New VisualBasicCompilationReference(varC_MTTestLib2),
                                           New VisualBasicCompilationReference(varC_MTTestLib3)})

                Dim asm3Prime = c3.SourceAssembly().BoundReferences()
                Dim asm3 = New AssemblySymbol() {asm3Prime(0), asm3Prime(2), asm3Prime(1), asm3Prime(3)}
                Assert.Same(asm3(0), asm_MTTestLib2(0))
                Assert.Same(asm3(1), asm_MTTestLib3(1))
                Assert.Same(asm3(2), asm_MTTestLib3(2))
                Assert.Same(asm3(3), varC_MTTestLib3.SourceAssembly())
                Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
                Assert.Same((DirectCast(asm3(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(4, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())
                Dim retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval2, asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Bar").OfType(Of FieldSymbol)().Single().[Type])
                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
                Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
                Assert.NotSame(asm3(2), asm2(2))
                Assert.NotSame(asm3(2), asm2(2))
                Assert.NotSame((DirectCast(asm3(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
                Assert.Equal(2, asm3(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
                Assert.Equal(6, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
                Dim type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
                Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
                Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
                Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim varMTTestLib4_Name = New AssemblyIdentity("MTTestLib4")

                Dim varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name,
                                                        Nothing,
                                                        {TestReferences.NetFx.v4_0_30319.mscorlib,
                                                        TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                                        New VisualBasicCompilationReference(varC_MTTestLib2),
                                                        New VisualBasicCompilationReference(varC_MTTestLib3),
                                                        TestReferences.SymbolsTests.V3.MTTestModule4.netmodule})

                Dim asm_MTTestLib4Prime = varC_MTTestLib4.SourceAssembly().BoundReferences()
                Dim asm_MTTestLib4 = New AssemblySymbol() {asm_MTTestLib4Prime(0), asm_MTTestLib4Prime(2), asm_MTTestLib4Prime(1), asm_MTTestLib4Prime(3)}
                Assert.Same(asm_MTTestLib4(0), asm_MTTestLib2(0))
                Assert.NotSame(asm_MTTestLib4(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm_MTTestLib4(2), asm3(2))
                Assert.NotSame(asm_MTTestLib4(2), asm2(2))
                Assert.NotSame(asm_MTTestLib4(3), varC_MTTestLib3.SourceAssembly())

                Dim c4 = CreateCompilation(New AssemblyIdentity("c4"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                            New VisualBasicCompilationReference(varC_MTTestLib2),
                                            New VisualBasicCompilationReference(varC_MTTestLib3),
                                            New VisualBasicCompilationReference(varC_MTTestLib4)})

                Dim asm4Prime = c4.SourceAssembly().BoundReferences()
                Dim asm4 = New AssemblySymbol() {asm4Prime(0), asm4Prime(2), asm4Prime(1), asm4Prime(3), asm4Prime(4)}
                Assert.Same(asm4(0), asm_MTTestLib2(0))
                Assert.Same(asm4(1), asm_MTTestLib4(1))
                Assert.Same(asm4(2), asm_MTTestLib4(2))
                Assert.Same(asm4(3), asm_MTTestLib4(3))
                Assert.Same(asm4(4), varC_MTTestLib4.SourceAssembly())
                Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
                Assert.NotSame(asm4(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm4(1), asm2(1))
                Assert.NotSame(asm4(1), asm3(1))
                Assert.Same((DirectCast(asm4(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(4, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
                Dim retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
                Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
                Assert.NotSame(asm4(2), asm2(2))
                Assert.NotSame(asm4(2), asm3(2))
                Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
                Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(2), PEAssemblySymbol)).[Assembly])
                Assert.Equal(3, asm4(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
                Assert.NotSame(asm4(3), asm3(3))
                Assert.Same((DirectCast(asm4(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(6, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
                Dim type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
                Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
                Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
                Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
                Assert.Equal(8, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
                Dim type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
                Dim retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
                Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Dim retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
                Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                Dim retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
                Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
                Dim retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
                Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
                Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())

                Dim c5 = CreateCompilation(New AssemblyIdentity("c5"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            New VisualBasicCompilationReference(varC_MTTestLib3)})

                Dim asm5 = c5.SourceAssembly().BoundReferences()
                Assert.Same(asm5(0), asm2(0))

                Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))

                Dim c6 = CreateCompilation(New AssemblyIdentity("c6"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            New VisualBasicCompilationReference(varC_MTTestLib2)})

                Dim asm6 = c6.SourceAssembly().BoundReferences()
                Assert.Same(asm6(0), asm2(0))

                Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))

                Dim c7 = CreateCompilation(New AssemblyIdentity("c7"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            New VisualBasicCompilationReference(varC_MTTestLib2),
                                            New VisualBasicCompilationReference(varC_MTTestLib3),
                                            New VisualBasicCompilationReference(varC_MTTestLib4)})

                Dim asm7 = c7.SourceAssembly().BoundReferences()
                Assert.Same(asm7(0), asm2(0))

                Assert.True(asm7(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))

                Assert.NotSame(asm7(2), asm3(3))
                Assert.NotSame(asm7(2), asm4(3))
                Assert.NotSame(asm7(3), asm4(4))
                Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
                Assert.Same((DirectCast(asm7(2), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(4, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
                Dim type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
                Dim retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType

                Dim missingAssembly As AssemblySymbol

                missingAssembly = retval15.ContainingAssembly

                Assert.True(missingAssembly.IsMissing)
                Assert.Equal("MTTestLib1", missingAssembly.Identity.Name)

                Dim retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(missingAssembly, retval16.ContainingAssembly)
                Dim retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
                Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
                Assert.Same((DirectCast(asm7(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm4(4))
                Assert.Equal(6, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
                Dim type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
                Dim retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", (DirectCast(retval18, MissingMetadataTypeSymbol)).ContainingAssembly.Identity.Name)
                Dim retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly)
                Dim retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly)
                Dim retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
                Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Dim retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
                Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())

                ' This test shows that simple reordering of references doesn't pick different set of assemblies
                Dim c8 = CreateCompilation(New AssemblyIdentity("c8"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            New VisualBasicCompilationReference(varC_MTTestLib4),
                                            New VisualBasicCompilationReference(varC_MTTestLib2),
                                            New VisualBasicCompilationReference(varC_MTTestLib3)})

                Dim asm8 = c8.SourceAssembly().BoundReferences()
                Assert.Same(asm8(0), asm2(0))

                Assert.True(asm8(2).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(1)))

                Assert.Same(asm8(2), asm7(1))

                Assert.True(asm8(3).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(3)))

                Assert.Same(asm8(3), asm7(2))

                Assert.True(asm8(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))

                Assert.Same(asm8(1), asm7(3))

                Dim c9 = CreateCompilation(New AssemblyIdentity("c9"),
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            New VisualBasicCompilationReference(varC_MTTestLib4)})

                Dim asm9 = c9.SourceAssembly().BoundReferences()
                Assert.Same(asm9(0), asm2(0))

                Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))

                Dim c10 = CreateCompilation(New AssemblyIdentity("c10"),
                                            Nothing,
                                            {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                            New VisualBasicCompilationReference(varC_MTTestLib2),
                                            New VisualBasicCompilationReference(varC_MTTestLib3),
                                            New VisualBasicCompilationReference(varC_MTTestLib4)})

                Dim asm10Prime = c10.SourceAssembly().BoundReferences()
                Dim asm10 = New AssemblySymbol() {asm10Prime(0), asm10Prime(2), asm10Prime(1), asm10Prime(3), asm10Prime(4)}
                Assert.Same(asm10(0), asm2(0))
                Assert.Same(asm10(1), asm4(1))
                Assert.Same(asm10(2), asm4(2))
                Assert.Same(asm10(3), asm4(3))
                Assert.Same(asm10(4), asm4(4))
                Assert.Same(asm2(0), asm_MTTestLib2(0))
                Assert.Equal("MTTestLib2", asm2(1).Identity.Name)
                Assert.Equal(4, (From a In asm2(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Equal(2, (From a In asm2(1).BoundReferences() Where a Is asm2(2) Select a).Count())
                retval1 = asm2(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind)
                Assert.Same(retval1, asm2(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm2(2).Identity.Name)
                Assert.Equal(1, asm2(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm2(2).BoundReferences() Where a Is asm2(0) Select a).Count())
                Assert.Same(asm_MTTestLib3(0), asm_MTTestLib2(0))
                Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm_MTTestLib3(2), asm_MTTestLib2(1))
                Assert.Same(asm3(0), asm_MTTestLib2(0))
                Assert.Same(asm3(1), asm_MTTestLib3(1))
                Assert.Same(asm3(2), asm_MTTestLib3(2))
                Assert.Same(asm3(3), varC_MTTestLib3.SourceAssembly())
                Assert.Equal("MTTestLib2", asm3(1).Identity.Name)
                Assert.Same((DirectCast(asm3(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(4, (From a In asm3(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(2, (From a In asm3(1).BoundReferences() Where a Is asm3(2) Select a).Count())
                retval2 = asm3(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind)
                Assert.Same(retval2, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm3(2).Identity.Name)
                Assert.NotSame(asm3(2), asm2(2))
                Assert.NotSame((DirectCast(asm3(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
                Assert.Equal(2, asm3(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm3(2).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm3(3).Identity.Name)
                Assert.Equal(6, (From a In asm3(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(0) Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(1) Select a).Count())
                Assert.Equal(2, (From a In asm3(3).BoundReferences() Where a Is asm3(2) Select a).Count())
                type1 = asm3(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval3 = type1.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind)
                Assert.Same(retval3, asm3(2).GlobalNamespace.GetMembers("Class1").Single())
                retval4 = type1.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind)
                Assert.Same(retval4, asm3(2).GlobalNamespace.GetMembers("Class2").Single())
                retval5 = type1.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind)
                Assert.Same(retval5, asm3(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Same(asm4(0), asm_MTTestLib2(0))
                Assert.Same(asm4(1), asm_MTTestLib4(1))
                Assert.Same(asm4(2), asm_MTTestLib4(2))
                Assert.Same(asm4(3), asm_MTTestLib4(3))
                Assert.Same(asm4(4), varC_MTTestLib4.SourceAssembly())
                Assert.Equal("MTTestLib2", asm4(1).Identity.Name)
                Assert.NotSame(asm4(1), varC_MTTestLib2.SourceAssembly())
                Assert.NotSame(asm4(1), asm2(1))
                Assert.NotSame(asm4(1), asm3(1))
                Assert.Same((DirectCast(asm4(1), RetargetingAssemblySymbol)).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly())
                Assert.Equal(4, (From a In asm4(1).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(1).BoundReferences() Where a Is asm4(2) Select a).Count())
                retval6 = asm4(1).GlobalNamespace.GetTypeMembers("Class4").Single().GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind)
                Assert.Same(retval6, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                Assert.Equal("MTTestLib1", asm4(2).Identity.Name)
                Assert.NotSame(asm4(2), asm2(2))
                Assert.NotSame(asm4(2), asm3(2))
                Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm2(2), PEAssemblySymbol)).[Assembly])
                Assert.NotSame((DirectCast(asm4(2), PEAssemblySymbol)).[Assembly], (DirectCast(asm3(2), PEAssemblySymbol)).[Assembly])
                Assert.Equal(3, asm4(2).Identity.Version.Major)
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(1, (From a In asm4(2).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal("MTTestLib3", asm4(3).Identity.Name)
                Assert.NotSame(asm4(3), asm3(3))
                Assert.Same((DirectCast(asm4(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(6, (From a In asm4(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(2, (From a In asm4(3).BoundReferences() Where a Is asm4(2) Select a).Count())
                type2 = asm4(3).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval7 = type2.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind)
                Assert.Same(retval7, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                retval8 = type2.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind)
                Assert.Same(retval8, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                retval9 = type2.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind)
                Assert.Same(retval9, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm4(4).Identity.Name)
                Assert.Equal(8, (From a In asm4(4).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(0) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(1) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(2) Select a).Count())
                Assert.Equal(2, (From a In asm4(4).BoundReferences() Where a Is asm4(3) Select a).Count())
                type3 = asm4(4).GlobalNamespace.GetTypeMembers("Class6").Single()
                retval10 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind)
                Assert.Same(retval10, asm4(2).GlobalNamespace.GetMembers("Class1").Single())
                retval11 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind)
                Assert.Same(retval11, asm4(2).GlobalNamespace.GetMembers("Class2").Single())
                retval12 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind)
                Assert.Same(retval12, asm4(2).GlobalNamespace.GetMembers("Class3").Single())
                retval13 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind)
                Assert.Same(retval13, asm4(1).GlobalNamespace.GetMembers("Class4").Single())
                retval14 = type3.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind)
                Assert.Same(retval14, asm4(3).GlobalNamespace.GetMembers("Class5").Single())
                Assert.Same(asm5(0), asm2(0))
                Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
                Assert.Same(asm6(0), asm2(0))
                Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Assert.Same(asm7(0), asm2(0))
                Assert.True(asm7(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
                Assert.NotSame(asm7(2), asm3(3))
                Assert.NotSame(asm7(2), asm4(3))
                Assert.NotSame(asm7(3), asm4(4))
                Assert.Equal("MTTestLib3", asm7(2).Identity.Name)
                Assert.Same((DirectCast(asm7(2), RetargetingAssemblySymbol)).UnderlyingAssembly, asm3(3))
                Assert.Equal(4, (From a In asm7(2).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(2, (From a In asm7(2).BoundReferences() Where a Is asm7(1) Select a).Count())
                type4 = asm7(2).GlobalNamespace.GetTypeMembers("Class5").Single()
                retval15 = type4.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType

                missingAssembly = retval15.ContainingAssembly

                Assert.True(missingAssembly.IsMissing)
                Assert.Equal("MTTestLib1", missingAssembly.Identity.Name)

                retval16 = type4.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(missingAssembly, retval16.ContainingAssembly)
                retval17 = type4.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind)
                Assert.Same(retval17, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                Assert.Equal("MTTestLib4", asm7(3).Identity.Name)
                Assert.Same((DirectCast(asm7(3), RetargetingAssemblySymbol)).UnderlyingAssembly, asm4(4))
                Assert.Equal(6, (From a In asm7(3).BoundReferences() Where Not a.IsMissing Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(0) Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(1) Select a).Count())
                Assert.Equal(2, (From a In asm7(3).BoundReferences() Where a Is asm7(2) Select a).Count())
                type5 = asm7(3).GlobalNamespace.GetTypeMembers("Class6").Single()
                retval18 = type5.GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("MTTestLib1", (DirectCast(retval18, MissingMetadataTypeSymbol)).ContainingAssembly.Identity.Name)
                retval19 = type5.GetMembers("Foo2").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly)
                retval20 = type5.GetMembers("Foo3").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly)
                retval21 = type5.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind)
                Assert.Same(retval21, asm7(1).GlobalNamespace.GetMembers("Class4").Single())
                retval22 = type5.GetMembers("Foo5").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind)
                Assert.Same(retval22, asm7(2).GlobalNamespace.GetMembers("Class5").Single())

            End Using

        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting4()
#Else
        <Fact()>
        Public Sub MultiTargeting4()
#End If
            Using MetadataCache.LockAndClean()

                '"Class C1(Of T)" & vbCrLf &
                '"   Class C2(Of S)" & vbCrLf &
                '"   End Class" & vbCrLf &
                '"End Class" & vbCrLf &
                '"" & vbCrLf &
                '"Class C3" & vbCrLf &
                '"   Function Foo() As C1(Of C3).C2(Of C4)" & vbCrLf &
                '"   End Function" & vbCrLf &
                '"End Class" & vbCrLf &
                '"" & vbCrLf &
                '"Class C4" & vbCrLf &
                '"End Class"

                Dim source1 =
        <s1>
Public Class C1(Of T)
    Public Class C2(Of S)
        Public Function Foo() As C1(Of T).C2(Of S)
            Return Nothing
        End Function
    End Class
End Class
</s1>
                Dim c1_V1_Name = New AssemblyIdentity("c1", New Version("1.0.0.0"))

                Dim c1_V1 As VisualBasicCompilation = CreateCompilation(c1_V1_Name,
                               {source1.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib})

                Dim asm1_V1 = c1_V1.SourceAssembly

                Dim c1_V2_Name = New AssemblyIdentity("c1", New Version("2.0.0.0"))

                Dim c1_V2 As VisualBasicCompilation = CreateCompilation(c1_V2_Name,
                               {source1.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib})

                Dim asm1_V2 = c1_V2.SourceAssembly

                Dim source4 =
        <s4>
Public Class C4
End Class
</s4>


                Dim c4_V1_Name = New AssemblyIdentity("c4", New Version("1.0.0.0"))

                Dim c4_V1 As VisualBasicCompilation = CreateCompilation(c4_V1_Name,
                               {source4.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib})

                Dim asm4_V1 = c4_V1.SourceAssembly

                Dim c4_V2_Name = New AssemblyIdentity("c4", New Version("2.0.0.0"))

                Dim c4_V2 As VisualBasicCompilation = CreateCompilation(c4_V2_Name,
                               {source4.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib})

                Dim asm4_V2 = c4_V2.SourceAssembly

                Dim source7 =
        <s3>
Public Class C7
End Class

Public Class C8(Of T)
End Class
</s3>

                Dim c7 As VisualBasicCompilation = CreateCompilation(New AssemblyIdentity("C7"),
                               {source7.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib})

                Dim asm7 = c7.SourceAssembly


                Dim source3 =
        <s3>
Public Class C3
    Public Function Foo() As C1(Of C3).C2(Of C4)
        Return Nothing
    End Function

    Public Shared Function Bar() As C6(Of C4)
        Return Nothing
    End Function

    Public Function Foo1() As C8(Of C7)
        Return Nothing
    End Function

    Public Sub Foo2(ByRef x1(,) As C300,
                    &lt;System.Runtime.InteropServices.Out()&gt; ByRef x2 As C4,
                    ByRef x3() As C7,
                    Optional ByVal x4 As C4 = Nothing)
    End Sub

    Friend Overridable Function Foo3(Of TFoo3 As C4)() As TFoo3
        Return Nothing
    End Function

    Public Function Foo4() As C8(Of C4)
        Return Nothing
    End Function

    Public MustInherit Class C301
        Implements I1

    End Class

    Friend Class C302

    End Class

End Class

Public Class C6(Of T As New)
End Class

Public Class C300
End Class

Public Interface I1
End Interface

Namespace ns1

    Namespace ns2

        Public Class C303
        End Class

    End Namespace

    Public Class C304

        Public Class C305
        End Class

    End Class

End Namespace
</s3>

                Dim c3 As VisualBasicCompilation = CreateCompilation(New AssemblyIdentity("C3"),
                               {source3.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib,
                                New VisualBasicCompilationReference(c1_V1),
                                New VisualBasicCompilationReference(c4_V1),
                                New VisualBasicCompilationReference(c7)})

                Dim asm3 = c3.SourceAssembly

                Dim localC3Foo2 = asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetMembers("Foo2").OfType(Of MethodSymbol)().Single()


                Dim source5 =
        <s5>
Public Class C5
    Inherits ns1.C304.C305
End Class
</s5>

                Dim c5 As VisualBasicCompilation = CreateCompilation(New AssemblyIdentity("C5"),
                               {source5.Value},
                               {TestReferences.NetFx.v4_0_30319.mscorlib,
                               New VisualBasicCompilationReference(c3),
                               New VisualBasicCompilationReference(c1_V2),
                               New VisualBasicCompilationReference(c4_V2),
                               New VisualBasicCompilationReference(c7)})

                Dim asm5 = c5.SourceAssembly.BoundReferences

                Assert.NotSame(asm5(1), asm3)
                Assert.Same(DirectCast(asm5(1), RetargetingAssemblySymbol).UnderlyingAssembly, asm3)
                Assert.Same(asm5(2), asm1_V2)
                Assert.Same(asm5(3), asm4_V2)
                Assert.Same(asm5(4), asm7)

                Dim type3 = asm5(1).GlobalNamespace.GetTypeMembers("C3").Single()
                Dim type1 = asm1_V2.GlobalNamespace.GetTypeMembers("C1").Single()
                Dim type2 = type1.GetTypeMembers("C2").Single()
                Dim type4 = asm4_V2.GlobalNamespace.GetTypeMembers("C4").Single()
                Dim retval1 = DirectCast(type3.GetMembers("Foo").OfType(Of MethodSymbol)().Single().ReturnType, NamedTypeSymbol)
                Assert.Equal("C1(Of C3).C2(Of C4)", retval1.ToTestDisplayString())
                Assert.Same(retval1.OriginalDefinition, type2)
                Dim args1 = retval1.ContainingType.TypeArguments.Concat(retval1.TypeArguments)
                Dim params1 = retval1.ContainingType.TypeParameters.Concat(retval1.TypeParameters)
                Assert.Same(params1(0), type1.TypeParameters(0))
                Assert.Same(params1(1).OriginalDefinition, type2.TypeParameters(0).OriginalDefinition)
                Assert.Same(args1(0), type3)
                Assert.Same(args1(0).ContainingAssembly, asm5(1))
                Assert.Same(args1(1), type4)
                Dim retval2 = retval1.ContainingType
                Assert.Equal("C1(Of C3)", retval2.ToTestDisplayString())
                Assert.Same(retval2.OriginalDefinition, type1)
                Dim bar = type3.GetMembers("Bar").OfType(Of MethodSymbol)().Single()
                Dim retval3 = DirectCast(bar.ReturnType, NamedTypeSymbol)
                Dim type6 = asm5(1).GlobalNamespace.GetTypeMembers("C6").Single()
                Assert.Equal("C6(Of C4)", retval3.ToTestDisplayString())
                Assert.Same(retval3.OriginalDefinition, type6)
                Assert.Same(retval3.ContainingAssembly, asm5(1))
                Dim args3 = retval3.TypeArguments
                Dim params3 = retval3.TypeParameters
                Assert.Same(params3(0), type6.TypeParameters(0))
                Assert.Same(params3(0).ContainingAssembly, asm5(1))
                Assert.Same(args3(0), type4)
                Dim foo1 = type3.GetMembers("Foo1").OfType(Of MethodSymbol)().Single()
                Dim retval4 = foo1.ReturnType
                Assert.Equal("C8(Of C7)", retval4.ToTestDisplayString())
                Assert.Same(retval4, asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetMembers("Foo1").OfType(Of MethodSymbol)().Single().ReturnType)
                Dim foo1Params = foo1.Parameters
                Assert.Equal(0, foo1Params.Length)
                Dim foo2 = type3.GetMembers("Foo2").OfType(Of MethodSymbol)().Single()
                Assert.NotEqual(localC3Foo2, foo2)
                Assert.Same(localC3Foo2, (DirectCast(foo2, RetargetingMethodSymbol)).UnderlyingMethod)
                Assert.Equal(1, ((DirectCast(foo2, RetargetingMethodSymbol)).Locations).Length)
                Dim foo2Params = foo2.Parameters
                Assert.Equal(4, foo2Params.Length)
                Assert.Same(localC3Foo2.Parameters(0), (DirectCast(foo2Params(0), RetargetingParameterSymbol)).UnderlyingParameter)
                Assert.Same(localC3Foo2.Parameters(1), (DirectCast(foo2Params(1), RetargetingParameterSymbol)).UnderlyingParameter)
                Assert.Same(localC3Foo2.Parameters(2), (DirectCast(foo2Params(2), RetargetingParameterSymbol)).UnderlyingParameter)
                Assert.Same(localC3Foo2.Parameters(3), (DirectCast(foo2Params(3), RetargetingParameterSymbol)).UnderlyingParameter)
                Dim x1 = foo2Params(0)
                Dim x2 = foo2Params(1)
                Dim x3 = foo2Params(2)
                Dim x4 = foo2Params(3)
                Assert.Equal("x1", x1.Name)
                Assert.NotEqual(localC3Foo2.Parameters(0).[Type], x1.[Type])
                Assert.Equal(localC3Foo2.Parameters(0).ToTestDisplayString(), x1.ToTestDisplayString())
                Assert.Same(asm5(1), x1.ContainingAssembly)
                Assert.Same(foo2, x1.ContainingSymbol)
                Assert.[False](x1.HasExplicitDefaultValue)
                Assert.[False](x1.IsOptional)
                Assert.True(x1.IsByRef)
                Assert.Equal(2, (DirectCast(x1.[Type], ArrayTypeSymbol)).Rank)
                Assert.Equal("x2", x2.Name)
                Assert.NotEqual(localC3Foo2.Parameters(1).[Type], x2.[Type])
                Assert.True(x2.IsByRef)
                Assert.Equal("x3", x3.Name)
                Assert.Same(localC3Foo2.Parameters(2).[Type], x3.[Type])
                Assert.Equal("x4", x4.Name)

                Assert.True(x4.HasExplicitDefaultValue)
                Assert.[True](x4.IsOptional)
                Assert.Equal("Foo2", foo2.Name)
                Assert.Equal(localC3Foo2.ToTestDisplayString(), foo2.ToTestDisplayString())
                Assert.Same(asm5(1), foo2.ContainingAssembly)
                Assert.Same(type3, foo2.ContainingSymbol)
                Assert.Equal(Accessibility.[Public], foo2.DeclaredAccessibility)
                Assert.False(foo2.IsOverloads)
                Assert.[False](foo2.IsMustOverride)
                Assert.[False](foo2.IsExternalMethod)
                Assert.[False](foo2.IsGenericMethod)
                Assert.[False](foo2.IsOverrides)
                Assert.[False](foo2.IsNotOverridable)
                Assert.[False](foo2.IsShared)
                Assert.[False](foo2.IsVararg)
                Assert.[False](foo2.IsOverridable)
                Assert.[True](foo2.IsSub)
                Assert.Equal(0, foo2.TypeParameters.Length)
                Assert.Equal(0, foo2.TypeArguments.Length)
                Assert.[True](bar.IsShared)
                Assert.[False](bar.IsSub)
                Dim foo3 = type3.GetMembers("Foo3").OfType(Of MethodSymbol)().Single()
                Assert.Equal(Accessibility.Friend, foo3.DeclaredAccessibility)
                Assert.[True](foo3.IsGenericMethod)
                Assert.[True](foo3.IsOverridable)
                Dim foo3TypeParams = foo3.TypeParameters
                Assert.Equal(1, foo3TypeParams.Length)
                Assert.Equal(1, foo3.TypeArguments.Length)
                Assert.Same(foo3TypeParams(0), foo3.TypeArguments(0))
                Dim typeC301 = type3.GetTypeMembers("C301").Single()
                Dim typeC302 = type3.GetTypeMembers("C302").Single()
                Dim typeC6 = asm5(1).GlobalNamespace.GetTypeMembers("C6").Single()
                Assert.Equal(typeC301.ToTestDisplayString(), asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetTypeMembers("C301").Single().ToTestDisplayString())
                Assert.Equal(typeC6.ToTestDisplayString(), asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToTestDisplayString())
                Assert.Equal(typeC301.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat), asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetTypeMembers("C301").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))
                Assert.Equal(typeC6.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat), asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))
                Assert.Equal(type3.GetMembers().Length, asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetMembers().Length)
                Assert.Equal(type3.GetTypeMembers().Length(), asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetTypeMembers().Length())
                Assert.Same(typeC301, type3.GetTypeMembers("C301", 0).Single())
                Assert.Equal(0, type3.Arity)
                Assert.Equal(1, typeC6.Arity)
                Assert.NotNull(type3.BaseType)
                Assert.Equal("System.Object", type3.BaseType.ToTestDisplayString())
                Assert.Equal(Accessibility.[Public], type3.DeclaredAccessibility)
                Assert.Equal(Accessibility.Friend, typeC302.DeclaredAccessibility)
                Assert.Equal(0, type3.Interfaces.Length)
                Assert.Equal(1, typeC301.Interfaces.Length)
                Assert.Equal("I1", typeC301.Interfaces.Single().Name)
                Assert.[False](type3.IsMustInherit)
                Assert.[True](typeC301.IsMustInherit)
                Assert.[False](type3.IsNotInheritable)
                Assert.[False](type3.IsShared)
                Assert.Equal(0, type3.TypeArguments.Length)
                Assert.Equal(0, type3.TypeParameters.Length)
                Dim localC6Params = typeC6.TypeParameters
                Assert.Equal(1, localC6Params.Length)
                Assert.Equal(1, typeC6.TypeArguments.Length)
                Assert.Same(localC6Params(0), typeC6.TypeArguments(0))
                Assert.Same((DirectCast(type3, RetargetingNamedTypeSymbol)).UnderlyingNamedType, asm3.GlobalNamespace.GetTypeMembers("C3").Single())
                Assert.Equal(1, ((DirectCast(type3, RetargetingNamedTypeSymbol)).Locations).Length)
                Assert.Equal(TypeKind.[Class], type3.TypeKind)
                Assert.Equal(TypeKind.[Interface], asm5(1).GlobalNamespace.GetTypeMembers("I1").Single().TypeKind)
                Dim localC6_T = localC6Params(0)
                Dim foo3TypeParam = foo3TypeParams(0)
                Assert.Equal(0, localC6_T.ConstraintTypes.Length)
                Assert.Equal(1, foo3TypeParam.ConstraintTypes.Length)
                Assert.Same(type4, foo3TypeParam.ConstraintTypes(0))
                Assert.Same(typeC6, localC6_T.ContainingSymbol)
                Assert.[False](foo3TypeParam.HasConstructorConstraint)
                Assert.[True](localC6_T.HasConstructorConstraint)
                Assert.[False](foo3TypeParam.HasReferenceTypeConstraint)
                Assert.[False](foo3TypeParam.HasValueTypeConstraint)
                Assert.Equal("TFoo3", foo3TypeParam.Name)
                Assert.Equal("T", localC6_T.Name)
                Assert.Equal(0, foo3TypeParam.Ordinal)
                Assert.Equal(0, localC6_T.Ordinal)
                Assert.Equal(VarianceKind.None, foo3TypeParam.Variance)
                Assert.Same((DirectCast(localC6_T, RetargetingTypeParameterSymbol)).UnderlyingTypeParameter, asm3.GlobalNamespace.GetTypeMembers("C6").Single().TypeParameters(0))
                Dim ns1 = asm5(1).GlobalNamespace.GetMembers("ns1").OfType(Of NamespaceSymbol)().Single()
                Dim ns2 = ns1.GetMembers("ns2").OfType(Of NamespaceSymbol)().Single()
                Assert.Equal("ns1.ns2", ns2.ToTestDisplayString())
                Assert.Equal(2, ns1.GetMembers().Length)
                Assert.Equal(1, ns1.GetTypeMembers().Length())
                Assert.Same(ns1.GetTypeMembers("C304").Single(), ns1.GetTypeMembers("C304", 0).Single())
                Assert.Same(asm5(1).Modules(0), asm5(1).Modules(0).GlobalNamespace.ContainingSymbol)
                Assert.Same(asm5(1).Modules(0).GlobalNamespace, ns1.ContainingSymbol)
                Assert.Same(asm5(1).Modules(0), ns1.Extent.[Module])
                Assert.Equal(1, ns1.ConstituentNamespaces.Length)
                Assert.Same(ns1, ns1.ConstituentNamespaces(0))
                Assert.[False](ns1.IsGlobalNamespace)
                Assert.Equal(DirectCast(ns2, RetargetingNamespaceSymbol).DeclaredAccessibilityOfMostAccessibleDescendantType, ns2.DeclaredAccessibilityOfMostAccessibleDescendantType)
                Assert.[True](asm5(1).Modules(0).GlobalNamespace.IsGlobalNamespace)
                Assert.Same(asm3.Modules(0).GlobalNamespace, (DirectCast(asm5(1).Modules(0).GlobalNamespace, RetargetingNamespaceSymbol)).UnderlyingNamespace)
                Assert.Same(asm3.Modules(0).GlobalNamespace.GetMembers("ns1").Single(), (DirectCast(ns1, RetargetingNamespaceSymbol)).UnderlyingNamespace)
                Dim module3 = DirectCast(asm5(1).Modules(0), RetargetingModuleSymbol)
                Assert.Equal("C3.exe", module3.ToTestDisplayString())
                Assert.Equal("C3.exe", module3.Name)
                Assert.Same(asm5(1), module3.ContainingSymbol)
                Assert.Same(asm5(1), module3.ContainingAssembly)
                Assert.Null(module3.ContainingType)
                Dim retval5 = type3.GetMembers("Foo4").OfType(Of MethodSymbol)().Single().ReturnType
                Assert.Equal("C8(Of C4)", retval5.ToTestDisplayString())
                Dim typeC5 = c5.[Assembly].GlobalNamespace.GetTypeMembers("C5").Single()
                Assert.Same(asm5(1), typeC5.BaseType.ContainingAssembly)
                Assert.Equal("ns1.C304.C305", typeC5.BaseType.ToTestDisplayString())
                Assert.NotEqual(SymbolKind.ErrorType, typeC5.Kind)

            End Using

        End Sub

        <Fact()>
        Public Sub MultiTargeting5()
            Using MetadataCache.LockAndClean()
                Dim c1_Name = New AssemblyIdentity("c1")

                Dim compilationDef =
    <compilation name="Dummy">
        <file name="Dummy.vb">
class Module1

    Function M1() As Class4
    End Function

    Function M2() As Class4.Class4_1
    End Function

    Function M3() As Class4
    End Function
End Class
        </file>
    </compilation>

                Dim refs = New List(Of MetadataReference)()
                refs.Add(TestReferences.SymbolsTests.V1.MTTestLib1.dll)
                refs.Add(TestReferences.SymbolsTests.V1.MTTestModule2.netmodule)

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib(compilationDef)
                c1 = c1.AddReferences(refs)

                Dim c2_Name = New AssemblyIdentity("MTTestLib2")
                Dim c2 = CreateCompilation(c2_Name,
                                           Nothing,
                                           {TestReferences.NetFx.v4_0_30319.mscorlib,
                                            TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                            New VisualBasicCompilationReference(c1)})

                Dim c1AsmSource As SourceAssemblySymbol = DirectCast(c1.[Assembly], SourceAssemblySymbol)
                Dim Lib1_V1 As PEAssemblySymbol = DirectCast(c1AsmSource.Modules(0).GetReferencedAssemblySymbols()(1), PEAssemblySymbol)
                Dim module1 As PEModuleSymbol = DirectCast(c1AsmSource.Modules(1), PEModuleSymbol)
                Dim c2AsmSource As SourceAssemblySymbol = DirectCast(c2.[Assembly], SourceAssemblySymbol)
                Dim c1AsmRef As RetargetingAssemblySymbol = DirectCast(c2AsmSource.Modules(0).GetReferencedAssemblySymbols()(2), RetargetingAssemblySymbol)
                Dim Lib1_V2 As PEAssemblySymbol = DirectCast(c2AsmSource.Modules(0).GetReferencedAssemblySymbols()(1), PEAssemblySymbol)
                Dim module2 As PEModuleSymbol = DirectCast(c1AsmRef.Modules(1), PEModuleSymbol)
                Assert.Equal(1, Lib1_V1.Identity.Version.Major)
                Assert.Equal(2, Lib1_V2.Identity.Version.Major)
                Assert.NotEqual(module1, module2)
                Assert.Same(module1.[Module], module2.[Module])
                Dim classModule1 As NamedTypeSymbol = c1AsmRef.Modules(0).GlobalNamespace.GetTypeMembers("Module1").Single()
                Dim m1 As MethodSymbol = classModule1.GetMembers("M1").OfType(Of MethodSymbol)().Single()
                Dim m2 As MethodSymbol = classModule1.GetMembers("M2").OfType(Of MethodSymbol)().Single()
                Dim m3 As MethodSymbol = classModule1.GetMembers("M3").OfType(Of MethodSymbol)().Single()
                Assert.Same(module2, m1.ReturnType.ContainingModule)
                Assert.Same(module2, m2.ReturnType.ContainingModule)
                Assert.Same(module2, m3.ReturnType.ContainingModule)
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
                Dim c1 = VisualBasicCompilation.Create("Test", references:={mscorlibRef, module1Ref})

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
                Dim c1 = VisualBasicCompilation.Create("Test", references:={mscorlibRef, module1Ref})

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
                Dim c1 = VisualBasicCompilation.Create("Test", references:={mscorlibRef, module1Ref})

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
                Dim c1 = VisualBasicCompilation.Create("Test1", references:={mscorlibRef, module1Ref})
                Dim c2 = VisualBasicCompilation.Create("Test2", references:={mscorlibRef, module1Ref, New VisualBasicCompilationReference(c1)})

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
                Dim c1 = VisualBasicCompilation.Create("Test1", references:={mscorlibRef, module1Ref})
                Dim c2 = VisualBasicCompilation.Create("Test2", references:={mscorlibRef, module1Ref, c1.ToMetadataReference()})

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

            Dim c1 As VisualBasicCompilation
            Dim c2 As VisualBasicCompilation

            c1 = VisualBasicCompilation.Create("Test1", references:={mscorlibRef, MTTestLib1V1Ref})

            Dim c1Ref = c1.ToMetadataReference()

            c2 = VisualBasicCompilation.Create("Test2", references:={mscorlibRef, MTTestLib1V2Ref, c1Ref})


            Dim asm2 = c2.GetReferencedAssemblySymbol(mscorlibRef)
            Dim asm1 = c1.GetReferencedAssemblySymbol(mscorlibRef)

            Assert.Same(asm2, asm1)

            GC.KeepAlive(c1)
            GC.KeepAlive(c2)
        End Sub


        ' Very simplistic test if a compilation has a single type with the given full name. Does NOT handle generics.
        Private Function HasSingleTypeOfKind(c As VisualBasicCompilation, kind As TypeKind, fullName As String) As Boolean
            Dim names As String() = fullName.Split("."c)

            Dim current As NamespaceOrTypeSymbol = c.GlobalNamespace
            For Each name In names
                Dim matchingSym = current.GetMembers(name)
                If (matchingSym.Length() <> 1) Then
                    Return False
                End If
                current = DirectCast(matchingSym.First(), NamespaceOrTypeSymbol)
            Next

            Return (TypeOf current Is TypeSymbol AndAlso DirectCast(current, TypeSymbol).TypeKind = kind)
        End Function

        <Fact()>
        Public Sub AddRemoveReferences()
            Dim mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib
            Dim systemCoreRef = TestReferences.NetFx.v4_0_30319.System_Core
            Dim systemRef = TestReferences.NetFx.v4_0_30319.System

            Dim c = VisualBasicCompilation.Create("Test")
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Structure, "System.Int32"))
            c = c.AddReferences(mscorlibRef)
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Structure, "System.Int32"))
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"))
            c = c.AddReferences(systemCoreRef)
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"))
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"))
            c = c.ReplaceReference(systemCoreRef, systemRef)
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"))
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"))
            c = c.RemoveReferences(systemRef)
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"))
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Structure, "System.Int32"))
            c = c.RemoveReferences(mscorlibRef)
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Structure, "System.Int32"))
        End Sub

        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Function CreateCompilation(name As String, ParamArray dependencies As Object()) As VisualBasicCompilation
            Dim result = VisualBasicCompilation.Create(name, references:=CreateMetadataReferences(dependencies))
            Dim asm = result.Assembly
            GC.KeepAlive(asm)
            Return result
        End Function

        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Function CreateWeakCompilation(name As String, ParamArray dependencies As Object()) As ObjectReference
            ' this compilation should only be reachable via the WeakReference we return
            Dim result = VisualBasicCompilation.Create(name, references:=CreateMetadataReferences(dependencies))
            Dim asm = result.Assembly
            GC.KeepAlive(asm)
            Return New ObjectReference(result)
        End Function


#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>       
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache1()
#Else
        <Fact()>
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Private Sub CompactRetargetingCache1()
#End If
            Dim mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
            Dim V1MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path
            Dim V2MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path

            ' Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            Dim c1 = VisualBasicCompilation.Create("Test1", references:=CreateMetadataReferences(
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
            Dim mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path
            Dim mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path
            Dim libV1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path
            Dim libV2 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path

            ' Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            Dim c1 = VisualBasicCompilation.Create("Test1", references:=CreateMetadataReferences(
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
        End Sub

        <Fact()>
        Public Sub ReferencesVersioning()
            Dim dir1 = Temp.CreateDirectory()
            Dim dir2 = Temp.CreateDirectory()
            Dim dir3 = Temp.CreateDirectory()
            Dim file1 = dir1.CreateFile("C.dll")
            Dim file2 = dir2.CreateFile("C.dll")
            Dim file3 = dir3.CreateFile("main.dll")
            file1.WriteAllBytes(TestResources.SymbolsTests.General.C1)
            file2.WriteAllBytes(TestResources.SymbolsTests.General.C2)

            Dim b = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="b">
    <file name="b.vb">
Public Class B
    Public Shared Function Main() As Integer
        Return C.Main()
    End Function
End Class
    </file>
</compilation>,
                references:={New MetadataImageReference(TestResources.SymbolsTests.General.C2.AsImmutableOrNull())},
                options:=OptionsDll
            )

            file3.WriteAllBytes(b.EmitToArray())

            Dim a = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="a">
    <file name="a.vb">
Class A
        Public Shared Sub Main()
            B.Main()
        End Sub
End Class
    </file>
</compilation>,
                references:={New MetadataFileReference(file1.Path), New MetadataFileReference(file2.Path), New MetadataFileReference(file3.Path)},
                options:=OptionsDll
            )

            Using stream = New MemoryStream()
                a.Emit(stream)
            End Using
        End Sub

        <Fact()>
        Public Sub SyntaxTreeOrderConstruct()
            Dim tree1 = CreateSyntaxTree("A")
            Dim tree2 = CreateSyntaxTree("B")

            Dim treeOrder1 = {tree1, tree2}
            Dim compilation1 = VisualBasicCompilation.Create("Compilation1", syntaxTrees:=treeOrder1)
            CheckCompilationSyntaxTrees(compilation1, treeOrder1)

            Dim treeOrder2 = {tree2, tree1}
            Dim compilation2 = VisualBasicCompilation.Create("Compilation2", syntaxTrees:=treeOrder2)
            CheckCompilationSyntaxTrees(compilation2, treeOrder2)
        End Sub

        <Fact()>
        Public Sub SyntaxTreeOrderAdd()
            Dim tree1 = CreateSyntaxTree("A")
            Dim tree2 = CreateSyntaxTree("B")
            Dim tree3 = CreateSyntaxTree("C")
            Dim tree4 = CreateSyntaxTree("D")

            Dim treeList1 = {tree1, tree2}
            Dim compilation1 = VisualBasicCompilation.Create("Compilation1", syntaxTrees:=treeList1)
            CheckCompilationSyntaxTrees(compilation1, treeList1)

            Dim treeList2 = {tree3, tree4}
            Dim compilation2 = compilation1.AddSyntaxTrees(treeList2)
            CheckCompilationSyntaxTrees(compilation1, treeList1) ' compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, treeList1.Concat(treeList2).ToArray())

            Dim treeList3 = {tree4, tree3}
            Dim compilation3 = VisualBasicCompilation.Create("Compilation3", syntaxTrees:=treeList3)
            CheckCompilationSyntaxTrees(compilation3, treeList3)

            Dim treeList4 = {tree2, tree1}
            Dim compilation4 = compilation3.AddSyntaxTrees(treeList4)
            CheckCompilationSyntaxTrees(compilation3, treeList3) ' compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, treeList3.Concat(treeList4).ToArray())
        End Sub

        <Fact()>
        Public Sub SyntaxTreeOrderRemove()
            Dim tree1 = CreateSyntaxTree("A")
            Dim tree2 = CreateSyntaxTree("B")
            Dim tree3 = CreateSyntaxTree("C")
            Dim tree4 = CreateSyntaxTree("D")

            Dim treeList1 = {tree1, tree2, tree3, tree4}
            Dim compilation1 = VisualBasicCompilation.Create("Compilation1", syntaxTrees:=treeList1)
            CheckCompilationSyntaxTrees(compilation1, treeList1)

            Dim treeList2 = {tree3, tree1}
            Dim compilation2 = compilation1.RemoveSyntaxTrees(treeList2)
            CheckCompilationSyntaxTrees(compilation1, treeList1) ' compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree2, tree4)

            Dim treeList3 = {tree4, tree3, tree2, tree1}
            Dim compilation3 = VisualBasicCompilation.Create("Compilation3", syntaxTrees:=treeList3)
            CheckCompilationSyntaxTrees(compilation3, treeList3)

            Dim treeList4 = {tree3, tree1}
            Dim compilation4 = compilation3.RemoveSyntaxTrees(treeList4)
            CheckCompilationSyntaxTrees(compilation3, treeList3) ' compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree4, tree2)
        End Sub

        <Fact()>
        Public Sub SyntaxTreeOrderReplace()
            Dim tree1 = CreateSyntaxTree("A")
            Dim tree2 = CreateSyntaxTree("B")
            Dim tree3 = CreateSyntaxTree("C")

            Dim treeList1 = {tree1, tree2}
            Dim compilation1 = VisualBasicCompilation.Create("Compilation1", syntaxTrees:=treeList1)
            CheckCompilationSyntaxTrees(compilation1, treeList1)

            Dim compilation2 = compilation1.ReplaceSyntaxTree(tree1, tree3)
            CheckCompilationSyntaxTrees(compilation1, treeList1) ' compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree3, tree2)

            Dim treeList3 = {tree2, tree1}
            Dim compilation3 = VisualBasicCompilation.Create("Compilation3", syntaxTrees:=treeList3)
            CheckCompilationSyntaxTrees(compilation3, treeList3)

            Dim compilation4 = compilation3.ReplaceSyntaxTree(tree1, tree3)
            CheckCompilationSyntaxTrees(compilation3, treeList3) ' compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree2, tree3)
        End Sub

        <WorkItem(578706)>
        <Fact>
        Public Sub DeclaringCompilationOfAddedModule()
            Dim source1 =
                <compilation name="Lib1">
                    <file name="a.vb">
Class C1
End Class
                    </file>
                </compilation>

            Dim source2 =
                <compilation name="Lib2">
                    <file name="a.vb">
Class C2
End Class
                    </file>
                </compilation>

            Dim lib1 = CreateCompilationWithMscorlib(source1, OutputKind.NetModule)
            Dim ref1 = lib1.EmitToImageReference()

            Dim lib2 = CreateCompilationWithMscorlibAndReferences(source2, {ref1})
            lib2.VerifyDiagnostics()

            Dim sourceAssembly = lib2.Assembly
            Dim sourceModule = sourceAssembly.Modules(0)
            Dim sourceType = sourceModule.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C2")

            Assert.IsType(Of SourceAssemblySymbol)(sourceAssembly)
            Assert.Equal(lib2, sourceAssembly.DeclaringCompilation)

            Assert.IsType(Of SourceModuleSymbol)(sourceModule)
            Assert.Equal(lib2, sourceModule.DeclaringCompilation)

            Assert.IsType(Of SourceNamedTypeSymbol)(sourceType)
            Assert.Equal(lib2, sourceType.DeclaringCompilation)


            Dim addedModule = sourceAssembly.Modules(1)
            Dim addedModuleAssembly = addedModule.ContainingAssembly
            Dim addedModuleType = addedModule.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C1")

            Assert.IsType(Of SourceAssemblySymbol)(addedModuleAssembly)
            Assert.Equal(lib2, addedModuleAssembly.DeclaringCompilation) ' NB: not lib1, not null

            Assert.IsType(Of PEModuleSymbol)(addedModule)
            Assert.Null(addedModule.DeclaringCompilation)

            Assert.IsType(Of PENamedTypeSymbol)(addedModuleType)
            Assert.Null(addedModuleType.DeclaringCompilation)
        End Sub

        Private Shared Function CreateSyntaxTree(className As String) As SyntaxTree
            Dim text = String.Format("Public Partial Class {0}{1}End Class", className, Environment.NewLine)
            Dim path = String.Format("{0}.vb", className)
            Return VisualBasicSyntaxTree.ParseText(text, path)
        End Function

        Private Shared Sub CheckCompilationSyntaxTrees(compilation As VisualBasicCompilation, ParamArray expectedSyntaxTrees As SyntaxTree())
            Dim actualSyntaxTrees = compilation.SyntaxTrees

            Dim numTrees = expectedSyntaxTrees.Length

            Assert.Equal(numTrees, actualSyntaxTrees.Length)
            For i = 0 To numTrees - 1
                Assert.Equal(expectedSyntaxTrees(i), actualSyntaxTrees(i))
            Next

            For i = 0 To numTrees - 1
                For j = 0 To numTrees - 1
                    Assert.Equal(Math.Sign(compilation.CompareSyntaxTreeOrdering(expectedSyntaxTrees(i), expectedSyntaxTrees(j))), Math.Sign(i.CompareTo(j)))
                Next
            Next

            Dim types = expectedSyntaxTrees.Select(Function(tree) compilation.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetCompilationUnitRoot().Members.Single())).ToArray()
            For i = 0 To numTrees - 1
                For j = 0 To numTrees - 1
                    Assert.Equal(Math.Sign(compilation.CompareSourceLocations(types(i).Locations(0), types(j).Locations(0))), Math.Sign(i.CompareTo(j)))
                Next
            Next
        End Sub
    End Class
End Namespace
