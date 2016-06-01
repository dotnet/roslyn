' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Options.Style.NamingPreferences
    Public Class NamingRuleDialogViewModelTests

        Private _exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(GetType(StubVsEditorAdaptersFactoryService)))

        <WpfFact>
        Public Sub TestTODO()
            Dim viewModel = New NamingRuleDialogViewModel(
                "Title",
                Nothing,
                New List(Of SymbolSpecificationViewModel),
                Nothing,
                New List(Of NamingStyleViewModel),
                Nothing,
                New List(Of NamingRuleTreeItemViewModel),
                New EnforcementLevel(DiagnosticSeverity.Error),
                Nothing)
        End Sub
    End Class
End Namespace
