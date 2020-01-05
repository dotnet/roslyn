' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageService(GetType(IClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorClassificationService
        Inherits AbstractClassificationService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim temp = ArrayBuilder(Of ClassifiedSpan).GetInstance()
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, temp, cancellationToken)
            AddRange(temp, result)
            temp.Free()
        End Sub

        Public Overrides Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function
    End Class
End Namespace
