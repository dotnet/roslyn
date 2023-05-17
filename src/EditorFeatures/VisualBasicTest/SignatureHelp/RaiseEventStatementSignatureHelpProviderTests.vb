' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    <Trait(Traits.Feature, Traits.Features.SignatureHelp)>
    Public Class RaiseEventStatementSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(RaiseEventStatementSignatureHelpProvider)
        End Function

#Region "Regular tests"

        <Fact>
        Public Async Function TestRaiseEvent() As Task
            Dim markup = <a><![CDATA[
Class C
    Event E(i As Integer, s As String)

    Sub M()
        RaiseEvent [|E($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem) From {
                New SignatureHelpTestItem("C.E(i As Integer, s As String)", String.Empty, String.Empty, currentParameterIndex:=0)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestRaiseEvent_NoDerivedEvents() As Task
            Dim markup = <a><![CDATA[
Class B
    Event E1(i As Integer, s As String)
End Class

Class C
    Inherits B

    Event E2(i As Integer, s As String)

    Sub M()
        RaiseEvent E1($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem)

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543558")>
        Public Async Function TestRaiseEvent_Shared() As Task
            Dim markup = <a><![CDATA[
Class C
    Shared Event E(i As Integer, s As String)

    Shared Sub M()
        RaiseEvent E($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem) From {
                New SignatureHelpTestItem("C.E(i As Integer, s As String)", String.Empty, String.Empty, currentParameterIndex:=0)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestRaiseEvent_NoInstanceInSharedContext() As Task
            Dim markup = <a><![CDATA[
Class C
    Event E(i As Integer, s As String)

    Shared Sub M()
        RaiseEvent E($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem)

            Await TestAsync(markup, expectedOrderedItems)
        End Function

#End Region

    End Class
End Namespace
