' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Globalization
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=CA1309DiagnosticAnalyzer.RuleId), [Shared]>
    Public Class CA1309BasicCodeFixProvider
        Inherits CA1309CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            ' if nothing can be fixed, return the unchanged node
            Dim newRoot = root
            Dim kind = nodeToFix.Kind()
            Dim syntaxFactoryService = document.Project.LanguageServices.GetService(Of SyntaxGenerator)
            Select Case kind
                Case SyntaxKind.SimpleArgument
                    If Not CType(nodeToFix, SimpleArgumentSyntax).IsNamed Then
                        ' StringComparison.CurrentCulture => StringComparison.Ordinal
                        ' StringComparison.CurrentCultureIgnoreCase => StringComparison.OrdinalIgnoreCase
                        Dim argument = CType(nodeToFix, SimpleArgumentSyntax)
                        Dim memberAccess = TryCast(argument.Expression, MemberAccessExpressionSyntax)
                        If memberAccess IsNot Nothing Then
                            ' preserve the "IgnoreCase" suffix if present
                            Dim isIgnoreCase = memberAccess.Name.GetText().ToString().EndsWith(CA1309DiagnosticAnalyzer.IgnoreCaseText, StringComparison.Ordinal)
                            Dim newOrdinalText = If(isIgnoreCase, CA1309DiagnosticAnalyzer.OrdinalIgnoreCaseText, CA1309DiagnosticAnalyzer.OrdinalText)
                            Dim newIdentifier = syntaxFactoryService.IdentifierName(newOrdinalText)
                            Dim newMemberAccess = memberAccess.WithName(CType(newIdentifier, SimpleNameSyntax)).WithAdditionalAnnotations(Formatter.Annotation)
                            newRoot = root.ReplaceNode(memberAccess, newMemberAccess)
                        End If
                    End If
                Case SyntaxKind.IdentifierName
                    ' String.Equals(a, b) => String.Equals(a, b, StringComparison.Ordinal)
                    ' String.Compare(a, b) => String.Compare(a, b, StringComparison.Ordinal)
                    Dim identifier = CType(nodeToFix, IdentifierNameSyntax)
                    Dim invokeParent = identifier.Parent?.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()
                    If invokeParent IsNot Nothing Then
                        Dim methodSymbol = TryCast(model.GetSymbolInfo(identifier).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso CanAddStringComparison(methodSymbol) Then
                            ' append a New StringComparison.Ordinal argument
                            Dim newArg = syntaxFactoryService.Argument(CreateOrdinalMemberAccess(syntaxFactoryService, model)).
                                WithAdditionalAnnotations(Formatter.Annotation)
                            Dim newInvoke = invokeParent.AddArgumentListArguments(CType(newArg, ArgumentSyntax)).WithAdditionalAnnotations(Formatter.Annotation)
                            newRoot = root.ReplaceNode(invokeParent, newInvoke)
                        End If
                    End If
                Case SyntaxKind.EqualsExpression
                    ' "a = b" => "String.Equals(a, b, StringComparison.Ordinal)"
                    Dim fixedExpr = FixBinaryExpression(syntaxFactoryService, model, CType(nodeToFix, BinaryExpressionSyntax), True).WithAdditionalAnnotations(Formatter.Annotation)
                    newRoot = root.ReplaceNode(nodeToFix, fixedExpr)
                Case SyntaxKind.NotEqualsExpression
                    ' "a <> b" => "!String.Equals(a, b, StringComparison.Ordinal)"
                    Dim fixedExpr = FixBinaryExpression(syntaxFactoryService, model, CType(nodeToFix, BinaryExpressionSyntax), False).WithAdditionalAnnotations(Formatter.Annotation)
                    newRoot = root.ReplaceNode(nodeToFix, fixedExpr)
            End Select

            If newRoot.Equals(root) Then
                Return Task.FromResult(document)
            End If
            Return Task.FromResult(document.WithSyntaxRoot(newRoot))
        End Function

        Private Function FixBinaryExpression(syntaxFactoryService As SyntaxGenerator, model As SemanticModel, node As BinaryExpressionSyntax, isEquals As Boolean) As SyntaxNode
            Dim invocation = CreateEqualsExpression(syntaxFactoryService, model, node.Left, node.Right, isEquals)
            Return invocation
        End Function

    End Class
End Namespace
