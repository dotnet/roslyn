' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.IO
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SerializationTests

        Private Sub RoundTrip(text As String)
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim root = tree.GetRoot()

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Dim dtext = droot.ToFullString()

            Assert.Equal(text, dtext)
            Assert.True(droot.IsEquivalentTo(tree.GetRoot()))
        End Sub

        <Fact>
        Public Sub TestRoundTripSyntaxNode()
            RoundTrip(<Foo>
Public Class C
End Class
</Foo>.Value)
        End Sub

        <Fact>
        Public Sub TestRoundTripSyntaxNodeWithDiagnostics()
            Dim text = <Foo>
Public Class C
End 
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim root = tree.GetVisualBasicRoot()
            Assert.True(root.HasErrors)

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Dim dtext = droot.ToFullString()

            Assert.Equal(text, dtext)
            Assert.True(DirectCast(droot, VisualBasicSyntaxNode).HasErrors)
            Assert.True(droot.IsEquivalentTo(tree.GetRoot()))
        End Sub

        <Fact>
        Public Sub TestRoundTripSyntaxNodeWithAnnotation()
            Dim text = <Foo>
Public Class C
End Class
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim annotation = New SyntaxAnnotation()
            Dim root = tree.GetRoot().WithAdditionalAnnotations(annotation)
            Assert.True(root.ContainsAnnotations)
            Assert.True(root.HasAnnotation(annotation))

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Dim dtext = droot.ToFullString()

            Assert.Equal(text, dtext)
            Assert.True(droot.ContainsAnnotations)
            Assert.True(droot.HasAnnotation(annotation))
            Assert.True(droot.IsEquivalentTo(tree.GetRoot()))
        End Sub

        <Fact>
        Public Sub TestRoundTripSyntaxNodeWithMultipleInstancesOfTheSameAnnotation()
            Dim text = <Foo>
Public Class C
End Class
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim annotation = New SyntaxAnnotation()
            Dim root = tree.GetRoot().WithAdditionalAnnotations(annotation, annotation)
            Assert.True(root.ContainsAnnotations)
            Assert.True(root.HasAnnotation(annotation))

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Dim dtext = droot.ToFullString()

            Assert.Equal(text, dtext)
            Assert.True(droot.ContainsAnnotations)
            Assert.True(droot.HasAnnotation(annotation))
            Assert.True(droot.IsEquivalentTo(tree.GetRoot()))
        End Sub

        <Fact>
        Public Sub RoundTripSyntaxNodeWithAnnotationsRemoved()
            Dim text = <Foo>
Public Class C
End Class
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim annotation1 = New SyntaxAnnotation("annotation1")
            Dim root = tree.GetRoot().WithAdditionalAnnotations(annotation1)
            Assert.Equal(True, root.ContainsAnnotations)
            Assert.Equal(True, root.HasAnnotation(annotation1))
            Dim removedRoot = root.WithoutAnnotations(annotation1)
            Assert.Equal(False, removedRoot.ContainsAnnotations)
            Assert.Equal(False, removedRoot.HasAnnotation(annotation1))

            Dim stream = New MemoryStream()
            removedRoot.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)

            Assert.Equal(False, droot.ContainsAnnotations)
            Assert.Equal(False, droot.HasAnnotation(annotation1))

            Dim annotation2 = New SyntaxAnnotation("annotation2")

            Dim doubleAnnoRoot = droot.WithAdditionalAnnotations(annotation1, annotation2)
            Assert.Equal(True, doubleAnnoRoot.ContainsAnnotations)
            Assert.Equal(True, doubleAnnoRoot.HasAnnotation(annotation1))
            Assert.Equal(True, doubleAnnoRoot.HasAnnotation(annotation2))
            Dim removedDoubleAnnoRoot = doubleAnnoRoot.WithoutAnnotations(annotation1, annotation2)
            Assert.Equal(False, removedDoubleAnnoRoot.ContainsAnnotations)
            Assert.Equal(False, removedDoubleAnnoRoot.HasAnnotation(annotation1))
            Assert.Equal(False, removedDoubleAnnoRoot.HasAnnotation(annotation2))

            stream = New MemoryStream()
            removedRoot.SerializeTo(stream)

            stream.Position = 0

            droot = VisualBasicSyntaxNode.DeserializeFrom(stream)

            Assert.Equal(False, droot.ContainsAnnotations)
            Assert.Equal(False, droot.HasAnnotation(annotation1))
            Assert.Equal(False, droot.HasAnnotation(annotation2))
        End Sub

        <Fact>
        Public Sub RoundTripSyntaxNodeWithAnnotationRemovedWithMultipleReference()
            Dim text = <Foo>
Public Class C
End Class
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim annotation1 = New SyntaxAnnotation("annotation1")
            Dim root = tree.GetRoot().WithAdditionalAnnotations(annotation1, annotation1)
            Assert.Equal(True, root.ContainsAnnotations)
            Assert.Equal(True, root.HasAnnotation(annotation1))
            Dim removedRoot = root.WithoutAnnotations(annotation1)
            Assert.Equal(False, removedRoot.ContainsAnnotations)
            Assert.Equal(False, removedRoot.HasAnnotation(annotation1))

            Dim stream = New MemoryStream()
            removedRoot.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)

            Assert.Equal(False, droot.ContainsAnnotations)
            Assert.Equal(False, droot.HasAnnotation(annotation1))
        End Sub

        <Fact()>
        Public Sub TestRoundTripSyntaxNodeWithSpecialAnnotation()
            Dim text = <Foo>
Public Class C
End Class
</Foo>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim annotation = New SyntaxAnnotation("TestAnnotation", "this is a test")
            Dim root = tree.GetRoot().WithAdditionalAnnotations(annotation)
            Assert.True(root.ContainsAnnotations)
            Assert.True(root.HasAnnotation(annotation))

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim droot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Dim dtext = droot.ToFullString()

            Assert.Equal(text, dtext)
            Assert.True(droot.ContainsAnnotations)
            Assert.True(droot.HasAnnotation(annotation))
            Assert.True(droot.IsEquivalentTo(tree.GetRoot()))

            Dim dannotation = droot.GetAnnotations("TestAnnotation").SingleOrDefault()
            Assert.NotNull(dannotation)
            Assert.NotSame(annotation, dannotation) ' not same instance
            Assert.Equal(annotation, dannotation) ' but are equivalent
        End Sub

        <ConditionalFact(GetType(x86))>
        <WorkItem(530374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530374")>
        Public Sub RoundtripSerializeDeepExpression()
            Dim text = <Foo><![CDATA[
Module Module15
    Declare Function GetDesktopWindow Lib "User32" () As Integer
    Declare Function EnumChildWindows Lib "User32" (ByVal hw As Integer, ByVal lpWndProc As mydel, ByVal lp As Integer) As Integer
    Public intCounter As Integer
    Delegate Function mydel(ByVal hw As Integer, ByVal lp As Integer) As Integer
    Public d As mydel = New mydel(AddressOf EnumChildProc)
    
    Sub Main()
        Dim x As Object
        intCounter = 0
        
        Dim hw As Integer
        hw = GetDesktopWindow()
        
        'Call API passing ptr to callback
        x = EnumChildWindows(hw, d, 5)
        'This should always be true, I would think
        If intCounter < 10 Then
		intcounter = 10
        End If        
    End Sub
    'Callback function for EnumWindows
    Function EnumChildProc(ByVal hw As Integer, ByVal lp As Integer) As Integer
        intCounter = intCounter + 1
        EnumChildProc = 1
    End Function
    Sub Regression41614()
        Dim abc = "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & _
            "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" & "abc" 


    End Sub
End Module
]]>
                       </Foo>.Value
            RoundTrip(text)
        End Sub

        <Fact>
        <WorkItem(530374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530374")>
        Public Sub RoundtripSerializeDeepExpression2()
            Dim text = <Foo><![CDATA[
Module GroupJoin2
    Sub Test1()
        q = From a In aa Group Join b As $ In bb On a Equals b
    End Sub
End Module
]]>
                       </Foo>.Value
            RoundTrip(text)
        End Sub

        <Fact>
        <WorkItem(1038237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038237")>
        Public Sub RoundTripPragmaDirective()
            Dim text = <Foo><![CDATA[
#Disable Warning BC40000
]]>
                       </Foo>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim root = tree.GetRoot()
            Assert.True(root.ContainsDirectives)

            Dim stream = New MemoryStream()
            root.SerializeTo(stream)

            stream.Position = 0

            Dim newRoot = VisualBasicSyntaxNode.DeserializeFrom(stream)
            Assert.True(newRoot.ContainsDirectives)
        End Sub
    End Class
End Namespace
