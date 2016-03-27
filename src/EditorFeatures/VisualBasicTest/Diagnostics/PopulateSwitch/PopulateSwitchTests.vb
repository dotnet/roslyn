Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.PopulateSwitch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PopulateSwitch
    Partial Public Class PopulateSwitchTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicPopulateSwitchDiagnosticAnalyzer(), New PopulateSwitchCodeFixProvider())
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select|]
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select|]
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
Class Foo
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

            Await TestAsync(markup, expected, compareTokens:=False)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
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
Class Foo
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

            Await TestAsync(markup, expected, compareTokens:=False)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select
            Case Else
                Exit Select|]
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
Class Foo
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

            Await TestAsync(markup, expected, compareTokens:=False)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
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
Class Foo
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

            Await TestAsync(markup, expected, compareTokens:=False)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
            Case Else
                Exit Select|]
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            Case MyEnum.Fizz
                Exit Select
            Case MyEnum.FizzBuzz
                Exit Select
            Case MyEnum.Buzz
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, compareTokens:=False)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e[||]
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
Class Foo
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
            
            Await TestAsync(markup, expected, compareTokens:=False)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsFlags() As Task
            Dim markup =
<File>
Imports System
&lt;Flags&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsFlagsAttribute() As Task
            Dim markup =
<File>
Imports System
&lt;FlagsAttribute&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsFullyQualifiedSystemFlags() As Task
            Dim markup =
<File>
&lt;System.Flags&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsFullyQualifiedSystemFlagsAttribute() As Task
            Dim markup =
<File>
&lt;System.FlagsAttribute&gt;
Enum MyEnum
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
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
Class Foo
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
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
Class Foo
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

            Await TestAsync(markup, expected, compareTokens:=False)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_EnumIsNested() As Task
            Dim markup =
<File>
Class Foo
    Enum MyEnum
        Fizz
        Buzz
        FizzBuzz
    End Enum
    Sub Bar()
        Dim e = MyEnum.Fizz
        Select Case e
            [|Case MyEnum.Fizz
                Exit Select
            Case MyEnum.Buzz
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Dim expected =
<File>
Class Foo
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
            Case Foo.MyEnum.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(markup, expected, compareTokens:=False)
        End Function
        
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        Public Async Function NotAllMembersExist_SwitchIsNotEnum() As Task
            Dim markup =
<File>
Class Foo
    Sub Bar()
        Dim e = "Test"
        Select Case e
            [|Case "Fizz"
                Exit Select
            Case Test"
                Exit Select|]
        End Select
    End Sub
End Class
</File>

            Await TestMissingAsync(markup)
        End Function
    End Class
End Namespace