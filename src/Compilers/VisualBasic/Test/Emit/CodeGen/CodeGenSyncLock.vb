' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenSyncLock
        Inherits BasicTestBase

        <Fact()>
        Public Sub SimpleSyncLock()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside SyncLock.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldtoken    "C1"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  .try
{
  IL_000d:  ldloc.0
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0015:  ldstr      "Inside SyncLock."
  IL_001a:  call       "Sub System.Console.WriteLine(String)"
  IL_001f:  leave.s    IL_002b
}
  finally
{
  IL_0021:  ldloc.1
  IL_0022:  brfalse.s  IL_002a
  IL_0024:  ldloc.0
  IL_0025:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_002a:  endfinally
}
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockOldMonitorEnter()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim allReferences As MetadataReference() = {
                            TestMetadata.Net20.mscorlib,
                            SystemRef,
                            MsvbRef}

            CompileAndVerify(source, allReferences,
).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldtoken    "C1"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  call       "Sub System.Threading.Monitor.Enter(Object)"
  .try
{
  IL_0011:  ldstr      "Inside SyncLock."
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  leave.s    IL_0024
}
  finally
{
  IL_001d:  ldloc.0
  IL_001e:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0023:  endfinally
}
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockTypeParameter()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        DoStuff(Of C1)()
    End Sub

    Public Shared Sub DoStuff(Of T as Class)()
        Dim lock as T = TryCast(new C1(), T)
        SyncLock lock
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Inside SyncLock.
]]>
            ).VerifyIL("C1.DoStuff", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  isinst     "T"
  IL_000a:  unbox.any  "T"
  IL_000f:  box        "T"
  IL_0014:  stloc.0
  IL_0015:  ldc.i4.0
  IL_0016:  stloc.1
  .try
{
  IL_0017:  ldloc.0
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_001f:  ldstr      "Inside SyncLock."
  IL_0024:  call       "Sub System.Console.WriteLine(String)"
  IL_0029:  leave.s    IL_0035
}
  finally
{
  IL_002b:  ldloc.1
  IL_002c:  brfalse.s  IL_0034
  IL_002e:  ldloc.0
  IL_002f:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0034:  endfinally
}
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockObjectType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim lock as new Object()
        SyncLock lock
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside SyncLock.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  .try
{
  IL_0013:  ldloc.0
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_001b:  ldstr      "Inside SyncLock."
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  leave.s    IL_0031
}
  finally
{
  IL_0027:  ldloc.1
  IL_0028:  brfalse.s  IL_0030
  IL_002a:  ldloc.0
  IL_002b:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0030:  endfinally
}
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockPropertyAccess()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared ReadOnly Property GetInstance() as C1
        Get
            return new C1()
        End Get
    End Property
    
    Public Shared Sub Main()
        SyncLock GetInstance
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside SyncLock.
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockNothing()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict ON
Imports System
Class Program
    Shared Sub Main()
        SyncLock Nothing
            Exit Sub
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  .try
{
  IL_000a:  ldloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0012:  leave.s    IL_001e
}
  finally
{
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001d
  IL_0017:  ldloc.0
  IL_0018:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001d:  endfinally
}
  IL_001e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockInterface()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict ON
Module M1
    Sub Main()
        SyncLock Goo
        End SyncLock
    End Sub
    Function Goo() As I1
        Return Nothing
    End Function
End Module
Interface I1
End Interface
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  call       "Function M1.Goo() As I1"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.1
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001b:  endfinally
}
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockSharedObject()
            Dim source =
<compilation>
    <file name="a.vb">
Class Program
    Shared Key As Object
    Shared Sub Main()
        SyncLock Key
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldsfld     "Program.Key As Object"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  .try
{
  IL_000e:  ldloc.0
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0016:  leave.s    IL_0022
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.0
  IL_001c:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0021:  endfinally
}
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockDelegate()
            Dim source =
<compilation>
    <file name="a.vb">
Delegate Sub D(p1 As Integer)
Class Program
    Public Shared Sub Main(args As String())
        SyncLock New D(AddressOf PM)
        End SyncLock
    End Sub
    Private Shared Sub PM(p1 As Integer)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub Program.PM(Integer)"
  IL_0007:  newobj     "Sub D..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  .try
{
  IL_000f:  ldloc.0
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0017:  leave.s    IL_0023
}
  finally
{
  IL_0019:  ldloc.1
  IL_001a:  brfalse.s  IL_0022
  IL_001c:  ldloc.0
  IL_001d:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0022:  endfinally
}
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CallMonitorExitInSyncLock()
            Dim source =
<compilation>
    <file name="a.vb">
Class Program
    Public Shared Sub Main(args As String())
        SyncLock args
            System.Threading.Monitor.Exit(args)
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  .try
{
  IL_0004:  ldloc.0
  IL_0005:  ldloca.s   V_1
  IL_0007:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_000c:  ldarg.0
  IL_000d:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0012:  leave.s    IL_001e
}
  finally
{
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001d
  IL_0017:  ldloc.0
  IL_0018:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001d:  endfinally
}
  IL_001e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CallMonitorExitInSyncLock_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class Program
    Public Shared Sub Main(args As String())
    End Sub
    Public Sub goo(obj As Object)
        SyncLock obj
            System.Threading.Monitor.Exit(obj)
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.goo", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  .try
{
  IL_000a:  ldloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0012:  ldarg.1
  IL_0013:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0018:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001d:  leave.s    IL_0029
}
  finally
{
  IL_001f:  ldloc.1
  IL_0020:  brfalse.s  IL_0028
  IL_0022:  ldloc.0
  IL_0023:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0028:  endfinally
}
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SyncLockMe()
            Dim source =
<compilation>
    <file name="a.vb">
Class Program
    Sub goo()
        SyncLock Me
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.goo", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  .try
{
  IL_0004:  ldloc.0
  IL_0005:  ldloca.s   V_1
  IL_0007:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_000c:  leave.s    IL_0018
}
  finally
{
  IL_000e:  ldloc.1
  IL_000f:  brfalse.s  IL_0017
  IL_0011:  ldloc.0
  IL_0012:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0017:  endfinally
}
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SyncLockString()
            Dim source =
<compilation>
    <file name="a.vb">
Class Program
    Sub goo()
        SyncLock "abc"
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.goo", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldstr      "abc"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.1
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001b:  endfinally
}
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub NestedSyncLock()
            Dim source =
<compilation>
    <file name="a.vb">
Public Class Program
    Public Sub goo()
        Dim syncroot As Object = New Object
        SyncLock syncroot
            SyncLock syncroot.ToString()
            End SyncLock
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.goo", <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  2
  .locals init (Object V_0, //syncroot
  Object V_1,
  Boolean V_2,
  Object V_3,
  Boolean V_4)
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
  .try
{
  IL_0015:  ldloc.1
  IL_0016:  ldloca.s   V_2
  IL_0018:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_001d:  ldloc.0
  IL_001e:  callvirt   "Function Object.ToString() As String"
  IL_0023:  stloc.3
  IL_0024:  ldc.i4.0
  IL_0025:  stloc.s    V_4
  .try
{
  IL_0027:  ldloc.3
  IL_0028:  ldloca.s   V_4
  IL_002a:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_002f:  leave.s    IL_0046
}
  finally
{
  IL_0031:  ldloc.s    V_4
  IL_0033:  brfalse.s  IL_003b
  IL_0035:  ldloc.3
  IL_0036:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_003b:  endfinally
}
}
  finally
{
  IL_003c:  ldloc.2
  IL_003d:  brfalse.s  IL_0045
  IL_003f:  ldloc.1
  IL_0040:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0045:  endfinally
}
  IL_0046:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub NestedSyncLock_1()
            Dim source =
<compilation>
    <file name="a.vb">
Public Class Program
    Public Sub goo()
        Dim syncroot As Object = New Object
        SyncLock syncroot
            SyncLock syncroot
            End SyncLock
        End SyncLock
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.goo", <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (Object V_0, //syncroot
  Object V_1,
  Boolean V_2,
  Object V_3,
  Boolean V_4)
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
  .try
{
  IL_0015:  ldloc.1
  IL_0016:  ldloca.s   V_2
  IL_0018:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_001d:  ldloc.0
  IL_001e:  stloc.3
  IL_001f:  ldloc.3
  IL_0020:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_0025:  ldc.i4.0
  IL_0026:  stloc.s    V_4
  .try
{
  IL_0028:  ldloc.3
  IL_0029:  ldloca.s   V_4
  IL_002b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0030:  leave.s    IL_0047
}
  finally
{
  IL_0032:  ldloc.s    V_4
  IL_0034:  brfalse.s  IL_003c
  IL_0036:  ldloc.3
  IL_0037:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_003c:  endfinally
}
}
  finally
{
  IL_003d:  ldloc.2
  IL_003e:  brfalse.s  IL_0046
  IL_0040:  ldloc.1
  IL_0041:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0046:  endfinally
}
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryAndSyncLock()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Try
            System.Threading.Monitor.Enter(Nothing)
            SyncLock Nothing
                Exit Try
            End SyncLock
        Catch ex As Exception
        End Try
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1,
  System.Exception V_2) //ex
  .try
{
  IL_0000:  ldnull
  IL_0001:  call       "Sub System.Threading.Monitor.Enter(Object)"
  IL_0006:  ldnull
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.1
  .try
{
  IL_0010:  ldloc.0
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0018:  leave.s    IL_0032
}
  finally
{
  IL_001a:  ldloc.1
  IL_001b:  brfalse.s  IL_0023
  IL_001d:  ldloc.0
  IL_001e:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0023:  endfinally
}
}
  catch System.Exception
{
  IL_0024:  dup
  IL_0025:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_002a:  stloc.2
  IL_002b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0030:  leave.s    IL_0032
}
  IL_0032:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryAndSyncLock_1()
            Dim source =
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        Try
            Dim o = Nothing
        Catch
        Finally
lab1:
            SyncLock String.Empty
                GoTo lab1
            End SyncLock
        End Try
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Object V_0,
                Boolean V_1)
  .try
  {
    .try
    {
      IL_0000:  leave.s    IL_002a
    }
    catch System.Exception
    {
      IL_0002:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0007:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_000c:  leave.s    IL_002a
    }
  }
  finally
  {
    IL_000e:  ldsfld     "String.Empty As String"
    IL_0013:  stloc.0
    IL_0014:  ldc.i4.0
    IL_0015:  stloc.1
    .try
    {
      IL_0016:  ldloc.0
      IL_0017:  ldloca.s   V_1
      IL_0019:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
      IL_001e:  leave.s    IL_000e
    }
    finally
    {
      IL_0020:  ldloc.1
      IL_0021:  brfalse.s  IL_0029
      IL_0023:  ldloc.0
      IL_0024:  call       "Sub System.Threading.Monitor.Exit(Object)"
      IL_0029:  endfinally
    }
  }
  IL_002a:  br.s       IL_002a
}
]]>)
        End Sub

        <Fact()>
        Public Sub JumpFormOneCaseToAnotherCase()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Select ""
            Case "a"
                SyncLock Nothing
                    GoTo lab1
                End SyncLock
            Case "b"
lab1:
        End Select
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (String V_0,
  Object V_1,
  Boolean V_2)
  IL_0000:  ldstr      ""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      "a"
  IL_000c:  ldc.i4.0
  IL_000d:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0012:  brfalse.s  IL_0022
  IL_0014:  ldloc.0
  IL_0015:  ldstr      "b"
  IL_001a:  ldc.i4.0
  IL_001b:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_0020:  pop
  IL_0021:  ret
  IL_0022:  ldnull
  IL_0023:  stloc.1
  IL_0024:  ldloc.1
  IL_0025:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_002a:  ldc.i4.0
  IL_002b:  stloc.2
  .try
{
  IL_002c:  ldloc.1
  IL_002d:  ldloca.s   V_2
  IL_002f:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0034:  leave.s    IL_0040
}
  finally
{
  IL_0036:  ldloc.2
  IL_0037:  brfalse.s  IL_003f
  IL_0039:  ldloc.1
  IL_003a:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_003f:  endfinally
}
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CustomerApplication()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main(args As String())
        Dim p As New D()
        Dim t As System.Threading.Thread() = New System.Threading.Thread(19) {}
        For i As Integer = 0 To 19
            t(i) = New System.Threading.Thread(AddressOf p.goo)
            t(i).Start()
        Next
        For i As Integer = 0 To 19
            t(i).Join()
        Next
        System.Console.WriteLine(p.s)
    End Sub
End Class
 
Class D
    Private syncroot As New Object()
    Public s As Integer
    Public Sub goo()
        SyncLock syncroot
            For i As Integer = 0 To 49999
                s = s + 1
            Next
        End SyncLock
        Return
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="1000000")
        End Sub

        <Fact()>
        Public Sub CustomerApplication_2()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Class Test
    Public Shared Sub Main()
        Dim p As New D()
        Dim t As System.Threading.Thread() = New System.Threading.Thread(9) {}
        For i As Integer = 0 To 4
            t(i) = New System.Threading.Thread(AddressOf p.goo)
            t(i).Start()
        Next
        For i As Integer = 0 To 4
            t(i).Join()
        Next
    End Sub
End Class
Class D
    Private syncroot As New Object()
    Public Sub goo()
        Try
            SyncLock syncroot
                System.Console.Write("Lock")
                Throw New Exception()
            End SyncLock
        Catch
            System.Console.Write("Catch")
        End Try
        Return
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("D.goo", <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  .try
{
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "D.syncroot As Object"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  .try
{
  IL_000f:  ldloc.0
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0017:  ldstr      "Lock"
  IL_001c:  call       "Sub System.Console.Write(String)"
  IL_0021:  newobj     "Sub System.Exception..ctor()"
  IL_0026:  throw
}
  finally
{
  IL_0027:  ldloc.1
  IL_0028:  brfalse.s  IL_0030
  IL_002a:  ldloc.0
  IL_002b:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0030:  endfinally
}
}
  catch System.Exception
{
  IL_0031:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0036:  ldstr      "Catch"
  IL_003b:  call       "Sub System.Console.Write(String)"
  IL_0040:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0045:  leave.s    IL_0047
}
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SyncLockNoCheckForSyncLockOnValueType()
            Dim source =
<compilation>
    <file name="a.vb">
Module Module1
    Private SyncObj As Object = New Object()
    Sub Main()
        SyncLock SyncObj
        End SyncLock
    End Sub
End Module        
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True)).VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldsfld     "Module1.SyncObj As Object"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.1
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_001b:  endfinally
}
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SyncLockWithCheckForSyncLockOnValueType()
            Dim source =
<compilation>
    <file name="a.vb">
Module Module1
    Private SyncObj As Object = New Object()
    Sub Main()
        SyncLock SyncObj
        End SyncLock
    End Sub
End Module        
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(False)).VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldsfld     "Module1.SyncObj As Object"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "Sub Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType(Object)"
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  .try
{
  IL_000e:  ldloc.0
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0016:  leave.s    IL_0022
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.0
  IL_001c:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0021:  endfinally
}
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(811916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/811916")>
        Public Sub VBLegacyThreading_VB7FreeThreading_SyncLock_SyncLock4()
            Dim source =
<compilation>
    <file name="a.vb">
Module Module1
    Dim x As New T1

    Sub Main()
        SyncLock x
            x.goo()
        End SyncLock
    End Sub
End Module

Class T1
    Public Sub goo()
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe).VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (Object V_0,
  Boolean V_1)
  IL_0000:  ldsfld     "Module1.x As T1"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0010:  ldsfld     "Module1.x As T1"
  IL_0015:  callvirt   "Sub T1.goo()"
  IL_001a:  leave.s    IL_0026
}
  finally
{
  IL_001c:  ldloc.1
  IL_001d:  brfalse.s  IL_0025
  IL_001f:  ldloc.0
  IL_0020:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0025:  endfinally
}
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_01()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter)

            CompileAndVerify(compilation, expectedOutput:="Inside SyncLock.")
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_02()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2)

            CompileAndVerify(compilation, expectedOutput:="Inside SyncLock.")
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_03()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2)

            AssertTheseEmitDiagnostics(compilation, <expected>
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Enter' is not defined.
        SyncLock GetType(C1)
                 ~~~~~~~~~~~
                                                    </expected>)
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_04()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Exit)

            AssertTheseEmitDiagnostics(compilation, <expected>
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Exit' is not defined.
        SyncLock GetType(C1)
        ~~~~~~~~~~~~~~~~~~~~~
                                                    </expected>)
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_05()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2)
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Exit)

            AssertTheseEmitDiagnostics(compilation, <expected>
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Exit' is not defined.
        SyncLock GetType(C1)
        ~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Enter' is not defined.
        SyncLock GetType(C1)
                 ~~~~~~~~~~~
                                                    </expected>)
        End Sub

        <Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")>
        Public Sub Bug1106943_06()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        SyncLock GetType(C1)
            Console.WriteLine("Inside SyncLock.")
        End SyncLock
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.MakeTypeMissing(WellKnownType.System_Threading_Monitor)

            AssertTheseEmitDiagnostics(compilation, <expected>
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Exit' is not defined.
        SyncLock GetType(C1)
        ~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Threading.Monitor.Enter' is not defined.
        SyncLock GetType(C1)
                 ~~~~~~~~~~~
                                                    </expected>)
        End Sub

    End Class
End Namespace
