' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Desktop.Analyzers.Common

Namespace Desktop.Analyzers

    ''' <summary>
    ''' CA2153: Do not catch corrupted state exceptions in general handlers
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDoNotCatchCorruptedStateExceptionsAnalyzer
        Inherits DoNotCatchCorruptedStateExceptionsAnalyzer

        Shared ReadOnly s_nodeKindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
                SyntaxKind.SubBlock,
                SyntaxKind.FunctionBlock,
                SyntaxKind.OperatorBlock,
                SyntaxKind.AddHandlerAccessorBlock,
                SyntaxKind.RemoveHandlerAccessorBlock,
                SyntaxKind.GetAccessorBlock,
                SyntaxKind.SetAccessorBlock)


        Protected Overrides Function GetAnalyzer(context As CompilationStartAnalysisContext, compilationTypes As CompilationSecurityTypes) As Analyzer
            Dim analyzer = New BasicAnalyzer(compilationTypes)
            context.RegisterSyntaxNodeAction(AddressOf analyzer.AnalyzeNode, s_nodeKindsOfInterest)
            Return analyzer
        End Function

        Private NotInheritable Class BasicAnalyzer
            Inherits Analyzer

            Public Sub New(compilationTypes As CompilationSecurityTypes)
                MyBase.New(compilationTypes)
            End Sub

            Private Function MayContainCatchNodesOfInterest(node As SyntaxNode) As Boolean

                Dim kind = node.Kind()
                Select Case kind
                    Case SyntaxKind.MultiLineFunctionLambdaExpression
                    Case SyntaxKind.MultiLineSubLambdaExpression
                    Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Case SyntaxKind.SingleLineSubLambdaExpression
                        ' for now there doesn't seem to have any way to annotate lambdas with attributes
                        Return False
                    Case Else
                        Return True
                End Select
                Return True
            End Function


            Protected Overrides Sub CheckNode(methodNode As SyntaxNode, model As SemanticModel, reportDiagnostic As Action(Of Diagnostic))

                Debug.Assert(s_nodeKindsOfInterest.Contains(methodNode.Kind()))

                For Each node As SyntaxNode In methodNode.DescendantNodes(AddressOf MayContainCatchNodesOfInterest)

                    Dim kind As SyntaxKind = node.Kind()
                    If (kind <> SyntaxKind.CatchBlock) Then
                        Continue For
                    End If

                    Dim exceptionTypeSym As ISymbol = Nothing
                    Dim catchNode As CatchBlockSyntax = CType(node, CatchBlockSyntax)

                    Debug.Assert(catchNode.CatchStatement IsNot Nothing)

                    Dim catchDeclaration As SimpleAsClauseSyntax = catchNode.CatchStatement.AsClause
                    If (catchDeclaration IsNot Nothing) Then
                        exceptionTypeSym = SyntaxNodeHelper.GetSymbol(catchDeclaration.Type, model)
                        If (Not IsCatchTypeTooGeneral(exceptionTypeSym)) Then
                            Continue For
                        End If
                    End If

                    If (Not HasCorrespondingRethrowInSubTree(catchNode)) Then
                        reportDiagnostic(
                            Diagnostic.Create(
                                Rule,
                                catchNode.GetLocation(),
                                SyntaxNodeHelper.GetSymbol(methodNode, model).ToDisplayString(),
                                If(exceptionTypeSym IsNot Nothing,
                                   exceptionTypeSym,
                                   TypesOfInterest.SystemObject).ToDisplayString()))
                    End If
                Next
            End Sub

            Private Function HasCorrespondingRethrowInSubTree(catchNode As SyntaxNode) As Boolean

                Debug.Assert(catchNode.Kind() = SyntaxKind.CatchBlock)

                Dim shouldDescend As Func(Of SyntaxNode, Boolean) =
                    Function(node) (node.Kind() <> SyntaxKind.CatchBlock) Or (node Is catchNode)

                For Each n In catchNode.DescendantNodes(shouldDescend)
                    If (n.Kind() = SyntaxKind.ThrowStatement) Then
                        Dim t As ThrowStatementSyntax = CType(n, ThrowStatementSyntax)
                        If (t.Expression Is Nothing) Then

                            ' We make the same assumption FxCop makes here -- one re-throw implies the dev
                            ' understands what he Is doing with corrupted process state exceptions
                            Return True
                        End If
                    End If
                Next

                Return False
            End Function
        End Class
    End Class
End Namespace
