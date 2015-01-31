' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryCast
    Partial Friend Class RemoveUnnecessaryCastCodeFixProvider
        Inherits CodeFixProvider

        Private Class RemoveUnnecessaryCastFixAllProvider
            Inherits BatchSimplificationFixAllProvider

            Friend Shared Shadows ReadOnly Instance As RemoveUnnecessaryCastFixAllProvider = New RemoveUnnecessaryCastFixAllProvider()

            Protected Overrides Function GetNodeToSimplify(root As SyntaxNode, model As SemanticModel, diagnostic As Diagnostic, workspace As Workspace, ByRef codeActionId As String, cancellationToken As CancellationToken) As SyntaxNode
                codeActionId = Nothing
                Return GetCastNode(root, model, diagnostic.Location.SourceSpan, cancellationToken)
            End Function
        End Class
    End Class
End Namespace
