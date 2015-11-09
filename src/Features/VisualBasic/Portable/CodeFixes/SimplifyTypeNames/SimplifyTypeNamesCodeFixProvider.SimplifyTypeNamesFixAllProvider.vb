' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits CodeFixProvider

        Private Class SimplifyTypeNamesFixAllProvider
            Inherits BatchSimplificationFixAllProvider

            Friend Shared Shadows ReadOnly Instance As SimplifyTypeNamesFixAllProvider = New SimplifyTypeNamesFixAllProvider

            Protected Overrides Function GetNodeToSimplify(root As SyntaxNode, model As SemanticModel, diagnostic As Diagnostic, workspace As Workspace, ByRef codeActionId As String, cancellationToken As CancellationToken) As SyntaxNode
                codeActionId = Nothing
                Dim diagnosticId As String = Nothing
                Dim node = SimplifyTypeNamesCodeFixProvider.GetNodeToSimplify(root, model, diagnostic.Location.SourceSpan, workspace.Options, diagnosticId, cancellationToken)
                If node IsNot Nothing Then
                    codeActionId = GetCodeActionId(diagnosticId, node.ToString)
                End If

                Return node
            End Function
        End Class
    End Class
End Namespace
