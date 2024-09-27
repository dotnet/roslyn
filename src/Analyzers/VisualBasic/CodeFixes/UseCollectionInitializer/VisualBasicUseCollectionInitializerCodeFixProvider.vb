' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCollectionInitializer), [Shared]>
    Friend Class VisualBasicUseCollectionInitializerCodeFixProvider
        Inherits AbstractUseCollectionInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            ObjectCollectionInitializerSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetAnalyzer() As VisualBasicCollectionInitializerAnalyzer
            Return VisualBasicCollectionInitializerAnalyzer.Allocate()
        End Function

        Protected Overrides Function GetReplacementNodesAsync(
                document As Document,
                objectCreation As ObjectCreationExpressionSyntax,
                useCollectionExpression As Boolean,
                matches As ImmutableArray(Of Match),
                cancellationToken As CancellationToken) As Task(Of (SyntaxNode, SyntaxNode))
            Contract.ThrowIfTrue(useCollectionExpression, "VB does not support collection expressions")

            Dim statement = objectCreation.FirstAncestorOrSelf(Of StatementSyntax)
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In matches
                For Each trivia In match.StatementOrExpression.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Dim result = newStatement.WithLeadingTrivia(totalTrivia).WithAdditionalAnnotations(Formatter.Annotation)
            Return Task.FromResult((DirectCast(statement, SyntaxNode), DirectCast(result, SyntaxNode)))
        End Function

        Private Shared Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of Match)) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectCollectionInitializer(
                    CreateCollectionInitializer(objectCreation, matches)))
        End Function

        Private Shared Function CreateCollectionInitializer(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of Match)) As CollectionInitializerSyntax
            Dim nodesAndTokens = ArrayBuilder(Of SyntaxNodeOrToken).GetInstance()

            AddExistingItems(objectCreation, nodesAndTokens)

            For i = 0 To matches.Length - 1
                Dim expressionStatement = DirectCast(matches(i).StatementOrExpression, ExpressionStatementSyntax)

                Dim newExpression As ExpressionSyntax
                Dim invocationExpression = DirectCast(expressionStatement.Expression, InvocationExpressionSyntax)
                Dim arguments = invocationExpression.ArgumentList.Arguments
                If arguments.Count = 1 Then
                    newExpression = arguments(0).GetExpression()
                Else
                    newExpression = SyntaxFactory.CollectionInitializer(
                        SyntaxFactory.SeparatedList(
                            arguments.Select(Function(a) a.GetExpression()),
                            arguments.GetSeparators()))
                End If

                newExpression = newExpression.WithLeadingTrivia(SyntaxFactory.ElasticMarker)

                If i < matches.Length - 1 Then
                    nodesAndTokens.Add(newExpression)
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                Else
                    newExpression = newExpression.WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                    nodesAndTokens.Add(newExpression)
                End If
            Next

            Dim result = SyntaxFactory.CollectionInitializer(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                SyntaxFactory.SeparatedList(Of ExpressionSyntax)(nodesAndTokens),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
            nodesAndTokens.Free()
            Return result
        End Function
    End Class
End Namespace
