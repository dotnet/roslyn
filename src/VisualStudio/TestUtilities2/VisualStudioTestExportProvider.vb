' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Editor.UnitTests

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Module VisualStudioTestExportProvider
#Disable Warning IDE0040 ' Add accessibility modifiers - https://github.com/dotnet/roslyn/issues/45962
        Sub New()
#Enable Warning IDE0040 ' Add accessibility modifiers
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

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function CreateService(workspaceServices As HostWorkspaceServices) As IWorkspaceService Implements IWorkspaceServiceFactory.CreateService
                Return Nothing
            End Function
        End Class
    End Module
End Namespace
