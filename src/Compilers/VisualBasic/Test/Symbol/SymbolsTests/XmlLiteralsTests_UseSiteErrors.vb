' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class XmlLiteralTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub XDocumentTypesMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XDocumentTypesMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Private F As Object = <?xml version="1.0"?><x/><?p?>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XDeclaration' from assembly or module 'XDocumentTypesMissing.dll' failed.
    Private F As Object = <?xml version="1.0"?><x/><?p?>
                          ~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'XDocument' from assembly or module 'XDocumentTypesMissing.dll' failed.
    Private F As Object = <?xml version="1.0"?><x/><?p?>
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'XProcessingInstruction' from assembly or module 'XDocumentTypesMissing.dll' failed.
    Private F As Object = <?xml version="1.0"?><x/><?p?>
                                                   ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XCommentTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XCommentTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Private F As Object = <!-- comment -->
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XComment' from assembly or module 'XCommentTypeMissing.dll' failed.
    Private F As Object = <!-- comment -->
                          ~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XElementTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Function Elements(o As Object) As Object
            Return Nothing
        End Function
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Module InternalXmlHelper
    End Module
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XElementTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class C
    Private F1 As XContainer = <x/>
    Private F2 As Object = F1.<x>
    Private F3 As Object = F1.@x
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XElement' from assembly or module 'XElementTypeMissing.dll' failed.
    Private F1 As XContainer = <x/>
                                ~
BC31091: Import of type 'XElement' from assembly or module 'XElementTypeMissing.dll' failed.
    Private F3 As Object = F1.@x
                           ~~~~~
BC36808: XML attributes cannot be selected from type 'XContainer'.
    Private F3 As Object = F1.@x
                           ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XElementConstructorInaccessible()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XName
    End Class
    Public Class XElement
        Friend Sub New(o As Object)
        End Sub
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class C
    Private F1 As XName = Nothing
    Private F2 As Object = <<%= F1 %>/>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30517: Overload resolution failed because no 'New' is accessible.
    Private F2 As Object = <<%= F1 %>/>
                            ~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XAttributeTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
    Public Class XNamespace
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XAttributeTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Private F1 As Object = <x <%= Nothing %>/>
    Private F2 As Object = <x <%= "a" %>="b"/>
    Private F3 As Object = <x a=<%= "b" %>/>
End Class
Namespace My
    Public Module InternalXmlHelper
        Public Function RemoveNamespaceAttributes(prefixes As String(), namespaces As XNamespace(), attributes As Object, o As Object) As Object
            Return Nothing
        End Function
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XAttribute' from assembly or module 'XAttributeTypeMissing.dll' failed.
    Private F2 As Object = <x <%= "a" %>="b"/>
                              ~~~~~~~~~~~~~~
BC30456: 'CreateAttribute' is not a member of 'InternalXmlHelper'.
    Private F3 As Object = <x a=<%= "b" %>/>
                                ~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XAttributeConstructorMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XAttribute
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
    Public Class XNamespace
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XAttributeTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Private F1 As Object = <x <%= Nothing %>/>
    Private F2 As Object = <x <%= "a" %>="b"/>
    Private F3 As Object = <x a=<%= "b" %>/>
End Class
Namespace My
    Public Module InternalXmlHelper
        Public Function RemoveNamespaceAttributes(prefixes As String(), namespaces As XNamespace(), attributes As List(Of XAttribute), o As Object) As Object
            Return Nothing
        End Function
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30057: Too many arguments to 'Public Sub New(o As Object)'.
    Private F2 As Object = <x <%= "a" %>="b"/>
                                         ~~~
BC30456: 'CreateAttribute' is not a member of 'InternalXmlHelper'.
    Private F3 As Object = <x a=<%= "b" %>/>
                                ~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XNameTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XAttribute
        Public Sub New(x As Object, y As Object)
        End Sub
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Module InternalXmlHelper
    End Module
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XNameTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Private F1 As Object = <x/>
    Private F2 As Object = <<%= Nothing %> a="b"/>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XName' from assembly or module 'XNameTypeMissing.dll' failed.
    Private F1 As Object = <x/>
                            ~
BC31091: Import of type 'XName' from assembly or module 'XNameTypeMissing.dll' failed.
    Private F2 As Object = <<%= Nothing %> a="b"/>
                                           ~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XContainerTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XElement
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XAttribute
        Public Sub New(x As Object, y As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Module InternalXmlHelper
    End Module
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XContainerTypeMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class C
    Private F1 As XElement = <x/>
    Private F2 As XElement = <x a="b"/>
    Private F3 As XElement = <x>c</>
    Private F4 As XElement = F1.<x>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XContainer' from assembly or module 'XContainerTypeMissing.dll' failed.
    Private F2 As XElement = <x a="b"/>
                             ~~~~~~~~~~
BC31091: Import of type 'XContainer' from assembly or module 'XContainerTypeMissing.dll' failed.
    Private F3 As XElement = <x>c</>
                             ~~~~~~~
BC31091: Import of type 'XContainer' from assembly or module 'XContainerTypeMissing.dll' failed.
    Private F4 As XElement = F1.<x>
                             ~~~~~~
BC36807: XML elements cannot be selected from type 'XElement'.
    Private F4 As XElement = F1.<x>
                             ~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XContainerMemberNotInvocable()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Add As Object
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Private F As Object = <x>c</>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'Add' is not a member of 'XContainer'.
    Private F As Object = <x>c</>
                          ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XCDataTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="XCDataTypeMissing">
    <file name="c.vb">
Option Strict On
Module M
    Private F As Object = &lt;![CDATA[value]]&gt;
End Module
</file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors>
BC31091: Import of type 'XCData' from assembly or module 'XCDataTypeMissing.dll' failed.
    Private F As Object = &lt;![CDATA[value]]&gt;
                          ~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub XNamespaceTypeMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Namespace System.Xml.Linq
    Public Class XObject
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XNamespaceTypeMissing">
    <file name="c.vb"><![CDATA[
Class C
    Private F = GetXmlNamespace()
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'XNamespace' from assembly or module 'XNamespaceTypeMissing.dll' failed.
    Private F = GetXmlNamespace()
                ~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XNamespaceTypeMissing_2()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
        Public Function Elements(o As Object) As IEnumerable(Of XContainer)
            Return Nothing
        End Function
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XAttribute
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XNamespaceTypeMissing_2">
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Class C
    Private F = <x><%= Nothing %></>
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'InternalXmlHelper' from assembly or module 'XNamespaceTypeMissing_2.dll' failed.
    Private F = <x><%= Nothing %></>
                ~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'XNamespace' from assembly or module 'XNamespaceTypeMissing_2.dll' failed.
    Private F = <x><%= Nothing %></>
                ~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub XNamespaceGetMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
        Public Function Elements(o As Object) As IEnumerable(Of XContainer)
            Return Nothing
        End Function
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XAttribute
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
    Public Class XNamespace
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="XNamespaceGetMissing">
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Shared F As Object = <p:x><%= Nothing %></>
End Class
Namespace My
    Public Module InternalXmlHelper
        Public Function CreateNamespaceAttribute(name As XName, ns As XNamespace) As XAttribute
            Return Nothing
        End Function
        Public Function RemoveNamespaceAttributes(prefixes As String(), namespaces As XNamespace(), attributes As Object, o As Object) As Object
            Return Nothing
        End Function
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'Get' is not a member of 'XNamespace'.
    Shared F As Object = <p:x><%= Nothing %></>
                         ~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ExtensionTypesMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
        Public Sub Add(o As Object)
        End Sub
        Public Function Elements(o As Object) As IEnumerable(Of XContainer)
            Return Nothing
        End Function
    End Class
    Public Class XElement
        Inherits XContainer
        Public Sub New(o As Object)
        End Sub
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="ExtensionTypesMissing">
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class C
    Private F1 As XElement = <x/>
    Private F2 As Object = F1.<x>
    Private F3 As Object = F1.<x>.<y>
    Private F4 As Object = F1.@x
End Class
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31091: Import of type 'Extensions' from assembly or module 'ExtensionTypesMissing.dll' failed.
    Private F3 As Object = F1.<x>.<y>
                           ~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'IEnumerable(Of XContainer)'.
    Private F3 As Object = F1.<x>.<y>
                           ~~~~~~~~~~
BC31091: Import of type 'InternalXmlHelper' from assembly or module 'ExtensionTypesMissing.dll' failed.
    Private F4 As Object = F1.@x
                           ~~~~~
BC36808: XML attributes cannot be selected from type 'XElement'.
    Private F4 As Object = F1.@x
                           ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ExtensionMethodAndPropertyMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XContainer
    End Class
    Public Class XElement
        Inherits XContainer
    End Class
    Public Class XName
        Public Shared Function [Get](localName As String, [namespace] As String) As XName
            Return Nothing
        End Function
    End Class
    Public Module Extensions
    End Module
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class C
    Private F1 As XElement = Nothing
    Private F2 As Object = F1.<x>
    Private F3 As Object = F1.@x
End Class
Namespace My
    Public Module InternalXmlHelper
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'Elements' is not a member of 'XContainer'.
    Private F2 As Object = F1.<x>
                           ~~~~~~
BC36807: XML elements cannot be selected from type 'XElement'.
    Private F2 As Object = F1.<x>
                           ~~~~~~
BC30456: 'AttributeValue' is not a member of 'InternalXmlHelper'.
    Private F3 As Object = F1.@x
                           ~~~~~
BC36808: XML attributes cannot be selected from type 'XElement'.
    Private F3 As Object = F1.@x
                           ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ValueExtensionPropertyMissing()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XElement
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Function F(x As IEnumerable(Of XElement)) As String
        Return x.Value
    End Function
End Class
Namespace My
    Public Module InternalXmlHelper
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
        Return x.Value
               ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ValueExtensionPropertyUnexpectedSignature()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Xml.Linq
Namespace System.Xml.Linq
    Public Class XObject
    End Class
    Public Class XElement
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace
    ]]></file>
</compilation>)
            compilation1.AssertNoErrors()
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Function F(x As IEnumerable(Of XElement)) As String
        Return x.VALUE
    End Function
End Class
Namespace My
    Public Module InternalXmlHelper
        Public ReadOnly Property Value(x As IEnumerable(Of XElement), y As Object, z As Object) As Object
            Get
                Return Nothing
            End Get
        End Property
    End Module
End Namespace
    ]]></file>
</compilation>, references:={New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
        Return x.VALUE
               ~~~~~~~
]]></errors>)
        End Sub

    End Class
End Namespace
