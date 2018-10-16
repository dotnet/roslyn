' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InitializeParameter

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InitializeParameter
    Partial Public Class InitializeMemberFromParameterTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInitializeMemberFromParameterCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithSameName() As Task

            Await TestInRegularAndScript1Async(
"
class C
    private s As String

    public sub new([||]s As String)

    end sub
end class",
"
class C
    private s As String

    public sub new(s As String)
        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestEndOfParameter1() As Task

            Await TestInRegularAndScript1Async(
"
class C
    private s As String

    public sub new(s As String[||])

    end sub
end class",
"
class C
    private s As String

    public sub new(s As String)
        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestEndOfParameter2() As Task

            Await TestInRegularAndScript1Async(
"
class C
    private s As String

    public sub new(s As String[||], t As String)
    end sub
end class",
"
class C
    private s As String

    public sub new(s As String, t As String)
        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithUnderscoreName() As Task

            Await TestInRegularAndScript1Async(
"
class C
    private _s As String

    public sub new([||]s As String)

    end sub
end class",
"
class C
    private _s As String

    public sub new(s As String)
        _s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeWritableProperty() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly property S As String

    public sub new([||]s As String)

    end sub
end class",
"
class C
    Private ReadOnly property S As String

    public sub new(s As String)
        Me.S = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithDifferentName() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    private t As String

    public sub new([||]s As String)
    end sub
end class",
"
class C
    private t As String

    public sub new(s As String)
        Me.S = s
    end sub

    Public ReadOnly Property S As String
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeDoesNotUsePropertyWithUnrelatedName() As Task

            Await TestInRegularAndScriptAsync(
"
class C
    Private ReadOnly Property T As String

    public sub new([||]s As String)

    end sub
end class",
"
class C
    Private ReadOnly Property T As String
    Public ReadOnly Property S As String

    public sub new(s As String)
        Me.S = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithWrongType1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    private s As Integer

    public sub new([||]s As String)

    end sub
end class",
"
class C
    private s As Integer

    public sub new(s As String)
        S1 = s
    end sub

    Public ReadOnly Property S1 As String
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithWrongType2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    private s As Integer

    public sub new([||]s As String)

    end sub
end class",
"
class C
    Private ReadOnly s1 As String
    private s As Integer

    public sub new(s As String)
        s1 = s
    end sub
end class", index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithConvertibleType() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    private s As Object

    public sub new([||]s As String)

    end sub
end class",
"
class C
    private s As Object

    public sub new(s As String)
        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestWhenAlreadyInitialized1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C

    private s As Integer
    private x As Integer

    public sub new([||]s As String)
        x = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestWhenAlreadyInitialized3() As Task
            Await TestInRegularAndScript1Async(
"
class C

    private s As Integer

    public sub new([||]s As String)
        Me.s = 0
    end sub
end class",
"
class C

    private s As Integer

    public sub new([||]s As String)
        Me.s = 0
        S1 = s
    end sub

    Public ReadOnly Property S1 As String
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInsertionLocation1() As Task
            Await TestInRegularAndScript1Async(
"
class C

    private s As String
    private t As String

    public sub new([||]s As String, t As String)
        Me.t = t   
    end sub
end class",
"
class C

    private s As String
    private t As String

    public sub new(s As String, t As String)
        Me.s = s
        Me.t = t   
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInsertionLocation2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    private s As String
    private t As String

    public sub new(s As String, [||]t As String)
        Me.s = s   
    end sub
end class",
"
class C
    private s As String
    private t As String

    public sub new(s As String, t As String)
        Me.s = s
        Me.t = t
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInsertionLocation3() As Task
            Await TestInRegularAndScript1Async(
"
class C

    private s As String

    public sub new([||]s As String)
        if true then
        end if
    end sub
end class",
"
class C

    private s As String

    public sub new(s As String)
        if true then
        end if

        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestNotInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C

    private s As String

    public function M([||]s As String)

    end sub
end class")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInsertionLocation6() As Task

            Await TestInRegularAndScript1Async(
"
class C
    public sub new(s As String, [||]t As String)
        Me.S = s   
    end sub

    Public ReadOnly Property S As String
end class",
"
class C
    public sub new(s As String, t As String)
        Me.S = s
        Me.T = t
    end sub

    Public ReadOnly Property S As String
    Public ReadOnly Property T As String
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInsertionLocation7() As Task

            Await TestInRegularAndScript1Async(
"
class C
    public sub new([||]s As String, t As String)
        Me.T = t   
    end sub

    public ReadOnly Property T As String
end class",
"
class C
    public sub new(s As String, t As String)
        Me.S = s
        Me.T = t   
    end sub

    Public ReadOnly Property S As String
    public ReadOnly Property T As String
end class")
        End Function

        <WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithParameterNameSelected1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    private s As String

    public sub new([|s|] As String)

    end sub
end class",
"
class C
    private s As String

    public sub new(s As String)
        Me.s = s
    end sub
end class")
        End Function

        <WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeFieldWithParameterNameSelected2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    private s As String

    public sub new(i as integer, [|s|] As String)

    end sub
end class",
"
class C
    private s As String

    public sub new(i as integer, s As String)
        Me.s = s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassProperty_RequiredAccessibilityOmitIfDefault() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.Test2 = test2
    End Sub

    ReadOnly Property Test2 As Integer
End Class
", index:=0, parameters:=OmitIfDefault_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassProperty_RequiredAccessibilityNever() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.Test2 = test2
    End Sub

    ReadOnly Property Test2 As Integer
End Class
", index:=0, parameters:=Never_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassProperty_RequiredAccessibilityAlways() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.Test2 = test2
    End Sub

    Public ReadOnly Property Test2 As Integer
End Class
", index:=0, parameters:=Always_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassField_RequiredAccessibilityOmitIfDefault() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test2 As Integer
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.test2 = test2
    End Sub
End Class
", index:=1, parameters:=OmitIfDefault_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassField_RequiredAccessibilityNever() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test2 As Integer
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.test2 = test2
    End Sub
End Class
", index:=1, parameters:=Never_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeClassField_RequiredAccessibilityAlways() As Task
            Await TestInRegularAndScript1Async("
Class C
    ReadOnly test As Integer = 5

    Public Sub New(ByVal test As Integer, ByVal [|test2|] As Integer)
    End Sub
End Class
", "
Class C
    ReadOnly test As Integer = 5
    Private ReadOnly test2 As Integer

    Public Sub New(ByVal test As Integer, ByVal test2 As Integer)
        Me.test2 = test2
    End Sub
End Class
", index:=1, parameters:=Always_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructProperty_RequiredAccessibilityOmitIfDefault() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Public Sub New(ByVal test As Integer)
        Me.Test = test
    End Sub

    ReadOnly Property Test As Integer
End Structure
", index:=0, parameters:=OmitIfDefault_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructProperty_RequiredAccessibilityNever() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Public Sub New(ByVal test As Integer)
        Me.Test = test
    End Sub

    ReadOnly Property Test As Integer
End Structure
", index:=0, parameters:=Never_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructProperty_RequiredAccessibilityAlways() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Public Sub New(ByVal test As Integer)
        Me.Test = test
    End Sub

    Public ReadOnly Property Test As Integer
End Structure
", index:=0, parameters:=Always_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructField_RequiredAccessibilityOmitIfDefault() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Private ReadOnly test As Integer

    Public Sub New(ByVal test As Integer)
        Me.test = test
    End Sub
End Structure
", index:=1, parameters:=OmitIfDefault_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructField_RequiredAccessibilityNever() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Private ReadOnly test As Integer

    Public Sub New(ByVal test As Integer)
        Me.test = test
    End Sub
End Structure
", index:=1, parameters:=Never_Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
        Public Async Function TestInitializeStructField_RequiredAccessibilityAlways() As Task
            Await TestInRegularAndScript1Async("
Structure S
    Public Sub New(ByVal [|test|] As Integer)
    End Sub
End Structure
", "
Structure S
    Private ReadOnly test As Integer

    Public Sub New(ByVal test As Integer)
        Me.test = test
    End Sub
End Structure
", index:=1, parameters:=Always_Warning)
        End Function

        Private ReadOnly Property OmitIfDefault_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption.Warning))
            End Get
        End Property

        Private ReadOnly Property Never_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.Never, NotificationOption.Warning))
            End Get
        End Property

        Private ReadOnly Property Always_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.Always, NotificationOption.Warning))
            End Get
        End Property
    End Class
End Namespace
