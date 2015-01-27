' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Iterator
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ConvertToIterator), [Shared]>
    Friend Class VisualBasicConvertToIteratorCodeFixProvider
        Inherits AbstractIteratorCodeFixProvider

        Friend Const BC30451 As String = "BC30451" ' error BC30451 : 'Yield' is not declared.  It may be inaccessible due its protection level.

        Friend Shared ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC30451)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Async Function GetCodeFixAsync(root As SyntaxNode, node As SyntaxNode, document As Document, diagnostics As Diagnostic, cancellationToken As CancellationToken) As Task(Of CodeAction)

            ' Check node is identifier name.
            Dim identifier = TryCast(node, IdentifierNameSyntax)
            If identifier Is Nothing Then
                Return Nothing
            End If

            ' Check identifier text is 'Yield'
            If String.Compare(identifier.Identifier.Text, "Yield", StringComparison.OrdinalIgnoreCase) <> 0 Then
                Return Nothing
            End If

            ' Check that parent is invocation expression syntax
            If identifier.Parent.Kind <> SyntaxKind.InvocationExpression Then
                Return Nothing
            End If

            ' Check that containing type is method
            Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim symbol = model.GetEnclosingSymbol(node.Span.Start, cancellationToken)
            Dim method = TryCast(symbol, IMethodSymbol)
            If method Is Nothing Then
                Return Nothing
            End If

            ' Check that return type of containing method is convertible to IEnumerable
            Dim ienumerableSymbol As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")
            If ienumerableSymbol Is Nothing Then
                Return Nothing
            End If

            If method.ReturnType.GetArity() <> 1 Then
                Return Nothing
            End If

            ienumerableSymbol = ienumerableSymbol.Construct(method.ReturnType.GetTypeArguments().First())

            If Not method.ReturnType.Equals(ienumerableSymbol) Then
                Return Nothing
            End If

            ' Get the syntax node for the function
            Dim syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault
            If syntaxReference Is Nothing Then
                Return Nothing
            End If

            ' Get the method or lambda expression and add the iterator keyword
            Dim methodNode = Await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(False)
            Select Case methodNode.Kind
                Case SyntaxKind.FunctionStatement
                    Dim methodStatementNode = TryCast(methodNode, MethodStatementSyntax)
                    If methodStatementNode IsNot Nothing AndAlso Not methodStatementNode.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                        root = AddIteratorKeywordToMethod(root, methodStatementNode)
                        Return New MyCodeAction(
                                        String.Format(ConvertToIterator, methodStatementNode.Identifier),
                                        document.WithSyntaxRoot(root))
                    End If
                Case SyntaxKind.MultiLineFunctionLambdaExpression
                    Dim lambdaNode = TryCast(methodNode, LambdaExpressionSyntax)
                    If lambdaNode IsNot Nothing AndAlso Not lambdaNode.SubOrFunctionHeader.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                        root = AddIteratorKeywordToLambda(root, lambdaNode)
                        Return New MyCodeAction(
                                    String.Format(ConvertToIterator, lambdaNode.SubOrFunctionHeader.GetTypeDisplayName()),
                                    document.WithSyntaxRoot(root))
                    End If
                Case Else
            End Select

            Return Nothing
        End Function

        Private Shared Function AddIteratorKeywordToMethod(root As SyntaxNode, methodStatementNode As MethodStatementSyntax) As SyntaxNode
            ' Add iterator keyword
            Dim iteratorToken As SyntaxToken = Token(SyntaxKind.IteratorKeyword).WithAdditionalAnnotations(Formatter.Annotation)
            Dim newFunctionNode As MethodStatementSyntax = Nothing

            ' If the iterator keyword is going to be added to the front of the method declaration,
            ' we will need to move the trivia from the method onto the keyword.
            If methodStatementNode.Modifiers.IsEmpty() AndAlso methodStatementNode.HasLeadingTrivia Then
                Dim leadingTrivia = methodStatementNode.GetLeadingTrivia
                iteratorToken = iteratorToken.WithLeadingTrivia(leadingTrivia)
                newFunctionNode = methodStatementNode.WithLeadingTrivia(CType(Nothing, SyntaxTriviaList))
                newFunctionNode = newFunctionNode.WithModifiers(methodStatementNode.Modifiers.Add(iteratorToken))
            Else
                newFunctionNode = methodStatementNode.WithModifiers(methodStatementNode.Modifiers.Add(iteratorToken))
            End If

            Return root.ReplaceNode(methodStatementNode, newFunctionNode)
        End Function

        Private Shared Function AddIteratorKeywordToLambda(root As SyntaxNode, lambdaNode As LambdaExpressionSyntax) As SyntaxNode
            ' Add iterator keyword
            Dim iteratorToken As SyntaxToken = Token(SyntaxKind.IteratorKeyword).WithAdditionalAnnotations(Formatter.Annotation)

            Dim newHeader = lambdaNode.SubOrFunctionHeader.WithModifiers(lambdaNode.SubOrFunctionHeader.Modifiers.Add(iteratorToken))

            Return root.ReplaceNode(lambdaNode.SubOrFunctionHeader, newHeader)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, newDocument As Document)
                MyBase.New(title, Function(c) Task.FromResult(newDocument))
            End Sub
        End Class
    End Class
End Namespace

