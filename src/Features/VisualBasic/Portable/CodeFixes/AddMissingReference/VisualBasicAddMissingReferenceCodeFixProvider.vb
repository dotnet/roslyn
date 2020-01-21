' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddMissingReference
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Packaging
Imports Microsoft.CodeAnalysis.SymbolSearch

Namespace Microsoft.CodeAnalysis.VisualBasic.AddMissingReference

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddMissingReference), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SimplifyNames)>
    Friend Class VisualBasicAddMissingReferenceCodeFixProvider
        Inherits AbstractAddMissingReferenceCodeFixProvider

        Friend Const BC30005 As String = "BC30005" ' ERR_UnreferencedAssemblyEvent3
        Friend Const BC30652 As String = "BC30652" ' ERR_UnreferencedAssembly3

        <ImportingConstructor>
        Public Sub New()
        End Sub

        ''' <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
        Friend Sub New(
            installerService As IPackageInstallerService,
            symbolSearchService As ISymbolSearchService)
            MyBase.New(installerService, symbolSearchService)
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30005, BC30652)
    End Class
End Namespace
