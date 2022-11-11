' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddObsoleteAttribute
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.AddObsoleteAttribute
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddObsoleteAttribute), [Shared]>
    Friend Class VisualBasicAddObsoleteAttributeCodeFixProvider
        Inherits AbstractAddObsoleteAttributeCodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(
                "BC40000", ' 'C' is obsolete. (msg)
                "BC40008"  ' 'C' is obsolete.
            )

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance, VisualBasicCodeFixesResources.Add_Obsolete)
        End Sub
    End Class
End Namespace
