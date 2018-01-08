' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.VirtualChars
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class RegexPatternTokenClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxTokenKinds As ImmutableArray(Of Integer) = ImmutableArray.Create(Of Integer)(SyntaxKind.StringLiteralToken)

        Public Overrides Sub AddClassifications(workspace As Workspace, token As SyntaxToken, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Debug.Assert(token.Kind() = SyntaxKind.StringLiteralToken)
            CommonRegexPatternTokenClassifier.AddClassifications(
                workspace, token, semanticModel, result,
                VisualBasicSyntaxFactsService.Instance,
                VisualBasicSemanticFactsService.Instance,
                VisualBasicVirtualCharService.Instance,
                cancellationToken)
        End Sub
    End Class
End Namespace
