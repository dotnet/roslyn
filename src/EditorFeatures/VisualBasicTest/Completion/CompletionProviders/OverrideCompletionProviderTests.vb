' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

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
    Public Sub NotOfferedBaseClassMember()
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

        VerifyItemIsAbsent(text.Value, "Foo()", "Sub Base.Foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub IntermediateClassOverriddenMember()
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

        VerifyItemExists(text.Value, "Foo()", "Sub Derived.Foo()")
    End Sub

    <WorkItem(543807)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub HideFinalize()
        Dim text = <a>Class foo
    Overrides $$
End Class</a>

        VerifyItemIsAbsent(text.Value, "Finalize()")
    End Sub

    <WorkItem(543807)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ShowShadowingFinalize()
        Dim text = <a>Class foo
    Overridable Shadows Sub Finalize()
    End Sub
End Class

Class bar
    Inherits foo

    overrides $$
End class</a>

        VerifyItemIsAbsent(text.Value, "foo.Finalize()")
    End Sub

    <WorkItem(543806)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ShowObjectOverrides()
        Dim text = <a>Class foo
    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "Equals(obj As Object)")
        VerifyItemExists(text.Value, "ToString()")
        VerifyItemExists(text.Value, "GetHashCode()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub InheritedOverridableSub()
        Dim text = <a>Public Class a
    Public Overridable Sub foo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub InheritedOverridableFunction()
        Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub InheritedMustOverrideFunction()
        Dim text = <a>Public Class a
    Public MustOverride Sub foo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub MatchSub()
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

        VerifyItemExists(text.Value, "foo()")
        VerifyItemIsAbsent(text.Value, "bar()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub MatchFunction()
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

        VerifyItemExists(text.Value, "bar()")
        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub DontFilterIfNothingMatchesReturnTypeVoidness()
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
        VerifyItemExists(text.Value, "Foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotAlreadyImplemented()
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

        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotShowNotInheritable()
        Dim text = <a>Public Class a
    Public NotInheritable Sub foo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotShowNotOverridable()
        Dim text = <a>Public Class a
    Public Sub foo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotIfTextAfterPosition()
        Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$ Function
End Class</a>

        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotIfDeclaringShared()
        Dim text = <a>Public Class a
    Public Overridable Function foo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Shared Overrides $$
End Class</a>

        VerifyItemIsAbsent(text.Value, "foo()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub SuggestProperty()
        Dim text = <a>Public Class a
    Public Overridable Property foo As String
End Class

Public Class b
    Inherits a
    Public Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "foo")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ShowAllAccessibilitiesIfNoneTyped()
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

        VerifyItemExists(text.Value, "r1()")
        VerifyItemExists(text.Value, "t1()")
        VerifyItemExists(text.Value, "u1()")
        VerifyItemIsAbsent(text.Value, "s1()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub FilterPublic()
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

        VerifyItemExists(text.Value, "r1()")
        VerifyItemIsAbsent(text.Value, "s1()")
        VerifyItemIsAbsent(text.Value, "t1()")
        VerifyItemIsAbsent(text.Value, "u1()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub FilterProtected()
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

        VerifyItemExists(text.Value, "t1()")
        VerifyItemIsAbsent(text.Value, "r1()")
        VerifyItemIsAbsent(text.Value, "s1()")
        VerifyItemIsAbsent(text.Value, "u1()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub FilterFriend()
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

        VerifyItemExists(text.Value, "u1()")
        VerifyItemIsAbsent(text.Value, "r1()")
        VerifyItemIsAbsent(text.Value, "s1()")
        VerifyItemIsAbsent(text.Value, "t1()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub FilterProtectedFriend()
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

        VerifyItemExists(text.Value, "v1()")
        VerifyItemIsAbsent(text.Value, "u1()")
        VerifyItemIsAbsent(text.Value, "r1()")
        VerifyItemIsAbsent(text.Value, "s1()")
        VerifyItemIsAbsent(text.Value, "t1()")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForGenericInDerivedClass1()
        Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X)
    Inherits Base(Of X)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As X)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForGenericInDerivedClass2()
        Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As Y)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForGenericInDerivedClass3()
        Dim markup = <a>Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Foo(t As T, s As S)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y, Z)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As Y, s As Z)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T, s As S)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForNonGenericInDerivedClass1()
        Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As Integer)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForNonGenericInDerivedClass2()
        Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Foo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As Integer)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericTypeNameSubstitutedForNonGenericInDerivedClass3()
        Dim markup = <a>Imports System

Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Foo(t As T, s As S)
End Class

Public Class SomeClass
    Inherits Base(Of Integer, Exception)
    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo(t As Integer, s As Exception)")
        VerifyItemIsAbsent(markup.Value, "Foo(t As T, s As S)")
    End Sub

    <WorkItem(529714)>
    <WpfFact(Skip:="529714"), Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub GenericMethodTypeParametersRenamed()
        Dim text = <a>Class CFoo
    Overridable Function Something(Of X)(arg As X) As X
    End Function
End Class

Class Derived(Of X)
    Inherits CFoo

    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "Something(Of X1)(arg As X1)")
        VerifyItemIsAbsent(text.Value, "Something(Of X)(arg As X)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ParameterTypeSimplified()
        Dim text = <a>Imports System

Class CBase
    Public Overridable Sub foo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "foo(e As Exception)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub EscapedMethodNameInIntelliSenseList()
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

        BaseVerifyWorker(code, position, "[Class]()", "Sub CBase.Class()", SourceCodeKind.Regular, False, False, Nothing, experimental:=False)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub EscapedPropertyNameInIntelliSenseList()
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

        BaseVerifyWorker(code, position, "[Class]", "Property CBase.Class As Integer", SourceCodeKind.Regular, False, False, Nothing, experimental:=False)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub EscapedParameterNameInIntelliSenseList()
        Dim markup = <a>Class CBase
    Public Overridable Sub Foo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

        VerifyItemExists(markup.Value, "Foo([Integer] As Integer)", "Sub CBase.Foo([Integer] As Integer)")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub HideKeywords()
        Dim text = <a>
Class Program
    Overrides $$
End Class</a>

        VerifyItemExists(text.Value, "ToString()")
        VerifyItemIsAbsent(text.Value, "Function")
    End Sub

#End Region

#Region "Commit tests"

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitInEmptyClass()
        Dim markupBeforeCommit = <a>Class c
    Overrides $$
End Class</a>

        Dim expectedCode = <a>Class c
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()$$
    End Function
End Class</a>

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSubBeforeSub()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSubAfterSub()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "GetHashCode()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitFunction()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitFunctionWithParams()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSubWithParams()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitProtected()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitFriend()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitProtectedFriend()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitAbstractThrows()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitRetainMustOverride()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitRetainNotOverridable()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitProperty()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitWriteOnlyProperty()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitReadOnlyProperty()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(543937)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitOptionalKeywordAndParameterValuesAreGenerated()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitAttributesAreNotGenerated()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitGenericMethod()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(Of T)(x As T)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(545627)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitGenericMethodOnArraySubstitutedGenericType()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "M(Of U)()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitFormats()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSimplifiesParameterTypes()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(e As Exception)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSimplifiesReturnType()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitEscapedMethodName()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "[Class]()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitEscapedPropertyName()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "[Class]", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitEscapedParameterName()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo([Integer] As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitByRef()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(ByRef x As Integer, y As String)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(529714)>
    <WpfFact(Skip:="529714"), Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitGenericMethodTypeParametersRenamed()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Something(Of X1)(arg As X1)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AddsImports()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(543937)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub OptionalArguments()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "foo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(636706)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ParameterizedProperty()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Bar(bay As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(529737)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub OverrideDefaultPropertiesByName()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub
#End Region

#Region "Commit: With Trivia"

    <WorkItem(529216)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitSurroundingTriviaDirective()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitBeforeTriviaDirective()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(529216)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitAfterTriviaDirective()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitBeforeComment()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub

    <WorkItem(529216)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitAfterComment()
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

        VerifyCustomCommitProvider(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Foo()", expectedCode.Value.Replace(vbLf, vbCrLf))
    End Sub
#End Region

    <WorkItem(529572)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub WitheventsFieldNotOffered()
        Dim text = <a>Public Class C1
    Public WithEvents w As C1 = Me
End Class
Class C2 : Inherits C1
        overrides $$
End Class
</a>

        VerifyItemIsAbsent(text.Value, "w")
    End Sub

    <WorkItem(715, "https://github.com/dotnet/roslyn/issues/715")>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub EventsNotOffered()
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

        Using workspace = TestWorkspaceFactory.CreateWorkspace(text)
            Dim hostDocument = workspace.Documents.First()
            Dim caretPosition = hostDocument.CursorPosition.Value
            Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()

            Dim completionList = GetCompletionList(document, caretPosition, triggerInfo)
            Assert.False(completionList.Items.Any(Function(c) c.DisplayText = "e"))
        End Using
    End Sub
End Class
