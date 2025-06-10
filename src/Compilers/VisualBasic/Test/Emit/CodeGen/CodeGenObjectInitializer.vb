' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenObjectInitializer
        Inherits BasicTestBase

        <Fact()>
        Public Sub ObjectInitializerAsRefTypeEqualsNew()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public SelfRef as C2
End Class

Class C1
    Public Shared Sub Main()
        Dim x as C2 = new C2() With {.Field = 23, .AProperty = 42, .SelfRef = x}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        If x.SelfRef is Nothing then
            Console.WriteLine("Nothing")
        End If
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
Nothing
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (C2 V_0) //x
  IL_0000:  newobj     "Sub C2..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.s   23
  IL_0008:  stfld      "C2.Field As Integer"
  IL_000d:  dup
  IL_000e:  ldc.i4.s   42
  IL_0010:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0015:  dup
  IL_0016:  ldloc.0
  IL_0017:  stfld      "C2.SelfRef As C2"
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ldfld      "C2.Field As Integer"
  IL_0023:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0028:  ldloc.0
  IL_0029:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_002e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0033:  ldloc.0
  IL_0034:  ldfld      "C2.SelfRef As C2"
  IL_0039:  brtrue.s   IL_0045
  IL_003b:  ldstr      "Nothing"
  IL_0040:  call       "Sub System.Console.WriteLine(String)"
  IL_0045:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewRefType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer 
   
    Public Property AProperty as Integer

    Public SelfRef as C2
End Class

Class C1
    Public Shared Sub Main()
        Dim x as New C2() With {.Field = 23, .AProperty = 42, .SelfRef = x}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        If x.SelfRef is Nothing then
            Console.WriteLine("Nothing")
        End If
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
Nothing
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (C2 V_0) //x
  IL_0000:  newobj     "Sub C2..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.s   23
  IL_0008:  stfld      "C2.Field As Integer"
  IL_000d:  dup
  IL_000e:  ldc.i4.s   42
  IL_0010:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0015:  dup
  IL_0016:  ldloc.0
  IL_0017:  stfld      "C2.SelfRef As C2"
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ldfld      "C2.Field As Integer"
  IL_0023:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0028:  ldloc.0
  IL_0029:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_002e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0033:  ldloc.0
  IL_0034:  ldfld      "C2.SelfRef As C2"
  IL_0039:  brtrue.s   IL_0045
  IL_003b:  ldstr      "Nothing"
  IL_0040:  call       "Sub System.Console.WriteLine(String)"
  IL_0045:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsValueTypeEqualsNew()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer
End Structure

Class C1
    Public Shared Sub Main()
        Dim x as S2 = new S2() With {.Field = 23, .AProperty = 23, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
23
0
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (S2 V_0, //x
  S2 V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "S2"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.s   23
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloc.1
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "S2.Field As Integer"
  IL_002f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0034:  ldloca.s   V_0
  IL_0036:  call       "Function S2.get_AProperty() As Integer"
  IL_003b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0040:  ldloc.0
  IL_0041:  ldfld      "S2.Field2 As Integer"
  IL_0046:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer
End Structure

Class C1
    Public Shared Sub Main()
        Dim x as new S2() With {.Field = 23, .AProperty = 42, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (S2 V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S2"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloc.0
  IL_0028:  ldfld      "S2.Field As Integer"
  IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0032:  ldloca.s   V_0
  IL_0034:  call       "Function S2.get_AProperty() As Integer"
  IL_0039:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003e:  ldloc.0
  IL_003f:  ldfld      "S2.Field2 As Integer"
  IL_0044:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0049:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsValueTypeEqualsNewOneParameterConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer

    Public Sub New(p as integer)
        Field = p
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Dim x as S2 = new S2(1) With {.Field = 23, .AProperty = 23, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
23
0
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (S2 V_0, //x
  S2 V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub S2..ctor(Integer)"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.s   23
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloc.1
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "S2.Field As Integer"
  IL_002f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0034:  ldloca.s   V_0
  IL_0036:  call       "Function S2.get_AProperty() As Integer"
  IL_003b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0040:  ldloc.0
  IL_0041:  ldfld      "S2.Field2 As Integer"
  IL_0046:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueTypeOneParameterConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer

    Public Sub New(p as integer)
        Field = p
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Dim x as new S2(1) With {.Field = 23, .AProperty = 42, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (S2 V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub S2..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloc.0
  IL_0028:  ldfld      "S2.Field As Integer"
  IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0032:  ldloca.s   V_0
  IL_0034:  call       "Function S2.get_AProperty() As Integer"
  IL_0039:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003e:  ldloc.0
  IL_003f:  ldfld      "S2.Field2 As Integer"
  IL_0044:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0049:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueTypeInTryCatch()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer
End Structure

Class C1
    Public Shared Sub Main()
        try
            Dim x as new S2() With {.Field = 23, .AProperty = 42, .Field2 = x.Field}

            Console.WriteLine(x.Field)
            Console.WriteLine(x.AProperty)
            Console.WriteLine(x.Field2)
        catch
            Console.WriteLine("failed")
        end try
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       98 (0x62)
  .maxstack  2
  .locals init (S2 V_0) //x
  .try
  {
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    "S2"
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldc.i4.s   23
    IL_000c:  stfld      "S2.Field As Integer"
    IL_0011:  ldloca.s   V_0
    IL_0013:  ldc.i4.s   42
    IL_0015:  call       "Sub S2.set_AProperty(Integer)"
    IL_001a:  ldloca.s   V_0
    IL_001c:  ldloc.0
    IL_001d:  ldfld      "S2.Field As Integer"
    IL_0022:  stfld      "S2.Field2 As Integer"
    IL_0027:  ldloc.0
    IL_0028:  ldfld      "S2.Field As Integer"
    IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0032:  ldloca.s   V_0
    IL_0034:  call       "Function S2.get_AProperty() As Integer"
    IL_0039:  call       "Sub System.Console.WriteLine(Integer)"
    IL_003e:  ldloc.0
    IL_003f:  ldfld      "S2.Field2 As Integer"
    IL_0044:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0049:  leave.s    IL_0061
  }
  catch System.Exception
  {
    IL_004b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0050:  ldstr      "failed"
    IL_0055:  call       "Sub System.Console.WriteLine(String)"
    IL_005a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_005f:  leave.s    IL_0061
  }
  IL_0061:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueTypeOneParameterConstructorMultipleVariables()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer

    Public Sub New(p as integer)
        Field = p
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Dim x, y as new S2(1) With {.Field = 23, .AProperty = 42, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)

        Console.WriteLine(y.Field)
        Console.WriteLine(y.AProperty)
        Console.WriteLine(y.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
23
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      147 (0x93)
  .maxstack  2
  .locals init (S2 V_0, //x
                S2 V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub S2..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloca.s   V_1
  IL_0029:  ldc.i4.1
  IL_002a:  call       "Sub S2..ctor(Integer)"
  IL_002f:  ldloca.s   V_1
  IL_0031:  ldc.i4.s   23
  IL_0033:  stfld      "S2.Field As Integer"
  IL_0038:  ldloca.s   V_1
  IL_003a:  ldc.i4.s   42
  IL_003c:  call       "Sub S2.set_AProperty(Integer)"
  IL_0041:  ldloca.s   V_1
  IL_0043:  ldloc.0
  IL_0044:  ldfld      "S2.Field As Integer"
  IL_0049:  stfld      "S2.Field2 As Integer"
  IL_004e:  ldloc.0
  IL_004f:  ldfld      "S2.Field As Integer"
  IL_0054:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0059:  ldloca.s   V_0
  IL_005b:  call       "Function S2.get_AProperty() As Integer"
  IL_0060:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0065:  ldloc.0
  IL_0066:  ldfld      "S2.Field2 As Integer"
  IL_006b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0070:  ldloc.1
  IL_0071:  ldfld      "S2.Field As Integer"
  IL_0076:  call       "Sub System.Console.WriteLine(Integer)"
  IL_007b:  ldloca.s   V_1
  IL_007d:  call       "Function S2.get_AProperty() As Integer"
  IL_0082:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0087:  ldloc.1
  IL_0088:  ldfld      "S2.Field2 As Integer"
  IL_008d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0092:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueTypeMultipleVariables()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer
End Structure

Class C1
    Public Shared Sub Main()
        Dim x, y as new S2() With {.Field = 23, .AProperty = 42, .Field2 = x.Field}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)
        Console.WriteLine(x.Field2)

        Console.WriteLine(y.Field)
        Console.WriteLine(y.AProperty)
        Console.WriteLine(y.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
23
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      147 (0x93)
  .maxstack  2
  .locals init (S2 V_0, //x
                S2 V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S2"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "S2.Field As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "Sub S2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "S2.Field As Integer"
  IL_0022:  stfld      "S2.Field2 As Integer"
  IL_0027:  ldloca.s   V_1
  IL_0029:  initobj    "S2"
  IL_002f:  ldloca.s   V_1
  IL_0031:  ldc.i4.s   23
  IL_0033:  stfld      "S2.Field As Integer"
  IL_0038:  ldloca.s   V_1
  IL_003a:  ldc.i4.s   42
  IL_003c:  call       "Sub S2.set_AProperty(Integer)"
  IL_0041:  ldloca.s   V_1
  IL_0043:  ldloc.0
  IL_0044:  ldfld      "S2.Field As Integer"
  IL_0049:  stfld      "S2.Field2 As Integer"
  IL_004e:  ldloc.0
  IL_004f:  ldfld      "S2.Field As Integer"
  IL_0054:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0059:  ldloca.s   V_0
  IL_005b:  call       "Function S2.get_AProperty() As Integer"
  IL_0060:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0065:  ldloc.0
  IL_0066:  ldfld      "S2.Field2 As Integer"
  IL_006b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0070:  ldloc.1
  IL_0071:  ldfld      "S2.Field As Integer"
  IL_0076:  call       "Sub System.Console.WriteLine(Integer)"
  IL_007b:  ldloca.s   V_1
  IL_007d:  call       "Function S2.get_AProperty() As Integer"
  IL_0082:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0087:  ldloc.1
  IL_0088:  ldfld      "S2.Field2 As Integer"
  IL_008d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0092:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewRefTypeMultipleVariables()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public Field2 as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim x, y as new C2() With {.Field = 23, .AProperty = 42, .Field2 = if(x is nothing, -1, x.Field)}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)        
        Console.WriteLine(x.Field2)

        Console.WriteLine(y.Field)
        Console.WriteLine(y.AProperty)
        Console.WriteLine(y.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
-1
23
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      145 (0x91)
  .maxstack  3
  .locals init (C2 V_0) //x
  IL_0000:  newobj     "Sub C2..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.s   23
  IL_0008:  stfld      "C2.Field As Integer"
  IL_000d:  dup
  IL_000e:  ldc.i4.s   42
  IL_0010:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0015:  dup
  IL_0016:  ldloc.0
  IL_0017:  brfalse.s  IL_0021
  IL_0019:  ldloc.0
  IL_001a:  ldfld      "C2.Field As Integer"
  IL_001f:  br.s       IL_0022
  IL_0021:  ldc.i4.m1
  IL_0022:  stfld      "C2.Field2 As Integer"
  IL_0027:  stloc.0
  IL_0028:  newobj     "Sub C2..ctor()"
  IL_002d:  dup
  IL_002e:  ldc.i4.s   23
  IL_0030:  stfld      "C2.Field As Integer"
  IL_0035:  dup
  IL_0036:  ldc.i4.s   42
  IL_0038:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_003d:  dup
  IL_003e:  ldloc.0
  IL_003f:  brfalse.s  IL_0049
  IL_0041:  ldloc.0
  IL_0042:  ldfld      "C2.Field As Integer"
  IL_0047:  br.s       IL_004a
  IL_0049:  ldc.i4.m1
  IL_004a:  stfld      "C2.Field2 As Integer"
  IL_004f:  ldloc.0
  IL_0050:  ldfld      "C2.Field As Integer"
  IL_0055:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005a:  ldloc.0
  IL_005b:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_0060:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0065:  ldloc.0
  IL_0066:  ldfld      "C2.Field2 As Integer"
  IL_006b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0070:  dup
  IL_0071:  ldfld      "C2.Field As Integer"
  IL_0076:  call       "Sub System.Console.WriteLine(Integer)"
  IL_007b:  dup
  IL_007c:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_0081:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0086:  ldfld      "C2.Field2 As Integer"
  IL_008b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0090:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewTypeParameterNewConstraint()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Interface IProperty
    Property AProperty as Integer
End Interface

Public Class C2
    implements IProperty

    Property AProperty as Integer implements IProperty.AProperty
End Class

Class C1
    Public Shared Sub DoStuff(Of T as {IProperty, New})()
        Dim x as new T() With {.AProperty = 42}

        Console.WriteLine(x.AProperty)        
    End Sub


    Public Shared Sub Main()
        DoStuff(Of C2)()
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
42
]]>).VerifyIL("C1.DoStuff", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0, //x
  T V_1)
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  ldc.i4.s   42
  IL_000a:  constrained. "T"
  IL_0010:  callvirt   "Sub IProperty.set_AProperty(Integer)"
  IL_0015:  ldloc.1
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Function IProperty.get_AProperty() As Integer"
  IL_0024:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewTypeParameterStructureConstraint()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Interface IProperty
    Property AProperty as Integer
End Interface

Public Structure C2
    implements IProperty

    Property AProperty as Integer implements IProperty.AProperty
End Structure

Class C1
    Public Shared Sub DoStuff(Of T as {IProperty, Structure})()
        Dim x as new T() With {.AProperty = 42}

        Console.WriteLine(x.AProperty)        
    End Sub


    Public Shared Sub Main()
        DoStuff(Of C2)()
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
42
]]>).VerifyIL("C1.DoStuff", <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (T V_0) //x
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.s   42
  IL_000a:  constrained. "T"
  IL_0010:  callvirt   "Sub IProperty.set_AProperty(Integer)"
  IL_0015:  ldloca.s   V_0
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Function IProperty.get_AProperty() As Integer"
  IL_0022:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0027:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewTypeParameterClassConstraint()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Interface IProperty
    Property AProperty as Integer
End Interface

Public Class C2
    implements IProperty

    Property AProperty as Integer implements IProperty.AProperty
End Class

Class C1
    Public Shared Sub DoStuff(Of T as {C2, New})()
        Dim x as new T() With {.AProperty = 42}

        Console.WriteLine(x.AProperty)        
    End Sub


    Public Shared Sub Main()
        DoStuff(Of C2)()
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
42
]]>).VerifyIL("C1.DoStuff", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  dup
  IL_0006:  box        "T"
  IL_000b:  ldc.i4.s   42
  IL_000d:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0012:  box        "T"
  IL_0017:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_001c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNestedAsRefTypeEqualsNew()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
   
    Public Property AProperty as Integer

    Public ARef as C2
End Class

Class C1
    Public Shared Sub Main()
        Dim x as C2 = new C2() With {.Field = 23, .AProperty = 42, .ARef = new C2() With {.Field=42, .AProperty=23} }

        Console.WriteLine(x.Field)
        Console.WriteLine(x.AProperty)

        Console.WriteLine(x.ARef.Field)
        Console.WriteLine(x.ARef.AProperty)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
42
23
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      102 (0x66)
  .maxstack  5
  IL_0000:  newobj     "Sub C2..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.s   23
  IL_0008:  stfld      "C2.Field As Integer"
  IL_000d:  dup
  IL_000e:  ldc.i4.s   42
  IL_0010:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0015:  dup
  IL_0016:  newobj     "Sub C2..ctor()"
  IL_001b:  dup
  IL_001c:  ldc.i4.s   42
  IL_001e:  stfld      "C2.Field As Integer"
  IL_0023:  dup
  IL_0024:  ldc.i4.s   23
  IL_0026:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_002b:  stfld      "C2.ARef As C2"
  IL_0030:  dup
  IL_0031:  ldfld      "C2.Field As Integer"
  IL_0036:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003b:  dup
  IL_003c:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_0041:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0046:  dup
  IL_0047:  ldfld      "C2.ARef As C2"
  IL_004c:  ldfld      "C2.Field As Integer"
  IL_0051:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0056:  ldfld      "C2.ARef As C2"
  IL_005b:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_0060:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0065:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerRefTypeIntoArrayElement()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
    Public Property AProperty as Integer
    Public SelfRef as C2
End Class

Class C1
    Public Shared Sub Main()
        Dim arr(0) as C2
        arr(0) = new C2() With {.Field = 23, .AProperty = 42, .SelfRef=arr(0)}

        Console.WriteLine(arr(0).Field)
        Console.WriteLine(arr(0).AProperty)
        If arr(0).SelfRef is Nothing then
            Console.WriteLine("Nothing")
        End If

    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
Nothing
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       87 (0x57)
  .maxstack  6
  .locals init (C2() V_0) //arr
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "C2"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  newobj     "Sub C2..ctor()"
  IL_000e:  dup
  IL_000f:  ldc.i4.s   23
  IL_0011:  stfld      "C2.Field As Integer"
  IL_0016:  dup
  IL_0017:  ldc.i4.s   42
  IL_0019:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_001e:  dup
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldelem.ref
  IL_0022:  stfld      "C2.SelfRef As C2"
  IL_0027:  stelem.ref
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.0
  IL_002a:  ldelem.ref
  IL_002b:  ldfld      "C2.Field As Integer"
  IL_0030:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4.0
  IL_0037:  ldelem.ref
  IL_0038:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_003d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0042:  ldloc.0
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.ref
  IL_0045:  ldfld      "C2.SelfRef As C2"
  IL_004a:  brtrue.s   IL_0056
  IL_004c:  ldstr      "Nothing"
  IL_0051:  call       "Sub System.Console.WriteLine(String)"
  IL_0056:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerValueTypeIntoArrayElement()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure C2
    Public Field as Integer
    Public Property AProperty as Integer
    Public Field2 as Integer
End Structure

Class C1
    Public Shared Sub Main()
        Dim arr(0) as C2
        arr(0) = new C2() With {.Field = 23, .AProperty = 42, .Field2=arr(0).Field}

        Console.WriteLine(arr(0).Field)
        Console.WriteLine(arr(0).AProperty)
        Console.WriteLine(arr(0).Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
0
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      112 (0x70)
  .maxstack  5
  .locals init (C2() V_0, //arr
  C2 V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "C2"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  initobj    "C2"
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.s   23
  IL_0015:  stfld      "C2.Field As Integer"
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.s   42
  IL_001e:  call       "Sub C2.set_AProperty(Integer)"
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.0
  IL_0027:  ldelema    "C2"
  IL_002c:  ldfld      "C2.Field As Integer"
  IL_0031:  stfld      "C2.Field2 As Integer"
  IL_0036:  ldloc.1
  IL_0037:  stelem     "C2"
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.0
  IL_003e:  ldelema    "C2"
  IL_0043:  ldfld      "C2.Field As Integer"
  IL_0048:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004d:  ldloc.0
  IL_004e:  ldc.i4.0
  IL_004f:  ldelema    "C2"
  IL_0054:  call       "Function C2.get_AProperty() As Integer"
  IL_0059:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005e:  ldloc.0
  IL_005f:  ldc.i4.0
  IL_0060:  ldelema    "C2"
  IL_0065:  ldfld      "C2.Field2 As Integer"
  IL_006a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_006f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerRefTypeIntoField()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
    Public Property AProperty as Integer
    Public SelfRef as C2
End Class

Class C1
    Public Shared MyTarget as C2

    Public Shared Sub Main()
        MyTarget = new C2() With {.Field = 23, .AProperty = 42, .SelfRef=MyTarget}

        Console.WriteLine(MyTarget.Field)
        Console.WriteLine(MyTarget.AProperty)
        If MyTarget.SelfRef is Nothing then
            Console.WriteLine("Nothing")
        End If

    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
Nothing
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       90 (0x5a)
  .maxstack  3
  IL_0000:  newobj     "Sub C2..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.s   23
  IL_0008:  stfld      "C2.Field As Integer"
  IL_000d:  dup
  IL_000e:  ldc.i4.s   42
  IL_0010:  callvirt   "Sub C2.set_AProperty(Integer)"
  IL_0015:  dup
  IL_0016:  ldsfld     "C1.MyTarget As C2"
  IL_001b:  stfld      "C2.SelfRef As C2"
  IL_0020:  stsfld     "C1.MyTarget As C2"
  IL_0025:  ldsfld     "C1.MyTarget As C2"
  IL_002a:  ldfld      "C2.Field As Integer"
  IL_002f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0034:  ldsfld     "C1.MyTarget As C2"
  IL_0039:  callvirt   "Function C2.get_AProperty() As Integer"
  IL_003e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0043:  ldsfld     "C1.MyTarget As C2"
  IL_0048:  ldfld      "C2.SelfRef As C2"
  IL_004d:  brtrue.s   IL_0059
  IL_004f:  ldstr      "Nothing"
  IL_0054:  call       "Sub System.Console.WriteLine(String)"
  IL_0059:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerValueTypeIntoField()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure C2
    Public Field as Integer
    Public Property AProperty as Integer
    Public Field2 as Integer
End Structure

Class C1

    Public Shared MyTarget as C2

    Public Shared Sub Main()
        MyTarget = new C2() With {.Field = 23, .AProperty = 42, .Field2=MyTarget.Field}

        Console.WriteLine(MyTarget.Field)
        Console.WriteLine(MyTarget.AProperty)
        Console.WriteLine(MyTarget.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
0
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (C2 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "C2"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   23
  IL_000c:  stfld      "C2.Field As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "Sub C2.set_AProperty(Integer)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldsflda    "C1.MyTarget As C2"
  IL_0021:  ldfld      "C2.Field As Integer"
  IL_0026:  stfld      "C2.Field2 As Integer"
  IL_002b:  ldloc.0
  IL_002c:  stsfld     "C1.MyTarget As C2"
  IL_0031:  ldsflda    "C1.MyTarget As C2"
  IL_0036:  ldfld      "C2.Field As Integer"
  IL_003b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0040:  ldsflda    "C1.MyTarget As C2"
  IL_0045:  call       "Function C2.get_AProperty() As Integer"
  IL_004a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004f:  ldsflda    "C1.MyTarget As C2"
  IL_0054:  ldfld      "C2.Field2 As Integer"
  IL_0059:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerAsNewValueTypeMultipleVariablesOfField()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure C2
    Public Field as Integer
    Public Property AProperty as Integer
    Public Field2 as Integer
End Structure

Class C1
    Public Shared MyField1, MyField2 As New C2() With {.Field = 23, .AProperty = 42, .Field2=Myfield2.Field}

    Public Shared Sub Main()
        Console.WriteLine(MyField1.Field)
        Console.WriteLine(MyField1.AProperty)
        Console.WriteLine(MyField1.Field2)

        Console.WriteLine(MyField2.Field)
        Console.WriteLine(MyField2.AProperty)
        Console.WriteLine(MyField2.Field2)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
0
23
42
23
]]>).VerifyIL("C1..cctor", <![CDATA[
{
  // Code size      111 (0x6f)
  .maxstack  2
  IL_0000:  ldsflda    "C1.MyField1 As C2"
  IL_0005:  initobj    "C2"
  IL_000b:  ldsflda    "C1.MyField1 As C2"
  IL_0010:  ldc.i4.s   23
  IL_0012:  stfld      "C2.Field As Integer"
  IL_0017:  ldsflda    "C1.MyField1 As C2"
  IL_001c:  ldc.i4.s   42
  IL_001e:  call       "Sub C2.set_AProperty(Integer)"
  IL_0023:  ldsflda    "C1.MyField1 As C2"
  IL_0028:  ldsflda    "C1.MyField2 As C2"
  IL_002d:  ldfld      "C2.Field As Integer"
  IL_0032:  stfld      "C2.Field2 As Integer"
  IL_0037:  ldsflda    "C1.MyField2 As C2"
  IL_003c:  initobj    "C2"
  IL_0042:  ldsflda    "C1.MyField2 As C2"
  IL_0047:  ldc.i4.s   23
  IL_0049:  stfld      "C2.Field As Integer"
  IL_004e:  ldsflda    "C1.MyField2 As C2"
  IL_0053:  ldc.i4.s   42
  IL_0055:  call       "Sub C2.set_AProperty(Integer)"
  IL_005a:  ldsflda    "C1.MyField2 As C2"
  IL_005f:  ldsflda    "C1.MyField2 As C2"
  IL_0064:  ldfld      "C2.Field As Integer"
  IL_0069:  stfld      "C2.Field2 As Integer"
  IL_006e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerPropertyInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S1
    Public Field as Integer
End Structure

Public Class C1
    Public Field as Integer
End Class


Class C2
    Public Shared Property MyProperty1 As New S1() With {.Field = 11}
    Public Shared Property MyProperty2 As S1 = New S1() With {.Field = 22}

    Public Shared Property MyProperty3 As New C1() With {.Field = 33}
    Public Shared Property MyProperty4 As C1 = New C1() With {.Field = 44}

    Public Shared Sub Main()
        Console.WriteLine(MyProperty1.Field)
        Console.WriteLine(MyProperty2.Field)
        Console.WriteLine(MyProperty3.Field)
        Console.WriteLine(MyProperty4.Field)
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
11
22
33
44
]]>).VerifyIL("C2..cctor", <![CDATA[
{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S1"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   11
  IL_000c:  stfld      "S1.Field As Integer"
  IL_0011:  ldloc.0
  IL_0012:  call       "Sub C2.set_MyProperty1(S1)"
  IL_0017:  ldloca.s   V_0
  IL_0019:  initobj    "S1"
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldc.i4.s   22
  IL_0023:  stfld      "S1.Field As Integer"
  IL_0028:  ldloc.0
  IL_0029:  call       "Sub C2.set_MyProperty2(S1)"
  IL_002e:  newobj     "Sub C1..ctor()"
  IL_0033:  dup
  IL_0034:  ldc.i4.s   33
  IL_0036:  stfld      "C1.Field As Integer"
  IL_003b:  call       "Sub C2.set_MyProperty3(C1)"
  IL_0040:  newobj     "Sub C1..ctor()"
  IL_0045:  dup
  IL_0046:  ldc.i4.s   44
  IL_0048:  stfld      "C1.Field As Integer"
  IL_004d:  call       "Sub C2.set_MyProperty4(C1)"
  IL_0052:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerFieldInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Structure S1
    Public Field as Integer
End Structure

Public Class C1
    Public Field as Integer
End Class


Class C2
    Public Shared Field1 As New S1() With {.Field = 11}
    Public Shared Field2, Field3 As New S1() With {.Field = 22}
    Public Shared Field4 As S1 = New S1() With {.Field = 33}

    Public Shared Field5 As New C1() With {.Field = 44}
    Public Shared Field6, Field7 As New C1() With {.Field = 55}
    Public Shared Field8 As C1 = New C1() With {.Field = 66}

    Public Field09 As New S1() With {.Field = 11}
    Public Field10, Field11 As New S1() With {.Field = 22}
    Public Field12 As S1 = New S1() With {.Field = 33}

    Public Field13 As New C1() With {.Field = 44}
    Public Field14, Field15 As New C1() With {.Field = 55}
    Public Field16 As C1 = New C1() With {.Field = 66}

    Public Shared Sub Main()
        Console.WriteLine(Field1.Field)
        Console.WriteLine(Field2.Field)
        Console.WriteLine(Field3.Field)
        Console.WriteLine(Field4.Field)
        Console.WriteLine(Field5.Field)
        Console.WriteLine(Field6.Field)
        Console.WriteLine(Field7.Field)
        Console.WriteLine(Field8.Field)

        dim x as new C2()
        Console.WriteLine(x.Field09.Field)
        Console.WriteLine(x.Field10.Field)
        Console.WriteLine(x.Field11.Field)
        Console.WriteLine(x.Field12.Field)
        Console.WriteLine(x.Field13.Field)
        Console.WriteLine(x.Field14.Field)
        Console.WriteLine(x.Field15.Field)
        Console.WriteLine(x.Field16.Field)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
11
22
22
33
44
55
55
66
11
22
22
33
44
55
55
66
]]>).VerifyIL("C2..cctor", <![CDATA[
{
  // Code size      165 (0xa5)
  .maxstack  3
  .locals init (S1 V_0)
  IL_0000:  ldsflda    "C2.Field1 As S1"
  IL_0005:  initobj    "S1"
  IL_000b:  ldsflda    "C2.Field1 As S1"
  IL_0010:  ldc.i4.s   11
  IL_0012:  stfld      "S1.Field As Integer"
  IL_0017:  ldsflda    "C2.Field2 As S1"
  IL_001c:  initobj    "S1"
  IL_0022:  ldsflda    "C2.Field2 As S1"
  IL_0027:  ldc.i4.s   22
  IL_0029:  stfld      "S1.Field As Integer"
  IL_002e:  ldsflda    "C2.Field3 As S1"
  IL_0033:  initobj    "S1"
  IL_0039:  ldsflda    "C2.Field3 As S1"
  IL_003e:  ldc.i4.s   22
  IL_0040:  stfld      "S1.Field As Integer"
  IL_0045:  ldloca.s   V_0
  IL_0047:  initobj    "S1"
  IL_004d:  ldloca.s   V_0
  IL_004f:  ldc.i4.s   33
  IL_0051:  stfld      "S1.Field As Integer"
  IL_0056:  ldloc.0
  IL_0057:  stsfld     "C2.Field4 As S1"
  IL_005c:  newobj     "Sub C1..ctor()"
  IL_0061:  dup
  IL_0062:  ldc.i4.s   44
  IL_0064:  stfld      "C1.Field As Integer"
  IL_0069:  stsfld     "C2.Field5 As C1"
  IL_006e:  newobj     "Sub C1..ctor()"
  IL_0073:  dup
  IL_0074:  ldc.i4.s   55
  IL_0076:  stfld      "C1.Field As Integer"
  IL_007b:  stsfld     "C2.Field6 As C1"
  IL_0080:  newobj     "Sub C1..ctor()"
  IL_0085:  dup
  IL_0086:  ldc.i4.s   55
  IL_0088:  stfld      "C1.Field As Integer"
  IL_008d:  stsfld     "C2.Field7 As C1"
  IL_0092:  newobj     "Sub C1..ctor()"
  IL_0097:  dup
  IL_0098:  ldc.i4.s   66
  IL_009a:  stfld      "C1.Field As Integer"
  IL_009f:  stsfld     "C2.Field8 As C1"
  IL_00a4:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InitTemp001()
            Dim source =
<compilation>
    <file name="a.vb">
imports System
Module Module1

    Sub Main()
        S.Test()
    End Sub

    Structure S
        Public x As Integer

        Shared Sub Test()
            Console.WriteLine((New S With {.x = 0}).Equals(New S() With {.x = 1}))
        End Sub
    End Structure

End Module
     
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
False
]]>).VerifyIL("Module1.S.Test", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (Module1.S V_0,
  Module1.S V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Module1.S"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      "Module1.S.x As Integer"
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    "Module1.S"
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.1
  IL_001d:  stfld      "Module1.S.x As Integer"
  IL_0022:  ldloc.1
  IL_0023:  box        "Module1.S"
  IL_0028:  constrained. "Module1.S"
  IL_002e:  callvirt   "Function System.ValueType.Equals(Object) As Boolean"
  IL_0033:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InitTemp002()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module Module1

    Sub Main()
        S.Test()
    End Sub

    Structure S
        Public x As Integer

        Shared Sub Test()
            Console.WriteLine(New S With {.x = 0}.Equals(New S With {.x = 1}).Equals(
                          New S With {.x = 1}.Equals(New S With {.x = 1})))
        End Sub
    End Structure

End Module
    
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
False
]]>).VerifyIL("Module1.S.Test", <![CDATA[
{
  // Code size      116 (0x74)
  .maxstack  4
  .locals init (Module1.S V_0,
  Module1.S V_1,
  Boolean V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Module1.S"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      "Module1.S.x As Integer"
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    "Module1.S"
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.1
  IL_001d:  stfld      "Module1.S.x As Integer"
  IL_0022:  ldloc.1
  IL_0023:  box        "Module1.S"
  IL_0028:  constrained. "Module1.S"
  IL_002e:  callvirt   "Function System.ValueType.Equals(Object) As Boolean"
  IL_0033:  stloc.2
  IL_0034:  ldloca.s   V_2
  IL_0036:  ldloca.s   V_0
  IL_0038:  initobj    "Module1.S"
  IL_003e:  ldloca.s   V_0
  IL_0040:  ldc.i4.1
  IL_0041:  stfld      "Module1.S.x As Integer"
  IL_0046:  ldloca.s   V_0
  IL_0048:  ldloca.s   V_1
  IL_004a:  initobj    "Module1.S"
  IL_0050:  ldloca.s   V_1
  IL_0052:  ldc.i4.1
  IL_0053:  stfld      "Module1.S.x As Integer"
  IL_0058:  ldloc.1
  IL_0059:  box        "Module1.S"
  IL_005e:  constrained. "Module1.S"
  IL_0064:  callvirt   "Function System.ValueType.Equals(Object) As Boolean"
  IL_0069:  call       "Function Boolean.Equals(Boolean) As Boolean"
  IL_006e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0073:  ret
}
]]>)
        End Sub

    End Class
End Namespace
