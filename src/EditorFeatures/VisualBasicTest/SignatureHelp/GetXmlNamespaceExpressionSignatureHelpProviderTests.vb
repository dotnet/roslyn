' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class GetXmlNamespaceExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New GetXmlNamespaceExpressionSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForGetType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = GetXmlNamespace($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"GetXmlNamespace([{VBWorkspaceResources.xmlNamespacePrefix}]) As System.Xml.Linq.XNamespace",
                                     VBWorkspaceResources.Returns_the_System_Xml_Linq_XNamespace_object_corresponding_to_the_specified_XML_namespace_prefix,
                                     VBWorkspaceResources.The_XML_namespace_prefix_to_return_a_System_Xml_Linq_XNamespace_object_for_If_this_is_omitted_the_object_for_the_default_XML_namespace_is_returned,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
