' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
