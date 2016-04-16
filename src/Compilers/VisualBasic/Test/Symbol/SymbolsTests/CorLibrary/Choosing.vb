' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
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
                                TestResources.NetFX.v4_0_21006.mscorlib
                             })

            Assert.Same(assemblies(1), DirectCast(assemblies(0).Modules(0), PEModuleSymbol).CorLibrary)
        End Sub

        <Fact, WorkItem(760148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760148")>
        Public Sub Bug760148_1()
            Dim corLib = CompilationUtils.CreateCompilationWithoutReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Public class Object
    End Class
End Namespace
    </file>
</compilation>, TestOptions.ReleaseDll)

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
</compilation>, {New VisualBasicCompilationReference(corLib)}, TestOptions.ReleaseDll)

            Assert.Same(obj, consumer.GetSpecialType(SpecialType.System_Object))
        End Sub

        <Fact, WorkItem(760148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760148")>
        Public Sub Bug760148_2()
            Dim corLib = CompilationUtils.CreateCompilationWithoutReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Class Object
    End Class
End Namespace
    </file>
</compilation>, TestOptions.ReleaseDll)

            Dim obj = corLib.GetSpecialType(SpecialType.System_Object)

            Dim consumer = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Namespace System
    Public class Object
    End Class
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(corLib)}, TestOptions.ReleaseDll)

            Assert.True(consumer.GetSpecialType(SpecialType.System_Object).IsErrorType())
        End Sub

    End Class

End Namespace
