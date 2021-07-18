' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(AwaitCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After = NameOf(KeywordCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class AwaitCompletionProvider
        Inherits AbstractAwaitCompletionProvider

    End Class
End Namespace
