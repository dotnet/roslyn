' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.Classification
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Classification

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Classification
    <ExportLanguageService(GetType(IEditorClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorClassificationService
        Inherits AbstractEditorClassificationService

        Public Overrides Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function
    End Class
End Namespace
