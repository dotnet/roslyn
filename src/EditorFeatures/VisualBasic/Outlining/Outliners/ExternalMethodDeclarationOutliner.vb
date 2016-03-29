' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class ExternalMethodDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of DeclareStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(externalMethodDeclaration As DeclareStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(externalMethodDeclaration, spans)
        End Sub
    End Class
End Namespace
