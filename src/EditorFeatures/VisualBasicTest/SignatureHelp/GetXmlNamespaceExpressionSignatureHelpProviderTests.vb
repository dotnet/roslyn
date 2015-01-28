' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class GetXmlNamespaceExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New GetXmlNamespaceExpressionSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForGetType()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = GetXmlNamespace($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "GetXmlNamespace([<xmlNamespacePrefix>]) As System.Xml.Linq.XNamespace",
                                     "Returns the System.Xml.Linq.XNamespace object corresponding to the specified XML namespace prefix.",
                                     "The XML namespace prefix to return a System.Xml.Linq.XNamespace object for. If this is omitted, the object for the default XML namespace is returned.",
                                     currentParameterIndex:=0))
            Test(markup, expectedOrderedItems)
        End Sub
    End Class
End Namespace
