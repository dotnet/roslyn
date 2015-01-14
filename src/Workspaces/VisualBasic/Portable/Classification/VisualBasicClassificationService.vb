' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageService(GetType(IClassificationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicClassificationService
        Inherits AbstractClassificationService

        Public Overrides Function GetDefaultSyntaxClassifiers() As IEnumerable(Of ISyntaxClassifier)
            Return SyntaxClassifier.DefaultSyntaxClassifiers
        End Function

        Public Overrides Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Sub AddSyntacticClassifications(syntaxTree As SyntaxTree, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim root = syntaxTree.GetRoot(cancellationToken)
            Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Function FixClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function
    End Class
End Namespace
