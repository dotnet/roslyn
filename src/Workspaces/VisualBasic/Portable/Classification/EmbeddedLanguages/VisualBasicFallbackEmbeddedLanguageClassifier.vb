' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Friend Class VisualBasicFallbackEmbeddedLanguageClassifier
        Inherits AbstractFallbackEmbeddedLanguageClassifier

        Public Shared ReadOnly Instance As New VisualBasicFallbackEmbeddedLanguageClassifier()

        Private Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Info)
        End Sub
    End Class
End Namespace
