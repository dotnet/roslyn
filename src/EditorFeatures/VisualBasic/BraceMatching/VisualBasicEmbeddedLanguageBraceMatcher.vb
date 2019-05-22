' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class VisualBasicEmbeddedLanguageBraceMatcher
        Inherits AbstractEmbeddedLanguageBraceMatcher

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
