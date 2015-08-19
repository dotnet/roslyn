' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class RaiseEventStatementSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New RaiseEventStatementSignatureHelpProvider()
        End Function

#Region "Regular tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestRaiseEvent()
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

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestRaiseEvent_NoDerivedEvents()
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

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(543558)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestRaiseEvent_Shared()
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

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestRaiseEvent_NoInstanceInSharedContext()
            Dim markup = <a><![CDATA[
Class C
    Event E(i As Integer, s As String)

    Shared Sub M()
        RaiseEvent E($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem)

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

    End Class
End Namespace
