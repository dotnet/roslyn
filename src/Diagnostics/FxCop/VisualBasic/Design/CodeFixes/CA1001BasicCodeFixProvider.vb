' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ' <summary>
    ' CA1001: Types that own disposable fields should be disposable
    ' </summary>
    <ExportCodeFixProvider(CA1001DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic), [Shared]>
    Public Class CA1001BasicCodeFixProvider
        Inherits CA1001CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            ' We are going to implement IDisposable interface
            '
            '        Public Sub Dispose() Implements IDisposable.Dispose
            '            Throw New NotImplementedException()
            '        End Sub

            Dim syntaxNode = TryCast(nodeToFix, ClassStatementSyntax)
            If syntaxNode Is Nothing Then
                Return Task.FromResult(document)
            End If

            Dim statement = CreateThrowNotImplementedStatement(model)
            If statement Is Nothing Then
                Return Task.FromResult(document)
            End If

            Dim member = CreateSimpleSubBlockDeclaration(CA1001DiagnosticAnalyzer.Dispose, statement)
            Dim parent = DirectCast(syntaxNode.Parent, ClassBlockSyntax)
            Dim implementsStatement = SyntaxFactory.ImplementsStatement(
                SyntaxFactory.ParseTypeName(CA1001DiagnosticAnalyzer.IDisposable) _
                    .WithAdditionalAnnotations(Simplifier.Annotation)) _
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim newNode = parent.AddMembers(New MethodBlockSyntax() {member}).AddImplements(implementsStatement).WithAdditionalAnnotations(Formatter.Annotation)
            Return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(parent, newNode)))
        End Function

        Protected Function CreateThrowNotImplementedStatement(model As SemanticModel) As StatementSyntax
            Dim exceptionType = model.Compilation.GetTypeByMetadataName(NotImplementedExceptionName)
            If exceptionType Is Nothing Then
                ' If we can't find the exception, we can't generate anything.
                Return Nothing
            End If

            Return SyntaxFactory.ThrowStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        Nothing,
                        SyntaxFactory.ParseTypeName(exceptionType.Name),
                        SyntaxFactory.ArgumentList(),
                        Nothing))
        End Function

        Protected Function CreateSimpleSubBlockDeclaration(name As String, statement As StatementSyntax) As MethodBlockSyntax
            Return SyntaxFactory.SubBlock(
                        SyntaxFactory.SubStatement(
                            Nothing,
                            SyntaxFactory.TokenList(New SyntaxToken() {SyntaxFactory.Token(SyntaxKind.PublicKeyword)}),
                            SyntaxFactory.Token(SyntaxKind.SubKeyword),
                            SyntaxFactory.Identifier(name),
                            Nothing,
                            SyntaxFactory.ParameterList(),
                            Nothing,
                            Nothing,
                            SyntaxFactory.ImplementsClause(
                                SyntaxFactory.QualifiedName(
                                    SyntaxFactory.ParseName(CA1001DiagnosticAnalyzer.IDisposable),
                                    SyntaxFactory.IdentifierName(CA1001DiagnosticAnalyzer.Dispose)) _
                                    .WithAdditionalAnnotations(Simplifier.Annotation))),
                        SyntaxFactory.SingletonList(statement))
        End Function
    End Class
End Namespace