' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Completion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion
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
