' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
    Partial Friend Class VisualBasicCodeCleanerService
        Inherits AbstractCodeCleanerService

        Private Shared ReadOnly s_defaultProviders As ImmutableArray(Of ICodeCleanupProvider) = ImmutableArray.Create(Of ICodeCleanupProvider)(
            New AddMissingTokensCodeCleanupProvider(),
            New FixIncorrectTokensCodeCleanupProvider(),
            New ReduceTokensCodeCleanupProvider(),
            New NormalizeModifiersOrOperatorsCodeCleanupProvider(),
            New RemoveUnnecessaryLineContinuationCodeCleanupProvider(),
            New CaseCorrectionCodeCleanupProvider(),
            New SimplificationCodeCleanupProvider(),
            New FormatCodeCleanupProvider())

        Public Overrides Function GetDefaultProviders() As ImmutableArray(Of ICodeCleanupProvider)
            Return s_defaultProviders
        End Function

        Protected Overrides Function GetSpansToAvoid(root As SyntaxNode) As ImmutableArray(Of TextSpan)
            ' We don't want to touch nodes in the document that have syntax errors on them and which
            ' contain multi-line string literals.  It's quite possible that there is some string 
            ' literal on a previous line that was intended to be terminated on that line, but which
            ' wasn't.  The string may then have terminated on this line (because of the start of 
            ' another literal) causing the literal contents to then be considered code.  We don't 
            ' want to cleanup 'code' that the user intends to be the content of a string literal.

            Dim result = ArrayBuilder(Of TextSpan).GetInstance()

            ProcessNode(root, result)

            Return result.ToImmutableAndFree()
        End Function

        Private Shared Sub ProcessNode(node As SyntaxNode, result As ArrayBuilder(Of TextSpan))
            If SkipProcessing(node, result) Then
                Return
            End If

            For Each child In node.ChildNodesAndTokens()
                If child.IsNode Then
                    ProcessNode(child.AsNode(), result)
                Else
                    ProcessToken(child.AsToken(), result)
                End If
            Next
        End Sub

        Private Shared Sub ProcessToken(token As SyntaxToken, result As ArrayBuilder(Of TextSpan))
            If SkipProcessing(token, result) Then
                Return
            End If

            Dim parentMultiLineNode = GetMultiLineContainer(token.Parent)
            If parentMultiLineNode IsNot Nothing Then
                If ContainsMultiLineStringLiteral(parentMultiLineNode) Then
                    result.Add(parentMultiLineNode.FullSpan)
                End If
            End If
        End Sub

        Private Shared Function SkipProcessing(nodeOrToken As SyntaxNodeOrToken, result As ArrayBuilder(Of TextSpan)) As Boolean
            ' Don't bother looking at nodes or token that don't have any syntax errors in them.
            If Not nodeOrToken.ContainsDiagnostics Then
                Return True
            End If

            If result.Count > 0 AndAlso result.Last.Contains(nodeOrToken.Span) Then
                ' Don't bother looking at nodes or token that are contained within a span we've already 
                ' marked as something to avoid. We would only ever produce mark the same (or smaller) 
                ' span again.
                Return True
            End If

            Return False
        End Function

        Private Shared Function ContainsMultiLineStringLiteral(node As SyntaxNode) As Boolean
            Return node.DescendantTokens().Any(
                Function(t)
                    If t.Kind() = SyntaxKind.StringLiteralToken OrElse
                       t.Kind() = SyntaxKind.InterpolatedStringTextToken Then
                        Return Not VisualBasicSyntaxFacts.Instance.IsOnSingleLine(t.Parent, fullSpan:=False)
                    End If

                    Return False
                End Function)
        End Function

        Private Shared Function GetMultiLineContainer(node As SyntaxNode) As SyntaxNode
            If node Is Nothing Then
                Return Nothing
            End If

            If Not VisualBasicSyntaxFacts.Instance.IsOnSingleLine(node, fullSpan:=False) Then
                Return node
            End If

            Return GetMultiLineContainer(node.Parent)
        End Function
    End Class
End Namespace
