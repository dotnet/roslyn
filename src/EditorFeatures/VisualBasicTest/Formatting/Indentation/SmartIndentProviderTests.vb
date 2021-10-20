' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    <[UseExportProvider]>
    Public Class SmartIndentProviderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent1()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()
            Dim provider = exportProvider.GetExportedValue(Of ISmartIndentProvider)()

            Assert.ThrowsAny(Of ArgumentException)(
                Function() provider.CreateSmartIndent(Nothing))
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent2()
            Using workspace = TestWorkspace.CreateCSharp("")
                Assert.Equal(True, workspace.Options.GetOption(InternalFeatureOnOffOptions.SmartIndenter))

                Dim document = workspace.Projects.Single().Documents.Single()
                Dim provider = workspace.ExportProvider.GetExportedValues(Of ISmartIndentProvider)().OfType(Of SmartIndentProvider)().Single()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.NotNull(smartIndenter)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent3()
            Using workspace = TestWorkspace.CreateCSharp("")
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(InternalFeatureOnOffOptions.SmartIndenter, False)))

                Dim document = workspace.Projects.Single().Documents.Single()
                Dim provider = workspace.ExportProvider.GetExportedValues(Of ISmartIndentProvider)().OfType(Of SmartIndentProvider)().Single()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.Null(smartIndenter)
            End Using
        End Sub
    End Class
End Namespace
