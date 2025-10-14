' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenConstLocal
        Inherits BasicTestBase

        <Fact()>
        Public Sub TestBooleanConstLocal()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    sub SByRef(byRef x as boolean) 
        x = not x
    end sub

    Sub Main()
        const t as boolean = true
        const tandf as boolean = t and false

        console.WriteLine(t)
        console.WriteLine(tandf)
        console.WriteLine((not t))

        SByRef(t)
        console.writeline(t)            
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
True
False
False
True
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (Boolean V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0006:  ldc.i4.0
  IL_0007:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_000c:  ldc.i4.0
  IL_000d:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       "Sub C.SByRef(ByRef Boolean)"
  IL_001b:  ldc.i4.1
  IL_001c:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestCharConstLocal()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C
    sub SByRef(byRef c as char) 
        c = Strings.Chr(Strings.Asc("a"c) + 1)
    end sub

    Sub Main()
        const a as char = "a"c
        const b as char = Strings.Chr(Strings.Asc(a) + 1)

        console.WriteLine(a)
        console.WriteLine(b)
        console.WriteLine(Strings.Chr(Strings.Asc(b) + 1))

        SByRef(a)
        console.writeline(a)            
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
a
b
c
a
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (Char V_0)
  IL_0000:  ldc.i4.s   97
  IL_0002:  call       "Sub System.Console.WriteLine(Char)"
  IL_0007:  ldc.i4.s   98
  IL_0009:  call       "Sub System.Console.WriteLine(Char)"
  IL_000e:  ldc.i4.s   99
  IL_0010:  call       "Sub System.Console.WriteLine(Char)"
  IL_0015:  ldc.i4.s   97
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Sub C.SByRef(ByRef Char)"
  IL_001f:  ldc.i4.s   97
  IL_0021:  call       "Sub System.Console.WriteLine(Char)"
  IL_0026:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestIntegerConstLocal()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C
    sub SByRef(byRef i as integer) 
        i += 1
    end sub

    Sub Main()
        const i as integer = 100
        const j as integer = (i + 100)

        console.WriteLine(i)
        console.WriteLine(j)
        console.WriteLine((j + 100))

        SByRef(i)
        console.writeline(i)            
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
100
200
300
100
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   100
  IL_0002:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0007:  ldc.i4     0xc8
  IL_000c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0011:  ldc.i4     0x12c
  IL_0016:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001b:  ldc.i4.s   100
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       "Sub C.SByRef(ByRef Integer)"
  IL_0025:  ldc.i4.s   100
  IL_0027:  call       "Sub System.Console.WriteLine(Integer)"
  IL_002c:  ret
}
]]>)

        End Sub

        <Fact()>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        Public Sub TestDoubleConstLocal()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C
    sub SByRef(byRef i as double) 
        i *= 2
    end sub
Dim cul As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
    Sub Main()
        const pi as double = 3.1415926
        const r as double = (3.0*1.0)
        const m as double = double.maxValue

        console.WriteLine(pi.ToString(cul))
        console.WriteLine(r)
        console.WriteLine((pi*(r^2)).ToString(cul))
        console.WriteLine(m.ToString("G15", cul))
        console.WriteLine(double.maxValue.ToString("G15", cul))

        SByRef(pi)
        console.writeline(pi.ToString(cul))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
3.1415926
3
28.2743334
1.79769313486232E+308
1.79769313486232E+308
3.1415926
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      177 (0xb1)
  .maxstack  3
  .locals init (Double V_0)
  IL_0000:  ldc.r8     3.1415926
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldsfld     "C.cul As System.Globalization.CultureInfo"
  IL_0011:  call       "Function Double.ToString(System.IFormatProvider) As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ldc.r8     3
  IL_0024:  call       "Sub System.Console.WriteLine(Double)"
  IL_0029:  ldc.r8     28.2743334
  IL_0032:  stloc.0
  IL_0033:  ldloca.s   V_0
  IL_0035:  ldsfld     "C.cul As System.Globalization.CultureInfo"
  IL_003a:  call       "Function Double.ToString(System.IFormatProvider) As String"
  IL_003f:  call       "Sub System.Console.WriteLine(String)"
  IL_0044:  ldc.r8     1.79769313486232E+308
  IL_004d:  stloc.0
  IL_004e:  ldloca.s   V_0
  IL_0050:  ldstr      "G15"
  IL_0055:  ldsfld     "C.cul As System.Globalization.CultureInfo"
  IL_005a:  call       "Function Double.ToString(String, System.IFormatProvider) As String"
  IL_005f:  call       "Sub System.Console.WriteLine(String)"
  IL_0064:  ldc.r8     1.79769313486232E+308
  IL_006d:  stloc.0
  IL_006e:  ldloca.s   V_0
  IL_0070:  ldstr      "G15"
  IL_0075:  ldsfld     "C.cul As System.Globalization.CultureInfo"
  IL_007a:  call       "Function Double.ToString(String, System.IFormatProvider) As String"
  IL_007f:  call       "Sub System.Console.WriteLine(String)"
  IL_0084:  ldc.r8     3.1415926
  IL_008d:  stloc.0
  IL_008e:  ldloca.s   V_0
  IL_0090:  call       "Sub C.SByRef(ByRef Double)"
  IL_0095:  ldc.r8     3.1415926
  IL_009e:  stloc.0
  IL_009f:  ldloca.s   V_0
  IL_00a1:  ldsfld     "C.cul As System.Globalization.CultureInfo"
  IL_00a6:  call       "Function Double.ToString(System.IFormatProvider) As String"
  IL_00ab:  call       "Sub System.Console.WriteLine(String)"
  IL_00b0:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestStringConstLocal()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Module C
            Sub SByRef(ByRef s As String)
                s = "bye"
            End Sub

            Sub Main()
                Const hello As String = "hello"
                Const world As String = "world"
                Const msg = hello & " " & "world"

                console.WriteLine(hello)
                console.WriteLine(world)
                console.WriteLine(hello & " " & "world")

                SByRef(hello)
                console.writeline(hello)
            End Sub
        End Module
    ]]></file>
</compilation>, expectedOutput:=<![CDATA[
hello
world
hello world
hello
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldstr      "hello"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ldstr      "world"
  IL_000f:  call       "Sub System.Console.WriteLine(String)"
  IL_0014:  ldstr      "hello world"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  ldstr      "hello"
  IL_0023:  stloc.0
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       "Sub C.SByRef(ByRef String)"
  IL_002b:  ldstr      "hello"
  IL_0030:  call       "Sub System.Console.WriteLine(String)"
  IL_0035:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestDateTimeConstLocal()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Module C
            Sub SByRef(ByRef d As datetime)
                d = datetime.minvalue
            End Sub

            Sub Main()
                Const d1 As datetime = #3/2/2012#
                Const fmt ="M/d/yyyy h:mm:ss tt"
                Dim cul = System.Globalization.CultureInfo.InvariantCulture
                console.WriteLine(d1.ToString(fmt, cul))

                SByRef(d1)
                console.writeline(d1.ToString(fmt, cul))
            End Sub
        End Module
    ]]></file>
</compilation>, expectedOutput:=<![CDATA[
3/2/2012 12:00:00 AM
3/2/2012 12:00:00 AM
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (System.Globalization.CultureInfo V_0, //cul
  Date V_1)
  IL_0000:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0005:  stloc.0
  IL_0006:  ldc.i8     0x8cec61e8ba54000
  IL_000f:  newobj     "Sub Date..ctor(Long)"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldstr      "M/d/yyyy h:mm:ss tt"
  IL_001c:  ldloc.0
  IL_001d:  call       "Function Date.ToString(String, System.IFormatProvider) As String"
  IL_0022:  call       "Sub System.Console.WriteLine(String)"
  IL_0027:  ldloca.s   V_1
  IL_0029:  ldc.i8     0x8cec61e8ba54000
  IL_0032:  call       "Sub Date..ctor(Long)"
  IL_0037:  ldloca.s   V_1
  IL_0039:  call       "Sub C.SByRef(ByRef Date)"
  IL_003e:  ldc.i8     0x8cec61e8ba54000
  IL_0047:  newobj     "Sub Date..ctor(Long)"
  IL_004c:  stloc.1
  IL_004d:  ldloca.s   V_1
  IL_004f:  ldstr      "M/d/yyyy h:mm:ss tt"
  IL_0054:  ldloc.0
  IL_0055:  call       "Function Date.ToString(String, System.IFormatProvider) As String"
  IL_005a:  call       "Sub System.Console.WriteLine(String)"
  IL_005f:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestDecimalConstLocal()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    sub SByRef(byRef x as decimal)
        x = 2 * x
    end sub

    Sub Main()
        const dec99 as decimal = 99.00D
        const decZero as decimal = decimal.zero
        const dec100 as decimal = decimal.one + dec99

        console.writeline(dec99.ToString(System.Globalization.CultureInfo.InvariantCulture))
        console.WriteLine(decZero)
        console.WriteLine(decimal.Zero)
        console.WriteLine((decimal.one + dec99).ToString(System.Globalization.CultureInfo.InvariantCulture))

        SByRef(dec99)
        console.WriteLine(dec99.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
99.00
0
0
100.00
99.00
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      140 (0x8c)
  .maxstack  6
  .locals init (Decimal V_0)
  IL_0000:  ldc.i4     0x26ac
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0016:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ldsfld     "Decimal.Zero As Decimal"
  IL_0025:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_002a:  ldsfld     "Decimal.Zero As Decimal"
  IL_002f:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_0034:  ldc.i4     0x2710
  IL_0039:  ldc.i4.0
  IL_003a:  ldc.i4.0
  IL_003b:  ldc.i4.0
  IL_003c:  ldc.i4.2
  IL_003d:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0042:  stloc.0
  IL_0043:  ldloca.s   V_0
  IL_0045:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_004a:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_004f:  call       "Sub System.Console.WriteLine(String)"
  IL_0054:  ldloca.s   V_0
  IL_0056:  ldc.i4     0x26ac
  IL_005b:  ldc.i4.0
  IL_005c:  ldc.i4.0
  IL_005d:  ldc.i4.0
  IL_005e:  ldc.i4.2
  IL_005f:  call       "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0064:  ldloca.s   V_0
  IL_0066:  call       "Sub C.SByRef(ByRef Decimal)"
  IL_006b:  ldc.i4     0x26ac
  IL_0070:  ldc.i4.0
  IL_0071:  ldc.i4.0
  IL_0072:  ldc.i4.0
  IL_0073:  ldc.i4.2
  IL_0074:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0079:  stloc.0
  IL_007a:  ldloca.s   V_0
  IL_007c:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0081:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_0086:  call       "Sub System.Console.WriteLine(String)"
  IL_008b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestIntegerConstLocalInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C

    Sub Main()
          dim x = sub()
                const i as integer = 100
                console.WriteLine(i)
                end sub

        x()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
100
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0013:  ldftn      "Sub C._Closure$__._Lambda$__0-0()"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("C._Closure$__._Lambda$__0-0", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   100
  IL_0002:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0007:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestDecimalConstLocalInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C

    Sub Main()
          dim x = sub()
                const i as decimal = 99.99D
                console.WriteLine(i.ToString(System.Globalization.CultureInfo.InvariantCulture))
                end sub
        x()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
99.99
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0013:  ldftn      "Sub C._Closure$__._Lambda$__0-0()"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("C._Closure$__._Lambda$__0-0", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (Decimal V_0)
  IL_0000:  ldc.i4     0x270f
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0016:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>)

        End Sub

        <WorkItem(543469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543469")>
        <Fact()>
        Public Sub TestLiftedIntegerConstLocalInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C

    Sub Main()
          const i as integer = 100
          dim x = sub()
                console.WriteLine(i)
                end sub

        x()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
100
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0013:  ldftn      "Sub C._Closure$__._Lambda$__0-0()"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("C._Closure$__._Lambda$__0-0", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.s   100
  IL_0002:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0007:  ret
}
]]>)

        End Sub

        <WorkItem(543469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543469")>
        <Fact()>
        Public Sub TestLiftedDecimalConstLocalInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Module C

    Sub Main()
          const i_Main as decimal = 99.99D
          dim x = sub()
                const i_Lambda as decimal = 3.25D
                console.WriteLine("{0} {1}", i_Main.ToString(System.Globalization.CultureInfo.InvariantCulture), i_Lambda.ToString(System.Globalization.CultureInfo.InvariantCulture))
                end sub

        x()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
99.99 3.25
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0013:  ldftn      "Sub C._Closure$__._Lambda$__0-0()"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("C._Closure$__._Lambda$__0-0", <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  7
  .locals init (Decimal V_0)
  IL_0000:  ldstr      "{0} {1}"
  IL_0005:  ldc.i4     0x270f
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.2
  IL_000e:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_001b:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_0020:  ldc.i4     0x145
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.2
  IL_0029:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_002e:  stloc.0
  IL_002f:  ldloca.s   V_0
  IL_0031:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0036:  call       "Function Decimal.ToString(System.IFormatProvider) As String"
  IL_003b:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_0040:  ret
}
]]>)

        End Sub

        <WorkItem(543475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543475")>
        <Fact()>
        Public Sub TestLocalConstCycleDetection()

            Dim verifier = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Const i As integer = i
        Const j As integer = k
        const k as integer = j
        Console.Write("{0} {1} {3}", i, j, k)
    End Sub
End Module

    </file>
</compilation>, TestOptions.ReleaseExe)

            verifier.VerifyDiagnostics(Diagnostic(ERRID.ERR_CircularEvaluation1, "i").WithArguments("i"),
                                       Diagnostic(ERRID.ERR_CircularEvaluation1, "j").WithArguments("j"))
        End Sub

        <WorkItem(542910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542910")>
        <Fact()>
        Public Sub TestSByteLocalConst()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Const SB As SByte = 2
        Const SB2 As SByte = SB
        Console.Write(SB2.ToString())
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
2
]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (SByte V_0)
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Function SByte.ToString() As String"
  IL_0009:  call       "Sub System.Console.Write(String)"
  IL_000e:  ret
}
]]>)

        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub TruncatePrecisionFloat()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Globalization
Module C
    Sub Main()
        const temp1 as single = 23334800f / 5.5f
        console.WriteLine(Microsoft.VisualBasic.Int(temp1))
        console.WriteLine(Microsoft.VisualBasic.Int(23334800f / 5.5f))
        console.WriteLine((temp1 * 5.5).ToString(CultureInfo.InvariantCulture))

        const temp2 as double = 23334800.0 / 5.5
        console.WriteLine(Microsoft.VisualBasic.Int(temp2))
        console.WriteLine(Microsoft.VisualBasic.Int(23334800.0 / 5.5))
        console.WriteLine((temp2 * 5.5).ToString(CultureInfo.InvariantCulture))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
4242691
4242691
23334800.5
4242690
4242690
23334800
            ]]>)
        End Sub

        <Fact()>
        <WorkItem(49902, "https://github.com/dotnet/roslyn/issues/49902")>
        Public Sub BadConstantValue_1()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Shared Sub Main()
        Dim z As Integer = 2
        Const w As Integer = 2 ^ z
    End Sub

End Class
    </file>
</compilation>)

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30059: Constant expression is required.
        Const w As Integer = 2 ^ z
                                 ~
</expected>)
        End Sub

        <Fact()>
        <WorkItem(49902, "https://github.com/dotnet/roslyn/issues/49902")>
        Public Sub BadConstantValue_2()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Shared Sub Main()
        Dim z As Integer = 2
        Const w As Integer = z
    End Sub

End Class
    </file>
</compilation>)

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30059: Constant expression is required.
        Const w As Integer = z
                             ~
</expected>)
        End Sub

        <Fact()>
        <WorkItem(49902, "https://github.com/dotnet/roslyn/issues/49902")>
        Public Sub BadConstantValue_3()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Shared Sub Main()
        Dim z As Integer = 2
        Const w As Integer = z ^ 2
    End Sub

End Class
    </file>
</compilation>)

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30059: Constant expression is required.
        Const w As Integer = z ^ 2
                             ~
</expected>)
        End Sub

        <Fact()>
        <WorkItem(49902, "https://github.com/dotnet/roslyn/issues/49902")>
        Public Sub BadConstantValue_4()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb">
Class Test

    Shared Sub Main()
        Dim z As Integer = 2
        Const w As Integer = z ^ z
    End Sub

End Class
    </file>
</compilation>)

            compilation.AssertTheseEmitDiagnostics(
<expected>
BC30059: Constant expression is required.
        Const w As Integer = z ^ z
                             ~
BC30059: Constant expression is required.
        Const w As Integer = z ^ z
                                 ~
</expected>)
        End Sub
    End Class

End Namespace

