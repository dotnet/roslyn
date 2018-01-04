' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.BraceMatching
Imports Microsoft.CodeAnalysis.VisualBasic.RegularExpressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class RegexBraceMatcher
        Inherits AbstractRegexBraceMatcher

        Public Sub New()
            MyBase.New(
                SyntaxKind.StringLiteralToken,
                VisualBasicSyntaxFactsService.Instance,
                VisualBasicSemanticFactsService.Instance,
                VisualBasicVirtualCharService.Instance)
        End Sub
    End Class
End Namespace
