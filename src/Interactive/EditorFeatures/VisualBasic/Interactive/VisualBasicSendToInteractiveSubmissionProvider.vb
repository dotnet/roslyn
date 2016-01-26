' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive

    <Export(GetType(ISendToInteractiveSubmissionProvider))>
    Friend NotInheritable Class VisualBasicSendToInteractiveSubmissionProvider
        Inherits SendToInteractiveSubmissionProvider

        Protected Overrides Function CanParseSubmission(code As String) As Boolean
            ' Return True to send the direct selection.
            Return True
        End Function

        Protected Overrides Function GetExecutableSyntaxTreeNodeSelection(position As TextSpan, source As SourceText, node As SyntaxNode, model As SemanticModel) As IEnumerable(Of TextSpan)
            Return Nothing
        End Function
    End Class
End Namespace
