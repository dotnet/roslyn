' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    Partial Friend Class InvocationExpressionSignatureHelpProvider

        Private Shared Function GetAccessibleMembers(invocationExpression As InvocationExpressionSyntax,
                                             semanticModel As SemanticModel,
                                             within As ISymbol,
                                             memberGroup As IEnumerable(Of ISymbol),
                                             cancellationToken As CancellationToken) As ImmutableArray(Of ISymbol)
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

            Return memberGroup.Where(Function(m) m.IsAccessibleWithin(within, throughType)).ToImmutableArray()
        End Function

        Private Shared Function GetMemberGroupItems(accessibleMembers As ImmutableArray(Of ISymbol),
                                             document As Document,
                                             invocationExpression As InvocationExpressionSyntax,
                                             semanticModel As SemanticModel) As IEnumerable(Of SignatureHelpItem)
            If accessibleMembers.Length = 0 Then
                Return SpecializedCollections.EmptyEnumerable(Of SignatureHelpItem)()
            End If

            Return accessibleMembers.Select(
                Function(s) ConvertMemberGroupMember(document, s, invocationExpression.SpanStart, semanticModel))
        End Function
    End Class
End Namespace
