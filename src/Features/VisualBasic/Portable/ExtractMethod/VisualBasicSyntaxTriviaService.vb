' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
