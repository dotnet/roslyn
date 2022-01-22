' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Language.CodeCleanUp
Imports Microsoft.VisualStudio.LanguageServices.FindUsages
Imports Microsoft.VisualStudio.LanguageServices.UnitTests

Namespace Tests
    <UseExportProvider>
    Public Class CodeCleanupExportTests

        <Fact>
        Public Sub TestUniqueFixId()
            Dim exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider()
            Dim fixIdDefinitions = exportProvider.GetExports(Of FixIdDefinition, NameMetadata)()
            Dim groups = fixIdDefinitions.GroupBy(Function(x) x.Metadata.Name).Where(Function(x) x.Count() > 1)
            Assert.Empty(groups.Select(Function(group) group.Select(Function(part) part.Metadata.Name)))
        End Sub

    End Class
End Namespace
