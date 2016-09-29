' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Partial Public Class MoveTypeTests
        Inherits BasicMoveTypeTestsBase

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function TestMissing_OnMatchingFileName() As Task
            Dim code =
<File>
[||]Class test1
End Class
</File>.ConvertTestSourceTag()

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function TestMissing_Nested_OnMatchingFileName_Simple() As Task
            Dim code =
<File>
Class Outer
    [||]Class test1
    End Class
End Class
</File>.ConvertTestSourceTag()

            Await TestMissingAsync(code)
        End Function

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function MoveNestedTypeToNewFile_Simple() As Task
            Dim code =
<File>
Public Class Class1
    Class Class2[||]
    End Class
End Class
</File>
            Dim codeAfterMove =
<File>
Public Partial Class Class1
End Class
</File>
            Dim expectedDocumentName = "Class2.vb"

            Dim destinationDocumentText =
<File>
Public Partial Class Class1
    Class Class2

    End Class
End Class
</File>
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function MoveNestedTypeToNewFile_Simple_DottedName() As Task
            Dim code =
<File>
Public Class Class1
    Class Class2[||]
    End Class
End Class
</File>
            Dim codeAfterMove =
<File>
Public Partial Class Class1
End Class
</File>
            Dim expectedDocumentName = "Class1.Class2.vb"

            Dim destinationDocumentText =
<File>
Public Partial Class Class1
    Class Class2

    End Class
End Class
</File>
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index:=1)
        End Function
    End Class
End Namespace
