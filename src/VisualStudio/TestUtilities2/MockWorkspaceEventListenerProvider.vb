' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    ' mock default workspace event listener so that we don't try to enable solution crawler and etc implicitly
    <ExportWorkspaceServiceFactory(GetType(IWorkspaceEventListenerService), ServiceLayer.Test), [Shared], PartNotDiscoverable>
    Friend Class MockWorkspaceEventListenerProvider
        Implements IWorkspaceServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return Nothing
        End Function
    End Class
End Namespace
