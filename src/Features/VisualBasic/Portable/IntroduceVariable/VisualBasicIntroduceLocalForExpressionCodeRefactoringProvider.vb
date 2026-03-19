' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression), [Shared]>
    Friend Class VisualBasicIntroduceLocalForExpressionCodeRefactoringProvider
        Inherits AbstractIntroduceLocalForExpressionCodeRefactoringProvider(Of
            ExpressionSyntax,
            StatementSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsValid(expressionStatement As ExpressionStatementSyntax, span As TextSpan) As Boolean
            ' Expression is likely too simple to want to offer to generate a local for.
            ' This leads to too many false cases where this is offered.
            If span.IsEmpty AndAlso expressionStatement.Expression.IsKind(SyntaxKind.IdentifierName) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function FixupLocalDeclaration(expressionStatement As ExpressionStatementSyntax, localDeclaration As LocalDeclarationStatementSyntax) As LocalDeclarationStatementSyntax
            ' Don't want an 'as clause' in our local decl. it's idiomatic already to
            ' just do `dim x = new DateTime()`
            Return localDeclaration.RemoveNode(
                localDeclaration.Declarators(0).AsClause,
                SyntaxRemoveOptions.KeepUnbalancedDirectives Or SyntaxRemoveOptions.AddElasticMarker)
        End Function

        Protected Overrides Function FixupDeconstruction(expressionStatement As ExpressionStatementSyntax, localDeclaration As ExpressionStatementSyntax) As ExpressionStatementSyntax
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function CreateTupleDeconstructionAsync(document As Document, tupleType As INamedTypeSymbol, expression As ExpressionSyntax, cancellationToken As CancellationToken) As Task(Of ExpressionStatementSyntax)
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
