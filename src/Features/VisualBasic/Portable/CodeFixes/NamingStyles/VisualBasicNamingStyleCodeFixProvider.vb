' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.NamingStyles

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.NamingStyles
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ApplyNamingStyle), [Shared]>
    Friend Class VisualBasicNamingStyleCodeFixProvider
        Inherits AbstractNamingStyleCodeFixProvider
    End Class
End Namespace