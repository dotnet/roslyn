' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace CompilationCreationTestHelpers
    Friend Module Helpers
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
        Public Sub CorLibTypes()
            Dim mdTestLib1 = TestReferences.SymbolsTests.MDTestLib1

            Dim c1 = VisualBasicCompilation.Create("Test", references:={MscorlibRef_v4_0_30316_17626, mdTestLib1})

            Dim c107 As TypeSymbol = c1.GlobalNamespace.GetTypeMembers("C107").Single()

            Assert.Equal(SpecialType.None, c107.SpecialType)

            For i As Integer = 1 To SpecialType.Count Step 1
                Dim type As NamedTypeSymbol = c1.Assembly.GetSpecialType(CType(i, SpecialType))

                If i = SpecialType.System_Runtime_CompilerServices_RuntimeFeature OrElse
                   i = SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute OrElse
                   i = SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute Then
                    Assert.Equal(type.Kind, SymbolKind.ErrorType) ' Not available
                Else
                    Assert.NotEqual(type.Kind, SymbolKind.ErrorType)
                End If

                Assert.Equal(CType(i, SpecialType), type.SpecialType)
            Next

            Assert.Equal(SpecialType.None, c107.SpecialType)

            Dim arrayOfc107 = ArrayTypeSymbol.CreateVBArray(c107, Nothing, 1, c1)

            Assert.Equal(SpecialType.None, arrayOfc107.SpecialType)

            Dim c2 = VisualBasicCompilation.Create("Test", references:={mdTestLib1})

            Assert.Equal(SpecialType.None, c2.GlobalNamespace.GetTypeMembers("C107").Single().SpecialType)
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

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, DefaultVbReferences, TestOptions.ReleaseDll.WithRootNamespace("A.B.C"))

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("C", root.Name)
            Assert.Equal("B", root.ContainingNamespace.Name)
            Assert.Equal("A", root.ContainingNamespace.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/16885")>
        <WorkItem(16885, "https://github.com/dotnet/roslyn/issues/16885")>
        Public Sub RootNamespace_NoTrees_SuppressEmbeddedDeclarations()

            Dim c1 = VisualBasicCompilation.Create("Test", {}, options:=TestOptions.ReleaseDll.WithRootNamespace("A.B.C").WithSuppressEmbeddedDeclarations(True))

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

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, DefaultVbReferences, TestOptions.ReleaseDll)

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)

            c1 = c1.WithOptions(TestOptions.ReleaseDll.WithRootNamespace("A.B"))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("B", root.Name)
            Assert.Equal("A", root.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)

            c1 = c1.WithOptions(TestOptions.ReleaseDll.WithRootNamespace(""))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)
        End Sub

        <Fact()>
        Public Sub RootNamespace_NoFiles_UpdateCompilation()
            Dim c1 = VisualBasicCompilation.Create("Test", references:=DefaultVbReferences, options:=TestOptions.ReleaseDll)

            Dim root As NamespaceSymbol = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("", root.Name)
            Assert.True(root.IsGlobalNamespace)

            c1 = c1.WithOptions(TestOptions.ReleaseDll.WithRootNamespace("A.B"))

            root = c1.RootNamespace
            Assert.NotNull(root)
            Assert.Equal("B", root.Name)
            Assert.Equal("A", root.ContainingNamespace.Name)
            Assert.False(root.IsGlobalNamespace)

            c1 = c1.WithOptions(TestOptions.ReleaseDll.WithRootNamespace(""))

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

        <WorkItem(753078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/753078")>
        <Fact()>
        Public Sub RootNamespaceUpdateViaChangeInCompilationOptions()
            Dim sourceTree = ParserTestUtilities.Parse(
<text>
Namespace InSource
End Namespace
</text>.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", {sourceTree}, options:=TestOptions.ReleaseDll.WithRootNamespace("FromOptions"))
            VerifyNamespaceShape(c1)
            c1 = VisualBasicCompilation.Create("Test", {sourceTree}, options:=TestOptions.ReleaseDll)
            c1 = c1.WithOptions(c1.Options.WithRootNamespace("FromOptions"))
            VerifyNamespaceShape(c1)
        End Sub

        <Fact()>
        Public Sub CyclicReference()
            Dim mscorlibRef = NetFramework.mscorlib
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
                Net40.References.mscorlib,
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
                Net40.References.mscorlib,
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
                Net40.References.mscorlib,
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
            Net40.References.mscorlib,
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
                Net40.References.mscorlib,
                TestReferences.SymbolsTests.V2.MTTestLib3.dll
            })

            Assert.Same(asm5(0), asm1(0))
            Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
            Dim asm6 = GetSymbolsForReferences(
            {
                Net40.References.mscorlib,
                TestReferences.SymbolsTests.V1.MTTestLib2.dll
            })

            Assert.Same(asm6(0), asm1(0))
            Assert.Same(asm6(1), asm1(1))
            Dim asm7 = GetSymbolsForReferences(
            {
            Net40.References.mscorlib,
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
            Net40.References.mscorlib,
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
                Net40.References.mscorlib,
                TestReferences.SymbolsTests.V3.MTTestLib4.dll
            })

            Assert.Same(asm9(0), asm1(0))
            Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
            Dim asm10 = GetSymbolsForReferences(
            {
            Net40.References.mscorlib,
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

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting2()
#Else
        <Fact()>
        Public Sub MultiTargeting2()
#End If
            Dim varMTTestLib1_V1_Name = New AssemblyIdentity("MTTestLib1", New Version("1.0.0.0"))
            Dim varC_MTTestLib1_V1 = CreateEmptyCompilation(varMTTestLib1_V1_Name, New String() {<text>
Public Class Class1

End Class
                </text>.Value}, {NetFramework.mscorlib})

            Dim asm_MTTestLib1_V1 = varC_MTTestLib1_V1.SourceAssembly().BoundReferences()
            Dim varMTTestLib2_Name = New AssemblyIdentity("MTTestLib2")
            Dim varC_MTTestLib2 = CreateEmptyCompilation(varMTTestLib2_Name, New String() {<text>
Public Class Class4
    Function Foo() As Class1
        Return Nothing
    End Function

    Public Bar As Class1

End Class
                </text>.Value}, {NetFramework.mscorlib, varC_MTTestLib1_V1.ToMetadataReference()})

            Dim asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences()
            Assert.Same(asm_MTTestLib2(0), asm_MTTestLib1_V1(0))
            Assert.Same(asm_MTTestLib2(1), varC_MTTestLib1_V1.SourceAssembly())

            Dim c2 = CreateEmptyCompilation(New AssemblyIdentity("c2"), Nothing,
                                       {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib1_V1.ToMetadataReference()})

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
            Dim varC_MTTestLib1_V2 = CreateEmptyCompilation(varMTTestLib1_V2_Name, New String() {<text>
Public Class Class1

End Class

Public Class Class2

End Class
                </text>.Value}, {NetFramework.mscorlib})
            Dim asm_MTTestLib1_V2 = varC_MTTestLib1_V2.SourceAssembly().BoundReferences()
            Dim varMTTestLib3_Name = New AssemblyIdentity("MTTestLib3")
            Dim varC_MTTestLib3 = CreateEmptyCompilation(varMTTestLib3_Name, New String() {<text>
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
                </text>.Value}, {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib1_V2.ToMetadataReference()})
            Dim asm_MTTestLib3 = varC_MTTestLib3.SourceAssembly().BoundReferences()
            Assert.Same(asm_MTTestLib3(0), asm_MTTestLib1_V1(0))
            Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
            Assert.NotSame(asm_MTTestLib3(2), varC_MTTestLib1_V1.SourceAssembly())
            Dim c3 = CreateEmptyCompilation(New AssemblyIdentity("c3"), Nothing, {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib1_V2.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference()})
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
            Dim varC_MTTestLib1_V3 = CreateEmptyCompilation(varMTTestLib1_V3_Name, New String() {<text>
Public Class Class1

End Class

Public Class Class2

End Class

Public Class Class3

End Class
                </text>.Value}, {NetFramework.mscorlib})
            Dim asm_MTTestLib1_V3 = varC_MTTestLib1_V3.SourceAssembly().BoundReferences()
            Dim varMTTestLib4_Name = New AssemblyIdentity("MTTestLib4")
            Dim varC_MTTestLib4 = CreateEmptyCompilation(varMTTestLib4_Name, New String() {<text>
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
                </text>.Value}, {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib1_V3.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference()})

            Dim asm_MTTestLib4 = varC_MTTestLib4.SourceAssembly().BoundReferences()
            Assert.Same(asm_MTTestLib4(0), asm_MTTestLib1_V1(0))
            Assert.NotSame(asm_MTTestLib4(1), varC_MTTestLib2.SourceAssembly())
            Assert.Same(asm_MTTestLib4(2), varC_MTTestLib1_V3.SourceAssembly())
            Assert.NotSame(asm_MTTestLib4(3), varC_MTTestLib3.SourceAssembly())

            Dim c4 = CreateEmptyCompilation(New AssemblyIdentity("c4"),
                                       Nothing,
                                       {NetFramework.mscorlib,
                                       varC_MTTestLib2.ToMetadataReference(),
                                       varC_MTTestLib1_V3.ToMetadataReference(),
                                       varC_MTTestLib3.ToMetadataReference(),
                                       varC_MTTestLib4.ToMetadataReference()})

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
            Dim c5 = CreateEmptyCompilation(New AssemblyIdentity("c5"), Nothing, {NetFramework.mscorlib, varC_MTTestLib3.ToMetadataReference()})
            Dim asm5 = c5.SourceAssembly().BoundReferences()
            Assert.Same(asm5(0), asm2(0))
            Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))
            Dim c6 = CreateEmptyCompilation(New AssemblyIdentity("c6"), Nothing, {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference()})
            Dim asm6 = c6.SourceAssembly().BoundReferences()
            Assert.Same(asm6(0), asm2(0))
            Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))
            Dim c7 = CreateEmptyCompilation(New AssemblyIdentity("c6"), Nothing, {NetFramework.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference(), varC_MTTestLib4.ToMetadataReference()})
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
            Dim c8 = CreateEmptyCompilation(New AssemblyIdentity("c8"), Nothing,
                                       {NetFramework.mscorlib,
                                       varC_MTTestLib4.ToMetadataReference(),
                                       varC_MTTestLib2.ToMetadataReference(),
                                       varC_MTTestLib3.ToMetadataReference()})

            Dim asm8 = c8.SourceAssembly().BoundReferences()
            Assert.Same(asm8(0), asm2(0))
            Assert.True(asm8(2).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(1)))
            Assert.Same(asm8(2), asm7(1))
            Assert.True(asm8(3).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(3)))
            Assert.Same(asm8(3), asm7(2))
            Assert.True(asm8(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))
            Assert.Same(asm8(1), asm7(3))
            Dim c9 = CreateEmptyCompilation(New AssemblyIdentity("c9"), Nothing, {NetFramework.mscorlib, varC_MTTestLib4.ToMetadataReference()})
            Dim asm9 = c9.SourceAssembly().BoundReferences()
            Assert.Same(asm9(0), asm2(0))
            Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))

            Dim c10 = CreateEmptyCompilation(New AssemblyIdentity("c10"), Nothing,
                                        {NetFramework.mscorlib,
                                        varC_MTTestLib2.ToMetadataReference(),
                                        varC_MTTestLib1_V3.ToMetadataReference(),
                                        varC_MTTestLib3.ToMetadataReference(),
                                        varC_MTTestLib4.ToMetadataReference()})

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
        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting3()
#Else
        <Fact()>
        Public Sub MultiTargeting3()
#End If
            Dim varMTTestLib2_Name = New AssemblyIdentity("MTTestLib2")
            Dim varC_MTTestLib2 = CreateEmptyCompilation(varMTTestLib2_Name,
                                                    Nothing,
                                                    {NetFramework.mscorlib,
                                                    TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                                    TestReferences.SymbolsTests.V1.MTTestModule2.netmodule})

            Dim asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences()
            Dim c2 = CreateEmptyCompilation(New AssemblyIdentity("c2"),
                                       Nothing,
                                       {NetFramework.mscorlib,
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

            Dim varC_MTTestLib3 = CreateEmptyCompilation(varMTTestLib3_Name,
                                                    Nothing,
                                                    {NetFramework.mscorlib,
                                                    TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                                    New VisualBasicCompilationReference(varC_MTTestLib2),
                                                    TestReferences.SymbolsTests.V2.MTTestModule3.netmodule})

            Dim asm_MTTestLib3Prime = varC_MTTestLib3.SourceAssembly().BoundReferences()
            Dim asm_MTTestLib3 = New AssemblySymbol() {asm_MTTestLib3Prime(0), asm_MTTestLib3Prime(2), asm_MTTestLib3Prime(1)}
            Assert.Same(asm_MTTestLib3(0), asm_MTTestLib2(0))
            Assert.NotSame(asm_MTTestLib3(1), varC_MTTestLib2.SourceAssembly())
            Assert.NotSame(asm_MTTestLib3(2), asm_MTTestLib2(1))

            Dim c3 = CreateEmptyCompilation(New AssemblyIdentity("c3"),
                                       Nothing,
                                       {NetFramework.mscorlib,
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

            Dim varC_MTTestLib4 = CreateEmptyCompilation(varMTTestLib4_Name,
                                                    Nothing,
                                                    {NetFramework.mscorlib,
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

            Dim c4 = CreateEmptyCompilation(New AssemblyIdentity("c4"),
                                       Nothing,
                                       {NetFramework.mscorlib,
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

            Dim c5 = CreateEmptyCompilation(New AssemblyIdentity("c5"),
                                       Nothing,
                                       {NetFramework.mscorlib,
                                        New VisualBasicCompilationReference(varC_MTTestLib3)})

            Dim asm5 = c5.SourceAssembly().BoundReferences()
            Assert.Same(asm5(0), asm2(0))

            Assert.True(asm5(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3(3)))

            Dim c6 = CreateEmptyCompilation(New AssemblyIdentity("c6"),
                                       Nothing,
                                       {NetFramework.mscorlib,
                                        New VisualBasicCompilationReference(varC_MTTestLib2)})

            Dim asm6 = c6.SourceAssembly().BoundReferences()
            Assert.Same(asm6(0), asm2(0))

            Assert.True(asm6(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()))

            Dim c7 = CreateEmptyCompilation(New AssemblyIdentity("c7"),
                                       Nothing,
                                       {NetFramework.mscorlib,
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
            Dim c8 = CreateEmptyCompilation(New AssemblyIdentity("c8"),
                                       Nothing,
                                       {NetFramework.mscorlib,
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

            Dim c9 = CreateEmptyCompilation(New AssemblyIdentity("c9"),
                                       Nothing,
                                       {NetFramework.mscorlib,
                                        New VisualBasicCompilationReference(varC_MTTestLib4)})

            Dim asm9 = c9.SourceAssembly().BoundReferences()
            Assert.Same(asm9(0), asm2(0))

            Assert.True(asm9(1).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4(4)))

            Dim c10 = CreateEmptyCompilation(New AssemblyIdentity("c10"),
                                        Nothing,
                                        {NetFramework.mscorlib,
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

        End Sub

#If Retargeting Then
        <Fact(skip:=SkipReason.AlreadyTestingRetargeting)>
        Public Sub MultiTargeting4()
#Else
        <Fact()>
        Public Sub MultiTargeting4()
#End If

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

            Dim c1_V1 As VisualBasicCompilation = CreateEmptyCompilation(c1_V1_Name,
                               {source1.Value},
                               {NetFramework.mscorlib})

            Dim asm1_V1 = c1_V1.SourceAssembly

            Dim c1_V2_Name = New AssemblyIdentity("c1", New Version("2.0.0.0"))

            Dim c1_V2 As VisualBasicCompilation = CreateEmptyCompilation(c1_V2_Name,
                               {source1.Value},
                               {NetFramework.mscorlib})

            Dim asm1_V2 = c1_V2.SourceAssembly

            Dim source4 =
        <s4>
Public Class C4
End Class
</s4>

            Dim c4_V1_Name = New AssemblyIdentity("c4", New Version("1.0.0.0"))

            Dim c4_V1 As VisualBasicCompilation = CreateEmptyCompilation(c4_V1_Name,
                               {source4.Value},
                               {NetFramework.mscorlib})

            Dim asm4_V1 = c4_V1.SourceAssembly

            Dim c4_V2_Name = New AssemblyIdentity("c4", New Version("2.0.0.0"))

            Dim c4_V2 As VisualBasicCompilation = CreateEmptyCompilation(c4_V2_Name,
                               {source4.Value},
                               {NetFramework.mscorlib})

            Dim asm4_V2 = c4_V2.SourceAssembly

            Dim source7 =
        <s3>
Public Class C7
End Class

Public Class C8(Of T)
End Class
</s3>

            Dim c7 As VisualBasicCompilation = CreateEmptyCompilation(New AssemblyIdentity("C7"),
                               {source7.Value},
                               {NetFramework.mscorlib})

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

            Dim c3 As VisualBasicCompilation = CreateEmptyCompilation(New AssemblyIdentity("C3"),
                               {source3.Value},
                               {NetFramework.mscorlib,
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

            Dim c5 As VisualBasicCompilation = CreateEmptyCompilation(New AssemblyIdentity("C5"),
                               {source5.Value},
                               {NetFramework.mscorlib,
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
        End Sub

        <Fact()>
        Public Sub MultiTargeting5()
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

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)
            c1 = c1.AddReferences(refs)

            Dim c2_Name = New AssemblyIdentity("MTTestLib2")
            Dim c2 = CreateEmptyCompilation(c2_Name,
                                       Nothing,
                                       {NetFramework.mscorlib,
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
            Dim mscorlibRef = NetFramework.mscorlib
            Dim systemCoreRef = NetFramework.SystemCore
            Dim systemRef = NetFramework.System

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

        <WorkItem(578706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578706")>
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

            Dim lib1 = CreateCompilationWithMscorlib40(source1, OutputKind.NetModule)
            Dim ref1 = lib1.EmitToImageReference()

            Dim lib2 = CreateCompilationWithMscorlib40AndReferences(source2, {ref1})
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
            Return VisualBasicSyntaxTree.ParseText(text, path:=path)
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

        <Fact>
        Public Sub RuntimeCapabilitiesSupported()
            Dim compilation = VisualBasicCompilation.Create("Compilation")
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.ByRefFields))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.UnmanagedSignatureCallingConvention))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.VirtualStaticsInInterfaces))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces))

            compilation = VisualBasicCompilation.Create("Compilation", references:=TargetFrameworkUtil.GetReferences(TargetFramework.Net50, Nothing))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.ByRefFields))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.UnmanagedSignatureCallingConvention))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.VirtualStaticsInInterfaces))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces))

            compilation = VisualBasicCompilation.Create("Compilation", references:=TargetFrameworkUtil.GetReferences(TargetFramework.Net60, Nothing))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.ByRefFields))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses))
            Assert.False(compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.UnmanagedSignatureCallingConvention))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.VirtualStaticsInInterfaces))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces))

            compilation = VisualBasicCompilation.Create("Compilation", references:=TargetFrameworkUtil.GetReferences(TargetFramework.Net70, Nothing))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.ByRefFields))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.UnmanagedSignatureCallingConvention))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.VirtualStaticsInInterfaces))
            Assert.True(compilation.SupportsRuntimeCapability(RuntimeCapability.DefaultImplementationsOfInterfaces))
        End Sub
    End Class
End Namespace
