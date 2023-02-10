' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE
    Public Class NoPia
        Inherits BasicTestBase

        <Fact()>
        Public Sub HideLocalTypeDefinitions()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.NoPia.LocalTypes1, TestReferences.SymbolsTests.NoPia.LocalTypes2})
            Dim localTypes1 = assemblies(0).Modules(0)
            Dim localTypes2 = assemblies(1).Modules(0)
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("I1").Length)
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("S1").Length)
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMember(Of NamespaceSymbol)("NS1").GetTypeMembers().Length())
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("I1").Length)
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("S1").Length)
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMember(Of NamespaceSymbol)("NS1").GetTypeMembers().Length())
        End Sub

        <Fact()>
        Public Sub LocalTypeSubstitution1()
            Dim assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia1,
                    Net40.mscorlib,
                    TestReferences.SymbolsTests.MDTestLib1
                })
            Dim localTypes1_1 = assemblies1(0)
            Dim localTypes2_1 = assemblies1(1)
            Dim pia1_1 = assemblies1(2)
            Dim varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single()
            Dim varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single()
            Dim varNS1 = pia1_1.GlobalNamespace.GetMember(Of NamespaceSymbol)("NS1")
            Dim varI2 = varNS1.GetTypeMembers("I2").Single()
            Dim varS2 = varNS1.GetTypeMembers("S2").Single()
            Dim classLocalTypes1 As NamedTypeSymbol
            Dim classLocalTypes2 As NamedTypeSymbol
            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            Dim test1 As MethodSymbol
            Dim test2 As MethodSymbol
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            Dim param As ImmutableArray(Of ParameterSymbol)
            param = test1.Parameters
            Assert.Same(varI1, param(0).[Type])
            Assert.Same(varI2, param(1).[Type])
            param = test2.Parameters
            Assert.Same(varS1, param(0).[Type])
            Assert.Same(varS2, param(1).[Type])
            Dim assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib
                    })
            Dim localTypes1_2 = assemblies2(0)
            Dim localTypes2_2 = assemblies2(1)
            Assert.NotSame(localTypes1_1, localTypes1_2)
            Assert.NotSame(localTypes2_1, localTypes2_2)
            Assert.Same(pia1_1, assemblies2(2))
            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Same(varI1, param(0).[Type])
            Assert.Same(varI2, param(1).[Type])
            param = test2.Parameters
            Assert.Same(varS1, param(0).[Type])
            Assert.Same(varS2, param(1).[Type])
            Dim assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                        TestReferences.SymbolsTests.NoPia.Pia1
                    })
            Dim localTypes1_3 = assemblies3(0)
            Dim localTypes2_3 = assemblies3(1)
            Dim pia1_3 = assemblies3(2)
            Assert.NotSame(localTypes1_1, localTypes1_3)
            Assert.NotSame(localTypes2_1, localTypes2_3)
            Assert.NotSame(localTypes1_2, localTypes1_3)
            Assert.NotSame(localTypes2_2, localTypes2_3)
            Assert.NotSame(pia1_1, pia1_3)
            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Same(pia1_3.GlobalNamespace.GetTypeMembers("I1").Single(), param(0).[Type])
            Assert.Same(pia1_3.GlobalNamespace.GetMember(Of NamespaceSymbol)("NS1").GetTypeMembers("I2").[Single](), param(1).[Type])
            param = test2.Parameters
            Dim missing As NoPiaMissingCanonicalTypeSymbol
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            missing = DirectCast(param(0).[Type], NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localTypes2_3, missing.EmbeddingAssembly)
            Assert.Null(missing.Guid)
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName)
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope)
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia1,
                    Net40.mscorlib,
                    TestReferences.SymbolsTests.MDTestLib1
                })

            For i As Integer = 0 To assemblies1.Length - 1 Step 1
                Assert.Same(assemblies1(i), assemblies4(i))
            Next

            Dim assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia2,
                    Net40.mscorlib
                })
            Dim localTypes1_5 = assemblies5(0)
            Dim localTypes2_5 = assemblies5(1)
            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            missing = DirectCast(param(0).[Type], NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly)
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid)
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName)
            Assert.Null(missing.Scope)
            Assert.Null(missing.Identifier)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia3,
                    Net40.mscorlib
                })
            Dim localTypes1_6 = assemblies6(0)
            Dim localTypes2_6 = assemblies6(1)
            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia4,
                    Net40.mscorlib
                })
            Dim localTypes1_7 = assemblies7(0)
            Dim localTypes2_7 = assemblies7(1)
            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Equal(TypeKind.[Interface], param(0).[Type].TypeKind)
            Assert.Equal(TypeKind.[Interface], param(1).[Type].TypeKind)
            Assert.NotEqual(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.NotEqual(SymbolKind.ErrorType, param(1).[Type].Kind)
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes2,
                    TestReferences.SymbolsTests.NoPia.Pia4,
                    TestReferences.SymbolsTests.NoPia.Pia1,
                    Net40.mscorlib
                })

            Dim localTypes1_8 = assemblies8(0)
            Dim localTypes2_8 = assemblies8(1)
            Dim pia4_8 = assemblies8(2)
            Dim pia1_8 = assemblies8(3)
            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Dim ambiguous As NoPiaAmbiguousCanonicalTypeSymbol
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            ambiguous = DirectCast(param(0).[Type], NoPiaAmbiguousCanonicalTypeSymbol)
            Assert.False(DirectCast(param(0).Type, INamedTypeSymbol).IsSerializable)
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly)
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate)
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaAmbiguousCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.Library1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.Pia4,
                    Net40.mscorlib
                })
            Dim library1_9 = assemblies9(0)
            Dim localTypes1_9 = assemblies9(1)
            Dim assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(
                {
                    TestReferences.SymbolsTests.NoPia.Library1,
                    TestReferences.SymbolsTests.NoPia.LocalTypes1,
                    TestReferences.SymbolsTests.NoPia.Pia4,
                    Net40.mscorlib,
                    TestReferences.SymbolsTests.MDTestLib1
                })
            Dim library1_10 = assemblies10(0)
            Dim localTypes1_10 = assemblies10(1)
            Assert.NotSame(library1_9, library1_10)
            Assert.NotSame(localTypes1_9, localTypes1_10)
            GC.KeepAlive(localTypes1_1)
            GC.KeepAlive(localTypes2_1)
            GC.KeepAlive(pia1_1)
            GC.KeepAlive(localTypes1_9)
            GC.KeepAlive(library1_9)
        End Sub

        <Fact()>
        Public Sub LocalTypeSubstitution2()
            Dim localTypes1Source As String = <text>
public class LocalTypes1

    public Sub Test1(x As I1, y As NS1.I2)
    End Sub
End Class
            </text>.Value

            Dim localTypes2Source As String = <text>
public class LocalTypes2

    public Sub Test2(x As S1, y As NS1.S2)
    End Sub
End Class
            </text>.Value
            Dim mscorlibRef = Net40.mscorlib
            Dim pia1CopyLink = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(True)
            Dim pia1CopyRef = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(False)

            ' vbc /t:library /vbruntime- LocalTypes1.vb /l:Pia1.dll
            Dim localTypes1 = VisualBasicCompilation.Create("LocalTypes1", {Parse(localTypes1Source)}, {pia1CopyLink, mscorlibRef})
            Dim localTypes1Asm = localTypes1.Assembly

            Dim localTypes2 = VisualBasicCompilation.Create("LocalTypes2", {Parse(localTypes2Source)}, {mscorlibRef, pia1CopyLink})
            Dim localTypes2Asm = localTypes2.Assembly

            Dim assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        TestReferences.SymbolsTests.MDTestLib1,
                        TestReferences.SymbolsTests.MDTestLib2,
                        localTypes1,
                        localTypes2
                    })

            Dim localTypes1_1 = assemblies1(4)
            Dim localTypes2_1 = assemblies1(5)
            Dim pia1_1 = assemblies1(0)

            Assert.NotSame(localTypes1Asm, localTypes1_1)
            Assert.Equal(1, localTypes1_1.Modules(0).GetReferencedAssemblies().Length)
            Assert.Equal(1, localTypes1_1.Modules(0).GetReferencedAssemblySymbols().Length)
            Assert.Same(localTypes1.GetReferencedAssemblySymbol(mscorlibRef), localTypes1_1.Modules(0).GetReferencedAssemblySymbols()(0))

            Assert.NotSame(localTypes2Asm, localTypes2_1)
            Assert.Equal(1, localTypes2_1.Modules(0).GetReferencedAssemblies().Length)
            Assert.Equal(1, localTypes2_1.Modules(0).GetReferencedAssemblySymbols().Length)
            Assert.Same(localTypes2.GetReferencedAssemblySymbol(mscorlibRef), localTypes2_1.Modules(0).GetReferencedAssemblySymbols()(0))

            Dim varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single()
            Dim varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single()
            Dim varNS1 = pia1_1.GlobalNamespace.GetMember(Of NamespaceSymbol)("NS1")
            Dim varI2 = varNS1.GetTypeMembers("I2").Single()
            Dim varS2 = varNS1.GetTypeMembers("S2").Single()
            Dim classLocalTypes1 As NamedTypeSymbol
            Dim classLocalTypes2 As NamedTypeSymbol
            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            Dim test1 As MethodSymbol
            Dim test2 As MethodSymbol
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            Dim param As ImmutableArray(Of ParameterSymbol)
            param = test1.Parameters
            Assert.Same(varI1, param(0).[Type])
            Assert.Same(varI2, param(1).[Type])
            param = test2.Parameters
            Assert.Same(varS1, param(0).[Type])
            Assert.Same(varS2, param(1).[Type])
            Dim assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        TestReferences.SymbolsTests.MDTestLib1,
                        localTypes1,
                        localTypes2
                    })
            Dim localTypes1_2 = assemblies2(3)
            Dim localTypes2_2 = assemblies2(4)
            Assert.NotSame(localTypes1_1, localTypes1_2)
            Assert.NotSame(localTypes2_1, localTypes2_2)
            Assert.Same(pia1_1, assemblies2(0))
            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Same(varI1, param(0).[Type])
            Assert.Same(varI2, param(1).[Type])
            param = test2.Parameters
            Assert.Same(varS1, param(0).[Type])
            Assert.Same(varS2, param(1).[Type])
            Dim assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        localTypes1,
                        localTypes2
                    })

            Dim localTypes1_3 = assemblies3(2)
            Dim localTypes2_3 = assemblies3(3)
            Assert.NotSame(localTypes1_1, localTypes1_3)
            Assert.NotSame(localTypes2_1, localTypes2_3)
            Assert.NotSame(localTypes1_2, localTypes1_3)
            Assert.NotSame(localTypes2_2, localTypes2_3)
            Assert.Same(pia1_1, assemblies3(0))
            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Same(varI1, param(0).[Type])
            Assert.Same(varI2, param(1).[Type])
            param = test2.Parameters
            Assert.Same(varS1, param(0).[Type])
            Assert.Same(varS2, param(1).[Type])
            Dim assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        TestReferences.SymbolsTests.MDTestLib1,
                        TestReferences.SymbolsTests.MDTestLib2,
                        localTypes1,
                        localTypes2
                    })

            For i As Integer = 0 To assemblies1.Length - 1 Step 1
                Assert.Same(assemblies1(i), assemblies4(i))
            Next

            Dim assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia2,
                        Net40.mscorlib,
                        localTypes1,
                        localTypes2
                    })
            Dim localTypes1_5 = assemblies5(2)
            Dim localTypes2_5 = assemblies5(3)
            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Dim missing As NoPiaMissingCanonicalTypeSymbol
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            missing = DirectCast(param(0).[Type], NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly)
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid)
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName)
            Assert.Null(missing.Scope)
            Assert.Null(missing.Identifier)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            missing = DirectCast(param(0).[Type], NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localTypes2_5, missing.EmbeddingAssembly)
            Assert.Null(missing.Guid)
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName)
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope)
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])

            Dim assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                         TestReferences.SymbolsTests.NoPia.Pia3,
                         Net40.mscorlib,
                         localTypes1,
                         localTypes2
                    })

            Dim localTypes1_6 = assemblies6(2)
            Dim localTypes2_6 = assemblies6(3)
            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])

            Dim assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia4,
                        Net40.mscorlib,
                        localTypes1,
                        localTypes2
                    })

            Dim pia4_7 = assemblies7(0)
            Dim localTypes1_7 = assemblies7(2)
            Dim localTypes2_7 = assemblies7(3)
            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Assert.Equal(TypeKind.[Interface], param(0).[Type].TypeKind)
            Assert.Equal(TypeKind.[Interface], param(1).[Type].TypeKind)
            Assert.NotEqual(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.NotEqual(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.Same(pia4_7.GlobalNamespace.GetTypeMembers("I1").Single(), param(0).[Type])
            Assert.Same(pia4_7, param(1).[Type].ContainingAssembly)
            Assert.Equal("NS1.I2", param(1).[Type].ToTestDisplayString())
            param = test2.Parameters
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Pia4,
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        localTypes1,
                        localTypes2
                    })

            Dim localTypes1_8 = assemblies8(3)
            Dim localTypes2_8 = assemblies8(4)
            Dim pia4_8 = assemblies8(0)
            Dim pia1_8 = assemblies8(1)
            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single()
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single()
            test1 = classLocalTypes1.GetMember(Of MethodSymbol)("Test1")
            test2 = classLocalTypes2.GetMember(Of MethodSymbol)("Test2")
            param = test1.Parameters
            Dim ambiguous As NoPiaAmbiguousCanonicalTypeSymbol
            Assert.Equal(SymbolKind.ErrorType, param(0).[Type].Kind)
            ambiguous = DirectCast(param(0).[Type], NoPiaAmbiguousCanonicalTypeSymbol)
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly)
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate)
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate)
            Assert.Equal(SymbolKind.ErrorType, param(1).[Type].Kind)
            Assert.IsType(Of NoPiaAmbiguousCanonicalTypeSymbol)(param(1).[Type])
            Dim assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Library1,
                        TestReferences.SymbolsTests.NoPia.Pia4,
                        Net40.mscorlib,
                        localTypes1
                    })

            Dim library1_9 = assemblies9(0)
            Dim localTypes1_9 = assemblies9(3)
            Assert.Equal("LocalTypes1", localTypes1_9.Identity.Name)
            Dim assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(
                    {
                        TestReferences.SymbolsTests.NoPia.Library1,
                        TestReferences.SymbolsTests.NoPia.Pia4,
                        Net40.mscorlib,
                        TestReferences.SymbolsTests.MDTestLib1,
                        localTypes1
                    })

            Dim library1_10 = assemblies10(0)
            Dim localTypes1_10 = assemblies10(4)
            Assert.Equal("LocalTypes1", localTypes1_10.Identity.Name)
            Assert.NotSame(library1_9, library1_10)
            Assert.NotSame(localTypes1_9, localTypes1_10)
            GC.KeepAlive(localTypes1_1)
            GC.KeepAlive(localTypes2_1)
            GC.KeepAlive(pia1_1)
            GC.KeepAlive(localTypes1_9)
            GC.KeepAlive(library1_9)
        End Sub

        <Fact()>
        Public Sub CyclicReference()
            Dim mscorlibRef = TestReferences.SymbolsTests.MDTestLib1
            Dim cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll
            Dim piaRef = TestReferences.SymbolsTests.NoPia.Pia1
            Dim localTypes1Ref = TestReferences.SymbolsTests.NoPia.LocalTypes1
            Dim tc1 = VisualBasicCompilation.Create("Cyclic1", references:={mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref})
            Assert.NotNull(tc1.Assembly)
            Dim tc2 = VisualBasicCompilation.Create("Cyclic1", references:={mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref})
            Assert.NotNull(tc2.Assembly)
            Assert.NotSame(tc1.GetReferencedAssemblySymbol(localTypes1Ref), tc2.GetReferencedAssemblySymbol(localTypes1Ref))
            GC.KeepAlive(tc1)
            GC.KeepAlive(tc2)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes1()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.NoPia.LocalTypes3, TestReferences.SymbolsTests.NoPia.Pia1})
            Dim asmLocalTypes3 = assemblies(0)
            Dim localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single()
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test1").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test2").ReturnType.Kind)
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType.Kind)
            Dim illegal As NoPiaIllegalGenericInstantiationSymbol = DirectCast(localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType, NoPiaIllegalGenericInstantiationSymbol)
            Assert.Equal("C31(Of I1).I31(Of C33)", illegal.UnderlyingSymbol.ToTestDisplayString())
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test4").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test5").ReturnType)
            assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.NoPia.LocalTypes3, TestReferences.SymbolsTests.NoPia.Pia1, Net40.mscorlib})
            localTypes3 = assemblies(0).GlobalNamespace.GetTypeMembers("LocalTypes3").Single()
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test1").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test2").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test4").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test5").ReturnType)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test6").ReturnType)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes2()
            Dim mscorlibRef = Net40.mscorlib
            Dim pia5Link = TestReferences.SymbolsTests.NoPia.Pia5.WithEmbedInteropTypes(True)
            Dim pia5Ref = TestReferences.SymbolsTests.NoPia.Pia5.WithEmbedInteropTypes(False)
            Dim library2Ref = TestReferences.SymbolsTests.NoPia.Library2.WithEmbedInteropTypes(False)
            Dim library2Link = TestReferences.SymbolsTests.NoPia.Library2.WithEmbedInteropTypes(True)
            Dim pia1Link = TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(True)
            Dim pia1Ref = TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(False)
            Assert.True(pia1Link.Properties.EmbedInteropTypes)
            Assert.False(pia1Ref.Properties.EmbedInteropTypes)
            Assert.True(pia5Link.Properties.EmbedInteropTypes)
            Assert.False(pia5Ref.Properties.EmbedInteropTypes)
            Assert.True(library2Link.Properties.EmbedInteropTypes)
            Assert.False(library2Ref.Properties.EmbedInteropTypes)
            Dim tc1 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, pia5Link})
            Dim pia5Asm1 = tc1.GetReferencedAssemblySymbol(pia5Link)
            Assert.True(pia5Asm1.IsLinked)
            Dim varI5_1 = pia5Asm1.GlobalNamespace.GetTypeMembers("I5").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI5_1.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Dim tc2 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, pia5Ref})
            Dim pia5Asm2 = tc2.GetReferencedAssemblySymbol(pia5Ref)
            Assert.False(pia5Asm2.IsLinked)
            Assert.NotSame(pia5Asm1, pia5Asm2)
            Dim varI5_2 = pia5Asm2.GlobalNamespace.GetTypeMembers("I5").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI5_2.GetMember(Of MethodSymbol)("Foo").ReturnType.Kind)
            Dim tc3 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Ref})
            Dim pia5Asm3 = tc3.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm3 = tc3.GetReferencedAssemblySymbol(library2Ref)
            Assert.True(pia5Asm3.IsLinked)
            Assert.False(library2Asm3.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm3)
            Dim varI7_3 = library2Asm3.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_3.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_3.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)
            Dim tc4 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Ref, pia1Ref})
            Dim pia5Asm4 = tc4.GetReferencedAssemblySymbol(pia5Ref)
            Dim library2Asm4 = tc4.GetReferencedAssemblySymbol(library2Ref)
            Assert.False(pia5Asm4.IsLinked)
            Assert.False(library2Asm4.IsLinked)
            Assert.NotSame(pia5Asm3, pia5Asm4)
            Assert.Same(pia5Asm2, pia5Asm4)
            Assert.NotSame(library2Asm3, library2Asm4)
            Dim varI7_4 = library2Asm4.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMember(Of MethodSymbol)("Foo").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)
            Dim tc5 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Link})
            Dim pia1Asm5 = tc5.GetReferencedAssemblySymbol(pia1Link)
            Dim pia5Asm5 = tc5.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm5 = tc5.GetReferencedAssemblySymbol(library2Ref)
            Assert.True(pia1Asm5.IsLinked)
            Assert.True(pia5Asm5.IsLinked)
            Assert.False(library2Asm5.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm5)
            Assert.NotSame(library2Asm5, library2Asm3)
            Assert.NotSame(library2Asm5, library2Asm4)
            Dim varI7_5 = library2Asm5.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_5.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_5.GetMember(Of MethodSymbol)("Bar").ReturnType)
            Dim tc6 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Ref})
            Dim pia1Asm6 = tc6.GetReferencedAssemblySymbol(pia1Ref)
            Dim pia5Asm6 = tc6.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm6 = tc6.GetReferencedAssemblySymbol(library2Ref)
            Assert.False(pia1Asm6.IsLinked)
            Assert.True(pia5Asm6.IsLinked)
            Assert.False(library2Asm6.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm6)
            Assert.Same(library2Asm6, library2Asm3)
            Dim tc7 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Link, pia5Link, pia1Ref})
            Dim pia5Asm7 = tc7.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm7 = tc7.GetReferencedAssemblySymbol(library2Link)
            Assert.True(pia5Asm7.IsLinked)
            Assert.True(library2Asm7.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm3)
            Assert.NotSame(library2Asm7, library2Asm3)
            Assert.NotSame(library2Asm7, library2Asm4)
            Assert.NotSame(library2Asm7, library2Asm5)
            Dim varI7_7 = library2Asm7.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_7.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_7.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)
            GC.KeepAlive(tc1)
            GC.KeepAlive(tc2)
            GC.KeepAlive(tc3)
            GC.KeepAlive(tc4)
            GC.KeepAlive(tc5)
            GC.KeepAlive(tc6)
            GC.KeepAlive(tc7)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes3()
            Dim varmscorlibRef = Net40.mscorlib
            Dim varALink = TestReferences.SymbolsTests.NoPia.A.WithEmbedInteropTypes(True)
            Dim varARef = TestReferences.SymbolsTests.NoPia.A.WithEmbedInteropTypes(False)
            Dim varBLink = TestReferences.SymbolsTests.NoPia.B.WithEmbedInteropTypes(True)
            Dim varBRef = TestReferences.SymbolsTests.NoPia.B.WithEmbedInteropTypes(False)
            Dim varCLink = TestReferences.SymbolsTests.NoPia.C.WithEmbedInteropTypes(True)
            Dim varCRef = TestReferences.SymbolsTests.NoPia.C.WithEmbedInteropTypes(False)
            Dim varDLink = TestReferences.SymbolsTests.NoPia.D.WithEmbedInteropTypes(True)
            Dim varDRef = TestReferences.SymbolsTests.NoPia.D.WithEmbedInteropTypes(False)
            Dim tc1 = VisualBasicCompilation.Create("C1", references:={varmscorlibRef, varCRef, varARef, varBLink})
            Dim varA1 = tc1.GetReferencedAssemblySymbol(varARef)
            Dim varB1 = tc1.GetReferencedAssemblySymbol(varBLink)
            Dim varC1 = tc1.GetReferencedAssemblySymbol(varCRef)
            Dim tc2 = VisualBasicCompilation.Create("C2", references:={varmscorlibRef, varCRef, varARef, varDRef, varBLink})
            Assert.Same(varA1, tc2.GetReferencedAssemblySymbol(varARef))
            Assert.Same(varB1, tc2.GetReferencedAssemblySymbol(varBLink))
            Assert.Same(varC1, tc2.GetReferencedAssemblySymbol(varCRef))
            Dim varD2 = tc2.GetReferencedAssemblySymbol(varDRef)
            Dim tc3 = VisualBasicCompilation.Create("C3", references:={varmscorlibRef, varCRef, varBLink})
            Assert.Same(varB1, tc3.GetReferencedAssemblySymbol(varBLink))
            Assert.True(tc3.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC1))
            Dim tc4 = VisualBasicCompilation.Create("C4", references:={varmscorlibRef, varCRef, varARef, varBRef})
            Assert.Same(varA1, tc4.GetReferencedAssemblySymbol(varARef))
            Dim varB4 = tc4.GetReferencedAssemblySymbol(varBRef)
            Dim varC4 = tc4.GetReferencedAssemblySymbol(varCRef)
            Assert.NotSame(varC1, varC4)
            Assert.NotSame(varB1, varB4)
            Dim tc5 = VisualBasicCompilation.Create("C5", references:={varmscorlibRef, varCRef, varALink, varBLink})
            Assert.Same(varB1, tc5.GetReferencedAssemblySymbol(varBLink))
            Dim varA5 = tc5.GetReferencedAssemblySymbol(varALink)
            Dim varC5 = tc5.GetReferencedAssemblySymbol(varCRef)
            Assert.NotSame(varA1, varA5)
            Assert.NotSame(varC1, varC5)
            Assert.NotSame(varC4, varC5)
            Dim tc6 = VisualBasicCompilation.Create("C6", references:={varmscorlibRef, varARef, varBLink, varCLink})
            Assert.Same(varA1, tc6.GetReferencedAssemblySymbol(varARef))
            Assert.Same(varB1, tc6.GetReferencedAssemblySymbol(varBLink))
            Dim varC6 = tc6.GetReferencedAssemblySymbol(varCLink)
            Assert.NotSame(varC1, varC6)
            Assert.NotSame(varC4, varC6)
            Assert.NotSame(varC5, varC6)
            Dim tc7 = VisualBasicCompilation.Create("C7", references:={varmscorlibRef, varCRef, varARef})
            Assert.Same(varA1, tc7.GetReferencedAssemblySymbol(varARef))
            Assert.True(tc7.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC4))
            GC.KeepAlive(tc1)
            GC.KeepAlive(tc2)
            GC.KeepAlive(tc3)
            GC.KeepAlive(tc4)
            GC.KeepAlive(tc5)
            GC.KeepAlive(tc6)
            GC.KeepAlive(tc7)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes4()
            Dim localTypes3Source As String = <text>
imports System.Collections.Generic

public class LocalTypes3

    public Function Test1() As C31(Of C33).I31(Of C33)
        return nothing
    End Function

    public Function Test2() As C31(Of C33).I31(Of I1)
        return nothing
    End Function

    public Function Test3() As C31(Of I1).I31(Of C33)
        return nothing
    End Function

    public Function Test4() As C31(Of C33).I31(Of I32(Of I1))
        return nothing
    End Function

    public Function Test5() As C31(Of I32(Of I1)).I31(Of C33)
        return nothing
    End Function

    public Function Test6() As List(Of I1)
        return nothing
    End Function

End Class


public class C31(Of T)
    public interface I31(Of S)
    End Interface
End Class

public class C32(Of T)
End Class

public interface I32(Of S)
End Interface

public class C33
End Class
            </text>.Value
            Dim mscorlibRef = Net40.mscorlib
            Dim pia1CopyLink = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(True)
            Dim pia1CopyRef = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(False)
            ' vbc /t:library /vbruntime- LocalTypes3.vb /l:Pia1.dll
            Dim varC_LocalTypes3 = VisualBasicCompilation.Create("LocalTypes3", {Parse(localTypes3Source)}, {mscorlibRef, pia1CopyLink})
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.NoPia.Pia1, varC_LocalTypes3})
            Dim asmLocalTypes3 = assemblies(1)
            Dim localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single()
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test1").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test2").ReturnType.Kind)
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType.Kind)
            Dim illegal As NoPiaIllegalGenericInstantiationSymbol = DirectCast(localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType, NoPiaIllegalGenericInstantiationSymbol)
            Assert.False(DirectCast(illegal, INamedTypeSymbol).IsSerializable)
            Assert.Equal("C31(Of I1).I31(Of C33)", illegal.UnderlyingSymbol.ToTestDisplayString())
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test4").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test5").ReturnType)
            assemblies = MetadataTestHelpers.GetSymbolsForReferences({TestReferences.SymbolsTests.NoPia.Pia1, Net40.mscorlib, varC_LocalTypes3})
            localTypes3 = assemblies(2).GlobalNamespace.GetTypeMembers("LocalTypes3").Single()
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test1").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test2").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test3").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMember(Of MethodSymbol)("Test4").ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test5").ReturnType)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localTypes3.GetMember(Of MethodSymbol)("Test6").ReturnType)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes5()
            Dim pia5Source As String = <text>
imports System.Reflection
imports System.Runtime.CompilerServices
imports System.Runtime.InteropServices
imports System.Collections.Generic

&lt;assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58259")&gt;
&lt;assembly: ImportedFromTypeLib("Pia5.dll")&gt;


&lt;ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c05"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)&gt;
public interface I5
    Function Foo() As List(Of I6)
end interface

&lt;ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c06"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)&gt;
public interface I6
end interface
            </text>.Value
            Dim pia1Source As String = <text>
imports System.Reflection
imports System.Runtime.CompilerServices
imports System.Runtime.InteropServices

&lt;assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")&gt;
&lt;assembly: ImportedFromTypeLib("Pia1.dll")&gt;


&lt;ComImport, Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c01"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)&gt;
public interface I1
    void Sub1(x As Integer)
End interface
            </text>.Value
            Dim library2Source As String = <text>
imports System.Collections.Generic
imports System.Reflection
imports System.Runtime.CompilerServices
imports System.Runtime.InteropServices

&lt;assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58260")&gt;
&lt;assembly: ImportedFromTypeLib("Library2.dll")&gt;


&lt;ComImport(), Guid("27e3e649-994b-4f58-b3c6-f8089a5f2002"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)&gt;
public interface I7
    Function Foo() As List(Of I5)
    Function Bar() As List(Of I1)
End interface
            </text>.Value
            Dim mscorlibRef = Net40.mscorlib
            ' vbc /t:library /vbruntime- Pia5.vb
            Dim varC_Pia5 = VisualBasicCompilation.Create("Pia5", {Parse(pia5Source)}, {mscorlibRef})
            Dim pia5Link = New VisualBasicCompilationReference(varC_Pia5, embedInteropTypes:=True)
            Dim pia5Ref = New VisualBasicCompilationReference(varC_Pia5, embedInteropTypes:=False)
            Assert.True(pia5Link.Properties.EmbedInteropTypes)
            Assert.False(pia5Ref.Properties.EmbedInteropTypes)

            ' vbc /t:library /vbruntime- Pia1.vb
            Dim varC_Pia1 = VisualBasicCompilation.Create("Pia1", {Parse(pia1Source)}, {mscorlibRef})
            Dim pia1Link = New VisualBasicCompilationReference(varC_Pia1, embedInteropTypes:=True)
            Dim pia1Ref = New VisualBasicCompilationReference(varC_Pia1, embedInteropTypes:=False)
            Assert.True(pia1Link.Properties.EmbedInteropTypes)
            Assert.False(pia1Ref.Properties.EmbedInteropTypes)

            ' vbc /t:library /vbruntime- /r:Pia1.dll,Pia5.dll Library2.vb
            Dim varC_Library2 = VisualBasicCompilation.Create("Library2", {Parse(library2Source)}, {mscorlibRef, pia1Ref, pia5Ref})
            Dim library2Ref = New VisualBasicCompilationReference(varC_Library2, embedInteropTypes:=False)
            Dim library2Link = New VisualBasicCompilationReference(varC_Library2, embedInteropTypes:=True)
            Assert.True(library2Link.Properties.EmbedInteropTypes)
            Assert.False(library2Ref.Properties.EmbedInteropTypes)

            Dim tc1 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, pia5Link})
            Dim pia5Asm1 = tc1.GetReferencedAssemblySymbol(pia5Link)
            Assert.True(pia5Asm1.IsLinked)
            Dim varI5_1 = pia5Asm1.GlobalNamespace.GetTypeMembers("I5").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI5_1.GetMember(Of MethodSymbol)("Foo").ReturnType)

            Dim tc2 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, pia5Ref})
            Dim pia5Asm2 = tc2.GetReferencedAssemblySymbol(pia5Ref)
            Assert.False(pia5Asm2.IsLinked)
            Assert.NotSame(pia5Asm1, pia5Asm2)
            Dim varI5_2 = pia5Asm2.GlobalNamespace.GetTypeMembers("I5").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI5_2.GetMember(Of MethodSymbol)("Foo").ReturnType.Kind)

            Dim tc3 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Ref})
            Dim pia5Asm3 = tc3.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm3 = tc3.GetReferencedAssemblySymbol(library2Ref)
            Assert.True(pia5Asm3.IsLinked)
            Assert.False(library2Asm3.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm3)
            Dim varI7_3 = library2Asm3.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_3.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_3.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)

            Dim tc4 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Ref, pia1Ref})
            Dim pia5Asm4 = tc4.GetReferencedAssemblySymbol(pia5Ref)
            Dim library2Asm4 = tc4.GetReferencedAssemblySymbol(library2Ref)
            Assert.False(pia5Asm4.IsLinked)
            Assert.False(library2Asm4.IsLinked)
            Assert.NotSame(pia5Asm3, pia5Asm4)
            Assert.Same(pia5Asm2, pia5Asm4)
            Assert.NotSame(library2Asm3, library2Asm4)
            Dim varI7_4 = library2Asm4.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMember(Of MethodSymbol)("Foo").ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)
            Dim tc5 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Link})
            Dim pia1Asm5 = tc5.GetReferencedAssemblySymbol(pia1Link)
            Dim pia5Asm5 = tc5.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm5 = tc5.GetReferencedAssemblySymbol(library2Ref)
            Assert.True(pia1Asm5.IsLinked)
            Assert.True(pia5Asm5.IsLinked)
            Assert.False(library2Asm5.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm5)
            Assert.NotSame(library2Asm5, library2Asm3)
            Assert.NotSame(library2Asm5, library2Asm4)
            Dim varI7_5 = library2Asm5.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_5.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_5.GetMember(Of MethodSymbol)("Bar").ReturnType)
            Dim tc6 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Ref, pia5Link, pia1Ref})
            Dim pia1Asm6 = tc6.GetReferencedAssemblySymbol(pia1Ref)
            Dim pia5Asm6 = tc6.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm6 = tc6.GetReferencedAssemblySymbol(library2Ref)
            Assert.False(pia1Asm6.IsLinked)
            Assert.True(pia5Asm6.IsLinked)
            Assert.False(library2Asm6.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm6)
            Assert.Same(library2Asm6, library2Asm3)
            Dim tc7 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, library2Link, pia5Link, pia1Ref})
            Dim pia5Asm7 = tc7.GetReferencedAssemblySymbol(pia5Link)
            Dim library2Asm7 = tc7.GetReferencedAssemblySymbol(library2Link)
            Assert.True(pia5Asm7.IsLinked)
            Assert.True(library2Asm7.IsLinked)
            Assert.Same(pia5Asm1, pia5Asm3)
            Assert.NotSame(library2Asm7, library2Asm3)
            Assert.NotSame(library2Asm7, library2Asm4)
            Assert.NotSame(library2Asm7, library2Asm5)
            Dim varI7_7 = library2Asm7.GlobalNamespace.GetTypeMembers("I7").Single()
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(varI7_7.GetMember(Of MethodSymbol)("Foo").ReturnType)
            Assert.NotEqual(SymbolKind.ErrorType, varI7_7.GetMember(Of MethodSymbol)("Bar").ReturnType.Kind)
            GC.KeepAlive(tc1)
            GC.KeepAlive(tc2)
            GC.KeepAlive(tc3)
            GC.KeepAlive(tc4)
            GC.KeepAlive(tc5)
            GC.KeepAlive(tc6)
            GC.KeepAlive(tc7)
            Dim varI5 = varC_Pia5.SourceModule.GlobalNamespace.GetTypeMembers("I5").Single()
            Dim varI5_Foo = varI5.GetMember(Of MethodSymbol)("Foo")
            Dim varI6 = varC_Pia5.SourceModule.GlobalNamespace.GetTypeMembers("I6").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI5.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, varI6.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, varI5_Foo.ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, (DirectCast(varI5_Foo.ReturnType, NamedTypeSymbol)).TypeArguments(0).Kind)
            Assert.Equal("System.Collections.Generic.List(Of I6)", varI5_Foo.ReturnType.ToTestDisplayString())
            Dim varI7 = varC_Library2.SourceModule.GlobalNamespace.GetTypeMembers("I7").Single()
            Dim varI7_Foo = varI7.GetMember(Of MethodSymbol)("Foo")
            Dim varI7_Bar = varI7.GetMember(Of MethodSymbol)("Bar")
            Assert.NotEqual(SymbolKind.ErrorType, varI7_Foo.ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, (DirectCast(varI7_Foo.ReturnType, NamedTypeSymbol)).TypeArguments(0).Kind)
            Assert.Equal("System.Collections.Generic.List(Of I5)", varI7_Foo.ReturnType.ToTestDisplayString())
            Assert.NotEqual(SymbolKind.ErrorType, varI7_Bar.ReturnType.Kind)
            Assert.NotEqual(SymbolKind.ErrorType, (DirectCast(varI7_Bar.ReturnType, NamedTypeSymbol)).TypeArguments(0).Kind)
            Assert.Equal("System.Collections.Generic.List(Of I1)", varI7_Bar.ReturnType.ToTestDisplayString())
            Dim varI1 = varC_Pia1.SourceModule.GlobalNamespace.GetTypeMembers("I1").Single()
            Assert.NotEqual(SymbolKind.ErrorType, varI1.Kind)
        End Sub

        <Fact()>
        Public Sub GenericsClosedOverLocalTypes6()
            Dim mscorlibRef = Net40.mscorlib
            Dim varC_A = VisualBasicCompilation.Create("A", references:={mscorlibRef})
            Dim varALink = New VisualBasicCompilationReference(varC_A, embedInteropTypes:=True)
            Dim varARef = New VisualBasicCompilationReference(varC_A, embedInteropTypes:=False)
            Dim varC_B = VisualBasicCompilation.Create("B", references:={mscorlibRef})
            Dim varBLink = New VisualBasicCompilationReference(varC_B, embedInteropTypes:=True)
            Dim varBRef = New VisualBasicCompilationReference(varC_B, embedInteropTypes:=False)
            Dim varC_C = VisualBasicCompilation.Create("C", references:={mscorlibRef, varARef, varBRef})
            Dim varCLink = New VisualBasicCompilationReference(varC_C, embedInteropTypes:=True)
            Dim varCRef = New VisualBasicCompilationReference(varC_C, embedInteropTypes:=False)
            Dim varC_D = VisualBasicCompilation.Create("D", references:={mscorlibRef})
            Dim varDLink = New VisualBasicCompilationReference(varC_D, embedInteropTypes:=True)
            Dim varDRef = New VisualBasicCompilationReference(varC_D, embedInteropTypes:=False)
            Dim tc1 = VisualBasicCompilation.Create("C1", references:={mscorlibRef, varCRef, varARef, varBLink})
            Dim varA1 = tc1.GetReferencedAssemblySymbol(varARef)
            Dim varB1 = tc1.GetReferencedAssemblySymbol(varBLink)
            Dim varC1 = tc1.GetReferencedAssemblySymbol(varCRef)
            Dim tc2 = VisualBasicCompilation.Create("C2", references:={mscorlibRef, varCRef, varARef, varDRef, varBLink})
            Assert.Same(varA1, tc2.GetReferencedAssemblySymbol(varARef))
            Assert.Same(varB1, tc2.GetReferencedAssemblySymbol(varBLink))
            Assert.Same(varC1, tc2.GetReferencedAssemblySymbol(varCRef))
            Dim varD2 = tc2.GetReferencedAssemblySymbol(varDRef)
            Dim tc3 = VisualBasicCompilation.Create("C3", references:={mscorlibRef, varCRef, varBLink})
            Assert.Same(varB1, tc3.GetReferencedAssemblySymbol(varBLink))
            Assert.True(tc3.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC1))
            Dim tc4 = VisualBasicCompilation.Create("C4", references:={mscorlibRef, varCRef, varARef, varBRef})
            Assert.Same(varA1, tc4.GetReferencedAssemblySymbol(varARef))
            Dim varB4 = tc4.GetReferencedAssemblySymbol(varBRef)
            Dim varC4 = tc4.GetReferencedAssemblySymbol(varCRef)
            Assert.NotSame(varC1, varC4)
            Assert.NotSame(varB1, varB4)
            Dim tc5 = VisualBasicCompilation.Create("C5", references:={mscorlibRef, varCRef, varALink, varBLink})
            Assert.Same(varB1, tc5.GetReferencedAssemblySymbol(varBLink))
            Dim varA5 = tc5.GetReferencedAssemblySymbol(varALink)
            Dim varC5 = tc5.GetReferencedAssemblySymbol(varCRef)
            Assert.NotSame(varA1, varA5)
            Assert.NotSame(varC1, varC5)
            Assert.NotSame(varC4, varC5)
            Dim tc6 = VisualBasicCompilation.Create("C6", references:={mscorlibRef, varARef, varBLink, varCLink})
            Assert.Same(varA1, tc6.GetReferencedAssemblySymbol(varARef))
            Assert.Same(varB1, tc6.GetReferencedAssemblySymbol(varBLink))
            Dim varC6 = tc6.GetReferencedAssemblySymbol(varCLink)
            Assert.NotSame(varC1, varC6)
            Assert.NotSame(varC4, varC6)
            Assert.NotSame(varC5, varC6)
            Dim tc7 = VisualBasicCompilation.Create("C7", references:={mscorlibRef, varCRef, varARef})
            Assert.Same(varA1, tc7.GetReferencedAssemblySymbol(varARef))
            Assert.True(tc7.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC4))
            GC.KeepAlive(tc1)
            GC.KeepAlive(tc2)
            GC.KeepAlive(tc3)
            GC.KeepAlive(tc4)
            GC.KeepAlive(tc5)
            GC.KeepAlive(tc6)
            GC.KeepAlive(tc7)
        End Sub

        <WorkItem(546735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546735")>
        <ConditionalFact(GetType(DesktopOnly), Reason:=ConditionalSkipReason.NoPiaNeedsDesktop)>
        Public Sub Bug16689_1()
            Dim ilSource =
            <![CDATA[
.class interface public abstract auto ansi import I1
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public newslot abstract strict virtual 
          instance void  M1() cil managed
  {
  } // end of method I1::M1

} // end of class I1

.class public auto ansi Base
       extends [mscorlib]System.Object
       implements I1
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual final 
          instance void  M1() cil managed
  {
    .override I1::M1
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Base::M1

} // end of class Base
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Class Derived
	Inherits Base
End Class

Class Program
	Shared Sub Main()
		Dim x as New Derived()
		System.Console.WriteLine(x)
	End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Derived
]]>)
        End Sub

        <Fact(), WorkItem(546735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546735")>
        Public Sub Bug16689_2()
            Dim ilSource =
            <![CDATA[
.class interface public abstract auto ansi import I1
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class I1

.class interface public abstract auto ansi I2`1<T>
{
  .method public newslot abstract strict virtual 
          instance void  M1() cil managed
  {
  } // end of method I2`1::M1

} // end of class I2`1

.class public auto ansi Base
       extends [mscorlib]System.Object
       implements class I2`1<class I1>
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual final 
          instance void  M1() cil managed
  {
    .override  method instance void class I2`1<class I1>::M1()
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Base::M1

} // end of class Base
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Class Derived
	Inherits Base
End Class

Class Program
	Shared Sub Main()
		Dim x as New Derived()
		System.Console.WriteLine(x)
	End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Derived
]]>)
        End Sub

        <WorkItem(546735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546735")>
        <ConditionalFact(GetType(DesktopOnly), Reason:=ConditionalSkipReason.NoPiaNeedsDesktop)>
        Public Sub Bug16689_3()

            Dim i3Def =
<compilation name="I3">
    <file name="a.vb">
Public Interface I3
End Interface
    </file>
</compilation>

            Dim i3Compilation = CreateCompilationWithMscorlib40(i3Def, options:=TestOptions.ReleaseDll)

            Dim ilSource =
            <![CDATA[
.assembly extern I3{}

.class interface public abstract auto ansi import I1
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class I1

.class interface public abstract auto ansi I2`2<T,S>
{
  .method public newslot abstract strict virtual 
          instance void  M1() cil managed
  {
  } // end of method I2`2::M1

} // end of class I2`2

.class public auto ansi Base
       extends [mscorlib]System.Object
       implements class I2`2<class I1,class [I3]I3>
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual final 
          instance void  M1() cil managed
  {
    .override  method instance void class I2`2<class I1,class [I3]I3>::M1()
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Base::M1

} // end of class Base
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Class Derived
    Inherits Base
End Class

Class Program
	Shared Sub Main()
		Dim x as New Derived()
		System.Console.WriteLine(x)
	End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
                             dependencies:={New ModuleData(i3Compilation.Assembly.Identity,
                                                            OutputKind.DynamicallyLinkedLibrary,
                                                            i3Compilation.EmitToArray(),
                                                            Nothing,
                                                            False,
                                                            False)},
                             expectedOutput:=
            <![CDATA[
Derived
]]>)
        End Sub

        <Fact>
        <WorkItem(62863, "https://github.com/dotnet/roslyn/issues/62863")>
        Public Sub ExplicitInterfaceImplementations()
            Dim sourcePIA =
"Imports System.Runtime.InteropServices
<assembly: PrimaryInteropAssembly(0, 0)>
<assembly: Guid(""863D5BC0-46A1-49AC-97AA-A5F0D441A9DA"")>
<ComImport>
<Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9DA"")>
public interface I1
    Function F1() As Integer
end interface
"
            Dim sourceBase =
"
public class C
    public Function F1() As Long
        Return 0
    End Function
end class

public class Base
    Inherits C
    Implements I1

    Function I1F1() As Integer Implements I1.F1
        throw new System.NotImplementedException()
    end Function
end class
"
            Dim verify = Sub(compilationDerived As VisualBasicCompilation)
                             Dim i1F1 = compilationDerived.GetTypeByMetadataName("I1").GetMember(Of MethodSymbol)("F1")
                             Dim baseI1F1 = compilationDerived.GetTypeByMetadataName("Base").GetMember(Of MethodSymbol)("I1F1")
                             Assert.Same(i1F1, baseI1F1.ExplicitInterfaceImplementations.Single())
                             compilationDerived.AssertNoDiagnostics()
                         End Sub

            Dim compilationPIA = CreateCompilation(sourcePIA, options:=TestOptions.DebugDll)
            compilationPIA.AssertNoDiagnostics()

            Dim referencePIAImage = compilationPIA.EmitToImageReference(embedInteropTypes:=true)
            Dim referencePIASource = compilationPIA.ToMetadataReference(embedInteropTypes:=True)

            Dim compilationBase = CreateCompilation(sourceBase, {referencePIASource}, TestOptions.DebugDll)
            compilationBase.AssertNoDiagnostics()

            Dim referenceBaseImage = compilationBase.EmitToImageReference()
            Dim referenceBaseSource = compilationBase.ToMetadataReference()

            Dim sourceDerived =
"
public interface I2
    Inherits I1
End Interface

public class Derived
    Inherits Base
    Implements I2
end class
"
            Dim compilationDerived1 = CreateCompilation(sourceDerived, {referencePIASource, referenceBaseSource}, TestOptions.DebugDll)
            verify(compilationDerived1)

            Dim compilationDerived2 = CreateCompilation(sourceDerived, {referencePIAImage, referenceBaseSource}, TestOptions.DebugDll)
            verify(compilationDerived2)

            Dim compilationDerived3 = CreateCompilation(sourceDerived, {referencePIASource, referenceBaseImage}, TestOptions.DebugDll)
            verify(compilationDerived3)

            Dim compilationDerived4 = CreateCompilation(sourceDerived, {referencePIAImage, referenceBaseImage}, TestOptions.DebugDll)
            verify(compilationDerived4)
        End Sub

    End Class

End Namespace
