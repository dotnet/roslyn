' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Friend Module SyntaxClassifier
        Public ReadOnly DefaultSyntaxClassifiers As IEnumerable(Of ISyntaxClassifier) =
            ImmutableArray.Create(Of ISyntaxClassifier)(
                New NameSyntaxClassifier(),
                New ImportAliasClauseSyntaxClassifier(),
                New IdentifierNameSyntaxClassifier())
    End Module
End Namespace
