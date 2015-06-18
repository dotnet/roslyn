' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class EndConstructCommandHandlerTests
        Private _endConstructServiceMock As New Mock(Of IEndConstructGenerationService)
        Private _featureOptions As New Mock(Of IOptionService)(MockBehavior.Strict)
        Private _textViewMock As New Mock(Of ITextView)
        Private _textBufferMock As New Mock(Of ITextBuffer)

#If False Then
        ' TODO(jasonmal): Figure out how to enable these tests.
        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ServiceNotCompletingShouldCallNextHandler()
            _endConstructServiceMock.Setup(Function(s) s.TryDo(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of Char))).Returns(False)
            _featureOptions.Setup(Function(s) s.GetOption(FeatureOnOffOptions.EndConstruct)).Returns(True)

            Dim nextHandlerCalled = False
            Dim handler As New EndConstructCommandHandler(_featureOptions.Object, _endConstructServiceMock.Object)
            handler.ExecuteCommand_ReturnKeyCommandHandler(New ReturnKeyCommandArgs(_textViewMock.Object, _textBufferMock.Object), Sub() nextHandlerCalled = True)

            Assert.True(nextHandlerCalled)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ServiceCompletingShouldCallNextHandler()
            _endConstructServiceMock.Setup(Function(s) s.TryDo(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of Char))).Returns(True)
            _featureOptions.Setup(Function(s) s.GetOption(FeatureOnOffOptions.EndConstruct)).Returns(True)

            Dim nextHandlerCalled = False
            Dim handler As New EndConstructCommandHandler(_featureOptions.Object, _endConstructServiceMock.Object)
            handler.ExecuteCommand_ReturnKeyCommandHandler(New ReturnKeyCommandArgs(_textViewMock.Object, _textBufferMock.Object), Sub() nextHandlerCalled = True)

            Assert.False(nextHandlerCalled)
        End Sub
#End If

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(544556)>
        Public Sub EndConstruct_AfterCodeCleanup()
            Dim code = <code>Class C
    Sub Main(args As String())
        Dim z = 1
        Dim y = 2
        If z &gt;&lt; y Then 
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <code>Class C
    Sub Main(args As String())
        Dim z = 1
        Dim y = 2
        If z &lt;&gt; y Then 

        End If
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyAppliedAfterReturnUsingCommandHandler(code, {4, -1}, expected, {5, 12})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(546798)>
        Public Sub EndConstruct_AfterCodeCleanup_FormatOnlyTouched()
            Dim code = <code>Class C1
    Sub M1()
        System.Diagnostics. _Debug.Assert(True)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <code>Class C1
    Sub M1()
        System.Diagnostics. _
            Debug.Assert(True)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyAppliedAfterReturnUsingCommandHandler(code, {2, 29}, expected, {3, 12})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(531347)>
        Public Sub EndConstruct_AfterCodeCleanup_FormatOnly_WhenContainsDiagnostics()
            Dim code = <code>Module Program
    Sub Main(args As String())
        Dim a
        'Comment
        Dim b
        Dim c
    End Sub
End Module</code>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <code>Module Program
    Sub Main(args As String())
        Dim a
        'Comment
        Dim b

        Dim c
    End Sub
End Module</code>.Value.Replace(vbLf, vbCrLf)

            VerifyAppliedAfterReturnUsingCommandHandler(code, {4, -1}, expected, {5, 8})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2858, "https://github.com/dotnet/roslyn/issues/2858")>
        Public Sub AddParenthesesToArgumentListOnReturn_1()
            Dim code = <code>Public Class C
    Public Sub M(arg As String)
        M ""
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <code>Public Class C
    Public Sub M(arg As String)
        M ("")

    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyAppliedAfterReturnUsingCommandHandler(code, {2, -1}, expected, {3, 8})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2858, "https://github.com/dotnet/roslyn/issues/2858")>
        Public Sub AddParenthesesToArgumentListOnReturn_2()
            Dim code = <code>Public Class C
    Public Sub M(arg1 As String, arg2 as String)
        M arg1,arg2
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <code>Public Class C
    Public Sub M(arg1 As String, arg2 as String)
        M (arg1,arg2)

    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyAppliedAfterReturnUsingCommandHandler(code, {2, -1}, expected, {3, 8})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2858, "https://github.com/dotnet/roslyn/issues/2858")>
        Public Sub AddParenthesesToArgumentListOnReturn_3()
            Dim code = <code>Public Class C
    Public Sub M(arg1 As String, arg2 as String)
        M2 arg1,arg2
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyStatementEndConstructNotApplied({code}, {2, -1})
        End Sub

        <WorkItem(628656)>
        <Fact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub EndConstruct_NotOnLineFollowingToken()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                        "",
                       ""},
                caret:={2, 0})
        End Sub
    End Class
End Namespace
