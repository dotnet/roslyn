' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CompilationAPITests
        Inherits BasicTestBase

        <Fact()>
        Public Sub GetTypesByMetadataName_NotInSourceNotInReferences()
            Dim comp = CreateCompilation("")
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")
            Assert.Empty(types)
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_SingleInSourceNotInReferences(useMetadataReferences As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim referenceComp = CreateCompilation("")

            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim comp = CreateCompilation(source, {If(useMetadataReferences, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Single(types)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_MultipleInSourceNotInReferences(useMetadataReferences As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim referenceComp = CreateCompilation("")

            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim comp = CreateCompilation(source, {If(useMetadataReferences, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())})
            comp.AssertTheseDiagnostics(
<expected>
BC30179: class 'C' and class 'C' conflict in namespace 'N'.
    <%= accessibility %> Class C(Of T)
                 ~
</expected>)

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Single(types)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            Assert.Equal(2, types(0).Locations.Length)
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_SingleInSourceSingleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp = CreateCompilation(source)
            Dim reference As MetadataReference = If(useMetadataReference, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())
            Dim comp = CreateCompilation(source, {reference})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(2, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            Assert.Same(comp.Assembly, types(0).ContainingAssembly)
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())

            Dim referenceAssembly = comp.GetAssemblyOrModuleSymbol(reference)
            Assert.Same(types(1).ContainingAssembly, referenceAssembly)
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_NotInSourceSingleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp = CreateCompilation(source)
            Dim reference As MetadataReference = GetReference(useMetadataReference, referenceComp)
            Dim comp = CreateCompilation("", {reference})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Single(types)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())

            Dim referenceAssembly = comp.GetAssemblyOrModuleSymbol(reference)
            Assert.Same(types(0).ContainingAssembly, referenceAssembly)
        End Sub

        Private Shared Function GetReference(useMetadataReference As Boolean, referenceComp As VisualBasicCompilation) As MetadataReference
            Return If(useMetadataReference, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())
        End Function

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_NotInSourceMultipleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp1 = CreateCompilation(source)
            Dim referenceComp2 = CreateCompilation(source)
            Dim reference1 As MetadataReference = GetReference(useMetadataReference, referenceComp1)
            Dim reference2 As MetadataReference = GetReference(useMetadataReference, referenceComp2)
            Dim comp = CreateCompilation("", {reference1, reference2})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(2, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())

            Dim referenceAssembly1 = comp.GetAssemblyOrModuleSymbol(reference1)
            Assert.Same(types(0).ContainingAssembly, referenceAssembly1)

            Dim referenceAssembly2 = comp.GetAssemblyOrModuleSymbol(reference2)
            Assert.Same(types(1).ContainingAssembly, referenceAssembly2)

            If (useMetadataReference) Then
            Else
                Assert.False(types(0).IsInSource())
                Assert.False(types(1).IsInSource())
            End If
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadataName_SingleInSourceMultipleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp1 = CreateCompilation(source)
            Dim referenceComp2 = CreateCompilation(source)
            Dim reference1 As MetadataReference = GetReference(useMetadataReference, referenceComp1)
            Dim reference2 As MetadataReference = GetReference(useMetadataReference, referenceComp2)
            Dim comp = CreateCompilation(source, {reference1, reference2})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(3, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            Assert.Same(comp.Assembly, types(0).ContainingAssembly)
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())
            AssertEx.Equal("N.C(Of T)", types(2).ToTestDisplayString())

            Dim referenceAssembly1 = comp.GetAssemblyOrModuleSymbol(reference1)
            Assert.Same(types(1).ContainingAssembly, referenceAssembly1)

            Dim referenceAssembly2 = comp.GetAssemblyOrModuleSymbol(reference2)
            Assert.Same(types(2).ContainingAssembly, referenceAssembly2)
        End Sub

        <Fact>
        Public Sub GetTypesByMetadataName_Ordering()
            Dim corlibSource = "
Namespace System
    Public Class [Object]
    End Class
    Public Class [Void]
    End Class
End Namespace
Public Class C
End Class
"

            Dim corlib = CreateEmptyCompilation(corlibSource)
            Dim corlibReference = corlib.EmitToImageReference()

            Dim otherSource = "
Public Class C
End Class
"

            Dim other = CreateEmptyCompilation(otherSource, {corlibReference})
            Dim otherReference = other.EmitToImageReference()

            Dim currentSource = "
Public Class C
End Class
"
            Dim current = CreateEmptyCompilation(currentSource, {otherReference, corlibReference})
            current.AssertNoDiagnostics()

            Dim types = current.GetTypesByMetadataName("C")

            AssertEx.Equal(types.Select(Function(t) t.ToTestDisplayString()), {"C", "C", "C"})

            Assert.Same(current.Assembly, types(0).ContainingAssembly)

            Dim corlibAssembly = current.GetAssemblyOrModuleSymbol(corlibReference)
            Assert.Same(types(1).ContainingAssembly, corlibAssembly)

            Dim otherAssembly = current.GetAssemblyOrModuleSymbol(otherReference)
            Assert.Same(types(2).ContainingAssembly, otherAssembly)
        End Sub
    End Class
End Namespace
