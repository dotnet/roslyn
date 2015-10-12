' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub Test1()
            Dim code = "$$"

            ExpectException_Test(code, indentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub Test2()
            Dim code = "$$Namespace"

            ExpectException_Test(code, indentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub FirstTokenInParameterList1()
            Dim code = <code>Class C
    Sub Method(
$$i As Test)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=14)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub FirstTokenInParameterList2()
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=14)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub FirstTokenInTypeParameterList()
            Dim code = <code>Class C
    Sub Method(
$$Of T)(i As Test)
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=14)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub FirstTokenInArrayRank1()
            Dim code = <code>Class C
    Sub Method(i As Test(
$$))
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub FirstTokenInArrayRank2()
            Dim code = <code>Class C
    Sub Method(i As Test(
$$,))
    End Sub
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub Attribute()
            Dim code = My.Resources.XmlLiterals.TokenFormatter2
            Test(code, indentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub XmlLiterals1()
            Dim code = My.Resources.XmlLiterals.TokenFormatter1
            Test(code, indentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub XmlLiterals2()
            Dim code = My.Resources.XmlLiterals.XmlTest1_TokenFormat
            Test(code, indentation:=19)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub EnterBetweenXmlLiterals()
            Dim code = My.Resources.XmlLiterals.XmlTest9
            Test(code, indentation:=30)
        End Sub

        <WorkItem(542240)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub MissingEndStatement()
            Dim code = <code>Module Module1
    Sub Main()
        If True Then
            Dim q
    
    $$End Sub
End Module</code>.Value.Replace(vbLf, vbCrLf)

            Test(code, indentation:=4)
        End Sub

        <WorkItem(542240)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)>
        Public Sub EmptyElement1()
            Test(My.Resources.XmlLiterals.EmptyElement1, indentation:=23)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub BlockIndentation()
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class
</code>.Value.Replace(vbLf, vbCrLf)

            ExpectException_Test(code, 4, FormattingOptions.IndentStyle.Block)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub NoIndentation()
            Dim code = <code>Class C
    Sub Method(
$$)
    End Sub
End Class
</code>.Value.Replace(vbLf, vbCrLf)

            ExpectException_Test(code, indentation:=0, indentStyle:=FormattingOptions.IndentStyle.None)
        End Sub

        Private Sub ExpectException_Test(codeWithMarkup As String, indentation As Integer, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart)
            Assert.NotNull(Record.Exception(Sub() Test(codeWithMarkup, indentation, indentStyle:=indentStyle)))
        End Sub

        Private Sub Test(codeWithMarkup As String, indentation As Integer, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart)
            Dim code As String = Nothing
            Dim position As Integer = 0
            MarkupTestFile.GetPosition(codeWithMarkup, code, position)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim hostdoc = workspace.Documents.First()
                Dim buffer = hostdoc.GetTextBuffer()

                SmartIndenterTests.SetIndentStyle(buffer, indentStyle)

                Dim snapshot = buffer.CurrentSnapshot
                Dim line = snapshot.GetLineFromPosition(position)

                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)
                Dim root = DirectCast(document.GetSyntaxRootAsync().Result, CompilationUnitSyntax)

                Dim formattingRules = (New SpecialFormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document))

                ' get token
                Dim token = root.FindToken(position)

                Dim previousToken = token.GetPreviousToken(includeZeroWidth:=True)
                Dim ignoreMissingToken = previousToken.IsMissing AndAlso line.Start.Position = position

                Assert.True(VisualBasicIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules, root, line, workspace.Options, Nothing, ignoreMissingToken))

                Dim smartFormatter = New SmartTokenFormatter(workspace.Options, formattingRules, root)
                Dim changes = smartFormatter.FormatToken(workspace, token, Nothing)

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
        End Sub
    End Class
End Namespace
