' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ReplaceMethodWithProperty
    Public Class ReplaceMethodWithPropertyTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New ReplaceMethodWithPropertyCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithGetName() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <WorkItem(17368, "https://github.com/dotnet/roslyn/issues/17368")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMissingParameterList() As Task
            Await TestInRegularAndScript1Async(
"class C
    function [||]GetFoo as integer
    End function
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutGetName() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]Foo() as integer
    End function
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutBody() As Task
            Await TestInRegularAndScriptAsync(
"mustinherit class C
    MustOverride function [||]GetFoo() as integer
End class",
"mustinherit class C
    MustOverride ReadOnly Property Foo as integer
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithModifiers() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public shared function [||]GetFoo() as integer
    End function
End class",
"class C
    public shared ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithAttributes() As Task
            Await TestInRegularAndScriptAsync(
"class C
    <A> function [||]GetFoo() as integer
    End function
End class",
"class C
    <A> ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithTrivia_1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    ' Foo
    function [||]GetFoo() as integer
    End function
End class",
"class C
    ' Foo
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class",
ignoreTrivia:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
#if true
    function [||]GetFoo() as integer
    End function
#End if
End class",
"class C
#if true
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
#End if
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithTrivia_2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    ' Foo
    function [||]GetFoo() as integer
    End function
    ' SetFoo
    sub SetFoo(i as integer)
    End sub
End class",
"class C
    ' Foo
    ' SetFoo
    Property Foo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
End class",
index:=1,
ignoreTrivia:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_2() As Task
            Await TestInRegularAndScriptAsync(
"interface I
    function GetFoo() as integer
End interface
class C
    implements I
    function [||]GetFoo() as integer implements I.GetFoo
    End function
End class",
"interface I
    ReadOnly Property Foo as integer
End interface
class C
    implements I
    ReadOnly Property Foo as integer implements I.Foo
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_3() As Task
            Await TestInRegularAndScriptAsync(
"interface I
    function [||]GetFoo() as integer
End interface
class C
    implements I
    function GetFoo() as integer implements I.GetFoo
    End function
End class",
"interface I
    ReadOnly Property Foo as integer
End interface
class C
    implements I
    ReadOnly Property Foo as integer implements I.Foo
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInAttribute() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    <At[||]tr> function GetFoo() as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function GetFoo() as integer

[||]    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestSubMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub [||]GetFoo()
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestAsyncMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    async function [||]GetFoo() as Task
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestGenericMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function [||]GetFoo(of T)() as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExtensionMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    <System.Runtime.CompilerServices.Extension> function [||]GetFoo(i as integer) as integer
    End function
End module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithParameters_1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    function [||]GetFoo(i as integer) as integer
    End function
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceNotInMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub Bar()
        dim x = GetFoo()
    End sub
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
    sub Bar()
        dim x = Foo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceMemberAccessInvocation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub Bar()
        dim x = me.GetFoo()
    End sub
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
    sub Bar()
        dim x = me.Foo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceBindingMemberInvocation() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub Bar()
        dim x as C
        dim v = x?.GetFoo()
    End sub
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
    sub Bar()
        dim x as C
        dim v = x?.Foo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceInMethod() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
        return GetFoo()
    End function
End class",
"class C
    ReadOnly Property Foo as integer
        Get
            return Foo
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestOverride() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public overridable function [||]GetFoo() as integer
    End function
End class
class D
    inherits C
    public overrides function GetFoo() as integer
    End function
End class",
"class C
    public overridable ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    public overrides ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReference_NonInvoked() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub Bar()
        dim i = GetFoo
    End sub
End class",
"class C
    ReadOnly Property Foo as integer
        Get
        End Get
    End Property
    sub Bar()
        dim i = Foo
    End sub
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub SetFoo(i as integer)
    End sub
End class",
"class C
    Property Foo as integer
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
    function [||]GetFoo() as integer
    End function
    sub SetFoo(i as integer)
    End sub
    sub Bar()
        dim i as Action(of integer) = addressof SetFoo
    End sub
End class",
"Imports System
class C
    Property Foo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
    sub Bar()
        dim i as Action(of integer) = addressof {|Conflict:Foo|}
    End sub
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetterAccessibility() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public function [||]GetFoo() as integer
    End function
    private sub SetFoo(i as integer)
    End sub
End class",
"class C
    public Property Foo as integer
        Get End Get 
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
    function [||]GetFoo() as integer
    End function
    sub SetFoo(i as integer)
    End sub
    sub Bar()
        SetFoo(GetFoo() + 1)
    End sub
End class",
"class C
    Property Foo as integer
        Get
        End Get
        Set(i as integer)
        End Set
    End Property
    sub Bar()
        Foo = Foo + 1
    End sub
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetReferenceInSetter() As Task
            Await TestInRegularAndScriptAsync(
"class C
    function [||]GetFoo() as integer
    End function
    sub SetFoo(i as integer)
        SetFoo(i - 1)
    End sub
End class",
"class C
    Property Foo as integer
        Get
        End Get
        Set(i as integer)
            Foo = i - 1
        End Set
    End Property
End class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    protected overridable function [||]GetFoo() as integer
    End function
End class
class D
    inherits C
    protected overrides function GetFoo() as integer
    End function
End class",
"class C
    protected overridable ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    protected overrides ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    protected overridable function [||]GetFoo() as integer
    End function
End class
class D
    inherits C
    protected overrides function GetFoo() as integer
        return mybase.GetFoo()
    End function
End class",
"class C
    protected overridable ReadOnly Property Foo as integer
        Get
        End Get
    End Property
End class
class D
    inherits C
    protected overrides ReadOnly Property Foo as integer
        Get
            return mybase.Foo
        End Get
    End Property
End class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestWithPartialClasses() As Task
            Await TestInRegularAndScriptAsync(
"partial class C
    function [||]GetFoo() as integer
    End function
End class
partial class C
    sub SetFoo(i as integer)
    End sub
End class",
"partial class C
    Property Foo as integer
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

        <WorkItem(14327, "https://github.com/dotnet/roslyn/issues/14327")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateChainedGet1() As Task
            Await TestInRegularAndScriptAsync(
"
public class Foo
    public sub Foo()
        dim v = GetValue().GetValue()
    end sub

    Public Function [||]GetValue() As Foo 
    End Function
end class",
"
public class Foo
    public sub Foo()
        dim v = Value.Value
    end sub

    Public ReadOnly Property Value As Foo
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
end class", ignoreTrivia:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInterfaceImplementation() As Task
            Await TestInRegularAndScriptAsync(
"Interface IFoo
    Function [||]GetFoo() As Integer
End Interface

Class C
    Implements IFoo

    Private _Foo As Integer

    Public Function GetFoo() As Integer Implements IFoo.GetFoo
        Return _Foo
    End Function
End Class",
"Interface IFoo
    ReadOnly Property Foo As Integer
End Interface

Class C
    Implements IFoo

    Private _Foo As Integer

    Public ReadOnly Property Foo As Integer Implements IFoo.Foo
        Get
            Return _Foo
        End Get
    End Property
End Class")
        End Function
    End Class
End Namespace
