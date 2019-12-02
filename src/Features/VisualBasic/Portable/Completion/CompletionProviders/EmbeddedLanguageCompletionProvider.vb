' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(EmbeddedLanguageCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(InternalsVisibleToCompletionProvider))>
    <[Shared]>
    Friend Class EmbeddedLanguageCompletionProvider
        Inherits AbstractEmbeddedLanguageCompletionProvider

    End Class
End Namespace
