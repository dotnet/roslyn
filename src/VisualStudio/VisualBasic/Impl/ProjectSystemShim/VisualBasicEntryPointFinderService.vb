' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    <ExportLanguageService(GetType(IEntryPointFinderService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEntryPointFinderService
        Implements IEntryPointFinderService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function FindEntryPoints(symbol As INamespaceSymbol, findFormsOnly As Boolean) As IEnumerable(Of INamedTypeSymbol) Implements IEntryPointFinderService.FindEntryPoints
            Return EntryPointFinder.FindEntryPoints(symbol, findFormsOnly)
        End Function
    End Class
End Namespace
