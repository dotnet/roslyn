' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
