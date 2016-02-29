'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeFixes.Qualify

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.QualifyMemberAccess), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Friend Class VisualBasicQualifyMemberAccessCodeFixProvider
        Inherits AbstractQualifyMemberAccessCodeFixprovider(Of SimpleNameSyntax)
    End Class
End Namespace
