Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ReplacePropertyWithMethods
    Public Class ReplacePropertyWithMethodsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New ReplacePropertyWithMethodsCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestGetWithBody() As Task
            Await TestInRegularAndScriptAsync(
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
        Public Async Function TestGetWithBodyLineContinuation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    readonly property [||]Prop as integer
        get 
            return _
                0
        end get
    end property
end class",
"class C
    Public Function GetProp() As Integer
        return _
0
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestGetWithBodyCommentsAfterLineContinuation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    readonly property [||]Prop as integer
        get 
            return _ ' Test
                0
        end get
    end property
end class",
"class C
    Public Function GetProp() As Integer
        return _ ' Test
0
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestIndentation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    readonly property [||]Prop As Integer
        get 
            dim count = 0
            for each x in y
                count = count + z
            next
            return count
        end get
    end property
end class",
"class C
    Public Function GetProp() As Integer
        dim count = 0
        for each x in y
            count = count + z
        next
        return count
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestPrivateProperty() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
        dim v = new with { .P = me.GetProp()}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAnonyousType2() As Task
            Await TestInRegularAndScriptAsync(
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
        dim v = new with {
        .Prop = me.GetProp()}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestPassedToRef1() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestSetWithBody1() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
        me.SetProp(1
) end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestGetterAndSetter() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
"class C
    writeonly property [||]Prop as integer
        set
            me.Prop = value + 1
        end set
    end property
end class",
"class C
    Public Sub SetProp(Value As Integer)
        me.SetProp(value + 1
)
    End Sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestAbstractProperty() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
"interface I
    readonly property [||]Prop as integer
end interface",
"interface I
    Function GetProp() As Integer
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceProperty2() As Task
            Await TestInRegularAndScriptAsync(
"interface I
    writeonly property [||]Prop as integer
end interface",
"interface I
    Sub SetProp(Value As Integer)
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceProperty3() As Task
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
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

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment1() As Task
            Await TestInRegularAndScriptAsync(
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <value>
    '''     An that provides access to the language service for the active configured project.
    ''' </value>
    ReadOnly Property [||]ActiveProjectContext As Object
End Interface",
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <returns>
    '''     An that provides access to the language service for the active configured project.
    ''' </returns>
    Function GetActiveProjectContext() As Object
End Interface")
        End Function

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment2() As Task
            Await TestInRegularAndScriptAsync(
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Sets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <value>
    '''     An that provides access to the language service for the active configured project.
    ''' </value>
    WriteOnly Property [||]ActiveProjectContext As Object
End Interface",
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Sets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <param name=""Value"">
    '''     An that provides access to the language service for the active configured project.
    ''' </param>
    Sub SetActiveProjectContext(Value As Object)
End Interface")
        End Function

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment3() As Task
            Await TestInRegularAndScriptAsync(
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets or sets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <value>
    '''     An that provides access to the language service for the active configured project.
    ''' </value>
    Property [||]ActiveProjectContext As Object
End Interface",
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets or sets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <returns>
    '''     An that provides access to the language service for the active configured project.
    ''' </returns>
    Function GetActiveProjectContext() As Object
    ''' <summary>
    '''     Gets or sets the active workspace project context that provides access to the language service for the active configured project.
    ''' </summary>
    ''' <param name=""Value"">
    '''     An that provides access to the language service for the active configured project.
    ''' </param>
    Sub SetActiveProjectContext(Value As Object)
End Interface")
        End Function

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment4() As Task
            Await TestInRegularAndScriptAsync(
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Sets <see cref=""ActiveProjectContext""/>.
    ''' </summary>
    ''' <seealso cref=""ActiveProjectContext""/>
    WriteOnly Property [||]ActiveProjectContext As Object
End Interface
Structure AStruct
    ''' <seealso cref=""ILanguageServiceHost.ActiveProjectContext""/>
    Private X As Integer
End Structure",
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Sets <see cref=""SetActiveProjectContext(Object)""/>.
    ''' </summary>
    ''' <seealso cref=""SetActiveProjectContext(Object)""/>
    Sub SetActiveProjectContext(Value As Object)
End Interface
Structure AStruct
    ''' <seealso cref=""ILanguageServiceHost.SetActiveProjectContext(Object)""/>
    Private X As Integer
End Structure")
        End Function

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment5() As Task
            Await TestInRegularAndScriptAsync(
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets or sets <see cref=""ActiveProjectContext""/>.
    ''' </summary>
    ''' <seealso cref=""ActiveProjectContext""/>
    Property [||]ActiveProjectContext As Object
End Interface
Structure AStruct
    ''' <seealso cref=""ILanguageServiceHost.ActiveProjectContext""/>
    Private X As Integer
End Structure",
"Interface ILanguageServiceHost
    ''' <summary>
    '''     Gets or sets <see cref=""GetActiveProjectContext()""/>.
    ''' </summary>
    ''' <seealso cref=""GetActiveProjectContext()""/>
    Function GetActiveProjectContext() As Object
    ''' <summary>
    '''     Gets or sets <see cref=""GetActiveProjectContext()""/>.
    ''' </summary>
    ''' <seealso cref=""GetActiveProjectContext()""/>
    Sub SetActiveProjectContext(Value As Object)
End Interface
Structure AStruct
    ''' <seealso cref=""ILanguageServiceHost.GetActiveProjectContext()""/>
    Private X As Integer
End Structure")
        End Function

        <WorkItem(18234, "https://github.com/dotnet/roslyn/issues/18234")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18261"), Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestDocumentationComment6() As Task
            Await TestInRegularAndScriptAsync(
"Interface ISomeInterface(Of T)
    ''' <seealso cref=""Context""/>
    WriteOnly Property [||]Context As ISomeInterface(Of T)
End Interface
Structure AStruct
    ''' <seealso cref=""ISomeInterface(Of T).Context""/>
    Private X As Integer
End Structure",
"Interface ISomeInterface(Of T)
    ''' <seealso cref=""SetContext(ISomeInterface(Of T))""/>
    Sub SetContext(Value As ISomeInterface(Of T))
End Interface
Structure AStruct
    ''' <seealso cref=""ISomeInterface(Of T).SetContext(ISomeInterface(Of T))""/>
    Private X As Integer
End Structure")
        End Function

        <WorkItem(440371, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/440371")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestInterfaceReplacement1() As Task
            Await TestInRegularAndScriptAsync(
"Interface IGoo
    Property [||]Goo As Integer
End Interface

Class C
    Implements IGoo

    Public Property Goo As Integer Implements IGoo.Goo
End Class",
"Interface IGoo
    Function GetGoo() As Integer
    Sub SetGoo(Value As Integer)
End Interface

Class C
    Implements IGoo

    Private _Goo As Integer

    Public Function GetGoo() As Integer Implements IGoo.GetGoo
        Return _Goo
    End Function

    Public Sub SetGoo(AutoPropertyValue As Integer) Implements IGoo.SetGoo
        _Goo = AutoPropertyValue
    End Sub
End Class")
        End Function
    End Class
End Namespace
