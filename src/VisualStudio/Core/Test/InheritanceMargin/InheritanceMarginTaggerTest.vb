' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.InheritanceMargin
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.[Shared].Extensions
Imports Microsoft.CodeAnalysis.UnitTests.Fakes
Imports Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin

    <Trait(Traits.Feature, Traits.Features.InheritanceMargin)>
    <UseExportProvider>
    Public Class InheritanceMarginTaggerTest

        <WpfFact>
        Public Async Function Test() As Task
            Dim code = "
interface II
[|class Program2 : II|]
{
}"
            Using workspace = EditorTestWorkspace.CreateCSharp(
                code, composition:=EditorTestCompositions.EditorFeatures.AddParts(GetType(InheritanceMarginTaggerProvider)))
                Dim document = workspace.Documents.First()
                Dim subjectBuffer = document.GetTextBuffer()
                Dim exportProvider = workspace.ExportProvider
                Dim taggerProvider = exportProvider.GetExportedValues(Of IViewTaggerProvider).
                    Single((Function(viewTaggerProvider) viewTaggerProvider.GetType().Name.Equals(NameOf(InheritanceMarginTaggerProvider))))
                Dim tagger = taggerProvider.CreateTagger(Of InheritanceMarginTag)(document.GetTextView(), subjectBuffer)

                Dim asyncListenerProvider = workspace.GetService(Of IAsynchronousOperationListenerProvider)
                Dim globalOptionService = workspace.GetService(Of IGlobalOptionService)
                globalOptionService.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp, False)
                globalOptionService.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, LanguageNames.CSharp, True)
                Await asyncListenerProvider.WaitAllDispatcherOperationAndTasksAsync(workspace, FeatureAttribute.InheritanceMargin)

                Dim output As String = Nothing
                Dim span As TextSpan = Nothing
                MarkupTestFile.GetSpan(code, output, span)
                Dim tags = tagger.
                    GetTags(New NormalizedSnapshotSpanCollection(subjectBuffer.CurrentSnapshot, span.ToSnapshotSpan(subjectBuffer.CurrentSnapshot))).ToArray()
                Assert.Single(tags)
            End Using
        End Function
    End Class
End Namespace
