' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    <ExportLanguageService(GetType(IEntryPointFinderService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEntryPointFinderService
        Implements IEntryPointFinderService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function FindEntryPoints(symbol As INamespaceSymbol, findFormsOnly As Boolean) As IEnumerable(Of INamedTypeSymbol) Implements IEntryPointFinderService.FindEntryPoints
            Return EntryPointFinder.FindEntryPoints(symbol, findFormsOnly)
        End Function
    End Class
End Namespace
