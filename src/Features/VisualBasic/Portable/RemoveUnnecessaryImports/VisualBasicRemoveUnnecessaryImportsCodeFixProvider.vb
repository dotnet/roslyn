' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddMissingReference)>
    Friend Class VisualBasicRemoveUnnecessaryImportsCodeFixProvider
        Inherits AbstractRemoveUnnecessaryImportsCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Remove_Unnecessary_Imports
        End Function
    End Class
End Namespace
