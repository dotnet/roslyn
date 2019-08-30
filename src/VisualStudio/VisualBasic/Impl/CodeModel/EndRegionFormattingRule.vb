' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend NotInheritable Class EndRegionFormattingRule
        Inherits AbstractFormattingRule

        Public Shared ReadOnly Instance As New EndRegionFormattingRule()

        Private Sub New()
        End Sub
    End Class
End Namespace
