' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    ' <summary>
    ' CA2231: Overload Operator equals on overriding ValueType.Equals
    ' </summary>
    <ExportCodeFixProvider(CA2231DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic), [Shared]>
    Public Class CA2231BasicCodeFixProvider
        Inherits CA2231CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnosticId As String, cancellationToken As CancellationToken) As Task(Of Document)
            ' We are going to add two operators:
            '
            '       Public Shared Operator =(left As A, right As A) As Boolean
            '             Throw New NotImplementedException()
            '      End Operator
            '
            '       Public Shared Operator <>(left As A, right As A) As Boolean
            '             Throw New NotImplementedException()
            '       End Operator

            Dim syntaxNode = TryCast(nodeToFix, StructureStatementSyntax)
            If syntaxNode Is Nothing Then
                Return Task.FromResult(document)

            End If

            Dim statement = CreateThrowNotImplementedStatement(model)
            If statement Is Nothing Then
                Return Task.FromResult(document)
            End If

            Dim params = SyntaxFactory.ParameterList(
                            SyntaxFactory.SeparatedList(Of ParameterSyntax)(New ParameterSyntax() {
                                    SyntaxFactory.Parameter(identifier:=SyntaxFactory.ModifiedIdentifier(LeftName)).WithAsClause(SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName(syntaxNode.Identifier.ValueText))),
                                    SyntaxFactory.Parameter(identifier:=SyntaxFactory.ModifiedIdentifier(RightName)).WithAsClause(SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName(syntaxNode.Identifier.ValueText)))
                                }))

            Dim equalsOperator = CreateOperatorDeclaration(SyntaxKind.EqualsToken, params, statement)
            Dim inequalsOperator = CreateOperatorDeclaration(SyntaxKind.LessThanGreaterThanToken, params, statement)
            Dim parent = DirectCast(syntaxNode.Parent, StructureBlockSyntax)
            Dim newNode = parent.AddMembers(New OperatorBlockSyntax() {equalsOperator, inequalsOperator}).WithAdditionalAnnotations(Formatter.Annotation)
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

        Protected Function CreateOperatorDeclaration(kind As SyntaxKind, params As ParameterListSyntax, statement As StatementSyntax) As OperatorBlockSyntax
            Return SyntaxFactory.OperatorBlock(
                            SyntaxFactory.OperatorStatement(Nothing,
                                SyntaxFactory.TokenList(New SyntaxToken() {SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SharedKeyword)}),
                                SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                                SyntaxFactory.Token(kind),
                                params,
                                SyntaxFactory.SimpleAsClause(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BooleanKeyword)))),
                            SyntaxFactory.SingletonList(statement))
        End Function
    End Class
End Namespace