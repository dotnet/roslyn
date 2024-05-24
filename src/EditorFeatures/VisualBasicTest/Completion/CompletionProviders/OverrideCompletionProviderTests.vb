' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Tests
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class OverrideCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(OverrideCompletionProvider)
        End Function

#Region "CompletionItem tests"

        <WpfFact>
        Public Async Function TestNotOfferedBaseClassMember() As Task
            Dim text = <a>MustInherit Class Base
    Public MustOverride Sub Goo()
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Goo()
    End Sub
End Class

Class SomeClass
    Inherits Derived
    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "Goo()", "Sub Base.Goo()")
        End Function

        <WpfFact>
        Public Async Function TestIntermediateClassOverriddenMember() As Task
            Dim text = <a>MustInherit Class Base
    Public MustOverride Sub Goo()
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Goo()
    End Sub
End Class

Class SomeClass
    Inherits Derived
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Goo()", "Sub Derived.Goo()")
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543807")>
        Public Async Function TestHideFinalize() As Task
            Dim text = <a>Class goo
    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "Finalize()")
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543807")>
        Public Async Function TestShowShadowingFinalize() As Task
            Dim text = <a>Class goo
    Overridable Shadows Sub Finalize()
    End Sub
End Class

Class bar
    Inherits goo

    overrides $$
End class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo.Finalize()")
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543806")>
        Public Async Function TestShowObjectOverrides() As Task
            Dim text = <a>Class goo
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Equals(obj As Object)")
            Await VerifyItemExistsAsync(text.Value, "ToString()")
            Await VerifyItemExistsAsync(text.Value, "GetHashCode()")
        End Function

        <WpfFact>
        Public Async Function TestInheritedOverridableSub() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub goo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestInheritedOverridableFunction() As Task
            Dim text = <a>Public Class a
    Public Overridable Function goo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestInheritedMustOverrideFunction() As Task
            Dim text = <a>Public Class a
    Public MustOverride Sub goo()
    End Sub
End Class

Public Class b
    Inherits a
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestMatchSub() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub goo()
    End Sub

    Public Overridable Function bar() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides Sub $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo()")
            Await VerifyItemIsAbsentAsync(text.Value, "bar()")
        End Function

        <WpfFact>
        Public Async Function TestMatchFunction() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub goo()
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
            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestDoNotFilterIfNothingMatchesReturnTypeVoidness() As Task
            Dim text = <a>MustInherit Class Base
    MustOverride Function Goo() As String
    Protected NotOverridable Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class

Class Derived
    Inherits Base
    Overrides Sub $$
End Class</a>

            ' Show Goo() even though it's a Function
            Await VerifyItemExistsAsync(text.Value, "Goo()")
        End Function

        <WpfFact>
        Public Async Function TestNotAlreadyImplemented() As Task
            Dim text = <a>Public Class a
    Public Overridable Sub goo()
    End Sub
End Class

Public Class b
    Inherits a
    Public Overrides Sub goo()
        MyBase.goo()
    End Sub

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestNotShowNotInheritable() As Task
            Dim text = <a>Public Class a
    Public NotInheritable Sub goo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestNotShowNotOverridable() As Task
            Dim text = <a>Public Class a
    Public Sub goo()
    End Sub
End Class

Public Class b
    Inherits a

    Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestNotIfTextAfterPosition() As Task
            Dim text = <a>Public Class a
    Public Overridable Function goo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Overrides $$ Function
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestNotIfDeclaringShared() As Task
            Dim text = <a>Public Class a
    Public Overridable Function goo() As Integer
        Return 0
    End Function
End Class

Public Class b
    Inherits a
    Shared Overrides $$
End Class</a>

            Await VerifyItemIsAbsentAsync(text.Value, "goo()")
        End Function

        <WpfFact>
        Public Async Function TestSuggestProperty() As Task
            Dim text = <a>Public Class a
    Public Overridable Property goo As String
End Class

Public Class b
    Inherits a
    Public Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo")
        End Function

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass1() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Goo(t As T)
End Class

Public Class SomeClass(Of X)
    Inherits Base(Of X)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As X)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T)")
        End Function

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass2() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Goo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As Y)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T)")
        End Function

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForGenericInDerivedClass3() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Goo(t As T, s As S)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Y, Z)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As Y, s As Z)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T, s As S)")
        End Function

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass1() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Goo(t As T)
End Class

Public Class SomeClass
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As Integer)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T)")
        End Function

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass2() As Task
            Dim markup = <a>Public MustInherit Class Base(Of T)
    Public MustOverride Sub Goo(t As T)
End Class

Public Class SomeClass(Of X, Y, Z)
    Inherits Base(Of Integer)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As Integer)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T)")
        End Function

        <WpfFact>
        Public Async Function TestGenericTypeNameSubstitutedForNonGenericInDerivedClass3() As Task
            Dim markup = <a>Imports System

Public MustInherit Class Base(Of T, S)
    Public MustOverride Sub Goo(t As T, s As S)
End Class

Public Class SomeClass
    Inherits Base(Of Integer, Exception)
    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo(t As Integer, s As Exception)")
            Await VerifyItemIsAbsentAsync(markup.Value, "Goo(t As T, s As S)")
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")>
        Public Async Function TestGenericMethodTypeParametersNotRenamed() As Task
            Dim text = <a>Class CGoo    
    Overridable Function Something(Of X)(arg As X) As X    
    End Function    
    End Class    
   
    Class Derived(Of X)    
     Inherits CGoo    
     Overrides $$    
     End Class</a>

            Await VerifyItemExistsAsync(text.Value, "Something(Of X)(arg As X)")
        End Function

        <WpfFact>
        Public Async Function TestParameterTypeSimplified() As Task
            Dim text = <a>Imports System

Class CBase
    Public Overridable Sub goo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(text.Value, "goo(e As Exception)")
        End Function

        <WpfFact>
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

            Await BaseVerifyWorkerAsync(code, position, "[Class]()", "Sub CBase.Class()", SourceCodeKind.Regular, False, False, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
        End Function

        <WpfFact>
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

            Await BaseVerifyWorkerAsync(
                code, position, "[Class]", "Property CBase.Class As Integer",
                SourceCodeKind.Regular, False, False, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
        End Function

        <WpfFact>
        Public Async Function TestEscapedParameterNameInIntelliSenseList() As Task
            Dim markup = <a>Class CBase
    Public Overridable Sub Goo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Await VerifyItemExistsAsync(markup.Value, "Goo([Integer] As Integer)", "Sub CBase.Goo([Integer] As Integer)")
        End Function

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
        Public Async Function TestCommitFunction() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Function goo() As Integer
        Return 0
    End Function
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Function goo() As Integer
        Return 0
    End Function
End Class

Public Class d
    Inherits c
    Public Overrides Function goo() As Integer
        Return MyBase.goo()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitFunctionWithParams() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Function goo(x As Integer) As Integer
        Return x
    End Function
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Function goo(x As Integer) As Integer
        Return x
    End Function
End Class

Public Class d
    Inherits c
    Public Overrides Function goo(x As Integer) As Integer
        Return MyBase.goo(x)$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitSubWithParams() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub goo(x As Integer)
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub goo(x As Integer)
    End Sub
End Class

Public Class d
    Inherits c
    Public Overrides Sub goo(x As Integer)
        MyBase.goo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitProtected() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Protected Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Protected Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Protected Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitFriend() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Friend Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Friend Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Friend Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitProtectedFriend() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Protected Friend Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Protected Friend Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Protected Friend Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitAbstractThrows() As Task
            Dim markupBeforeCommit = <a>Imports System

Public MustInherit Class c
    Public MustOverride Sub goo()
End Class

Public Class d
    Inherits c
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System

Public MustInherit Class c
    Public MustOverride Sub goo()
End Class

Public Class d
    Inherits c
    Public Overrides Sub goo()
        Throw New NotImplementedException()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitRetainMustOverride() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    MustOverride Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c
    Public MustOverride Overrides Sub goo()$$
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitRetainNotOverridable() As Task
            Dim markupBeforeCommit = <a>Public Class c
    Public Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c

    NotOverridable Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class c
    Public Overridable Sub goo()
    End Sub
End Class

Public Class d
    Inherits c

    Public NotOverridable Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable Property goo As String
End Class

Public Class derived
    Inherits base

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class base
    Public Overridable Property goo As String
End Class

Public Class derived
    Inherits base

    Public Overrides Property goo As String
        Get
            Return MyBase.goo$$
        End Get
        Set(value As String)
            MyBase.goo = value
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitWriteOnlyProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable WriteOnly Property goo As String
        Set(value As String)

        End Set
    End Property
End Class

Class derived
    Inherits base

    Public Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class base
    Public Overridable WriteOnly Property goo As String
        Set(value As String)

        End Set
    End Property
End Class

Class derived
    Inherits base

    Public Overrides WriteOnly Property goo As String
        Set(value As String)
            MyBase.goo = value$$
        End Set
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitReadOnlyProperty() As Task
            Dim markupBeforeCommit = <a>Public Class base
    Public Overridable ReadOnly Property goo As String
        Get

        End Get
    End Property
End Class

Class derived
    Inherits base

    Public Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class base
    Public Overridable ReadOnly Property goo As String
        Get

        End Get
    End Property
End Class

Class derived
    Inherits base

    Public Overrides ReadOnly Property goo As String
        Get
            Return MyBase.goo$$
        End Get
    End Property
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitPropertyInaccessibleParameterAttributesAreNotGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Imports System

Public Class Class1
    Private Class MyPrivate
        Inherits Attribute
    End Class
    Public Class MyPublic
        Inherits Attribute
    End Class

    Default Public Overridable Property Item(<MyPrivate, MyPublic> i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

Public Class Class2
    Inherits Class1

    Public Overrides Property $$
End Class]]></a>

            Dim expectedCode = <a><![CDATA[Imports System

Public Class Class1
    Private Class MyPrivate
        Inherits Attribute
    End Class
    Public Class MyPublic
        Inherits Attribute
    End Class

    Default Public Overridable Property Item(<MyPrivate, MyPublic> i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

Public Class Class2
    Inherits Class1

    Default Public Overrides Property Item(<MyPublic> i As Integer) As Integer
        Get
            Return MyBase.Item(i)$$
        End Get
        Set(value As Integer)
            MyBase.Item(i) = value
        End Set
    End Property
End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Item(i As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543937")>
        Public Async Function TestCommitOptionalKeywordAndParameterValuesAreGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Class CBase
    Public Overridable Sub goo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$

End Class]]></a>

            Dim expectedCode = <a><![CDATA[Class CBase
    Public Overridable Sub goo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo(Optional x As Integer = 42)
        MyBase.goo(x)$$
    End Sub

End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitAttributesAreNotGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Imports System

Class CBase
    <Obsolete()>
    Public Overridable Sub goo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class]]></a>

            Dim expectedCode = <a><![CDATA[Imports System

Class CBase
    <Obsolete()>
    Public Overridable Sub goo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function CommitInaccessibleParameterAttributesAreNotGenerated() As Task
            Dim markupBeforeCommit = <a><![CDATA[Imports System

Public Class Class1
    Private Class MyPrivate
        Inherits Attribute
    End Class
    Public Class MyPublic
        Inherits Attribute
    End Class

    Public Overridable Sub M(<MyPrivate, MyPublic> i As Integer)
    End Sub
End Class

Public Class Class2
    Inherits Class1

    Public Overrides Sub $$
End Class]]></a>

            Dim expectedCode = <a><![CDATA[Imports System

Public Class Class1
    Private Class MyPrivate
        Inherits Attribute
    End Class
    Public Class MyPublic
        Inherits Attribute
    End Class

    Public Overridable Sub M(<MyPrivate, MyPublic> i As Integer)
    End Sub
End Class

Public Class Class2
    Inherits Class1

    Public Overrides Sub M(<MyPublic> i As Integer)
        MyBase.M(i)$$
    End Sub
End Class]]></a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "M(i As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitGenericMethod() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub goo(Of T)(x As T)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub goo(Of T)(x As T)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo(Of T)(x As T)
        MyBase.goo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(Of T)(x As T)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545627")>
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

        <WpfFact>
        Public Async Function TestCommitFormats() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub goo()
    End Sub
End Class

Class CDerived
    Inherits CBase

overrides         $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub goo()
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo()
        MyBase.goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitSimplifiesParameterTypes() As Task
            Dim markupBeforeCommit = <a>Imports System
Class CBase
    Public Overridable Sub goo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System
Class CBase
    Public Overridable Sub goo(e As System.Exception)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo(e As Exception)
        MyBase.goo(e)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(e As Exception)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitSimplifiesReturnType() As Task
            Dim markupBeforeCommit = <a>Imports System
Class CBase
    Public Overridable Function goo() As System.Exception
        Return 0
    End Function
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Imports System
Class CBase
    Public Overridable Function goo() As System.Exception
        Return 0
    End Function
End Class

Class CDerived
    Inherits CBase

    Public Overrides Function goo() As Exception
        Return MyBase.goo()$$
    End Function
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
        Public Async Function TestCommitEscapedParameterName() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub Goo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub Goo([Integer] As Integer)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub Goo([Integer] As Integer)
        MyBase.Goo([Integer])$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo([Integer] As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitByRef() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub goo(ByRef x As Integer, y As String)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub goo(ByRef x As Integer, y As String)
    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo(ByRef x As Integer, y As String)
        MyBase.goo(x, y)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(ByRef x As Integer, y As String)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")>
        Public Async Function TestCommitGenericMethodTypeParametersNotRenamed() As Task
            Dim markupBeforeCommit = <a>Class CGoo    
    Overridable Function Something(Of X)(arg As X) As X    
    End Function    
End Class    
    
Class Derived(Of X)    
    Inherits CGoo    
      Overrides $$    
End Class</a>

            Dim expectedCode = <a>Class CGoo    
    Overridable Function Something(Of X)(arg As X) As X    
    End Function    
End Class    
    
Class Derived(Of X)    
    Inherits CGoo
    Public Overrides Function Something(Of X)(arg As X) As X
        Return MyBase.Something(arg)$$
    End Function
End Class</a>
            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Something(Of X)(arg As X)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestAddsImports() As Task
            Dim markupBeforeCommit = <a>MustInherit Class CBase
    MustOverride Sub Goo()
End Class

Class Derived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>MustInherit Class CBase
    MustOverride Sub Goo()
End Class

Class Derived
    Inherits CBase

    Public Overrides Sub Goo()
        Throw New System.NotImplementedException()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543937")>
        Public Async Function TestOptionalArguments() As Task
            Dim markupBeforeCommit = <a>Class CBase
    Public Overridable Sub goo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class CBase
    Public Overridable Sub goo(Optional x As Integer = 42)

    End Sub
End Class

Class CDerived
    Inherits CBase

    Public Overrides Sub goo(Optional x As Integer = 42)
        MyBase.goo(x)$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "goo(x As Integer = 42)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636706")>
        Public Async Function TestParameterizedProperty() As Task
            Dim markupBeforeCommit = <a>Public Class Goo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Public Class Goo3
    Inherits Goo

    Overrides $$
End Class</a>

            Dim expectedCode = <a>Public Class Goo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Public Class Goo3
    Inherits Goo

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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529737")>
        Public Async Function TestOverrideDefaultPropertiesByName() As Task
            Dim markupBeforeCommit = <a>Class A
    Default Overridable ReadOnly Property Goo(x As Integer) As Object
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
    Default Overridable ReadOnly Property Goo(x As Integer) As Object
        Get
        End Get
    End Property
End Class

Class B
    Inherits A

    Default Public Overrides ReadOnly Property Goo(x As Integer) As Object
        Get
            Return MyBase.Goo(x)$$
        End Get
    End Property
End Class
</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo(x As Integer)", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function
#End Region

#Region "Commit: With Trivia"

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        Public Async Function TestCommitSurroundingTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
    Overrides $$
#End If
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
    Public Overrides Sub Goo()
        MyBase.Goo()$$
    End Sub
#End If
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitBeforeTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
    Overrides $$
#If True Then
#End If
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
    Public Overrides Sub Goo()
        MyBase.Goo()$$
    End Sub
#If True Then
#End If
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        Public Async Function TestCommitAfterTriviaDirective() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
#End If
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
#If True Then
#End If
    Public Overrides Sub Goo()
        MyBase.Goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact>
        Public Async Function TestCommitBeforeComment() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base

    Overrides $$
    'SomeComment
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base

    Public Overrides Sub Goo()
        MyBase.Goo()$$
    End Sub
    'SomeComment
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529216")>
        Public Async Function TestCommitAfterComment() As Task
            Dim markupBeforeCommit = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
    'SomeComment
    Overrides $$
End Class</a>

            Dim expectedCode = <a>Class Base
    Public Overridable Sub Goo()
    End Sub
End Class

Class Derived
    Inherits Base
    'SomeComment
    Public Overrides Sub Goo()
        MyBase.Goo()$$
    End Sub
End Class</a>

            Await VerifyCustomCommitProviderAsync(markupBeforeCommit.Value.Replace(vbLf, vbCrLf), "Goo()", expectedCode.Value.Replace(vbLf, vbCrLf))
        End Function
#End Region

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529572")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/715")>
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

            Using workspace = EditorTestWorkspace.Create(text, composition:=GetComposition())
                Dim hostDocument = workspace.Documents.First()
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)

                Dim service = GetCompletionService(document.Project)
                Dim completionList = Await GetCompletionListAsync(service, document, caretPosition, CompletionTrigger.Invoke)
                Assert.False(completionList.ItemsList.Any(Function(c) c.DisplayText = "e"))
            End Using
        End Function

        Public Overloads Function VerifyItemExistsAsync(markup As String, expectedItem As String) As Task
            Return VerifyItemExistsAsync(markup, expectedItem, isComplexTextEdit:=True)
        End Function
    End Class
End Namespace
