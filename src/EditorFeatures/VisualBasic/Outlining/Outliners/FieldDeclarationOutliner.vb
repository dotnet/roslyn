' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class FieldDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of FieldDeclarationSyntax)

        Protected Overrides Sub CollectOutliningSpans(fieldDeclaration As FieldDeclarationSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(fieldDeclaration, spans)
        End Sub
    End Class
End Namespace
