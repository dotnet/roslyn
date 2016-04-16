' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Compilers.VisualBasic
Imports Microsoft.CodeAnalysis.Services.Editor.Implementation.ExtractMethod
Imports Microsoft.CodeAnalysis.Services.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Services.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text.Classification

Namespace Microsoft.CodeAnalysis.Services.Editor.VisualBasic.Extensions
    <Extension()>
    Friend Module IClassificationTypesExtensions
        ''' <summary>
        ''' Return the classification type associated with this token.
        ''' </summary>
        ''' <param name="classificationTypes">The source of classification types.</param>
        ''' <param name="token">The token to be classified.</param>
        ''' <returns>The classification type for the token</returns>
        ''' <remarks></remarks>
        <Extension()>
        Public Function GetClassificationForToken(classificationTypes As IClassificationTypes, token As SyntaxToken) As IClassificationType
            If token.Kind.IsKeywordKind() Then
                Return classificationTypes.Keyword
            ElseIf token.Kind.IsPunctuation() Then
                Return ClassifyPunctuation(classificationTypes, token)
            ElseIf token.Kind = SyntaxKind.IdentifierToken Then
                Return ClassifyIdentifierSyntax(classificationTypes, token)
            ElseIf token.MatchesKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken) Then
                Return classificationTypes.StringLiteral
            ElseIf token.MatchesKind(
                SyntaxKind.DecimalLiteralToken,
                SyntaxKind.FloatingLiteralToken,
                SyntaxKind.IntegerLiteralToken,
                SyntaxKind.DateLiteralToken) Then
                Return classificationTypes.NumericLiteral
            ElseIf token.Kind = SyntaxKind.XmlNameToken Then
                Return classificationTypes.XmlName
            ElseIf token.Kind = SyntaxKind.XmlTextLiteralToken Then
                Select Case token.Parent.Kind
                    Case SyntaxKind.XmlString
                        Return classificationTypes.XmlAttributeValue
                    Case SyntaxKind.XmlProcessingInstruction
                        Return classificationTypes.XmlProcessingInstruction
                    Case SyntaxKind.XmlComment
                        Return classificationTypes.XmlComment
                    Case SyntaxKind.XmlCDataSection
                        Return classificationTypes.XmlCDataSection
                    Case Else
                        Return classificationTypes.XmlText
                End Select
            ElseIf token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return classificationTypes.XmlEntityReference
            ElseIf token.MatchesKind(SyntaxKind.None, SyntaxKind.BadToken) Then
                Return Nothing
            Else
                Return Contract.FailWithReturn(Of IClassificationType)("Unhandled token kind: " & token.Kind)
            End If
        End Function

        Private Function ClassifyPunctuation(classificationTypes As IClassificationTypes, token As SyntaxToken) As IClassificationType
            If AllOperators.Contains(token.Kind) Then
                ' special cases...
                Select Case token.Kind
                    Case SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken
                        If TypeOf token.Parent Is AttributeBlockSyntax Then
                            Return classificationTypes.Punctuation
                        End If
                End Select

                Return classificationTypes.Operator
            Else
                Return classificationTypes.Punctuation
            End If
        End Function

        Private Function ClassifyIdentifierSyntax(classificationTypes As IClassificationTypes, identifier As SyntaxToken) As IClassificationType
            'Note: parent might be Nothing, if we are classifying raw tokens.
            Dim parent = identifier.Parent

            If TypeOf parent Is TypeStatementSyntax AndAlso
                DirectCast(parent, TypeStatementSyntax).Identifier = identifier Then

                Return ClassifyTypeDeclarationIdentifier(classificationTypes, identifier)
            ElseIf TypeOf parent Is EnumStatementSyntax AndAlso
                DirectCast(parent, EnumStatementSyntax).Identifier = identifier Then

                Return classificationTypes.EnumTypeName
            ElseIf TypeOf parent Is DelegateStatementSyntax AndAlso
                DirectCast(parent, DelegateStatementSyntax).Identifier = identifier AndAlso
                (parent.Kind = SyntaxKind.DelegateSubStatement OrElse
                parent.Kind = SyntaxKind.DelegateFunctionStatement) Then

                Return classificationTypes.DelegateTypeName
            ElseIf TypeOf parent Is TypeParameterSyntax AndAlso
                DirectCast(parent, TypeParameterSyntax).Identifier = identifier Then

                Return classificationTypes.TypeParameterName
            ElseIf (identifier.GetText() = "IsTrue" OrElse identifier.GetText() = "IsFalse") AndAlso
                TypeOf parent Is OperatorStatementSyntax AndAlso
                DirectCast(parent, OperatorStatementSyntax).Operator = identifier Then

                Return classificationTypes.Keyword
            Else
                Return classificationTypes.Identifier
            End If
        End Function

        Private Function ClassifyTypeDeclarationIdentifier(classificationTypes As IClassificationTypes, identifier As SyntaxToken) As IClassificationType
            Select Case identifier.Parent.Kind
                Case SyntaxKind.ClassStatement
                    Return classificationTypes.TypeName
                Case SyntaxKind.ModuleStatement
                    Return classificationTypes.ModuleTypeName
                Case SyntaxKind.InterfaceStatement
                    Return classificationTypes.InterfaceTypeName
                Case SyntaxKind.StructureStatement
                    Return classificationTypes.StructureTypeName
                Case Else
                    Return Contract.FailWithReturn(Of IClassificationType)("Unhandled type declaration")
            End Select
        End Function
    End Module
End Namespace
