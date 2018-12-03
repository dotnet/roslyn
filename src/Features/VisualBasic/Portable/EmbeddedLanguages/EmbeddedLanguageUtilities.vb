' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    Friend Module EmbeddedLanguageUtilities
        Public Function EscapeText(text As String, token As SyntaxToken) As String
            ' VB has no need to escape any regex characters that would be passed in through this API.
            Return text
        End Function
    End Module
End Namespace
