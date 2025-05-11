' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateEnumMember
    <ExportLanguageService(GetType(IGenerateEnumMemberService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend NotInheritable Class VisualBasicGenerateEnumMemberService
        Inherits AbstractGenerateEnumMemberService(Of VisualBasicGenerateEnumMemberService, SimpleNameSyntax, ExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsIdentifierNameGeneration(node As SyntaxNode) As Boolean
            Return TypeOf node Is IdentifierNameSyntax
        End Function

        Protected Overrides Function TryInitializeIdentifierNameState(
                document As SemanticDocument, identifierName As SimpleNameSyntax, cancellationToken As CancellationToken,
                ByRef identifierToken As SyntaxToken, ByRef simpleNameOrMemberAccessExpression As ExpressionSyntax) As Boolean
            identifierToken = identifierName.Identifier

            Dim memberAccess = TryCast(identifierName.Parent, MemberAccessExpressionSyntax)
            simpleNameOrMemberAccessExpression =
                If(memberAccess IsNot Nothing AndAlso memberAccess.Name Is identifierName,
                   DirectCast(memberAccess, ExpressionSyntax),
                   identifierName)

            If simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) Then
                Dim invocation = DirectCast(simpleNameOrMemberAccessExpression.Parent, InvocationExpressionSyntax)
                If invocation.ArgumentList IsNot Nothing Then
                    Return False
                End If
            End If

            If Not simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.ObjectCreationExpression) AndAlso
               Not simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.AddressOfExpression) Then
                Dim syntaxTree = DirectCast(document.SyntaxTree, VisualBasicSyntaxTree)
                Dim position = simpleNameOrMemberAccessExpression.SpanStart

                If syntaxTree.IsExpressionContext(position, cancellationToken) OrElse
                   syntaxTree.IsSingleLineStatementContext(position, cancellationToken) Then
                    Return True
                End If
            End If

            identifierToken = Nothing
            simpleNameOrMemberAccessExpression = Nothing
            Return False
        End Function
    End Class
End Namespace
