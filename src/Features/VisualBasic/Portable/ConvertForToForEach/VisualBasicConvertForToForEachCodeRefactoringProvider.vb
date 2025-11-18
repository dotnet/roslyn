' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertForToForEach
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertForToForEach
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertForToForEach), [Shared]>
    Friend Class VisualBasicConvertForToForEachCodeRefactoringProvider
        Inherits AbstractConvertForToForEachCodeRefactoringProvider(Of
            StatementSyntax,
            ForBlockSyntax,
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            TypeSyntax,
            VariableDeclaratorSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Convert_to_For_Each
        End Function

        Protected Overrides Function GetBodyStatements(forStatement As ForBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return forStatement.Statements
        End Function

        Protected Overrides Function IsValidVariableDeclarator(firstVariable As VariableDeclaratorSyntax) As Boolean
            ' we only support local declarations in vb that have a single identifier.  i.e.
            '    dim x = list(i)
            Return firstVariable.Names.Count = 1
        End Function

        Protected Overrides Function TryGetForStatementComponents(
                forBlock As ForBlockSyntax,
                ByRef iterationVariable As SyntaxToken,
                ByRef initializer As ExpressionSyntax,
                ByRef memberAccess As MemberAccessExpressionSyntax,
                ByRef stepValueExpressionOpt As ExpressionSyntax,
                cancellationToken As CancellationToken) As Boolean

            Dim forStatement As ForStatementSyntax = forBlock.ForStatement
            Dim identifierName = TryCast(forStatement.ControlVariable, IdentifierNameSyntax)

            If identifierName IsNot Nothing Then
                iterationVariable = identifierName.Identifier

                If forStatement.FromValue IsNot Nothing Then
                    initializer = forStatement.FromValue

                    Dim subtraction = TryCast(forStatement.ToValue, BinaryExpressionSyntax)
                    If subtraction?.Kind() = SyntaxKind.SubtractExpression Then
                        Dim subtractionRight = TryCast(subtraction.Right, LiteralExpressionSyntax)
                        If TypeOf subtractionRight?.Token.Value Is Integer AndAlso
                           DirectCast(subtractionRight.Token.Value, Integer) = 1 Then

                            memberAccess = TryCast(subtraction.Left, MemberAccessExpressionSyntax)
                            If memberAccess IsNot Nothing Then
                                stepValueExpressionOpt = forStatement.StepClause?.StepValue
                                Return True
                            End If
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Protected Overrides Function ConvertForNode(
                currentFor As ForBlockSyntax, typeNode As TypeSyntax,
                foreachIdentifier As SyntaxToken, collectionExpression As ExpressionSyntax, iterationVariableType As ITypeSymbol) As SyntaxNode

            Dim forStatement = currentFor.ForStatement
            Return SyntaxFactory.ForEachBlock(
                SyntaxFactory.ForEachStatement(
                    forStatement.ForKeyword,
                    SyntaxFactory.Token(SyntaxKind.EachKeyword),
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(foreachIdentifier)),
                        If(typeNode IsNot Nothing, SyntaxFactory.SimpleAsClause(typeNode), Nothing),
                        Nothing),
                    SyntaxFactory.Token(SyntaxKind.InKeyword),
                    collectionExpression).WithTriviaFrom(forStatement),
                currentFor.Statements,
                currentFor.NextStatement)
        End Function
    End Class
End Namespace
