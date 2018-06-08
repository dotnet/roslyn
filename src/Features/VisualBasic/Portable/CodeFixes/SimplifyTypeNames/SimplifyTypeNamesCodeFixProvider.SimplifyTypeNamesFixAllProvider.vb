' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits CodeFixProvider

        Private Class SimplifyTypeNamesFixAllProvider
            Inherits BatchSimplificationFixAllProvider

            Friend Shared Shadows ReadOnly Instance As SimplifyTypeNamesFixAllProvider = New SimplifyTypeNamesFixAllProvider

            Protected Overrides Function GetNodeToSimplify(root As SyntaxNode, model As SemanticModel, diagnostic As Diagnostic, options As DocumentOptionSet, cancellationToken As CancellationToken) As SyntaxNode
                Dim diagnosticId As String = Nothing
                Return SimplifyTypeNamesCodeFixProvider.GetNodeToSimplify(root, model, diagnostic.Location.SourceSpan, options, diagnosticId, cancellationToken)
            End Function
        End Class
    End Class
End Namespace
