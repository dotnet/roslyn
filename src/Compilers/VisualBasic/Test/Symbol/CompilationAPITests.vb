' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CompilationAPITests
        Inherits BasicTestBase

        <Fact()>
        Public Sub GetTypesByMetadtaName_NotInSourceNotInReferences()
            Dim comp = CreateCompilation("")
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")
            Assert.Empty(types)
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadtaName_SingleInSourceNotInReferences(useMetadataReferences As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
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
        Public Sub GetTypesByMetadtaName_MultipleInSourceNotInReferences(useMetadataReferences As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
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
        Public Sub GetTypesByMetadtaName_SingleInSourceSingleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp = CreateCompilation(source)
            Dim comp = CreateCompilation(source, {If(useMetadataReference, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(2, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            Assert.Same(comp.Assembly, types(0).ContainingAssembly)
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())
            If (useMetadataReference) Then
                Assert.Same(referenceComp.Assembly, types(1).ContainingAssembly)
            Else
                Assert.False(types(1).IsInSource())
            End If
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadtaName_NotInSourceSingleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp = CreateCompilation(source)
            Dim comp = CreateCompilation("", {GetReference(useMetadataReference, referenceComp)})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Single(types)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            If (useMetadataReference) Then
                Assert.Same(referenceComp.Assembly, types(0).ContainingAssembly)
            Else
                Assert.False(types(0).IsInSource())
            End If
        End Sub

        Private Shared Function GetReference(useMetadataReference As Boolean, referenceComp As VisualBasicCompilation) As MetadataReference
            Return If(useMetadataReference, referenceComp.ToMetadataReference(), referenceComp.EmitToImageReference())
        End Function

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadtaName_NotInSourceMultipleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp1 = CreateCompilation(source)
            Dim referenceComp2 = CreateCompilation(source)
            Dim comp = CreateCompilation("", {GetReference(useMetadataReference, referenceComp1), GetReference(useMetadataReference, referenceComp2)})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(2, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())
            If (useMetadataReference) Then
                Assert.Same(referenceComp1.Assembly, types(0).ContainingAssembly)
                Assert.Same(referenceComp2.Assembly, types(1).ContainingAssembly)
            Else
                Assert.False(types(0).IsInSource())
                Assert.False(types(1).IsInSource())
            End If
        End Sub

        <Theory, CombinatorialData>
        Public Sub GetTypesByMetadtaName_SingleInSourceMultipleInReferences(useMetadataReference As Boolean, <CombinatorialValues("Public", "Friend")> accessibility As String)
            Dim source =
$"Namespace N
    {accessibility} Class C(Of T)
    End Class
End Namespace"

            Dim referenceComp1 = CreateCompilation(source)
            Dim referenceComp2 = CreateCompilation(source)
            Dim comp = CreateCompilation(source, {GetReference(useMetadataReference, referenceComp1), GetReference(useMetadataReference, referenceComp2)})
            comp.AssertNoDiagnostics()

            Dim types = comp.GetTypesByMetadataName("N.C`1")

            Assert.Equal(3, types.Length)
            AssertEx.Equal("N.C(Of T)", types(0).ToTestDisplayString())
            Assert.Same(comp.Assembly, types(0).ContainingAssembly)
            AssertEx.Equal("N.C(Of T)", types(1).ToTestDisplayString())
            AssertEx.Equal("N.C(Of T)", types(2).ToTestDisplayString())
            If (useMetadataReference) Then
                Assert.Same(referenceComp1.Assembly, types(1).ContainingAssembly)
                Assert.Same(referenceComp2.Assembly, types(2).ContainingAssembly)
            Else
                Assert.False(types(1).IsInSource())
                Assert.False(types(2).IsInSource())
            End If
        End Sub
    End Class
End Namespace
