' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenVbRuntime
        Inherits BasicTestBase

        <Fact()>
        Public Sub ObjValueOnAssign()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Structure S1
    Public x As Integer

    Public Overrides Function GetHashCode() As Integer
        x += 1
        Return x
    End Function
End Structure

Module EmitTest
    Sub Main()
        Dim o As Object = New S1
        Dim oo As Object = o
        System.Console.Write(o.GetHashCode())
        System.Console.Write(o.GetHashCode())
        System.Console.Write(oo.GetHashCode())
        System.Console.Write(oo.GetHashCode())
    End Sub
End Module

Namespace Microsoft
    Namespace VisualBasic
        Namespace CompilerServices
            <AttributeUsage(AttributeTargets.[Class], Inherited:=False, AllowMultiple:=False)>
            Public Class StandardModuleAttribute
                Inherits Attribute

                Public Sub New()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

]]>
    </file>
</compilation>,
expectedOutput:="1234").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (Object V_0, //oo
  S1 V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "S1"
  IL_0008:  ldloc.1
  IL_0009:  box        "S1"
  IL_000e:  dup
  IL_000f:  stloc.0
  IL_0010:  dup
  IL_0011:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_0016:  call       "Sub System.Console.Write(Integer)"
  IL_001b:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_0020:  call       "Sub System.Console.Write(Integer)"
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_002b:  call       "Sub System.Console.Write(Integer)"
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_0036:  call       "Sub System.Console.Write(Integer)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ObjValueOnPassByValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Structure S1
    Public x As Integer

    Public Overrides Function GetHashCode() As Integer
        x += 1
        Return x
    End Function
End Structure

Module EmitTest
    Sub Main()
        Dim o As Object = New S1
        Test(o)
        Test(o)
        Test(o)
        Test(o)
    End Sub

    Private Sub Test(o As Object)
        System.Console.Write(o.GetHashCode())
    End Sub
End Module

Namespace Microsoft
    Namespace VisualBasic
        Namespace CompilerServices
            <AttributeUsage(AttributeTargets.[Class], Inherited:=False, AllowMultiple:=False)>
            Public Class StandardModuleAttribute
                Inherits Attribute

                Public Sub New()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

]]>
    </file>
</compilation>,
expectedOutput:="1234").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S1"
  IL_0008:  ldloc.0
  IL_0009:  box        "S1"
  IL_000e:  dup
  IL_000f:  call       "Sub EmitTest.Test(Object)"
  IL_0014:  dup
  IL_0015:  call       "Sub EmitTest.Test(Object)"
  IL_001a:  dup
  IL_001b:  call       "Sub EmitTest.Test(Object)"
  IL_0020:  call       "Sub EmitTest.Test(Object)"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ErrInCatch()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module EmitTest
    Sub Main()
        try
            system.Console.Write("boo")
        catch ex as exception
        end try
    End Sub
End Module

Namespace Microsoft
    Namespace VisualBasic
        Namespace CompilerServices
            <AttributeUsage(AttributeTargets.[Class], Inherited:=False, AllowMultiple:=False)>
            Public Class StandardModuleAttribute
                Inherits Attribute

                Public Sub New()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

]]>
    </file>
</compilation>,
expectedOutput:="boo").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (System.Exception V_0) //ex
  .try
{
  IL_0000:  ldstr      "boo"
  IL_0005:  call       "Sub System.Console.Write(String)"
  IL_000a:  leave.s    IL_000f
}
  catch System.Exception
{
  IL_000c:  stloc.0
  IL_000d:  leave.s    IL_000f
}
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ErrInCatch1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module EmitTest
    Sub Main()
        try
            system.Console.Write("boo")
        catch 
        end try
    End Sub
End Module

Namespace Microsoft
    Namespace VisualBasic
        Namespace CompilerServices
            <AttributeUsage(AttributeTargets.[Class], Inherited:=False, AllowMultiple:=False)>
            Public Class StandardModuleAttribute
                Inherits Attribute

                Public Sub New()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

]]>
    </file>
</compilation>,
expectedOutput:="boo").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .try
{
  IL_0000:  ldstr      "boo"
  IL_0005:  call       "Sub System.Console.Write(String)"
  IL_000a:  leave.s    IL_000f
}
  catch System.Exception
{
  IL_000c:  pop
  IL_000d:  leave.s    IL_000f
}
  IL_000f:  ret
}
]]>)
        End Sub
    End Class
End Namespace

