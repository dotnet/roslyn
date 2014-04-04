' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA2214DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA2214DiagnosticAnalyzer
        Inherits CA2214DiagnosticAnalyzer

        Protected Overrides Function GetCodeBlockEndedAnalyzer() As ICodeBlockEndedAnalyzer
            Return New SyntaxNodeAnalyzer()
        End Function

        Private NotInheritable Class SyntaxNodeAnalyzer
            Inherits AbstractSyntaxNodeAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.IdentifierName)

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return _kindsOfInterest
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                ' TODO: should we restrict this to invocation, delegate creation, etc?
                Select Case node.VisualBasicKind()
                    Case SyntaxKind.IdentifierName
                        Dim id = DirectCast(node, IdentifierNameSyntax)
                        Dim method = TryCast(semanticModel.GetSymbolInfo(id).Symbol, IMethodSymbol)
                        If method Is Nothing OrElse Not (method.IsAbstract OrElse method.IsVirtual) Then
                            Return
                        End If

                        addDiagnostic(id.CreateDiagnostic(Rule))

                    Case SyntaxKind.SimpleMemberAccessExpression
                        Dim qid = DirectCast(node, MemberAccessExpressionSyntax)
                        Dim method = TryCast(semanticModel.GetSymbolInfo(qid).Symbol, IMethodSymbol)
                        If method Is Nothing OrElse Not (method.IsAbstract OrElse method.IsVirtual) Then
                            Return
                        End If

                        If qid.Expression.VisualBasicKind() = SyntaxKind.MyBaseExpression Then
                            Return
                        End If

                        Dim receiver = TryCast(semanticModel.GetSymbolInfo(qid.Expression).Symbol, IParameterSymbol)
                        If receiver Is Nothing OrElse Not receiver.IsThis Then
                            Return
                        End If

                        addDiagnostic(qid.CreateDiagnostic(Rule))
                End Select
            End Sub
        End Class
    End Class
End Namespace