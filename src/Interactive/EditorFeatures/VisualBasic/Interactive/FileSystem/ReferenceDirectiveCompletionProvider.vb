' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Completion.FileSystem
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders

    <ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.VisualBasic)>
    <TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)>
    Friend Class ReferenceDirectiveCompletionProvider : Inherits AbstractReferenceDirectiveCompletionProvider
        Protected Overrides Function TryGetStringLiteralToken(tree As SyntaxTree, position As Integer, ByRef stringLiteral As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            If tree.IsEntirelyWithinStringLiteral(position, cancellationToken) Then
                Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)

                ' Verifies that the string literal under caret is the path token.
                If token.IsKind(SyntaxKind.StringLiteralToken) AndAlso token.Parent.IsKind(SyntaxKind.ReferenceDirectiveTrivia) Then
                    stringLiteral = token
                    Return True
                End If
            End If

            stringLiteral = Nothing
            Return False
        End Function
    End Class

End Namespace