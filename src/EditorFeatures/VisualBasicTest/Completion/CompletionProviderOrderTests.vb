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
    Public Class CompletionProviderOrderTests
        ''' <summary>
        ''' Verifies the exact order of all built-in completion providers.
        ''' </summary>
        <Fact>
        Public Sub TestCompletionProviderOrder()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim completionProviderExports = exportProvider.GetExports(Of CompletionProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))

            Dim actualOrder = orderedVisualBasicCompletionProviders.Select(Function(x) x.Value.GetType()).ToArray()
            Dim expectedOrder =
                {
                GetType(FirstBuiltInCompletionProvider),
                GetType(KeywordCompletionProvider),
                GetType(AwaitCompletionProvider),
                GetType(SymbolCompletionProvider),
                GetType(PreprocessorCompletionProvider),
                GetType(ObjectInitializerCompletionProvider),
                GetType(ObjectCreationCompletionProvider),
                GetType(EnumCompletionProvider),
                GetType(NamedParameterCompletionProvider),
                GetType(VisualBasicSuggestionModeCompletionProvider),
                GetType(ImplementsClauseCompletionProvider),
                GetType(HandlesClauseCompletionProvider),
                GetType(PartialTypeCompletionProvider),
                GetType(CrefCompletionProvider),
                GetType(CompletionListTagCompletionProvider),
                GetType(OverrideCompletionProvider),
                GetType(XmlDocCommentCompletionProvider),
                GetType(InternalsVisibleToCompletionProvider),
                GetType(AggregateEmbeddedLanguageCompletionProvider),
                GetType(TypeImportCompletionProvider),
                GetType(ExtensionMethodImportCompletionProvider),
                GetType(LastBuiltInCompletionProvider)
                }

            AssertEx.EqualOrDiff(
                String.Join(Environment.NewLine, expectedOrder.Select(Function(x) x.FullName)),
                String.Join(Environment.NewLine, actualOrder.Select(Function(x) x.FullName)))
        End Sub

        ''' <summary>
        ''' Verifies that the order of built-in completion providers is deterministic.
        ''' </summary>
        <Fact>
        Public Sub TestCompletionProviderOrderMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim completionProviderExports = exportProvider.GetExports(Of CompletionProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))

            For i = 0 To orderedVisualBasicCompletionProviders.Count - 1
                If i = 0 Then
                    Assert.Empty(orderedVisualBasicCompletionProviders(i).Metadata.BeforeTyped)
                    Assert.Empty(orderedVisualBasicCompletionProviders(i).Metadata.AfterTyped)
                    Continue For
                ElseIf i = orderedVisualBasicCompletionProviders.Count - 1 Then
                    Assert.Empty(orderedVisualBasicCompletionProviders(i).Metadata.BeforeTyped)
                    If Not orderedVisualBasicCompletionProviders(i).Metadata.AfterTyped.Contains(orderedVisualBasicCompletionProviders(i - 1).Metadata.Name) Then
                        ' Make sure the last built-in provider comes before the marker
                        Assert.Contains(orderedVisualBasicCompletionProviders(i).Metadata.Name, orderedVisualBasicCompletionProviders(i - 1).Metadata.BeforeTyped)
                    End If

                    Continue For
                End If

                If orderedVisualBasicCompletionProviders(i).Metadata.BeforeTyped.Any() Then
                    Assert.Equal(orderedVisualBasicCompletionProviders.Last().Metadata.Name, Assert.Single(orderedVisualBasicCompletionProviders(i).Metadata.BeforeTyped))
                End If

                Dim after = Assert.Single(orderedVisualBasicCompletionProviders(i).Metadata.AfterTyped)
                Assert.Equal(orderedVisualBasicCompletionProviders(i - 1).Metadata.Name, after)
            Next
        End Sub

        <Fact>
        Public Sub TestCompletionProviderFirstNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim completionProviderExports = exportProvider.GetExports(Of CompletionProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))
            Dim firstCompletionProvider = orderedVisualBasicCompletionProviders.First()

            Assert.Equal("FirstBuiltInCompletionProvider", firstCompletionProvider.Metadata.Name)
        End Sub

        <Fact>
        Public Sub TestCompletionProviderLastNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim completionProviderExports = exportProvider.GetExports(Of CompletionProvider, CompletionProviderMetadata)()
            Dim orderedVisualBasicCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic))
            Dim lastCompletionProvider = orderedVisualBasicCompletionProviders.Last()

            Assert.Equal("LastBuiltInCompletionProvider", lastCompletionProvider.Metadata.Name)
        End Sub

        <Fact>
        Public Sub TestCompletionProviderNameMetadata()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim completionProviderExports = exportProvider.GetExports(Of CompletionProvider, CompletionProviderMetadata)()
            Dim visualBasicCompletionProviders = completionProviderExports.Where(Function(export) export.Metadata.Language = LanguageNames.VisualBasic)
            For Each export In visualBasicCompletionProviders
                Assert.Equal(export.Value.GetType().Name, export.Metadata.Name)
            Next
        End Sub
    End Class
End Namespace
