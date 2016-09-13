' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportClassificationProvider(ClassificationProviderNames.Default, LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicClassificationProvider
        Inherits CommonClassificationProvider

        Private ReadOnly s_defaultClassifiers As ImmutableArray(Of ISemanticClassifier) =
            ImmutableArray.Create(Of ISemanticClassifier)(
                New NameSyntaxClassifier(),
                New ImportAliasClauseSyntaxClassifier(),
                New IdentifierNameSyntaxClassifier())

        Public Overrides Function GetDefaultSemanticClassifiers() As ImmutableArray(Of ISemanticClassifier)
            Return s_defaultClassifiers
        End Function

        Public Overrides Sub AddLexicalClassifications(text As SourceText, span As TextSpan, context As ClassificationContext, cancellationToken As CancellationToken)
            ClassificationHelpers.AddLexicalClassifications(text, span, context, cancellationToken)
        End Sub

        Protected Overrides Sub AddSyntacticClassifications(syntaxTree As SyntaxTree, textSpan As TextSpan, context As ClassificationContext, cancellationToken As CancellationToken)
            Dim root = syntaxTree.GetRoot(cancellationToken)
            SyntacticClassifier.CollectClassifiedSpans(root, textSpan, context, cancellationToken)
        End Sub

        Public Overrides Function AdjustClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function

    End Class
End Namespace
