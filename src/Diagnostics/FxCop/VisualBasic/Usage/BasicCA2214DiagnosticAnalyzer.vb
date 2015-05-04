' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2214DiagnosticAnalyzer
        Inherits CA2214DiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Sub GetCodeBlockEndedAnalyzer(context As CodeBlockStartAnalysisContext(Of SyntaxKind), constructorSymbol As IMethodSymbol)
            context.RegisterSyntaxNodeAction(AddressOf New SyntaxNodeAnalyzer(constructorSymbol).AnalyzeNode, SyntaxKind.InvocationExpression)
        End Sub

        Private NotInheritable Class SyntaxNodeAnalyzer

            Private ReadOnly _containingType As INamedTypeSymbol

            Public Sub New(constructorSymbol As IMethodSymbol)
                _containingType = constructorSymbol.ContainingType
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                ' TODO: For this to be correct, we need flow analysis to determine if a given method
                ' is actually invoked inside the current constructor. A method may be assigned to a
                ' delegate which can be called inside or outside the constructor. A method may also
                ' be called from within a lambda which is called inside or outside the constructor.
                ' Currently, FxCop does not produce a warning if a virtual method is called indirectly
                ' through a delegate or through a lambda.

                Dim invocationExpression = DirectCast(context.Node, InvocationExpressionSyntax)
                Dim method = TryCast(context.SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol, IMethodSymbol)
                If method IsNot Nothing AndAlso
                   (method.IsAbstract OrElse method.IsVirtual) AndAlso
                   method.ContainingType.Equals(_containingType) Then
                    context.ReportDiagnostic(invocationExpression.Expression.CreateDiagnostic(Rule))
                End If
            End Sub
        End Class
    End Class
End Namespace
