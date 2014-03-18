' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata
Imports Roslyn.Test.Utilities
Imports VBReferenceManager = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ReferenceManager

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.CorLibrary

    Public Class Choosing
        Inherits BasicTestBase

        <Fact()>
        Public Sub MultipleMscorlibReferencesInMetadata()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                             {
                                TestResources.SymbolsTests.CorLibrary.GuidTest2,
                                ProprietaryTestResources.NetFX.v4_0_21006.mscorlib
                             })

            Assert.Same(assemblies(1), DirectCast(assemblies(0).Modules(0), PEModuleSymbol).CorLibrary)
        End Sub


        <Fact()>
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

        <Fact, WorkItem(760148)>
        Public Sub Bug760148_1()
            Dim corLib = CompilationUtils.CreateCompilationWithoutReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Public class Object
    End Class
End Namespace
    </file>
</compilation>, OptionsDll)

            Dim obj = corLib.GetSpecialType(SpecialType.System_Object)

            Assert.False(obj.IsErrorType())
            Assert.Same(corLib.Assembly, obj.ContainingAssembly)

            Dim consumer = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Public class Object
    End Class
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(corLib)}, OptionsDll)

            Assert.Same(obj, consumer.GetSpecialType(SpecialType.System_Object))
        End Sub

        <Fact, WorkItem(760148)>
        Public Sub Bug760148_2()
            Dim corLib = CompilationUtils.CreateCompilationWithoutReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Class Object
    End Class
End Namespace
    </file>
</compilation>, OptionsDll)

            Dim obj = corLib.GetSpecialType(SpecialType.System_Object)

            Dim consumer = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Public class Object
    End Class
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(corLib)}, OptionsDll)

            Assert.True(consumer.GetSpecialType(SpecialType.System_Object).IsErrorType())
        End Sub

    End Class

End Namespace
