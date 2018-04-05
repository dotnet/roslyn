' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    <[UseExportProvider]>
    Public Class SmartIndentProviderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent1()
            Dim provider = New SmartIndentProvider()

            Assert.ThrowsAny(Of ArgumentException)(
                Function() provider.CreateSmartIndent(Nothing))
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent2()
            Using workspace = TestWorkspace.CreateCSharp("")
                Assert.Equal(True, workspace.Options.GetOption(InternalFeatureOnOffOptions.SmartIndenter))

                Dim document = workspace.Projects.Single().Documents.Single()
                Dim provider = New SmartIndentProvider()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.NotNull(smartIndenter)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent3()
            Using workspace = TestWorkspace.CreateCSharp("")
                workspace.Options = workspace.Options.WithChangedOption(InternalFeatureOnOffOptions.SmartIndenter, False)

                Dim document = workspace.Projects.Single().Documents.Single()
                Dim provider = New SmartIndentProvider()
                Dim smartIndenter = provider.CreateSmartIndent(document.GetTextView())

                Assert.Null(smartIndenter)
            End Using
        End Sub
    End Class
End Namespace
