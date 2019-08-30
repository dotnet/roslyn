' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Editor.UnitTests

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Module VisualStudioTestExportProvider
        Sub New()
            Dim additionalAssemblies = {GetType(CSharpCodeModelService).Assembly,
                                        GetType(VisualBasicCodeModelService).Assembly}

            Factory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                    ExportProviderCache.GetOrCreateAssemblyCatalog(additionalAssemblies)).WithPart(GetType(MockWorkspaceEventListenerProvider)))
        End Sub

        Public ReadOnly Property Factory As IExportProviderFactory

        ' mock default workspace event listener so that we don't try to enable solution crawler and etc
        ' implicitly
        <ExportWorkspaceServiceFactory(GetType(IWorkspaceEventListenerService), ServiceLayer.Host), System.Composition.Shared>
        Friend Class MockWorkspaceEventListenerProvider
            Implements IWorkspaceServiceFactory
            Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
                Return Nothing
            End Function
        End Class
    End Module
End Namespace
