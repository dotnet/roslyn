' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion
    <[UseExportProvider]>
    Public Class CompletionServiceTests
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AcquireCompletionService()
            Dim workspace = New AdhocWorkspace()

            Dim document = workspace _
                .AddProject("TestProject", LanguageNames.VisualBasic) _
                .AddDocument("TestDocument.vb", String.Empty)

            Dim service = CompletionService.GetService(document)
            Assert.NotNull(service)
        End Sub
    End Class
End Namespace
