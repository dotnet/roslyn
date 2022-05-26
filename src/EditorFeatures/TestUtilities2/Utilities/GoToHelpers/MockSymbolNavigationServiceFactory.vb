' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    <ExportWorkspaceServiceFactory(GetType(ISymbolNavigationService), ServiceLayer.Test), [Shared], PartNotDiscoverable>
    Friend Class MockSymbolNavigationServiceFactory
        Implements IWorkspaceServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return New MockSymbolNavigationService()
        End Function
    End Class
End Namespace
