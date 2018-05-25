' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Completion.FileSystem
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
#If TODO Then ' VB doesn't have LoadDirectiveTrivia defined yet
    <ExportCompletionProviderMef1("LoadDirectiveCompletionProvider", LanguageNames.VisualBasic)>
    <TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)>
    Friend NotInheritable Class LoadDirectiveCompletionProvider : Inherits AbstractReferenceDirectiveCompletionProvider
        Protected Overrides Function TryGetStringLiteralToken(tree As SyntaxTree, position As Integer, ByRef stringLiteral As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return DirectiveCompletionProviderUtilities.TryGetStringLiteralToken(tree, position, SyntaxKind.LoadDirectiveTrivia, stringLiteral, cancellationToken)
        End Function
    End Class
#End If
End Namespace
