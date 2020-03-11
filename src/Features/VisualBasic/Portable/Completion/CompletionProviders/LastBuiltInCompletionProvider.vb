' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    ''' <summary>
    ''' Provides a completion provider that always appears after all built-in completion providers. This completion
    ''' provider does not provide any completions.
    ''' </summary>
    <ExportCompletionProvider(NameOf(LastBuiltInCompletionProvider), LanguageNames.VisualBasic)>
    <[Shared]>
    Friend Class LastBuiltInCompletionProvider
        Inherits CompletionProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Return Task.CompletedTask
        End Function
    End Class
End Namespace
