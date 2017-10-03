' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.VisualStudio.Text.BraceCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class StringLiteralCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.StringLiteralToken, SyntaxKind.StringLiteralToken)
        End Sub

        Public Overrides Function AllowOverType(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Return CheckClosingTokenKind(session, cancellationToken)
        End Function
    End Class
End Namespace
