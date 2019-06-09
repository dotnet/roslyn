' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageService(GetType(ISyntaxClassificationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSyntaxClassificationService
        Inherits AbstractSyntaxClassificationService

        Private Shared ReadOnly s_defaultSyntaxClassifiers As ImmutableArray(Of ISyntaxClassifier) =
            ImmutableArray.Create(Of ISyntaxClassifier)(
                New NameSyntaxClassifier(),
                New ImportAliasClauseSyntaxClassifier(),
                New IdentifierNameSyntaxClassifier(),
                New EmbeddedLanguagesClassifier(),
                New OperatorOverloadSyntaxClassifier())

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function GetDefaultSyntaxClassifiers() As ImmutableArray(Of ISyntaxClassifier)
            Return s_defaultSyntaxClassifiers
        End Function

        Public Overrides Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Sub AddSyntacticClassifications(syntaxTree As SyntaxTree, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim root = syntaxTree.GetRoot(cancellationToken)
            Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Function FixClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function
    End Class
End Namespace
