' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Options.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion

    <ExportOptionProvider, System.Composition.Shared>
    Friend Class VisualBasicCompletionOptionsProvider
        Implements IOptionProvider

        Dim _options As IEnumerable(Of IOption) = ImmutableArray.Create(DirectCast(VisualBasicCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord, IOption))

        Public Function GetOptions() As IEnumerable(Of IOption) Implements IOptionProvider.GetOptions
            Return _options
        End Function
    End Class
End Namespace
