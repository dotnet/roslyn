' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Partial Public Class MoveTypeTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function SingleClassInFileWithNoContainerNamespace() As Task
            Dim code =
<File>
[||]Class Class1
End Class
</File>
            Dim expectedDocumentName = "Class1.vb"

            Await TestRenameFileToMatchTypeAsync(code, expectedDocumentName)
        End Function
    End Class
End Namespace
