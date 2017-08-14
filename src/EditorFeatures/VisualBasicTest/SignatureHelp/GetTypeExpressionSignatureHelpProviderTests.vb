' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class GetTypeExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New GetTypeExpressionSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForGetType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = GetType($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"GetType({VBWorkspaceResources.typeName}) As System.Type",
                                     VBWorkspaceResources.Returns_a_System_Type_object_for_the_specified_type_name,
                                     VBWorkspaceResources.The_type_name_to_return_a_System_Type_object_for,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
