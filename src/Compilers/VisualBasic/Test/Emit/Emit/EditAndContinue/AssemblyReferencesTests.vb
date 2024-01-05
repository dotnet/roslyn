' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Reflection.Metadata
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.MetadataUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    Public Class AssemblyReferencesTests
        Inherits EditAndContinueTestBase

        Private Shared ReadOnly s_signedDll As VisualBasicCompilationOptions =
            TestOptions.ReleaseDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2)

        ''' <summary>
        ''' Symbol matcher considers two source types that only differ in the declaring compilations different.
        ''' </summary>
        <Fact>
        Public Sub ChangingCompilationDependencies()
            Dim srcLib = "
Public Class D
End Class
"
            Dim src0 = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 1
    End Function
End Class
"
            Dim src1 = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 3
    End Function
End Class
"

            Dim lib0 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib0.VerifyDiagnostics()
            Dim lib1 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib1.VerifyDiagnostics()
            Dim lib2 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib40({src0}, {lib0.ToMetadataReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.ToMetadataReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.ToMetadataReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify()

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2)))

            diff2.EmitResult.Diagnostics.Verify()
        End Sub

        <Fact>
        Public Sub DependencyVersionWildcards_Compilation1()
            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002))

            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1002))

            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1003),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1001))

            TestDependencyVersionWildcards(
                "1.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1003))

            TestDependencyVersionWildcards(
                "1.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1005),
                New Version(1, 0, 2000, 1002))
        End Sub

        Private Sub TestDependencyVersionWildcards(sourceVersion As String, version0 As Version, version1 As Version, version2 As Version)
            Dim srcLib = $"
<Assembly: System.Reflection.AssemblyVersion(""{sourceVersion}"")>

Public Class D
End Class
"
            Dim src0 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 1
    End Function
End Class
"
            Dim src1 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 3
    End Function

    Public Shared Function G(a As D) As Integer
        Return 4
    End Function
End Class
"
            Dim lib0 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib0.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version0)
            lib0.VerifyDiagnostics()

            Dim lib1 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version1)
            lib1.VerifyDiagnostics()

            Dim lib2 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version2)
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib40({src0}, {lib0.ToMetadataReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.ToMetadataReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.ToMetadataReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2),
                                      New SemanticEdit(SemanticEditKind.Insert, Nothing, g2)))

            Dim md1 = diff1.GetMetadata()
            Dim md2 = diff2.GetMetadata()

            Dim aggReader = New AggregatedMetadataReader(md0.MetadataReader, md1.Reader, md2.Reader)
            VerifyAssemblyReferences(aggReader,
            {
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString(),
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString(),
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString()
            })
        End Sub

        <Fact>
        Public Sub DependencyVersionWildcards_Metadata()
            Dim srcLib = $"
<Assembly: System.Reflection.AssemblyVersion(""1.0.*"")>

Public Class D
End Class
"
            Dim src0 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 1
    End Function
End Class
"
            Dim src1 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 3
    End Function

    Public Shared Function G(a As D) As Integer
        Return 4
    End Function
End Class
"
            Dim lib0 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib0.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1001))
            lib0.VerifyDiagnostics()

            Dim lib1 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1002))
            lib1.VerifyDiagnostics()

            Dim lib2 = CreateCompilationWithMscorlib40({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1003))
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib40({src0}, {lib0.EmitToImageReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.EmitToImageReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.EmitToImageReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments("C",
                    String.Format(CodeAnalysisResources.ChangingVersionOfAssemblyReferenceIsNotAllowedDuringDebugging,
                        "Lib, Version=1.0.2000.1001, Culture=neutral, PublicKeyToken=null", "1.0.2000.1002")))
        End Sub

        <Fact, WorkItem(9004, "https://github.com/dotnet/roslyn/issues/9004")>
        Public Sub DependencyVersionWildcardsCollisions()
            Dim srcLib01 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.1"")>

Public Class D0
End Class
"
            Dim srcLib02 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.2"")>

Public Class D0
End Class
"
            Dim srcLib11 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.1.1"")>

Public Class D1
End Class
"
            Dim srcLib12 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.1.2"")>

Public Class D1
End Class 
"
            Dim src0 = "
Class C 
    Public Shared Function F(a As D0, b As D1) As Integer
        Return 1
    End Function
End Class
"
            Dim src1 = "
Class C 
    Public Shared Function F(a As D0, b As D1) As Integer
        Return 2
    End Function
End Class
"
            Dim lib01 = CreateCompilationWithMscorlib40({srcLib01}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref01 = lib01.ToMetadataReference()
            Dim lib02 = CreateCompilationWithMscorlib40({srcLib02}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref02 = lib02.ToMetadataReference()
            Dim lib11 = CreateCompilationWithMscorlib40({srcLib11}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref11 = lib11.ToMetadataReference()
            Dim lib12 = CreateCompilationWithMscorlib40({srcLib12}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref12 = lib12.ToMetadataReference()

            Dim compilation0 = CreateCompilationWithMscorlib40({src0}, {ref01, ref11}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, ref02, ref12})

            ' ILVerify: Failed to load type 'D1' from assembly 'Lib, Version=1.0.0.1, Culture=neutral, PublicKeyToken=ce65828c82a341f2'
            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.FailsILVerify)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments("C",
                    String.Format(CodeAnalysisResources.ChangingVersionOfAssemblyReferenceIsNotAllowedDuringDebugging,
                        "Lib, Version=1.0.0.1, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "1.0.0.2")))
        End Sub

        Public Sub VerifyAssemblyReferences(reader As AggregatedMetadataReader, expected As String())
            AssertEx.Equal(expected, reader.GetAssemblyReferences().Select(Function(aref) $"{reader.GetString(aref.Name)}, {aref.Version}"))
        End Sub

        <Fact>
        <WorkItem(202017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/202017")>
        Public Sub CurrentCompilationVersionWildcards()
            Dim source0 = MarkedSource("
Imports System
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")>

Class C
    Shared Sub M()
        Console.WriteLine(New Action(
            <N:0>Sub() 
                   Console.WriteLine(1) 
                 End Sub</N:0>))
    End Sub

    Shared Sub F()
    End Sub
End Class")
            Dim source1 = MarkedSource("
Imports System
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")>

Class C
    Shared Sub M()
        Console.WriteLine(New Action(
            <N:0>Sub() 
                   Console.WriteLine(1) 
                 End Sub</N:0>))

        Console.WriteLine(New Action(
            <N:1>Sub() 
                   Console.WriteLine(2) 
                 End Sub</N:1>))
    End Sub

    Shared Sub F()
    End Sub
End Class")
            Dim source2 = MarkedSource("
Imports System
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")>

Class C
    Shared Sub M()
       Console.WriteLine(New Action(
            <N:0>Sub() 
                   Console.WriteLine(1) 
                 End Sub</N:0>))

        Console.WriteLine(New Action(
            <N:1>Sub() 
                   Console.WriteLine(2) 
                 End Sub</N:1>))
    End Sub

    Shared Sub F()
        Console.WriteLine(1)
    End Sub
End Class")
            Dim source3 = MarkedSource("
Imports System
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")>

Class C
    Shared Sub M()
       Console.WriteLine(New Action(
            <N:0>Sub() 
                   Console.WriteLine(1) 
                 End Sub</N:0>))

        Console.WriteLine(New Action(
            <N:1>Sub() 
                   Console.WriteLine(2) 
                 End Sub</N:1>))

        Console.WriteLine(New Action(
            <N:2>Sub() 
                   Console.WriteLine(3) 
                 End Sub</N:2>))

        Console.WriteLine(New Action(
            <N:3>Sub() 
                   Console.WriteLine(4) 
                 End Sub</N:3>))
    End Sub

    Shared Sub F()
        Console.WriteLine(1)
    End Sub
End Class")
            Dim options = ComSafeDebugDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2)

            Dim compilation0 = CreateCompilationWithMscorlib40(source0.Tree, options:=options.WithCurrentLocalTime(New DateTime(2016, 1, 1, 1, 0, 0)))
            Dim compilation1 = compilation0.WithSource(source1.Tree).WithOptions(options.WithCurrentLocalTime(New DateTime(2016, 1, 1, 1, 0, 10)))
            Dim compilation2 = compilation1.WithSource(source2.Tree).WithOptions(options.WithCurrentLocalTime(New DateTime(2016, 1, 1, 1, 0, 20)))
            Dim compilation3 = compilation2.WithSource(source3.Tree).WithOptions(options.WithCurrentLocalTime(New DateTime(2016, 1, 1, 1, 0, 30)))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim reader0 = md0.MetadataReader

            Dim m0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim m1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim m2 = compilation2.GetMember(Of MethodSymbol)("C.M")
            Dim m3 = compilation3.GetMember(Of MethodSymbol)("C.M")

            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            ' First update adds some new synthesized members (lambda related)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1#1, _Lambda$__1-0, _Lambda$__1-1#1}")

            ' Second update is to a method that doesn't produce any synthesized members 
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1#1, _Lambda$__1-0, _Lambda$__1-1#1}")

            ' Last update again adds some new synthesized members (lambdas).
            ' Synthesized members added in the first update need to be mapped to the current compilation.
            ' Their containing assembly version is different than the version of the previous assembly and 
            ' hence we need to account for wildcards when comparing the versions.
            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, m2, m3, GetSyntaxMapFromMarkers(source2, source3))))

            diff3.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1#1, $I1-2#3, $I1-3#3, _Lambda$__1-0, _Lambda$__1-1#1, _Lambda$__1-2#3, _Lambda$__1-3#3}")
        End Sub
    End Class
End Namespace
