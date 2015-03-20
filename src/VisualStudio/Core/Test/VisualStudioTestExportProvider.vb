' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Module VisualStudioTestExportProvider
        Private ReadOnly s_exportProvider As ExportProvider
        Private ReadOnly s_partCatalog As ComposableCatalog

        Sub New()
            Dim additionalAssemblies = {GetType(CSharpCodeModelService).Assembly,
                                        GetType(VisualBasicCodeModelService).Assembly}

            s_partCatalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(MinimalTestExportProvider.CreateAssemblyCatalog(additionalAssemblies))
            s_exportProvider = MinimalTestExportProvider.CreateExportProvider(s_partCatalog)
        End Sub

        Public ReadOnly Property ExportProvider As ExportProvider
            Get
                Return s_exportProvider
            End Get
        End Property

        Public ReadOnly Property PartCatalog As ComposableCatalog
            Get
                Return s_partCatalog
            End Get
        End Property
    End Module
End Namespace
