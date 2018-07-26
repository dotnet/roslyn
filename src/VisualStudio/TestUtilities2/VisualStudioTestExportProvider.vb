' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Module VisualStudioTestExportProvider
        Sub New()
            Dim additionalAssemblies = {GetType(CSharpCodeModelService).Assembly,
                                        GetType(VisualBasicCodeModelService).Assembly}
            Dim additionalParts = {GetType(FakeVsServiceProvider), GetType(FakePrimaryWorkspaceProvider)}

            Factory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic _
                    .WithParts(ExportProviderCache.GetOrCreateAssemblyCatalog(additionalAssemblies)) _
                    .WithParts(additionalParts))
        End Sub

        Public ReadOnly Property Factory As IExportProviderFactory

        <Export(GetType(SVsServiceProvider))>
        <[Shared]>
        <PartNotDiscoverable>
        Private NotInheritable Class FakeVsServiceProvider
            Implements SVsServiceProvider, IServiceProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
                Throw New NotImplementedException()
            End Function
        End Class

        <Export(GetType(IProgressionPrimaryWorkspaceProvider))>
        <[Shared]>
        <PartNotDiscoverable>
        Private NotInheritable Class FakePrimaryWorkspaceProvider
            Implements IProgressionPrimaryWorkspaceProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public ReadOnly Property PrimaryWorkspace As Workspace Implements IProgressionPrimaryWorkspaceProvider.PrimaryWorkspace
                Get
                    Throw New NotImplementedException()
                End Get
            End Property
        End Class
    End Module
End Namespace
