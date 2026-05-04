' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseObjectInitializer), [Shared]>
    Friend NotInheritable Class VisualBasicUseObjectInitializerCodeFixProvider
        Inherits AbstractUseObjectInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicUseNamedMemberInitializerAnalyzer)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetAnalyzer() As VisualBasicUseNamedMemberInitializerAnalyzer
            Return VisualBasicUseNamedMemberInitializerAnalyzer.Allocate()
        End Function

        Protected Overrides ReadOnly Property SyntaxFormatting As ISyntaxFormatting = VisualBasicSyntaxFormatting.Instance

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds = VisualBasicSyntaxKinds.Instance

        Protected Overrides Function Whitespace(text As String) As SyntaxTrivia
            Return SyntaxFactory.Whitespace(text)
        End Function

        Protected Overrides Function GetNewStatement(
                statement As StatementSyntax,
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As StatementSyntax
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, options, matches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In matches
                For Each trivia In match.Statement.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Return newStatement.WithLeadingTrivia(totalTrivia)
        End Function

        Private Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectMemberInitializer(
                    CreateFieldInitializers(objectCreation, options, matches)))
        End Function

        Private Function CreateFieldInitializers(
                objectCreation As ObjectCreationExpressionSyntax,
                options As SyntaxFormattingOptions,
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As SeparatedSyntaxList(Of FieldInitializerSyntax)
            Dim nodesAndTokens = ArrayBuilder(Of SyntaxNodeOrToken).GetInstance()

            AddExistingItems(objectCreation, nodesAndTokens)

            For i = 0 To matches.Length - 1
                Dim match = matches(i)

                Dim rightValue = Indent(match.Initializer, options)
                If i < matches.Length - 1 Then
                    rightValue = rightValue.WithoutTrailingTrivia()
                End If

                Dim initializer = SyntaxFactory.NamedFieldInitializer(
                    keyKeyword:=Nothing,
                    dotToken:=match.MemberAccessExpression.OperatorToken,
                    name:=SyntaxFactory.IdentifierName(match.MemberName),
                    equalsToken:=match.Statement.OperatorToken,
                    expression:=rightValue).WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)

                nodesAndTokens.Add(initializer)
                If i < matches.Length - 1 Then
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(match.Initializer.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                End If
            Next

            Dim result = SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(nodesAndTokens)
            nodesAndTokens.Free()
            Return result
        End Function
    End Class
End Namespace
