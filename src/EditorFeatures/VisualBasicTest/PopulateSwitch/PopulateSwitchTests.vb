' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PopulateSwitch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PopulateSwitch
    Partial Public Class PopulateSwitchTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New PopulateSwitchDiagnosticAnalyzer(), New PopulateSwitchCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function OnlyOnFirstToken() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case [||]e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function AllMembersAndElseExist() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function AllMembersExist_NotElse() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_NotElse() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_WithElse() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_NotElse_EnumHasExplicitType() As Task
            Dim markup =
<File>
Enum MyEnum As Long
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum As Long
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_WithMembersAndElseInBlock_NewValuesAboveElseBlock() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz ' not legal.  VB does not allow fallthrough.
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz ' not legal.  VB does not allow fallthrough.
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NoMembersExist() As Task
            Dim markup =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function ImportsEnum_AllMembersExist() As Task
            Dim markup =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        [||]Select Case e
            Case CreateNew
                Exit Select
            Case Create
                Exit Select
            Case Open
                Exit Select
            Case OpenOrCreate
                Exit Select
            Case Truncate
                Exit Select
            Case Append
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function ImportsEnum_AllMembersExist_OutOfDefaultOrder() As Task
            Dim markup =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        [||]Select Case e
            Case Truncate
                Exit Select
            Case Append
                Exit Select
            Case CreateNew
                Exit Select
            Case Open
                Exit Select
            Case OpenOrCreate
                Exit Select
            Case Create
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function ImportsEnum_NotAllMembersExist() As Task
            Dim markup =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        [||]Select Case e
            Case CreateNew
                Exit Select
            Case Create
                Exit Select
            Case Open
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        Select Case e
            Case CreateNew
                Exit Select
            Case Create
                Exit Select
            Case Open
                Exit Select
            Case OpenOrCreate
                Exit Select
            Case Truncate
                Exit Select
            Case Append
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function ImportsEnum_NoMembersExist() As Task
            Dim markup =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        [||]Select Case e
            
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System.IO.FileMode
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = CreateNew
        Select Case e
            Case CreateNew
                Exit Select
            Case Create
                Exit Select
            Case Open
                Exit Select
            Case OpenOrCreate
                Exit Select
            Case Truncate
                Exit Select
            Case Append
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumHasNonFlagsAttribute() As Task
            Dim markup =
<File>
&lt;System.Obsolete&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
&lt;System.Obsolete&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsNested() As Task
            Dim markup =
<File>
Class Goo
    Enum MyEnum
        Fizz
        Buzz
        FizzBuzz
    End Enum
    Sub Bar()
        Dim e = MyEnum.Fizz
        [||]Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Class Goo
    Enum MyEnum
        Fizz
        Buzz
        FizzBuzz
    End Enum
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_SwitchIsNotEnum() As Task
            Dim markup =
<File>
Class Goo
    Sub Bar()
        Dim e = "Test"
        [||]Select Case e
            Case "Fizz"
                Exit Select
            Case "Test"
                Exit Select
        End Select
    End Sub
End Class
</File>
            Dim expected =
<File>
Class Goo
    Sub Bar()
        Dim e = "Test"
        Select Case e
            Case "Fizz"
                Exit Select
            Case "Test"
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function
    End Class
End Namespace
