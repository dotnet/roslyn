' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
