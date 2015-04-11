' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicUseOrdinalStringComparisonAnalyzer
        Inherits UseOrdinalStringComparisonAnalyzer

        Protected Overrides Sub GetAnalyzer(context As CompilationStartAnalysisContext, stringComparisonType As INamedTypeSymbol)
            context.RegisterSyntaxNodeAction(AddressOf New Analyzer(stringComparisonType).AnalyzeNode, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.InvocationExpression)
        End Sub

        Private NotInheritable Class Analyzer
            Inherits AbstractCodeBlockAnalyzer

            Public Sub New(stringComparisonType As INamedTypeSymbol)
                MyBase.New(stringComparisonType)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Select Case context.Node.Kind
                    Case SyntaxKind.InvocationExpression
                        AnalyzeInvocationExpression(DirectCast(context.Node, InvocationExpressionSyntax), context.SemanticModel, AddressOf context.ReportDiagnostic)
                    Case Else
                        AnalyzeBinaryExpression(DirectCast(context.Node, BinaryExpressionSyntax), context.SemanticModel, AddressOf context.ReportDiagnostic)
                End Select
            End Sub

            Private Sub AnalyzeInvocationExpression(node As InvocationExpressionSyntax, model As SemanticModel, reportDiagnostic As Action(Of Diagnostic))
                If (node.Expression.Kind() = SyntaxKind.SimpleMemberAccessExpression) Then
                    Dim memberAccess = CType(node.Expression, MemberAccessExpressionSyntax)
                    If memberAccess.Name IsNot Nothing AndAlso IsEqualsOrCompare(memberAccess.Name.Identifier.ValueText) Then
                        Dim methodSymbol = TryCast(model.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso methodSymbol.ContainingType.SpecialType = SpecialType.System_String Then
                            Debug.Assert(IsEqualsOrCompare(methodSymbol.Name))

                            If Not IsAcceptableOverload(methodSymbol, model) Then
                                ' wrong overload
                                reportDiagnostic(memberAccess.Name.GetLocation().CreateDiagnostic(Rule))
                            Else
                                Dim lastArgument = TryCast(node.ArgumentList.Arguments.Last(), SimpleArgumentSyntax)
                                Dim lastArgSymbol = model.GetSymbolInfo(lastArgument.Expression).Symbol
                                If lastArgSymbol IsNot Nothing AndAlso lastArgSymbol.ContainingType IsNot Nothing AndAlso
                                lastArgSymbol.ContainingType.Equals(StringComparisonType) AndAlso
                                Not IsOrdinalOrOrdinalIgnoreCase(lastArgument, model) Then
                                    ' right overload, wrong value
                                    reportDiagnostic(lastArgument.GetLocation().CreateDiagnostic(Rule))
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
