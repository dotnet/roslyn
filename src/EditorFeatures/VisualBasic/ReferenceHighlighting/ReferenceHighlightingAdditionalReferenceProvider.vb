' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ReferenceHighlighting
    <ExportLanguageService(GetType(IReferenceHighlightingAdditionalReferenceProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class ReferenceHighlightingAdditionalReferenceProvider
        Implements IReferenceHighlightingAdditionalReferenceProvider

        Public Function GetAdditionalReferencesAsync(document As Document, symbol As ISymbol, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of Location)) Implements IReferenceHighlightingAdditionalReferenceProvider.GetAdditionalReferencesAsync
            Return SpecializedTasks.EmptyEnumerable(Of Location)()
        End Function
    End Class
End Namespace
