' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
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
    <ExportLanguageService(GetType(ISyntaxClassificationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSyntaxClassificationService
        Inherits AbstractSyntaxClassificationService

        Private ReadOnly s_defaultSyntaxClassifiers As ImmutableArray(Of ISyntaxClassifier) = ImmutableArray.Create(Of ISyntaxClassifier)(
            New NameSyntaxClassifier(),
            New ImportAliasClauseSyntaxClassifier(),
            New IdentifierNameSyntaxClassifier(),
            New OperatorOverloadSyntaxClassifier())

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function GetDefaultSyntaxClassifiers() As ImmutableArray(Of ISyntaxClassifier)
            Return s_defaultSyntaxClassifiers
        End Function

        Public Overrides Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Sub AddSyntacticClassifications(root As SyntaxNode, textSpan As TextSpan, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken)
        End Sub

        Public Overrides Function FixClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan
            Return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan)
        End Function
    End Class
End Namespace
