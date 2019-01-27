' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Formatting.Rules

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping
    Friend Class WrappingIndentationService
        Inherits VisualBasicIndentationService

        Public Shared ReadOnly Instance As New WrappingIndentationService()

        Private Sub New()
        End Sub

        Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
            ' Override default indentation behavior.  The special indentation rule tries to 
            ' align parameters.  But that's what we're actually trying to control, so we need
            ' to remove this.
            Return New NoOpFormattingRule()
        End Function
    End Class
End Namespace
