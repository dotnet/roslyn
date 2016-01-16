' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    Public Class SmartTokenFormatter_FormatTokenTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function Test1() As Task
            Dim code = "$$"

            Await ExpectException_TestAsync(code, indentation:=0)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function Test2() As Task
            Dim code = "$$Namespace"

            Await ExpectException_TestAsync(code, indentation:=0)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function FirstTokenInParameterList1() As Task
            Dim code = <code>Class C
    Sub Method(
$$i As Test)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=14)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function FirstTokenInParameterList2() As Task
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=14)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function FirstTokenInTypeParameterList() As Task
            Dim code = <code>Class C
    Sub Method(
$$Of T)(i As Test)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=14)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function FirstTokenInArrayRank1() As Task
            Dim code = <code>Class C
    Sub Method(i As Test(
$$))
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=24)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function FirstTokenInArrayRank2() As Task
            Dim code = <code>Class C
    Sub Method(i As Test(
$$,))
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=24)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function Attribute() As Task
            Dim code = My.Resources.XmlLiterals.TokenFormatter2
            Await TestAsync(code, indentation:=4)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function XmlLiterals1() As Task
            Dim code = My.Resources.XmlLiterals.TokenFormatter1
            Await TestAsync(code, indentation:=12)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function XmlLiterals2() As Task
            Dim code = My.Resources.XmlLiterals.XmlTest1_TokenFormat
            Await TestAsync(code, indentation:=19)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function EnterBetweenXmlLiterals() As Task
            Dim code = My.Resources.XmlLiterals.XmlTest9
            Await TestAsync(code, indentation:=30)
        End Function

        <WorkItem(542240)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function MissingEndStatement() As Task
            Dim code = <code>Module Module1
    Sub Main()
        If True Then
            Dim q
    
    $$End Sub
End Module</code>.Value.Replace(vbLf, vbCrLf)

            Await TestAsync(code, indentation:=4)
        End Function

        <WorkItem(542240)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Async Function EmptyElement1() As Task
            Await TestAsync(My.Resources.XmlLiterals.EmptyElement1, indentation:=23)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function BlockIndentation() As Task
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class
</code>.Value.Replace(vbLf, vbCrLf)

            Await ExpectException_TestAsync(code, 4, FormattingOptions.IndentStyle.Block)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function NoIndentation() As Task
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class
</code>.Value.Replace(vbLf, vbCrLf)

            Await ExpectException_TestAsync(code, indentation:=0, indentStyle:=FormattingOptions.IndentStyle.None)
        End Function

        Private Async Function ExpectException_TestAsync(codeWithMarkup As String, indentation As Integer, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart) As Task
            Assert.NotNull(Await Record.ExceptionAsync(Function() TestAsync(codeWithMarkup, indentation, indentStyle:=indentStyle)))
        End Function

        Private Async Function TestAsync(codeWithMarkup As String, indentation As Integer, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart) As Threading.Tasks.Task
            Dim code As String = Nothing
            Dim position As Integer = 0
            MarkupTestFile.GetPosition(codeWithMarkup, code, position)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code)
                Dim hostdoc = workspace.Documents.First()
                Dim buffer = hostdoc.GetTextBuffer()

                SmartIndenterTests.SetIndentStyle(buffer, indentStyle)

                Dim snapshot = buffer.CurrentSnapshot
                Dim line = snapshot.GetLineFromPosition(position)

                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)
                Dim root = DirectCast(Await document.GetSyntaxRootAsync(), CompilationUnitSyntax)

                Dim formattingRules = (New SpecialFormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document))

                ' get token
                Dim token = root.FindToken(position)

                Dim previousToken = token.GetPreviousToken(includeZeroWidth:=True)
                Dim ignoreMissingToken = previousToken.IsMissing AndAlso line.Start.Position = position

                Assert.True(VisualBasicIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules, root, line, workspace.Options, Nothing, ignoreMissingToken))

                Dim smartFormatter = New SmartTokenFormatter(workspace.Options, formattingRules, root)
                Dim changes = Await smartFormatter.FormatTokenAsync(workspace, token, Nothing)

                Using edit = buffer.CreateEdit()
                    For Each change In changes
                        edit.Replace(change.Span.ToSpan(), change.NewText)
                    Next

                    edit.Apply()
                End Using

                ' no changes. original location is correct position
                Dim editorOptions = New Mock(Of IEditorOptions)(MockBehavior.Strict)
                editorOptions.Setup(Function(x) x.GetOptionValue(DefaultOptions.IndentSizeOptionId)).Returns(4)
                editorOptions.Setup(Function(x) x.GetOptionValue(DefaultOptions.TabSizeOptionId)).Returns(4)

                Dim actualIndentation = buffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber).GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(editorOptions.Object)

                Assert.Equal(indentation, actualIndentation)
            End Using
        End Function
    End Class
End Namespace
