' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    ''' <summary>
    ''' Adds VB specific parts to the line directive map
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class VisualBasicLineDirectiveMap
        Inherits LineDirectiveMap(Of DirectiveTriviaSyntax)

        Public Sub New(tree As SyntaxTree)
            MyBase.New(tree)
        End Sub

        ' Add all active #ExternalSource directives under trivia and #End ExternalSource directives, in source code order.
        Protected Overrides Function ShouldAddDirective(directive As DirectiveTriviaSyntax) As Boolean
            Debug.Assert(directive IsNot Nothing)
            Return directive.Kind = SyntaxKind.ExternalSourceDirectiveTrivia OrElse directive.Kind = SyntaxKind.EndExternalSourceDirectiveTrivia
        End Function

        ' Given a directive and the previous entry, create a new entry
        Protected Overrides Function GetEntry(directive As DirectiveTriviaSyntax,
                                              sourceText As SourceText,
                                              previous As LineMappingEntry) As LineMappingEntry
            Debug.Assert(ShouldAddDirective(directive))

            ' Get line number of NEXT line, hence the +1.
            Dim directiveLineNumber As Integer = sourceText.Lines.IndexOf(directive.SpanStart) + 1

            ' The default for the current entry does the same thing as the previous entry
            Dim unmappedLine = directiveLineNumber
            Dim mappedLine = previous.MappedLine + directiveLineNumber - previous.UnmappedLine
            Dim mappedPathOpt = previous.MappedPathOpt
            Dim state As PositionState

            ' Modify the current entry based on the directive
            If directive.Kind = SyntaxKind.ExternalSourceDirectiveTrivia Then
                Dim extSourceDirective = DirectCast(directive, ExternalSourceDirectiveTriviaSyntax)

                If Not extSourceDirective.LineStart.IsMissing AndAlso
                   Not extSourceDirective.ExternalSource.IsMissing Then
                    ' we do not allow negative values coming in from overflown integers and make them Integer.MaxValue to
                    ' point to the "last line"
                    ' + convert to zero based line numbers
                    ' mapped line number can get -1 if user specified a 0, although the general
                    ' interpretation of user given line numbers is 1-based.
                    mappedLine = CInt(Math.Min(CLng(extSourceDirective.LineStart.Value), Integer.MaxValue) - 1)
                    Debug.Assert(mappedLine >= -1)
                    mappedPathOpt = CStr(extSourceDirective.ExternalSource.Value)
                End If

                If previous.State = PositionState.Unknown Then
                    state = PositionState.RemappedAfterUnknown
                Else
                    ' should be Hidden, but can be a remapping in broken code scenarios (nested #ExternalSource)
                    ' RemappedAfterUnknown can happen in such a case:
                    ' <file begin>
                    ' stmts
                    ' #externalsource("file.vb", 23)
                    ' #externalsource("file.vb", 23)
                    '
                    ' RemappedAfterHidden can happen in such a case:
                    ' <file begin>
                    ' stmts
                    ' #externalsource("file.vb", 23)
                    ' stmts
                    ' #end externalSource
                    ' #externalsource("file.vb", 23)
                    ' #externalsource("file.vb", 23)
                    '
                    ' Hidden is the common case (2nd directive):
                    ' <file begin>
                    ' stmts
                    ' #externalsource("file.vb", 23)
                    ' stmts
                    ' #end externalSource
                    ' #externalsource("file.vb", 42)
                    ' stmts
                    ' #end externalSource
                    Debug.Assert(previous.State = PositionState.Hidden OrElse
                                 previous.State = PositionState.RemappedAfterUnknown OrElse
                                 previous.State = PositionState.RemappedAfterHidden)
                    state = PositionState.RemappedAfterHidden
                End If

            ElseIf directive.Kind = SyntaxKind.EndExternalSourceDirectiveTrivia Then
                mappedLine = unmappedLine
                mappedPathOpt = Nothing
                If unmappedLine > previous.UnmappedLine + 1 AndAlso
                    (previous.State = PositionState.RemappedAfterHidden OrElse previous.State = PositionState.RemappedAfterUnknown) Then
                    ' only directives that actually span multiple lines should be considered. It must also be a complete sequence of 
                    ' #externalsource ... #end externalsource
                    ' "empty" directives like 
                    ' #externalsource("file.vb", 23)
                    ' #end externalSource
                    ' should be ignored.
                    state = PositionState.Hidden
                Else
                    If previous.State = PositionState.RemappedAfterHidden Then
                        state = PositionState.Hidden
                    Else
                        ' Should be RemappedAfterUnknown. but can be Hidden in broken code (multiple #End ExternalSource)
                        ' Hidden can happen here (2nd end directive):
                        ' <file begin>
                        ' stmts
                        ' #externalsource("file.vb", 23)
                        ' stmts
                        ' #end externalSource
                        ' #end externalSource
                        '
                        ' Unknown can happen here:
                        ' <file begin>
                        ' stmts
                        ' #end externalSource
                        '
                        ' RemappedAfterUnknown is the common case
                        ' <file begin>
                        ' stmts
                        ' #externalsource("file.vb", 23)
                        ' stmts
                        ' #end externalSource
                        Debug.Assert(previous.State = PositionState.RemappedAfterUnknown OrElse
                                     previous.State = PositionState.Hidden OrElse
                                     previous.State = PositionState.Unknown)
                        state = PositionState.Unknown
                    End If
                End If
            End If

            Return New LineMappingEntry(unmappedLine,
                                        mappedLine,
                                        mappedPathOpt,
                                        state)
        End Function

        Protected Overrides Function InitializeFirstEntry() As LineMappingEntry
            ' The first entry of the map is always 0,0,null,Unknown -- the default mapping.
            Return New LineMappingEntry(0, 0, Nothing, PositionState.Unknown)
        End Function

        Public Overrides Function GetLineVisibility(sourceText As SourceText, position As Integer) As LineVisibility
            Dim unmappedPos As LinePosition = sourceText.Lines.GetLinePosition(position)
            Dim index As Integer = FindEntryIndex(unmappedPos.Line)

            Return GetLineVisibility(index)
        End Function

        Protected Overrides Function GetUnknownStateVisibility(index As Integer) As LineVisibility
            Return GetLineVisibility(index)
        End Function

        Private Overloads Function GetLineVisibility(index As Integer) As LineVisibility
            ' #ExternalSource is used primarily for ASP.NET (formerly XSP) or Venus. The requirement is that only spans marked
            ' with the directive should add sequence points. This is because the XSP guys don't want the user debugging into 
            ' generated code that didn't explicitly come from the ASP/Venus page.
            ' Dev11 omitted SP for spans outside of the directive (but kept hidden sequence points). Roslyn will generate hidden
            ' sequence points instead because the spans outside of the directive are marked as "hidden". This way we can share 
            ' the information through the common syntax tree and use a shared emitter.

            Dim entry As LineMappingEntry = Entries(index)

            If entry.State = PositionState.Unknown Then
                If Entries.Length < index + 3 Then
                    ' it's either just a one entry being unknown or it's a broken code scenario like
                    ' e.g. an externalsource with no end.
                    ' the minimal legal entries are:
                    ' (Unknown) or (Unknown, RemappedAfterUnknown, (Unknown | Hidden | RemappedAfterHidden))

                    Return LineVisibility.Visible
                End If

                ' if there is only one entry, then there wasn't a external source
                Debug.Assert(Entries.Length > index + 1)
                Debug.Assert(Entries(index + 1).State = PositionState.RemappedAfterUnknown OrElse
                             Entries(index + 1).State = PositionState.Unknown)

                If Entries(index + 1).State = PositionState.Unknown Then
                    ' in broken code scenarios, where there are leading #end external sources, we'll find 
                    ' unknown -> unknown

                    Return GetLineVisibility(index + 1)
                End If

                Debug.Assert(Entries(index + 2).State = PositionState.Unknown OrElse
                             Entries(index + 2).State = PositionState.Hidden OrElse
                             Entries(index + 2).State = PositionState.RemappedAfterHidden)

                Dim lookaheadEntryState = Entries(index + 2).State
                If lookaheadEntryState = PositionState.Unknown Then
                    ' there can be a couple of Unknown -> RemappedAfterUnknown -> Unmapped -> RemappedAfterUnknown in the entries 
                    ' list if there are "empty" #ExternalSource directives. In that case we search further in the list of entries 
                    ' if we can find a hidden state, which would be generated by a non empty directive

                    Return GetLineVisibility(index + 2)
                End If

                Return If(lookaheadEntryState = PositionState.Hidden, LineVisibility.Hidden, LineVisibility.Visible)
            End If

            Return If(entry.State = PositionState.Hidden, LineVisibility.Hidden, LineVisibility.Visible)
        End Function

        Friend Overrides Function TranslateSpanAndVisibility(sourceText As SourceText, treeFilePath As String, span As TextSpan, ByRef isHiddenPosition As Boolean) As FileLinePositionSpan
            Dim unmappedStartPos = sourceText.Lines.GetLinePosition(span.Start)
            Dim unmappedEndPos = sourceText.Lines.GetLinePosition(span.End)

            Dim index As Integer = FindEntryIndex(unmappedStartPos.Line)

            isHiddenPosition = GetLineVisibility(index) = LineVisibility.Hidden

            Dim entry = Entries(index)

            Return TranslateSpan(entry, treeFilePath, unmappedStartPos, unmappedEndPos)
        End Function
    End Class
End Namespace
