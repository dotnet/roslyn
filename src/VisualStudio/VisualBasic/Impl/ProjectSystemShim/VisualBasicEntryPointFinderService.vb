' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    <ExportLanguageService(GetType(IEntryPointFinderService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicEntryPointFinderService
        Inherits AbstractEntryPointFinderService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function FindEntryPoints(compilation As Compilation, findFormsOnly As Boolean) As IEnumerable(Of INamedTypeSymbol)
            Return VisualBasicEntryPointFinder.FindEntryPoints(compilation, findFormsOnly)
        End Function
    End Class
End Namespace
