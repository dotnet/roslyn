' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Function GetMemberGroupItems(document As Document,
                                             invocationExpression As InvocationExpressionSyntax,
                                             semanticModel As SemanticModel,
                                             within As ISymbol,
                                             memberGroup As IEnumerable(Of ISymbol),
                                             cancellationToken As CancellationToken) As IEnumerable(Of SignatureHelpItem)
            Dim throughType As ITypeSymbol = Nothing
            Dim expression = TryCast(invocationExpression.Expression, MemberAccessExpressionSyntax).GetExpressionOfMemberAccessExpression()

            ' if it is via a base expression "MyBase.", we know the "throughType" is the base class but
            ' we need to be able to tell between "New Base().M()" and "MyBase.M()".
            ' currently, Access check methods do not differentiate between them.
            ' so handle "MyBase." primary-expression here by nulling out "throughType"
            If expression IsNot Nothing AndAlso TypeOf expression IsNot MyBaseExpressionSyntax Then
                throughType = semanticModel.GetTypeInfo(expression, cancellationToken).Type
            End If

            If TypeOf invocationExpression.Expression Is SimpleNameSyntax AndAlso
               invocationExpression.IsInStaticContext() Then
                memberGroup = memberGroup.Where(Function(m) m.IsStatic)
            End If

            Dim accessibleMembers = memberGroup.Where(Function(m) m.IsAccessibleWithin(within, throughType:=throughType)).ToList()
            If accessibleMembers.Count = 0 Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Return accessibleMembers.Select(
                Function(s) ConvertMemberGroupMember(document, s, invocationExpression.SpanStart, semanticModel, cancellationToken))
        End Function
    End Class
End Namespace
