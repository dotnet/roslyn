' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenSelectCase
        Inherits BasicTestBase

        Private Shared Sub VerifySynthesizedStringHashMethod(compVerifier As CompilationVerifier, expected As Boolean)
            Dim methodName = PrivateImplementationDetails.SynthesizedStringHashFunctionName + "(String)"
            compVerifier.VerifyMemberInIL(methodName, expected)

            If expected Then
                compVerifier.VerifyIL(methodName, <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (UInteger V_0,
  Integer V_1)
  IL_0000:  ldc.i4     0x811c9dc5
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  brfalse.s  IL_002a
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.1
  IL_000b:  br.s       IL_0021
  IL_000d:  ldarg.0
  IL_000e:  ldloc.1
  IL_000f:  callvirt   "Function String.get_Chars(Integer) As Char"
  IL_0014:  ldloc.0
  IL_0015:  xor
  IL_0016:  ldc.i4     0x1000193
  IL_001b:  mul
  IL_001c:  stloc.0
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.1
  IL_001f:  add
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldarg.0
  IL_0023:  callvirt   "Function String.get_Length() As Integer"
  IL_0028:  blt.s      IL_000d
  IL_002a:  ldloc.0
  IL_002b:  ret
}
]]>)
            End If
        End Sub

        <Fact()>
        Public Sub SelectCase_Empty()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.WriteLine("Goo")
        Return 0
    End Function

    Sub Main()
        Select Case Goo()
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:="Goo").VerifyIL("M1.Main", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  call       "Function M1.Goo() As Integer"
  IL_0005:  pop
  IL_0006:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact()>
        Public Sub SimpleSelectCase_IfList()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(number as Integer)
        Select Case number
            Case Is < 1
                Console.WriteLine("Less than 1")
            Case 1 To 5
                Console.WriteLine("Between 1 and 5, inclusive")
            Case 6, 7, 8
                Console.WriteLine("Between 6 and 8, inclusive")
            Case 9 To 10
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:Less than 1
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size       91 (0x5b)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Less than 1"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  blt.s      IL_0024
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.5
  IL_0017:  bgt.s      IL_0024
  IL_0019:  ldstr      "Between 1 and 5, inclusive"
  IL_001e:  call       "Sub System.Console.WriteLine(String)"
  IL_0023:  ret
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.6
  IL_0026:  beq.s      IL_0030
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.7
  IL_002a:  beq.s      IL_0030
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.8
  IL_002e:  bne.un.s   IL_003b
  IL_0030:  ldstr      "Between 6 and 8, inclusive"
  IL_0035:  call       "Sub System.Console.WriteLine(String)"
  IL_003a:  ret
  IL_003b:  ldloc.0
  IL_003c:  ldc.i4.s   9
  IL_003e:  blt.s      IL_0050
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.s   10
  IL_0043:  bgt.s      IL_0050
  IL_0045:  ldstr      "Equal to 9 or 10"
  IL_004a:  call       "Sub System.Console.WriteLine(String)"
  IL_004f:  ret
  IL_0050:  ldstr      "Greater than 10"
  IL_0055:  call       "Sub System.Console.WriteLine(String)"
  IL_005a:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SimpleSelectCase_Boolean()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 To 11
            Console.Write(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(count As Integer)
        Dim b As Boolean = count

        Select Case b
            Case 9, 10
                If count <> 0 Then
                    Console.WriteLine("Non zero")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
            Case 1, 2, 3, 4, 5
            Case Else
        End Select

    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:Equal to 0
1:Non zero
2:Non zero
3:Non zero
4:Non zero
5:Non zero
6:Non zero
7:Non zero
8:Non zero
9:Non zero
10:Non zero
11:Non zero]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (Boolean V_0) //b
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  cgt.un
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  brfalse.s  IL_001a
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  bne.un.s   IL_0027
  IL_000c:  ldarg.0
  IL_000d:  brfalse.s  IL_0027
  IL_000f:  ldstr      "Non zero"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  brtrue.s   IL_0027
  IL_001d:  ldstr      "Equal to 0"
  IL_0022:  call       "Sub System.Console.WriteLine(String)"
  IL_0027:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SimpleSelectCase_DateTime()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Test(Nothing)
        Test(#1/1/0001 12:00:00 AM#)
        Test(#8/13/2002 12:15:10 PM#)
        Test(#8/13/2002 12:00:00 PM#)
        Test(#8/13/2002 12:16 PM#)
        Test(#8/13/2002 12:15 PM#)
        Test(#8/13/2002 12:05 PM#)
        Test(#8/13/2002 12:17 PM#)
        Test(#8/13/2002#)
    End Sub
    Dim cul = System.Globalization.CultureInfo.InvariantCulture
    Sub Test(d as DateTime)
        Console.WriteLine(d.ToString("M/d/yyyy h:mm:ss tt", cul))
        Select Case d
            Case #8/13/2002 12:15:10 PM#, #8/13/2002 12 PM#
                Console.WriteLine("Case #8/13/2002 12:15:10 PM#, #8/13/2002 12 PM#")
            Case Is >= #8/13/2002 12:16:15 PM#
                Console.WriteLine("Case Is >= #8/13/2002 12:16:15 PM#")
            Case #8/13/2002 12:14 PM# To #8/13/2002 12:16 PM#
                Console.WriteLine("Case #8/13/2002 12:14 PM# To #8/13/2002 12:16 PM#")
            Case #8/13/2002 12 PM#
                Console.WriteLine("Case #8/13/2002 12 PM#")
            Case #8/13/2002 12:05 PM#
                Console.WriteLine("Case #8/13/2002 12:05 PM#")
            Case Nothing
                Console.WriteLine("Case Nothing")
            Case Else
                Console.WriteLine("Case Else")
        End Select
        Console.WriteLine()
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[1/1/0001 12:00:00 AM
Case Nothing

1/1/0001 12:00:00 AM
Case Nothing

8/13/2002 12:15:10 PM
Case #8/13/2002 12:15:10 PM#, #8/13/2002 12 PM#

8/13/2002 12:00:00 PM
Case #8/13/2002 12:15:10 PM#, #8/13/2002 12 PM#

8/13/2002 12:16:00 PM
Case #8/13/2002 12:14 PM# To #8/13/2002 12:16 PM#

8/13/2002 12:15:00 PM
Case #8/13/2002 12:14 PM# To #8/13/2002 12:16 PM#

8/13/2002 12:05:00 PM
Case #8/13/2002 12:05 PM#

8/13/2002 12:17:00 PM
Case Is >= #8/13/2002 12:16:15 PM#

8/13/2002 12:00:00 AM
Case Else]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      293 (0x125)
  .maxstack  3
  .locals init (Date V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldstr      "M/d/yyyy h:mm:ss tt"
  IL_0007:  ldsfld     "M1.cul As Object"
  IL_000c:  castclass  "System.IFormatProvider"
  IL_0011:  call       "Function Date.ToString(String, System.IFormatProvider) As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ldarg.0
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ldc.i8     0x8c410da33ff5b00
  IL_0027:  newobj     "Sub Date..ctor(Long)"
  IL_002c:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_0031:  brfalse.s  IL_0049
  IL_0033:  ldloc.0
  IL_0034:  ldc.i8     0x8c410d815986000
  IL_003d:  newobj     "Sub Date..ctor(Long)"
  IL_0042:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_0047:  brtrue.s   IL_0058
  IL_0049:  ldstr      "Case #8/13/2002 12:15:10 PM#, #8/13/2002 12 PM#"
  IL_004e:  call       "Sub System.Console.WriteLine(String)"
  IL_0053:  br         IL_011f
  IL_0058:  ldloc.0
  IL_0059:  ldc.i8     0x8c410da5abd9180
  IL_0062:  newobj     "Sub Date..ctor(Long)"
  IL_0067:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_006c:  ldc.i4.0
  IL_006d:  blt.s      IL_007e
  IL_006f:  ldstr      "Case Is >= #8/13/2002 12:16:15 PM#"
  IL_0074:  call       "Sub System.Console.WriteLine(String)"
  IL_0079:  br         IL_011f
  IL_007e:  ldloc.0
  IL_007f:  ldc.i8     0x8c410da0a463400
  IL_0088:  newobj     "Sub Date..ctor(Long)"
  IL_008d:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_0092:  ldc.i4.0
  IL_0093:  blt.s      IL_00b8
  IL_0095:  ldloc.0
  IL_0096:  ldc.i8     0x8c410da51ccc000
  IL_009f:  newobj     "Sub Date..ctor(Long)"
  IL_00a4:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_00a9:  ldc.i4.0
  IL_00aa:  bgt.s      IL_00b8
  IL_00ac:  ldstr      "Case #8/13/2002 12:14 PM# To #8/13/2002 12:16 PM#"
  IL_00b1:  call       "Sub System.Console.WriteLine(String)"
  IL_00b6:  br.s       IL_011f
  IL_00b8:  ldloc.0
  IL_00b9:  ldc.i8     0x8c410d815986000
  IL_00c2:  newobj     "Sub Date..ctor(Long)"
  IL_00c7:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_00cc:  brtrue.s   IL_00da
  IL_00ce:  ldstr      "Case #8/13/2002 12 PM#"
  IL_00d3:  call       "Sub System.Console.WriteLine(String)"
  IL_00d8:  br.s       IL_011f
  IL_00da:  ldloc.0
  IL_00db:  ldc.i8     0x8c410d8c868be00
  IL_00e4:  newobj     "Sub Date..ctor(Long)"
  IL_00e9:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_00ee:  brtrue.s   IL_00fc
  IL_00f0:  ldstr      "Case #8/13/2002 12:05 PM#"
  IL_00f5:  call       "Sub System.Console.WriteLine(String)"
  IL_00fa:  br.s       IL_011f
  IL_00fc:  ldloc.0
  IL_00fd:  ldsfld     "Date.MinValue As Date"
  IL_0102:  call       "Function Date.Compare(Date, Date) As Integer"
  IL_0107:  brtrue.s   IL_0115
  IL_0109:  ldstr      "Case Nothing"
  IL_010e:  call       "Sub System.Console.WriteLine(String)"
  IL_0113:  br.s       IL_011f
  IL_0115:  ldstr      "Case Else"
  IL_011a:  call       "Sub System.Console.WriteLine(String)"
  IL_011f:  call       "Sub System.Console.WriteLine()"
  IL_0124:  ret
}]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SimpleSelectCase_SwitchTable_Integer()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(number as Integer)
        Select Case number
            Case 0
                Console.WriteLine("Equal to 0")
            Case 1, 2, 3, 4, 5
                Console.WriteLine("Between 1 and 5, inclusive")
            Case 6, 7, 8
                Console.WriteLine("Between 6 and 8, inclusive")
            Case 9, 10
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      109 (0x6d)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  switch    (
  IL_0036,
  IL_0041,
  IL_0041,
  IL_0041,
  IL_0041,
  IL_0041,
  IL_004c,
  IL_004c,
  IL_004c,
  IL_0057,
  IL_0057)
  IL_0034:  br.s       IL_0062
  IL_0036:  ldstr      "Equal to 0"
  IL_003b:  call       "Sub System.Console.WriteLine(String)"
  IL_0040:  ret
  IL_0041:  ldstr      "Between 1 and 5, inclusive"
  IL_0046:  call       "Sub System.Console.WriteLine(String)"
  IL_004b:  ret
  IL_004c:  ldstr      "Between 6 and 8, inclusive"
  IL_0051:  call       "Sub System.Console.WriteLine(String)"
  IL_0056:  ret
  IL_0057:  ldstr      "Equal to 9 or 10"
  IL_005c:  call       "Sub System.Console.WriteLine(String)"
  IL_0061:  ret
  IL_0062:  ldstr      "Greater than 10"
  IL_0067:  call       "Sub System.Console.WriteLine(String)"
  IL_006c:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SimpleSelectCase_SwitchTable_Signed()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 To 11
            Console.WriteLine(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(count As Integer)
        Dim i8 As SByte = count
        Dim i16 As Int16 = count
        Dim i64 As Int64 = count

        Select Case i8
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select

        Select Case i16
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select

        Select Case i64
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:
Equal to 0
Equal to 0
Equal to 0
1:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
2:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
3:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
4:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
5:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
6:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
7:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
8:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
9:
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
10:
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
11:
Greater than 10
Greater than 10
Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      451 (0x1c3)
  .maxstack  3
  .locals init (SByte V_0, //i8
  Short V_1, //i16
  Long V_2) //i64
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  conv.ovf.i2
  IL_0005:  stloc.1
  IL_0006:  ldarg.0
  IL_0007:  conv.i8
  IL_0008:  stloc.2
  IL_0009:  ldloc.0
  IL_000a:  switch    (
  IL_0053,
  IL_0076,
  IL_0076,
  IL_0076,
  IL_0076,
  IL_0076,
  IL_0062,
  IL_0062,
  IL_0062,
  IL_003d,
  IL_003d)
  IL_003b:  br.s       IL_008b
  IL_003d:  ldarg.0
  IL_003e:  ldc.i4.s   9
  IL_0040:  blt.s      IL_009a
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.s   10
  IL_0045:  bgt.s      IL_009a
  IL_0047:  ldstr      "Equal to 9 or 10"
  IL_004c:  call       "Sub System.Console.WriteLine(String)"
  IL_0051:  br.s       IL_009a
  IL_0053:  ldarg.0
  IL_0054:  brtrue.s   IL_009a
  IL_0056:  ldstr      "Equal to 0"
  IL_005b:  call       "Sub System.Console.WriteLine(String)"
  IL_0060:  br.s       IL_009a
  IL_0062:  ldarg.0
  IL_0063:  ldc.i4.6
  IL_0064:  blt.s      IL_009a
  IL_0066:  ldarg.0
  IL_0067:  ldc.i4.8
  IL_0068:  bgt.s      IL_009a
  IL_006a:  ldstr      "Between 6 and 8, inclusive"
  IL_006f:  call       "Sub System.Console.WriteLine(String)"
  IL_0074:  br.s       IL_009a
  IL_0076:  ldarg.0
  IL_0077:  ldc.i4.1
  IL_0078:  blt.s      IL_009a
  IL_007a:  ldarg.0
  IL_007b:  ldc.i4.s   10
  IL_007d:  bgt.s      IL_009a
  IL_007f:  ldstr      "Between 1 and 5, inclusive"
  IL_0084:  call       "Sub System.Console.WriteLine(String)"
  IL_0089:  br.s       IL_009a
  IL_008b:  ldarg.0
  IL_008c:  ldc.i4.s   10
  IL_008e:  ble.s      IL_009a
  IL_0090:  ldstr      "Greater than 10"
  IL_0095:  call       "Sub System.Console.WriteLine(String)"
  IL_009a:  ldloc.1
  IL_009b:  switch    (
  IL_00e4,
  IL_0107,
  IL_0107,
  IL_0107,
  IL_0107,
  IL_0107,
  IL_00f3,
  IL_00f3,
  IL_00f3,
  IL_00ce,
  IL_00ce)
  IL_00cc:  br.s       IL_011c
  IL_00ce:  ldarg.0
  IL_00cf:  ldc.i4.s   9
  IL_00d1:  blt.s      IL_012b
  IL_00d3:  ldarg.0
  IL_00d4:  ldc.i4.s   10
  IL_00d6:  bgt.s      IL_012b
  IL_00d8:  ldstr      "Equal to 9 or 10"
  IL_00dd:  call       "Sub System.Console.WriteLine(String)"
  IL_00e2:  br.s       IL_012b
  IL_00e4:  ldarg.0
  IL_00e5:  brtrue.s   IL_012b
  IL_00e7:  ldstr      "Equal to 0"
  IL_00ec:  call       "Sub System.Console.WriteLine(String)"
  IL_00f1:  br.s       IL_012b
  IL_00f3:  ldarg.0
  IL_00f4:  ldc.i4.6
  IL_00f5:  blt.s      IL_012b
  IL_00f7:  ldarg.0
  IL_00f8:  ldc.i4.8
  IL_00f9:  bgt.s      IL_012b
  IL_00fb:  ldstr      "Between 6 and 8, inclusive"
  IL_0100:  call       "Sub System.Console.WriteLine(String)"
  IL_0105:  br.s       IL_012b
  IL_0107:  ldarg.0
  IL_0108:  ldc.i4.1
  IL_0109:  blt.s      IL_012b
  IL_010b:  ldarg.0
  IL_010c:  ldc.i4.s   10
  IL_010e:  bgt.s      IL_012b
  IL_0110:  ldstr      "Between 1 and 5, inclusive"
  IL_0115:  call       "Sub System.Console.WriteLine(String)"
  IL_011a:  br.s       IL_012b
  IL_011c:  ldarg.0
  IL_011d:  ldc.i4.s   10
  IL_011f:  ble.s      IL_012b
  IL_0121:  ldstr      "Greater than 10"
  IL_0126:  call       "Sub System.Console.WriteLine(String)"
  IL_012b:  ldloc.2
  IL_012c:  dup
  IL_012d:  ldc.i4.s   10
  IL_012f:  conv.i8
  IL_0130:  ble.un.s   IL_0135
  IL_0132:  pop
  IL_0133:  br.s       IL_01b3
  IL_0135:  conv.u4
  IL_0136:  switch    (
  IL_017e,
  IL_019f,
  IL_019f,
  IL_019f,
  IL_019f,
  IL_019f,
  IL_018c,
  IL_018c,
  IL_018c,
  IL_0169,
  IL_0169)
  IL_0167:  br.s       IL_01b3
  IL_0169:  ldarg.0
  IL_016a:  ldc.i4.s   9
  IL_016c:  blt.s      IL_01c2
  IL_016e:  ldarg.0
  IL_016f:  ldc.i4.s   10
  IL_0171:  bgt.s      IL_01c2
  IL_0173:  ldstr      "Equal to 9 or 10"
  IL_0178:  call       "Sub System.Console.WriteLine(String)"
  IL_017d:  ret
  IL_017e:  ldarg.0
  IL_017f:  brtrue.s   IL_01c2
  IL_0181:  ldstr      "Equal to 0"
  IL_0186:  call       "Sub System.Console.WriteLine(String)"
  IL_018b:  ret
  IL_018c:  ldarg.0
  IL_018d:  ldc.i4.6
  IL_018e:  blt.s      IL_01c2
  IL_0190:  ldarg.0
  IL_0191:  ldc.i4.8
  IL_0192:  bgt.s      IL_01c2
  IL_0194:  ldstr      "Between 6 and 8, inclusive"
  IL_0199:  call       "Sub System.Console.WriteLine(String)"
  IL_019e:  ret
  IL_019f:  ldarg.0
  IL_01a0:  ldc.i4.1
  IL_01a1:  blt.s      IL_01c2
  IL_01a3:  ldarg.0
  IL_01a4:  ldc.i4.s   10
  IL_01a6:  bgt.s      IL_01c2
  IL_01a8:  ldstr      "Between 1 and 5, inclusive"
  IL_01ad:  call       "Sub System.Console.WriteLine(String)"
  IL_01b2:  ret
  IL_01b3:  ldarg.0
  IL_01b4:  ldc.i4.s   10
  IL_01b6:  ble.s      IL_01c2
  IL_01b8:  ldstr      "Greater than 10"
  IL_01bd:  call       "Sub System.Console.WriteLine(String)"
  IL_01c2:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SimpleSelectCase_SwitchTable_Unsigned()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 To 11
            Console.WriteLine(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(count As Integer)
        Dim b As Byte = count
        Dim ui16 As UInt16 = count
        Dim ui32 As UInt32 = count
        Dim ui64 As UInt64 = count

        Select Case b
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select

        Select Case ui16
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select

        Select Case ui32
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select

        Select Case ui64
            Case 9, 10
                If count >= 9 AndAlso count <= 10 Then
                    Console.WriteLine("Equal to 9 or 10")
                End If
            Case 0
                If count = 0 Then
                    Console.WriteLine("Equal to 0")
                End If
            Case 6, 7, 8
                If count >= 6 AndAlso count <= 8 Then
                    Console.WriteLine("Between 6 and 8, inclusive")
                End If
            Case 1, 2, 3, 4, 5
                If count >= 1 AndAlso count <= 10 Then
                    Console.WriteLine("Between 1 and 5, inclusive")
                End If
            Case Else
                If count > 10 Then
                    Console.WriteLine("Greater than 10")
                End If
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:
Equal to 0
Equal to 0
Equal to 0
Equal to 0
1:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
2:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
3:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
4:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
5:
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
Between 1 and 5, inclusive
6:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
7:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
8:
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
Between 6 and 8, inclusive
9:
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
10:
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
Equal to 9 or 10
11:
Greater than 10
Greater than 10
Greater than 10
Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      599 (0x257)
  .maxstack  3
  .locals init (Byte V_0, //b
  UShort V_1, //ui16
  UInteger V_2, //ui32
  ULong V_3) //ui64
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  conv.ovf.u2
  IL_0005:  stloc.1
  IL_0006:  ldarg.0
  IL_0007:  conv.ovf.u4
  IL_0008:  stloc.2
  IL_0009:  ldarg.0
  IL_000a:  conv.ovf.u8
  IL_000b:  stloc.3
  IL_000c:  ldloc.0
  IL_000d:  switch    (
  IL_0056,
  IL_0079,
  IL_0079,
  IL_0079,
  IL_0079,
  IL_0079,
  IL_0065,
  IL_0065,
  IL_0065,
  IL_0040,
  IL_0040)
  IL_003e:  br.s       IL_008e
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.s   9
  IL_0043:  blt.s      IL_009d
  IL_0045:  ldarg.0
  IL_0046:  ldc.i4.s   10
  IL_0048:  bgt.s      IL_009d
  IL_004a:  ldstr      "Equal to 9 or 10"
  IL_004f:  call       "Sub System.Console.WriteLine(String)"
  IL_0054:  br.s       IL_009d
  IL_0056:  ldarg.0
  IL_0057:  brtrue.s   IL_009d
  IL_0059:  ldstr      "Equal to 0"
  IL_005e:  call       "Sub System.Console.WriteLine(String)"
  IL_0063:  br.s       IL_009d
  IL_0065:  ldarg.0
  IL_0066:  ldc.i4.6
  IL_0067:  blt.s      IL_009d
  IL_0069:  ldarg.0
  IL_006a:  ldc.i4.8
  IL_006b:  bgt.s      IL_009d
  IL_006d:  ldstr      "Between 6 and 8, inclusive"
  IL_0072:  call       "Sub System.Console.WriteLine(String)"
  IL_0077:  br.s       IL_009d
  IL_0079:  ldarg.0
  IL_007a:  ldc.i4.1
  IL_007b:  blt.s      IL_009d
  IL_007d:  ldarg.0
  IL_007e:  ldc.i4.s   10
  IL_0080:  bgt.s      IL_009d
  IL_0082:  ldstr      "Between 1 and 5, inclusive"
  IL_0087:  call       "Sub System.Console.WriteLine(String)"
  IL_008c:  br.s       IL_009d
  IL_008e:  ldarg.0
  IL_008f:  ldc.i4.s   10
  IL_0091:  ble.s      IL_009d
  IL_0093:  ldstr      "Greater than 10"
  IL_0098:  call       "Sub System.Console.WriteLine(String)"
  IL_009d:  ldloc.1
  IL_009e:  switch    (
  IL_00e7,
  IL_010a,
  IL_010a,
  IL_010a,
  IL_010a,
  IL_010a,
  IL_00f6,
  IL_00f6,
  IL_00f6,
  IL_00d1,
  IL_00d1)
  IL_00cf:  br.s       IL_011f
  IL_00d1:  ldarg.0
  IL_00d2:  ldc.i4.s   9
  IL_00d4:  blt.s      IL_012e
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   10
  IL_00d9:  bgt.s      IL_012e
  IL_00db:  ldstr      "Equal to 9 or 10"
  IL_00e0:  call       "Sub System.Console.WriteLine(String)"
  IL_00e5:  br.s       IL_012e
  IL_00e7:  ldarg.0
  IL_00e8:  brtrue.s   IL_012e
  IL_00ea:  ldstr      "Equal to 0"
  IL_00ef:  call       "Sub System.Console.WriteLine(String)"
  IL_00f4:  br.s       IL_012e
  IL_00f6:  ldarg.0
  IL_00f7:  ldc.i4.6
  IL_00f8:  blt.s      IL_012e
  IL_00fa:  ldarg.0
  IL_00fb:  ldc.i4.8
  IL_00fc:  bgt.s      IL_012e
  IL_00fe:  ldstr      "Between 6 and 8, inclusive"
  IL_0103:  call       "Sub System.Console.WriteLine(String)"
  IL_0108:  br.s       IL_012e
  IL_010a:  ldarg.0
  IL_010b:  ldc.i4.1
  IL_010c:  blt.s      IL_012e
  IL_010e:  ldarg.0
  IL_010f:  ldc.i4.s   10
  IL_0111:  bgt.s      IL_012e
  IL_0113:  ldstr      "Between 1 and 5, inclusive"
  IL_0118:  call       "Sub System.Console.WriteLine(String)"
  IL_011d:  br.s       IL_012e
  IL_011f:  ldarg.0
  IL_0120:  ldc.i4.s   10
  IL_0122:  ble.s      IL_012e
  IL_0124:  ldstr      "Greater than 10"
  IL_0129:  call       "Sub System.Console.WriteLine(String)"
  IL_012e:  ldloc.2
  IL_012f:  switch    (
  IL_0178,
  IL_019b,
  IL_019b,
  IL_019b,
  IL_019b,
  IL_019b,
  IL_0187,
  IL_0187,
  IL_0187,
  IL_0162,
  IL_0162)
  IL_0160:  br.s       IL_01b0
  IL_0162:  ldarg.0
  IL_0163:  ldc.i4.s   9
  IL_0165:  blt.s      IL_01bf
  IL_0167:  ldarg.0
  IL_0168:  ldc.i4.s   10
  IL_016a:  bgt.s      IL_01bf
  IL_016c:  ldstr      "Equal to 9 or 10"
  IL_0171:  call       "Sub System.Console.WriteLine(String)"
  IL_0176:  br.s       IL_01bf
  IL_0178:  ldarg.0
  IL_0179:  brtrue.s   IL_01bf
  IL_017b:  ldstr      "Equal to 0"
  IL_0180:  call       "Sub System.Console.WriteLine(String)"
  IL_0185:  br.s       IL_01bf
  IL_0187:  ldarg.0
  IL_0188:  ldc.i4.6
  IL_0189:  blt.s      IL_01bf
  IL_018b:  ldarg.0
  IL_018c:  ldc.i4.8
  IL_018d:  bgt.s      IL_01bf
  IL_018f:  ldstr      "Between 6 and 8, inclusive"
  IL_0194:  call       "Sub System.Console.WriteLine(String)"
  IL_0199:  br.s       IL_01bf
  IL_019b:  ldarg.0
  IL_019c:  ldc.i4.1
  IL_019d:  blt.s      IL_01bf
  IL_019f:  ldarg.0
  IL_01a0:  ldc.i4.s   10
  IL_01a2:  bgt.s      IL_01bf
  IL_01a4:  ldstr      "Between 1 and 5, inclusive"
  IL_01a9:  call       "Sub System.Console.WriteLine(String)"
  IL_01ae:  br.s       IL_01bf
  IL_01b0:  ldarg.0
  IL_01b1:  ldc.i4.s   10
  IL_01b3:  ble.s      IL_01bf
  IL_01b5:  ldstr      "Greater than 10"
  IL_01ba:  call       "Sub System.Console.WriteLine(String)"
  IL_01bf:  ldloc.3
  IL_01c0:  dup
  IL_01c1:  ldc.i4.s   10
  IL_01c3:  conv.i8
  IL_01c4:  ble.un.s   IL_01c9
  IL_01c6:  pop
  IL_01c7:  br.s       IL_0247
  IL_01c9:  conv.u4
  IL_01ca:  switch    (
  IL_0212,
  IL_0233,
  IL_0233,
  IL_0233,
  IL_0233,
  IL_0233,
  IL_0220,
  IL_0220,
  IL_0220,
  IL_01fd,
  IL_01fd)
  IL_01fb:  br.s       IL_0247
  IL_01fd:  ldarg.0
  IL_01fe:  ldc.i4.s   9
  IL_0200:  blt.s      IL_0256
  IL_0202:  ldarg.0
  IL_0203:  ldc.i4.s   10
  IL_0205:  bgt.s      IL_0256
  IL_0207:  ldstr      "Equal to 9 or 10"
  IL_020c:  call       "Sub System.Console.WriteLine(String)"
  IL_0211:  ret
  IL_0212:  ldarg.0
  IL_0213:  brtrue.s   IL_0256
  IL_0215:  ldstr      "Equal to 0"
  IL_021a:  call       "Sub System.Console.WriteLine(String)"
  IL_021f:  ret
  IL_0220:  ldarg.0
  IL_0221:  ldc.i4.6
  IL_0222:  blt.s      IL_0256
  IL_0224:  ldarg.0
  IL_0225:  ldc.i4.8
  IL_0226:  bgt.s      IL_0256
  IL_0228:  ldstr      "Between 6 and 8, inclusive"
  IL_022d:  call       "Sub System.Console.WriteLine(String)"
  IL_0232:  ret
  IL_0233:  ldarg.0
  IL_0234:  ldc.i4.1
  IL_0235:  blt.s      IL_0256
  IL_0237:  ldarg.0
  IL_0238:  ldc.i4.s   10
  IL_023a:  bgt.s      IL_0256
  IL_023c:  ldstr      "Between 1 and 5, inclusive"
  IL_0241:  call       "Sub System.Console.WriteLine(String)"
  IL_0246:  ret
  IL_0247:  ldarg.0
  IL_0248:  ldc.i4.s   10
  IL_024a:  ble.s      IL_0256
  IL_024c:  ldstr      "Greater than 10"
  IL_0251:  call       "Sub System.Console.WriteLine(String)"
  IL_0256:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub CaseElseOnlySelectCase()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Dim number As Integer = 0
        Select Case number
            Case Else
                Console.WriteLine("CaseElse")
        End Select

        Select Case 0
            Case Else
                Console.WriteLine("CaseElse")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[CaseElse
CaseElse]]>).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (Integer V_0, //number
  Integer V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldstr      "CaseElse"
  IL_0007:  call       "Sub System.Console.WriteLine(String)"
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  IL_000e:  ldstr      "CaseElse"
  IL_0013:  call       "Sub System.Console.WriteLine(String)"
  IL_0018:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_NonNullableExpr_NothingCaseClause()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        Select Case x
            Case Nothing, 1, 2, 3, 4
                Console.Write("Success")
            Case 0
                Console.Write("Fail")
            Case Else
                Console.Write("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Integer V_0, //x
  Integer V_1)
  IL_0000:  ldloc.0
  IL_0001:  stloc.1
  IL_0002:  ldloc.1
  IL_0003:  brfalse.s  IL_0015
  IL_0005:  ldloc.1
  IL_0006:  ldc.i4.1
  IL_0007:  beq.s      IL_0015
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4.2
  IL_000b:  beq.s      IL_0015
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4.3
  IL_000f:  beq.s      IL_0015
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4.4
  IL_0013:  bne.un.s   IL_0020
  IL_0015:  ldstr      "Success"
  IL_001a:  call       "Sub System.Console.Write(String)"
  IL_001f:  ret
  IL_0020:  ldloc.1
  IL_0021:  brtrue.s   IL_002e
  IL_0023:  ldstr      "Fail"
  IL_0028:  call       "Sub System.Console.Write(String)"
  IL_002d:  ret
  IL_002e:  ldstr      "Fail"
  IL_0033:  call       "Sub System.Console.Write(String)"
  IL_0038:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_NothingSelectExpr()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case Nothing
            Case 1, 2, 3, 4
                Console.Write("Fail")
            Case 0
                Console.Write("Success")
            Case Else
                Console.Write("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size      143 (0x8f)
  .maxstack  3
  .locals init (Object V_0)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  box        "Integer"
  IL_0009:  ldc.i4.0
  IL_000a:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual(Object, Object, Boolean) As Object"
  IL_000f:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0014:  brtrue.s   IL_0052
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.2
  IL_0018:  box        "Integer"
  IL_001d:  ldc.i4.0
  IL_001e:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual(Object, Object, Boolean) As Object"
  IL_0023:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0028:  brtrue.s   IL_0052
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.3
  IL_002c:  box        "Integer"
  IL_0031:  ldc.i4.0
  IL_0032:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual(Object, Object, Boolean) As Object"
  IL_0037:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_003c:  brtrue.s   IL_0052
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.4
  IL_0040:  box        "Integer"
  IL_0045:  ldc.i4.0
  IL_0046:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual(Object, Object, Boolean) As Object"
  IL_004b:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0050:  br.s       IL_0053
  IL_0052:  ldc.i4.1
  IL_0053:  box        "Boolean"
  IL_0058:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_005d:  brfalse.s  IL_006a
  IL_005f:  ldstr      "Fail"
  IL_0064:  call       "Sub System.Console.Write(String)"
  IL_0069:  ret
  IL_006a:  ldloc.0
  IL_006b:  ldc.i4.0
  IL_006c:  box        "Integer"
  IL_0071:  ldc.i4.0
  IL_0072:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ConditionalCompareObjectEqual(Object, Object, Boolean) As Boolean"
  IL_0077:  brfalse.s  IL_0084
  IL_0079:  ldstr      "Success"
  IL_007e:  call       "Sub System.Console.Write(String)"
  IL_0083:  ret
  IL_0084:  ldstr      "Fail"
  IL_0089:  call       "Sub System.Console.Write(String)"
  IL_008e:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ValueClause_01()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case 0, 1
                Console.WriteLine("Success")
            Case 1 - 1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  bgt.un.s   IL_0011
  IL_0006:  ldstr      "Success"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldstr      "Fail"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ValueClause_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case 1, 2
                Console.WriteLine("Fail")
            Case 1 - 1
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_0016
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  ldc.i4.1
  IL_0009:  bgt.un.s   IL_0021
  IL_000b:  ldstr      "Fail"
  IL_0010:  call       "Sub System.Console.WriteLine(String)"
  IL_0015:  ret
  IL_0016:  ldstr      "Success"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
  IL_0021:  ldstr      "Fail"
  IL_0026:  call       "Sub System.Console.WriteLine(String)"
  IL_002b:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ValueClause_03()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case 1, 2
                Console.WriteLine("Fail")
            Case 3, 4
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Success")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  ldc.i4.1
  IL_0006:  ble.un.s   IL_0010
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.3
  IL_000a:  sub
  IL_000b:  ldc.i4.1
  IL_000c:  ble.un.s   IL_001b
  IL_000e:  br.s       IL_0026
  IL_0010:  ldstr      "Fail"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldstr      "Fail"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ret
  IL_0026:  ldstr      "Success"
  IL_002b:  call       "Sub System.Console.WriteLine(String)"
  IL_0030:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_RelationalClause()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case Is < 1
                Console.WriteLine("Success")
            Case 0
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Success"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldstr      "Fail"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  ret
  IL_001f:  ldstr      "Fail"
  IL_0024:  call       "Sub System.Console.WriteLine(String)"
  IL_0029:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_RelationalAndRangeClause_01()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case Is < 0
                Console.WriteLine("Fail")
            Case -1 To 1
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Fail"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  blt.s      IL_0024
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  bgt.s      IL_0024
  IL_0019:  ldstr      "Success"
  IL_001e:  call       "Sub System.Console.WriteLine(String)"
  IL_0023:  ret
  IL_0024:  ldstr      "Fail"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_RelationalAndRangeClause_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case Is < 0
                Console.WriteLine("Fail")
            Case -2 To -1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Success")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Fail"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.s   -2
  IL_0014:  blt.s      IL_0025
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.m1
  IL_0018:  bgt.s      IL_0025
  IL_001a:  ldstr      "Fail"
  IL_001f:  call       "Sub System.Console.WriteLine(String)"
  IL_0024:  ret
  IL_0025:  ldstr      "Success"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_NoTrueClause()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case Is < 0
                Console.WriteLine("Fail")
                Return
            Case -2 To -1
                Console.WriteLine("Fail")
                Return
        End Select
        Console.WriteLine("Success")
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Fail"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.s   -2
  IL_0014:  blt.s      IL_0025
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.m1
  IL_0018:  bgt.s      IL_0025
  IL_001a:  ldstr      "Fail"
  IL_001f:  call       "Sub System.Console.WriteLine(String)"
  IL_0024:  ret
  IL_0025:  ldstr      "Success"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_01()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo()
                Console.WriteLine("Success")
            Case 0
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  bne.un.s   IL_0015
  IL_000a:  ldstr      "Success"
  IL_000f:  call       "Sub System.Console.WriteLine(String)"
  IL_0014:  ret
  IL_0015:  ldloc.0
  IL_0016:  brtrue.s   IL_0023
  IL_0018:  ldstr      "Fail"
  IL_001d:  call       "Sub System.Console.WriteLine(String)"
  IL_0022:  ret
  IL_0023:  ldstr      "Fail"
  IL_0028:  call       "Sub System.Console.WriteLine(String)"
  IL_002d:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation__02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo(), 0
                Console.WriteLine("Success")
            Case 1 - 1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  beq.s      IL_000d
  IL_000a:  ldloc.0
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  ldstr      "Success"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
  IL_0018:  ldloc.0
  IL_0019:  brtrue.s   IL_0026
  IL_001b:  ldstr      "Fail"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ret
  IL_0026:  ldstr      "Fail"
  IL_002b:  call       "Sub System.Console.WriteLine(String)"
  IL_0030:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation__03()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo() + 1, 2
                Console.WriteLine("Fail")
            Case 3, 4
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Success")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  add.ovf
  IL_000a:  beq.s      IL_0010
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.2
  IL_000e:  bne.un.s   IL_001b
  IL_0010:  ldstr      "Fail"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4.3
  IL_001d:  beq.s      IL_0023
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.4
  IL_0021:  bne.un.s   IL_002e
  IL_0023:  ldstr      "Fail"
  IL_0028:  call       "Sub System.Console.WriteLine(String)"
  IL_002d:  ret
  IL_002e:  ldstr      "Success"
  IL_0033:  call       "Sub System.Console.WriteLine(String)"
  IL_0038:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_04()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Is < Goo() + 1
                Console.WriteLine("Success")
            Case 0
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  add.ovf
  IL_000a:  bge.s      IL_0017
  IL_000c:  ldstr      "Success"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  brtrue.s   IL_0025
  IL_001a:  ldstr      "Fail"
  IL_001f:  call       "Sub System.Console.WriteLine(String)"
  IL_0024:  ret
  IL_0025:  ldstr      "Fail"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_05()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo() - 1 To Goo() + 1
                Console.WriteLine("Success")
            Case 0
                Console.Write("Fail")
            Case Else
                Console.Write("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  sub.ovf
  IL_000a:  blt.s      IL_0021
  IL_000c:  ldloc.0
  IL_000d:  call       "Function M1.Goo() As Integer"
  IL_0012:  ldc.i4.1
  IL_0013:  add.ovf
  IL_0014:  bgt.s      IL_0021
  IL_0016:  ldstr      "Success"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
  IL_0021:  ldloc.0
  IL_0022:  brtrue.s   IL_002f
  IL_0024:  ldstr      "Fail"
  IL_0029:  call       "Sub System.Console.Write(String)"
  IL_002e:  ret
  IL_002f:  ldstr      "Fail"
  IL_0034:  call       "Sub System.Console.Write(String)"
  IL_0039:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_06_ShortCircuit()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo() + 1 To Goo() + 2
                Console.WriteLine("Fail")
            Case 0
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  add.ovf
  IL_000a:  blt.s      IL_0021
  IL_000c:  ldloc.0
  IL_000d:  call       "Function M1.Goo() As Integer"
  IL_0012:  ldc.i4.2
  IL_0013:  add.ovf
  IL_0014:  bgt.s      IL_0021
  IL_0016:  ldstr      "Fail"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
  IL_0021:  ldloc.0
  IL_0022:  brtrue.s   IL_002f
  IL_0024:  ldstr      "Success"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ret
  IL_002f:  ldstr      "Fail"
  IL_0034:  call       "Sub System.Console.WriteLine(String)"
  IL_0039:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_07_ShortCircuit()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Goo() - 1 To 1
                Console.Write("Success,")
            Case 0
                Console.Write("Fail,")
            Case Else
                Console.Write("Fail")
        End Select

        Select Case 0
            Case 1 To Goo()
                Console.Write("Fail,")
            Case Else
                Console.Write("Success,")
        End Select

        Select Case 0
            Case Goo() - 1 To -2
                Console.WriteLine("Fail")
            Case 0
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select

    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success,Success,Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size      142 (0x8e)
  .maxstack  3
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function M1.Goo() As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  sub.ovf
  IL_000a:  blt.s      IL_001c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  bgt.s      IL_001c
  IL_0010:  ldstr      "Success,"
  IL_0015:  call       "Sub System.Console.Write(String)"
  IL_001a:  br.s       IL_0035
  IL_001c:  ldloc.0
  IL_001d:  brtrue.s   IL_002b
  IL_001f:  ldstr      "Fail,"
  IL_0024:  call       "Sub System.Console.Write(String)"
  IL_0029:  br.s       IL_0035
  IL_002b:  ldstr      "Fail"
  IL_0030:  call       "Sub System.Console.Write(String)"
  IL_0035:  ldc.i4.0
  IL_0036:  stloc.1
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4.1
  IL_0039:  blt.s      IL_004f
  IL_003b:  ldloc.1
  IL_003c:  call       "Function M1.Goo() As Integer"
  IL_0041:  bgt.s      IL_004f
  IL_0043:  ldstr      "Fail,"
  IL_0048:  call       "Sub System.Console.Write(String)"
  IL_004d:  br.s       IL_0059
  IL_004f:  ldstr      "Success,"
  IL_0054:  call       "Sub System.Console.Write(String)"
  IL_0059:  ldc.i4.0
  IL_005a:  stloc.2
  IL_005b:  ldloc.2
  IL_005c:  call       "Function M1.Goo() As Integer"
  IL_0061:  ldc.i4.1
  IL_0062:  sub.ovf
  IL_0063:  blt.s      IL_0075
  IL_0065:  ldloc.2
  IL_0066:  ldc.i4.s   -2
  IL_0068:  bgt.s      IL_0075
  IL_006a:  ldstr      "Fail"
  IL_006f:  call       "Sub System.Console.WriteLine(String)"
  IL_0074:  ret
  IL_0075:  ldloc.2
  IL_0076:  brtrue.s   IL_0083
  IL_0078:  ldstr      "Success"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ret
  IL_0083:  ldstr      "Fail"
  IL_0088:  call       "Sub System.Console.WriteLine(String)"
  IL_008d:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_08()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case -1 To Goo() + 1
                Console.WriteLine("Success")
            Case 0
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.m1
  IL_0004:  blt.s      IL_001b
  IL_0006:  ldloc.0
  IL_0007:  call       "Function M1.Goo() As Integer"
  IL_000c:  ldc.i4.1
  IL_000d:  add.ovf
  IL_000e:  bgt.s      IL_001b
  IL_0010:  ldstr      "Success"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldloc.0
  IL_001c:  brtrue.s   IL_0029
  IL_001e:  ldstr      "Fail"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
  IL_0029:  ldstr      "Fail"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_ClauseExprEvaluation_09()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case 0
            Case Is < 0
                Console.WriteLine("Fail")
            Case Goo() - 1 To Goo() + 1
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  bge.s      IL_0011
  IL_0006:  ldstr      "Fail"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
  IL_0011:  ldloc.0
  IL_0012:  call       "Function M1.Goo() As Integer"
  IL_0017:  ldc.i4.1
  IL_0018:  sub.ovf
  IL_0019:  blt.s      IL_0030
  IL_001b:  ldloc.0
  IL_001c:  call       "Function M1.Goo() As Integer"
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  bgt.s      IL_0030
  IL_0025:  ldstr      "Success"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ret
  IL_0030:  ldstr      "Fail"
  IL_0035:  call       "Sub System.Console.WriteLine(String)"
  IL_003a:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_SelectExprEvaluation_IfList()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case Goo()
            Case -2 To -1
                Console.WriteLine("Fail")
            Case 1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Success")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  call       "Function M1.Goo() As Integer"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   -2
  IL_0009:  blt.s      IL_001a
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.m1
  IL_000d:  bgt.s      IL_001a
  IL_000f:  ldstr      "Fail"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  bne.un.s   IL_0029
  IL_001e:  ldstr      "Fail"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
  IL_0029:  ldstr      "Success"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_SelectExprEvaluation_SwitchTable()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case Goo()
            Case -1, 1, 3
                Console.WriteLine("Fail")
            Case -2, 0, 2
                Console.WriteLine("Success")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  call       "Function M1.Goo() As Integer"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   -2
  IL_0009:  sub
  IL_000a:  switch    (
  IL_0034,
  IL_0029,
  IL_0034,
  IL_0029,
  IL_0034,
  IL_0029)
  IL_0027:  br.s       IL_003f
  IL_0029:  ldstr      "Fail"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
  IL_0034:  ldstr      "Success"
  IL_0039:  call       "Sub System.Console.WriteLine(String)"
  IL_003e:  ret
  IL_003f:  ldstr      "Fail"
  IL_0044:  call       "Sub System.Console.WriteLine(String)"
  IL_0049:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_SwitchTable_DuplicateCase()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case Goo()
            Case -1, 1, 3
                Console.WriteLine("Fail")
            Case 2.5 - 2.4
                Console.WriteLine("Success")
            Case -2, 0, 2
                Console.WriteLine("Fail")
            Case 0
                Console.WriteLine("Fail")
            Case 1 - 1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  call       "Function M1.Goo() As Integer"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   -2
  IL_0009:  sub
  IL_000a:  switch    (
  IL_003f,
  IL_0029,
  IL_0034,
  IL_0029,
  IL_003f,
  IL_0029)
  IL_0027:  br.s       IL_004a
  IL_0029:  ldstr      "Fail"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
  IL_0034:  ldstr      "Success"
  IL_0039:  call       "Sub System.Console.WriteLine(String)"
  IL_003e:  ret
  IL_003f:  ldstr      "Fail"
  IL_0044:  call       "Sub System.Console.WriteLine(String)"
  IL_0049:  ret
  IL_004a:  ldstr      "Fail"
  IL_004f:  call       "Sub System.Console.WriteLine(String)"
  IL_0054:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_IfList_Conversions()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Dim success As Boolean = True
        For count = 0 To 13
            Test(count, success)
        Next

        If success Then
            Console.Write("Success")
        Else
            Console.Write("Fail")
        End If
    End Sub

    Sub Test(count As Integer, ByRef success As Boolean)
        Dim Bo As Boolean
        Dim Ob As Object
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim St As String
        
        Bo = False
        Ob = 1
        SB = 2
        By = 3
        Sh = 4
        US = 5
        [In] = 6
        UI = 7
        Lo = 8
        UL = 9
        Si = 10
        [Do] = 11
        De = 12D
        St = "13"
        
        Select Case count
            Case Bo
                success = success AndAlso If(count = 0, True, False)
            Case Ob
                success = success AndAlso If(count = 1, True, False)
            Case SB
                success = success AndAlso If(count = 2, True, False)
            Case By
                success = success AndAlso If(count = 3, True, False)
            Case Sh
                success = success AndAlso If(count = 4, True, False)
            Case US
                success = success AndAlso If(count = 5, True, False)
            Case [In]
                success = success AndAlso If(count = 6, True, False)
            Case UI
                success = success AndAlso If(count = 7, True, False)
            Case Lo
                success = success AndAlso If(count = 8, True, False)
            Case UL
                success = success AndAlso If(count = 9, True, False)
            Case Si
                success = success AndAlso If(count = 10, True, False)
            Case [Do]
                success = success AndAlso If(count = 11, True, False)
            Case De
                success = success AndAlso If(count = 12, True, False)
            Case St
                success = success AndAlso If(count = 13, True, False)
            Case Else
                success = False
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Test", <![CDATA[
{
  // Code size      398 (0x18e)
  .maxstack  3
  .locals init (Boolean V_0, //Bo
                Object V_1, //Ob
                SByte V_2, //SB
                Byte V_3, //By
                Short V_4, //Sh
                UShort V_5, //US
                Integer V_6, //In
                UInteger V_7, //UI
                Long V_8, //Lo
                ULong V_9, //UL
                Decimal V_10, //De
                Single V_11, //Si
                Double V_12, //Do
                String V_13, //St
                Integer V_14)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  box        "Integer"
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.2
  IL_000a:  stloc.2
  IL_000b:  ldc.i4.3
  IL_000c:  stloc.3
  IL_000d:  ldc.i4.4
  IL_000e:  stloc.s    V_4
  IL_0010:  ldc.i4.5
  IL_0011:  stloc.s    V_5
  IL_0013:  ldc.i4.6
  IL_0014:  stloc.s    V_6
  IL_0016:  ldc.i4.7
  IL_0017:  stloc.s    V_7
  IL_0019:  ldc.i4.8
  IL_001a:  conv.i8
  IL_001b:  stloc.s    V_8
  IL_001d:  ldc.i4.s   9
  IL_001f:  conv.i8
  IL_0020:  stloc.s    V_9
  IL_0022:  ldc.r4     10
  IL_0027:  stloc.s    V_11
  IL_0029:  ldc.r8     11
  IL_0032:  stloc.s    V_12
  IL_0034:  ldloca.s   V_10
  IL_0036:  ldc.i4.s   12
  IL_0038:  conv.i8
  IL_0039:  call       "Sub Decimal..ctor(Long)"
  IL_003e:  ldstr      "13"
  IL_0043:  stloc.s    V_13
  IL_0045:  ldarg.0
  IL_0046:  stloc.s    V_14
  IL_0048:  ldloc.s    V_14
  IL_004a:  ldloc.0
  IL_004b:  ldc.i4.0
  IL_004c:  cgt.un
  IL_004e:  neg
  IL_004f:  bne.un.s   IL_005f
  IL_0051:  ldarg.1
  IL_0052:  ldarg.1
  IL_0053:  ldind.u1
  IL_0054:  brfalse.s  IL_005c
  IL_0056:  ldarg.0
  IL_0057:  ldc.i4.0
  IL_0058:  ceq
  IL_005a:  br.s       IL_005d
  IL_005c:  ldc.i4.0
  IL_005d:  stind.i1
  IL_005e:  ret
  IL_005f:  ldloc.s    V_14
  IL_0061:  box        "Integer"
  IL_0066:  ldloc.1
  IL_0067:  ldc.i4.0
  IL_0068:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ConditionalCompareObjectEqual(Object, Object, Boolean) As Boolean"
  IL_006d:  brfalse.s  IL_007d
  IL_006f:  ldarg.1
  IL_0070:  ldarg.1
  IL_0071:  ldind.u1
  IL_0072:  brfalse.s  IL_007a
  IL_0074:  ldarg.0
  IL_0075:  ldc.i4.1
  IL_0076:  ceq
  IL_0078:  br.s       IL_007b
  IL_007a:  ldc.i4.0
  IL_007b:  stind.i1
  IL_007c:  ret
  IL_007d:  ldloc.s    V_14
  IL_007f:  ldloc.2
  IL_0080:  bne.un.s   IL_0090
  IL_0082:  ldarg.1
  IL_0083:  ldarg.1
  IL_0084:  ldind.u1
  IL_0085:  brfalse.s  IL_008d
  IL_0087:  ldarg.0
  IL_0088:  ldc.i4.2
  IL_0089:  ceq
  IL_008b:  br.s       IL_008e
  IL_008d:  ldc.i4.0
  IL_008e:  stind.i1
  IL_008f:  ret
  IL_0090:  ldloc.s    V_14
  IL_0092:  ldloc.3
  IL_0093:  bne.un.s   IL_00a3
  IL_0095:  ldarg.1
  IL_0096:  ldarg.1
  IL_0097:  ldind.u1
  IL_0098:  brfalse.s  IL_00a0
  IL_009a:  ldarg.0
  IL_009b:  ldc.i4.3
  IL_009c:  ceq
  IL_009e:  br.s       IL_00a1
  IL_00a0:  ldc.i4.0
  IL_00a1:  stind.i1
  IL_00a2:  ret
  IL_00a3:  ldloc.s    V_14
  IL_00a5:  ldloc.s    V_4
  IL_00a7:  bne.un.s   IL_00b7
  IL_00a9:  ldarg.1
  IL_00aa:  ldarg.1
  IL_00ab:  ldind.u1
  IL_00ac:  brfalse.s  IL_00b4
  IL_00ae:  ldarg.0
  IL_00af:  ldc.i4.4
  IL_00b0:  ceq
  IL_00b2:  br.s       IL_00b5
  IL_00b4:  ldc.i4.0
  IL_00b5:  stind.i1
  IL_00b6:  ret
  IL_00b7:  ldloc.s    V_14
  IL_00b9:  ldloc.s    V_5
  IL_00bb:  bne.un.s   IL_00cb
  IL_00bd:  ldarg.1
  IL_00be:  ldarg.1
  IL_00bf:  ldind.u1
  IL_00c0:  brfalse.s  IL_00c8
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.5
  IL_00c4:  ceq
  IL_00c6:  br.s       IL_00c9
  IL_00c8:  ldc.i4.0
  IL_00c9:  stind.i1
  IL_00ca:  ret
  IL_00cb:  ldloc.s    V_14
  IL_00cd:  ldloc.s    V_6
  IL_00cf:  bne.un.s   IL_00df
  IL_00d1:  ldarg.1
  IL_00d2:  ldarg.1
  IL_00d3:  ldind.u1
  IL_00d4:  brfalse.s  IL_00dc
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.6
  IL_00d8:  ceq
  IL_00da:  br.s       IL_00dd
  IL_00dc:  ldc.i4.0
  IL_00dd:  stind.i1
  IL_00de:  ret
  IL_00df:  ldloc.s    V_14
  IL_00e1:  ldloc.s    V_7
  IL_00e3:  conv.ovf.i4.un
  IL_00e4:  bne.un.s   IL_00f4
  IL_00e6:  ldarg.1
  IL_00e7:  ldarg.1
  IL_00e8:  ldind.u1
  IL_00e9:  brfalse.s  IL_00f1
  IL_00eb:  ldarg.0
  IL_00ec:  ldc.i4.7
  IL_00ed:  ceq
  IL_00ef:  br.s       IL_00f2
  IL_00f1:  ldc.i4.0
  IL_00f2:  stind.i1
  IL_00f3:  ret
  IL_00f4:  ldloc.s    V_14
  IL_00f6:  ldloc.s    V_8
  IL_00f8:  conv.ovf.i4
  IL_00f9:  bne.un.s   IL_0109
  IL_00fb:  ldarg.1
  IL_00fc:  ldarg.1
  IL_00fd:  ldind.u1
  IL_00fe:  brfalse.s  IL_0106
  IL_0100:  ldarg.0
  IL_0101:  ldc.i4.8
  IL_0102:  ceq
  IL_0104:  br.s       IL_0107
  IL_0106:  ldc.i4.0
  IL_0107:  stind.i1
  IL_0108:  ret
  IL_0109:  ldloc.s    V_14
  IL_010b:  ldloc.s    V_9
  IL_010d:  conv.ovf.i4.un
  IL_010e:  bne.un.s   IL_011f
  IL_0110:  ldarg.1
  IL_0111:  ldarg.1
  IL_0112:  ldind.u1
  IL_0113:  brfalse.s  IL_011c
  IL_0115:  ldarg.0
  IL_0116:  ldc.i4.s   9
  IL_0118:  ceq
  IL_011a:  br.s       IL_011d
  IL_011c:  ldc.i4.0
  IL_011d:  stind.i1
  IL_011e:  ret
  IL_011f:  ldloc.s    V_14
  IL_0121:  ldloc.s    V_11
  IL_0123:  conv.r8
  IL_0124:  call       "Function System.Math.Round(Double) As Double"
  IL_0129:  conv.ovf.i4
  IL_012a:  bne.un.s   IL_013b
  IL_012c:  ldarg.1
  IL_012d:  ldarg.1
  IL_012e:  ldind.u1
  IL_012f:  brfalse.s  IL_0138
  IL_0131:  ldarg.0
  IL_0132:  ldc.i4.s   10
  IL_0134:  ceq
  IL_0136:  br.s       IL_0139
  IL_0138:  ldc.i4.0
  IL_0139:  stind.i1
  IL_013a:  ret
  IL_013b:  ldloc.s    V_14
  IL_013d:  ldloc.s    V_12
  IL_013f:  call       "Function System.Math.Round(Double) As Double"
  IL_0144:  conv.ovf.i4
  IL_0145:  bne.un.s   IL_0156
  IL_0147:  ldarg.1
  IL_0148:  ldarg.1
  IL_0149:  ldind.u1
  IL_014a:  brfalse.s  IL_0153
  IL_014c:  ldarg.0
  IL_014d:  ldc.i4.s   11
  IL_014f:  ceq
  IL_0151:  br.s       IL_0154
  IL_0153:  ldc.i4.0
  IL_0154:  stind.i1
  IL_0155:  ret
  IL_0156:  ldloc.s    V_14
  IL_0158:  ldloc.s    V_10
  IL_015a:  call       "Function System.Convert.ToInt32(Decimal) As Integer"
  IL_015f:  bne.un.s   IL_0170
  IL_0161:  ldarg.1
  IL_0162:  ldarg.1
  IL_0163:  ldind.u1
  IL_0164:  brfalse.s  IL_016d
  IL_0166:  ldarg.0
  IL_0167:  ldc.i4.s   12
  IL_0169:  ceq
  IL_016b:  br.s       IL_016e
  IL_016d:  ldc.i4.0
  IL_016e:  stind.i1
  IL_016f:  ret
  IL_0170:  ldloc.s    V_14
  IL_0172:  ldloc.s    V_13
  IL_0174:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
  IL_0179:  bne.un.s   IL_018a
  IL_017b:  ldarg.1
  IL_017c:  ldarg.1
  IL_017d:  ldind.u1
  IL_017e:  brfalse.s  IL_0187
  IL_0180:  ldarg.0
  IL_0181:  ldc.i4.s   13
  IL_0183:  ceq
  IL_0185:  br.s       IL_0188
  IL_0187:  ldc.i4.0
  IL_0188:  stind.i1
  IL_0189:  ret
  IL_018a:  ldarg.1
  IL_018b:  ldc.i4.0
  IL_018c:  stind.i1
  IL_018d:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        ' TODO: Update test case once bug 10352 and bug 10354 are fixed.
        ' TODO: Verify switch table is used in codegen for select case statement.
        <WorkItem(542910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542910")>
        <WorkItem(10354, "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/10354")>
        <Fact()>
        Public Sub SelectCase_SwitchTable_Conversions_01()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub Main()
        Dim success As Boolean = True
        For count = 0 To 12
            Test(count, success)
        Next

        If success Then
            Console.Write("Success")
        Else
            Console.Write("Fail")
        End If
    End Sub

    Sub Test(count As Integer, ByRef success As Boolean)
        Const Bo As Boolean = False
        'Const Ob As Object = 1
        Const SB As SByte = 2
        Const By As Byte = 3
        Const Sh As Short = 4
        Const US As UShort = 5
        Const [In] As Integer = 6
        Const UI As UInteger = 7
        Const Lo As Long = 8
        Const UL As ULong = 9
        Const Si As Single = 10
        Const [Do] As Double = 11
        Const De As Decimal = 12D
        
        Select Case count
            Case Bo
                success = success AndAlso If(count = 0, True, False)
'            Case Ob
            Case 1
                success = success AndAlso If(count = 1, True, False)
            Case SB
                success = success AndAlso If(count = 2, True, False)
            Case By
                success = success AndAlso If(count = 3, True, False)
            Case Sh
                success = success AndAlso If(count = 4, True, False)
            Case US
                success = success AndAlso If(count = 5, True, False)
            Case [In]
                success = success AndAlso If(count = 6, True, False)
            Case UI
                success = success AndAlso If(count = 7, True, False)
            Case Lo
                success = success AndAlso If(count = 8, True, False)
            Case UL
                success = success AndAlso If(count = 9, True, False)
            Case Si
                success = success AndAlso If(count = 10, True, False)
            Case [Do]
                success = success AndAlso If(count = 11, True, False)
            Case De
                success = success AndAlso If(count = 12, True, False)
            Case Else
                success = False
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Success").VerifyIL("M1.Test", <![CDATA[
{
  // Code size      255 (0xff)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  switch    (
        IL_0041,
        IL_004f,
        IL_005d,
        IL_006b,
        IL_0079,
        IL_0087,
        IL_0095,
        IL_00a3,
        IL_00b1,
        IL_00bf,
        IL_00ce,
        IL_00dd,
        IL_00ec)
  IL_003c:  br         IL_00fb
  IL_0041:  ldarg.1
  IL_0042:  ldarg.1
  IL_0043:  ldind.u1
  IL_0044:  brfalse.s  IL_004c
  IL_0046:  ldarg.0
  IL_0047:  ldc.i4.0
  IL_0048:  ceq
  IL_004a:  br.s       IL_004d
  IL_004c:  ldc.i4.0
  IL_004d:  stind.i1
  IL_004e:  ret
  IL_004f:  ldarg.1
  IL_0050:  ldarg.1
  IL_0051:  ldind.u1
  IL_0052:  brfalse.s  IL_005a
  IL_0054:  ldarg.0
  IL_0055:  ldc.i4.1
  IL_0056:  ceq
  IL_0058:  br.s       IL_005b
  IL_005a:  ldc.i4.0
  IL_005b:  stind.i1
  IL_005c:  ret
  IL_005d:  ldarg.1
  IL_005e:  ldarg.1
  IL_005f:  ldind.u1
  IL_0060:  brfalse.s  IL_0068
  IL_0062:  ldarg.0
  IL_0063:  ldc.i4.2
  IL_0064:  ceq
  IL_0066:  br.s       IL_0069
  IL_0068:  ldc.i4.0
  IL_0069:  stind.i1
  IL_006a:  ret
  IL_006b:  ldarg.1
  IL_006c:  ldarg.1
  IL_006d:  ldind.u1
  IL_006e:  brfalse.s  IL_0076
  IL_0070:  ldarg.0
  IL_0071:  ldc.i4.3
  IL_0072:  ceq
  IL_0074:  br.s       IL_0077
  IL_0076:  ldc.i4.0
  IL_0077:  stind.i1
  IL_0078:  ret
  IL_0079:  ldarg.1
  IL_007a:  ldarg.1
  IL_007b:  ldind.u1
  IL_007c:  brfalse.s  IL_0084
  IL_007e:  ldarg.0
  IL_007f:  ldc.i4.4
  IL_0080:  ceq
  IL_0082:  br.s       IL_0085
  IL_0084:  ldc.i4.0
  IL_0085:  stind.i1
  IL_0086:  ret
  IL_0087:  ldarg.1
  IL_0088:  ldarg.1
  IL_0089:  ldind.u1
  IL_008a:  brfalse.s  IL_0092
  IL_008c:  ldarg.0
  IL_008d:  ldc.i4.5
  IL_008e:  ceq
  IL_0090:  br.s       IL_0093
  IL_0092:  ldc.i4.0
  IL_0093:  stind.i1
  IL_0094:  ret
  IL_0095:  ldarg.1
  IL_0096:  ldarg.1
  IL_0097:  ldind.u1
  IL_0098:  brfalse.s  IL_00a0
  IL_009a:  ldarg.0
  IL_009b:  ldc.i4.6
  IL_009c:  ceq
  IL_009e:  br.s       IL_00a1
  IL_00a0:  ldc.i4.0
  IL_00a1:  stind.i1
  IL_00a2:  ret
  IL_00a3:  ldarg.1
  IL_00a4:  ldarg.1
  IL_00a5:  ldind.u1
  IL_00a6:  brfalse.s  IL_00ae
  IL_00a8:  ldarg.0
  IL_00a9:  ldc.i4.7
  IL_00aa:  ceq
  IL_00ac:  br.s       IL_00af
  IL_00ae:  ldc.i4.0
  IL_00af:  stind.i1
  IL_00b0:  ret
  IL_00b1:  ldarg.1
  IL_00b2:  ldarg.1
  IL_00b3:  ldind.u1
  IL_00b4:  brfalse.s  IL_00bc
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.8
  IL_00b8:  ceq
  IL_00ba:  br.s       IL_00bd
  IL_00bc:  ldc.i4.0
  IL_00bd:  stind.i1
  IL_00be:  ret
  IL_00bf:  ldarg.1
  IL_00c0:  ldarg.1
  IL_00c1:  ldind.u1
  IL_00c2:  brfalse.s  IL_00cb
  IL_00c4:  ldarg.0
  IL_00c5:  ldc.i4.s   9
  IL_00c7:  ceq
  IL_00c9:  br.s       IL_00cc
  IL_00cb:  ldc.i4.0
  IL_00cc:  stind.i1
  IL_00cd:  ret
  IL_00ce:  ldarg.1
  IL_00cf:  ldarg.1
  IL_00d0:  ldind.u1
  IL_00d1:  brfalse.s  IL_00da
  IL_00d3:  ldarg.0
  IL_00d4:  ldc.i4.s   10
  IL_00d6:  ceq
  IL_00d8:  br.s       IL_00db
  IL_00da:  ldc.i4.0
  IL_00db:  stind.i1
  IL_00dc:  ret
  IL_00dd:  ldarg.1
  IL_00de:  ldarg.1
  IL_00df:  ldind.u1
  IL_00e0:  brfalse.s  IL_00e9
  IL_00e2:  ldarg.0
  IL_00e3:  ldc.i4.s   11
  IL_00e5:  ceq
  IL_00e7:  br.s       IL_00ea
  IL_00e9:  ldc.i4.0
  IL_00ea:  stind.i1
  IL_00eb:  ret
  IL_00ec:  ldarg.1
  IL_00ed:  ldarg.1
  IL_00ee:  ldind.u1
  IL_00ef:  brfalse.s  IL_00f8
  IL_00f1:  ldarg.0
  IL_00f2:  ldc.i4.s   12
  IL_00f4:  ceq
  IL_00f6:  br.s       IL_00f9
  IL_00f8:  ldc.i4.0
  IL_00f9:  stind.i1
  IL_00fa:  ret
  IL_00fb:  ldarg.1
  IL_00fc:  ldc.i4.0
  IL_00fd:  stind.i1
  IL_00fe:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_SwitchTable_Conversions_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Function Goo() As Integer
        Console.Write("Goo,")
        Return 0
    End Function

    Sub Main()
        Select Case Goo()
            Case -1, 1, 3
                Console.WriteLine("Fail")
            Case 2.5 - 2.4
                Console.WriteLine("Success")
            Case -2, 0, 2
                Console.WriteLine("Fail")
            Case 0
                Console.WriteLine("Fail")
            Case 1 - 1
                Console.WriteLine("Fail")
            Case Else
                Console.WriteLine("Fail")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="Goo,Success").VerifyIL("M1.Main", <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  call       "Function M1.Goo() As Integer"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   -2
  IL_0009:  sub
  IL_000a:  switch    (
  IL_003f,
  IL_0029,
  IL_0034,
  IL_0029,
  IL_003f,
  IL_0029)
  IL_0027:  br.s       IL_004a
  IL_0029:  ldstr      "Fail"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
  IL_0034:  ldstr      "Success"
  IL_0039:  call       "Sub System.Console.WriteLine(String)"
  IL_003e:  ret
  IL_003f:  ldstr      "Fail"
  IL_0044:  call       "Sub System.Console.WriteLine(String)"
  IL_0049:  ret
  IL_004a:  ldstr      "Fail"
  IL_004f:  call       "Sub System.Console.WriteLine(String)"
  IL_0054:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SwitchOnNullableInt64WithInt32Label()

            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module C
    Function F(ByVal x As Long?) As Boolean
        Select Case x
            Case 1:
                Return True
            Case Else
                Return False
        End Select
    End Function

    Sub Main()
        System.Console.WriteLine(F(1))
    End Sub
End Module
    ]]></file>
</compilation>, expectedOutput:="True").VerifyIL("C.F(Long?)", <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (Boolean V_0, //F
                Long? V_1,
                Boolean? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloca.s   V_1
  IL_0004:  call       "Function Long?.get_HasValue() As Boolean"
  IL_0009:  brtrue.s   IL_0016
  IL_000b:  ldloca.s   V_2
  IL_000d:  initobj    "Boolean?"
  IL_0013:  ldloc.2
  IL_0014:  br.s       IL_0026
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       "Function Long?.GetValueOrDefault() As Long"
  IL_001d:  ldc.i4.1
  IL_001e:  conv.i8
  IL_001f:  ceq
  IL_0021:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0026:  stloc.2
  IL_0027:  ldloca.s   V_2
  IL_0029:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002e:  brfalse.s  IL_0034
  IL_0030:  ldc.i4.1
  IL_0031:  stloc.0
  IL_0032:  br.s       IL_0036
  IL_0034:  ldc.i4.0
  IL_0035:  stloc.0
  IL_0036:  ldloc.0
  IL_0037:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

#Region "Select case string tests"

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        Public Sub SelectCase_Hash_SwitchTable_String_OptionCompareBinary()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      420 (0x1a4)
  .maxstack  3
  .locals init (String V_0,
  UInteger V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function ComputeStringHash(String) As UInteger"
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4     0x330ca589
  IL_000f:  bgt.un.s   IL_005a
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4     0x300ca0d0
  IL_0017:  bgt.un.s   IL_0034
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4     0x1beb2a44
  IL_001f:  beq        IL_015d
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4     0x300ca0d0
  IL_002a:  beq        IL_010d
  IL_002f:  br         IL_0199
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4     0x310ca263
  IL_003a:  beq        IL_00fa
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4     0x320ca3f6
  IL_0045:  beq        IL_012d
  IL_004a:  ldloc.1
  IL_004b:  ldc.i4     0x330ca589
  IL_0050:  beq        IL_011d
  IL_0055:  br         IL_0199
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4     0x360caa42
  IL_0060:  bgt.un.s   IL_007f
  IL_0062:  ldloc.1
  IL_0063:  ldc.i4     0x340ca71c
  IL_0068:  beq.s      IL_00b8
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0x350ca8af
  IL_0070:  beq.s      IL_00a2
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4     0x360caa42
  IL_0078:  beq.s      IL_00e4
  IL_007a:  br         IL_0199
  IL_007f:  ldloc.1
  IL_0080:  ldc.i4     0x370cabd5
  IL_0085:  beq.s      IL_00ce
  IL_0087:  ldloc.1
  IL_0088:  ldc.i4     0x3c0cb3b4
  IL_008d:  beq        IL_014d
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4     0x3d0cb547
  IL_0098:  beq        IL_013d
  IL_009d:  br         IL_0199
  IL_00a2:  ldloc.0
  IL_00a3:  ldstr      "0"
  IL_00a8:  ldc.i4.0
  IL_00a9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ae:  brfalse    IL_016d
  IL_00b3:  br         IL_0199
  IL_00b8:  ldloc.0
  IL_00b9:  ldstr      "1"
  IL_00be:  ldc.i4.0
  IL_00bf:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00c4:  brfalse    IL_0178
  IL_00c9:  br         IL_0199
  IL_00ce:  ldloc.0
  IL_00cf:  ldstr      "2"
  IL_00d4:  ldc.i4.0
  IL_00d5:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00da:  brfalse    IL_0178
  IL_00df:  br         IL_0199
  IL_00e4:  ldloc.0
  IL_00e5:  ldstr      "3"
  IL_00ea:  ldc.i4.0
  IL_00eb:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00f0:  brfalse    IL_0178
  IL_00f5:  br         IL_0199
  IL_00fa:  ldloc.0
  IL_00fb:  ldstr      "4"
  IL_0100:  ldc.i4.0
  IL_0101:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0106:  brfalse.s  IL_0178
  IL_0108:  br         IL_0199
  IL_010d:  ldloc.0
  IL_010e:  ldstr      "5"
  IL_0113:  ldc.i4.0
  IL_0114:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0119:  brfalse.s  IL_0178
  IL_011b:  br.s       IL_0199
  IL_011d:  ldloc.0
  IL_011e:  ldstr      "6"
  IL_0123:  ldc.i4.0
  IL_0124:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0129:  brfalse.s  IL_0183
  IL_012b:  br.s       IL_0199
  IL_012d:  ldloc.0
  IL_012e:  ldstr      "7"
  IL_0133:  ldc.i4.0
  IL_0134:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0139:  brfalse.s  IL_0183
  IL_013b:  br.s       IL_0199
  IL_013d:  ldloc.0
  IL_013e:  ldstr      "8"
  IL_0143:  ldc.i4.0
  IL_0144:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0149:  brfalse.s  IL_0183
  IL_014b:  br.s       IL_0199
  IL_014d:  ldloc.0
  IL_014e:  ldstr      "9"
  IL_0153:  ldc.i4.0
  IL_0154:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0159:  brfalse.s  IL_018e
  IL_015b:  br.s       IL_0199
  IL_015d:  ldloc.0
  IL_015e:  ldstr      "10"
  IL_0163:  ldc.i4.0
  IL_0164:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0169:  brfalse.s  IL_018e
  IL_016b:  br.s       IL_0199
  IL_016d:  ldstr      "Equal to 0"
  IL_0172:  call       "Sub System.Console.WriteLine(String)"
  IL_0177:  ret
  IL_0178:  ldstr      "Between 1 and 5, inclusive"
  IL_017d:  call       "Sub System.Console.WriteLine(String)"
  IL_0182:  ret
  IL_0183:  ldstr      "Between 6 and 8, inclusive"
  IL_0188:  call       "Sub System.Console.WriteLine(String)"
  IL_018d:  ret
  IL_018e:  ldstr      "Equal to 9 or 10"
  IL_0193:  call       "Sub System.Console.WriteLine(String)"
  IL_0198:  ret
  IL_0199:  ldstr      "Greater than 10"
  IL_019e:  call       "Sub System.Console.WriteLine(String)"
  IL_01a3:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)

            ' verify that hash method is Friend
            Dim reference = compVerifier.Compilation.EmitToImageReference()
            Dim comp = VisualBasicCompilation.Create("Name", references:={reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim pid = DirectCast(comp.GlobalNamespace.GetMembers().Single(Function(s) s.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)), NamedTypeSymbol)

            Dim member = pid.GetMembers(PrivateImplementationDetails.SynthesizedStringHashFunctionName).Single()
            Assert.Equal(Accessibility.Friend, member.DeclaredAccessibility)
        End Sub

        <Fact()>
        Public Sub SelectCase_NonHash_SwitchTable_String_OptionCompareBinary()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 6
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case Else
                Console.WriteLine("Greater than 5")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Greater than 5]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      121 (0x79)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      "0"
  IL_0008:  ldc.i4.0
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brfalse.s  IL_0058
  IL_0010:  ldloc.0
  IL_0011:  ldstr      "1"
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_001c:  brfalse.s  IL_0063
  IL_001e:  ldloc.0
  IL_001f:  ldstr      "2"
  IL_0024:  ldc.i4.0
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_002a:  brfalse.s  IL_0063
  IL_002c:  ldloc.0
  IL_002d:  ldstr      "3"
  IL_0032:  ldc.i4.0
  IL_0033:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0038:  brfalse.s  IL_0063
  IL_003a:  ldloc.0
  IL_003b:  ldstr      "4"
  IL_0040:  ldc.i4.0
  IL_0041:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0046:  brfalse.s  IL_0063
  IL_0048:  ldloc.0
  IL_0049:  ldstr      "5"
  IL_004e:  ldc.i4.0
  IL_004f:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0054:  brfalse.s  IL_0063
  IL_0056:  br.s       IL_006e
  IL_0058:  ldstr      "Equal to 0"
  IL_005d:  call       "Sub System.Console.WriteLine(String)"
  IL_0062:  ret
  IL_0063:  ldstr      "Between 1 and 5, inclusive"
  IL_0068:  call       "Sub System.Console.WriteLine(String)"
  IL_006d:  ret
  IL_006e:  ldstr      "Greater than 5"
  IL_0073:  call       "Sub System.Console.WriteLine(String)"
  IL_0078:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact()>
        Public Sub SelectCase_IfList_String_OptionCompareText()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(True),
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      211 (0xd3)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      "0"
  IL_0008:  ldc.i4.1
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldstr      "Equal to 0"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldloc.0
  IL_001c:  ldstr      "1"
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0027:  brfalse.s  IL_0061
  IL_0029:  ldloc.0
  IL_002a:  ldstr      "2"
  IL_002f:  ldc.i4.1
  IL_0030:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0035:  brfalse.s  IL_0061
  IL_0037:  ldloc.0
  IL_0038:  ldstr      "3"
  IL_003d:  ldc.i4.1
  IL_003e:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0043:  brfalse.s  IL_0061
  IL_0045:  ldloc.0
  IL_0046:  ldstr      "4"
  IL_004b:  ldc.i4.1
  IL_004c:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0051:  brfalse.s  IL_0061
  IL_0053:  ldloc.0
  IL_0054:  ldstr      "5"
  IL_0059:  ldc.i4.1
  IL_005a:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_005f:  brtrue.s   IL_006c
  IL_0061:  ldstr      "Between 1 and 5, inclusive"
  IL_0066:  call       "Sub System.Console.WriteLine(String)"
  IL_006b:  ret
  IL_006c:  ldloc.0
  IL_006d:  ldstr      "6"
  IL_0072:  ldc.i4.1
  IL_0073:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0078:  brfalse.s  IL_0096
  IL_007a:  ldloc.0
  IL_007b:  ldstr      "7"
  IL_0080:  ldc.i4.1
  IL_0081:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0086:  brfalse.s  IL_0096
  IL_0088:  ldloc.0
  IL_0089:  ldstr      "8"
  IL_008e:  ldc.i4.1
  IL_008f:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0094:  brtrue.s   IL_00a1
  IL_0096:  ldstr      "Between 6 and 8, inclusive"
  IL_009b:  call       "Sub System.Console.WriteLine(String)"
  IL_00a0:  ret
  IL_00a1:  ldloc.0
  IL_00a2:  ldstr      "9"
  IL_00a7:  ldc.i4.1
  IL_00a8:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ad:  brfalse.s  IL_00bd
  IL_00af:  ldloc.0
  IL_00b0:  ldstr      "10"
  IL_00b5:  ldc.i4.1
  IL_00b6:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00bb:  brtrue.s   IL_00c8
  IL_00bd:  ldstr      "Equal to 9 or 10"
  IL_00c2:  call       "Sub System.Console.WriteLine(String)"
  IL_00c7:  ret
  IL_00c8:  ldstr      "Greater than 10"
  IL_00cd:  call       "Sub System.Console.WriteLine(String)"
  IL_00d2:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact()>
        Public Sub SelectCase_Hash_SwitchTable_String_MDConstant()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1

    Sub Main()
        Test("a")
        Test("A")
    End Sub

    Sub Test(str as String)

        Dim x As Integer() = {1, 2, 3, 4, 5, 6, 7, 8, 9}

        Select Case str
            Case "a"
                Console.WriteLine("Equal to a")
            Case "A"
                Console.WriteLine("Equal to A")
            Case "1", "2", "3", "4", "5", "6", "4", "5", "6"
                Console.WriteLine("Error")
            Case Else
                Console.WriteLine("Error")
        End Select
    End Sub
End Module

    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[Equal to a
Equal to A]]>)
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        Public Sub SelectCase_Hash_SwitchTable_String_OptionCompareBinary_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Test("a")
        Test("A")
    End Sub

    Sub Test(str as String)
        Select Case str
            Case "a"
                Console.WriteLine("Equal to a")
            Case "A"
                Console.WriteLine("Equal to A")
            Case "1", "2", "3", "4", "5", "6", "4", "5", "6"
                Console.WriteLine("Error")
            Case Else
                Console.WriteLine("Error")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[Equal to a
Equal to A]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      302 (0x12e)
  .maxstack  3
  .locals init (String V_0,
  UInteger V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function ComputeStringHash(String) As UInteger"
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4     0x340ca71c
  IL_000f:  bgt.un.s   IL_004c
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4     0x310ca263
  IL_0017:  bgt.un.s   IL_0034
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4     0x300ca0d0
  IL_001f:  beq        IL_00e2
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4     0x310ca263
  IL_002a:  beq        IL_00d2
  IL_002f:  br         IL_0123
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4     0x330ca589
  IL_003a:  beq        IL_00f2
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4     0x340ca71c
  IL_0045:  beq.s      IL_00a2
  IL_0047:  br         IL_0123
  IL_004c:  ldloc.1
  IL_004d:  ldc.i4     0x370cabd5
  IL_0052:  bgt.un.s   IL_0069
  IL_0054:  ldloc.1
  IL_0055:  ldc.i4     0x360caa42
  IL_005a:  beq.s      IL_00c2
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4     0x370cabd5
  IL_0062:  beq.s      IL_00b2
  IL_0064:  br         IL_0123
  IL_0069:  ldloc.1
  IL_006a:  ldc.i4     0xc40bf6cc
  IL_006f:  beq.s      IL_008f
  IL_0071:  ldloc.1
  IL_0072:  ldc.i4     0xe40c292c
  IL_0077:  bne.un     IL_0123
  IL_007c:  ldloc.0
  IL_007d:  ldstr      "a"
  IL_0082:  ldc.i4.0
  IL_0083:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0088:  brfalse.s  IL_0102
  IL_008a:  br         IL_0123
  IL_008f:  ldloc.0
  IL_0090:  ldstr      "A"
  IL_0095:  ldc.i4.0
  IL_0096:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_009b:  brfalse.s  IL_010d
  IL_009d:  br         IL_0123
  IL_00a2:  ldloc.0
  IL_00a3:  ldstr      "1"
  IL_00a8:  ldc.i4.0
  IL_00a9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ae:  brfalse.s  IL_0118
  IL_00b0:  br.s       IL_0123
  IL_00b2:  ldloc.0
  IL_00b3:  ldstr      "2"
  IL_00b8:  ldc.i4.0
  IL_00b9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00be:  brfalse.s  IL_0118
  IL_00c0:  br.s       IL_0123
  IL_00c2:  ldloc.0
  IL_00c3:  ldstr      "3"
  IL_00c8:  ldc.i4.0
  IL_00c9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ce:  brfalse.s  IL_0118
  IL_00d0:  br.s       IL_0123
  IL_00d2:  ldloc.0
  IL_00d3:  ldstr      "4"
  IL_00d8:  ldc.i4.0
  IL_00d9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00de:  brfalse.s  IL_0118
  IL_00e0:  br.s       IL_0123
  IL_00e2:  ldloc.0
  IL_00e3:  ldstr      "5"
  IL_00e8:  ldc.i4.0
  IL_00e9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ee:  brfalse.s  IL_0118
  IL_00f0:  br.s       IL_0123
  IL_00f2:  ldloc.0
  IL_00f3:  ldstr      "6"
  IL_00f8:  ldc.i4.0
  IL_00f9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00fe:  brfalse.s  IL_0118
  IL_0100:  br.s       IL_0123
  IL_0102:  ldstr      "Equal to a"
  IL_0107:  call       "Sub System.Console.WriteLine(String)"
  IL_010c:  ret
  IL_010d:  ldstr      "Equal to A"
  IL_0112:  call       "Sub System.Console.WriteLine(String)"
  IL_0117:  ret
  IL_0118:  ldstr      "Error"
  IL_011d:  call       "Sub System.Console.WriteLine(String)"
  IL_0122:  ret
  IL_0123:  ldstr      "Error"
  IL_0128:  call       "Sub System.Console.WriteLine(String)"
  IL_012d:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)
        End Sub

        <Fact()>
        Public Sub SelectCase_NonHash_SwitchTable_String_OptionCompareBinary_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Test("a")
        Test("A")
    End Sub

    Sub Test(str as String)
        Select Case str
            Case "a"
                Console.WriteLine("Equal to a")
            Case "A"
                Console.WriteLine("Equal to A")
            Case "1", "1", "1", "1", "1", "1", "1", "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case "1"
                Console.WriteLine("Error")
            Case Else
                Console.WriteLine("Error")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[Equal to a
Equal to A]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      "a"
  IL_0008:  ldc.i4.0
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brfalse.s  IL_002e
  IL_0010:  ldloc.0
  IL_0011:  ldstr      "A"
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_001c:  brfalse.s  IL_0039
  IL_001e:  ldloc.0
  IL_001f:  ldstr      "1"
  IL_0024:  ldc.i4.0
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_002a:  brfalse.s  IL_0044
  IL_002c:  br.s       IL_004f
  IL_002e:  ldstr      "Equal to a"
  IL_0033:  call       "Sub System.Console.WriteLine(String)"
  IL_0038:  ret
  IL_0039:  ldstr      "Equal to A"
  IL_003e:  call       "Sub System.Console.WriteLine(String)"
  IL_0043:  ret
  IL_0044:  ldstr      "Error"
  IL_0049:  call       "Sub System.Console.WriteLine(String)"
  IL_004e:  ret
  IL_004f:  ldstr      "Error"
  IL_0054:  call       "Sub System.Console.WriteLine(String)"
  IL_0059:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact()>
        Public Sub SelectCase_IfList_String_OptionCompareText_02()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Test("a")
        Test("A")
    End Sub

    Sub Test(str as String)
        Select Case str
            Case "a"
                Console.WriteLine("Equal to a")
            Case "A"
                Console.WriteLine("Error")
            Case "1", "2", "3", "4", "5", "6", "4", "5", "6"
                Console.WriteLine("Error")
            Case Else
                Console.WriteLine("Error")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(True),
    expectedOutput:=<![CDATA[Equal to a
Equal to a]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      200 (0xc8)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      "a"
  IL_0008:  ldc.i4.1
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldstr      "Equal to a"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldloc.0
  IL_001c:  ldstr      "A"
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0027:  brtrue.s   IL_0034
  IL_0029:  ldstr      "Error"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  ret
  IL_0034:  ldloc.0
  IL_0035:  ldstr      "1"
  IL_003a:  ldc.i4.1
  IL_003b:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0040:  brfalse.s  IL_00b2
  IL_0042:  ldloc.0
  IL_0043:  ldstr      "2"
  IL_0048:  ldc.i4.1
  IL_0049:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_004e:  brfalse.s  IL_00b2
  IL_0050:  ldloc.0
  IL_0051:  ldstr      "3"
  IL_0056:  ldc.i4.1
  IL_0057:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_005c:  brfalse.s  IL_00b2
  IL_005e:  ldloc.0
  IL_005f:  ldstr      "4"
  IL_0064:  ldc.i4.1
  IL_0065:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_006a:  brfalse.s  IL_00b2
  IL_006c:  ldloc.0
  IL_006d:  ldstr      "5"
  IL_0072:  ldc.i4.1
  IL_0073:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0078:  brfalse.s  IL_00b2
  IL_007a:  ldloc.0
  IL_007b:  ldstr      "6"
  IL_0080:  ldc.i4.1
  IL_0081:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0086:  brfalse.s  IL_00b2
  IL_0088:  ldloc.0
  IL_0089:  ldstr      "4"
  IL_008e:  ldc.i4.1
  IL_008f:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0094:  brfalse.s  IL_00b2
  IL_0096:  ldloc.0
  IL_0097:  ldstr      "5"
  IL_009c:  ldc.i4.1
  IL_009d:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00a2:  brfalse.s  IL_00b2
  IL_00a4:  ldloc.0
  IL_00a5:  ldstr      "6"
  IL_00aa:  ldc.i4.1
  IL_00ab:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00b0:  brtrue.s   IL_00bd
  IL_00b2:  ldstr      "Error"
  IL_00b7:  call       "Sub System.Console.WriteLine(String)"
  IL_00bc:  ret
  IL_00bd:  ldstr      "Error"
  IL_00c2:  call       "Sub System.Console.WriteLine(String)"
  IL_00c7:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        Public Sub SelectCase_SwitchTable_String_RelationalEqualityClause()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", = "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case Is = "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      420 (0x1a4)
  .maxstack  3
  .locals init (String V_0,
  UInteger V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function ComputeStringHash(String) As UInteger"
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4     0x330ca589
  IL_000f:  bgt.un.s   IL_005a
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4     0x300ca0d0
  IL_0017:  bgt.un.s   IL_0034
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4     0x1beb2a44
  IL_001f:  beq        IL_015d
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4     0x300ca0d0
  IL_002a:  beq        IL_010d
  IL_002f:  br         IL_0199
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4     0x310ca263
  IL_003a:  beq        IL_00fa
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4     0x320ca3f6
  IL_0045:  beq        IL_012d
  IL_004a:  ldloc.1
  IL_004b:  ldc.i4     0x330ca589
  IL_0050:  beq        IL_011d
  IL_0055:  br         IL_0199
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4     0x360caa42
  IL_0060:  bgt.un.s   IL_007f
  IL_0062:  ldloc.1
  IL_0063:  ldc.i4     0x340ca71c
  IL_0068:  beq.s      IL_00b8
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0x350ca8af
  IL_0070:  beq.s      IL_00a2
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4     0x360caa42
  IL_0078:  beq.s      IL_00e4
  IL_007a:  br         IL_0199
  IL_007f:  ldloc.1
  IL_0080:  ldc.i4     0x370cabd5
  IL_0085:  beq.s      IL_00ce
  IL_0087:  ldloc.1
  IL_0088:  ldc.i4     0x3c0cb3b4
  IL_008d:  beq        IL_014d
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4     0x3d0cb547
  IL_0098:  beq        IL_013d
  IL_009d:  br         IL_0199
  IL_00a2:  ldloc.0
  IL_00a3:  ldstr      "0"
  IL_00a8:  ldc.i4.0
  IL_00a9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ae:  brfalse    IL_016d
  IL_00b3:  br         IL_0199
  IL_00b8:  ldloc.0
  IL_00b9:  ldstr      "1"
  IL_00be:  ldc.i4.0
  IL_00bf:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00c4:  brfalse    IL_0178
  IL_00c9:  br         IL_0199
  IL_00ce:  ldloc.0
  IL_00cf:  ldstr      "2"
  IL_00d4:  ldc.i4.0
  IL_00d5:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00da:  brfalse    IL_0178
  IL_00df:  br         IL_0199
  IL_00e4:  ldloc.0
  IL_00e5:  ldstr      "3"
  IL_00ea:  ldc.i4.0
  IL_00eb:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00f0:  brfalse    IL_0178
  IL_00f5:  br         IL_0199
  IL_00fa:  ldloc.0
  IL_00fb:  ldstr      "4"
  IL_0100:  ldc.i4.0
  IL_0101:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0106:  brfalse.s  IL_0178
  IL_0108:  br         IL_0199
  IL_010d:  ldloc.0
  IL_010e:  ldstr      "5"
  IL_0113:  ldc.i4.0
  IL_0114:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0119:  brfalse.s  IL_0178
  IL_011b:  br.s       IL_0199
  IL_011d:  ldloc.0
  IL_011e:  ldstr      "6"
  IL_0123:  ldc.i4.0
  IL_0124:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0129:  brfalse.s  IL_0183
  IL_012b:  br.s       IL_0199
  IL_012d:  ldloc.0
  IL_012e:  ldstr      "7"
  IL_0133:  ldc.i4.0
  IL_0134:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0139:  brfalse.s  IL_0183
  IL_013b:  br.s       IL_0199
  IL_013d:  ldloc.0
  IL_013e:  ldstr      "8"
  IL_0143:  ldc.i4.0
  IL_0144:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0149:  brfalse.s  IL_0183
  IL_014b:  br.s       IL_0199
  IL_014d:  ldloc.0
  IL_014e:  ldstr      "9"
  IL_0153:  ldc.i4.0
  IL_0154:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0159:  brfalse.s  IL_018e
  IL_015b:  br.s       IL_0199
  IL_015d:  ldloc.0
  IL_015e:  ldstr      "10"
  IL_0163:  ldc.i4.0
  IL_0164:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0169:  brfalse.s  IL_018e
  IL_016b:  br.s       IL_0199
  IL_016d:  ldstr      "Equal to 0"
  IL_0172:  call       "Sub System.Console.WriteLine(String)"
  IL_0177:  ret
  IL_0178:  ldstr      "Between 1 and 5, inclusive"
  IL_017d:  call       "Sub System.Console.WriteLine(String)"
  IL_0182:  ret
  IL_0183:  ldstr      "Between 6 and 8, inclusive"
  IL_0188:  call       "Sub System.Console.WriteLine(String)"
  IL_018d:  ret
  IL_018e:  ldstr      "Equal to 9 or 10"
  IL_0193:  call       "Sub System.Console.WriteLine(String)"
  IL_0198:  ret
  IL_0199:  ldstr      "Greater than 10"
  IL_019e:  call       "Sub System.Console.WriteLine(String)"
  IL_01a3:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23818, "https://github.com/dotnet/roslyn/issues/23818")>
        <Fact()>
        Public Sub SelectCase_IfList_String_RelationalRangeClauses()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6" To "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9" To "8"
                Console.WriteLine("Fail")
            Case >= "9", <= "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(True),
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      242 (0xf2)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldstr      "0"
  IL_0008:  ldc.i4.1
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldstr      "Equal to 0"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
  IL_001b:  ldloc.0
  IL_001c:  ldstr      "1"
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0027:  brfalse.s  IL_0061
  IL_0029:  ldloc.0
  IL_002a:  ldstr      "2"
  IL_002f:  ldc.i4.1
  IL_0030:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0035:  brfalse.s  IL_0061
  IL_0037:  ldloc.0
  IL_0038:  ldstr      "3"
  IL_003d:  ldc.i4.1
  IL_003e:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0043:  brfalse.s  IL_0061
  IL_0045:  ldloc.0
  IL_0046:  ldstr      "4"
  IL_004b:  ldc.i4.1
  IL_004c:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0051:  brfalse.s  IL_0061
  IL_0053:  ldloc.0
  IL_0054:  ldstr      "5"
  IL_0059:  ldc.i4.1
  IL_005a:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_005f:  brtrue.s   IL_006c
  IL_0061:  ldstr      "Between 1 and 5, inclusive"
  IL_0066:  call       "Sub System.Console.WriteLine(String)"
  IL_006b:  ret
  IL_006c:  ldloc.0
  IL_006d:  ldstr      "6"
  IL_0072:  ldc.i4.1
  IL_0073:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0078:  ldc.i4.0
  IL_0079:  blt.s      IL_0095
  IL_007b:  ldloc.0
  IL_007c:  ldstr      "8"
  IL_0081:  ldc.i4.1
  IL_0082:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0087:  ldc.i4.0
  IL_0088:  bgt.s      IL_0095
  IL_008a:  ldstr      "Between 6 and 8, inclusive"
  IL_008f:  call       "Sub System.Console.WriteLine(String)"
  IL_0094:  ret
  IL_0095:  ldloc.0
  IL_0096:  ldstr      "9"
  IL_009b:  ldc.i4.1
  IL_009c:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00a1:  ldc.i4.0
  IL_00a2:  blt.s      IL_00be
  IL_00a4:  ldloc.0
  IL_00a5:  ldstr      "8"
  IL_00aa:  ldc.i4.1
  IL_00ab:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00b0:  ldc.i4.0
  IL_00b1:  bgt.s      IL_00be
  IL_00b3:  ldstr      "Fail"
  IL_00b8:  call       "Sub System.Console.WriteLine(String)"
  IL_00bd:  ret
  IL_00be:  ldloc.0
  IL_00bf:  ldstr      "9"
  IL_00c4:  ldc.i4.1
  IL_00c5:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ca:  ldc.i4.0
  IL_00cb:  bge.s      IL_00dc
  IL_00cd:  ldloc.0
  IL_00ce:  ldstr      "10"
  IL_00d3:  ldc.i4.1
  IL_00d4:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00d9:  ldc.i4.0
  IL_00da:  bgt.s      IL_00e7
  IL_00dc:  ldstr      "Equal to 9 or 10"
  IL_00e1:  call       "Sub System.Console.WriteLine(String)"
  IL_00e6:  ret
  IL_00e7:  ldstr      "Greater than 10"
  IL_00ec:  call       "Sub System.Console.WriteLine(String)"
  IL_00f1:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)

            Dim compilation = compVerifier.Compilation

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().First()

            Assert.Equal("""6"" To ""8""", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: '"6" To "8"')
  Min: 
    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "6") (Syntax: '"6"')
  Max: 
    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "8") (Syntax: '"8"')
]]>.Value)
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        Public Sub SelectCase_String_Multiple_Hash_SwitchTable()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x.ToString())
            Console.Write(x.ToString() + ":")
            Test2(x.ToString())
        Next
    End Sub

    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub

    Sub Test2(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    options:=TestOptions.ReleaseExe.WithOptionCompareText(False),
    expectedOutput:=<![CDATA[0:Equal to 0
0:Equal to 0
1:Between 1 and 5, inclusive
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
9:Equal to 9 or 10
10:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10
11:Greater than 10]]>).VerifyIL("M1.Test", <![CDATA[
{
  // Code size      420 (0x1a4)
  .maxstack  3
  .locals init (String V_0,
  UInteger V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Function ComputeStringHash(String) As UInteger"
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4     0x330ca589
  IL_000f:  bgt.un.s   IL_005a
  IL_0011:  ldloc.1
  IL_0012:  ldc.i4     0x300ca0d0
  IL_0017:  bgt.un.s   IL_0034
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4     0x1beb2a44
  IL_001f:  beq        IL_015d
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4     0x300ca0d0
  IL_002a:  beq        IL_010d
  IL_002f:  br         IL_0199
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4     0x310ca263
  IL_003a:  beq        IL_00fa
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4     0x320ca3f6
  IL_0045:  beq        IL_012d
  IL_004a:  ldloc.1
  IL_004b:  ldc.i4     0x330ca589
  IL_0050:  beq        IL_011d
  IL_0055:  br         IL_0199
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4     0x360caa42
  IL_0060:  bgt.un.s   IL_007f
  IL_0062:  ldloc.1
  IL_0063:  ldc.i4     0x340ca71c
  IL_0068:  beq.s      IL_00b8
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0x350ca8af
  IL_0070:  beq.s      IL_00a2
  IL_0072:  ldloc.1
  IL_0073:  ldc.i4     0x360caa42
  IL_0078:  beq.s      IL_00e4
  IL_007a:  br         IL_0199
  IL_007f:  ldloc.1
  IL_0080:  ldc.i4     0x370cabd5
  IL_0085:  beq.s      IL_00ce
  IL_0087:  ldloc.1
  IL_0088:  ldc.i4     0x3c0cb3b4
  IL_008d:  beq        IL_014d
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4     0x3d0cb547
  IL_0098:  beq        IL_013d
  IL_009d:  br         IL_0199
  IL_00a2:  ldloc.0
  IL_00a3:  ldstr      "0"
  IL_00a8:  ldc.i4.0
  IL_00a9:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00ae:  brfalse    IL_016d
  IL_00b3:  br         IL_0199
  IL_00b8:  ldloc.0
  IL_00b9:  ldstr      "1"
  IL_00be:  ldc.i4.0
  IL_00bf:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00c4:  brfalse    IL_0178
  IL_00c9:  br         IL_0199
  IL_00ce:  ldloc.0
  IL_00cf:  ldstr      "2"
  IL_00d4:  ldc.i4.0
  IL_00d5:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00da:  brfalse    IL_0178
  IL_00df:  br         IL_0199
  IL_00e4:  ldloc.0
  IL_00e5:  ldstr      "3"
  IL_00ea:  ldc.i4.0
  IL_00eb:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00f0:  brfalse    IL_0178
  IL_00f5:  br         IL_0199
  IL_00fa:  ldloc.0
  IL_00fb:  ldstr      "4"
  IL_0100:  ldc.i4.0
  IL_0101:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0106:  brfalse.s  IL_0178
  IL_0108:  br         IL_0199
  IL_010d:  ldloc.0
  IL_010e:  ldstr      "5"
  IL_0113:  ldc.i4.0
  IL_0114:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0119:  brfalse.s  IL_0178
  IL_011b:  br.s       IL_0199
  IL_011d:  ldloc.0
  IL_011e:  ldstr      "6"
  IL_0123:  ldc.i4.0
  IL_0124:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0129:  brfalse.s  IL_0183
  IL_012b:  br.s       IL_0199
  IL_012d:  ldloc.0
  IL_012e:  ldstr      "7"
  IL_0133:  ldc.i4.0
  IL_0134:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0139:  brfalse.s  IL_0183
  IL_013b:  br.s       IL_0199
  IL_013d:  ldloc.0
  IL_013e:  ldstr      "8"
  IL_0143:  ldc.i4.0
  IL_0144:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0149:  brfalse.s  IL_0183
  IL_014b:  br.s       IL_0199
  IL_014d:  ldloc.0
  IL_014e:  ldstr      "9"
  IL_0153:  ldc.i4.0
  IL_0154:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0159:  brfalse.s  IL_018e
  IL_015b:  br.s       IL_0199
  IL_015d:  ldloc.0
  IL_015e:  ldstr      "10"
  IL_0163:  ldc.i4.0
  IL_0164:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0169:  brfalse.s  IL_018e
  IL_016b:  br.s       IL_0199
  IL_016d:  ldstr      "Equal to 0"
  IL_0172:  call       "Sub System.Console.WriteLine(String)"
  IL_0177:  ret
  IL_0178:  ldstr      "Between 1 and 5, inclusive"
  IL_017d:  call       "Sub System.Console.WriteLine(String)"
  IL_0182:  ret
  IL_0183:  ldstr      "Between 6 and 8, inclusive"
  IL_0188:  call       "Sub System.Console.WriteLine(String)"
  IL_018d:  ret
  IL_018e:  ldstr      "Equal to 9 or 10"
  IL_0193:  call       "Sub System.Console.WriteLine(String)"
  IL_0198:  ret
  IL_0199:  ldstr      "Greater than 10"
  IL_019e:  call       "Sub System.Console.WriteLine(String)"
  IL_01a3:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)
        End Sub

        <Fact()>
        Public Sub MissingReferenceToVBRuntime()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Class C
    Sub Test(number as String)
        Select Case number
            Case "0"
                Console.WriteLine("Equal to 0")
            Case "1", "2", "3", "4", "5"
                Console.WriteLine("Between 1 and 5, inclusive")
            Case "6", "7", "8"
                Console.WriteLine("Between 6 and 8, inclusive")
            Case "9", "10"
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Class
]]></file>
</compilation>, OutputKind.DynamicallyLinkedLibrary).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "number").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.CompareString"))
        End Sub

        <WorkItem(529047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529047")>
        <Fact>
        Public Sub SelectOutOfMethod()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class m1
    Select ""
    End Select
End Class
]]></file>
</compilation>).
            VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Select """""),
                Diagnostic(ERRID.ERR_EndSelectNoSelect, "End Select"))
        End Sub

        <WorkItem(529047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529047")>
        <Fact>
        Public Sub SelectOutOfMethod_1()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
    Select ""
    End Select
]]></file>
</compilation>).
            VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Select """""),
                Diagnostic(ERRID.ERR_EndSelectNoSelect, "End Select"))
        End Sub

        <WorkItem(543410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543410")>
        <Fact()>
        Public Sub SelectCase_GetType()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module M1
    Sub Main()
        Select GetType(Object)
        End Select
    End Sub
End Module
    ]]></file>
</compilation>).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldtoken    "Object"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)
        End Sub

        <WorkItem(634404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634404")>
        <WorkItem(913556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/913556")>
        <Fact()>
        Public Sub MissingCharsProperty()
            CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class M1
    Shared Sub Main()

    End Sub

    Shared Function Test(number as String) as string
        Select Case number
            Case "0"
                return "0"
            Case "1"
                return "1"
            Case "2"
                return "2"
            Case "3"
                return "3"
            Case "4"
                return "4"
            Case Else
                return "Else"
        End Select
    End Function
End Class
]]></file>
</compilation>, references:={AacorlibRef}).
            VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "number").WithArguments("System.String.get_Chars"),
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "number").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.CompareString"))
        End Sub

        <Fact>
        Public Sub SelectCase_Nothing001()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
    Dim str As String = ""
        Select Case str
            Case CStr(Nothing)
            System.Console.WriteLine("null")
            Case "1"
            System.Console.WriteLine("1")
            'Case "1"
            ' System.Console.WriteLine("2")
            'Case "3"
            ' System.Console.WriteLine("3")
            'Case "4"
            ' System.Console.WriteLine("4")
            'Case "5"
            ' System.Console.WriteLine("5")
            'Case "6"
            ' System.Console.WriteLine("6")
            'Case "7"
            ' System.Console.WriteLine("7")
            'Case "8"
            ' System.Console.WriteLine("8")
　
        End Select
    End Sub
End Module

    ]]></file>
</compilation>, expectedOutput:="null").VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (String V_0) //str
  IL_0000:  ldstr      ""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  ldc.i4.0
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_000e:  brfalse.s  IL_001f
  IL_0010:  ldloc.0
  IL_0011:  ldstr      "1"
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_001c:  brfalse.s  IL_002a
  IL_001e:  ret
  IL_001f:  ldstr      "null"
  IL_0024:  call       "Sub System.Console.WriteLine(String)"
  IL_0029:  ret
  IL_002a:  ldstr      "1"
  IL_002f:  call       "Sub System.Console.WriteLine(String)"
  IL_0034:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub SelectCase_Nothing002()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
    Dim str As String = ""
        Select Case str
            Case CStr(Nothing)
            System.Console.WriteLine("null")
            Case "1"
            System.Console.WriteLine("1")
            Case "1"
             System.Console.WriteLine("2")
            Case "3"
             System.Console.WriteLine("3")
            Case "4"
             System.Console.WriteLine("4")
            Case "5"
             System.Console.WriteLine("5")
            Case "6"
             System.Console.WriteLine("6")
            Case "7"
             System.Console.WriteLine("7")
            Case "8"
             System.Console.WriteLine("8")
　
        End Select
    End Sub
End Module

    ]]></file>
</compilation>, expectedOutput:="null").VerifyIL("Module1.Main", <![CDATA[
{
  // Code size      317 (0x13d)
  .maxstack  3
  .locals init (String V_0, //str
  UInteger V_1)
  IL_0000:  ldstr      ""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "Function ComputeStringHash(String) As UInteger"
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldc.i4     0x330ca589
  IL_0013:  bgt.un.s   IL_0045
  IL_0015:  ldloc.1
  IL_0016:  ldc.i4     0x310ca263
  IL_001b:  bgt.un.s   IL_0031
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4     0x300ca0d0
  IL_0023:  beq        IL_00a9
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4     0x310ca263
  IL_002e:  beq.s      IL_009a
  IL_0030:  ret
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4     0x320ca3f6
  IL_0037:  beq        IL_00c7
  IL_003c:  ldloc.1
  IL_003d:  ldc.i4     0x330ca589
  IL_0042:  beq.s      IL_00b8
  IL_0044:  ret
  IL_0045:  ldloc.1
  IL_0046:  ldc.i4     0x360caa42
  IL_004b:  bgt.un.s   IL_005e
  IL_004d:  ldloc.1
  IL_004e:  ldc.i4     0x340ca71c
  IL_0053:  beq.s      IL_007c
  IL_0055:  ldloc.1
  IL_0056:  ldc.i4     0x360caa42
  IL_005b:  beq.s      IL_008b
  IL_005d:  ret
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4     0x3d0cb547
  IL_0064:  beq.s      IL_00d6
  IL_0066:  ldloc.1
  IL_0067:  ldc.i4     0x811c9dc5
  IL_006c:  bne.un     IL_013c
  IL_0071:  ldloc.0
  IL_0072:  ldnull
  IL_0073:  ldc.i4.0
  IL_0074:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0079:  brfalse.s  IL_00e5
  IL_007b:  ret
  IL_007c:  ldloc.0
  IL_007d:  ldstr      "1"
  IL_0082:  ldc.i4.0
  IL_0083:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0088:  brfalse.s  IL_00f0
  IL_008a:  ret
  IL_008b:  ldloc.0
  IL_008c:  ldstr      "3"
  IL_0091:  ldc.i4.0
  IL_0092:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0097:  brfalse.s  IL_00fb
  IL_0099:  ret
  IL_009a:  ldloc.0
  IL_009b:  ldstr      "4"
  IL_00a0:  ldc.i4.0
  IL_00a1:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00a6:  brfalse.s  IL_0106
  IL_00a8:  ret
  IL_00a9:  ldloc.0
  IL_00aa:  ldstr      "5"
  IL_00af:  ldc.i4.0
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00b5:  brfalse.s  IL_0111
  IL_00b7:  ret
  IL_00b8:  ldloc.0
  IL_00b9:  ldstr      "6"
  IL_00be:  ldc.i4.0
  IL_00bf:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00c4:  brfalse.s  IL_011c
  IL_00c6:  ret
  IL_00c7:  ldloc.0
  IL_00c8:  ldstr      "7"
  IL_00cd:  ldc.i4.0
  IL_00ce:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00d3:  brfalse.s  IL_0127
  IL_00d5:  ret
  IL_00d6:  ldloc.0
  IL_00d7:  ldstr      "8"
  IL_00dc:  ldc.i4.0
  IL_00dd:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_00e2:  brfalse.s  IL_0132
  IL_00e4:  ret
  IL_00e5:  ldstr      "null"
  IL_00ea:  call       "Sub System.Console.WriteLine(String)"
  IL_00ef:  ret
  IL_00f0:  ldstr      "1"
  IL_00f5:  call       "Sub System.Console.WriteLine(String)"
  IL_00fa:  ret
  IL_00fb:  ldstr      "3"
  IL_0100:  call       "Sub System.Console.WriteLine(String)"
  IL_0105:  ret
  IL_0106:  ldstr      "4"
  IL_010b:  call       "Sub System.Console.WriteLine(String)"
  IL_0110:  ret
  IL_0111:  ldstr      "5"
  IL_0116:  call       "Sub System.Console.WriteLine(String)"
  IL_011b:  ret
  IL_011c:  ldstr      "6"
  IL_0121:  call       "Sub System.Console.WriteLine(String)"
  IL_0126:  ret
  IL_0127:  ldstr      "7"
  IL_012c:  call       "Sub System.Console.WriteLine(String)"
  IL_0131:  ret
  IL_0132:  ldstr      "8"
  IL_0137:  call       "Sub System.Console.WriteLine(String)"
  IL_013c:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)
        End Sub

        <Fact>
        Public Sub SelectCase_Nothing003()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
    Dim str As String = ""
        Select Case str
            Case CStr("")
            System.Console.WriteLine("empty")
            Case CStr(Nothing)
            System.Console.WriteLine("null")
            Case "1"
            System.Console.WriteLine("1")
            Case "1"
             System.Console.WriteLine("2")
            Case "3"
             System.Console.WriteLine("3")
            Case "4"
             System.Console.WriteLine("4")
            Case "5"
             System.Console.WriteLine("5")
            Case "6"
             System.Console.WriteLine("6")
            Case "7"
             System.Console.WriteLine("7")
            Case "8"
             System.Console.WriteLine("8")
　
        End Select
    End Sub
End Module

    ]]></file>
</compilation>, expectedOutput:="empty")

            VerifySynthesizedStringHashMethod(compVerifier, expected:=True)
        End Sub

        <Fact>
        Public Sub Regression947580()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module Module1

    Sub Main()
        boo(42)
    End Sub

    Function boo(i As Integer) As String
        Select Case i
            Case 42
                Dim x = "goo"
                If x <> "bar" Then
                    Exit Select
                End If

                Return x
        End Select

        Return Nothing
    End Function

End Module
    ]]></file>
</compilation>, expectedOutput:="").VerifyIL("Module1.boo", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (String V_0, //boo
  Integer V_1,
  String V_2) //x
  IL_0000:  ldarg.0
  IL_0001:  stloc.1
  IL_0002:  ldloc.1
  IL_0003:  ldc.i4.s   42
  IL_0005:  bne.un.s   IL_001f
  IL_0007:  ldstr      "goo"
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  ldstr      "bar"
  IL_0013:  ldc.i4.0
  IL_0014:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0019:  brtrue.s   IL_001f
  IL_001b:  ldloc.2
  IL_001c:  stloc.0
  IL_001d:  br.s       IL_0021
  IL_001f:  ldnull
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

        <Fact>
        Public Sub Regression947580a()
            Dim compVerifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System        
Module Module1

    Sub Main()
        boo(42)
    End Sub

    Function boo(i As Integer) As String
        Select Case i
            Case 42
                Dim x = "goo"
                If x <> "bar" Then
                    Exit Select
                End If

                Exit Select
        End Select

        Return Nothing
    End Function

End Module
    ]]></file>
</compilation>, expectedOutput:="").VerifyIL("Module1.boo", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.s   42
  IL_0005:  bne.un.s   IL_0018
  IL_0007:  ldstr      "goo"
  IL_000c:  ldstr      "bar"
  IL_0011:  ldc.i4.0
  IL_0012:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0017:  pop
  IL_0018:  ldnull
  IL_0019:  ret
}
]]>)
            VerifySynthesizedStringHashMethod(compVerifier, expected:=False)
        End Sub

#End Region

    End Class

End Namespace
