' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Inherits SimplifyTypeNamesDiagnosticAnalyzerBase(Of SyntaxKind)

        Private Shared ReadOnly s_kindsOfInterest As SyntaxKind() =
        {
            SyntaxKind.QualifiedName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.GenericName
        }

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, s_kindsOfInterest)
        End Sub

        Protected Overrides Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            If context.Node.Ancestors(ascendOutOfTrivia:=False).Any(AddressOf IsNodeKindInteresting) Then
                ' Already simplified an ancestor of this node.
                Return
            End If

            Dim descendIntoChildren As Func(Of SyntaxNode, Boolean) =
                Function(n)
                    Dim diagnostic As diagnostic = Nothing

                    If Not IsCandidate(n) OrElse
                       Not TrySimplifyTypeNameExpression(context.SemanticModel, n, context.Options, diagnostic, context.CancellationToken) Then
                        Return True
                    End If

                    context.ReportDiagnostic(diagnostic)
                    Return False
                End Function

            For Each candidate In context.Node.DescendantNodesAndSelf(descendIntoChildren, descendIntoTrivia:=True)
                context.CancellationToken.ThrowIfCancellationRequested()
            Next
        End Sub

        Private Shared Function IsNodeKindInteresting(node As SyntaxNode) As Boolean
            Return s_kindsOfInterest.Contains(node.Kind)
        End Function

        Friend Shared Function IsCandidate(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso IsNodeKindInteresting(node)
        End Function

        Protected Overrides Function CanSimplifyTypeNameExpressionCore(model As SemanticModel, node As SyntaxNode, optionSet As OptionSet, ByRef issueSpan As TextSpan, ByRef diagnosticId As String, cancellationToken As CancellationToken) As Boolean
            Return CanSimplifyTypeNameExpression(model, node, optionSet, issueSpan, diagnosticId, cancellationToken)
        End Function

        Friend Shared Function CanSimplifyTypeNameExpression(model As SemanticModel, node As SyntaxNode, optionSet As OptionSet, ByRef issueSpan As TextSpan, ByRef diagnosticId As String, cancellationToken As CancellationToken) As Boolean
            issueSpan = Nothing
            diagnosticId = IDEDiagnosticIds.SimplifyNamesDiagnosticId

            Dim expression = DirectCast(node, ExpressionSyntax)
            If expression.ContainsDiagnostics Then
                Return False
            End If

            Dim replacementSyntax As ExpressionSyntax = Nothing
            If Not expression.TryReduceOrSimplifyExplicitName(model, replacementSyntax, issueSpan, optionSet, cancellationToken) Then
                Return False
            End If

            If expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                diagnosticId = If(memberAccess.Expression.Kind = SyntaxKind.MeExpression,
                    IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId,
                    IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId)
            End If

            Return True
        End Function

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
