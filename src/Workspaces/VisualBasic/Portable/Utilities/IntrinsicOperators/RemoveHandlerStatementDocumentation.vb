' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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
