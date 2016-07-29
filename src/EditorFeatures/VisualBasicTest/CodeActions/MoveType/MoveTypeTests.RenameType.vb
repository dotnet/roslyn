' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Partial Public Class MoveTypeTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function SingleClassInFileWithNoContainerNamespace_RenameType() As Task
            Dim code =
<File>
[||]Class Class1
End Class
</File>

            Dim codeAfterRenamingType =
<File>
Class [|test1|]
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, codeAfterRenamingType)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function TypeNameMatchesFileName_RenameType() As Task
            ' testworkspace creates files Like test1.cs, test2.cs And so on.. 
            ' so type name matches filename here And rename file action should Not be offered.
            Dim code =
<File>
[||]Class test1
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, expectedCodeAction:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function MoreThanOneTypeInFile_RenameType() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class Class2
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, expectedCodeAction:=False)
        End Function
    End Class
End Namespace
