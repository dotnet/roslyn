' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        <WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")>
        Public Async Function InPropertyWithSameNameAsGenericTypeArgument1() As Task
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Namespace Namespace1
    Module Program
        Public Bar As List(Of Bar)

        Sub Main()
            Bar = New $$
        End Sub
    End Module

    Class Bar
    End Class
End Namespace
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "List(Of Bar)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")>
        Public Async Function InPropertyWithSameNameAsGenericTypeArgument2() As Task
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Namespace Namespace1
    Module Program
        Public Bar As List(Of Bar) = New $$
    End Module

    Class Bar
    End Class
End Namespace
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "List(Of Bar)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")>
        Public Async Function InPropertyWithSameNameAsGenericTypeArgument3() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Module Program
        Public A As C(Of B)
        Public B As C(Of A)

        Sub M()
            A = New $$
        End Sub
    End Module

    Class A
    End Class

    Class B
    End Class

    Class C(Of T)
    End Class
End Namespace
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "C(Of B)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(21674, "https://github.com/dotnet/roslyn/issues/21674")>
        Public Async Function PropertyWithSameNameAsOtherType() As Task
            Dim markup = <Text><![CDATA[
Namespace Namespace1
    Module Program
        Public Property A() As B
        Public Property B() As A

        Sub M()
            B = New $$
        End Sub
    End Module

    Class A
    End Class

    Class B
    End Class
End Namespace
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "A")
        End Function
    End Class
End Namespace
