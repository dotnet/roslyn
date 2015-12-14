' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Editor.Completion.FileSystem
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
    <ExportCompletionProvider("LoadDirectiveCompletionProvider", LanguageNames.VisualBasic)>
    <TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)>
    Friend Class VisualBasicLoadDirectiveCompletionProvider
        Inherits LoadDirectiveCompletionProvider

        Private Shared s_directiveRegex As Regex = New Regex("#load\s+(""[^""]*""?)", RegexOptions.Compiled Or RegexOptions.IgnoreCase)

        Protected Overrides Function GetDirectiveMatch(lineText As String) As Match
            Return s_directiveRegex.Match(lineText)
        End Function

        Protected Overrides Function AllowableExtensions() As String()
            Return {".vbx"}
        End Function
    End Class
End Namespace