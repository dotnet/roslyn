' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Partial Friend Class VisualBasicSyntaxClassificationService
        Inherits AbstractSyntaxClassificationService

        Private ReadOnly s_defaultSyntaxClassifiers As ImmutableArray(Of ISyntaxClassifier)

        <Obsolete(MefConstruction.FactoryMethodMessage, True)>
        Public Sub New(languageServices As HostLanguageServices)
            Dim syntaxClassifiers = ImmutableArray(Of ISyntaxClassifier).Empty
            Dim embeddedLanguagesProvider = languageServices.GetService(Of IEmbeddedLanguagesProvider)()
            If embeddedLanguagesProvider IsNot Nothing Then
                syntaxClassifiers = syntaxClassifiers.Add(New EmbeddedLanguagesClassifier(embeddedLanguagesProvider))
            End If

            s_defaultSyntaxClassifiers = syntaxClassifiers.AddRange(
                {
                    New NameSyntaxClassifier(),
                    New ImportAliasClauseSyntaxClassifier(),
                    New IdentifierNameSyntaxClassifier(),
                    New OperatorOverloadSyntaxClassifier()
                })
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
