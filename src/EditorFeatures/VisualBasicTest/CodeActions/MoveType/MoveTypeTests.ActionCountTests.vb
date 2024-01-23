' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    <Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
    Partial Public Class MoveTypeTests
        Inherits BasicMoveTypeTestsBase

        <WpfFact>
        Public Async Function MoveType_ActionCounts_RenameOnly() As Task
            Dim code =
<File>
[||]Class Class1
End Class
</File>.ConvertTestSourceTag()

            'Fixes offered will be rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=2)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithDefinitionSelected() As Task
            Dim code =
<File>
[|Class Class1|]
End Class
</File>.ConvertTestSourceTag()

            'Fixes offered will be rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=2)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithDefinitionAndAttributeSelected() As Task
            Dim code =
"[|<Obsolete>
Class Class1
|]
End Class"

            'Fixes offered will be rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=2)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithDefinitionAndCommentSelected() As Task
            Dim code =
"[|''' <summary>
''' 
''' </summary>
Class Class1
|]
End Class"

            'Fixes offered will be rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=2)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithDefinitionAndAttributeAndCommentSelected() As Task
            Dim code =
"[|''' <summary>
''' 
''' </summary>
<Obsolete>
Class Class1
|]
End Class"

            'Fixes offered will be rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=2)
        End Function

        <WpfFact>
        Public Async Function MoveType_InsideClassSelected() As Task
            Dim code =
<File>
Class Class1
[|
    Sub Something()
    End Sub|]
End Class
</File>.ConvertTestSourceTag()

            Await TestActionCountAsync(code, count:=0)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithAttributeSelected() As Task
            Dim code =
"[|<Obsolete>|]
Class Class1
End Class"

            Await TestActionCountAsync(code, count:=0)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithCommentSelected() As Task
            Dim code =
"[|''' <summary>
''' 
''' </summary>|]
Class Class1
End Class"

            Await TestActionCountAsync(code, count:=0)
        End Function

        <WpfFact>
        Public Async Function MoveType_WithAttributeAndCommentSelected() As Task
            Dim code =
"[|''' <summary>
''' 
''' </summary>
<Obsolete>|]
Class Class1
End Class"

            Await TestActionCountAsync(code, count:=0)
        End Function

        <WpfFact>
        Public Async Function MoveType_ActionCounts_MoveOnly() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class test1 'this matches file name assigned by TestWorkspace
End Class
</File>.ConvertTestSourceTag()

            ' Fixes offered will be move type to new file.
            Await TestActionCountAsync(code, count:=1)
        End Function

        <WpfFact>
        Public Async Function MoveType_ActionCounts_RenameAndMove() As Task
            Dim code =
<File>
[||]Class Class1
End Class

Class Class2
End Class
</File>.ConvertTestSourceTag()

            ' Fixes offered will be move type, rename type to match file, rename file to match type.
            Await TestActionCountAsync(code, count:=3)
        End Function

        <WpfFact>
        Public Async Function MoveType_ActionCounts_All() As Task
            Dim code =
<File>
Class Class1
    Class Class2[||]
    End Class
End Class
Class Class3
End Class
</File>.ConvertTestSourceTag()

            ' Fixes offered will be
            ' 1. move type to InnerType.vb
            ' 2. move type to OuterType.InnerType.vb
            ' 3. rename file to InnerType.vb
            ' 4. rename file to OuterType.InnerType.vb
            ' 5. rename type to test1 (which Is the default document name given by TestWorkspace).
            Await TestActionCountAsync(code, count:=5)
        End Function
    End Class
End Namespace
