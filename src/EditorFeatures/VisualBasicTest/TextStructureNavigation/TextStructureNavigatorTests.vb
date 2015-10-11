' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.TextStructureNavigation
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TextStructureNavigation
    Public Class TextStructureNavigatorTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Empty()
            AssertExtent(
                String.Empty,
                pos:=0,
                isSignificant:=False,
                start:=0, length:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Whitespace()
            AssertExtent(
                "   ",
                pos:=0,
                isSignificant:=False,
                start:=0, length:=3)

            AssertExtent(
                "   ",
                pos:=1,
                isSignificant:=False,
                start:=0, length:=3)

            AssertExtent(
                "   ",
                pos:=3,
                isSignificant:=False,
                start:=0, length:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub EndOfFile()
            AssertExtent(
                "Imports System",
                pos:=14,
                isSignificant:=True,
                start:=8, length:=6)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub NewLine()
            AssertExtent(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=14,
                isSignificant:=False,
                start:=14, length:=2)

            AssertExtent(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=15,
                isSignificant:=False,
                start:=14, length:=2)

            AssertExtent(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=16,
                isSignificant:=False,
                start:=16, length:=2)

            AssertExtent(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=17,
                isSignificant:=False,
                start:=16, length:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Comment()
            AssertExtent(
                " ' Comment  ",
                pos:=1,
                isSignificant:=True,
                start:=1, length:=11)

            AssertExtent(
                " ' Comment  ",
                pos:=5,
                isSignificant:=True,
                start:=3, length:=7)

            AssertExtent(
                " ' () test",
                pos:=4,
                isSignificant:=True,
                start:=3, length:=2)

            AssertExtent(
                " REM () test",
                pos:=1,
                isSignificant:=True,
                start:=1, length:=11)

            AssertExtent(
                " rem () test",
                pos:=6,
                isSignificant:=True,
                start:=5, length:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Keyword()
            For i = 7 To 12
                AssertExtent(
                    "Public Module Module1",
                    pos:=i,
                    isSignificant:=True,
                    start:=7, length:=6)
            Next
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Identifier()
            For i = 13 To 13 + 8
                AssertExtent(
                    "Public Class SomeClass : Inherits Object",
                    pos:=i,
                    isSignificant:=True,
                    start:=13, length:=9)
            Next
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub EscapedIdentifier()
            For i = 12 To 12 + 7
                AssertExtent(
                    "Friend Enum [Module] As Long",
                    pos:=i,
                    isSignificant:=True,
                    start:=12, length:=8)
            Next
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub Number()
            For i = 37 To 37 + 12
                AssertExtent(
                    "Class Test : Dim number As Double = -1.234678E-120 : End Class",
                    pos:=i,
                    isSignificant:=True,
                    start:=37, length:=13)
            Next
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub [String]()
            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=33,
                isSignificant:=True,
                start:=33, length:=1)

            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=34,
                isSignificant:=False,
                start:=34, length:=1)

            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=35,
                isSignificant:=True,
                start:=35, length:=2)

            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=43,
                isSignificant:=False,
                start:=42, length:=2)

            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=44,
                isSignificant:=True,
                start:=44, length:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Sub InterpolatedString()
            AssertExtent(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=33,
                isSignificant:=True,
                start:=33, length:=2)

            AssertExtent(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=35,
                isSignificant:=False,
                start:=35, length:=1)

            AssertExtent(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=36,
                isSignificant:=True,
                start:=36, length:=2)

            AssertExtent(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=44,
                isSignificant:=False,
                start:=43, length:=2)

            AssertExtent(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=45,
                isSignificant:=False,
                start:=45, length:=1)
        End Sub

        Private Shared Sub AssertExtent(
            code As String,
            pos As Integer,
            isSignificant As Boolean,
            start As Integer,
            length As Integer)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                Dim provider = New TextStructureNavigatorProvider(
                               workspace.GetService(Of ITextStructureNavigatorSelectorService),
                               workspace.GetService(Of IContentTypeRegistryService),
                               workspace.GetService(Of IWaitIndicator))

                Dim navigator = provider.CreateTextStructureNavigator(buffer)

                Dim extent = navigator.GetExtentOfWord(New SnapshotPoint(buffer.CurrentSnapshot, pos))

                Assert.Equal(isSignificant, extent.IsSignificant)

                Dim expectedSpan As New SnapshotSpan(buffer.CurrentSnapshot, start, length)
                Assert.Equal(expectedSpan, extent.Span)
            End Using
        End Sub

    End Class

End Namespace
