' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class XmlLiteralTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub XComment()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Private F = <!-- comment -->
    Sub Main()
        System.Console.WriteLine("{0}", F)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<!-- comment -->
]]>)
            compilation.VerifyIL("M..cctor", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      " comment "
  IL_0005:  newobj     "Sub System.Xml.Linq.XComment..ctor(String)"
  IL_000a:  stsfld     "M.F As Object"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub XDocument()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub Main()
        Dim x = <?xml version="1.0"?>
                <!-- A -->
                <?p?>
                <x>
                    <!-- B -->
                    <?q?>
                </x>
                <?r?>
                <!-- C-->
        Report(x)
        Report(x.Declaration)
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<!-- A -->
<?p?>
<x>
  <!-- B -->
  <?q?>
</x>
<?r?>
<!-- C-->
<?xml version="1.0"?>
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      172 (0xac)
  .maxstack  6
  IL_0000:  ldstr      "1.0"
  IL_0005:  ldnull
  IL_0006:  ldnull
  IL_0007:  newobj     "Sub System.Xml.Linq.XDeclaration..ctor(String, String, String)"
  IL_000c:  ldnull
  IL_000d:  newobj     "Sub System.Xml.Linq.XDocument..ctor(System.Xml.Linq.XDeclaration, ParamArray Object())"
  IL_0012:  dup
  IL_0013:  ldstr      " A "
  IL_0018:  newobj     "Sub System.Xml.Linq.XComment..ctor(String)"
  IL_001d:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0022:  dup
  IL_0023:  ldstr      "p"
  IL_0028:  ldstr      ""
  IL_002d:  newobj     "Sub System.Xml.Linq.XProcessingInstruction..ctor(String, String)"
  IL_0032:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0037:  dup
  IL_0038:  ldstr      "x"
  IL_003d:  ldstr      ""
  IL_0042:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0047:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_004c:  dup
  IL_004d:  ldstr      " B "
  IL_0052:  newobj     "Sub System.Xml.Linq.XComment..ctor(String)"
  IL_0057:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_005c:  dup
  IL_005d:  ldstr      "q"
  IL_0062:  ldstr      ""
  IL_0067:  newobj     "Sub System.Xml.Linq.XProcessingInstruction..ctor(String, String)"
  IL_006c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0071:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0076:  dup
  IL_0077:  ldstr      "r"
  IL_007c:  ldstr      ""
  IL_0081:  newobj     "Sub System.Xml.Linq.XProcessingInstruction..ctor(String, String)"
  IL_0086:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_008b:  dup
  IL_008c:  ldstr      " C"
  IL_0091:  newobj     "Sub System.Xml.Linq.XComment..ctor(String)"
  IL_0096:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_009b:  dup
  IL_009c:  call       "Sub M.Report(Object)"
  IL_00a1:  callvirt   "Function System.Xml.Linq.XDocument.get_Declaration() As System.Xml.Linq.XDeclaration"
  IL_00a6:  call       "Sub M.Report(Object)"
  IL_00ab:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub AttributeNamespace()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Linq
Imports System.Xml.Linq
Imports <xmlns="http://roslyn/default1">
Class C
    Private Shared F1 As XElement = <x a="1"/>
    Private Shared F2 As XElement = <x xmlns="http://roslyn/default2" a="2"/>
    Private Shared F4 As XElement = <p:x xmlns:p="http://roslyn/p4" a="4"/>
    Private Shared F5 As XElement = <x xmlns="http://roslyn/default5" xmlns:p="http://roslyn/p5" p:a="5"/>
    Shared Sub Main()
        Report(F0)
        Report(F1)
        Report(F2)
        Report(F3)
        Report(F4)
        Report(F5)
    End Sub
    Shared Sub Report(x As XElement)
        Console.WriteLine("{0}", x)
        Dim a = x.Attributes().First(Function(o) o.Name.LocalName = "a")
        Console.WriteLine("{0}, {1}", x.Name, a.Name)
    End Sub
End Class
]]>
    </file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Partial Class C
    Private Shared F0 As XElement = <x a="0"/>
    Private Shared F3 As XElement = <p:x xmlns:p="http://roslyn/p3" a="3"/>
End Class
]]>
    </file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<x a="0" />
x, a
<x a="1" xmlns="http://roslyn/default1" />
{http://roslyn/default1}x, a
<x xmlns="http://roslyn/default2" a="2" />
{http://roslyn/default2}x, a
<p:x xmlns:p="http://roslyn/p3" a="3" />
{http://roslyn/p3}x, a
<p:x xmlns:p="http://roslyn/p4" a="4" />
{http://roslyn/p4}x, a
<x xmlns="http://roslyn/default5" xmlns:p="http://roslyn/p5" p:a="5" />
{http://roslyn/default5}x, {http://roslyn/p5}a
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccess()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Imports <xmlns:r1="http://roslyn">
Module M
    Sub Main()
        Dim x = <a xmlns:r2="http://roslyn" r1:b="a.b">
                    <b>1</b>
                    <r2:c>2</r2:c>
                    <c d="c.d">3</c>
                    <b>4</b>
                    <b/>
                </a>
        Report(x.<b>)
        Report(x.<c>)
        Report(x.<r1:c>)
        Report(x.@r1:b)
        Report(x.@xmlns:r2)
    End Sub
    Sub Report(x As IEnumerable(Of XElement))
        For Each e In x
            Console.WriteLine("{0}", e.Value)
            Dim a = e.@d
            If a IsNot Nothing Then
                Console.WriteLine("  {0}", a)
            End If
        Next
    End Sub
    Sub Report(s As String)
        Console.WriteLine("{0}", s)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
1
4

3
  c.d
2
a.b
http://roslyn
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      453 (0x1c5)
  .maxstack  6
  IL_0000:  ldstr      "a"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "r2"
  IL_001a:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "http://roslyn"
  IL_0029:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  dup
  IL_0034:  ldstr      "b"
  IL_0039:  ldstr      "http://roslyn"
  IL_003e:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0043:  ldstr      "a.b"
  IL_0048:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_004d:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0052:  dup
  IL_0053:  ldstr      "b"
  IL_0058:  ldstr      ""
  IL_005d:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0062:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0067:  dup
  IL_0068:  ldstr      "1"
  IL_006d:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0072:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0077:  dup
  IL_0078:  ldstr      "c"
  IL_007d:  ldstr      "http://roslyn"
  IL_0082:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0087:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_008c:  dup
  IL_008d:  ldstr      "2"
  IL_0092:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0097:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_009c:  dup
  IL_009d:  ldstr      "c"
  IL_00a2:  ldstr      ""
  IL_00a7:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00ac:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00b1:  dup
  IL_00b2:  ldstr      "d"
  IL_00b7:  ldstr      ""
  IL_00bc:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00c1:  ldstr      "c.d"
  IL_00c6:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_00cb:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00d0:  dup
  IL_00d1:  ldstr      "3"
  IL_00d6:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00db:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00e0:  dup
  IL_00e1:  ldstr      "b"
  IL_00e6:  ldstr      ""
  IL_00eb:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00f0:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00f5:  dup
  IL_00f6:  ldstr      "4"
  IL_00fb:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0100:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0105:  dup
  IL_0106:  ldstr      "b"
  IL_010b:  ldstr      ""
  IL_0110:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0115:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_011a:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_011f:  dup
  IL_0120:  ldstr      "r1"
  IL_0125:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_012a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_012f:  ldstr      "http://roslyn"
  IL_0134:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0139:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_013e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0143:  dup
  IL_0144:  ldstr      "b"
  IL_0149:  ldstr      ""
  IL_014e:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0153:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0158:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_015d:  dup
  IL_015e:  ldstr      "c"
  IL_0163:  ldstr      ""
  IL_0168:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_016d:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0172:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_0177:  dup
  IL_0178:  ldstr      "c"
  IL_017d:  ldstr      "http://roslyn"
  IL_0182:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0187:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_018c:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_0191:  dup
  IL_0192:  ldstr      "b"
  IL_0197:  ldstr      "http://roslyn"
  IL_019c:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_01a1:  call       "Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String"
  IL_01a6:  call       "Sub M.Report(String)"
  IL_01ab:  ldstr      "r2"
  IL_01b0:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_01b5:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_01ba:  call       "Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String"
  IL_01bf:  call       "Sub M.Report(String)"
  IL_01c4:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccess_DistinctDefaultNamespaces()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Partial Class C
    Private Shared F1 As XElement = <x xmlns="http://roslyn/p" a="a1" p:b="b1"/>
    Private Shared F2 As XElement = <p:x a="a1" q:b="b1"/>
    Shared Sub Main()
        Report(F1)
        Report(F2)
        Report(F3)
        Report(F4)
    End Sub
    Private Shared Sub Report(x As XElement)
        Console.WriteLine("{0}", x)
        Report(x.@<a>)
        Report(x.@<p:a>)
        Report(x.@<q:a>)
        Report(x.@<b>)
        Report(x.@<p:b>)
        Report(x.@<q:b>)
    End Sub
    Private Shared Sub Report(s As String)
        Console.WriteLine("{0}", If(s, "[none]"))
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Imports <xmlns:p1="http://roslyn/p">
Imports <xmlns:p2="http://roslyn/q">
Partial Class C
    Private Shared F3 As XElement = <x xmlns="http://roslyn/q" a="a2" p1:b="b2"/>
    Private Shared F4 As XElement = <p1:x a="a2" p2:b="b2"/>
End Class
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<p:x xmlns="http://roslyn/p" a="a1" p:b="b1" xmlns:p="http://roslyn/p" />
a1
[none]
[none]
[none]
b1
[none]
<p:x a="a1" q:b="b1" xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p" />
a1
[none]
[none]
[none]
[none]
b1
<x xmlns="http://roslyn/q" a="a2" p1:b="b2" xmlns:p1="http://roslyn/p" />
a2
[none]
[none]
[none]
b2
[none]
<p1:x a="a2" p2:b="b2" xmlns:p2="http://roslyn/q" xmlns:p1="http://roslyn/p" />
a2
[none]
[none]
[none]
[none]
b2
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessXmlns()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Linq
Module M
    Sub Main()
        Console.WriteLine("{0}", <x/>.<xmlns>.Count())
        Console.WriteLine("{0}", <x/>.@xmlns)
        Console.WriteLine("{0}", <x xmlns="http://roslyn/default"/>.@xmlns)
        Console.WriteLine("{0}", <x xmlns:p="http://roslyn/p"/>.@<xmlns:p>)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
0

http://roslyn/default
http://roslyn/p
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessXmlns_DistinctDefaultNamespaces()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Imports <xmlns="http://roslyn/default1">
Imports <xmlns:default1="http://roslyn/default1">
Imports <xmlns:default2="http://roslyn/default2">
Partial Class C
    Private Shared F1 As XElement = <x xmlns="http://roslyn/1" xmlns:p="http://roslyn/p"/>
    Shared Sub Main()
        Report(F1)
        Report(F2)
    End Sub
    Private Shared Sub Report(x As XElement)
        Console.WriteLine("{0}", x)
        Report(x.@<xmlns>)
        Report(x.@<default1:xmlns>)
        Report(x.@<default2:xmlns>)
    End Sub
    Private Shared Sub Report(s As String)
        Console.WriteLine("{0}", If(s, "[none]"))
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Imports <xmlns="http://roslyn/default2">
Partial Class C
    Private Shared F2 As XElement = <x xmlns="http://roslyn/2" xmlns:q="http://roslyn/q"/>
End Class
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<x xmlns="http://roslyn/1" xmlns:p="http://roslyn/p" />
http://roslyn/1
[none]
[none]
<x xmlns="http://roslyn/2" xmlns:q="http://roslyn/q" />
http://roslyn/2
[none]
[none]
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessIEnumerableOfXElement()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Module M
    Property P1 As XElement = <a><b c="1"/><b c="2"/></a>
    Property P2 As IEnumerable(Of XElement) = P1.<b>
    Property P3 As XElement = <a>
                                  <b><c>1</c></b>
                                  <b><c>2</c></b>
                              </a>
    Property P4 As IEnumerable(Of XElement) = P3.<b>
    Sub Main()
        Report(P1.<b>.@c)
        Report(P2.@c)
        Report(P3.<b>.<c>)
        Report(P4.<c>)
    End Sub
    Sub Report(s As String)
        Console.WriteLine("{0}", s)
    End Sub
    Sub Report(c As IEnumerable(Of XElement))
        For Each o In c
            Console.WriteLine("{0}", o)
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
1
1
<c>1</c>
<c>2</c>
<c>1</c>
<c>2</c>
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      161 (0xa1)
  .maxstack  3
  IL_0000:  call       "Function M.get_P1() As System.Xml.Linq.XElement"
  IL_0005:  ldstr      "b"
  IL_000a:  ldstr      ""
  IL_000f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0014:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0019:  ldstr      "c"
  IL_001e:  ldstr      ""
  IL_0023:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0028:  call       "Function My.InternalXmlHelper.get_AttributeValue(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), System.Xml.Linq.XName) As String"
  IL_002d:  call       "Sub M.Report(String)"
  IL_0032:  call       "Function M.get_P2() As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0037:  ldstr      "c"
  IL_003c:  ldstr      ""
  IL_0041:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0046:  call       "Function My.InternalXmlHelper.get_AttributeValue(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), System.Xml.Linq.XName) As String"
  IL_004b:  call       "Sub M.Report(String)"
  IL_0050:  call       "Function M.get_P3() As System.Xml.Linq.XElement"
  IL_0055:  ldstr      "b"
  IL_005a:  ldstr      ""
  IL_005f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0064:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0069:  ldstr      "c"
  IL_006e:  ldstr      ""
  IL_0073:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0078:  call       "Function System.Xml.Linq.Extensions.Elements(Of System.Xml.Linq.XElement)(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_007d:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_0082:  call       "Function M.get_P4() As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0087:  ldstr      "c"
  IL_008c:  ldstr      ""
  IL_0091:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0096:  call       "Function System.Xml.Linq.Extensions.Elements(Of System.Xml.Linq.XElement)(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_009b:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_00a0:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DescendantAccess()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Module M
    Sub Main()
        M(<a>
              <c>1</>
              <b>
                  <q:c>2</>
                  <p3:c xmlns:p3="http://roslyn/p">3</>
              </b>
              <b>
                  <c>4</>
                  <p5:c xmlns:p5="http://roslyn/p">5</>
              </b>
          </a>)
    End Sub
    Sub M(x As XElement)
        Report(x...<c>)
        Report(x...<b>...<p:c>)
    End Sub
    Sub Report(c As IEnumerable(Of XElement))
        For Each x In c
            Console.WriteLine("{0}", x)
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<c>1</c>
<c>4</c>
<p3:c xmlns:p3="http://roslyn/p">3</p3:c>
<p5:c xmlns:p5="http://roslyn/p">5</p5:c>
]]>)
            compilation.VerifyIL("M.M", <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "c"
  IL_0006:  ldstr      ""
  IL_000b:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0010:  callvirt   "Function System.Xml.Linq.XContainer.Descendants(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0015:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_001a:  ldarg.0
  IL_001b:  ldstr      "b"
  IL_0020:  ldstr      ""
  IL_0025:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_002a:  callvirt   "Function System.Xml.Linq.XContainer.Descendants(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_002f:  ldstr      "c"
  IL_0034:  ldstr      "http://roslyn/p"
  IL_0039:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_003e:  call       "Function System.Xml.Linq.Extensions.Descendants(Of System.Xml.Linq.XElement)(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement), System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0043:  call       "Sub M.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_0048:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessReceiverNotRValue()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Module M
    ReadOnly Property P As XElement
        Get
            Return Nothing
        End Get
    End Property
    WriteOnly Property Q As XElement
        Set(value As XElement)
        End Set
    End Property
    Sub M()
        Dim o As Object
        o = P.<x>
        o = P.@x
        o = Q.<x>
        o = Q.@x
        With P
            o = ...<x>
            .@<a> = "b"
        End With
        With Q
            o = ...<y>
            .@<c> = "d"
        End With
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30524: Property 'Q' is 'WriteOnly'.
        o = Q.<x>
            ~
BC30524: Property 'Q' is 'WriteOnly'.
        o = Q.@x
            ~
BC30524: Property 'Q' is 'WriteOnly'.
        With Q
             ~
]]></errors>)
        End Sub

        ' Should not report cascading member access errors
        ' if the receiver is an error type.
        <Fact()>
        Public Sub MemberAccessUnknownReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M()
        Dim x As C = Nothing
        Dim y As Object
        y = x.<y>
        y = x...<y>
        y = x.@a
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'C' is not defined.
        Dim x As C = Nothing
                 ~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub MemberAccessUntypedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M()
        Dim o As Object
        o = Nothing.<x>
        o = Nothing.@a
        o = (Function() Nothing)...<x>
        o = (Function() Nothing).@<a>
        With Nothing
            o = .<x>
        End With
        With (Function() Nothing)
            .@a = "b"
        End With
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31168: XML axis properties do not support late binding.
        o = Nothing.<x>
            ~~~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        o = Nothing.@a
            ~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'Function <generated method>() As Object'.
        o = (Function() Nothing)...<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'Function <generated method>() As Object'.
        o = (Function() Nothing).@<a>
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31168: XML axis properties do not support late binding.
            o = .<x>
                ~~~~
BC36808: XML attributes cannot be selected from type 'Function <generated method>() As Object'.
            .@a = "b"
            ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub MemberAccessImplicitReceiver()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Module M
    Function F1(x As XElement) As IEnumerable(Of XElement)
        With x
            Return .<y>
        End With
    End Function
    Function F2(x As XElement) As IEnumerable(Of XElement)
        With x
            Return ...<y>
        End With
    End Function
    Function F3(x As XElement) As Object
        With x
            Return .@a
        End With
    End Function
    Function F4(x As XElement) As Object
        With x
            .@<b> = .@<a>
        End With
        Return x
    End Function
    Sub Main()
        Console.WriteLine("{0}", F1(<x1><y/></x1>).FirstOrDefault())
        Console.WriteLine("{0}", F2(<x2><y/></x2>).FirstOrDefault())
        Console.WriteLine("{0}", F3(<x3 a="1"/>))
        Console.WriteLine("{0}", F4(<x4 a="2"/>))
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<y />
<y />
1
<x4 a="2" b="2" />
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessImplicitReceiverLambda()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Class C
    Implements IEnumerable(Of XElement)
    Private value As IEnumerable(Of XElement) = {<x a="1"><y/></x>}
    Public Function F1() As IEnumerable(Of XElement)
        With Me
            Return (Function() .<y>)()
        End With
    End Function
    Public Function F2() As IEnumerable(Of XElement)
        With Me
            Return (Function() ...<y>)()
        End With
    End Function
    Public Function F3() As Object
        With Me
            Return (Function() .@a)()
        End With
    End Function
    Public Function F4() As IEnumerable(Of XElement)
        With Me
            Dim a As Action = Sub() .@<b> = .@<a>
            a()
        End With
        Return Me
    End Function
    Private Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return value.GetEnumerator()
    End Function
    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
Module M
    Sub Main()
        Dim o As New C()
        Console.WriteLine("{0}", o.F1().FirstOrDefault())
        Console.WriteLine("{0}", o.F2().FirstOrDefault())
        Console.WriteLine("{0}", o.F3())
        Console.WriteLine("{0}", o.F4().FirstOrDefault())
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<y />
<y />
1
<x a="1" b="1">
  <y />
</x>
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessImplicitReceiverLambdaCannotLiftMe()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Xml.Linq
Structure S
    Implements IEnumerable(Of XElement)
    Public Function F1() As IEnumerable(Of XElement)
        With Me
            Return (Function() .<y>)()
        End With
    End Function
    Public Function F2() As IEnumerable(Of XElement)
        With Me
            Return (Function() ...<y>)()
        End With
    End Function
    Public Function F3() As Object
        With Me
            Return (Function() .@a)()
        End With
    End Function
    Public Function F4() As IEnumerable(Of XElement)
        With Me
            Dim a As Action = Sub() .@<b> = .@<a>
            a()
        End With
        Return Me
    End Function
    Private Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return Nothing
    End Function
    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
            Return (Function() .<y>)()
                               ~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
            Return (Function() ...<y>)()
                               ~~~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
            Return (Function() .@a)()
                               ~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
            Dim a As Action = Sub() .@<b> = .@<a>
                                    ~~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
            Dim a As Action = Sub() .@<b> = .@<a>
                                            ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub MemberAccessIncludeNamespaces()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="http://roslyn">
Module M
    Sub Main()
        Dim x = <pa:a xmlns:pa="http://roslyn">
                    <pb:b xmlns:pb="http://roslyn"/>
                    <pa:c/>
                    <p:d/>
                </pa:a>
        Report(x.<p:b>)
        Report(x.<p:c>)
        Report(x.<p:d>)
    End Sub
    Sub Report(c As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))
        For Each x In c
            System.Console.WriteLine("{0}", x)
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<pb:b xmlns:pb="http://roslyn" />
<pa:c xmlns:pa="http://roslyn" />
<pa:d xmlns:pa="http://roslyn" />
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAssignment()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Module M
    Sub M()
        Dim x = <x/>
        x.<y> = Nothing
        x.<y>.<z> = Nothing
        x...<y> = Nothing
        x...<y>...<z> = Nothing
        N(x.<y>)
        N(x.<y>.<z>)
        N(x...<y>)
        N(x...<y>...<z>)
    End Sub
    Sub N(ByRef o As IEnumerable(Of XElement))
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x.<y> = Nothing
        ~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x.<y>.<z> = Nothing
        ~~~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x...<y> = Nothing
        ~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x...<y>...<z> = Nothing
        ~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAttributeAssignment()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:y="http://roslyn">
Module M
    Sub Main()
        Dim x = <x a="1"><y/><y/></x>
        Report(x)
        x.@a = "2"
        x.<y>.@y:b = "3"
        N(x.@c, "4")
        N(x.<y>.@y:d, "5")
        Report(x)
    End Sub
    Sub N(ByRef x As String, y As String)
        x = y
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<x a="1">
  <y />
  <y />
</x>
<x a="2" c="4">
  <y p2:b="3" p2:d="5" xmlns:p2="http://roslyn" />
  <y />
</x>
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAttributeCompoundAssignment()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Module M
    Sub Main()
        Dim x = <x a="1"/>
        x.@a += "2"
        x.@b += "3"
        System.Console.WriteLine("{0}", x)
    End Sub
End Module
]]>
    </file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
<x a="12" b="3" />
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAttributeAsReceiver()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub Main()
        System.Console.WriteLine("{0}", <x a="b"/>.@a.ToString())
        System.Console.WriteLine("{0}", <x><y>c</y></x>.<y>.Value)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
b
c
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAttributeByRefExtensionMethod()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Module M
    Sub Main()
        Dim x = <x a="1"/>
        x.@a.M("2")
        System.Console.WriteLine("{0}", x)
    End Sub
    <Extension()>
    Sub M(ByRef x As String, y As String)
        x = y
    End Sub
End Module
]]>
    </file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x a="2" />
]]>)
        End Sub

        <Fact()>
        Public Sub MemberAccessAttributeAddressOf()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Module M
    Sub Main()
        Dim d As Func(Of String) = AddressOf <x a="b"/>.@a.ToString
        Console.WriteLine("{0}", d())
    End Sub
End Module
]]>
    </file>
</compilation>, references:=Net40XmlReferences, expectedOutput:="b")
        End Sub

        ' Project-level imports should be used if file-level
        ' imports do not contain namespace.
        <Fact()>
        Public Sub ProjectImports()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"<xmlns=""default1"">", "<xmlns:p=""p1"">", "<xmlns:q=""q1"">"}))
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Xml.Linq
Imports <xmlns:q="q2">
Class C
    Shared Sub Main()
        Dim x = <a>
                    <b/>
                    <p:b/>
                    <q:b/>
                </a>
        Report(x.<b>)
        Report(x.<p:b>)
        Report(x.<q:b>)
    End Sub
    Shared Sub Report(c As IEnumerable(Of XElement))
        For Each i In c
            Console.WriteLine(i.Name.Namespace)
        Next
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=options, expectedOutput:=<![CDATA[
default1
p1
q2
]]>)
            compilation.VerifyIL("C.Main", <![CDATA[
{
  // Code size      284 (0x11c)
  .maxstack  4
  IL_0000:  ldstr      "a"
  IL_0005:  ldstr      "default1"
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "b"
  IL_001a:  ldstr      "default1"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0029:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_002e:  dup
  IL_002f:  ldstr      "b"
  IL_0034:  ldstr      "p1"
  IL_0039:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_003e:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0043:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0048:  dup
  IL_0049:  ldstr      "b"
  IL_004e:  ldstr      "q2"
  IL_0053:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0058:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_005d:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0062:  dup
  IL_0063:  ldstr      "q"
  IL_0068:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_006d:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0072:  ldstr      "q2"
  IL_0077:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_007c:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_0081:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0086:  dup
  IL_0087:  ldstr      "p"
  IL_008c:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_0091:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0096:  ldstr      "p1"
  IL_009b:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_00a0:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_00a5:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00aa:  dup
  IL_00ab:  ldstr      "xmlns"
  IL_00b0:  ldstr      ""
  IL_00b5:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00ba:  ldstr      "default1"
  IL_00bf:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_00c4:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_00c9:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00ce:  dup
  IL_00cf:  ldstr      "b"
  IL_00d4:  ldstr      "default1"
  IL_00d9:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00de:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_00e3:  call       "Sub C.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_00e8:  dup
  IL_00e9:  ldstr      "b"
  IL_00ee:  ldstr      "p1"
  IL_00f3:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00f8:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_00fd:  call       "Sub C.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_0102:  ldstr      "b"
  IL_0107:  ldstr      "q2"
  IL_010c:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0111:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0116:  call       "Sub C.Report(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement))"
  IL_011b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitXmlnsAttributes()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"<xmlns=""http://roslyn"">", "<xmlns:p=""http://roslyn/p"">"}))
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports <xmlns:P="http://roslyn/P">
Imports <xmlns:q="http://roslyn/p">
Module M
    Private F1 As Object = <x><p:y/></x>
    Private F2 As Object = <x><p:y/><P:z/></x>
    Private F3 As Object = <p:x><q:y/></p:x>
    Sub Main()
        Console.WriteLine("{0}", F1)
        Console.WriteLine("{0}", F2)
        Console.WriteLine("{0}", F3)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=options, expectedOutput:=<![CDATA[
<x xmlns:p="http://roslyn/p" xmlns="http://roslyn">
  <p:y />
</x>
<x xmlns:P="http://roslyn/P" xmlns:p="http://roslyn/p" xmlns="http://roslyn">
  <p:y />
  <P:z />
</x>
<p:x xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/p">
  <p:y />
</p:x>
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitXmlnsAttributes_DefaultAndEmpty()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns="http://roslyn/default">
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="">
Module M
    Sub Main()
        Report(<p:x>
                   <y/>
                   <q:z/>
               </p:x>)
    End Sub 
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<p:x xmlns="http://roslyn/default" xmlns:p="http://roslyn/p">
  <y />
  <z xmlns="" />
</p:x>
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      150 (0x96)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      "http://roslyn/p"
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "y"
  IL_001a:  ldstr      "http://roslyn/default"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0029:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_002e:  dup
  IL_002f:  ldstr      "z"
  IL_0034:  ldstr      ""
  IL_0039:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_003e:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0043:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0048:  dup
  IL_0049:  ldstr      "xmlns"
  IL_004e:  ldstr      ""
  IL_0053:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0058:  ldstr      "http://roslyn/default"
  IL_005d:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0062:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_0067:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_006c:  dup
  IL_006d:  ldstr      "p"
  IL_0072:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_0077:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_007c:  ldstr      "http://roslyn/p"
  IL_0081:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0086:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_008b:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0090:  call       "Sub M.Report(Object)"
  IL_0095:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitXmlnsAttributes_EmbeddedExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports <xmlns="http://roslyn/default">
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Module M
    Private F0 As Object = <p:y/>
    Private F1 As Object = <x><%= <p:y1/> %><%= <p:y2/> %><%= <q:y3/> %></x>
    Private F2 As Object = <x><%= F0 %></x>
    Private F3 As Object = <p:x><%= <<%= <q:y/> %>/> %></p:x>
    Private F4 As Object = <p:x><%= (Function() <y/>)() %></p:x>
    Sub Main()
        Console.WriteLine("{0}", F1)
        Console.WriteLine("{0}", F2)
        Console.WriteLine("{0}", F3)
        Console.WriteLine("{0}", F4)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns="http://roslyn/default" xmlns:p="http://roslyn/p" xmlns:q="http://roslyn/q">
  <p:y1 />
  <p:y2 />
  <q:y3 />
</x>
<x xmlns="http://roslyn/default" xmlns:p="http://roslyn/p">
  <p:y />
</x>
<p:x xmlns:p="http://roslyn/p">
  <q:y xmlns:q="http://roslyn/q" />
</p:x>
<p:x xmlns:p="http://roslyn/p" xmlns="http://roslyn/default">
  <y />
</p:x>
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitXmlnsAttributes_EmbeddedExpressions_2()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Imports <xmlns:p1="http://roslyn/1">
Imports <xmlns:p2="http://roslyn/2">
Module M
    Sub Main()
        ' Same xmlns merged from sibling children.
        Report(<a>
                   <b>
                       <%= F(<c>
                                 <p1:d/>
                             </c>) %>
                   </b>
                   <b>
                       <%= F(<c>
                                 <p1:d/>
                                 <p2:d/>
                             </c>) %>
                   </b>
               </a>)
        ' Different xmlns at sibling scopes.
        Report(<a>
                   <b xmlns:p1="http://roslyn/3">
                       <%= F(<c>
                                 <p1:d/>
                                 <p2:d/>
                             </c>) %>
                   </b>
                   <b xmlns:p2="http://roslyn/4">
                       <%= F(<c>
                                 <p1:d/>
                                 <p2:d/>
                             </c>) %>
                   </b>
               </a>)
        ' Different xmlns at nested scopes. Dev11: "Duplicate attribute" exception.
        Report(<a xmlns:p1="http://roslyn/3">
                   <b xmlns:p2="http://roslyn/4">
                       <%= F(<c>
                                 <p1:d/>
                                 <p2:d/>
                             </c>) %>
                   </b>
               </a>)
    End Sub
    Function F(x As XElement) As XElement
        Return x
    End Function
    Sub Report(x As XElement)
        System.Console.WriteLine("{0}", x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<a xmlns:p1="http://roslyn/1" xmlns:p2="http://roslyn/2">
  <b>
    <c>
      <p1:d />
    </c>
  </b>
  <b>
    <c>
      <p1:d />
      <p2:d />
    </c>
  </b>
</a>
<a xmlns:p2="http://roslyn/2" xmlns:p1="http://roslyn/1">
  <b xmlns:p1="http://roslyn/3">
    <c xmlns:p1="http://roslyn/1">
      <p1:d />
      <p2:d />
    </c>
  </b>
  <b xmlns:p2="http://roslyn/4">
    <c xmlns:p2="http://roslyn/2">
      <p1:d />
      <p2:d />
    </c>
  </b>
</a>
<a xmlns:p1="http://roslyn/3">
  <b xmlns:p2="http://roslyn/4">
    <c xmlns:p2="http://roslyn/2" xmlns:p1="http://roslyn/1">
      <p1:d />
      <p2:d />
    </c>
  </b>
</a>
]]>)
        End Sub

        ' Embedded expression from separate file with distinct default namespaces.
        <Fact()>
        Public Sub ImplicitXmlnsAttributes_DistinctDefaultNamespaces()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Partial Class C
    Private Shared F1 As Object = <x>
                                      <%= E1() %>
                                      <%= E2() %>
                                      <%= E3() %>
                                  </x>
    Private Shared Function E1() As Object
        Return <y/>
    End Function
    Shared Sub Main()
        Console.WriteLine("{0}", F1)
        Console.WriteLine("{0}", F2)
        Console.WriteLine("{0}", F3)
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports <xmlns="http://roslyn/2">
Partial Class C
    Private Shared F2 As Object = <x>
                                      <%= E1() %>
                                      <%= E2() %>
                                      <%= E3() %>
                                  </x>
    Private Shared Function E2() As Object
        Return <y/>
    End Function
End Class
    ]]></file>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns="http://roslyn/3">
Partial Class C
    Private Shared F3 As Object = <x>
                                      <%= E1() %>
                                      <%= E2() %>
                                      <%= E3() %>
                                  </x>
    Private Shared Function E3() As Object
        Return <y/>
    End Function
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x>
  <y />
  <y xmlns="http://roslyn/2" />
  <y xmlns="http://roslyn/3" />
</x>
<x xmlns="http://roslyn/2">
  <y xmlns="" />
  <y />
  <y xmlns="http://roslyn/3" />
</x>
<x xmlns="http://roslyn/3">
  <y xmlns="" />
  <y xmlns="http://roslyn/2" />
  <y />
</x>
]]>)
        End Sub

        ' Embedded expression from separate file with distinct Imports.
        <Fact()>
        Public Sub ImplicitXmlnsAttributes_DistinctImports()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q1">
Imports <xmlns:r="http://roslyn/r">
Module M
    Public F As Object = <x>
                             <y>
                                 <p:z q:a="b" r:c="d"/>
                                 <%= N.F %>
                             </y>
                         </x>
    Sub Main()
        System.Console.WriteLine("{0}", F)
    End Sub
End Module
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q2">
Imports <xmlns:s="http://roslyn/r">
Module N
    Public F As Object = <p:z q:a="b" s:c="d"/>
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:r="http://roslyn/r" xmlns:q="http://roslyn/q1" xmlns:p="http://roslyn/p" xmlns:s="http://roslyn/r">
  <y>
    <p:z q:a="b" s:c="d" />
    <p:z q:a="b" s:c="d" xmlns:q="http://roslyn/q2" />
  </y>
</x>
]]>)
        End Sub

        ' Embedded expression other than as XElement content.
        ' (Dev11 does not merge namespaces in <x <%= F %>/>
        ' although Roslyn does.)
        <Fact()>
        Public Sub ImplicitXmlnsAttributes_EmbeddedExpressionOutsideContent()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/">
Module M
    Private F1 As Object = <x <%= <p:y/> %>/>
    Private F2 As Object = <<%= <p:y/> %>/>
    Private F3 As Object = <?xml version="1.0"?><%= <p:y/> %>
    Sub Main()
        System.Console.WriteLine("{0}", F1)
        System.Console.WriteLine("{0}", F2)
        System.Console.WriteLine("{0}", F3)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:p="http://roslyn/">
  <p:y />
</x>
<p:y xmlns:p="http://roslyn/" />
<p:y xmlns:p="http://roslyn/" />
]]>)
        End Sub

        ' Opaque embedded expression and XML literal embedded expression
        ' with duplicate namespace references. (Dev11 generates code
        ' that throws InvalidOperationException: "Duplicate attribute".)
        <Fact()>
        Public Sub ImplicitXmlnsAttributes_EmbeddedExpressionAndEmbeddedLiteral()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Module M
    Private F1 As Object = <x>
                               <%= F() %>
                               <%= <p:z/> %>
                           </x>
    Private F2 As Object = <x>
                               <%= <p:y><%= F() %></p:y> %>
                           </x>
    Function F() As Object
        Return <p:y q:a="b"/>
    End Function
    Sub Main()
        System.Console.WriteLine("{0}", F1)
        System.Console.WriteLine("{0}", F2)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p">
  <p:y q:a="b" />
  <p:z />
</x>
<x xmlns:p="http://roslyn/p" xmlns:q="http://roslyn/q">
  <p:y>
    <p:y q:a="b" />
  </p:y>
</x>
]]>)
        End Sub

        ' InternalXmlHelper.RemoveNamespaceAttributes() modifies
        ' the embedded expression argument so subsequent uses of
        ' the expression may give different results.
        <WorkItem(529410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529410")>
        <Fact()>
        Public Sub ImplicitXmlnsAttributes_SideEffects()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Module M
    Private F1 As Object = <q:y/>
    Private F2 As Object = <p:x><%= F1 %></p:x>
    Private F3 As Object = <p:x><%= F1 %></p:x>
    Sub Main()
        System.Console.WriteLine("{0}", F2)
        System.Console.WriteLine("{0}", F3)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<p:x xmlns:p="http://roslyn/p" xmlns:q="http://roslyn/q">
  <q:y />
</p:x>
<p:x xmlns:p="http://roslyn/p">
  <y xmlns="http://roslyn/q" />
</p:x>
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedStringNameExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Module M
    Private F As String = "x"
    Private X As XElement = <<%= F %> <%= F %>="..."/>
    Sub Main()
        Console.WriteLine("{0}", X)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x x="..." />
]]>)
            compilation.VerifyIL("M..cctor", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  stsfld     "M.F As String"
  IL_000a:  ldsfld     "M.F As String"
  IL_000f:  call       "Function System.Xml.Linq.XName.op_Implicit(String) As System.Xml.Linq.XName"
  IL_0014:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0019:  dup
  IL_001a:  ldsfld     "M.F As String"
  IL_001f:  call       "Function System.Xml.Linq.XName.op_Implicit(String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "..."
  IL_0029:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  stsfld     "M.X As System.Xml.Linq.XElement"
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedXNameExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Module M
    Private F As XName = XName.Get("x", "")
    Private G As XName = XName.Get("y", "http://roslyn")
    Private H As XName = XName.Get("z", "")
    Private X As XElement = <<%= F %> <%= F %>="..."/>
    Private Y As XElement = <<%= G %> <%= G %>="..."/>
    Private Z As XElement = <<%= H %> <%= H %>="..."><%= H %></>
    Sub Main()
        Console.WriteLine("{0}", X)
        Console.WriteLine("{0}", Y)
        Console.WriteLine("{0}", Z)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x x="..." />
<y p1:y="..." xmlns:p1="http://roslyn" xmlns="http://roslyn" />
<z z="...">z</z>
]]>)
            compilation.VerifyIL("M..cctor", <![CDATA[
{
  // Code size      180 (0xb4)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  stsfld     "M.F As System.Xml.Linq.XName"
  IL_0014:  ldstr      "y"
  IL_0019:  ldstr      "http://roslyn"
  IL_001e:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0023:  stsfld     "M.G As System.Xml.Linq.XName"
  IL_0028:  ldstr      "z"
  IL_002d:  ldstr      ""
  IL_0032:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0037:  stsfld     "M.H As System.Xml.Linq.XName"
  IL_003c:  ldsfld     "M.F As System.Xml.Linq.XName"
  IL_0041:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0046:  dup
  IL_0047:  ldsfld     "M.F As System.Xml.Linq.XName"
  IL_004c:  ldstr      "..."
  IL_0051:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_0056:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_005b:  stsfld     "M.X As System.Xml.Linq.XElement"
  IL_0060:  ldsfld     "M.G As System.Xml.Linq.XName"
  IL_0065:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_006a:  dup
  IL_006b:  ldsfld     "M.G As System.Xml.Linq.XName"
  IL_0070:  ldstr      "..."
  IL_0075:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_007a:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_007f:  stsfld     "M.Y As System.Xml.Linq.XElement"
  IL_0084:  ldsfld     "M.H As System.Xml.Linq.XName"
  IL_0089:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_008e:  dup
  IL_008f:  ldsfld     "M.H As System.Xml.Linq.XName"
  IL_0094:  ldstr      "..."
  IL_0099:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_009e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00a3:  dup
  IL_00a4:  ldsfld     "M.H As System.Xml.Linq.XName"
  IL_00a9:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00ae:  stsfld     "M.Z As System.Xml.Linq.XElement"
  IL_00b3:  ret
}
]]>)
        End Sub

        ' Use InternalXmlHelper.CreateAttribute to generate attributes
        ' with embedded expression values since CreateAttribute will
        ' handle Nothing value. Otherwise, use New XAttribute().
        <Fact()>
        Public Sub EmbeddedAttributeValueExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Linq
Imports System.Xml.Linq
Module M
    Sub Main()
        Report(<x a1="b1"/>)
        Report(<x a2=<%= "b2" %>/>)
        Report(<x a3=<%= 3 %>/>)
        Report(<x a4=<%= Nothing %>/>)
    End Sub
    Sub Report(x As XElement)
        System.Console.WriteLine("[{0}] {1}", x.Attributes.Count(), x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
[1] <x a1="b1" />
[1] <x a2="b2" />
[1] <x a3="3" />
[0] <x />
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      222 (0xde)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "a1"
  IL_001a:  ldstr      ""
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "b1"
  IL_0029:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0038:  ldstr      "x"
  IL_003d:  ldstr      ""
  IL_0042:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0047:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_004c:  dup
  IL_004d:  ldstr      "a2"
  IL_0052:  ldstr      ""
  IL_0057:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_005c:  ldstr      "b2"
  IL_0061:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_0066:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_006b:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0070:  ldstr      "x"
  IL_0075:  ldstr      ""
  IL_007a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_007f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0084:  dup
  IL_0085:  ldstr      "a3"
  IL_008a:  ldstr      ""
  IL_008f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0094:  ldc.i4.3
  IL_0095:  box        "Integer"
  IL_009a:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_009f:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00a4:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_00a9:  ldstr      "x"
  IL_00ae:  ldstr      ""
  IL_00b3:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00b8:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00bd:  dup
  IL_00be:  ldstr      "a4"
  IL_00c3:  ldstr      ""
  IL_00c8:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00cd:  ldnull
  IL_00ce:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_00d3:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00d8:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_00dd:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedChildExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Module M
    Property F As String = "f"
    Property G As XElement = <g/>
    Property H As XElement = <r:h xmlns:r="http://roslyn"/>
    Property W As XElement = <w w=<%= F %>/>
    Property X As XElement = <x><%= F %></x>
    Property Y As XElement = <y><%= G %></y>
    Property Z As XElement = <z><%= F %><%= G %><%= H %></z>
    Sub Main()
        Console.WriteLine("{0}", W)
        Console.WriteLine("{0}", X)
        Console.WriteLine("{0}", Y)
        Console.WriteLine("{0}", Z)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<w w="f" />
<x>f</x>
<y>
  <g />
</y>
<z>f<g /><r:h xmlns:r="http://roslyn" /></z>
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedChildExpressions_2()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Xml.Linq
Module M
    Private FString = "s"
    Private FName As XName = XName.Get("n", "")
    Private FAttribute As XAttribute = New XAttribute(XName.Get("a", ""), "b")
    Private FElement As XElement = <e/>
    Private X1 As XElement = <x1 <%= FString %>/>
    Private X2 As XElement = <x2><%= FString %></x2>
    Private Y1 As XElement = <y1 <%= FName %>/>
    Private Y2 As XElement = <y2><%= FName %></y2>
    Private Z1 As XElement = <z1 <%= FAttribute %>/>
    Private Z2 As XElement = <z2><%= FAttribute %></z2>
    Private W1 As XElement = <w1 <%= FElement %>/>
    Private W2 As XElement = <w2><%= FElement %></w2>
    Sub Main()
        Console.WriteLine("{0}", X1)
        Console.WriteLine("{0}", X2)
        Console.WriteLine("{0}", Y1)
        Console.WriteLine("{0}", Y2)
        Console.WriteLine("{0}", Z1)
        Console.WriteLine("{0}", Z2)
        Console.WriteLine("{0}", W1)
        Console.WriteLine("{0}", W2)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x1>s</x1>
<x2>s</x2>
<y1>n</y1>
<y2>n</y2>
<z1 a="b" />
<z2 a="b" />
<w1>
  <e />
</w1>
<w2>
  <e />
</w2>
]]>)
        End Sub

        ' XContainer.Add(ParamArray content As Object()) overload
        ' should be used if the expression represents an Object().
        <Fact()>
        Public Sub EmbeddedChildCollectionExpressions()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Module M
    Private Function F() As XElement()
        Dim x = New XElement(1) {}
        x(0) = <<%= XName.Get("c1", "") %>/>
        x(1) = <<%= XName.Get("c2", "") %>/>
        Return x
    End Function
    Private Function G() As XElement(,)
        Dim x = New XElement(1, 1) {}
        x(0, 0) = <<%= XName.Get("c1", "") %>/>
        x(0, 1) = <<%= XName.Get("c2", "") %>/>
        x(1, 0) = <<%= XName.Get("c3", "") %>/>
        x(1, 1) = <<%= XName.Get("c4", "") %>/>
        Return x
    End Function
    Private Function H() As XElement()()
        Dim x = New XElement(1)() {}
        x(0) = F()
        x(1) = F()
        Return x
    End Function
    Private F0 As Object = F()
    Private F1 As Object() = F()
    Private F2 As XElement() = F()
    Private F3 As IEnumerable(Of Object) = F()
    Private F4 As IEnumerable(Of XElement) = F()
    Private F5 As XElement(,) = G()
    Private F6 As XElement()() = H()
    Sub Main()
        Report(<x0 <%= F0 %>/>)
        Report(<x0><%= F0 %></x0>)
        Report(<x1 <%= F1 %>/>)
        Report(<x1><%= F1 %></x1>)
        Report(<x2 <%= F2 %>/>)
        Report(<x2><%= F2 %></x2>)
        Report(<x3 <%= F3 %>/>)
        Report(<x3><%= F3 %></x3>)
        Report(<x4 <%= F4 %>/>)
        Report(<x4><%= F4 %></x4>)
        Report(<x5 <%= F5 %>/>)
        Report(<x5><%= F5 %></x5>)
        Report(<x6 <%= F6 %>/>)
        Report(<x6><%= F6 %></x6>)
    End Sub
    Sub Report(x As XElement)
        Console.WriteLine("{0}, #={1}", x, x.Elements.Count())
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x0>
  <c1 />
  <c2 />
</x0>, #=2
<x0>
  <c1 />
  <c2 />
</x0>, #=2
<x1>
  <c1 />
  <c2 />
</x1>, #=2
<x1>
  <c1 />
  <c2 />
</x1>, #=2
<x2>
  <c1 />
  <c2 />
</x2>, #=2
<x2>
  <c1 />
  <c2 />
</x2>, #=2
<x3>
  <c1 />
  <c2 />
</x3>, #=2
<x3>
  <c1 />
  <c2 />
</x3>, #=2
<x4>
  <c1 />
  <c2 />
</x4>, #=2
<x4>
  <c1 />
  <c2 />
</x4>, #=2
<x5>
  <c1 />
  <c2 />
  <c3 />
  <c4 />
</x5>, #=4
<x5>
  <c1 />
  <c2 />
  <c3 />
  <c4 />
</x5>, #=4
<x6>
  <c1 />
  <c2 />
  <c1 />
  <c2 />
</x6>, #=4
<x6>
  <c1 />
  <c2 />
  <c1 />
  <c2 />
</x6>, #=4
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      515 (0x203)
  .maxstack  3
  IL_0000:  ldstr      "x0"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldsfld     "M.F0 As Object"
  IL_001a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001f:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0024:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0029:  ldstr      "x0"
  IL_002e:  ldstr      ""
  IL_0033:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0038:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_003d:  dup
  IL_003e:  ldsfld     "M.F0 As Object"
  IL_0043:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0048:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_004d:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0052:  ldstr      "x1"
  IL_0057:  ldstr      ""
  IL_005c:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0061:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0066:  dup
  IL_0067:  ldsfld     "M.F1 As Object()"
  IL_006c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_0071:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0076:  ldstr      "x1"
  IL_007b:  ldstr      ""
  IL_0080:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0085:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_008a:  dup
  IL_008b:  ldsfld     "M.F1 As Object()"
  IL_0090:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_0095:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_009a:  ldstr      "x2"
  IL_009f:  ldstr      ""
  IL_00a4:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00a9:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00ae:  dup
  IL_00af:  ldsfld     "M.F2 As System.Xml.Linq.XElement()"
  IL_00b4:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_00b9:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_00be:  ldstr      "x2"
  IL_00c3:  ldstr      ""
  IL_00c8:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00cd:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00d2:  dup
  IL_00d3:  ldsfld     "M.F2 As System.Xml.Linq.XElement()"
  IL_00d8:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_00dd:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_00e2:  ldstr      "x3"
  IL_00e7:  ldstr      ""
  IL_00ec:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00f1:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_00f6:  dup
  IL_00f7:  ldsfld     "M.F3 As System.Collections.Generic.IEnumerable(Of Object)"
  IL_00fc:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0101:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0106:  ldstr      "x3"
  IL_010b:  ldstr      ""
  IL_0110:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0115:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_011a:  dup
  IL_011b:  ldsfld     "M.F3 As System.Collections.Generic.IEnumerable(Of Object)"
  IL_0120:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0125:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_012a:  ldstr      "x4"
  IL_012f:  ldstr      ""
  IL_0134:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0139:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_013e:  dup
  IL_013f:  ldsfld     "M.F4 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0144:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0149:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_014e:  ldstr      "x4"
  IL_0153:  ldstr      ""
  IL_0158:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_015d:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0162:  dup
  IL_0163:  ldsfld     "M.F4 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0168:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_016d:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0172:  ldstr      "x5"
  IL_0177:  ldstr      ""
  IL_017c:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0181:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0186:  dup
  IL_0187:  ldsfld     "M.F5 As System.Xml.Linq.XElement(,)"
  IL_018c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0191:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0196:  ldstr      "x5"
  IL_019b:  ldstr      ""
  IL_01a0:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_01a5:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_01aa:  dup
  IL_01ab:  ldsfld     "M.F5 As System.Xml.Linq.XElement(,)"
  IL_01b0:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_01b5:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_01ba:  ldstr      "x6"
  IL_01bf:  ldstr      ""
  IL_01c4:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_01c9:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_01ce:  dup
  IL_01cf:  ldsfld     "M.F6 As System.Xml.Linq.XElement()()"
  IL_01d4:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_01d9:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_01de:  ldstr      "x6"
  IL_01e3:  ldstr      ""
  IL_01e8:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_01ed:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_01f2:  dup
  IL_01f3:  ldsfld     "M.F6 As System.Xml.Linq.XElement()()"
  IL_01f8:  callvirt   "Sub System.Xml.Linq.XContainer.Add(ParamArray Object())"
  IL_01fd:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0202:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedExpressionConversions()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class A
End Class
Structure S
End Structure
Class C(Of T)
    Private Shared F1 As Object = Nothing
    Private Shared F2 As String = Nothing
    Private Shared F3 As XName = Nothing
    Private Shared F4 As XElement = Nothing
    Private Shared F5 As A = Nothing
    Private Shared F6 As S = Nothing
    Private Shared F7 As T = Nothing
    Private Shared F8 As Unknown = Nothing
    Shared Sub M()
        Dim x As XElement
        x = <a><%= F1 %></a>
        x = <a><%= F2 %></a>
        x = <a><%= F3 %></a>
        x = <a><%= F4 %></a>
        x = <a><%= F5 %></a>
        x = <a><%= F6 %></a>
        x = <a><%= F7 %></a>
        x = <a><%= F8 %></a>
        x = <a <%= F1 %>="b"/>
        x = <a <%= F2 %>="b"/>
        x = <a <%= F3 %>="b"/>
        x = <a <%= F4 %>="b"/>
        x = <a <%= F5 %>="b"/>
        x = <a <%= F6 %>="b"/>
        x = <a <%= F7 %>="b"/>
        x = <a <%= F8 %>="b"/>
        x = <a b=<%= F1 %>/>
        x = <a b=<%= F2 %>/>
        x = <a b=<%= F3 %>/>
        x = <a b=<%= F4 %>/>
        x = <a b=<%= F5 %>/>
        x = <a b=<%= F6 %>/>
        x = <a b=<%= F7 %>/>
        x = <a b=<%= F8 %>/>
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Unknown' is not defined.
    Private Shared F8 As Unknown = Nothing
                         ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'XName'.
        x = <a <%= F1 %>="b"/>
               ~~~~~~~~~
BC30311: Value of type 'XElement' cannot be converted to 'XName'.
        x = <a <%= F4 %>="b"/>
               ~~~~~~~~~
BC30311: Value of type 'A' cannot be converted to 'XName'.
        x = <a <%= F5 %>="b"/>
               ~~~~~~~~~
BC30311: Value of type 'S' cannot be converted to 'XName'.
        x = <a <%= F6 %>="b"/>
               ~~~~~~~~~
BC30311: Value of type 'T' cannot be converted to 'XName'.
        x = <a <%= F7 %>="b"/>
               ~~~~~~~~~
]]></errors>)
        End Sub

        ' Values of constant embedded expressions
        ' should be inlined in generated code.
        <Fact()>
        Public Sub EmbeddedExpressionConstants()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Private Const F1 As String = "v1"
    Private F2 As String = "v2"
    Function F() As Object
        Return <x a0=<%= "v0" %> a1=<%= F1 %> a2=<%= F2 %>/>
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.VerifyIL("M.F", <![CDATA[
{
  // Code size      114 (0x72)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "a0"
  IL_001a:  ldstr      ""
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "v0"
  IL_0029:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  dup
  IL_0034:  ldstr      "a1"
  IL_0039:  ldstr      ""
  IL_003e:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0043:  ldstr      "v1"
  IL_0048:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_004d:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0052:  dup
  IL_0053:  ldstr      "a2"
  IL_0058:  ldstr      ""
  IL_005d:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0062:  ldsfld     "M.F2 As String"
  IL_0067:  call       "Function My.InternalXmlHelper.CreateAttribute(System.Xml.Linq.XName, Object) As System.Xml.Linq.XAttribute"
  IL_006c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0071:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedExpressionDelegateConversion()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Delegate Sub D()
Module M
    Sub M0()
    End Sub
    Sub M1()
    End Sub
    Function M2() As Object
        Return Nothing
    End Function
    Function M3() As Object
        Return Nothing
    End Function
    Private F0 As D = <%= AddressOf M0 %>
    Private F1 As Object = <%= AddressOf M1 %>
    Private F2 As XElement = <x y=<%= AddressOf M2 %>/>
    Private F3 As XElement = <x><%= AddressOf M3 %></x>
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31172: An embedded expression cannot be used here.
    Private F0 As D = <%= AddressOf M0 %>
                      ~~~~~~~~~~~~~~~~~~~
BC30491: Expression does not produce a value.
    Private F0 As D = <%= AddressOf M0 %>
                          ~~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private F1 As Object = <%= AddressOf M1 %>
                           ~~~~~~~~~~~~~~~~~~~
BC30491: Expression does not produce a value.
    Private F1 As Object = <%= AddressOf M1 %>
                               ~~~~~~~~~~~~
BC30491: Expression does not produce a value.
    Private F2 As XElement = <x y=<%= AddressOf M2 %>/>
                                      ~~~~~~~~~~~~
BC30491: Expression does not produce a value.
    Private F3 As XElement = <x><%= AddressOf M3 %></x>
                                    ~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub EmbeddedExpressionXElementConstructor()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Module M
    Sub Main()
        Report(<<%= XName.Get("x", "") %>/>)
        Report(<<%= XName.Get("x", "") %> a="b">c</>)
        Report(<<%= <x1 a1="b1">c1</x1> %> a2="b2">c2</>)
    End Sub
    Sub Report(o As XElement)
        Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x />
<x a="b">c</x>
<x1 a1="b1" a2="b2">c1c2</x1>
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      207 (0xcf)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_0019:  ldstr      "x"
  IL_001e:  ldstr      ""
  IL_0023:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0028:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_002d:  dup
  IL_002e:  ldstr      "a"
  IL_0033:  ldstr      ""
  IL_0038:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_003d:  ldstr      "b"
  IL_0042:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_0047:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_004c:  dup
  IL_004d:  ldstr      "c"
  IL_0052:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0057:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_005c:  ldstr      "x1"
  IL_0061:  ldstr      ""
  IL_0066:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_006b:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0070:  dup
  IL_0071:  ldstr      "a1"
  IL_0076:  ldstr      ""
  IL_007b:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0080:  ldstr      "b1"
  IL_0085:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_008a:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_008f:  dup
  IL_0090:  ldstr      "c1"
  IL_0095:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_009a:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XElement)"
  IL_009f:  dup
  IL_00a0:  ldstr      "a2"
  IL_00a5:  ldstr      ""
  IL_00aa:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00af:  ldstr      "b2"
  IL_00b4:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_00b9:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00be:  dup
  IL_00bf:  ldstr      "c2"
  IL_00c4:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_00c9:  call       "Sub M.Report(System.Xml.Linq.XElement)"
  IL_00ce:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmbeddedExpressionNoXElementConstructor()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Class A
End Class
Structure S
End Structure
Class C(Of T)
    Private Shared F1 As Object = Nothing
    Private Shared F2 As String = Nothing
    Private Shared F3 As XName = Nothing
    Private Shared F4 As XElement = Nothing
    Private Shared F5 As A = Nothing
    Private Shared F6 As S = Nothing
    Private Shared F7 As T = Nothing
    Private Shared F8 As Unknown = Nothing
    Shared Sub M()
        Dim x As XElement
        x = <<%= F1 %>/>
        x = <<%= F2 %>/>
        x = <<%= F3 %>/>
        x = <<%= F4 %>/>
        x = <<%= F5 %>/>
        x = <<%= F6 %>/>
        x = <<%= F7 %>/>
        x = <<%= F8 %>/>
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Unknown' is not defined.
    Private Shared F8 As Unknown = Nothing
                         ~~~~~~~
BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Option Strict On disallows implicit conversions from 'Object' to 'XName'.
    'Public Overloads Sub New(other As XElement)': Option Strict On disallows implicit conversions from 'Object' to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Option Strict On disallows implicit conversions from 'Object' to 'XStreamingElement'.
        x = <<%= F1 %>/>
             ~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Value of type 'A' cannot be converted to 'XName'.
    'Public Overloads Sub New(other As XElement)': Value of type 'A' cannot be converted to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Value of type 'A' cannot be converted to 'XStreamingElement'.
        x = <<%= F5 %>/>
             ~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Value of type 'S' cannot be converted to 'XName'.
    'Public Overloads Sub New(other As XElement)': Value of type 'S' cannot be converted to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Value of type 'S' cannot be converted to 'XStreamingElement'.
        x = <<%= F6 %>/>
             ~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Value of type 'T' cannot be converted to 'XName'.
    'Public Overloads Sub New(other As XElement)': Value of type 'T' cannot be converted to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Value of type 'T' cannot be converted to 'XStreamingElement'.
        x = <<%= F7 %>/>
             ~~~~~~~~~
]]></errors>)
        End Sub

        ' Expressions within XmlEmbeddedExpressionSyntax should be
        ' bound, even if outside of an XML expression (error cases).
        <Fact()>
        Public Sub EmbeddedExpressionOutsideXmlExpression()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Class A
End Class
Class B
End Class
Module M
    Property P1 As A
    ReadOnly Property P2 As A
        Get
            Return Nothing
        End Get
    End Property
    WriteOnly Property P3 As A
        Set(value As A)
        End Set
    End Property
    Private F1 As B = <%= P1 %>
    Private F2 As B = <%= P2 %>
    Private F3 As B = <%= P3 %>
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30311: Value of type 'A' cannot be converted to 'B'.
    Private F1 As B = <%= P1 %>
                      ~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private F1 As B = <%= P1 %>
                      ~~~~~~~~~
BC30311: Value of type 'A' cannot be converted to 'B'.
    Private F2 As B = <%= P2 %>
                      ~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private F2 As B = <%= P2 %>
                      ~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private F3 As B = <%= P3 %>
                      ~~~~~~~~~
BC30524: Property 'P3' is 'WriteOnly'.
    Private F3 As B = <%= P3 %>
                          ~~
]]></errors>)
        End Sub

        ' Embedded expressions should be ignored for xmlns
        ' declarations, even if the expression is a string constant.
        <Fact()>
        Public Sub EmbeddedXmlnsExpressions()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Xml.Linq
Module M
    Private F1 As XElement = <x:y xmlns:x="http://roslyn"/>
    Private F2 As XElement = <x:y <%= "xmlns:x" %>="http://roslyn"/>
    Private F3 As XElement = <x:y <%= XName.Get("x", "http://www.w3.org/2000/xmlns/") %>="http://roslyn"/>
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31148: XML namespace prefix 'x' is not defined.
    Private F2 As XElement = <x:y <%= "xmlns:x" %>="http://roslyn"/>
                              ~
BC31148: XML namespace prefix 'x' is not defined.
    Private F3 As XElement = <x:y <%= XName.Get("x", "http://www.w3.org/2000/xmlns/") %>="http://roslyn"/>
                              ~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub EmbeddedExpressionCycle()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Xml.Linq
Class C
    Private Shared F As XElement = <f><%= F %></f>
    Shared Sub Main()
        Dim G As XElement = <g><%= G %></g>
        Console.WriteLine("{0}", F)
        Console.WriteLine("{0}", G)
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<f />
<g />
]]>)
        End Sub

        ' Do not evaluate embedded expressions in Imports to avoid cycles.
        <Fact()>
        Public Sub EmbeddedExpressionImportCycle()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=<%= <p:x/>.@y %>>"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:q=<%= <q:x/>.@y %>>
Module M
    Private F As String = <p:x q:y=""/>.@z
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31172: Error in project-level import '<xmlns:p=<%= <p:x/>.@y %>>' at '<%= <p:x/>.@y %>' : An embedded expression cannot be used here.
BC31172: An embedded expression cannot be used here.
Imports <xmlns:q=<%= <q:x/>.@y %>>
                 ~~~~~~~~~~~~~~~~
BC31148: XML namespace prefix 'p' is not defined.
    Private F As String = <p:x q:y=""/>.@z
                           ~
BC31148: XML namespace prefix 'q' is not defined.
    Private F As String = <p:x q:y=""/>.@z
                               ~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub CharacterAndEntityReferences()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="&amp;&apos;&#x30;abc">
Class C
    Shared Sub Main()
        Dim x = <x xmlns:p="&amp;&apos;&#x30;abc" p:y="&amp;&apos;&gt;&lt;&quot;&#x0058;&#x59;&#x5a;"/>
        Dim y = <x>&amp;&apos;&gt;&lt;&quot;<y/>&#x0058;&#x59;&#x5a;</x>
        System.Console.WriteLine(x.@p:y)
        System.Console.WriteLine(y.Value)
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
&'><"XYZ
&'><"XYZ
]]>)
            compilation.VerifyIL("C.Main", <![CDATA[
{
  // Code size      193 (0xc1)
  .maxstack  4
  .locals init (System.Xml.Linq.XElement V_0) //x
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "y"
  IL_001a:  ldstr      "&'0abc"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "&'><"XYZ"
  IL_0029:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  dup
  IL_0034:  ldstr      "p"
  IL_0039:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_003e:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0043:  ldstr      "&'0abc"
  IL_0048:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_004d:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_0052:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0057:  stloc.0
  IL_0058:  ldstr      "x"
  IL_005d:  ldstr      ""
  IL_0062:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0067:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_006c:  dup
  IL_006d:  ldstr      "&'><""
  IL_0072:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0077:  dup
  IL_0078:  ldstr      "y"
  IL_007d:  ldstr      ""
  IL_0082:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0087:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_008c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0091:  dup
  IL_0092:  ldstr      "XYZ"
  IL_0097:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_009c:  ldloc.0
  IL_009d:  ldstr      "y"
  IL_00a2:  ldstr      "&'0abc"
  IL_00a7:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_00ac:  call       "Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String"
  IL_00b1:  call       "Sub System.Console.WriteLine(String)"
  IL_00b6:  callvirt   "Function System.Xml.Linq.XElement.get_Value() As String"
  IL_00bb:  call       "Sub System.Console.WriteLine(String)"
  IL_00c0:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CDATA()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb">
Option Strict On
Module M
    Sub Main()
        Dim o = &lt;![CDATA[value]]&gt;
        System.Console.WriteLine("{0}: {1}", o.GetType(), o)
    End Sub
End Module
</file>
</compilation>, references:=Net40XmlReferences, expectedOutput:="System.Xml.Linq.XCData: <![CDATA[value]]>")
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (System.Xml.Linq.XCData V_0) //o
  IL_0000:  ldstr      "value"
  IL_0005:  newobj     "Sub System.Xml.Linq.XCData..ctor(String)"
  IL_000a:  stloc.0
  IL_000b:  ldstr      "{0}: {1}"
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Function Object.GetType() As System.Type"
  IL_0016:  ldloc.0
  IL_0017:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CDATAContent()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System
Imports System.Xml.Linq
Module M
    Sub Main()
        Dim x As XElement = &lt;a&gt;
            &lt;![CDATA[&lt;b&gt;
  &lt;c/&gt;
&lt;/&gt;]]&gt;
        &lt;/a&gt;
        Console.WriteLine("{0}", x.Value)
    End Sub
End Module
</file>
</compilation>, references:=Net40XmlReferences, expectedOutput:="<b>" & vbLf & "  <c/>" & vbLf & "</>")
        End Sub

        <Fact()>
        Public Sub [GetXmlNamespace]()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:F="http://roslyn/F">
Imports <xmlns:p-q="http://roslyn/p-q">
Module M
    Private F As Object
    Sub Main()
        Report(GetXmlNamespace(xml))
        Report(GetXmlNamespace(xmlns))
        Report(GetXmlNamespace())
        Report(GetXmlNamespace(F))
        Report(GetXmlNamespace(p-q))
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
http://www.w3.org/XML/1998/namespace
http://www.w3.org/2000/xmlns/

http://roslyn/F
http://roslyn/p-q
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  1
  IL_0000:  ldstr      "http://www.w3.org/XML/1998/namespace"
  IL_0005:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_000a:  call       "Sub M.Report(Object)"
  IL_000f:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_0014:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0019:  call       "Sub M.Report(Object)"
  IL_001e:  ldstr      ""
  IL_0023:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0028:  call       "Sub M.Report(Object)"
  IL_002d:  ldstr      "http://roslyn/F"
  IL_0032:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0037:  call       "Sub M.Report(Object)"
  IL_003c:  ldstr      "http://roslyn/p-q"
  IL_0041:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_0046:  call       "Sub M.Report(Object)"
  IL_004b:  ret
}
]]>)
        End Sub

        ' Dev10 reports an error (BC31146: "XML name expected.") for
        ' leading or trailing trivia around the GetXmlNamespace
        ' argument. Those cases are not treated as errors in Roslyn.
        <Fact()>
        Public Sub GetXmlNamespaceWithTrivia()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Module M
    Private F1 = GetXmlNamespace( )
    Private F2 = GetXmlNamespace(xml )
    Private F3 = GetXmlNamespace( p)
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(544261, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544261")>
        <Fact()>
        Public Sub IncompleteProjectLevelImport()
            Assert.Throws(Of ArgumentException)(Sub() TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=""..."""})))
            Assert.Throws(Of ArgumentException)(Sub() TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=""..."">, <xmlns:q=""..."""})))
        End Sub

        <WorkItem(544360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544360")>
        <Fact()>
        Public Sub ExplicitDefaultXmlnsAttribute_1()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub Main()
        Report(<x xmlns="   "/>)
        Report(<y xmlns="http://roslyn"/>)
    End Sub
    Sub Report(x As System.Xml.Linq.XElement)
        System.Console.WriteLine("[{0}, {1}]: {2}", x.Name.LocalName, x.Name.NamespaceName, x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
[x,    ]: <x xmlns="   " />
[y, http://roslyn]: <y xmlns="http://roslyn" />
]]>)
        End Sub

        <Fact()>
        Public Sub ExplicitDefaultXmlnsAttribute_2()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns="http://roslyn/1">
Module M
    Sub Main()
        Report(<x xmlns="http://roslyn/2"/>)
    End Sub
    Sub Report(x As System.Xml.Linq.XElement)
        System.Console.WriteLine("[{0}, {1}]: {2}", x.Name.LocalName, x.Name.NamespaceName, x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
[x, http://roslyn/2]: <x xmlns="http://roslyn/2" />
]]>)
        End Sub

        <WorkItem(544461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544461")>
        <Fact()>
        Public Sub ValueExtensionProperty()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class X
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Structure S
    Implements IEnumerable(Of XElement)
    Public Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return Nothing
    End Function
    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure
Interface IEnumerableOfXElement
    Inherits IEnumerable(Of XElement)
End Interface
Module M
    Sub M(Of T As XElement)(
          _1 As XElement,
          _2 As X,
          _3 As T,
          _4 As IEnumerable(Of XElement),
          _5 As IEnumerable(Of X),
          _6 As IEnumerable(Of T),
          _7 As IEnumerableOfXElement,
          _8 As XElement(),
          _9 As List(Of XElement),
          _10 As S)
        Dim o As Object
        o = <x/>.Value
        o = <x/>.<y>.Value
        o = _1.Value
        o = _2.Value
        o = _3.Value
        o = _4.Value
        o = _5.Value
        o = _6.Value
        o = _7.Value
        o = _8.Value
        o = _9.Value
        o = _10.Value
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.VerifyIL("M.M(Of T)", <![CDATA[
{
  // Code size      166 (0xa6)
  .maxstack  3
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  call       "Function System.Xml.Linq.XElement.get_Value() As String"
  IL_0019:  pop
  IL_001a:  ldstr      "x"
  IL_001f:  ldstr      ""
  IL_0024:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0029:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_002e:  ldstr      "y"
  IL_0033:  ldstr      ""
  IL_0038:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_003d:  call       "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0042:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_0047:  pop
  IL_0048:  ldarg.0
  IL_0049:  callvirt   "Function System.Xml.Linq.XElement.get_Value() As String"
  IL_004e:  pop
  IL_004f:  ldarg.1
  IL_0050:  callvirt   "Function System.Xml.Linq.XElement.get_Value() As String"
  IL_0055:  pop
  IL_0056:  ldarga.s   V_2
  IL_0058:  constrained. "T"
  IL_005e:  callvirt   "Function System.Xml.Linq.XElement.get_Value() As String"
  IL_0063:  pop
  IL_0064:  ldarg.3
  IL_0065:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_006a:  pop
  IL_006b:  ldarg.s    V_4
  IL_006d:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_0072:  pop
  IL_0073:  ldarg.s    V_5
  IL_0075:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_007a:  pop
  IL_007b:  ldarg.s    V_6
  IL_007d:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_0082:  pop
  IL_0083:  ldarg.s    V_7
  IL_0085:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_008a:  pop
  IL_008b:  ldarg.s    V_8
  IL_008d:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_0092:  pop
  IL_0093:  ldarg.s    V_9
  IL_0095:  box        "S"
  IL_009a:  castclass  "System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_009f:  call       "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_00a4:  pop
  IL_00a5:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ValueExtensionProperty_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Class A
    Inherits List(Of XElement)
    Public Property P As String
End Class
Class B
    Inherits A
    Public Property Value As String
End Class
Class C
    Inherits A
    Public Value As String
End Class
Class D
    Inherits A
    Public Property Value(o As Object) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
End Class
Class E
    Inherits A
    Public Function Value() As String
        Return Nothing
    End Function
End Class
Class F
    Inherits A
End Class
Module M
    <Extension()>
    Public Function Value(o As F) As String
        Return Nothing
    End Function
    Sub M()
        Dim _a As New A() With {.Value = .P, .P = .Value}
        Dim _b As New B() With {.Value = .P, .P = .Value}
        Dim _c As New C() With {.Value = .P, .P = .Value}
        Dim _d As New D() With {.Value = .P, .P = .Value}
        Dim _e As New E() With {.Value = .P, .P = .Value}
        Dim _f As New F() With {.Value = .P, .P = .Value}
        _a.VALUE = Nothing
        _b.VALUE = Nothing
        _c.VALUE = Nothing
        _d.value = Nothing
        _e.value = Nothing
        _f.value = Nothing
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30991: Member 'Value' cannot be initialized in an object initializer expression because it is shared.
        Dim _a As New A() With {.Value = .P, .P = .Value}
                                 ~~~~~
BC30992: Property 'Value' cannot be initialized in an object initializer expression because it requires arguments.
        Dim _d As New D() With {.Value = .P, .P = .Value}
                                 ~~~~~
BC30455: Argument not specified for parameter 'o' of 'Public Property Value(o As Object) As String'.
        Dim _d As New D() With {.Value = .P, .P = .Value}
                                                   ~~~~~
BC30990: Member 'Value' cannot be initialized in an object initializer expression because it is not a field or property.
        Dim _e As New E() With {.Value = .P, .P = .Value}
                                 ~~~~~
BC30990: Member 'Value' cannot be initialized in an object initializer expression because it is not a field or property.
        Dim _f As New F() With {.Value = .P, .P = .Value}
                                 ~~~~~
BC30455: Argument not specified for parameter 'o' of 'Public Property Value(o As Object) As String'.
        _d.value = Nothing
           ~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        _e.value = Nothing
        ~~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        _f.value = Nothing
        ~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ValueExtensionProperty_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
    Inherits List(Of XElement)
    Sub M()
        Me.Value = F(Me.Value)
        MyBase.Value = F(MyBase.Value)
        Value = F(Value)
        Dim c As Char = Me.Value(0)
        c = Me.Value()(1)
        Me.Value() = Me.Value(Of Object)()
    End Sub
    Function F(o As String) As String
        Return Nothing
    End Function
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        Value = F(Value)
        ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        Value = F(Value)
                  ~~~~~
BC30456: 'Value' is not a member of 'C'.
        Me.Value() = Me.Value(Of Object)()
                     ~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ValueExtensionProperty_4()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Xml.Linq
Module M
    Sub Main()
        Dim x = <x>
                    <y>content</y>
                    <z/>
                </x>
        x.<x>.Value += "1"
        x.<y>.Value += "2"
        Add(x.<z>.Value, "3")
        Console.WriteLine("{0}", x)
    End Sub
    Sub Add(ByRef s As String, value As String)
        s += value
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x>
  <y>content2</y>
  <z>3</z>
</x>
]]>)
        End Sub

        ''' <summary>
        ''' If there is an accessible extension method named "Value", the InternalXmlHelper
        ''' Value extension property should be dropped, since we do not perform overload
        ''' resolution between methods and properties. If the extension method is inaccessible
        ''' however, the InternalXmlHelper property should be used.
        ''' </summary>
        <Fact()>
        Public Sub ValueExtensionPropertyAndExtensionMethod()
            ' Accessible extension method.
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Class C
    Sub M()
        Dim x = <x/>.<y>
        Dim o = x.Value()
        x.Value(o)
    End Sub
End Class
Module M
    <Extension()>
    Function Value(x As IEnumerable(Of XElement), y As Object) As Object
        Return Nothing
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC36586: Argument not specified for parameter 'y' of extension method 'Public Function Value(y As Object) As Object' defined in 'M'.
        Dim o = x.Value()
                  ~~~~~
]]></errors>)
            ' Inaccessible extension method.
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Class C
    Sub M()
        Dim x = <x/>.<y>
        Dim o = x.Value()
        x.Value(o)
    End Sub
End Class
Module M
    <Extension()>
    Private Function Value(x As IEnumerable(Of XElement), y As Object) As Object
        Return Nothing
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30057: Too many arguments to 'Public Property Value As String'.
        x.Value(o)
                ~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Bind to InternalXmlHelper Value extension property if a member named "Value" is inaccessible.
        ''' Note that Dev11 ignores the InternalXmlHelper property if regular binding finds a
        ''' member (in this case, the inaccessible member). Therefore this is a breaking change.
        ''' </summary>
        <Fact()>
        Public Sub ValueExtensionPropertyAndInaccessibleMember()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Xml.Linq
Structure S
    Implements IEnumerable(Of XElement)
    Public Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return Nothing
    End Function
    Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
    Private Property Value As Object
End Structure
Module M
    Function F(o As S)
        ' Dev11: BC30390: 'S.Value' is not accessible in this context because it is 'Private'.
        Return o.Value
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertNoErrors()
        End Sub

        ' The InternalXmlHelper.Value extension property should be available
        ' for IEnumerable(Of XElement) only. The AttributeValue extension property
        ' is overloaded for XElement and IEnumerable(Of XElement) but should
        ' only be available if the namespace is imported.
        <Fact()>
        Public Sub ValueAndAttributeValueExtensionProperties()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class X
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Interface IEnumerableOfXElement
    Inherits IEnumerable(Of XElement)
End Interface
Module M
    Sub M(Of T As XElement)(
          _1 As XObject,
          _2 As XElement,
          _3 As X,
          _4 As T,
          _5 As IEnumerable(Of XObject),
          _6 As IEnumerable(Of XElement),
          _7 As IEnumerable(Of X),
          _8 As IEnumerable(Of T),
          _9 As IEnumerableOfXElement,
          _10 As XElement(),
          _11 As List(Of XElement),
          _12 As IEnumerable(Of XElement)())
        Dim name As XName = Nothing
        Dim o As Object
        o = <x/>.Value
        o = <x/>.AttributeValue(name)
        o = <x/>.<y>.Value
        o = <x/>.<y>.AttributeValue(name)
        o = <x/>.@y.Value
        o = <x/>.@y.AttributeValue(name)
        o = _1.Value
        o = _1.AttributeValue(name)
        o = _2.Value
        o = _2.AttributeValue(name)
        o = _3.Value
        o = _3.AttributeValue(name)
        o = _4.Value
        o = _4.AttributeValue(name)
        o = _5.Value
        o = _5.AttributeValue(name)
        o = _6.Value
        o = _6.AttributeValue(name)
        o = _7.Value
        o = _7.AttributeValue(name)
        o = _8.Value
        o = _8.AttributeValue(name)
        o = _9.Value
        o = _9.AttributeValue(name)
        o = _10.Value
        o = _10.AttributeValue(name)
        o = _11.Value
        o = _11.AttributeValue(name)
        o = _12.Value
        o = _12.AttributeValue(name)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'AttributeValue' is not a member of 'XElement'.
        o = <x/>.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of XElement)'.
        o = <x/>.<y>.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'Value' is not a member of 'String'.
        o = <x/>.@y.Value
            ~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'String'.
        o = <x/>.@y.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'Value' is not a member of 'XObject'.
        o = _1.Value
            ~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'XObject'.
        o = _1.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'XElement'.
        o = _2.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'X'.
        o = _3.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'T'.
        o = _4.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'Value' is not a member of 'IEnumerable(Of XObject)'.
        o = _5.Value
            ~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of XObject)'.
        o = _5.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of XElement)'.
        o = _6.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of X)'.
        o = _7.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of T As XElement)'.
        o = _8.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerableOfXElement'.
        o = _9.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'XElement()'.
        o = _10.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'List(Of XElement)'.
        o = _11.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~
BC30456: 'Value' is not a member of 'IEnumerable(Of XElement)()'.
        o = _12.Value
            ~~~~~~~~~
BC30456: 'AttributeValue' is not a member of 'IEnumerable(Of XElement)()'.
        o = _12.AttributeValue(name)
            ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub TrimElementContent()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports Microsoft.VisualBasic
Imports System
Imports System.Xml.Linq
Module M
    Private F As XElement = <x>
                                <y>   <z> nested </z>   </y>
                                <y> &#x20; <z> nested </z> &#x20; </y>
                                <y>
                                    begin <z> nested </z> end
                                </y>
                                <y xml:space="default">
                                    begin <z> nested </z> end
                                </y>
                                <y xml:space="preserve">
                                    begin <z> nested </z> end
                                </y>
                            </x>
    Sub Main()
        For Each y In F.<y>
            Console.Write("{0}" & Environment.NewLine, y.ToString())
            Console.Write("[{0}]" & Environment.NewLine, y.Value.Replace(vbLf, Environment.NewLine))
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<y>
  <z> nested </z>
</y>
[ nested ]
<y>   <z> nested </z>   </y>
[    nested    ]
<y>
                                    begin <z> nested </z> end
                                </y>
[
                                    begin  nested  end
                                ]
<y xml:space="default">
                                    begin <z> nested </z> end
                                </y>
[
                                    begin  nested  end
                                ]
<y xml:space="preserve">
                                    begin <z> nested </z> end
                                </y>
[
                                    begin  nested  end
                                ]
]]>)
        End Sub

        ''' <summary>
        ''' CR/LF and single CR characters should be
        ''' replaced by single LF characters.
        ''' </summary>
        <WorkItem(545508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545508")>
        <Fact()>
        Public Sub NormalizeNewlinesTest()
            For Each eol In {vbCr, vbLf, vbCrLf}
                Dim sourceBuilder = New StringBuilder()
                sourceBuilder.AppendLine("Module M")
                sourceBuilder.AppendLine("    Sub Main()")
                sourceBuilder.AppendLine("        Report(<x>[" & eol & "|" & eol & eol & "]</>.Value)")
                sourceBuilder.AppendLine("        Report(<x><![CDATA[[" & eol & "|" & eol & eol & "]]]></>.Value)")
                sourceBuilder.AppendLine("    End Sub")
                sourceBuilder.AppendLine("    Sub Report(s As String)")
                sourceBuilder.AppendLine("        For Each c As Char in s")
                sourceBuilder.AppendLine("            System.Console.WriteLine(""{0}"", Microsoft.VisualBasic.AscW(c))")
                sourceBuilder.AppendLine("        Next")
                sourceBuilder.AppendLine("    End Sub")
                sourceBuilder.AppendLine("End Module")

                Dim sourceTree = VisualBasicSyntaxTree.ParseText(sourceBuilder.ToString())
                Dim comp = VisualBasicCompilation.Create(Guid.NewGuid().ToString(), {sourceTree}, DefaultVbReferences.Concat(Net40XmlReferences))
                CompileAndVerify(comp, expectedOutput:=<![CDATA[
91
10
124
10
10
93
91
10
124
10
10
93
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub NormalizeAttributeValue()
            Const space = " "
            Dim strs = {space, vbCr, vbLf, vbCrLf, vbTab, "&#x20;", "&#xD;", "&#xA;", "&#x9;"}

            ' Empty string.
            NormalizeAttributeValueCore("")

            ' Single characters.
            For Each str0 In strs
                NormalizeAttributeValueCore(str0)
                NormalizeAttributeValueCore("[" & str0 & "]")
            Next

            ' Pairs of characters.
            For Each str1 In strs
                For Each str2 In strs
                    Dim str = str1 & str2
                    NormalizeAttributeValueCore(str)
                    NormalizeAttributeValueCore("[" & str & "]")
                Next
            Next
        End Sub

        Private Sub NormalizeAttributeValueCore(str As String)
            Dim sourceBuilder = New StringBuilder()
            sourceBuilder.AppendLine("Module M")
            sourceBuilder.AppendLine("    Sub Main()")
            sourceBuilder.AppendLine("        System.Console.WriteLine(""[[{0}]]"", <x a=""" & str & """/>.@a)")
            sourceBuilder.AppendLine("    End Sub")
            sourceBuilder.AppendLine("End Module")

            Dim sourceTree = VisualBasicSyntaxTree.ParseText(sourceBuilder.ToString())
            Dim comp = VisualBasicCompilation.Create(Guid.NewGuid().ToString(), {sourceTree}, DefaultVbReferences.Concat(Net40XmlReferences))
            CompileAndVerify(comp, expectedOutput:="[[" & NormalizeValue(str) & "]]")
        End Sub

        Private Function NormalizeValue(str As String) As String
            Const space = " "
            str = str.Replace(vbCrLf, space)
            str = str.Replace(vbCr, space)
            str = str.Replace(vbLf, space)
            str = str.Replace(vbTab, space)
            str = str.Replace("&#x20;", space)
            str = str.Replace("&#xD;", vbCr)
            str = str.Replace("&#xA;", vbLf)
            str = str.Replace("&#x9;", vbTab)
            Return str
        End Function

        ' Dev11 treats p:xmlns="..." as an xmlns declaration for the default
        ' namespace. Roslyn issues warnings for these cases and only considers
        ' p:xmlns="..." an xmlns declaration if 'p' maps to the default namespace.
        <WorkItem(544366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544366")>
        <Fact()>
        Public Sub PrefixAndXmlnsLocalName()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Xml.Linq
Imports <xmlns="N0">
Imports <xmlns:p1="">
Imports <xmlns:p2="N2">
Module M
    Sub Main()
        Report(<x1 p1:a="b"/>)
        Report(<x2 p2:a="b"/>)
        Report(<y1 p1:xmlns="A1"/>)
        Report(<y2 p2:xmlns="A2"/>)
        Report(<y3 xmlns:p3="N3" p3:xmlns="A3"/>)
    End Sub
    Sub Report(x As XElement)
        Console.WriteLine("{0}: {1}", x.Name, x)
        For Each a In x.Attributes
            Console.WriteLine("  {0}", a.Name)
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
{N0}x1: <x1 a="b" xmlns="N0" />
  a
  xmlns
{N0}x2: <x2 p2:a="b" xmlns:p2="N2" xmlns="N0" />
  {N2}a
  {http://www.w3.org/2000/xmlns/}p2
  xmlns
{A1}y1: <y1 xmlns="A1" />
  xmlns
{N0}y2: <y2 p2:xmlns="A2" xmlns:p2="N2" xmlns="N0" />
  {N2}xmlns
  {http://www.w3.org/2000/xmlns/}p2
  xmlns
{N0}y3: <y3 xmlns:p3="N3" p3:xmlns="A3" xmlns="N0" />
  {http://www.w3.org/2000/xmlns/}p3
  {N3}xmlns
  xmlns
]]>)
            compilation.Compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42368: The xmlns attribute has special meaning and should not be written with a prefix.
        Report(<y1 p1:xmlns="A1"/>)
                   ~~~~~~~~
BC42360: It is not recommended to have attributes named xmlns. Did you mean to write 'xmlns:p2' to define a prefix named 'p2'?
        Report(<y2 p2:xmlns="A2"/>)
                   ~~~~~~~~
BC42360: It is not recommended to have attributes named xmlns. Did you mean to write 'xmlns:p3' to define a prefix named 'p3'?
        Report(<y3 xmlns:p3="N3" p3:xmlns="A3"/>)
                                 ~~~~~~~~
]]></errors>)
        End Sub

        ' BC42361 is a warning only and should not prevent code gen.
        <Fact()>
        Public Sub BC42361WRN_UseValueForXmlExpression3()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict Off
Module M
    Sub Main()
        System.Console.WriteLine("{0}", If(TryCast(<x/>.<y>, String), "[Nothing]"))
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
[Nothing]
]]>)
            compilation.Compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        System.Console.WriteLine("{0}", If(TryCast(<x/>.<y>, String), "[Nothing]"))
                                                   ~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub UseLocallyRedefinedImport()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/">
Module M
    Sub Main()
        ' Local declaration never used.
        Report(<x0 xmlns:p="http://roslyn/" a="b"/>)
        ' Local declaration used at root.
        Report(<x1 xmlns:p="http://roslyn/" p:a="b"/>)
        ' Local declaration used beneath root.
        Report(<x2 xmlns:p="http://roslyn/">
                   <y p:a="b"/>
               </x2>)
        ' Local declaration defined and used beneath root.
        Report(<x3>
                   <y xmlns:p="http://roslyn/" p:a="b"/>
               </x3>)
        ' Local declaration defined beneath root and used below.
        Report(<x4>
                   <y xmlns:p="http://roslyn/">
                       <z p:a="b"/>
                   </y>
               </x4>)
        ' Local declaration defined beneath root and used on sibling.
        Report(<x5>
                   <y xmlns:p="http://roslyn/"/>
                   <z p:a="b"/>
               </x5>)
        ' Local declaration re-defined at root.
        Report(<x6 xmlns:p="http://roslyn/other" p:a="b"/>)
        ' Local declaration re-defined beneath root.
        Report(<x7>
                   <y xmlns:p="http://roslyn/other" p:a="b"/>
               </x7>)
        ' Local declaration defined and re-defined.
        Report(<x8 xmlns:p="http://roslyn/" p:a1="b1">
                   <y xmlns:p="http://roslyn/other" p:a2="b2">
                       <z xmlns:p="http://roslyn/" p:a3="b3"/>
                   </y>
               </x8>)
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x0 a="b" xmlns:p="http://roslyn/" />
<x1 p:a="b" xmlns:p="http://roslyn/" />
<x2 xmlns:p="http://roslyn/">
  <y p:a="b" />
</x2>
<x3 xmlns:p="http://roslyn/">
  <y p:a="b" />
</x3>
<x4 xmlns:p="http://roslyn/">
  <y>
    <z p:a="b" />
  </y>
</x4>
<x5 xmlns:p="http://roslyn/">
  <y />
  <z p:a="b" />
</x5>
<x6 xmlns:p="http://roslyn/other" p:a="b" />
<x7>
  <y xmlns:p="http://roslyn/other" p:a="b" />
</x7>
<x8 p:a1="b1" xmlns:p="http://roslyn/">
  <y xmlns:p="http://roslyn/other" p:a2="b2">
    <z xmlns:p="http://roslyn/" p:a3="b3" />
  </y>
</x8>
]]>)
        End Sub

        ' If the xmlns attribute is a re-definition of an Imports xmlns
        ' declaration, the attribute should be created with CreateNamespaceAttribute
        ' (so the attribute can be removed if the element is embedded).
        ' Otherwise, the attribute should be created with XAttribute .ctor.
        <Fact()>
        Public Sub ConstructingXmlnsAttributes()
            ' The only difference between b.vb and c.vb is that c.vb
            ' contains an Imports <xmlns:...> declaration that matches
            ' the explicit xmlns declaration within the XElement.
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Partial Class C
    Shared Sub Main()
        M1()
        M2()
    End Sub
    Shared Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Partial Class C
    Shared Sub M1()
        Report(<x xmlns:p="http://roslyn/"/>)
    End Sub
End Class
    ]]></file>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/">
Partial Class C
    Shared Sub M2()
        Report(<x xmlns:p="http://roslyn/"/>)
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:p="http://roslyn/" />
<x xmlns:p="http://roslyn/" />
]]>)
            ' If no matching Imports, use XAttribute .ctor.
            compilation.VerifyIL("C.M1()", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "p"
  IL_001a:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "http://roslyn/"
  IL_0029:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_002e:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0033:  call       "Sub C.Report(Object)"
  IL_0038:  ret
}
]]>)
            ' If matching Imports, use CreateNamespaceAttribute.
            compilation.VerifyIL("C.M2()", <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  4
  IL_0000:  ldstr      "x"
  IL_0005:  ldstr      ""
  IL_000a:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_000f:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0014:  dup
  IL_0015:  ldstr      "p"
  IL_001a:  ldstr      "http://www.w3.org/2000/xmlns/"
  IL_001f:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0024:  ldstr      "http://roslyn/"
  IL_0029:  call       "Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace"
  IL_002e:  call       "Function My.InternalXmlHelper.CreateNamespaceAttribute(System.Xml.Linq.XName, System.Xml.Linq.XNamespace) As System.Xml.Linq.XAttribute"
  IL_0033:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0038:  call       "Sub C.Report(Object)"
  IL_003d:  ret
}
]]>)
        End Sub

        <WorkItem(545345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545345")>
        <Fact()>
        Public Sub RemoveExistingNamespaceAttribute()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn/p">
Partial Class C
    Shared Function F1() As XElement
        Return <p:y/>
    End Function
    Shared Sub Main()
        Report(<x xmlns:p="http://roslyn/p"><%= F1() %></x>)
        Report(<x xmlns:p="http://roslyn/q"><%= F1() %></x>)
        Report(<x xmlns:p="http://roslyn/q"><%= F2() %></x>)
        Report(<x xmlns:q="http://roslyn/q"><%= F2() %></x>)
        Report(<x xmlns:p="http://Roslyn/p"><%= F1() %></x>)
    End Sub
    Shared Sub Report(x As XElement)
        System.Console.WriteLine("{0}", x)
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Imports <xmlns:q="http://roslyn/q">
Partial Class C
    Shared Function F2() As XElement
        Return <q:y/>
    End Function
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:p="http://roslyn/p">
  <p:y />
</x>
<x xmlns:p="http://roslyn/q">
  <p:y xmlns:p="http://roslyn/p" />
</x>
<x xmlns:p="http://roslyn/q" xmlns:q="http://roslyn/q">
  <q:y />
</x>
<x xmlns:q="http://roslyn/q">
  <q:y />
</x>
<x xmlns:p="http://Roslyn/p">
  <p:y xmlns:p="http://roslyn/p" />
</x>
]]>)
        End Sub

        <Fact()>
        Public Sub DefaultAndEmptyNamespaces_1()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports <xmlns="">
Imports <xmlns:e="">
Module M
    Sub Main()
        Report(<x e:a="1"/>)
        Report(<e:x a="1"/>)
        Report(<e:x><y/></e:x>)
        Report(<x><e:y/></x>)
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x a="1" />
<x a="1" xmlns="" />
<x xmlns="">
  <y />
</x>
<x xmlns="">
  <y />
</x>
]]>)
        End Sub

        <WorkItem(545401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545401")>
        <Fact()>
        Public Sub DefaultAndEmptyNamespaces_2()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports <xmlns="default">
Imports <xmlns:e="">
Imports <xmlns:p="ns">
Module M
    Sub Main()
        Report(<x e:a="1" p:b="2"/>)
        Report(<e:x a="1" p:b="2"/>)
        Report(<p:x e:a="1" b="2"/>)
        Report(<e:x><y/><p:z/></e:x>)
        Report(<x><e:y/><p:z/></x>)
        Report(<p:x><e:y/><z/></p:x>)
    End Sub
    Sub Report(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x a="1" p:b="2" xmlns:p="ns" xmlns="default" />
<x a="1" p:b="2" xmlns:p="ns" xmlns="" />
<p:x a="1" b="2" xmlns:p="ns" />
<x xmlns:p="ns" xmlns="">
  <y xmlns="default" />
  <p:z />
</x>
<x xmlns:p="ns" xmlns="default">
  <y xmlns="" />
  <p:z />
</x>
<p:x xmlns="" xmlns:p="ns">
  <y />
  <z xmlns="default" />
</p:x>
]]>)
        End Sub

        ''' <summary>
        ''' Should not call RemoveNamespaceAttributes
        ''' on intrinsic types or enums.
        ''' </summary>
        <WorkItem(546191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546191")>
        <Fact()>
        Public Sub RemoveNamespaceAttributes_OtherContentTypes()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports <xmlns:p="http://roslyn">
Enum E
    A
End Enum
Structure S
End Structure
Class C(Of T)
    Private _1 As Object = Nothing
    Private _2 As Boolean = False
    Private _3 As Byte = 0
    Private _4 As SByte = 0
    Private _5 As Int16 = 0
    Private _6 As UInt16 = 0
    Private _7 As Int32 = 0
    Private _8 As UInt32 = 0
    Private _9 As Int64 = 0
    Private _10 As UInt64 = 0
    Private _11 As Single = 0
    Private _12 As Double = 0
    Private _13 As Decimal = 0
    Private _14 As DateTime = Nothing
    Private _15 As Char = Nothing
    Private _16 As String = ""
    Private _17 As E = E.A
    Private _18 As Integer? = Nothing
    Private _19 As S = Nothing
    Private _20 As T = Nothing
    Private _21 As ValueType = E.A
    Private _22 As System.Enum = E.A
    Private _23 As Object() = Nothing
    Private _24 As Array = Nothing
    Function F1() As Object
        Return <x><%= _1 %></x>
    End Function
    Function F2() As Object
        Return <x><%= _2 %></x>
    End Function
    Function F3() As Object
        Return <x><%= _3 %></x>
    End Function
    Function F4() As Object
        Return <x><%= _4 %></x>
    End Function
    Function F5() As Object
        Return <x><%= _5 %></x>
    End Function
    Function F6() As Object
        Return <x><%= _6 %></x>
    End Function
    Function F7() As Object
        Return <x><%= _7 %></x>
    End Function
    Function F8() As Object
        Return <x><%= _8 %></x>
    End Function
    Function F9() As Object
        Return <x><%= _9 %></x>
    End Function
    Function F10() As Object
        Return <x><%= _10 %></x>
    End Function
    Function F11() As Object
        Return <x><%= _11 %></x>
    End Function
    Function F12() As Object
        Return <x><%= _12 %></x>
    End Function
    Function F13() As Object
        Return <x><%= _13 %></x>
    End Function
    Function F14() As Object
        Return <x><%= _14 %></x>
    End Function
    Function F15() As Object
        Return <x><%= _15 %></x>
    End Function
    Function F16() As Object
        Return <x><%= _16 %></x>
    End Function
    Function F17() As Object
        Return <x><%= _17 %></x>
    End Function
    Function F18() As Object
        Return <x><%= _18 %></x>
    End Function
    Function F19() As Object
        Return <x><%= _19 %></x>
    End Function
    Function F20() As Object
        Return <x><%= _20 %></x>
    End Function
    Function F21() As Object
        Return <x><%= _21 %></x>
    End Function
    Function F22() As Object
        Return <x><%= _22 %></x>
    End Function
    Function F23() As Object
        Return <x><%= _23 %></x>
    End Function
    Function F24() As Object
        Return <x><%= _24 %></x>
    End Function
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F1()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F2()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F3()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F4()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F5()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F6()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F7()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F8()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F9()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F10()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F11()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F12()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F13()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F14()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F15()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F16()")))
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F17()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F18()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F19()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F20()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F21()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F22()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F23()")))
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("C(Of T).F24()")))
        End Sub

        ''' <summary>
        ''' Should not call RemoveNamespaceAttributes
        ''' unless there are xmlns Imports in scope.
        ''' </summary>
        <WorkItem(546191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546191")>
        <Fact()>
        Public Sub RemoveNamespaceAttributes_XmlnsInScope()
            ' No xmlns.
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Xml.Linq
Module M
    Function F1() As XElement
        Return <x><%= F2() %></x>
    End Function
    Function F2() As XElement
        Return <y/>
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("M.F1()")))

            ' xmlns attribute.
            verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Xml.Linq
Module M
    Function F1() As XElement
        Return <x xmlns:p="http://roslyn"><%= F2() %></x>
    End Function
    Function F2() As XElement
        Return <y/>
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            Assert.False(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("M.F1()")))

            ' Imports <...> in file.
            verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn">
Module M
    Function F1() As XElement
        Return <x><%= F2() %></x>
    End Function
    Function F2() As XElement
        Return <y/>
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences)
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("M.F1()")))

            ' Imports <...> at project scope.
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=""http://roslyn"">"}))
            verifier = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Xml.Linq
Module M
    Function F1() As XElement
        Return <x><%= F2() %></x>
    End Function
    Function F2() As XElement
        Return <y/>
    End Function
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=options)
            Assert.True(CallsRemoveNamespaceAttributes(verifier.VisualizeIL("M.F1()")))
        End Sub

        Private Function CallsRemoveNamespaceAttributes(actualIL As String) As Boolean
            Return actualIL.Contains("My.InternalXmlHelper.RemoveNamespaceAttributes")
        End Function

        <WorkItem(546480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546480")>
        <Fact()>
        Public Sub OpenCloseTag()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Linq
Imports System.Xml.Linq
Module M
    Sub Main()
        Report(<x/>)
        Report(<x></>)
        Report(<x> </>)
        Report(<x>
</>)
        Report(<x><!-- --></>)
        Report(<x> <!----> <!----> </>)
        Report(<x> <y/> </>)
    End Sub
    Sub Report(x As XElement)
        System.Console.WriteLine("[{0}] {1}", x.Nodes.Count(), x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
[0] <x />
[0] <x></x>
[0] <x></x>
[0] <x></x>
[1] <x>
  <!-- -->
</x>
[2] <x>
  <!---->
  <!---->
</x>
[1] <x>
  <y />
</x>
]]>)
        End Sub

        <Fact(), WorkItem(530882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530882")>
        Public Sub SelectFromIEnumerableOfXElementMultitargetingNetFX35()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq
Imports System.Xml.Linq

Module Module1
    Dim stuff As XElement =
        <root>
            <output someattrib="goo1">
                <value>1</value>
            </output>
            <output>
                <value>2</value>
            </output>
        </root>

    Sub Main()
        For Each value In stuff.<output>.<value>
            Console.WriteLine(value.Value)
        Next

        dim stuffArray() as XElement = {stuff, stuff}
        for each value in stuffArray.<output>
            Console.WriteLine(value.Value)
        next

        Console.WriteLine(stuff.<output>.@someattrib)
    End Sub
End Module]]>
                    </file>
                </compilation>

            Dim comp = CreateEmptyCompilationWithReferences(
                source,
                references:={MscorlibRef_v20, SystemRef_v20, MsvbRef, SystemXmlRef, SystemXmlLinqRef, SystemCoreRef},
                options:=TestOptions.ReleaseExe.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default))

            CompileAndVerify(comp, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine &
                                                   "1" & Environment.NewLine & "2" & Environment.NewLine &
                                                   "1" & Environment.NewLine & "2" & Environment.NewLine &
                                                   "goo1")
        End Sub

        <Fact(), WorkItem(530882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530882")>
        Public Sub SelectFromIEnumerableOfXElementMultitargetingNetFX35_Errors()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module Module1

    Sub Main()
        Dim objArray() As Object = {New Object(), New Object()}
        For Each value In objArray.<output>
        Next

        Console.WriteLine(objArray.@someAttrib)
    End Sub
End Module
                    ]]></file>
                </compilation>

            Dim comp = CreateEmptyCompilationWithReferences(
                source,
                references:={MscorlibRef_v20, SystemRef_v20, MsvbRef, SystemXmlRef, SystemXmlLinqRef, SystemCoreRef},
                options:=TestOptions.ReleaseExe.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default))

            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_TypeDisallowsElements, "objArray.<output>").WithArguments("Object()"),
                                    Diagnostic(ERRID.ERR_TypeDisallowsAttributes, "objArray.@someAttrib").WithArguments("Object()"))
        End Sub

        <Fact(), WorkItem(531351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531351")>
        Public Sub Bug17985()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Xml.Linq

Class scen1(Of T As XElement)
    Sub goo(ByVal o As T)
        Dim res = o.<moo>
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=TestOptions.ReleaseDll).
            VerifyIL("scen1(Of T).goo(T)",
            <![CDATA[
{
  // Code size       28 (0x1c)
      .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  box        "T"
  IL_0006:  ldstr      "moo"
  IL_000b:  ldstr      ""
  IL_0010:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0015:  callvirt   "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_001a:  pop
  IL_001b:  ret
}
]]>)
        End Sub

        <WorkItem(531445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531445")>
        <WorkItem(101597, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=101597")>
        <Fact>
        Public Sub SameNamespaceDifferentPrefixes()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"<xmlns:r=""http://roslyn/"">", "<xmlns:s=""http://roslyn/"">"}))

            Dim expectedOutput As Xml.Linq.XCData

            Const bug101597IsFixed = False

            If bug101597IsFixed Then
                expectedOutput = <![CDATA[
<p:x xmlns:s="http://roslyn/" xmlns:r="http://roslyn/" xmlns:q="http://roslyn/" xmlns:p="http://roslyn/">
  <p:y p:a="" p:b="" />
</p:x>
]]>
            Else
                expectedOutput = <![CDATA[
<q:x xmlns:p="http://roslyn/" xmlns:s="http://roslyn/" xmlns:r="http://roslyn/" xmlns:q="http://roslyn/">
  <q:y q:a="" q:b="" />
</q:x>
]]>
            End If

            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Imports <xmlns:q="http://roslyn/">
Module M
    Sub Main()
        Dim x = <p:x>
                    <%= <q:y r:a="" s:b=""/> %>
                 </p:x>
        System.Console.WriteLine("{0}", x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, options:=options, expectedOutput:=expectedOutput)
        End Sub

        <WorkItem(623035, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623035")>
        <Fact()>
        Public Sub Bug623035()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Friend Module Program
    Sub Main()
        Dim o2 As Object = "E"
        o2 = System.Xml.Linq.XName.Get("HELLO")
        Dim y2 = <<%= o2 %>></>
        System.Console.WriteLine(y2)
    End Sub
End Module
    ]]></file>
</compilation>, Net40XmlReferences, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(compilation,
            <![CDATA[
<HELLO></HELLO>
]]>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Custom))

            CompileAndVerify(compilation,
            <![CDATA[
<HELLO></HELLO>
]]>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.On))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Option Strict On disallows implicit conversions from 'Object' to 'XName'.
    'Public Overloads Sub New(other As XElement)': Option Strict On disallows implicit conversions from 'Object' to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Option Strict On disallows implicit conversions from 'Object' to 'XStreamingElement'.
        Dim y2 = <<%= o2 %>></>
                  ~~~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(631047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631047")>
        <Fact()>
        Public Sub Regress631047()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System        
Module Program
    Sub Main()
        Console.Write(<?goo                       ?>.ToString() = "<?goo                       ?>")
    End Sub
End Module
]]>
    </file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[True]]>)
        End Sub

        <WorkItem(814075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/814075")>
        <Fact()>
        Public Sub ExpressionTreeContainingExtensionProperty()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Xml.Linq
Module M
    Sub Main()
        M(Function(x) x.<y>.Value)
    End Sub
    Sub M(e As Expression(Of Func(Of XElement, String)))
        Console.WriteLine(e)
        Dim c = e.Compile()
        Dim s = c.Invoke(<x><y>content</></>)
        Console.WriteLine(s)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
x => get_Value(x.Elements(Get("y", "")))
content
]]>)
            compilation.VerifyIL("M.Main", <![CDATA[
{
  // Code size      175 (0xaf)
  .maxstack  17
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  ldtoken    "System.Xml.Linq.XElement"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  ldstr      "x"
  IL_000f:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0014:  stloc.0
  IL_0015:  ldnull
  IL_0016:  ldtoken    "Function My.InternalXmlHelper.get_Value(System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)) As String"
  IL_001b:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_0020:  castclass  "System.Reflection.MethodInfo"
  IL_0025:  ldc.i4.1
  IL_0026:  newarr     "System.Linq.Expressions.Expression"
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldloc.0
  IL_002e:  ldtoken    "Function System.Xml.Linq.XContainer.Elements(System.Xml.Linq.XName) As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)"
  IL_0033:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_0038:  castclass  "System.Reflection.MethodInfo"
  IL_003d:  ldc.i4.1
  IL_003e:  newarr     "System.Linq.Expressions.Expression"
  IL_0043:  dup
  IL_0044:  ldc.i4.0
  IL_0045:  ldnull
  IL_0046:  ldtoken    "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_004b:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_0050:  castclass  "System.Reflection.MethodInfo"
  IL_0055:  ldc.i4.2
  IL_0056:  newarr     "System.Linq.Expressions.Expression"
  IL_005b:  dup
  IL_005c:  ldc.i4.0
  IL_005d:  ldstr      "y"
  IL_0062:  ldtoken    "String"
  IL_0067:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_006c:  call       "Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression"
  IL_0071:  stelem.ref
  IL_0072:  dup
  IL_0073:  ldc.i4.1
  IL_0074:  ldstr      ""
  IL_0079:  ldtoken    "String"
  IL_007e:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0083:  call       "Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression"
  IL_0088:  stelem.ref
  IL_0089:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
  IL_008e:  stelem.ref
  IL_008f:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
  IL_0094:  stelem.ref
  IL_0095:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
  IL_009a:  ldc.i4.1
  IL_009b:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_00a0:  dup
  IL_00a1:  ldc.i4.0
  IL_00a2:  ldloc.0
  IL_00a3:  stelem.ref
  IL_00a4:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of System.Xml.Linq.XElement, String))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of System.Xml.Linq.XElement, String))"
  IL_00a9:  call       "Sub M.M(System.Linq.Expressions.Expression(Of System.Func(Of System.Xml.Linq.XElement, String)))"
  IL_00ae:  ret
}
]]>)
        End Sub

        <WorkItem(814052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/814052")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub XmlnsNamespaceTooLong()
            Dim identifier = New String("a"c, MetadataWriter.PdbLengthLimit)
            XmlnsNamespaceTooLongCore(identifier.Substring(6), tooLong:=False)
            XmlnsNamespaceTooLongCore(identifier, tooLong:=True)
        End Sub

        Private Sub XmlnsNamespaceTooLongCore(identifier As String, tooLong As Boolean)
            Dim [imports] = GlobalImport.Parse({String.Format("<xmlns:p=""{0}"">", identifier)})
            Dim options = TestOptions.DebugDll.WithGlobalImports([imports])
            Dim source = String.Format(<![CDATA[
Imports <xmlns="{0}">
Imports <xmlns:q="{0}">
Module M
    Private F As Object = <x
        xmlns="{0}"
        xmlns:r="{0}"
        p:a="{0}"
        q:b="" />
End Module
    ]]>.Value, identifier)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation><file name="c.vb"><%= source %></file></compilation>,
                references:=Net40XmlReferences, options:=options)

            If Not tooLong Then
                compilation.AssertTheseDiagnostics(<errors/>)
                compilation.AssertTheseEmitDiagnostics(<errors/>)
            Else
                Dim squiggles = New String("~"c, identifier.Length)
                Dim errors = String.Format(<![CDATA[
BC42374: Import string '@FX:={0}' is too long for PDB.  Consider shortening or compiling without /debug.
Module M
       ~
BC42374: Import string '@FX:q={0}' is too long for PDB.  Consider shortening or compiling without /debug.
Module M
       ~
BC42374: Import string '@PX:p={0}' is too long for PDB.  Consider shortening or compiling without /debug.
Module M
       ~
        ]]>.Value, identifier)
                compilation.AssertTheseDiagnostics(<errors/>)
                compilation.AssertTheseEmitDiagnostics(<errors><%= errors %></errors>)
            End If
        End Sub

        ''' <summary>
        ''' Constant embedded expression with duplicate xmlns attribute.
        ''' </summary>
        <WorkItem(863159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863159")>
        <Fact()>
        Public Sub XmlnsPrefixUsedInEmbeddedExpressionAndSibling_Constant()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Module M
    Sub Main()
        Dim x = <x>
                    <y>
                        <%= <p:z/> %>
                    </y>
                    <p:z/>
                </x>
        System.Console.WriteLine("{0}", x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:p="http://roslyn/">
  <y>
    <p:z />
  </y>
  <p:z />
</x>
]]>)
        End Sub

        ''' <summary>
        ''' Non-constant embedded expression with duplicate xmlns attribute.
        ''' </summary>
        <WorkItem(863159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863159")>
        <Fact()>
        Public Sub XmlnsPrefixUsedInEmbeddedExpressionAndSibling_NonConstant()
            ' Dev12 generates code that throws "InvalidOperationException: Duplicate attribute".
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Imports <xmlns:r="http://roslyn/r">
Class A
    Friend Shared Function F() As System.Xml.Linq.XElement
        Return <a>
                 <p:x/>
                 <q:y/>
                 <r:z/>
               </>
    End Function
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/q">
Imports <xmlns:q="http://roslyn/p">
Class B
    Friend Shared Function F() As System.Xml.Linq.XElement
        Return <b>
                 <p:x/>
                 <q:y/>
               </>
    End Function
End Class
    ]]></file>
    <file name="c.vb"><![CDATA[
Imports System
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Class C
    Shared Sub Main()
        Console.WriteLine(<x>
                            <y>
                              <%= A.F() %>
                            </>
                            <z>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x>
                            <y>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                            <p:z>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x>
                            <p:y>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                            <z>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x>
                            <p:y>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                            <q:z>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x>
                            <p:y>
                              <%= B.F() %>
                              <%= A.F() %>
                            </>
                            <q:z xmlns:q="http://roslyn/q">
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x xmlns:q="http://roslyn/q">
                            <p:y xmlns:p="http://roslyn/p">
                              <%= B.F() %>
                              <%= A.F() %>
                            </>
                            <z>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
        Console.WriteLine(<x>
                            <p:y xmlns:p="http://roslyn/p">
                              <%= B.F() %>
                              <%= A.F() %>
                            </>
                            <q:z>
                              <%= A.F() %>
                              <%= B.F() %>
                            </>
                          </>)
    End Sub
End Class
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:r="http://roslyn/r" xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p">
  <y>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
  </y>
  <z>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </z>
</x>
<x xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r" xmlns:q="http://roslyn/q">
  <y>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </y>
  <p:z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </p:z>
</x>
<x xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r" xmlns:q="http://roslyn/q">
  <p:y>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </p:y>
  <z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </z>
</x>
<x xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r">
  <p:y>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </p:y>
  <q:z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </q:z>
</x>
<x xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r">
  <p:y>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
  </p:y>
  <q:z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </q:z>
</x>
<x xmlns:p="http://roslyn/p" xmlns:q="http://roslyn/q" xmlns:r="http://roslyn/r">
  <p:y>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
  </p:y>
  <z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </z>
</x>
<x xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r">
  <p:y>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
  </p:y>
  <q:z>
    <a>
      <p:x />
      <q:y />
      <r:z />
    </a>
    <b xmlns:q="http://roslyn/p" xmlns:p="http://roslyn/q">
      <p:x />
      <q:y />
    </b>
  </q:z>
</x>
]]>)
        End Sub

        <WorkItem(863159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863159")>
        <Fact()>
        Public Sub XmlnsPrefixUsedInEmbeddedExpressionAndSibling_ExpressionTree()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn/p">
Module M
    Function F() As XElement
        Return <p:z/>
    End Function
    Sub Main()
        Dim e As Expression(Of Func(Of Object)) = Function() <x xmlns:q="http://roslyn/q">
                       <y>
                         <%= F() %>
                       </y>
                       <p:z/>
                     </x>
        Dim c = e.Compile()
        Console.WriteLine(c())
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<x xmlns:q="http://roslyn/q" xmlns:p="http://roslyn/p">
  <y>
    <p:z />
  </y>
  <p:z />
</x>
]]>)
        End Sub

        ''' <summary>
        ''' Should not traverse into embedded expressions
        ''' to determine set of used Imports.
        ''' </summary>
        <Fact()>
        Public Sub XmlnsPrefix_UnusedExpression()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Imports <xmlns:r="http://roslyn/r">
Imports System
Imports System.Xml.Linq
Module M
    Function F(x As XElement) As XElement
        Console.WriteLine(x)
        Return <r:z/>
    End Function
    Sub Main()
        Dim x = <p:x>
                  <%= F(<q:y/>) %>
                </p:x>
        Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
</compilation>, references:=Net40XmlReferences, expectedOutput:=<![CDATA[
<q:y xmlns:q="http://roslyn/q" />
<p:x xmlns:p="http://roslyn/p" xmlns:r="http://roslyn/r">
  <r:z />
</p:x>
]]>)
        End Sub

        ''' <summary>
        ''' My.InternalXmlHelper should be emitted into the root namespace.
        ''' </summary>
        <Fact()>
        Public Sub InternalXmlHelper_RootNamespace()
            Const source = "
Imports System
Imports System.Xml.Linq

Class C
    Sub M()
        Dim a = <element attr='value'/>.@attr
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)

            Dim refBuilder = ArrayBuilder(Of MetadataReference).GetInstance()
            refBuilder.Add(Net40.References.mscorlib)
            refBuilder.Add(Net40.References.System)
            refBuilder.Add(Net40.References.MicrosoftVisualBasic)
            refBuilder.AddRange(Net40XmlReferences)
            Dim refs = refBuilder.ToImmutableAndFree()

            CompileAndVerify(
                CreateEmptyCompilationWithReferences(tree, refs, TestOptions.DebugDll),
                symbolValidator:=
                    Sub(moduleSymbol)
                        moduleSymbol.GlobalNamespace.
                            GetMember(Of NamespaceSymbol)("My").
                            GetMember(Of NamedTypeSymbol)("InternalXmlHelper")
                    End Sub)

            CompileAndVerify(
                CreateEmptyCompilationWithReferences(tree, refs, TestOptions.DebugDll.WithRootNamespace("Root")),
                symbolValidator:=
                    Sub(moduleSymbol)
                        moduleSymbol.GlobalNamespace.
                            GetMember(Of NamespaceSymbol)("Root").
                            GetMember(Of NamespaceSymbol)("My").
                            GetMember(Of NamedTypeSymbol)("InternalXmlHelper")
                    End Sub)
        End Sub

    End Class
End Namespace
