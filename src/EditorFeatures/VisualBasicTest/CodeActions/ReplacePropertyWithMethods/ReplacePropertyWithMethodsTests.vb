Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ReplacePropertyWithMethods
    Public Class ReplacePropertyWithMethodsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New ReplacePropertyWithMethodsCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestGetWithBody() As Task
            Await TestAsync(
"class C
    readonly property [||]Prop as integer
        get 
            return 0
        end get
    end property
end class",
"class C
    Public Function GetProp() As Integer
        return 0
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestPrivateProperty() As Task
            Await TestAsync(
"class C
    private readonly property [||]Prop as integer
        get
            return 0
        end get
    end property
end class",
"class C
    Private Function GetProp() As Integer
        return 0
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAnonyousType1() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer 
        get
            return 0
        end get
    end property
    public sub M()
        dim v = new with { .P = me.Prop }
    end sub
end class",
"class C
    Public Function GetProp() As Integer
        return 0
    End Function
    public sub M()
        dim v = new with { .P = me.GetProp() }
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAnonyousType2() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer
        get
            return 0
        end get
    end property
    public sub M()
        dim v = new with { me.Prop }
    end sub
end class",
"class C
    Public Function GetProp() As Integer
        return 0
    End Function
    public sub M()
        dim v = new with { .Prop = me.GetProp() }
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestPassedToRef1() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer
        get
            return 0
        end get
    end property
    public sub RefM(byref i as integer)
    end sub
    public sub M()
        RefM(me.Prop)
    end sub
end class",
"class C
    Public Function GetProp() As Integer
        return 0
    End Function
    public sub RefM(byref i as integer)
    end sub
    public sub M()
        RefM(me.{|Conflict:GetProp|}())
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestUsedInAttribute1() As Task
            Await TestAsync(
"
imports System

class CAttribute 
    inherits Attribute

    public readonly property [||]Prop as integer
        get
            return 0
        end get
    end property
end class

<C(Prop:=1)>
class D
end class
",
"
imports System

class CAttribute 
    inherits Attribute

    Public Function GetProp() As Integer
        return 0
    End Function
end class

<C({|Conflict:Prop|}:=1)>
class D
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestSetWithBody1() As Task
            Await TestAsync(
"class C
    writeonly property [||]Prop as integer 
        set
            dim v = value
        end set
    end property
end class",
"class C
    Public Sub SetProp(Value As Integer)
        dim v = value
    End Sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestSetWithBody2() As Task
            Await TestAsync(
"class C
    writeonly property [||]Prop as integer 
        set(val as integer)
            dim v = val
        end set
    end property
end class",
"class C
    Public Sub SetProp(val As Integer)
        dim v = val
    End Sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestSetReference1() As Task
            Await TestAsync(
"class C
    writeonly property [||]Prop as integer 
        set(val as integer)
            dim v = val
        end set
    end property
    sub M()
        me.Prop = 1
    end sub
end class",
"class C
    Public Sub SetProp(val As Integer)
        dim v = val
    End Sub
    sub M() 
        me.SetProp(1)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestGetterAndSetter() As Task
            Await TestAsync(
"class C
    property [||]Prop as integer
        get
            return 0
        end get
        set
            dim v = value
        end set
    end property
end class",
"class C
    Public Function GetProp() As Integer
        return 0
    End Function
    Public Sub SetProp(Value As Integer)
        dim v = value
    End Sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestRecursiveGet() As Task
            Await TestAsync(
"class C
    readonly property [||]Prop as integer
        get
            return me.Prop + 1
        end get
    end property
end class",
"class C
    Public Function GetProp() As Integer
        return me.GetProp() + 1
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestRecursiveSet() As Task
            Await TestAsync(
"class C
    writeonly property [||]Prop as integer
        set
            me.Prop = value + 1
        end set
    end property
end class",
"class C
    Public Sub SetProp(Value As Integer)
        me.SetProp(value + 1)
    End Sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAbstractProperty() As Task
            Await TestAsync(
"class C
    public readonly mustoverride property [||]Prop as integer
    public sub M()
        dim v = me.Prop
    end sub
end class",
"class C
    Public MustOverride Function GetProp() As Integer
    public sub M()
        dim v = me.GetProp()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestVirtualProperty() As Task
            Await TestAsync(
"class C
    public readonly overridable property [||]Prop as integer
        get
            return 0
        end get
    end property

    public sub M()
        dim v = me.Prop
    end sub
end class",
"class C
    Public Overridable Function GetProp() As Integer
        return 0
    End Function
    public sub M()
        dim v = me.GetProp()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceProperty1() As Task
            Await TestAsync(
"interface I
    readonly property [||]Prop as integer
end interface",
"interface I
    Function GetProp() As Integer
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceProperty2() As Task
            Await TestAsync(
"interface I
    writeonly property [||]Prop as integer
end interface",
"interface I
    Sub SetProp(Value As Integer)
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceProperty3() As Task
            Await TestAsync(
"interface I
    property [||]Prop as integer
end interface",
"interface I
    Function GetProp() As Integer
    Sub SetProp(Value As Integer)
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAutoProperty1() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer
end class",
"class C
    Private _Prop As Integer
    Public Function GetProp() As Integer
        Return _Prop
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAutoProperty2() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer
    public sub new()
        me.Prop = 1
    end sub
end class",
"class C
    Private _Prop As Integer
    Public Function GetProp() As Integer
        Return _Prop
    End Function
    public sub new()
        me._Prop = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAutoProperty4() As Task
            Await TestAsync(
"class C
    public readonly property [||]Prop as integer = 1
end class",
"class C
    Private _Prop As Integer = 1
    Public Function GetProp() As Integer
        Return _Prop
    End Function
end class")
        End Function
    End Class
End Namespace