' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class GetXmlNamespaceExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(GetXmlNamespaceExpressionSignatureHelpProvider)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForGetType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
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
