' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion
    <UseExportProvider>
    Public Class ArgumentProviderOrderTests
        ''' <summary>
        ''' Verifies the exact order of all built-in argument providers.
        ''' </summary>
        <Fact>
        Public Sub TestArgumentProviderOrder()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim argumentProviderExports = exportProvider.GetExports(Of ArgumentProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))

            Dim actualOrder = orderedVisualBasicArgumentProviders.Select(Function(x) x.Value.GetType()).ToArray()
            Dim expectedOrder =
                {
                GetType(FirstBuiltInArgumentProvider),
                GetType(ContextVariableArgumentProvider),
                GetType(DefaultArgumentProvider),
                GetType(LastBuiltInArgumentProvider)
                }

            AssertEx.EqualOrDiff(
                String.Join(Environment.NewLine, expectedOrder.Select(Function(x) x.FullName)),
                String.Join(Environment.NewLine, actualOrder.Select(Function(x) x.FullName)))
        End Sub

        ''' <summary>
        ''' Verifies that the order of built-in argument providers is deterministic.
        ''' </summary>
        <Fact>
        Public Sub TestArgumentProviderOrderMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim argumentProviderExports = exportProvider.GetExports(Of ArgumentProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))

            For i = 0 To orderedVisualBasicArgumentProviders.Count - 1
                If i = 0 Then
                    Assert.Empty(orderedVisualBasicArgumentProviders(i).Metadata.BeforeTyped)
                    Assert.Empty(orderedVisualBasicArgumentProviders(i).Metadata.AfterTyped)
                    Continue For
                ElseIf i = orderedVisualBasicArgumentProviders.Count - 1 Then
                    Assert.Empty(orderedVisualBasicArgumentProviders(i).Metadata.BeforeTyped)
                    If Not orderedVisualBasicArgumentProviders(i).Metadata.AfterTyped.Contains(orderedVisualBasicArgumentProviders(i - 1).Metadata.Name) Then
                        ' Make sure the last built-in provider comes before the marker
                        Assert.Contains(orderedVisualBasicArgumentProviders(i).Metadata.Name, orderedVisualBasicArgumentProviders(i - 1).Metadata.BeforeTyped)
                    End If

                    Continue For
                End If

                If orderedVisualBasicArgumentProviders(i).Metadata.BeforeTyped.Any() Then
                    Assert.Equal(orderedVisualBasicArgumentProviders.Last().Metadata.Name, Assert.Single(orderedVisualBasicArgumentProviders(i).Metadata.BeforeTyped))
                End If

                Dim after = Assert.Single(orderedVisualBasicArgumentProviders(i).Metadata.AfterTyped)
                Assert.Equal(orderedVisualBasicArgumentProviders(i - 1).Metadata.Name, after)
            Next
        End Sub

        <Fact>
        Public Sub TestArgumentProviderFirstNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim argumentProviderExports = exportProvider.GetExports(Of ArgumentProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))
            Dim firstArgumentProvider = orderedVisualBasicArgumentProviders.First()

            Assert.Equal("FirstBuiltInArgumentProvider", firstArgumentProvider.Metadata.Name)
        End Sub

        <Fact>
        Public Sub TestArgumentProviderLastNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim argumentProviderExports = exportProvider.GetExports(Of ArgumentProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))
            Dim lastArgumentProvider = orderedVisualBasicArgumentProviders.Last()

            Assert.Equal("LastBuiltInArgumentProvider", lastArgumentProvider.Metadata.Name)
        End Sub

        <Fact>
        Public Sub TestArgumentProviderNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim argumentProviderExports = exportProvider.GetExports(Of ArgumentProvider, CompletionProviderMetadata)()
            Dim visualBasicArgumentProviders = argumentProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic)
            For Each export In visualBasicArgumentProviders
                Assert.Equal(export.Value.GetType().Name, export.Metadata.Name)
            Next
        End Sub
    End Class
End Namespace
