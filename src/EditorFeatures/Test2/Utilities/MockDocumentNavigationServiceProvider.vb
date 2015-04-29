' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Navigation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    ' Note: by default, TestWorkspace produces a composition from all assemblies except EditorServicesTest2.
    ' This type has to be defined here until we get that cleaned up. Otherwise, other tests may import it.
    <ExportWorkspaceServiceFactory(GetType(IDocumentNavigationService), ServiceLayer.Host), [Shared]>
    Public Class MockDocumentNavigationServiceProvider
        Implements IWorkspaceServiceFactory

        Private _instance As MockDocumentNavigationService = New MockDocumentNavigationService()

        Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
            Return _instance
        End Function
    End Class
End Namespace
