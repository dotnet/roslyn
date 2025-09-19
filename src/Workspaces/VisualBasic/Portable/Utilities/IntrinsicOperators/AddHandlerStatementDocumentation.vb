' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class AddHandlerStatementDocumentation
        Inherits AbstractAddRemoveHandlerStatementDocumentation

        Public Overrides ReadOnly Property DocumentationText As String =
            VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_event_to_associate_an_event_handler_delegate_or_lambda_expression_with
                Case 1
                    Return VBWorkspaceResources.The_event_handler_to_associate_with_the_event_This_may_take_the_form_of_AddressOf_eventHandler_delegate_lambdaExpression
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property PrefixParts As ImmutableArray(Of SymbolDisplayPart) = ImmutableArray.Create(
            New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "AddHandler"),
            New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
    End Class
End Namespace

