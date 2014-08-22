' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2214DiagnosticAnalyzer
        Inherits CA2214DiagnosticAnalyzer

        Protected Overrides Function GetCodeBlockEndedAnalyzer(constructorSymbol As IMethodSymbol) As IDiagnosticAnalyzer
            Return New SyntaxNodeAnalyzer(constructorSymbol)
        End Function

        Private NotInheritable Class SyntaxNodeAnalyzer
            Inherits AbstractSyntaxNodeAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.InvocationExpression)
            Private _containingType As INamedTypeSymbol

            Public Sub New(constructorSymbol As IMethodSymbol)
                _containingType = constructorSymbol.ContainingType
            End Sub

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return _kindsOfInterest
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                ' TODO: For this to be correct, we need flow analysis to determine if a given method
                ' is actually invoked inside the current constructor. A method may be assigned to a
                ' delegate which can be called inside or outside the constructor. A method may also
                ' be called from within a lambda which is called inside or outside the constructor.
                ' Currently, FxCop does not produce a warning if a virtual method is called indirectly
                ' through a delegate or through a lambda.

                Dim invocationExpression = DirectCast(node, InvocationExpressionSyntax)
                Dim method = TryCast(SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol, IMethodSymbol)
                If method IsNot Nothing AndAlso
                   (method.IsAbstract OrElse method.IsVirtual) AndAlso
                   method.ContainingType.Equals(_containingType) Then
                    addDiagnostic(invocationExpression.Expression.CreateDiagnostic(Rule))
                End If
            End Sub
        End Class
    End Class
End Namespace