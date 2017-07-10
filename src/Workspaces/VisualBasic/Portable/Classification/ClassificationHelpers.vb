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
        Public Function GetClassification(token As SyntaxToken) As ClassificationTypeKind?
            If SyntaxFacts.IsKeywordKind(token.Kind) Then
                Return ClassificationTypeKind.Keyword
            ElseIf IsStringToken(token) Then
                Return ClassificationTypeKind.StringLiteral
            ElseIf SyntaxFacts.IsPunctuation(token.Kind) Then
                Return ClassifyPunctuation(token)
            ElseIf token.Kind = SyntaxKind.IdentifierToken Then
                Return ClassifyIdentifierSyntax(token)
            ElseIf token.IsNumericLiteral() Then
                Return ClassificationTypeKind.NumericLiteral
            ElseIf token.Kind = SyntaxKind.XmlNameToken Then
                Return ClassificationTypeKind.XmlLiteralName
            ElseIf token.Kind = SyntaxKind.XmlTextLiteralToken Then
                Select Case token.Parent.Kind
                    Case SyntaxKind.XmlString
                        Return ClassificationTypeKind.XmlLiteralAttributeValue
                    Case SyntaxKind.XmlProcessingInstruction
                        Return ClassificationTypeKind.XmlLiteralProcessingInstruction
                    Case SyntaxKind.XmlComment
                        Return ClassificationTypeKind.XmlLiteralComment
                    Case SyntaxKind.XmlCDataSection
                        Return ClassificationTypeKind.XmlLiteralCDataSection
                    Case Else
                        Return ClassificationTypeKind.XmlLiteralText
                End Select
            ElseIf token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return ClassificationTypeKind.XmlLiteralEntityReference
            ElseIf token.IsKind(SyntaxKind.None, SyntaxKind.BadToken) Then
                Return Nothing
            Else
                Return Contract.FailWithReturn(Of ClassificationTypeKind)("Unhandled token kind: " & token.Kind().ToString())
            End If
        End Function

        Private Function ClassifyPunctuation(token As SyntaxToken) As ClassificationTypeKind
            If AllOperators.Contains(token.Kind) Then
                ' special cases...
                Select Case token.Kind
                    Case SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken
                        If TypeOf token.Parent Is AttributeListSyntax Then
                            Return ClassificationTypeKind.Punctuation
                        End If
                End Select

                Return ClassificationTypeKind.Operator
            Else
                Return ClassificationTypeKind.Punctuation
            End If
        End Function

        Private Function ClassifyIdentifierSyntax(identifier As SyntaxToken) As ClassificationTypeKind
            'Note: parent might be Nothing, if we are classifying raw tokens.
            Dim parent = identifier.Parent

            If TypeOf parent Is TypeStatementSyntax AndAlso DirectCast(parent, TypeStatementSyntax).Identifier = identifier Then
                Return ClassifyTypeDeclarationIdentifier(identifier)
            ElseIf TypeOf parent Is EnumStatementSyntax AndAlso DirectCast(parent, EnumStatementSyntax).Identifier = identifier Then
                Return ClassificationTypeKind.EnumName
            ElseIf TypeOf parent Is DelegateStatementSyntax AndAlso DirectCast(parent, DelegateStatementSyntax).Identifier = identifier AndAlso
                  (parent.Kind = SyntaxKind.DelegateSubStatement OrElse parent.Kind = SyntaxKind.DelegateFunctionStatement) Then

                Return ClassificationTypeKind.DelegateName
            ElseIf TypeOf parent Is TypeParameterSyntax AndAlso DirectCast(parent, TypeParameterSyntax).Identifier = identifier Then
                Return ClassificationTypeKind.TypeParameterName
            ElseIf (identifier.ToString() = "IsTrue" OrElse identifier.ToString() = "IsFalse") AndAlso
                TypeOf parent Is OperatorStatementSyntax AndAlso DirectCast(parent, OperatorStatementSyntax).OperatorToken = identifier Then

                Return ClassificationTypeKind.Keyword
            Else
                Return ClassificationTypeKind.Identifier
            End If
        End Function

        Private Function IsStringToken(token As SyntaxToken) As Boolean
            If token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken, SyntaxKind.InterpolatedStringTextToken) Then
                Return True
            End If

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken) AndAlso
                   token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression)
        End Function

        Private Function ClassifyTypeDeclarationIdentifier(identifier As SyntaxToken) As ClassificationTypeKind
            Select Case identifier.Parent.Kind
                Case SyntaxKind.ClassStatement
                    Return ClassificationTypeKind.ClassName
                Case SyntaxKind.ModuleStatement
                    Return ClassificationTypeKind.ModuleName
                Case SyntaxKind.InterfaceStatement
                    Return ClassificationTypeKind.InterfaceName
                Case SyntaxKind.StructureStatement
                    Return ClassificationTypeKind.StructName
                Case Else
                    Return Contract.FailWithReturn(Of ClassificationTypeKind)("Unhandled type declaration")
            End Select
        End Function

        Friend Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpanSlim), cancellationToken As CancellationToken)
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
