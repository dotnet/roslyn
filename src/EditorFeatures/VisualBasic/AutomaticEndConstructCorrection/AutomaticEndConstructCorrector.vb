' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    ''' <summary>
    ''' Tracks user's interaction with editor
    ''' </summary>
    Partial Friend Class AutomaticEndConstructCorrector
        Inherits AbstractCorrector

        Public Sub New(subjectBuffer As ITextBuffer, waitIndicator As IWaitIndicator)
            MyBase.New(subjectBuffer, waitIndicator)
        End Sub

        Protected Overrides Function IsAllowableTextUnderPosition(lineText As String, startIndex As Integer, length As Integer) As Boolean
            Dim textUnderPosition = lineText.Substring(startIndex, length)

            Return AutomaticEndConstructSet.Contains(textUnderPosition)
        End Function

        Protected Overrides Function GetLinkedEditSpans(snapshot As ITextSnapshot, token As SyntaxToken) As IEnumerable(Of ITrackingSpan)
            Dim startToken = GetBeginToken(token.Parent)
            If startToken.Kind = SyntaxKind.None Then
                startToken = GetCorrespondingBeginToken(token)
            End If

            Dim endToken = GetCorrespondingEndToken(startToken)

            Return {New LetterOnlyTrackingSpan(startToken.Span.ToSnapshotSpan(snapshot)), New LetterOnlyTrackingSpan(endToken.Span.ToSnapshotSpan(snapshot))}
        End Function

        Protected Overrides Function TryGetValidToken(bufferChanges As TextContentChangedEventArgs, ByRef token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Dim changes = bufferChanges.Changes
            Dim textChange = changes.Item(0)

            Dim root = Me.PreviousDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            token = root.FindToken(textChange.OldPosition)

            If Not IsChangeOnCorrectToken(token) Then
                Return False
            End If

            Return True
        End Function

        Private Function IsChangeOnCorrectToken(token As SyntaxToken) As Boolean
            Select Case token.Kind
                Case SyntaxKind.StructureKeyword, SyntaxKind.EnumKeyword, SyntaxKind.InterfaceKeyword,
                     SyntaxKind.ClassKeyword, SyntaxKind.ModuleKeyword, SyntaxKind.NamespaceKeyword,
                     SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.GetKeyword, SyntaxKind.SetKeyword

                    If token.Parent Is Nothing Then
                        Return False
                    End If

                    ' we found right token, let's see whether we are under right context
                    If IsChangeOnBeginToken(token) Then
                        Return CorrespondingEndTokenExist(token)
                    End If

                    If IsChangeOnEndToken(token) Then
                        Return CorrespondingBeginTokenExist(token)
                    End If
            End Select

            Return False
        End Function

        Private Function CorrespondingBeginTokenExist(token As SyntaxToken) As Boolean
            Dim pairToken = GetCorrespondingBeginToken(token)

            Dim hasValidToken = pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token.ValueText = pairToken.ValueText
            If Not hasValidToken Then
                Return False
            End If

            If BeginStatementIsInValidForm(pairToken.Parent) Then
                Return True
            End If

            Return False
        End Function

        Private Function GetCorrespondingBeginToken(token As SyntaxToken) As SyntaxToken
            If token.Parent.Parent Is Nothing Then
                Return New SyntaxToken()
            End If

            Dim beginNode = token.Parent.Parent.TypeSwitch(
                        Function(context As TypeBlockSyntax) context.BlockStatement,
                        Function(context As EnumBlockSyntax) context.EnumStatement,
                        Function(context As NamespaceBlockSyntax) context.NamespaceStatement,
                        Function(context As MethodBlockBaseSyntax) context.BlockStatement,
                        Function(context As MultiLineLambdaExpressionSyntax) context.SubOrFunctionHeader,
                        Function(dontCare As SyntaxNode) CType(Nothing, SyntaxNode))

            If beginNode Is Nothing Then
                Return New SyntaxToken()
            End If

            Return GetBeginToken(beginNode)
        End Function

        Private Function IsChangeOnEndToken(token As SyntaxToken) As Boolean
            Dim endBlockStatement = TryCast(token.Parent, EndBlockStatementSyntax)
            If endBlockStatement Is Nothing Then
                Return False
            End If

            Return endBlockStatement.BlockKeyword = token
        End Function

        Private Function CorrespondingEndTokenExist(token As SyntaxToken) As Boolean
            Dim pairToken = GetCorrespondingEndToken(token)

            Return pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token.ValueText = pairToken.ValueText
        End Function

        Private Function GetCorrespondingEndToken(token As SyntaxToken) As SyntaxToken
            If token.Parent.Parent Is Nothing Then
                Return New SyntaxToken()
            End If

            Return token.Parent.Parent.TypeSwitch(
                        Function(context As TypeBlockSyntax) context.EndBlockStatement.BlockKeyword,
                        Function(context As EnumBlockSyntax) context.EndEnumStatement.BlockKeyword,
                        Function(context As NamespaceBlockSyntax) context.EndNamespaceStatement.BlockKeyword,
                        Function(context As MethodBlockBaseSyntax) context.EndBlockStatement.BlockKeyword,
                        Function(context As MultiLineLambdaExpressionSyntax) context.EndSubOrFunctionStatement.BlockKeyword,
                        Function(dontCare As SyntaxNode) New SyntaxToken())
        End Function

        Private Function IsChangeOnBeginToken(token As SyntaxToken) As Boolean
            Dim pairToken = GetBeginToken(token.Parent)

            Dim hasValidToken = pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token = pairToken
            If Not hasValidToken Then
                Return False
            End If

            If BeginStatementIsInValidForm(token.Parent) Then
                Return True
            End If

            Return False
        End Function

        Private Function BeginStatementIsInValidForm(node As SyntaxNode) As Boolean
            ' turns out in malformed code, parser would pair some constructs together even if user wouldn't consider them being
            ' paired. So, rather than the feature being very naive, we will make sure begin construct have at least some valid shape.
            Return node.TypeSwitch(
                        Function(context As TypeStatementSyntax) Not context.Identifier.IsMissing,
                        Function(context As EnumStatementSyntax) Not context.Identifier.IsMissing,
                        Function(context As NamespaceStatementSyntax) context.Name IsNot Nothing,
                        Function(context As MethodStatementSyntax) Not context.Identifier.IsMissing,
                        Function(context As AccessorStatementSyntax) Not context.DeclarationKeyword.IsMissing,
                        Function(context As LambdaHeaderSyntax) True,
                        Function(dontCare As SyntaxNode) False)
        End Function

        Private Function GetBeginToken(node As SyntaxNode) As SyntaxToken
            Return node.TypeSwitch(
                        Function(context As TypeStatementSyntax) context.DeclarationKeyword,
                        Function(context As EnumStatementSyntax) context.EnumKeyword,
                        Function(context As NamespaceStatementSyntax) context.NamespaceKeyword,
                        Function(context As MethodStatementSyntax) context.DeclarationKeyword,
                        Function(context As LambdaHeaderSyntax) context.DeclarationKeyword,
                        Function(context As AccessorStatementSyntax) context.DeclarationKeyword,
                        Function(dontCare As SyntaxNode) New SyntaxToken())
        End Function
    End Class
End Namespace
