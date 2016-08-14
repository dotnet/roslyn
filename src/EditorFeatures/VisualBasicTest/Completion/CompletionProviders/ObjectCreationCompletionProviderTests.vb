' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ObjectCreationCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New ObjectCreationCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestInYieldReturn() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function M() As IEnumerable(Of EntryPointNotFoundException())
        Yield New $$
    End Function
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "EntryPointNotFoundException")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestInAsyncMethodReturnStatement() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Function M() As Task(Of EntryPointNotFoundException)
        Await Task.Delay(1)
        Return New $$
    End Function
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "EntryPointNotFoundException")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(892209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892209")>
        Public Async Function TestUnwrapNullable() As Task
            Dim markup = <Text><![CDATA[
Public Class C
  Sub M1(arg As N.S?)
  End Sub
 
  Sub M2()
    M1(New $$)
  End Sub
End Class
 
Namespace N
  Public Structure S
    Public Sub New(arg As Integer)
    End Sub
  End Structure
End Namespace

]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "N.S")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInTrivia() As Task
            Dim markup = <Text><![CDATA[
Public Class C
  Sub M1(arg As N.S?)
  End Sub
 
  Sub M2()
    M1(New $$)
  End Sub
End Class
 
Namespace N
  Public Structure S
    Public Sub New(arg As Integer)
    End Sub
  End Structure
End Namespace

]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "N.S")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4585, "https://github.com/dotnet/roslyn/issues/4585")>
        Public Async Function TestGenericArguments_PredefinedTypes() As Task
            Dim markup = <Text><![CDATA[
Imports System

Class A(Of T)
End Class

Class B
  Private _a As A(Of Int32) = New $$
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "A(Of Integer)",
                options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic, True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4585, "https://github.com/dotnet/roslyn/issues/4585")>
        Public Async Function TestGenericArguments_FrameworkTypes() As Task
            Dim markup = <Text><![CDATA[
Imports System

Class A(Of T)
End Class

Class B
  Private _a As A(Of Integer) = New $$
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "A(Of Int32)",
                options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic, False))
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/13610"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EscapeAwaitIdentifierInAsyncMethod() As Task
            Dim text =
<code><![CDATA[
Class Await
End Class

Class C
    Async Sub Foo()
        Dim a As [Await] = New $$
    End Sub
End Class
]]></code>.Value

            Dim expected =
<code><![CDATA[
Class Await
End Class

Class C
    Async Sub Foo()
        Dim a As [Await] = New [Await]
    End Sub
End Class
]]></code>.Value

            Await VerifyProviderCommitAsync(text, "Await", expected, Nothing, "")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function UnescapedAwaitIdentifierInNonAsyncMethod() As Task
            Dim text =
<code><![CDATA[
Class Await
End Class

Class C
    Sub Foo()
        Dim a As Await = New $$
    End Sub
End Class
]]></code>.Value

            Dim expected =
<code><![CDATA[
Class Await
End Class

Class C
    Sub Foo()
        Dim a As Await = New Await
    End Sub
End Class
]]></code>.Value

            Await VerifyProviderCommitAsync(text, "Await", expected, Nothing, "")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function UnescapedNestedAwaitIdentifierInAsyncMethod() As Task
            Dim text =
<code><![CDATA[
Namespace NS
    Class Await
    End Class
End Namespace

Class C
    Async Sub Foo()
        Dim a As NS.Await = New $$
    End Sub
End Class
]]></code>.Value

            Dim expected =
<code><![CDATA[
Namespace NS
    Class Await
    End Class
End Namespace

Class C
    Async Sub Foo()
        Dim a As NS.Await = New NS.Await
    End Sub
End Class
]]></code>.Value

            Await VerifyProviderCommitAsync(text, "NS.Await", expected, Nothing, "")
        End Function
    End Class
End Namespace