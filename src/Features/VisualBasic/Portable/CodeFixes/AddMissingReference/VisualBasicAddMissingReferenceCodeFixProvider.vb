' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddMissingReference
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.AddMissingReference

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddMissingReference), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SimplifyNames)>
    Friend Class VisualBasicAddMissingReferenceCodeFixProvider
        Inherits AbstractAddMissingReferenceCodeFixProvider

        Friend Const BC30005 As String = "BC30005" ' ERR_UnreferencedAssemblyEvent3
        Friend Const BC30652 As String = "BC30652" ' ERR_UnreferencedAssembly3

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30005, BC30652)
    End Class
End Namespace
