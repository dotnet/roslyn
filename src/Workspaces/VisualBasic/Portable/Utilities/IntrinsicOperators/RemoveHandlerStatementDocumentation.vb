' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class RemoveHandlerStatementDocumentation
        Inherits AbstractAddRemoveHandlerStatementDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time
            End Get
        End Property

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_event_to_disassociate_an_event_handler_or_delegate_from
                Case 1
                    Return VBWorkspaceResources.The_event_handler_to_disassociate_from_the_event_This_may_take_the_form_of_AddressOf_eventHandler_delegate
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "RemoveHandler"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " ")}
            End Get
        End Property
    End Class
End Namespace
