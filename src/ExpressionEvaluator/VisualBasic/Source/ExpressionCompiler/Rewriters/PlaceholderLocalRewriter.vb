' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class PlaceholderLocalRewriter
        Inherits BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

        Friend Shared Function Rewrite(compilation As VisualBasicCompilation, container As EENamedTypeSymbol, node As BoundNode, diagnostics As DiagnosticBag) As BoundNode
            Dim rewriter As New PlaceholderLocalRewriter(compilation, container, diagnostics)
            Return rewriter.Visit(node)
        End Function

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _container As EENamedTypeSymbol
        Private ReadOnly _diagnostics As DiagnosticBag

        Private Sub New(compilation As VisualBasicCompilation, container As EENamedTypeSymbol, diagnostics As DiagnosticBag)
            _compilation = compilation
            _container = container
            _diagnostics = diagnostics
        End Sub

        Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            Dim result = RewriteLocal(node, _diagnostics)
            Debug.Assert(TypeSymbol.Equals(result.Type, node.Type, TypeCompareKind.ConsiderEverything))
            Debug.Assert(result.IsLValue = node.IsLValue)
            Return result
        End Function

        Private Function RewriteLocal(node As BoundLocal, diagnostics As DiagnosticBag) As BoundExpression
            Dim local = node.LocalSymbol
            If local.DeclarationKind = LocalDeclarationKind.ImplicitVariable Then
                Return ObjectIdLocalSymbol.RewriteLocal(_compilation, _container, node.Syntax, local, node.IsLValue)
            End If
            Dim placeholder = TryCast(local, PlaceholderLocalSymbol)
            If placeholder IsNot Nothing Then
                Return placeholder.RewriteLocal(_compilation, _container, node.Syntax, node.IsLValue, diagnostics)
            End If
            Return node
        End Function

    End Class
End Namespace
