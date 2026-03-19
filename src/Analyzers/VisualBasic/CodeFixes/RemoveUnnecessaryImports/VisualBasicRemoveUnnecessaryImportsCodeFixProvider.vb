' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddMissingReference)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.FileHeader)>
    Friend Class VisualBasicRemoveUnnecessaryImportsCodeFixProvider
        Inherits AbstractRemoveUnnecessaryImportsCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VisualBasicCodeFixesResources.Remove_unnecessary_imports
        End Function

        Protected Overrides Function GetSyntaxFormatting() As ISyntaxFormatting
            Return VisualBasicSyntaxFormatting.Instance
        End Function
    End Class
End Namespace
