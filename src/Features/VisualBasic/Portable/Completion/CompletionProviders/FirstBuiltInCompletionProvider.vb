' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(FirstBuiltInCompletionProvider), LanguageNames.VisualBasic)>
    <[Shared]>
    Friend Class FirstBuiltInCompletionProvider
        Inherits CompletionProvider

        Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Return Task.CompletedTask
        End Function
    End Class
End Namespace
