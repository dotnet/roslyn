' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    Friend Class VisualBasicCompletionOptions
        Public Const FeatureName As String = "VisualBasic Completion"

        Public Shared AddNewLineOnEnterAfterFullyTypedWord As Options.Option(Of Integer) =
            New Options.Option(Of Integer)(FeatureName, NameOf(AddNewLineOnEnterAfterFullyTypedWord), defaultValue:=EnterKeyRule.Always)
    End Class
End Namespace
