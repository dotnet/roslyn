' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InitializeParameter

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InitializeParameter
    <Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
    Partial Public Class InitializeMemberFromParameterTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInitializeMemberFromParameterCodeRefactoringProvider()
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestNotInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C

    private s As String

    public function M([||]s As String)

    end sub
end class")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingFields1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    public sub new([||]i as integer, j as integer, k as integer)
    end sub
end class",
"
class C
    Private ReadOnly i As Integer
    Private ReadOnly j As Integer
    Private ReadOnly k As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.i = i
        Me.j = j
        Me.k = k
    end sub
end class", index:=3)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingFields2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly i As Integer

    public sub new(i as integer, [||]j as integer, k as integer)
        Me.i = i
    end sub
end class",
"
class C
    Private ReadOnly i As Integer
    Private ReadOnly j As Integer
    Private ReadOnly k As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.i = i
        Me.j = j
        Me.k = k
    end sub
end class", index:=2)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingFields3() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly j As Integer

    public sub new([||]i as integer, j as integer, k as integer)
        Me.j = j
    end sub
end class",
"
class C
    Private ReadOnly i As Integer
    Private ReadOnly j As Integer
    Private ReadOnly k As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.i = i
        Me.j = j
        Me.k = k
    end sub
end class", index:=2)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingFields4() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly k As Integer

    public sub new([||]i as integer, j as integer, k as integer)
        Me.k = k
    end sub
end class",
"
class C
    Private ReadOnly i As Integer
    Private ReadOnly j As Integer
    Private ReadOnly k As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.i = i
        Me.j = j
        Me.k = k
    end sub
end class", index:=2)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingProperties1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    public sub new([||]i as integer, j as integer, k as integer)
    end sub
end class",
"
class C
    public sub new(i as integer, j as integer, k as integer)
        Me.I = i
        Me.J = j
        Me.K = k
    end sub

    Public ReadOnly Property I As Integer
    Public ReadOnly Property J As Integer
    Public ReadOnly Property K As Integer
end class", index:=2)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingProperties2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly i As Integer

    public sub new(i as integer, [||]j as integer, k as integer)
        Me.i = i
    end sub
end class",
"
class C
    Private ReadOnly i As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.i = i
        Me.J = j
        Me.K = k
    end sub

    Public ReadOnly Property J As Integer
    Public ReadOnly Property K As Integer
end class", index:=3)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingProperties3() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly j As Integer

    public sub new([||]i as integer, j as integer, k as integer)
        Me.j = j
    end sub
end class",
"
class C
    Private ReadOnly j As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.I = i
        Me.j = j
        Me.K = k
    end sub

    Public ReadOnly Property I As Integer
    Public ReadOnly Property K As Integer
end class", index:=3)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")>
        Public Async Function TestGenerateRemainingProperties4() As Task
            Await TestInRegularAndScript1Async(
"
class C
    Private ReadOnly k As Integer

    public sub new([||]i as integer, j as integer, k as integer)
        Me.k = k
    end sub
end class",
"
class C
    Private ReadOnly k As Integer

    public sub new(i as integer, j as integer, k as integer)
        Me.I = i
        Me.J = j
        Me.k = k
    end sub

    Public ReadOnly Property I As Integer
    Public ReadOnly Property J As Integer
end class", index:=3)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")>
        Public Async Function TestInitializeThrowingProperty1() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    Private ReadOnly property S As String
        get
            throw new NotImplementedException
        end get
    end property

    public sub new([||]s As String)

    end sub
end class",
"
imports System

class C
    Private ReadOnly property S As String
        get
        end get
    end property

    public sub new(s As String)
        Me.S = s
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")>
        Public Async Function TestInitializeThrowingProperty2() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    Private property S As String
        get
            throw new NotImplementedException
        end get
        set(value as S)
            throw new NotImplementedException
        end set
    end property

    public sub new([||]s As String)

    end sub
end class",
"
imports System

class C
    Private property S As String
        get
        end get
        set(value as S)
        end set
    end property

    public sub new(s As String)
        Me.S = s
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")>
        Public Async Function TestInitializeThrowingProperty3() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    Private ReadOnly property S As String
        get
            throw new InvalidOperationException
        end get
    end property

    public sub new([||]s As String)

    end sub
end class",
"
imports System

class C
    Private ReadOnly property S As String
        get
            throw new InvalidOperationException
        end get
    end property

    Public ReadOnly Property S1 As String

    public sub new(s As String)
        S1 = s
    end sub
end class")
        End Function

        Private ReadOnly Property OmitIfDefault_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption2.Warning))
            End Get
        End Property

        Private ReadOnly Property Never_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never, NotificationOption2.Warning))
            End Get
        End Property

        Private ReadOnly Property Always_Warning As TestParameters
            Get
                Return New TestParameters(options:=[Option](CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always, NotificationOption2.Warning))
            End Get
        End Property
    End Class
End Namespace
