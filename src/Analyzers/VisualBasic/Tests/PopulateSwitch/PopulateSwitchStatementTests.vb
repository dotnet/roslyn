' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PopulateSwitch
    <Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
    Partial Public Class PopulateSwitchStatementTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicPopulateSwitchStatementDiagnosticAnalyzer(), New VisualBasicPopulateSwitchStatementCodeFixProvider())
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40240")>
        Public Async Function TestAddMissingCasesForNullableEnum As Task
            Dim markup =
<File>
Module Program
    Sub Main(args As String())
        Dim bar As Bar? = Program.Bar.Option1

        [||]Select Case bar
            Case Program.Bar.Option1
                Exit Select
            Case Program.Bar.Option2
                Exit Select
            Case vbNull
                Exit Select
        End Select
    End Sub

    Enum Bar
        Option1 = 1
        Option2 = 2
        Option3 = 3
    End Enum
End Module
</File>
            Dim expected =
<File>
Module Program
    Sub Main(args As String())
        Dim bar As Bar? = Program.Bar.Option1

        Select Case bar
            Case Program.Bar.Option1
                Exit Select
            Case Program.Bar.Option2
                Exit Select
            Case vbNull
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub

    Enum Bar
        Option1 = 1
        Option2 = 2
        Option3 = 3
    End Enum
End Module
</File>

            Await TestAsync(markup, expected)
        End Function
    End Class
End Namespace
