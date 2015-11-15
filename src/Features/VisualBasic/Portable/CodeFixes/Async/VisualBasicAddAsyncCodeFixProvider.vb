' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Async
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Resources = Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddAsync), [Shared]>
    Friend Class VisualBasicAddAsyncCodeFixProvider
        Inherits AbstractAddAsyncCodeFixProvider

        Friend Const BC36937 As String = "BC36937" ' error BC36937: 'Await' can only be used when contained within a method or lambda expression marked with the 'Async' modifier.
        Friend Const BC37057 As String = "BC37057" ' error BC37057: 'Await' can only be used within an Async method. Consider marking this method with the 'Async' modifier and changing its return type to 'Task'.
        Friend Const BC37058 As String = "BC37058" ' error BC37058: 'Await' can only be used within an Async method. Consider marking this method with the 'Async' modifier and changing its return type to 'Task'.
        Friend Const BC37059 As String = "BC37059" ' error BC37059: 'Await' can only be used within an Async lambda expression. Consider marking this expression with the 'Async' modifier and changing its return type to 'Task'.

        Friend Shared ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC36937, BC37057, BC37058, BC37059)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Function GetDescription(diagnostic As Diagnostic, node As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As String
            Return Resources.MakeAsync
        End Function

        Protected Overrides Async Function GetNewRoot(root As SyntaxNode, oldNode As SyntaxNode, semanticModel As SemanticModel, diagnostic As Diagnostic, document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim methodNode = GetContainingMember(oldNode)
            If methodNode Is Nothing Then
                Return Nothing
            End If
            Return root.ReplaceNode(methodNode, Await ConvertToAsync(methodNode, semanticModel, document, cancellationToken).ConfigureAwait(False))
        End Function

        Private Shared Function GetContainingMember(oldNode As SyntaxNode) As StatementSyntax

            Dim lambda = oldNode.GetAncestor(Of LambdaExpressionSyntax)
            If lambda IsNot Nothing Then
                Return TryCast(lambda.ChildNodes().FirstOrDefault(Function(a) _
                        a.IsKind(SyntaxKind.FunctionLambdaHeader) Or
                        a.IsKind(SyntaxKind.SubLambdaHeader)),
                    StatementSyntax)
            End If

            Dim ancestor As MethodBlockBaseSyntax = oldNode.GetAncestor(Of MethodBlockBaseSyntax)
            If ancestor IsNot Nothing Then
                Return TryCast(ancestor.ChildNodes().FirstOrDefault(Function(a) _
                        a.IsKind(SyntaxKind.SubStatement) Or
                        a.IsKind(SyntaxKind.FunctionStatement)),
                    StatementSyntax)
            End If

            Return Nothing
        End Function

        Private Async Function ConvertToAsync(node As StatementSyntax, semanticModel As SemanticModel, document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim methodNode = TryCast(node, MethodStatementSyntax)
            If methodNode IsNot Nothing Then
                Return Await ConvertMethodToAsync(document, semanticModel, methodNode, cancellationToken).ConfigureAwait(False)
            End If

            Dim lambdaNode = TryCast(node, LambdaHeaderSyntax)
            If lambdaNode IsNot Nothing Then
                Return lambdaNode.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)).WithAdditionalAnnotations(Formatter.Annotation)
            End If

            Return Nothing
        End Function

        Protected Overrides Function AddAsyncKeyword(node As SyntaxNode) As SyntaxNode
            Dim methodNode = TryCast(node, MethodStatementSyntax)
            If methodNode Is Nothing Then
                Return Nothing
            End If
            ' Visual Basic includes newlines in the trivia for MethodStatementSyntax nodes.  If we are
            ' inserting into the beginning of method statement, we will need to move the trivia.
            Dim token = methodNode.ChildTokens().FirstOrDefault()
            Dim keyword = methodNode.DeclarationKeyword
            If keyword = token Then
                Dim trivia = token.LeadingTrivia
                methodNode = methodNode.ReplaceToken(token, token.WithLeadingTrivia())
                Return methodNode.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithLeadingTrivia(trivia)).WithAdditionalAnnotations(Formatter.Annotation)
            End If
            Return methodNode.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)).WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Protected Overrides Function AddAsyncKeywordAndTaskReturnType(node As SyntaxNode, existingReturnType As ITypeSymbol, taskTypeSymbol As INamedTypeSymbol) As SyntaxNode
            Dim methodNode = TryCast(node, MethodStatementSyntax)
            If methodNode Is Nothing Then
                Return Nothing
            End If
            If taskTypeSymbol Is Nothing Then
                Return Nothing
            End If

            Dim returnType = taskTypeSymbol.Construct(existingReturnType).GenerateTypeSyntax()
            Return AddAsyncKeyword(methodNode.WithAsClause(methodNode.AsClause.WithType(returnType)))
        End Function

        Protected Overrides Function DoesConversionExist(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(source, destination).Exists
        End Function
    End Class
End Namespace
