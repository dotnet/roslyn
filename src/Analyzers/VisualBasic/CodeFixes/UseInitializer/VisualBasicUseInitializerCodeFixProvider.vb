' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.UseInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInitializer
    ''' <summary>
    ''' Pass 3 of the IDE0017+IDE0028 unification: a single VB fix provider class registered
    ''' for IDE0017 (object-initializer), IDE0028 (collection-initializer), and IDE0400 (mixed
    ''' object/collection initializer; never actually reported in VB but supported by the
    ''' shared abstract base for symmetry), backed by a single walk
    ''' (<c>VisualBasicCollectionInitializerAnalyzer</c>). Replaces the prior
    ''' <c>VisualBasicUseObjectInitializerCodeFixProvider</c> and
    ''' <c>VisualBasicUseCollectionInitializerCodeFixProvider</c> classes, and removes the
    ''' per-language member-initializer walk dependency.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCollectionInitializer), [Shared]>
    Friend NotInheritable Class VisualBasicUseInitializerCodeFixProvider
        Inherits AbstractUseInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)
        ' Note: above the `AssignmentStatementSyntax` slot serves both `TAssignmentStatementSyntax`
        ' on the abstract base AND the matching slot on the collection-init analyzer's generic
        ' instantiation — VB's separate AssignmentStatementSyntax type is the right node for both.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetAnalyzer() As VisualBasicCollectionInitializerAnalyzer
            Return VisualBasicCollectionInitializerAnalyzer.Allocate()
        End Function

        Protected Overrides ReadOnly Property SyntaxFormatting As ISyntaxFormatting = VisualBasicSyntaxFormatting.Instance

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds = VisualBasicSyntaxKinds.Instance

        Protected Overrides Function Whitespace(text As String) As SyntaxTrivia
            Return SyntaxFactory.Whitespace(text)
        End Function

        Protected Overrides Function GetNewStatementForMemberInit(
                statement As StatementSyntax,
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode))) As StatementSyntax
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreationForMemberInit(objectCreation, options, matches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In matches
                For Each trivia In match.Node.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Return newStatement.WithLeadingTrivia(totalTrivia)
        End Function

        Private Function GetNewObjectCreationForMemberInit(
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode))) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectMemberInitializer(
                    CreateFieldInitializers(objectCreation, options, matches)))
        End Function

        Private Function CreateFieldInitializers(
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode))) As SeparatedSyntaxList(Of FieldInitializerSyntax)
            Dim nodesAndTokens = ArrayBuilder(Of SyntaxNodeOrToken).GetInstance()

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens)

            For i = 0 To matches.Length - 1
                Dim match = matches(i)

                ' Pass 1 of the IDE0017+IDE0028 unification widened the match type to
                ' `InitializerMatch(Of StatementSyntax)` with a `Kind` discriminator. VB's
                ' analyzer only ever emits MemberInitializer (the Add-fold path is gated on
                ' `SupportsMixedObjectAndCollectionInitializers`, which always returns false in
                ' VB); fail loudly if a future walk extension surfaces another kind here so
                ' the VB fixer never silently corrupts the output.
                Contract.ThrowIfFalse(match.Kind = InitializerMatchKind.MemberInitializer)

                Dim assignment = DirectCast(match.Node, AssignmentStatementSyntax)
                Dim memberAccess = DirectCast(assignment.Left, MemberAccessExpressionSyntax)
                Dim memberName = memberAccess.Name.Identifier.ValueText

                Dim rightValue = Indent(assignment.Right, options)
                If i < matches.Length - 1 Then
                    rightValue = rightValue.WithoutTrailingTrivia()
                End If

                Dim initializer = SyntaxFactory.NamedFieldInitializer(
                    keyKeyword:=Nothing,
                    dotToken:=memberAccess.OperatorToken,
                    name:=SyntaxFactory.IdentifierName(memberName),
                    equalsToken:=assignment.OperatorToken,
                    expression:=rightValue).WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)

                nodesAndTokens.Add(initializer)
                If i < matches.Length - 1 Then
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(assignment.Right.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                End If
            Next

            Dim result = SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(nodesAndTokens)
            nodesAndTokens.Free()
            Return result
        End Function

        Protected Overrides Function GetReplacementNodesForCollectionInitAsync(
                document As Document,
                objectCreation As ObjectCreationExpressionSyntax,
                useCollectionExpression As Boolean,
                preMatches As ImmutableArray(Of InitializerMatch(Of SyntaxNode)),
                postMatches As ImmutableArray(Of InitializerMatch(Of SyntaxNode)),
                cancellationToken As CancellationToken) As Task(Of (SyntaxNode, SyntaxNode))
            Contract.ThrowIfFalse(preMatches.IsEmpty)
            Contract.ThrowIfTrue(useCollectionExpression, "VB does not support collection expressions")

            Dim statement = objectCreation.FirstAncestorOrSelf(Of StatementSyntax)
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreationForCollectionInit(objectCreation, postMatches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In postMatches
                For Each trivia In match.Node.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Dim result = newStatement.WithLeadingTrivia(totalTrivia).WithAdditionalAnnotations(Formatter.Annotation)
            Return Task.FromResult((DirectCast(statement, SyntaxNode), DirectCast(result, SyntaxNode)))
        End Function

        Private Shared Function GetNewObjectCreationForCollectionInit(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode))) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectCollectionInitializer(
                    CreateCollectionInitializer(objectCreation, matches)))
        End Function

        Private Shared Function CreateCollectionInitializer(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode))) As CollectionInitializerSyntax
            Dim nodesAndTokens = ArrayBuilder(Of SyntaxNodeOrToken).GetInstance()

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens)

            For i = 0 To matches.Length - 1
                Dim expressionStatement = DirectCast(matches(i).Node, ExpressionStatementSyntax)

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
