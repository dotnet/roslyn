' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Partial Friend Class EndConstructStatementVisitor
        Inherits VisualBasicSyntaxVisitor(Of AbstractEndConstructResult)

        Private ReadOnly _textView As ITextView
        Private ReadOnly _subjectBuffer As ITextBuffer
        Private ReadOnly _cancellationToken As CancellationToken
        Private ReadOnly _state As EndConstructState

        Public Sub New(textView As ITextView,
                       subjectBuffer As ITextBuffer,
                       state As EndConstructState,
                       cancellationToken As CancellationToken)

            _textView = textView
            _subjectBuffer = subjectBuffer
            _state = state
            _cancellationToken = cancellationToken
        End Sub

        Public Overrides Function VisitDoStatement(node As DoStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of DoLoopBlockSyntax)().Any(Function(block) block.LoopStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "Loop"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitEnumStatement(node As EnumStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of EnumBlockSyntax)().Any(Function(block) block.EndEnumStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End " & node.EnumKeyword.ToString()})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitForStatement(node As ForStatementSyntax) As AbstractEndConstructResult
            Return TryApplyOnForStatement(node)
        End Function

        Public Overrides Function VisitForEachStatement(node As ForEachStatementSyntax) As AbstractEndConstructResult
            Return TryApplyOnForStatement(node)
        End Function

        ''' <param name="forStatement">The ForStatementSyntax or ForEachStatementSyntax for the loop.</param>
        Public Function TryApplyOnForStatement(forStatement As StatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = False

            For Each parent In forStatement.GetAncestorsOrThis(Of ForOrForEachBlockSyntax)()
                If parent.NextStatement Is Nothing Then
                    ' For Blocks may have a null EndOpt, which indicates that they were closed by a parent block's Next
                    ' statement. Thus this block is closed, and we are done
                    Return Nothing
                ElseIf parent.NextStatement.ControlVariables.Count > 0 Then
                    ' The parser has matched start/end blocks, and so anything within this we consider as done
                    Return Nothing
                ElseIf parent.NextStatement.IsMissing Then
                    needsEnd = True
                End If
            Next

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(forStatement.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "Next"})
            Else
                Return Nothing
            End If
        End Function

        Private Function HandleMethodBlockSyntax(methodBlock As MethodBlockBaseSyntax) As AbstractEndConstructResult
            If methodBlock IsNot Nothing AndAlso methodBlock.EndBlockStatement.IsMissing Then
                Dim result = TryGenerateResultForConstructorSpitWithInitializeComponent(methodBlock)
                If result IsNot Nothing Then
                    Return result
                End If

                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(methodBlock.BlockStatement.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End " & methodBlock.BlockStatement.DeclarationKeyword.ToString()})
            Else
                Return Nothing
            End If
        End Function

        Private Function TryGenerateResultForConstructorSpitWithInitializeComponent(methodBlock As MethodBlockBaseSyntax) As AbstractEndConstructResult
            If methodBlock.BlockStatement.Kind = SyntaxKind.SubNewStatement Then
                Dim boundConstructor = _state.SemanticModel.GetDeclaredSymbol(DirectCast(methodBlock.BlockStatement, SubNewStatementSyntax))
                If boundConstructor IsNot Nothing Then
                    If boundConstructor.ContainingType.IsDesignerGeneratedTypeWithInitializeComponent(_state.SemanticModel.Compilation) Then
                        Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(methodBlock.BlockStatement.SpanStart)
                        Dim innerAligningWhitespace = aligningWhitespace & "    "

                        ' When sticking on the comments, we don't want the ' in the localized string
                        ' lest we try localizing the comment character itself
                        Return New SpitLinesResult(
                            {
                                "",
                                innerAligningWhitespace & "' " & VBEditorResources.This_call_is_required_by_the_designer,
                                innerAligningWhitespace & "InitializeComponent()",
                                "",
                                innerAligningWhitespace & "' " & VBEditorResources.Add_any_initialization_after_the_InitializeComponent_call,
                                "",
                                aligningWhitespace & "End Sub"
                             })

                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As AbstractEndConstructResult
            Dim blockToClose = node.GetAncestor(Of MethodBlockSyntax)()
            Return HandleMethodBlockSyntax(blockToClose)
        End Function

        Public Overrides Function VisitSubNewStatement(node As SubNewStatementSyntax) As AbstractEndConstructResult
            Dim blockToClose = node.GetAncestor(Of ConstructorBlockSyntax)()
            Return HandleMethodBlockSyntax(blockToClose)
        End Function

        Public Overrides Function VisitOperatorStatement(node As OperatorStatementSyntax) As AbstractEndConstructResult
            Dim blockToClose = node.GetAncestor(Of OperatorBlockSyntax)()
            Return HandleMethodBlockSyntax(blockToClose)
        End Function

        Public Overrides Function VisitNamespaceStatement(node As NamespaceStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of NamespaceBlockSyntax)().Any(Function(block) block.EndNamespaceStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Namespace"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitSelectStatement(node As SelectStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of SelectBlockSyntax)().Any(Function(block) block.EndSelectStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Dim stringBuilder As New StringBuilder
                stringBuilder.Append(_textView.Options.GetNewLineCharacter())
                StringBuilder.Append(aligningWhitespace & "    Case ")
                Dim finalCaretPoint = stringBuilder.Length
                stringBuilder.AppendLine()
                StringBuilder.Append(aligningWhitespace & "End Select")

                Return New ReplaceSpanResult(New SnapshotSpan(_subjectBuffer.CurrentSnapshot, _state.CaretPosition, 0),
                                             stringBuilder.ToString(), newCaretPosition:=finalCaretPoint)
            Else
                Return (Nothing)
            End If
        End Function

        Public Overrides Function VisitSyncLockStatement(node As SyncLockStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of SyncLockBlockSyntax)().Any(Function(block) block.EndSyncLockStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End SyncLock"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitTryStatement(node As TryStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of TryBlockSyntax)().Any(Function(block) block.EndTryStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "Catch ex As Exception", "", aligningWhitespace & "End Try"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitModuleStatement(ByVal node As ModuleStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of ModuleBlockSyntax)().Any(Function(block) block.EndBlockStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Module"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitClassStatement(ByVal node As ClassStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of ClassBlockSyntax)().Any(Function(block) block.EndBlockStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Class"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitStructureStatement(ByVal node As StructureStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of StructureBlockSyntax)().Any(Function(block) block.EndBlockStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Structure"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitInterfaceStatement(ByVal node As InterfaceStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of InterfaceBlockSyntax)().Any(Function(block) block.EndBlockStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Interface"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitUsingStatement(ByVal node As UsingStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of UsingBlockSyntax)().Any(Function(block) block.EndUsingStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End Using"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitWhileStatement(node As WhileStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of WhileBlockSyntax)().Any(Function(block) block.EndWhileStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End While"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitWithStatement(node As WithStatementSyntax) As AbstractEndConstructResult
            Dim needsEnd = node.GetAncestorsOrThis(Of WithBlockSyntax)().Any(Function(block) block.EndWithStatement.IsMissing)

            If needsEnd Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                Return New SpitLinesResult({"", aligningWhitespace & "End With"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitIfDirectiveTrivia(directive As IfDirectiveTriviaSyntax) As AbstractEndConstructResult
            Dim matchingDirectives = directive.GetMatchingConditionalDirectives(_cancellationToken)
            Dim needsEnd = Not matchingDirectives.Any(Function(d) d.Kind = SyntaxKind.EndIfDirectiveTrivia)

            If needsEnd Then
                Return New SpitLinesResult({"", "#End If"})
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function VisitRegionDirectiveTrivia(directive As RegionDirectiveTriviaSyntax) As AbstractEndConstructResult
            Dim precedingRegionDirectives =
                directive.SyntaxTree.GetStartDirectives(_cancellationToken).
                            Where(Function(d) d.SpanStart <= directive.SpanStart)

            If precedingRegionDirectives.Any(Function(d) d.GetMatchingStartOrEndDirective(_cancellationToken) Is Nothing) Then
                Return New SpitLinesResult({"", "#End Region"})
            Else
                Return Nothing
            End If
        End Function
    End Class
End Namespace
