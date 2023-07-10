' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.CorLibrary

    Public Class CorTypes
        Inherits BasicTestBase

        <Fact()>
        Public Sub MissingCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.CorLibrary.NoMsCorLibRef})
            Dim noMsCorLibRef = assemblies(0)

            For i As Integer = 1 To SpecialType.Count
                Dim t = noMsCorLibRef.GetSpecialType(CType(i, SpecialType))
                Assert.Equal(CType(i, SpecialType), t.SpecialType)
                Assert.Equal(TypeKind.Error, t.TypeKind)
                Assert.NotNull(t.ContainingAssembly)
                Assert.Equal("<Missing Core Assembly>", t.ContainingAssembly.Identity.Name)
            Next

            Dim p = noMsCorLibRef.GlobalNamespace.GetTypeMembers("I1").Single().
                GetMembers("M1").OfType(Of MethodSymbol)().Single().
                Parameters(0).Type

            Assert.Equal(TypeKind.Error, p.TypeKind)
            Assert.Equal(SpecialType.System_Int32, p.SpecialType)
        End Sub

        <Fact()>
        Public Sub PresentCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({NetCoreApp.SystemRuntime})
            Dim msCorLibRef As MetadataOrSourceAssemblySymbol = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)

            Dim knownMissingTypes As HashSet(Of Integer) = New HashSet(Of Integer) From {SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute}

            For i As Integer = 1 To SpecialType.Count
                Dim t = msCorLibRef.GetSpecialType(CType(i, SpecialType))
                Assert.Equal(CType(i, SpecialType), t.SpecialType)
                Assert.Same(msCorLibRef, t.ContainingAssembly)
                If knownMissingTypes.Contains(i) Then
                    ' not present on dotnet core 3.1
                    Assert.Equal(TypeKind.Error, t.TypeKind)
                Else
                    Assert.NotEqual(TypeKind.Error, t.TypeKind)
                End If
            Next

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            assemblies = MetadataTestHelpers.GetSymbolsForReferences({MetadataReference.CreateFromImage(Net50.Resources.SystemRuntime)})
            msCorLibRef = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)
            Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            Dim namespaces As New Queue(Of NamespaceSymbol)()

            namespaces.Enqueue(msCorLibRef.Modules(0).GlobalNamespace)
            Dim count As Integer = 0

            While (namespaces.Count > 0)

                For Each m In namespaces.Dequeue().GetMembers()
                    Dim ns = TryCast(m, NamespaceSymbol)

                    If (ns IsNot Nothing) Then
                        namespaces.Enqueue(ns)
                    ElseIf (DirectCast(m, NamedTypeSymbol).SpecialType <> SpecialType.None) Then
                        count += 1
                    End If

                    If (count >= SpecialType.Count) Then
                        Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                    End If
                Next
            End While

            Assert.Equal(count + knownMissingTypes.Count, CType(SpecialType.Count, Integer))
            Assert.Equal(knownMissingTypes.Any(), msCorLibRef.KeepLookingForDeclaredSpecialTypes)
        End Sub

        <Fact()>
        Public Sub FakeCorLib()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.CorLibrary.FakeMsCorLib.dll})
            Dim msCorLibRef = DirectCast(assemblies(0), MetadataOrSourceAssemblySymbol)

            For i As Integer = 1 To SpecialType.Count
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                Dim t = msCorLibRef.GetSpecialType(CType(i, SpecialType))
                Assert.Equal(CType(i, SpecialType), t.SpecialType)

                If (t.SpecialType = SpecialType.System_Object) Then
                    Assert.NotEqual(TypeKind.Error, t.TypeKind)
                Else
                    Assert.Equal(TypeKind.Error, t.TypeKind)
                    Assert.Same(msCorLibRef, t.ContainingAssembly)
                End If

                Assert.Same(msCorLibRef, t.ContainingAssembly)
            Next

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
        End Sub

        <Fact()>
        Public Sub SourceCorLib()
            Dim source =
<source>
namespace System
    public class Object
    End Class
ENd NAmespace
</source>

            Dim c1 = VisualBasicCompilation.Create("CorLib", syntaxTrees:={VisualBasicSyntaxTree.ParseText(source.Value)})

            Assert.Same(c1.Assembly, c1.Assembly.CorLibrary)

            Dim msCorLibRef = DirectCast(c1.Assembly, MetadataOrSourceAssemblySymbol)

            For i As Integer = 1 To SpecialType.Count
                If (i <> SpecialType.System_Object) Then
                    Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes)
                    Dim t = c1.Assembly.GetSpecialType(CType(i, SpecialType))
                    Assert.Equal(CType(i, SpecialType), t.SpecialType)

                    Assert.Equal(TypeKind.Error, t.TypeKind)
                    Assert.Same(msCorLibRef, t.ContainingAssembly)
                End If
            Next

            Dim system_object = msCorLibRef.Modules(0).GlobalNamespace.GetMembers("System").
                Select(Function(m) DirectCast(m, NamespaceSymbol)).Single().GetTypeMembers("Object").Single()

            Assert.Equal(SpecialType.System_Object, system_object.SpecialType)

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes)

            Assert.Same(system_object, c1.Assembly.GetSpecialType(SpecialType.System_Object))

            Assert.Throws(Of ArgumentOutOfRangeException)(Function() c1.GetSpecialType(SpecialType.None))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() c1.GetSpecialType(CType(SpecialType.Count + 1, SpecialType)))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() msCorLibRef.GetSpecialType(SpecialType.None))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() msCorLibRef.GetSpecialType(CType(SpecialType.Count + 1, SpecialType)))
        End Sub

        <Fact()>
        Public Sub TestGetTypeByNameAndArity()

            Dim source1 =
<source>
namespace System

    Public Class TestClass
    End Class

    public class TestClass(Of T)
    End Class
End Namespace
</source>

            Dim source2 =
<source>
namespace System

    Public Class TestClass
    End Class
End Namespace
</source>

            Dim c1 = VisualBasicCompilation.Create("Test1",
                syntaxTrees:={VisualBasicSyntaxTree.ParseText(source1.Value)},
                references:={TestMetadata.Net40.mscorlib})

            Assert.Null(c1.GetTypeByMetadataName("DoesntExist"))
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`1"))
            Assert.Null(c1.GetTypeByMetadataName("DoesntExist`2"))

            Dim c1TestClass As NamedTypeSymbol = c1.GetTypeByMetadataName("System.TestClass")
            Assert.NotNull(c1TestClass)
            Dim c1TestClassT As NamedTypeSymbol = c1.GetTypeByMetadataName("System.TestClass`1")
            Assert.NotNull(c1TestClassT)
            Assert.Null(c1.GetTypeByMetadataName("System.TestClass`2"))

            Dim c2 = VisualBasicCompilation.Create("Test2",
                        syntaxTrees:={VisualBasicSyntaxTree.ParseText(source2.Value)},
                        references:={New VisualBasicCompilationReference(c1),
                                        TestMetadata.Net40.mscorlib})

            Dim c2TestClass As NamedTypeSymbol = c2.GetTypeByMetadataName("System.TestClass")
            Assert.Same(c2.Assembly, c2TestClass.ContainingAssembly)

            Dim c3 = VisualBasicCompilation.Create("Test3",
                        references:={New VisualBasicCompilationReference(c2),
                                    TestMetadata.Net40.mscorlib})

            Dim c3TestClass As NamedTypeSymbol = c3.GetTypeByMetadataName("System.TestClass")
            Assert.NotSame(c2TestClass, c3TestClass)
            Assert.True(c3TestClass.ContainingAssembly.RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(c2TestClass.ContainingAssembly))

            Assert.Null(c3.GetTypeByMetadataName("System.TestClass`1"))

            Dim c4 = VisualBasicCompilation.Create("Test4",
                        references:={New VisualBasicCompilationReference(c1), New VisualBasicCompilationReference(c2),
                                    TestMetadata.Net40.mscorlib})

            Dim c4TestClass As NamedTypeSymbol = c4.GetTypeByMetadataName("System.TestClass")
            Assert.Null(c4TestClass)

            Assert.Same(c1TestClassT, c4.GetTypeByMetadataName("System.TestClass`1"))
        End Sub

    End Class

End Namespace
