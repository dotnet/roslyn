' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Partial Public Class MoveTypeTests
        Inherits BasicMoveTypeTestsBase

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function MultipleTypesInFileWithNoContainerNamespace() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class Class2
End Class
</File>
            Dim codeAfterMove =
<File>
Class Class2
End Class
</File>
            Dim expectedDocumentName = "Class1.vb"

            Dim destinationDocumentText =
<File>
Class Class1
End Class
</File>
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function
    End Class
End Namespace
