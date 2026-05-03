' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.SmartIndent)>
    Public Class SmartIndentProviderTests
        <WpfFact>
        Public Sub GetSmartIndent1()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim provider = exportProvider.GetExportedValue(Of ISmartIndentProvider)()

            Assert.ThrowsAny(Of ArgumentException)(
                Function() provider.CreateSmartIndent(Nothing))
        End Sub

        <WpfFact>
        Public Sub GetSmartIndent2()
            Using workspace = EditorTestWorkspace.CreateCSharp("")
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Assert.Equal(True, globalOptions.GetOption(SmartIndenterOptionsStorage.SmartIndenter))

                Dim document = workspace.Documents.Single()
                Dim provider = workspace.ExportProvider.GetExportedValues(Of ISmartIndentProvider)().OfType(Of SmartIndentProvider)().Single()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.NotNull(smartIndenter)
            End Using
        End Sub

        <WpfFact>
        Public Sub GetSmartIndent3()
            Using workspace = EditorTestWorkspace.CreateCSharp("")
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(SmartIndenterOptionsStorage.SmartIndenter, False)

                Dim document = workspace.Documents.Single()
                Dim provider = workspace.ExportProvider.GetExportedValues(Of ISmartIndentProvider)().OfType(Of SmartIndentProvider)().Single()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.Null(smartIndenter)
            End Using
        End Sub
    End Class
End Namespace
