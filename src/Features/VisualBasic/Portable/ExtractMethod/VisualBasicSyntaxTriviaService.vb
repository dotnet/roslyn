' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Friend Class VisualBasicSyntaxTriviaService
        Inherits AbstractSyntaxTriviaService

        Friend Sub New(provider As HostLanguageServices)
            MyBase.New(provider.GetService(Of ISyntaxFactsService)(), SyntaxKind.EndOfLineTrivia)
        End Sub
    End Class
End Namespace
