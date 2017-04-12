' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend MustInherit Class AbstractAddRemoveHandlerStatementDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.event_
                Case 1
                    Return VBWorkspaceResources.handler
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 2
            End Get
        End Property
    End Class
End Namespace
