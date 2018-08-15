' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class VisualBasicEmbeddedLanguageCompletionProvider
        Inherits AbstractEmbeddedLanguageCompletionProvider

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Instance)
        End Sub
    End Class
End Namespace
