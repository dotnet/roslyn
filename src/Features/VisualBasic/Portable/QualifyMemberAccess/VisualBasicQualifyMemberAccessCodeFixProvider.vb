'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.QualifyMemberAccess), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Friend Class VisualBasicQualifyMemberAccessCodeFixProvider
        Inherits AbstractQualifyMemberAccessCodeFixprovider(Of SimpleNameSyntax)

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Add_Me
        End Function
    End Class
End Namespace
