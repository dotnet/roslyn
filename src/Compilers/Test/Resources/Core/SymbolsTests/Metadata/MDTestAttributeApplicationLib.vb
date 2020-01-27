' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'
' vbc /t:library /vbruntime- MDTestAttributeApplicationLib.vb /r:MDTestAttributeDefLib.dll
' 


'Test basic types

' Assembly
<Assembly: AString("assembly")>
<Assembly: ABoolean(True)>
<Assembly: AByte(1)>
<Assembly: AChar("a"c)>
<Assembly: ADouble(3.1415926)>
<Assembly: AInt16(16)>
<Assembly: AInt32(32)>
<Assembly: AInt64(64)>
<Assembly: AObject("object")>
<Assembly: ASingle(3.14159)>
<Assembly: AType(GetType(String))>

' Module
<Module: AString("module")>
<Module: ABoolean(True)>
<Module: AByte(1)>
<Module: AChar("a"c)>
<Module: ADouble(3.1415926)>
<Module: AInt16(16)>
<Module: AInt32(32)>
<Module: AInt64(64)>
<Module: AObject("object")>
<Module: ASingle(3.14159)>
<Module: AType(GetType(String))>

<AString("C1")>
Public Class C1
    <AString("InnerC1")>
    <TopLevelClass.ANested(True)>
    Public Class InnerC1(of t1)
        <AString("InnerC2")>
        Public class InnerC2(of s1, s2)
        End class
    End Class

    <AString("field1")>
    Public field1 As integer

    <AString("Property1")>
    Public Property Property1 As <AString("Integer")> Integer

    <AString("Sub1")>
    Public Sub Sub1( <AString("p1")> p1 As Integer)
    End Sub

    <AString("Function1")>
    Public Function Function1( <AString("p1")> p1 As Integer) As <AString("Integer")> Integer
        Return 0
    End Function
End Class

Public Class C2(Of T1)
    ' Custom attributes with generics

    <AType(GetType(List(Of )))>
    Public L1 As List(Of T1)

    <AType(GetType(List(Of C1)))>
    Public L2 As List(Of C1)

    <AType(GetType(List(Of String)))>
    Public L3 As List(Of String)

    <AType(GetType(List(Of KeyValuePair(Of C1, string))))>
    Public L4 As List(Of KeyValuePair(Of C1, string))

    <AType(GetType(List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))))>
    Public L5 As List(Of KeyValuePair(Of String, C1.InnerC1(of integer).InnerC2(of string, string)))

    ' Custom Attributes with arrays

    ' Arrays
    <AInt32(New Integer() {1, 2})>
    Public A1 As Type()

    <AType(new Type() {GetType(string), GetType(C2(of))})>
    Public A2 As Object()

    <AObject(new Type() {GetType(string)})>
    Public A3 As Object()

    <AObject(new Object() {GetType(string)})>
    Public A4 As Object()

    <AObject(new Object() {new Object() {GetType(string)}})>
    Public A5 As Object()

    <AObject({1, "two", GetType(string), 3.1415926})>
    Public A6 As Object()

    <AObject({1, new Object() {2, 3, 4}, 5})>
    Public A7 As Object()

    <AObject(new Integer() {1, 2, 3})>
    Public A8 As Object()

End Class

<ABoolean(False, B:=True)>
<AByte(0, B:=1)>
<AChar("a"c, C:="b"c)>
<AEnum(TestAttributeEnum.No, e:=TestAttributeEnum.Yes)>
<AInt16(16, I:=16)>
<AInt32(32, I:=32)>
<AInt64(64, I:=64)>
<ASingle(3.1459, S:=3.14159)>
<ADouble(3.1415926, D:=3.1415926)>
<AString("hello", S:="world")>
<AType(GetType(C1), T:=GetType(C3))>
Class C3
End Class

<AInt32(0, IA:={1, 2})>
<AEnum(TestAttributeEnum.No, ea:={TestAttributeEnum.Yes, TestAttributeEnum.No})>
<AString("No", sa:={"Yes", "No"})>
<AObject("No", oa:={CType("Yes", Object), CType("No", Object)})>
<AType(GetType(C1), ta:={GetType(C1), GetType(C3)})>
Class C4
End Class


