' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class CurlyBraceCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        End Sub
    End Class
End Namespace
