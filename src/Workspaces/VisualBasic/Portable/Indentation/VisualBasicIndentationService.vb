﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Indentation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Indentation
    <ExportLanguageService(GetType(IIndentationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend NotInheritable Class VisualBasicIndentationService
        Inherits AbstractIndentationService(Of CompilationUnitSyntax)

        Public Shared ReadOnly DefaultInstance As New VisualBasicIndentationService()
        Public Shared ReadOnly WithoutParameterAlignmentInstance As New VisualBasicIndentationService(NoOpFormattingRule.Instance)

        Private ReadOnly _specializedIndentationRule As AbstractFormattingRule

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")>
        Public Sub New()
            Me.New(Nothing)
        End Sub

        <SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification:="Intentionally used for creating multiple instances")>
        Private Sub New(specializedIndentationRule As AbstractFormattingRule)
            _specializedIndentationRule = specializedIndentationRule
        End Sub

        Protected Overrides Function GetSpecializedIndentationFormattingRule(indentStyle As FormattingOptions.IndentStyle) As AbstractFormattingRule
            Return If(_specializedIndentationRule, New SpecialFormattingRule(indentStyle))
        End Function

        Public Overloads Shared Function ShouldUseSmartTokenFormatterInsteadOfIndenter(
                formattingRules As IEnumerable(Of AbstractFormattingRule),
                root As CompilationUnitSyntax,
                line As TextLine,
                optionService As IOptionService,
                optionSet As OptionSet,
                ByRef token As SyntaxToken,
                Optional neverUseWhenHavingMissingToken As Boolean = True) As Boolean

            ' find first text on line
            Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
            If Not firstNonWhitespacePosition.HasValue Then
                Return False
            End If

            ' enter on token only works when first token on line is first text on line
            token = root.FindToken(firstNonWhitespacePosition.Value)
            If IsInvalidToken(token) Then
                Return False
            End If

            If token.Kind = SyntaxKind.None OrElse token.SpanStart <> firstNonWhitespacePosition Then
                Return False
            End If

            ' now try to gather various token information to see whether we are at an applicable position.
            ' all these are heuristic based
            ' 
            ' we need at least current and previous tokens to ask about existing line break formatting rules 
            Dim previousToken = token.GetPreviousToken(includeZeroWidth:=True)

            ' only use smart token formatter when we have at least two visible tokens.
            If previousToken.Kind = SyntaxKind.None Then
                Return False
            End If

            ' check special case 
            ' if previous token (or one before previous token if the previous token is statement terminator token) is missing, make sure
            ' we are a first token of a statement
            If previousToken.IsMissing AndAlso neverUseWhenHavingMissingToken Then
                Return False
            ElseIf previousToken.IsMissing Then
                Dim statement = token.GetAncestor(Of StatementSyntax)()
                If statement Is Nothing Then
                    Return False
                End If

                ' check whether current token is first token of a statement
                Return statement.GetFirstToken() = token
            End If

            Dim options = optionSet.AsAnalyzerConfigOptions(optionService, root.Language)

            ' now, regular case. ask formatting rule to see whether we should use token formatter or not
            Dim lineOperation = FormattingOperations.GetAdjustNewLinesOperation(formattingRules, previousToken, token, options)
            If lineOperation IsNot Nothing AndAlso lineOperation.Option <> AdjustNewLinesOption.ForceLinesIfOnSingleLine Then
                Return True
            End If

            ' check whether there is an alignment operation
            Dim startNode = token.Parent

            Dim currentNode = startNode
            Dim localToken = token
            Do While currentNode IsNot Nothing
                Dim operations = FormattingOperations.GetAlignTokensOperations(
                    formattingRules, currentNode, options)

                If Not operations.Any() Then
                    currentNode = currentNode.Parent
                    Continue Do
                End If

                ' make sure we have the given token as one of tokens to be aligned to the base token
                Dim match = operations.FirstOrDefault(Function(o) o.Tokens.Contains(localToken))
                If match IsNot Nothing Then
                    Return True
                End If

                currentNode = currentNode.Parent
            Loop

            ' no indentation operation, nothing to do for smart token formatter
            Return False
        End Function

        Private Shared Function IsInvalidToken(token As SyntaxToken) As Boolean
            ' invalid token to be formatted
            Return token.Kind = SyntaxKind.None OrElse
                   token.Kind = SyntaxKind.EndOfFileToken
        End Function
    End Class
End Namespace
