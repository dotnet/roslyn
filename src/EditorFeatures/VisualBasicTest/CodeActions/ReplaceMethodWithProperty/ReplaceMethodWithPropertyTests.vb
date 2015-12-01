' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ReplaceMethodWithProperty
    Public Class ReplaceMethodWithPropertyTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New ReplaceMethodWithPropertyCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithGetName() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutGetName() As Task
            Await TestAsync(
NewLines("class C \n function [||]Foo() as integer \n End function \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithoutBody() As Task
            Await TestAsync(
NewLines("mustinherit class C \n MustOverride function [||]GetFoo() as integer \n End class"),
NewLines("mustinherit class C \n MustOverride ReadOnly Property Foo as integer \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithModifiers() As Task
            Await TestAsync(
NewLines("class C \n public shared function [||]GetFoo() as integer \n End function \n End class"),
NewLines("class C \n public shared ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithAttributes() As Task
            Await TestAsync(
NewLines("class C \n <A>function [||]GetFoo() as integer \n End function \n End class"),
NewLines("class C \n <A>ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithTrivia_1() As Task
            Await TestAsync(
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
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestIfDefMethod() As Task
            Await TestAsync(
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
            Await TestAsync(
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
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_2() As Task
            Await TestAsync(
NewLines("interface I \n function GetFoo() as integer \n End interface \n class C \n implements I \n function [||]GetFoo() as integer implements I.GetFoo \n End function \n End class"),
NewLines("interface I \n ReadOnly Property Foo as integer \n End interface \n class C \n implements I \n ReadOnly Property Foo as integer implements I.Foo \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExplicitInterfaceMethod_3() As Task
            Await TestAsync(
NewLines("interface I \n function [||]GetFoo() as integer \n End interface \n class C \n implements I \n function GetFoo() as integer implements I.GetFoo \n End function \n End class"),
NewLines("interface I \n ReadOnly Property Foo as integer \n End interface \n class C \n implements I \n ReadOnly Property Foo as integer implements I.Foo \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInAttribute() As Task
            Await TestMissingAsync(
NewLines("class C \n <At[||]tr>function GetFoo() as integer \n End function \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestInMethod() As Task
            Await TestMissingAsync(
NewLines("class C \n function GetFoo() as integer \n [||] \n End function \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestSubMethod() As Task
            Await TestMissingAsync(
NewLines("class C \n sub [||]GetFoo() \n End sub \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestAsyncMethod() As Task
            Await TestMissingAsync(
NewLines("class C \n async function [||]GetFoo() as Task \n End function \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestGenericMethod() As Task
            Await TestMissingAsync(
NewLines("class C \n function [||]GetFoo(of T)() as integer \n End function \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestExtensionMethod() As Task
            Await TestMissingAsync(
NewLines("module C \n <System.Runtime.CompilerServices.Extension>function [||]GetFoo(i as integer) as integer \n End function \n End module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestMethodWithParameters_1() As Task
            Await TestMissingAsync(
NewLines("class C \n function [||]GetFoo(i as integer) as integer \n End function \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceNotInMethod() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub Bar() \n dim x = GetFoo() \n End sub \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n sub Bar() \n dim x = Foo \n End sub \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceMemberAccessInvocation() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub Bar() \n dim x = me.GetFoo() \n End sub \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n sub Bar() \n dim x = me.Foo \n End sub \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceBindingMemberInvocation() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub Bar() \n dim x as C \n dim v = x?.GetFoo() \n End sub \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n sub Bar() \n dim x as C \n dim v = x?.Foo \n End sub \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReferenceInMethod() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n return GetFoo() \n End function \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n return Foo \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestOverride() As Task
            Await TestAsync(
NewLines("class C \n public overridable function [||]GetFoo() as integer \n End function \n End class \n class D \n inherits C \n public overrides function GetFoo() as integer \n End function \n End class"),
NewLines("class C \n public overridable ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class \n class D \n inherits C \n public overrides ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetReference_NonInvoked() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub Bar() \n dim i = GetFoo \n End sub \n End class"),
NewLines("class C \n ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n sub Bar() \n dim i = Foo \n End sub \n End class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub SetFoo(i as integer) \n End sub \n End class"),
NewLines("class C \n Property Foo as integer \n Get \n End Get \n Set(i as integer) \n End Set \n End Property \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSetReference_NonInvoked() As Task
            Await TestAsync(
NewLines("Imports System \n class C \n function [||]GetFoo() as integer \n End function \n sub SetFoo(i as integer) \n End sub \n sub Bar() \n dim i as Action(of integer) = addressof SetFoo \n End sub \n End class"),
NewLines("Imports System \n class C \n Property Foo as integer \n Get \n End Get \n Set(i as integer) \n End Set \n End Property \n sub Bar() \n dim i as Action(of integer) = addressof {|Conflict:Foo|} \n End sub \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetterAccessibility() As Task
            Await TestAsync(
NewLines("class C \n public function [||]GetFoo() as integer \n End function \n private sub SetFoo(i as integer) \n End sub \n End class"),
NewLines("class C \n public Property Foo as integer \n Get End Get \n Private Set(i as integer) \n End Set \n End Property \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_GetInSetReference() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub SetFoo(i as integer) \n End sub \n sub Bar() \n SetFoo(GetFoo() + 1) \n End sub \n End class"),
NewLines("class C \n Property Foo as integer \n Get \n End Get \n Set(i as integer) \n End Set \n End Property \n sub Bar() \n Foo = Foo + 1 \n End sub \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestUpdateGetSet_SetReferenceInSetter() As Task
            Await TestAsync(
NewLines("class C \n function [||]GetFoo() as integer \n End function \n sub SetFoo(i as integer) \n SetFoo(i - 1) \n End sub \n End class"),
NewLines("class C \n Property Foo as integer \n Get \n End Get \n Set(i as integer) \n Foo = i - 1 \n End Set \n End Property \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_1() As Task
            Await TestAsync(
NewLines("class C \n protected overridable function [||]GetFoo() as integer \n End function \n End class \n class D \n inherits C \n protected overrides function GetFoo() as integer \n End function \n End class"),
NewLines("class C \n protected overridable ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class \n class D \n inherits C \n protected overrides ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class"),
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestVirtualGetWithOverride_2() As Task
            Await TestAsync(
NewLines("class C \n protected overridable function [||]GetFoo() as integer \n End function \n End class \n class D \n inherits C \n protected overrides function GetFoo() as integer \n return mybase.GetFoo() \n End function \n End class"),
NewLines("class C \n protected overridable ReadOnly Property Foo as integer \n Get \n End Get \n End Property \n End class \n class D \n inherits C \n protected overrides ReadOnly Property Foo as integer \n Get \n return mybase.Foo \n End Get \n End Property \n End class"),
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)>
        Public Async Function TestWithPartialClasses() As Task
            Await TestAsync(
NewLines("partial class C \n function [||]GetFoo() as integer \n End function \n End class \n partial class C \n sub SetFoo(i as integer) \n End sub \n End class"),
NewLines("partial class C \n Property Foo as integer \n Get \n End Get \n Set(i as integer) \n End Set \n End Property \n End class \n partial class C \n End class"),
index:=1)
        End Function
    End Class
End Namespace