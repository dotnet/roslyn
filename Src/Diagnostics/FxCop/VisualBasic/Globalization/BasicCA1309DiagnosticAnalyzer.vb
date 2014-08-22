' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Globalization
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1309DiagnosticAnalyzer
        Inherits CA1309DiagnosticAnalyzer

        Protected Overrides Function GetAnalyzer(stringComparisonType As INamedTypeSymbol) As AbstractCodeBlockAnalyzer
            Return New Analyzer(stringComparisonType)
        End Function

        Private NotInheritable Class Analyzer
            Inherits AbstractCodeBlockAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.InvocationExpression)

            Public Sub New(stringComparisonType As INamedTypeSymbol)
                MyBase.New(stringComparisonType)
            End Sub

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return _kindsOfInterest
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Select Case node.VisualBasicKind
                    Case SyntaxKind.InvocationExpression
                        AnalyzeInvocationExpression(DirectCast(node, InvocationExpressionSyntax), semanticModel, addDiagnostic)
                    Case Else
                        AnalyzeBinaryExpression(DirectCast(node, BinaryExpressionSyntax), semanticModel, addDiagnostic)
                End Select
            End Sub

            Private Sub AnalyzeInvocationExpression(node As InvocationExpressionSyntax, model As SemanticModel, addDiagnostic As Action(Of Diagnostic))
                If (node.Expression.VisualBasicKind() = SyntaxKind.SimpleMemberAccessExpression) Then
                    Dim memberAccess = CType(node.Expression, MemberAccessExpressionSyntax)
                    If memberAccess.Name IsNot Nothing AndAlso IsEqualsOrCompare(memberAccess.Name.Identifier.ValueText) Then
                        Dim methodSymbol = TryCast(model.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso methodSymbol.ContainingType.SpecialType = SpecialType.System_String Then
                            Debug.Assert(IsEqualsOrCompare(methodSymbol.Name))

                            If Not IsAcceptableOverload(methodSymbol, model) Then
                                ' wrong overload
                                addDiagnostic(memberAccess.Name.GetLocation().CreateDiagnostic(Rule))
                            Else
                                Dim lastArgument = TryCast(node.ArgumentList.Arguments.Last(), SimpleArgumentSyntax)
                                Dim lastArgSymbol = model.GetSymbolInfo(lastArgument.Expression).Symbol
                                If lastArgSymbol IsNot Nothing AndAlso lastArgSymbol.ContainingType IsNot Nothing AndAlso
                                lastArgSymbol.ContainingType.Equals(StringComparisonType) AndAlso
                                Not IsOrdinalOrOrdinalIgnoreCase(lastArgument, model) Then
                                    ' right overload, wrong value
                                    addDiagnostic(lastArgument.GetLocation().CreateDiagnostic(Rule))
                                End If
                            End If
                        End If
                    End If
                End If
            End Sub

            Private Shared Sub AnalyzeBinaryExpression(node As BinaryExpressionSyntax, model As SemanticModel, addDiagnostic As Action(Of Diagnostic))
                Dim leftType = model.GetTypeInfo(node.Left).Type
                Dim rightType = model.GetTypeInfo(node.Right).Type
                If leftType IsNot Nothing AndAlso rightType IsNot Nothing AndAlso leftType.SpecialType = SpecialType.System_String AndAlso rightType.SpecialType = SpecialType.System_String Then
                    addDiagnostic(node.OperatorToken.GetLocation().CreateDiagnostic(Rule))
                End If
            End Sub

            Private Overloads Shared Function IsOrdinalOrOrdinalIgnoreCase(argumentSyntax As SimpleArgumentSyntax, model As SemanticModel) As Boolean
                Dim argumentSymbol As ISymbol = model.GetSymbolInfo(argumentSyntax.Expression).Symbol
                If argumentSymbol IsNot Nothing Then
                    Return IsOrdinalOrOrdinalIgnoreCase(argumentSymbol.Name)
                End If

                Return False
            End Function
        End Class
    End Class
End Namespace
