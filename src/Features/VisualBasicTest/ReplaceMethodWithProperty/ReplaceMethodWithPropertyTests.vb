' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ReplaceMethodWithProperty
    Public Class ReplaceMethodWithPropertyTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New ReplaceMethodWithPropertyCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithGetName() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17368")>
        Public Async Function TestMissingParameterList() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo as integer
    End function
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutGetName() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]Goo() as integer
    End function
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutBody() As Task
            Await TestInRegularAndScriptAsync(
"mustinherit class C
    MustOverride function [||]GetGoo() as integer
End class",
"mustinherit class C
    MustOverride ReadOnly Property Goo as integer
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithModifiers() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public shared function [||]GetGoo() as integer
    End function
End class",
"class C
    public shared ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithAttributes() As Task
            Await TestInRegularAndScriptAsync(
"class C
    <A> function [||]GetGoo() as integer
    End function
End class",
"class C
    <A>
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithTrivia_1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    ' Goo
    function [||]GetGoo() as integer
    End function
End class",
"class C
    ' Goo
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    function [||]GetGoo() as integer
    End function
#End if
End class",
"class C
#if true
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
#End if
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod2() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    function [||]GetGoo() as integer
    End function

    sub SetGoo(i as integer)
    end sub
#End if
End class",
"class C
#if true
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property

    sub SetGoo(i as integer)
    end sub
#End if
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod3() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    function [||]GetGoo() as integer
    End function

    sub SetGoo(i as integer)
    end sub
#End if
End class",
"class C
#if true
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
#End if
End class", index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod4() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    sub SetGoo(i as integer)
    end sub

    function [||]GetGoo() as integer
    End function
#End if
End class",
"class C
#if true
    sub SetGoo(i as integer)
    end sub

    ReadOnly Property Goo as integer
        Get
        End Get
    End Property
#End if
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod5() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    sub SetGoo(i as integer)
    end sub

    function [||]GetGoo() as integer
    End function
#End if
End class",
"class C

#if true

    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
#End if
End class", index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithTrivia_2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    ' Goo
    function [||]GetGoo() as integer
    End function
    ' SetGoo
    sub SetGoo(i as integer)
    End sub
End class",
"class C
    ' Goo
    ' SetGoo
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_2() As Task
            Await TestInRegularAndScriptAsync(
"interface I
    function GetGoo() as integer
End interface
class C
    implements I
    function [||]GetGoo() as integer implements I.GetGoo
    End function
End class",
"interface I
    ReadOnly Property Goo as integer
End interface
class C
    implements I
    ReadOnly Property Goo as integer implements I.Goo
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_3() As Task
            Await TestInRegularAndScriptAsync(
"interface I
    function [||]GetGoo() as integer
End interface
class C
    implements I
    function GetGoo() as integer implements I.GetGoo
    End function
End class",
"interface I
    ReadOnly Property Goo as integer
End interface
class C
    implements I
    ReadOnly Property Goo as integer implements I.Goo
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInAttribute() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    <At[||]tr> function GetGoo() as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function GetGoo() as integer

[||]    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestSubMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub [||]GetGoo()
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestAsyncMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    async function [||]GetGoo() as Task
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestGenericMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function [||]GetGoo(of T)() as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExtensionMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    <System.Runtime.CompilerServices.Extension> function [||]GetGoo(i as integer) as integer
    End function
End module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithParameters_1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function [||]GetGoo(i as integer) as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceNotInMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub Bar()
        dim x = GetGoo()
    End sub
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property

    sub Bar()
        dim x = Goo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceMemberAccessInvocation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub Bar()
        dim x = me.GetGoo()
    End sub
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property

    sub Bar()
        dim x = me.Goo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceBindingMemberInvocation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub Bar()
        dim x as C
        dim v = x?.GetGoo()
    End sub
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property

    sub Bar()
        dim x as C
        dim v = x?.Goo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceInMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
        return GetGoo()
    End function
End class",
"class C
    ReadOnly Property Goo as integer
        Get
            return Goo
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestOverride() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public overridable function [||]GetGoo() as integer
    End function
End class
class D
    inherits C
    public overrides function GetGoo() as integer
    End function
End class",
"class C
    public overridable ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    public overrides ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReference_NonInvoked() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub Bar()
        dim i = GetGoo
    End sub
End class",
"class C
    ReadOnly Property Goo as integer
        Get
        End Get
    End Property

    sub Bar()
        dim i = Goo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub SetGoo(i as integer)
    End sub
End class",
"class C
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSetReference_NonInvoked() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
class C
    function [||]GetGoo() as integer
    End function
    sub SetGoo(i as integer)
    End sub
    sub Bar()
        dim i as Action(of integer) = addressof SetGoo
    End sub
End class",
"Imports System
class C
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property

    sub Bar()
        dim i as Action(of integer) = addressof {|Conflict:Goo|}
    End sub
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetterAccessibility() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public function [||]GetGoo() as integer
    End function
    private sub SetGoo(i as integer)
    End sub
End class",
"class C
    public Property Goo as integer
        Get
        End Get
        Private Set(i as integer)
        End Set
    End Property
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_GetInSetReference() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub SetGoo(i as integer)
    End sub
    sub Bar()
        SetGoo(GetGoo() + 1)
    End sub
End class",
"class C
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property

    sub Bar()
        Goo = Goo + 1    End sub
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetReferenceInSetter() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetGoo() as integer
    End function
    sub SetGoo(i as integer)
        SetGoo(i - 1)
    End sub
End class",
"class C
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
            Goo = i - 1
        End Set
    End Property
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    protected overridable function [||]GetGoo() as integer
    End function
End class
class D
    inherits C
    protected overrides function GetGoo() as integer
    End function
End class",
"class C
    protected overridable ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    protected overrides ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    protected overridable function [||]GetGoo() as integer
    End function
End class
class D
    inherits C
    protected overrides function GetGoo() as integer
        return mybase.GetGoo()
    End function
End class",
"class C
    protected overridable ReadOnly Property Goo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    protected overrides ReadOnly Property Goo as integer
        Get
            return mybase.Goo
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestWithPartialClasses() As Task
            Await TestInRegularAndScriptAsync(
"partial class C
    function [||]GetGoo() as integer
    End function
End class
partial class C
    sub SetGoo(i as integer)
    End sub
End class",
"partial class C
    Property Goo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
End class
partial class C
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14327")>
        Public Async Function TestUpdateChainedGet1() As Task
            Await TestInRegularAndScriptAsync(
"
public class Goo
    public sub Goo()
        dim v = GetValue().GetValue()
    end sub

    Public Function [||]GetValue() As Goo 
    End Function
end class",
"
public class Goo
    public sub Goo()
        dim v = Value.Value
    end sub

    Public ReadOnly Property Value As Goo
        Get
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)>
        Public Async Function TestIndentation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    Public Function [||]GetProp() As Integer
        dim count = 0
        for each x in y
            count = count + z
        next
        return  count
    End Function
end class",
"class C
    Public ReadOnly Property Prop As Integer
        Get
            dim count = 0
            for each x in y
                count = count + z
            next
            return count
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInterfaceImplementation() As Task
            Await TestInRegularAndScriptAsync(
"Interface IGoo
    Function [||]GetGoo() As Integer
End Interface

Class C
    Implements IGoo

    Private _Goo As Integer

    Public Function GetGoo() As Integer Implements IGoo.GetGoo
        Return _Goo
    End Function
End Class",
"Interface IGoo
    ReadOnly Property Goo As Integer
End Interface

Class C
    Implements IGoo

    Private _Goo As Integer

    Public ReadOnly Property Goo As Integer Implements IGoo.Goo
        Get
            Return _Goo
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")>
        Public Async Function TestSystemObjectMetadataOverride() As Task
            Await TestMissingAsync(
"class C
    public overrides function [||]ToString() as string
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")>
        Public Async Function TestMetadataOverride() As Task
            Await TestInRegularAndScriptAsync(
"class C
    inherits system.type

    public overrides function [||]GetArrayRank() as integer
    End function
End class",
"class C
    inherits system.type

    public overrides ReadOnly Property {|Warning:ArrayRank|} as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestAtStartOfMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [||]Function GetGoo() As Integer
    End Function
end class",
"class C
    ReadOnly Property Goo As Integer
        Get
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_OnSameLine() As Task
            Await TestInRegularAndScriptAsync(
"class C
[||]    Function GetGoo() As Integer
    End Function
end class",
"class C
    ReadOnly Property Goo As Integer
        Get
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_OnPreviousLine() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [||]
    Function GetGoo() As Integer
    End Function
end class",
"class C

    ReadOnly Property Goo As Integer
        Get
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_NotMultipleLinesPrior() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    [||]

    Function GetGoo() As Integer
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_NotBeforeAttributes() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [||]<A>
    Function GetGoo() As Integer
    End Function
end class",
"class C
    <A>
    ReadOnly Property Goo As Integer
        Get
        End Get
    End Property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_NotBeforeComments() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    [||] ''' <summary/>
    Function GetGoo() As Integer
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestBeforeStartOfMethod_NotInComment() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    ''' [||]<summary/>
    Function GetGoo() As Integer
    End Function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/42699")>
        Public Async Function TestSameNameMemberAsProperty() As Task
            Await TestInRegularAndScriptAsync(
"class C
    Public Goo as integer
    function [||]GetGoo() as integer
    End function
End class",
"class C
    Public Goo as integer
    ReadOnly Property Goo1 as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/42699")>
        Public Async Function TestSameNameMemberAsPropertyDifferentCase() As Task
            Await TestInRegularAndScriptAsync(
"class C
    Public goo as integer
    function [||]GetGoo() as integer
    End function
End class",
"class C
    Public goo as integer
    ReadOnly Property Goo1 as integer
        Get
        End Get
    End Property
End class")
        End Function
    End Class
End Namespace
