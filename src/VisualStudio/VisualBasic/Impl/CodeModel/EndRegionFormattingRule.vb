' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend NotInheritable Class EndRegionFormattingRule
        Inherits AbstractFormattingRule

        Public Shared ReadOnly Instance As New EndRegionFormattingRule()

        Private Sub New()
        End Sub
    End Class
End Namespace
