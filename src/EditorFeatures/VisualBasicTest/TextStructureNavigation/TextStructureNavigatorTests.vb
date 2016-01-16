' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.TextStructureNavigation
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TextStructureNavigation
    Public Class TextStructureNavigatorTests

        <Fact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestEmpty() As Task
            Await AssertExtentAsync(
                String.Empty,
                pos:=0,
                isSignificant:=False,
                start:=0, length:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestWhitespace() As Task
            Await AssertExtentAsync(
                "   ",
                pos:=0,
                isSignificant:=False,
                start:=0, length:=3)

            Await AssertExtentAsync(
                "   ",
                pos:=1,
                isSignificant:=False,
                start:=0, length:=3)

            Await AssertExtentAsync(
                "   ",
                pos:=3,
                isSignificant:=False,
                start:=0, length:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestEndOfFile() As Task
            Await AssertExtentAsync(
                "Imports System",
                pos:=14,
                isSignificant:=True,
                start:=8, length:=6)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestNewLine() As Task
            Await AssertExtentAsync(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=14,
                isSignificant:=False,
                start:=14, length:=2)

            Await AssertExtentAsync(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=15,
                isSignificant:=False,
                start:=14, length:=2)

            Await AssertExtentAsync(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=16,
                isSignificant:=False,
                start:=16, length:=2)

            Await AssertExtentAsync(
                "Module Module1" & vbCrLf & vbCrLf & "End Module",
                pos:=17,
                isSignificant:=False,
                start:=16, length:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestComment() As Task
            Await AssertExtentAsync(
                " ' Comment  ",
                pos:=1,
                isSignificant:=True,
                start:=1, length:=11)

            Await AssertExtentAsync(
                " ' Comment  ",
                pos:=5,
                isSignificant:=True,
                start:=3, length:=7)

            Await AssertExtentAsync(
                " ' () test",
                pos:=4,
                isSignificant:=True,
                start:=3, length:=2)

            Await AssertExtentAsync(
                " REM () test",
                pos:=1,
                isSignificant:=True,
                start:=1, length:=11)

            Await AssertExtentAsync(
                " rem () test",
                pos:=6,
                isSignificant:=True,
                start:=5, length:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestKeyword() As Task
            For i = 7 To 12
                Await AssertExtentAsync(
                    "Public Module Module1",
                    pos:=i,
                    isSignificant:=True,
                    start:=7, length:=6)
            Next
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestIdentifier() As Task
            For i = 13 To 13 + 8
                Await AssertExtentAsync(
                    "Public Class SomeClass : Inherits Object",
                    pos:=i,
                    isSignificant:=True,
                    start:=13, length:=9)
            Next
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestEscapedIdentifier() As Task
            For i = 12 To 12 + 7
                Await AssertExtentAsync(
                    "Friend Enum [Module] As Long",
                    pos:=i,
                    isSignificant:=True,
                    start:=12, length:=8)
            Next
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestNumber() As Task
            For i = 37 To 37 + 12
                Await AssertExtentAsync(
                    "Class Test : Dim number As Double = -1.234678E-120 : End Class",
                    pos:=i,
                    isSignificant:=True,
                    start:=37, length:=13)
            Next
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestString() As Task
            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=33,
                isSignificant:=True,
                start:=33, length:=1)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=34,
                isSignificant:=False,
                start:=34, length:=1)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=35,
                isSignificant:=True,
                start:=35, length:=2)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=43,
                isSignificant:=False,
                start:=42, length:=2)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=44,
                isSignificant:=True,
                start:=44, length:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)>
        Public Async Function TestInterpolatedString() As Task
            Await AssertExtentAsync(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=33,
                isSignificant:=True,
                start:=33, length:=2)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=35,
                isSignificant:=False,
                start:=35, length:=1)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=36,
                isSignificant:=True,
                start:=36, length:=2)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = $"" () test  "" : End Class",
                pos:=44,
                isSignificant:=False,
                start:=43, length:=2)

            Await AssertExtentAsync(
                "Class Test : Dim str As String = "" () test  "" : End Class",
                pos:=45,
                isSignificant:=False,
                start:=45, length:=1)
        End Function

        Private Shared Async Function AssertExtentAsync(
            code As String,
            pos As Integer,
            isSignificant As Boolean,
            start As Integer,
            length As Integer) As Task

            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code)
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
        End Function

    End Class

End Namespace
