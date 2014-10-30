' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    ' <summary>
    ' CA2213: Disposable fields should be disposed
    ' </summary>
    <ExportCodeFixProvider(CA2213DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic), [Shared]>
    Public Class CA2213BasicCodeFixProvider
        Inherits CA2213CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            ' We are going to add a call Dispose on fields:
            '
            '      Public Sub Dispose() Implements IDisposable.Dispose
            '             a.Dispose()
            '      End Sub

            Dim syntaxNode = TryCast(nodeToFix, ModifiedIdentifierSyntax)
            If syntaxNode Is Nothing Then
                Return Task.FromResult(document)
            End If

            ' Find a Dispose method. Note that in VB a different method name can be used to implement an interface.
            Dim classBlock = syntaxNode.FirstAncestorOrSelf(Of ClassBlockSyntax)()
            Dim disposeMethod = model.Compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers(CA2213DiagnosticAnalyzer.Dispose).Single()
            Dim fieldSymbol = TryCast(model.GetDeclaredSymbol(nodeToFix), IFieldSymbol)
            Dim disposeSymbolOfField = fieldSymbol.Type.FindImplementationForInterfaceMember(disposeMethod)
            Dim typeSymbol = TryCast(model.GetDeclaredSymbol(classBlock), ITypeSymbol)
            Dim disposeSymbolOftypeSymbol = typeSymbol.FindImplementationForInterfaceMember(disposeMethod)

            Dim member = classBlock.DescendantNodes().OfType(Of MethodStatementSyntax)().Where(Function(n) n.Identifier.ValueText = disposeSymbolOftypeSymbol.MetadataName).FirstOrDefault()
            If member Is Nothing Then
                Return Task.FromResult(document)
            End If

            Dim factory = document.GetLanguageService(Of SyntaxGenerator)()

            ' Handle a case where a local in the Dipose method with the same name by generating this (or ClassName) and simplifying it
            Dim path = If(fieldSymbol.IsStatic, factory.IdentifierName(typeSymbol.MetadataName), factory.ThisExpression())

            Dim statement =
                factory.ExpressionStatement(
                    factory.InvocationExpression(
                        factory.MemberAccessExpression(
                            factory.MemberAccessExpression(path, factory.IdentifierName(fieldSymbol.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation),
                                factory.IdentifierName(disposeSymbolOfField.MetadataName))))

            Dim parent = DirectCast(member.Parent, MethodBlockSyntax)
            Dim newMember = parent.AddStatements(DirectCast(statement, StatementSyntax)).WithAdditionalAnnotations(Formatter.Annotation)
            Return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(parent, newMember)))
        End Function
    End Class
End Namespace