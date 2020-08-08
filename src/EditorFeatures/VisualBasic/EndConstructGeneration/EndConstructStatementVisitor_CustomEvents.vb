' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Partial Friend Class EndConstructStatementVisitor
        Public Overrides Function VisitEventStatement(node As EventStatementSyntax) As AbstractEndConstructResult
            ' If it's a event with a signature we don't want to spit since it can't be a custom
            ' event
            If node.AsClause Is Nothing Then
                Return Nothing
            End If

            If node.CustomKeyword.Kind <> SyntaxKind.CustomKeyword Then
                Return Nothing
            End If

            Dim eventBlock = node.GetAncestor(Of EventBlockSyntax)()

            ' If we have an End, we don't have to spit
            If Not eventBlock.EndEventStatement.IsMissing Then
                Return Nothing
            End If

            ' We need to generate our various handlers
            Dim lines As New List(Of String)
            lines.AddRange(GenerateAddOrRemoveHandler(node, SyntaxKind.AddHandlerKeyword))
            lines.AddRange(GenerateAddOrRemoveHandler(node, SyntaxKind.RemoveHandlerKeyword))
            lines.AddRange(GenerateRaiseEventHandler(node))

            Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
            lines.Add(aligningWhitespace & "End Event")

            Return New SpitLinesResult(lines)
        End Function

        Private Function GenerateAddOrRemoveHandler(eventStatement As EventStatementSyntax, kind As SyntaxKind) As String()
            Dim type = _state.SemanticModel.GetTypeInfo(DirectCast(eventStatement.AsClause, SimpleAsClauseSyntax).Type, Me._cancellationToken)
            Dim position As Integer = eventStatement.SpanStart
            Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(position) & "    "
            Return {aligningWhitespace & SyntaxFacts.GetText(kind) & "(value As " & type.Type.ToMinimalDisplayString(_state.SemanticModel, position, SymbolDisplayFormats.NameFormat) & ")",
                    "",
                    aligningWhitespace & "End " & SyntaxFacts.GetText(kind)}
        End Function

        Private Function GenerateRaiseEventHandler(eventStatement As EventStatementSyntax) As String()
            Dim type = TryCast(_state.SemanticModel.GetTypeInfo(DirectCast(eventStatement.AsClause, SimpleAsClauseSyntax).Type, Me._cancellationToken).Type, INamedTypeSymbol)
            Dim signature = ""

            If type IsNot Nothing AndAlso type.DelegateInvokeMethod IsNot Nothing Then
                Dim parameterStrings = type.DelegateInvokeMethod.Parameters.Select(
                                           Function(p) p.ToMinimalDisplayString(_state.SemanticModel, eventStatement.SpanStart))
                signature = String.Join(", ", parameterStrings)
            End If

            Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(eventStatement.SpanStart) & "    "
            Return {aligningWhitespace & "RaiseEvent(" & signature & ")",
                    "",
                    aligningWhitespace & "End RaiseEvent"}
        End Function
    End Class
End Namespace
