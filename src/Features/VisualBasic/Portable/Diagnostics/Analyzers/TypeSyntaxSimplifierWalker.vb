' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
    Friend Class TypeSyntaxSimplifierWalker
        Inherits VisualBasicSyntaxWalker

        Private ReadOnly _analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Private ReadOnly _semanticModel As SemanticModel
        Private ReadOnly _optionSet As OptionSet
        Private ReadOnly _cancellationToken As CancellationToken

        Public ReadOnly Property Diagnostics As List(Of Diagnostic) = New List(Of Diagnostic)()

        Public Sub New(analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer, semanticModel As SemanticModel, optionSet As OptionSet, cancellationToken As CancellationToken)
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)

            _analyzer = analyzer
            _semanticModel = semanticModel
            _optionSet = optionSet
            _cancellationToken = cancellationToken
        End Sub

        Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
            If node.IsKind(SyntaxKind.QualifiedName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitQualifiedName(node)
        End Sub

        Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
            If node.IsKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitMemberAccessExpression(node)
        End Sub

        Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
            ' Always try to simplify identifiers with an 'Attribute' suffix.
            '
            ' In other cases, don't bother looking at the right side of A.B or A!B. We will process those in
            ' one of our other top level Visit methods (Like VisitQualifiedName).
            Dim canTrySimplify = CaseInsensitiveComparison.EndsWith(node.Identifier.ValueText, "Attribute") _
                OrElse Not node.IsRightSideOfDotOrBang()

            If canTrySimplify AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitIdentifierName(node)
        End Sub

        Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
            If node.IsKind(SyntaxKind.GenericName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitGenericName(node)
        End Sub

        Private Function TrySimplify(node As SyntaxNode) As Boolean
            Dim diagnostic As Diagnostic = Nothing
            If Not _analyzer.TrySimplify(_semanticModel, node, diagnostic, _optionSet, _cancellationToken) Then
                Return False
            End If

            Diagnostics.Add(diagnostic)
            Return True
        End Function
    End Class
End Namespace
