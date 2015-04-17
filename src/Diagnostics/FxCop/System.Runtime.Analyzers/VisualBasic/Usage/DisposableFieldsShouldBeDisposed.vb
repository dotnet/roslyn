' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2213DiagnosticAnalyzer
        Inherits DisposableFieldsShouldBeDisposedAnalyzer

        Protected Overrides Function GetAnalyzer(context As CompilationStartAnalysisContext, disposableType As INamedTypeSymbol) As AbstractAnalyzer
            Dim analyzer As New Analyzer(disposableType)
            context.RegisterSyntaxNodeAction(AddressOf analyzer.AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.UsingStatement)
            Return analyzer
        End Function

        Private NotInheritable Class Analyzer
            Inherits AbstractAnalyzer

            Public Sub New(disposableType As INamedTypeSymbol)
                MyBase.New(disposableType)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Select Case context.Node.Kind
                    Case SyntaxKind.SimpleMemberAccessExpression
                        ' NOTE: This cannot be optimized based on memberAccess.Name because a given method
                        ' may be an explicit interface implementation of IDisposable.Dispose.
                        ' If the right hand side of the member access binds to IDisposable.Dispose
                        Dim memberAccess = DirectCast(context.Node, MemberAccessExpressionSyntax)
                        Dim methodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso
                            (methodSymbol.MetadataName = Dispose OrElse methodSymbol.ExplicitInterfaceImplementations.Any(Function(m) m.MetadataName = Dispose)) Then

                            Dim recieverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type
                            If (recieverType.Inherits(_disposableType)) Then
                                Dim exp = RemoveParentheses(memberAccess.Expression)
                                ' this can be simply x.Dispose() where x is the field.
                                Dim fieldSymbol = TryCast(context.SemanticModel.GetSymbolInfo(exp).Symbol, IFieldSymbol)
                                If fieldSymbol IsNot Nothing Then
                                    NoteFieldDisposed(fieldSymbol)
                                Else
                                    ' Or it can be an explicit interface dispatch like DirectCast(f, IDisposable).Dispose()
                                    Dim expression = RemoveParentheses(memberAccess.Expression)
                                    If (expression.IsKind(SyntaxKind.DirectCastExpression) OrElse expression.IsKind(SyntaxKind.TryCastExpression)) OrElse expression.IsKind(SyntaxKind.CTypeExpression) Then
                                        Dim castExpression = DirectCast(expression, CastExpressionSyntax)
                                        fieldSymbol = TryCast(context.SemanticModel.GetSymbolInfo(castExpression.Expression).Symbol, IFieldSymbol)
                                        If (fieldSymbol IsNot Nothing) Then
                                            NoteFieldDisposed(fieldSymbol)
                                        End If
                                    End If
                                End If
                            End If
                        End If

                    Case SyntaxKind.UsingStatement
                            Dim usingStatementExpression = RemoveParentheses(DirectCast(context.Node, UsingStatementSyntax).Expression)
                        If usingStatementExpression IsNot Nothing Then
                            Dim fieldSymbol = TryCast(context.SemanticModel.GetSymbolInfo(usingStatementExpression).Symbol, IFieldSymbol)
                            If fieldSymbol IsNot Nothing Then
                                NoteFieldDisposed(fieldSymbol)
                            End If
                        End If
                End Select
            End Sub

            Private Function RemoveParentheses(exp As ExpressionSyntax) As ExpressionSyntax
                Dim syntax = TryCast(exp, ParenthesizedExpressionSyntax)
                If syntax IsNot Nothing Then
                    Return RemoveParentheses(syntax.Expression)
                End If

                Return exp
            End Function
        End Class
    End Class
End Namespace
