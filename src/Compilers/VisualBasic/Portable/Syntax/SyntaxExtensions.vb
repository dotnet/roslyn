' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Public Module SyntaxExtensions
        <Extension()>
        Friend Function ReportDocumentationCommentDiagnostics(tree As SyntaxTree) As Boolean
            Return tree.Options.DocumentationMode >= DocumentationMode.Diagnose
        End Function

        <Extension()>
        Public Function ToSyntaxTriviaList(sequence As IEnumerable(Of SyntaxTrivia)) As SyntaxTriviaList
            Return SyntaxFactory.TriviaList(sequence)
        End Function

        <Extension()>
        Public Function NormalizeWhitespace(Of TNode As SyntaxNode)(node As TNode, useDefaultCasing As Boolean, indentation As String, elasticTrivia As Boolean) As TNode
            Return CType(SyntaxNormalizer.Normalize(node, indentation, Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, elasticTrivia, useDefaultCasing), TNode)
        End Function

        <Extension()>
        Public Function NormalizeWhitespace(Of TNode As SyntaxNode)(node As TNode, useDefaultCasing As Boolean, Optional indentation As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, Optional eol As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, Optional elasticTrivia As Boolean = False) As TNode
            Return CType(SyntaxNormalizer.Normalize(node, indentation, eol, elasticTrivia, useDefaultCasing), TNode)
        End Function

        <Extension()>
        Public Function NormalizeWhitespace(token As SyntaxToken, indentation As String, elasticTrivia As Boolean) As SyntaxToken
            Return SyntaxNormalizer.Normalize(token, indentation, Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, elasticTrivia, False)
        End Function

        <Extension()>
        Public Function NormalizeWhitespace(token As SyntaxToken, Optional indentation As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, Optional eol As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, Optional elasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxToken
            Return SyntaxNormalizer.Normalize(token, indentation, eol, elasticTrivia, useDefaultCasing)
        End Function

        <Extension()>
        Public Function NormalizeWhitespace(trivia As SyntaxTriviaList, Optional indentation As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, Optional eol As String = Microsoft.CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, Optional elasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxTriviaList
            Return SyntaxNormalizer.Normalize(trivia, indentation, eol, elasticTrivia, useDefaultCasing)
        End Function

        ''' <summary>
        ''' Returns the TypeSyntax of the given NewExpressionSyntax if specified.
        ''' </summary>
        <Extension()> _
        Public Function Type(newExpressionSyntax As NewExpressionSyntax) As TypeSyntax
            Select Case newExpressionSyntax.Kind
                Case SyntaxKind.ObjectCreationExpression
                    Return DirectCast(newExpressionSyntax, ObjectCreationExpressionSyntax).Type
                Case SyntaxKind.AnonymousObjectCreationExpression
                    Return Nothing
                Case SyntaxKind.ArrayCreationExpression
                    Return DirectCast(newExpressionSyntax, ArrayCreationExpressionSyntax).Type
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(newExpressionSyntax.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Returns the TypeSyntax of the given AsClauseSyntax if specified.
        ''' </summary>
        <Extension()> _
        Public Function Type(asClauseSyntax As AsClauseSyntax) As TypeSyntax
            Select Case asClauseSyntax.Kind
                Case SyntaxKind.SimpleAsClause
                    Return DirectCast(asClauseSyntax, SimpleAsClauseSyntax).Type
                Case SyntaxKind.AsNewClause
                    Return DirectCast(asClauseSyntax, AsNewClauseSyntax).NewExpression.Type
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(asClauseSyntax.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Returns the AttributeBlockSyntax of the given AsClauseSyntax if specified.
        ''' </summary>
        <Extension()> _
        Public Function Attributes(asClauseSyntax As AsClauseSyntax) As SyntaxList(Of AttributeListSyntax)
            Select Case asClauseSyntax.Kind
                Case SyntaxKind.SimpleAsClause
                    Return DirectCast(asClauseSyntax, SimpleAsClauseSyntax).AttributeLists
                Case SyntaxKind.AsNewClause
                    Return DirectCast(asClauseSyntax, AsNewClauseSyntax).NewExpression.AttributeLists
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(asClauseSyntax.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Updates the given SimpleNameSyntax node with the given identifier token.
        ''' This function is a wrapper that calls WithIdentifier on derived syntax nodes.
        ''' </summary>
        ''' <param name="simpleName"></param>
        ''' <param name="identifier"></param>
        ''' <returns>The given simple name updated with the given identifier.</returns>
        <Extension>
        Public Function WithIdentifier(simpleName As SimpleNameSyntax, identifier As SyntaxToken) As SimpleNameSyntax
            Return If(simpleName.Kind = SyntaxKind.IdentifierName,
                      DirectCast(DirectCast(simpleName, IdentifierNameSyntax).WithIdentifier(identifier), SimpleNameSyntax),
                      DirectCast(DirectCast(simpleName, GenericNameSyntax).WithIdentifier(identifier), SimpleNameSyntax))
        End Function

        ''' <summary>
        ''' Given an initializer expression infer the name of anonymous property or tuple element.
        ''' Returns Nothing if unsuccessful
        ''' </summary>
        <Extension>
        Public Function TryGetInferredMemberName(syntax As SyntaxNode) As String
            If syntax Is Nothing Then
                Return Nothing
            End If

            Dim expr = TryCast(syntax, ExpressionSyntax)
            If expr Is Nothing Then
                Return Nothing
            End If

            Dim ignore As XmlNameSyntax = Nothing
            Dim nameToken As SyntaxToken = expr.ExtractAnonymousTypeMemberName(ignore)
            Return If(nameToken.Kind() = SyntaxKind.IdentifierToken, nameToken.ValueText, Nothing)
        End Function
    End Module
End Namespace
