' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    <Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
    Partial Public Class MoveTypeTests

        <WpfFact>
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

        <WpfFact>
        Public Async Function TestMissing_TypeNameMatchesFileName_RenameType() As Task
            ' testworkspace creates files Like test1.cs, test2.cs And so on.. 
            ' so type name matches filename here And rename file action should Not be offered.
            Dim code =
<File>
[||]Class test1
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, expectedCodeAction:=False)
        End Function

        <WpfFact>
        Public Async Function TestMissing_MultipleTopLevelTypesInFileAndAtleastOneMatchesFileName_RenameType() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class test1
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, expectedCodeAction:=False)
        End Function

        <WpfFact>
        Public Async Function MultipleTopLevelTypesInFileAndNoneMatchFileName1_RenameType() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class Class2
End Class
</File>

            Dim codeAfterRenamingType =
<File>
Class [|test1|]
End Class

Class Class2
End Class
</File>

            Await TestRenameTypeToMatchFileAsync(code, codeAfterRenamingType)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")>
        Public Async Function NothingOfferedWhenTypeHasNoNameYet1() As Task
            Dim code = "Class[||]"
            Await TestMissingAsync(code)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40043")>
        Public Async Function NothingOfferedWhenTypeHasNoNameYet() As Task
            Dim code = "Class[||]
End Class"
            Await TestMissingAsync(code)
        End Function
    End Class
End Namespace
