' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class PDBSyncLockTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub SyncLockWithThrow()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

Class C1
    Public Shared Function Something(x As Integer) As C1
        Return New C1()
    End Function

    Public Shared Sub Main()
        Try
            Dim lock As New Object()
            SyncLock Something(12)
                Dim x As Integer = 23
                Throw New exception()
                Console.WriteLine("Inside SyncLock.")
            End SyncLock
        Catch
        End Try
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            Dim v = CompileAndVerify(compilation)

            v.VerifyIL("C1.Main", "
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (Object V_0, //lock
                Object V_1,
                Boolean V_2,
                Integer V_3, //x
                Boolean V_4)
 -IL_0000:  nop
  .try
  {
   -IL_0001:  nop
   -IL_0002:  newobj     ""Sub Object..ctor()""
    IL_0007:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_000c:  stloc.0
   -IL_000d:  nop
   -IL_000e:  ldc.i4.s   12
    IL_0010:  call       ""Function C1.Something(Integer) As C1""
    IL_0015:  stloc.1
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.2
    .try
    {
      IL_0018:  ldloc.1
      IL_0019:  ldloca.s   V_2
      IL_001b:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
      IL_0020:  nop
     -IL_0021:  ldc.i4.s   23
      IL_0023:  stloc.3
     -IL_0024:  newobj     ""Sub System.Exception..ctor()""
      IL_0029:  throw
    }
    finally
    {
     ~IL_002a:  ldloc.2
      IL_002b:  ldc.i4.0
      IL_002c:  ceq
      IL_002e:  stloc.s    V_4
      IL_0030:  ldloc.s    V_4
      IL_0032:  brtrue.s   IL_003b
      IL_0034:  ldloc.1
      IL_0035:  call       ""Sub System.Threading.Monitor.Exit(Object)""
      IL_003a:  nop
     -IL_003b:  nop
      IL_003c:  endfinally
    }
  }
  catch System.Exception
  {
   ~IL_003d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
   -IL_0042:  nop
    IL_0043:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0048:  leave.s    IL_004a
  }
 -IL_004a:  nop
 -IL_004b:  ret
}", sequencePoints:="C1.Main")

            v.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="21"/>
                    <slot kind="3" offset="55"/>
                    <slot kind="2" offset="55"/>
                    <slot kind="0" offset="99"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="11" startColumn="9" endLine="11" endColumn="12" document="0"/>
                <entry offset="0x2" startLine="12" startColumn="17" endLine="12" endColumn="37" document="0"/>
                <entry offset="0xd" startLine="13" startColumn="13" endLine="13" endColumn="35" document="0"/>
                <entry offset="0xe" startLine="13" startColumn="22" endLine="13" endColumn="35" document="0"/>
                <entry offset="0x21" startLine="14" startColumn="21" endLine="14" endColumn="38" document="0"/>
                <entry offset="0x24" startLine="15" startColumn="17" endLine="15" endColumn="38" document="0"/>
                <entry offset="0x2a" hidden="true" document="0"/>
                <entry offset="0x3b" startLine="17" startColumn="13" endLine="17" endColumn="25" document="0"/>
                <entry offset="0x3d" hidden="true" document="0"/>
                <entry offset="0x42" startLine="18" startColumn="9" endLine="18" endColumn="14" document="0"/>
                <entry offset="0x4a" startLine="19" startColumn="9" endLine="19" endColumn="16" document="0"/>
                <entry offset="0x4b" startLine="20" startColumn="5" endLine="20" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4c">
                <importsforward declaringType="C1" methodName="Something" parameterNames="x"/>
                <scope startOffset="0x2" endOffset="0x3c">
                    <local name="lock" il_index="0" il_start="0x2" il_end="0x3c" attributes="0"/>
                    <scope startOffset="0x21" endOffset="0x29">
                        <local name="x" il_index="3" il_start="0x21" il_end="0x29" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class

End Namespace
