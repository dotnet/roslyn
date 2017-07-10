' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Friend Module ClassificationHelpers
        ''' <summary>
        ''' Return the classification type associated with this token.
        ''' </summary>
        ''' <param name="token">The token to be classified.</param>
        ''' <returns>The classification type for the token</returns>
        ''' <remarks></remarks>
        Public Function GetClassification(token As SyntaxToken) As String
            If SyntaxFacts.IsKeywordKind(token.Kind) Then
                Return ClassificationTypeNames.Keyword
            ElseIf IsStringToken(token) Then
                Return ClassificationTypeNames.StringLiteral
            ElseIf SyntaxFacts.IsPunctuation(token.Kind) Then
                Return ClassifyPunctuation(token)
            ElseIf token.Kind = SyntaxKind.IdentifierToken Then
                Return ClassifyIdentifierSyntax(token)
            ElseIf token.IsNumericLiteral() Then
                Return ClassificationTypeNames.NumericLiteral
            ElseIf token.Kind = SyntaxKind.XmlNameToken Then
                Return ClassificationTypeNames.XmlLiteralName
            ElseIf token.Kind = SyntaxKind.XmlTextLiteralToken Then
                Select Case token.Parent.Kind
                    Case SyntaxKind.XmlString
                        Return ClassificationTypeNames.XmlLiteralAttributeValue
                    Case SyntaxKind.XmlProcessingInstruction
                        Return ClassificationTypeNames.XmlLiteralProcessingInstruction
                    Case SyntaxKind.XmlComment
                        Return ClassificationTypeNames.XmlLiteralComment
                    Case SyntaxKind.XmlCDataSection
                        Return ClassificationTypeNames.XmlLiteralCDataSection
                    Case Else
                        Return ClassificationTypeNames.XmlLiteralText
                End Select
            ElseIf token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return ClassificationTypeNames.XmlLiteralEntityReference
            ElseIf token.IsKind(SyntaxKind.None, SyntaxKind.BadToken) Then
                Return Nothing
            Else
                Return Contract.FailWithReturn(Of String)("Unhandled token kind: " & token.Kind().ToString())
            End If
        End Function

        Private Function ClassifyPunctuation(token As SyntaxToken) As String
            If AllOperators.Contains(token.Kind) Then
                ' special cases...
                Select Case token.Kind
                    Case SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken
                        If TypeOf token.Parent Is AttributeListSyntax Then
                            Return ClassificationTypeNames.Punctuation
                        End If
                End Select

                Return ClassificationTypeNames.Operator
            Else
                Return ClassificationTypeNames.Punctuation
            End If
        End Function

        Private Function ClassifyIdentifierSyntax(identifier As SyntaxToken) As String
            'Note: parent might be Nothing, if we are classifying raw tokens.
            Dim parent = identifier.Parent

            If TypeOf parent Is TypeStatementSyntax AndAlso DirectCast(parent, TypeStatementSyntax).Identifier = identifier Then
                Return ClassifyTypeDeclarationIdentifier(identifier)
            ElseIf TypeOf parent Is EnumStatementSyntax AndAlso DirectCast(parent, EnumStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.EnumName
            ElseIf TypeOf parent Is DelegateStatementSyntax AndAlso DirectCast(parent, DelegateStatementSyntax).Identifier = identifier AndAlso
                  (parent.Kind = SyntaxKind.DelegateSubStatement OrElse parent.Kind = SyntaxKind.DelegateFunctionStatement) Then

                Return ClassificationTypeNames.DelegateName
            ElseIf TypeOf parent Is TypeParameterSyntax AndAlso DirectCast(parent, TypeParameterSyntax).Identifier = identifier Then
                Return ClassificationTypeNames.TypeParameterName
            ElseIf (identifier.ToString() = "IsTrue" OrElse identifier.ToString() = "IsFalse") AndAlso
                TypeOf parent Is OperatorStatementSyntax AndAlso DirectCast(parent, OperatorStatementSyntax).OperatorToken = identifier Then

                Return ClassificationTypeNames.Keyword
            Else
                Return ClassificationTypeNames.Identifier
            End If
        End Function

        Private Function IsStringToken(token As SyntaxToken) As Boolean
            If token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken, SyntaxKind.InterpolatedStringTextToken) Then
                Return True
            End If

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken) AndAlso
                   token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression)
        End Function

        Private Function ClassifyTypeDeclarationIdentifier(identifier As SyntaxToken) As String
            Select Case identifier.Parent.Kind
                Case SyntaxKind.ClassStatement
                    Return ClassificationTypeNames.ClassName
                Case SyntaxKind.ModuleStatement
                    Return ClassificationTypeNames.ModuleName
                Case SyntaxKind.InterfaceStatement
                    Return ClassificationTypeNames.InterfaceName
                Case SyntaxKind.StructureStatement
                    Return ClassificationTypeNames.StructName
                Case Else
                    Return Contract.FailWithReturn(Of String)("Unhandled type declaration")
            End Select
        End Function

        Friend Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim text2 = text.ToString(textSpan)
            Dim tokens = SyntaxFactory.ParseTokens(text2, initialTokenPosition:=textSpan.Start)
            Worker.CollectClassifiedSpans(tokens, textSpan, result, cancellationToken)
        End Sub

        Friend Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            ' TODO: Do we need to do the same work here that we do in C#?
            Return classifiedSpan
        End Function
    End Module
End Namespace
