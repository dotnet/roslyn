' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

Namespace Tests
    Public Class OverrideCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New OverrideCompletionProvider(TestWaitIndicator.Default)
        End Function

#Region "CompletionItem tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOfferedBaseClassMember() As Task
            Dim text = <a>MustInherit Class Base
    Public MustOverride Sub Foo()
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Foo()
    End Sub
End Class

Class SomeClass
    Inherits Derived
    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "Foo()", "Sub Base.Foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIntermediateClassOverriddenMember() As Task
            Dim text = <a>MustInherit Class Base
    Public MustOverride Sub Foo()
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Foo()
    End Sub
End Class

Class SomeClass
    Inherits Derived
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Foo()", "Sub Derived.Foo()")
        End Function

        <WorkItem(543807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543807")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestHideFinalize() As Task
            Dim text = <a>Class foo
    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "Finalize()")
        End Function

        <WorkItem(543807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543807")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestShowShadowingFinalize() As Task
            Dim text = <a>Class foo
    Overridable Shadows Sub Finalize()
    End Sub
End Class

Class bar
    Inherits foo

    overrides $$
End class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo.Finalize()")
        End Function

        <WorkItem(543806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543806")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestShowObjectOverrides() As Task
            Dim text = <a>Class foo
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Equals(obj As Object)")
            Await VerifyItemExistsAsync(text.Value, "ToString()")
            Await VerifyItemExistsAsync(text.Value, "GetHashCode()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInheritedOverridableSub() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub foo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInheritedOverridableFunction() As Task
            Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInheritedMustOverrideFunction() As Task
            Dim text = <a>Public Class a
    Public MustOverride Sub foo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchSub() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub foo()
    End Sub

    Public Overridable Function bar() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides Sub $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo()")
            Await VerifyItemIsAbsentAsync(text.Value, "bar()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchFunction() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub foo()
    End Sub

    Public Overridable Function bar() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides Function $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "bar()")
            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDontFilterIfNothingMatchesReturnTypeVoidness() As Task
            Dim text = <a>MustInherit Class Base
    MustOverride Function Foo() As String
    Protected NotOverridable Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class

Class Derived
    Inherits Base
    Overrides Sub $$
End Class</a>

            ' Show Foo() even though it's a Function
            Await VerifyItemExistsAsync(text.Value, "Foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAlreadyImplemented() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub foo()
    End Sub
End Class

Public Class b
    Inherits a
    Public Overrides Sub foo()
        MyBase.foo()
    End Sub

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotShowNotInheritable() As Task
            Dim text = <a>Public Class a
    Public NotInheritable Sub foo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotShowNotOverridable() As Task
            Dim text = <a>Public Class a
    Public Sub foo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotIfTextAfterPosition() As Task
            Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$ Function
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotIfDeclaringShared() As Task
            Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Shared Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "foo()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestProperty() As Task
            Dim text = <a>Public Class a
    Public Overridable Property foo As String
End Class

Public Class b
    Inherits a
    Public Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestShowAllAccessibilitiesIfNoneTyped() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub r1()
    End Sub
    Private Overridable Sub s1()
    End Sub
    Protected Overridable Sub t1()
    End Sub
    Friend Overridable Sub u1()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "r1()")
            Await VerifyItemExistsAsync(text.Value, "t1()")
            Await VerifyItemExistsAsync(text.Value, "u1()")
            Await VerifyItemIsAbsentAsync(text.Value, "s1()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilterPublic() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub r1()
    End Sub
    Private Overridable Sub s1()
    End Sub
    Protected Overridable Sub t1()
    End Sub
    Friend Overridable Sub u1()
    End Sub
End Class

Public Class b
    Inherits a
    Public Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "r1()")
            Await VerifyItemIsAbsentAsync(text.Value, "s1()")
            Await VerifyItemIsAbsentAsync(text.Value, "t1()")
            Await VerifyItemIsAbsentAsync(text.Value, "u1()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilterProtected() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub r1()
    End Sub
    Private Overridable Sub s1()
    End Sub
    Protected Overridable Sub t1()
    End Sub
    Friend Overridable Sub u1()
    End Sub
End Class

Public Class b
    Inherits a
    Protected Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "t1()")
            Await VerifyItemIsAbsentAsync(text.Value, "r1()")
            Await VerifyItemIsAbsentAsync(text.Value, "s1()")
            Await VerifyItemIsAbsentAsync(text.Value, "u1()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilterFriend() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub r1()
    End Sub
    Private Overridable Sub s1()
    End Sub
    Protected Overridable Sub t1()
    End Sub
    Friend Overridable Sub u1()
    End Sub
End Class

Public Class b
    Inherits a
    Friend Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "u1()")
            Await VerifyItemIsAbsentAsync(text.Value, "r1()")
            Await VerifyItemIsAbsentAsync(text.Value, "s1()")
            Await VerifyItemIsAbsentAsync(text.Value, "t1()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilterProtectedFriend() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub r1()
    End Sub
    Private Overridable Sub s1()
    End Sub
    Protected Overridable Sub t1()
    End Sub
    Friend Overridable Sub u1()
    End Sub
    Protected Friend Overridable Sub v1()
    End Sub
End Class

Public Class b
    Inherits a
    Protected Friend Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "v1()")
            Await VerifyItemIsAbsentAsync(text.Value, "u1()")
            Await VerifyItemIsAbsentAsync(text.Value, "r1()")
            Await VerifyItemIsAbsentAsync(text.Value, "s1()")
            Await VerifyItemIsAbsentAsync(text.Value, "t1()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass1() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X)
    Inherits Base(Of X)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As X)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass2() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As Y)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass3() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Foo(t As T, s As S)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y, Z)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As Y, s As Z)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T, s As S)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass1() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As Integer)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass2() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As Integer)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass3() As Task
            Dim markup = <a>Imports System

Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Foo(t As T, s As S)
End Class

Public Class SomeClass
    Inherits Base(Of Integer, Exception)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo(t As Integer, s As Exception)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Foo(t As T, s As S)")
        End Function

        <WorkItem(529714, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")>
        <WpfFact(Skip:="529714"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericMethodTypeParametersRenamed() As Task
            Dim text = <a>Class CFoo
    Overridable Function Something(Of X)(arg As X) As X
    End Function
End Class

Class Derived(Of X)
    Inherits CFoo

    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Something(Of X1)(arg As X1)")
            Await VerifyItemIsAbsentAsync(text.Value, "Something(Of X)(arg As X)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterTypeSimplified() As Task
            Dim text = <a>Imports System

Class CBase
    Public Overridable Sub foo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "foo(e As Exception)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedMethodNameInIntelliSenseList() As Task
            Dim markup = <a>Class CBase
    Public Overridable Sub [Class]()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>.Value

            Dim code As String = Nothing
            Dim position As Integer
            MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), code, position)

            Await BaseVerifyWorkerAsync(code, position, "[Class]()", "Sub CBase.Class()", SourceCodeKind.Regular, False, False, Nothing, experimental:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedPropertyNameInIntelliSenseList() As Task
            Dim markup = <a>Class CBase
    Public Overridable Property [Class] As Integer
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>.Value

            Dim code As String = Nothing
            Dim position As Integer
            MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), code, position)

            Await BaseVerifyWorkerAsync(code, position, "[Class]", "Property CBase.Class As Integer", SourceCodeKind.Regular, False, False, Nothing, experimental:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedParameterNameInIntelliSenseList() As Task
            Dim markup = <a>Class CBase
    Public Overridable Sub Foo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Foo([Integer] As Integer)", "Sub CBase.Foo([Integer] As Integer)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestHideKeywords() As Task
            Dim text = <a>
Class Program
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "ToString()")
            Await VerifyItemIsAbsentAsync(text.Value, "Function")
        End Function

#End Region

#Region "Commit tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitInEmptyClass() As Task
            Dim markupBeforeCommit = <a>Class c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class c
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSubBeforeSub() As Task
            Dim markupBeforeCommit = <a>Class c
    Overrides $$
    
        Sub bar()
    End Sub
End Class</a>

            Dim expectedCode = <a>Class c
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()$$
    End Function

    Sub bar()
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSubAfterSub() As Task
            Dim markupBeforeCommit = <a>Class c
    Sub bar()
    End Sub
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class c
    Sub bar()
    End Sub
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitFunction() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class d
    Inherits c
    Public Overrides Function foo() As Integer
        Return MyBase.foo()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitFunctionWithParams() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Function foo(x As Integer) As Integer
        Return x
    End Function
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Function foo(x As Integer) As Integer
        Return x
    End Function
End Class

Public Class d
    Inherits c
    Public Overrides Function foo(x As Integer) As Integer
        Return MyBase.foo(x)$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSubWithParams() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub foo(x As Integer)
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub foo(x As Integer)
    End Sub
End Class

Public Class d
    Inherits c
    Public Overrides Sub foo(x As Integer)
        MyBase.foo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitProtected() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Protected Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Protected Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Protected Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitFriend() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Friend Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Friend Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Friend Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitProtectedFriend() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Protected Friend Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Protected Friend Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Protected Friend Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAbstractThrows() As Task
            Dim markupBeforeCommit = <a>Public MustInherit Class c
    Public MustOverride Sub foo()
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System

Public MustInherit Class c
    Public MustOverride Sub foo()
End Class

Public Class d
    Inherits c
    Public Overrides Sub foo()
        Throw New NotImplementedException()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitRetainMustOverride() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    MustOverride Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c
    Public MustOverride Overrides Sub foo()$$
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitRetainNotOverridable() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c

    NotOverridable Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub foo()
    End Sub
End Class

Public Class d
    Inherits c

    Public NotOverridable Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable Property foo As String
End Class

Public Class derived
    Inherits base

    Overrides $$
End Class</a>


            Dim expectedCode = <a>Public Class base
    Public Overridable Property foo As String
End Class

Public Class derived
    Inherits base

    Public Overrides Property foo As String
        Get
            Return MyBase.foo$$
        End Get
        Set(value As String)
            MyBase.foo = value
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitWriteOnlyProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable WriteOnly Property foo As String
        Set(value As String)

        End Set
    End Property
End Class

Class derived
    Inherits base

    Public Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class base
    Public Overridable WriteOnly Property foo As String
        Set(value As String)

        End Set
    End Property
End Class

Class derived
    Inherits base

    Public Overrides WriteOnly Property foo As String
        Set(value As String)
            MyBase.foo = value$$
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitReadOnlyProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable ReadOnly Property foo As String
        Get

        End Get
    End Property
End Class

Class derived
    Inherits base

    Public Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class base
    Public Overridable ReadOnly Property foo As String
        Get

        End Get
    End Property
End Class

Class derived
    Inherits base

    Public Overrides ReadOnly Property foo As String
        Get
            Return MyBase.foo$$
        End Get
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(543937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543937")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitOptionalKeywordAndParameterValuesAreGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Class CBase
    Public Overridable Sub foo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$

End Class]]></a>

            Dim expectedCode = <a><![CDATA[Class CBase
    Public Overridable Sub foo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo(Optional x As Integer = 42)
        MyBase.foo(x)$$
    End Sub

End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAttributesAreNotGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Imports System

Class CBase
    <Obsolete()>
    Public Overridable Sub foo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class]]></a>

            Dim expectedCode = <a><![CDATA[Imports System

Class CBase
    <Obsolete()>
    Public Overridable Sub foo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitGenericMethod() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub foo(Of T)(x As T)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub foo(Of T)(x As T)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo(Of T)(x As T)
        MyBase.foo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(Of T)(x As T)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(545627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545627")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitGenericMethodOnArraySubstitutedGenericType() As Task
            Dim markupBeforeCommit = <a>Class A(Of T)
    Public Overridable Sub M(Of U As T)()
    End Sub
End Class
Class B
    Inherits A(Of Object())

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class A(Of T)
    Public Overridable Sub M(Of U As T)()
    End Sub
End Class
Class B
    Inherits A(Of Object())

    Public Overrides Sub M(Of U As Object())()
        MyBase.M(Of U)()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "M(Of U)()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitFormats() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub foo()
    End Sub
End Class

Class CDerived
    Inherits CBase

overrides         $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub foo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo()
        MyBase.foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSimplifiesParameterTypes() As Task
            Dim markupBeforeCommit = <a>Imports System
Class CBase
    Public Overridable Sub foo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System
Class CBase
    Public Overridable Sub foo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo(e As Exception)
        MyBase.foo(e)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(e As Exception)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSimplifiesReturnType() As Task
            Dim markupBeforeCommit = <a>Imports System
Class CBase
    Public Overridable Function foo() As System.Exception
        Return 0
    End Function
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System
Class CBase
    Public Overridable Function foo() As System.Exception
        Return 0
    End Function
End Class

Class CDerived
    Inherits CBase

    Public Overrides Function foo() As Exception
        Return MyBase.foo()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitEscapedMethodName() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub [Class]()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub [Class]()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub [Class]()
        MyBase.Class()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "[Class]()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitEscapedPropertyName() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Property [Class] As Integer
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Property [Class] As Integer
End Class

Class CDerived
    Inherits CBase

    Public Overrides Property [Class] As Integer
        Get
            Return MyBase.Class$$
        End Get
        Set(value As Integer)
            MyBase.Class = value
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "[Class]", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitEscapedParameterName() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub Foo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub Foo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub Foo([Integer] As Integer)
        MyBase.Foo([Integer])$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo([Integer] As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitByRef() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub foo(ByRef x As Integer, y As String)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub foo(ByRef x As Integer, y As String)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo(ByRef x As Integer, y As String)
        MyBase.foo(x, y)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(ByRef x As Integer, y As String)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(529714, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")>
        <WpfFact(Skip:="529714"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitGenericMethodTypeParametersRenamed() As Task
            Dim markupBeforeCommit = <a>Class CFoo
    Overridable Function Something(Of X)(arg As X) As X
    End Function
End Class

Class Derived(Of X)
    Inherits CFoo

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CFoo
    Overridable Function Something(Of X)(arg As X) As X
    End Function
End Class

Class Derived(Of X)
    Inherits CFoo

    Public Overrides Function Something(Of X1)(arg As X1) As X1
        Return MyBase.Something(Of X1)(arg)$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Something(Of X1)(arg As X1)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAddsImports() As Task
            Dim markupBeforeCommit = <a>MustInherit Class CBase
    MustOverride Sub Foo()
End Class

Class Derived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System

MustInherit Class CBase
    MustOverride Sub Foo()
End Class

Class Derived
    Inherits CBase

    Public Overrides Sub Foo()
        Throw New NotImplementedException()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(543937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543937")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOptionalArguments() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub foo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub foo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub foo(Optional x As Integer = 42)
        MyBase.foo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(636706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636706")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterizedProperty() As Task
            Dim markupBeforeCommit = <a>Public Class Foo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Public Class Foo3
    Inherits Foo

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class Foo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Public Class Foo3
    Inherits Foo

    Public Overrides Property Bar(bay As Integer) As Integer
        Get
            Return MyBase.Bar(bay)$$
        End Get
        Set(value As Integer)
            MyBase.Bar(bay) = value
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Bar(bay As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(529737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529737")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultPropertiesByName() As Task
            Dim markupBeforeCommit = <a>Class A
    Default Overridable ReadOnly Property Foo(x As Integer) As Object
        Get
        End Get
    End Property
End Class

Class B
    Inherits A

    Overrides $$
End Class
</a>

            Dim expectedCode = <a>Class A
    Default Overridable ReadOnly Property Foo(x As Integer) As Object
        Get
        End Get
    End Property
End Class

Class B
    Inherits A

    Default Public Overrides ReadOnly Property Foo(x As Integer) As Object
        Get
            Return MyBase.Foo(x)$$
        End Get
    End Property
End Class
</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function
#End Region

#Region "Commit: With Trivia"

        <WorkItem(529216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitSurroundingTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
    Overrides $$
#End If
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
    Public Overrides Sub Foo()
        MyBase.Foo()$$
    End Sub
#End If
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitBeforeTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
    Overrides $$
#If True Then
#End If
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Foo()
        MyBase.Foo()$$
    End Sub
#If True Then
#End If
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(529216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAfterTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
#End If
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
#End If
    Public Overrides Sub Foo()
        MyBase.Foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitBeforeComment() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base

    Overrides $$
    'SomeComment
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base

    Public Overrides Sub Foo()
        MyBase.Foo()$$
    End Sub
    'SomeComment
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(529216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAfterComment() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
    'SomeComment
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Foo()
    End Sub
End Class

Class Derived
    Inherits Base
    'SomeComment
    Public Overrides Sub Foo()
        MyBase.Foo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function
#End Region

        <WorkItem(529572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529572")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWitheventsFieldNotOffered() As Task
            Dim text = <a>Public Class C1
    Public WithEvents w As C1 = Me
End Class
Class C2 : Inherits C1
        overrides $$
End Class
</a>

            Await VerifyItemIsAbsentAsync(text.Value, "w")
        End Function

        <WorkItem(715, "https://github.com/dotnet/roslyn/issues/715")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventsNotOffered() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <ProjectReference>CSProject</ProjectReference>
                               <Document FilePath="VBDocument">
Class D
    Inherits C

    overrides $$
End Class</Document>
                           </Project>
                           <Project Language="C#" CommonReferences="true" AssemblyName="CSProject">
                               <Document FilePath="CSDocument">
using System;

public class C
{
    public virtual event EventHandler e;
}
        </Document>
                           </Project>
                       </Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(text)
                Dim hostDocument = workspace.Documents.First()
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()

                Dim completionList = Await GetCompletionListAsync(document, caretPosition, triggerInfo)
                Assert.False(completionList.Items.Any(Function(c) c.DisplayText = "e"))
            End Using
        End Function
    End Class
End Namespace